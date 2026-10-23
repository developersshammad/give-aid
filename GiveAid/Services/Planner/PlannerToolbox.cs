using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using GiveAid.Data;
using GiveAid.Models;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Services.Planner
{
    // =====================================================================
    //  Data shapes the planner tools take and return (sent to the AI as JSON).
    // =====================================================================

    /// <summary>A planned budget line, e.g. "Reading glasses: 420,000".</summary>
    public class BudgetLine
    {
        public string Item { get; set; } = "";
        public decimal Amount { get; set; }
    }

    /// <summary>The draft programme the planner proposes (a write action - needs admin approval).</summary>
    public class DraftProposal
    {
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public int? NgoId { get; set; }
        public string Venue { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Beneficiaries { get; set; }
        public string Summary { get; set; } = "";
        public string Details { get; set; } = "";
        public List<BudgetLine> BudgetLines { get; set; } = new();
    }

    /// <summary>Volunteers the planner proposes to invite (a write action - needs admin approval).</summary>
    public class InviteProposal
    {
        /// <summary>Volunteer keys from find_volunteers, e.g. "m3" (member 3) or "i7" (interest form 7).</summary>
        public List<string> VolunteerIds { get; set; } = new();
        public string Message { get; set; } = "";
    }

    /// <summary>What a tool returned.</summary>
    public class ToolOutcome
    {
        public bool Ok { get; set; } = true;
        /// <summary>The data, serialised as JSON for the AI and the step card.</summary>
        public object? Data { get; set; }
        /// <summary>One plain sentence for the step card.</summary>
        public string Summary { get; set; } = "";
        public string Sources { get; set; } = "";
        /// <summary>True when the admin should look at this result (e.g. weather unavailable).</summary>
        public bool NeedsReview { get; set; }
    }

    /// <summary>
    /// The ONLY things the AI Programme Planner can do. The AI never touches the database or SQL
    /// directly: it can call these read-only tools, and it can PROPOSE a draft programme or
    /// volunteer invitations, which are validated here and wait for an admin's approval.
    /// </summary>
    public partial class PlannerToolbox
    {
        public static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        private readonly GiveAidContext _db;
        private readonly IHttpClientFactory _http;
        private readonly ILogger<PlannerToolbox> _log;

        public PlannerToolbox(GiveAidContext db, IHttpClientFactory http, ILogger<PlannerToolbox> log)
        {
            _db = db;
            _http = http;
            _log = log;
        }

        // Category of a programme  ->  the NGO focus area / member interest word that matches it
        private static readonly Dictionary<string, string[]> CategoryFocus = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Education"] = new[] { "Education" },
            ["Health Care"] = new[] { "Health", "Elderly" },
            ["Specially Challenged Children"] = new[] { "Disabled" },
            ["Children"] = new[] { "Children" },
            ["Women Empowerment"] = new[] { "Women" },
            ["Youth Development"] = new[] { "Youth" },
            ["Elderly Care"] = new[] { "Elderly" },
            ["Disaster Relief"] = new[] { "Children", "Health" }
        };

        public static string[] Categories => GiveAid.Areas.Admin.Controllers.ProgrammesController.Categories;

        // =================================================================
        //  READ TOOLS (run straight away)
        // =================================================================

        /// <summary>NGOs ranked for a programme: past work in the same category, matching focus area, same province.</summary>
        public async Task<ToolOutcome> FindNgosAsync(string? category, string? place)
        {
            var ngos = await _db.Ngos.ToListAsync();
            var programmes = await _db.Programmes.Select(p => new { p.NgoId, p.Category, p.Title }).ToListAsync();
            var focus = category != null && CategoryFocus.TryGetValue(category, out var f) ? f : Array.Empty<string>();
            var province = PlaceBook.ProvinceOf(place);

            var ranked = ngos.Select(n =>
            {
                var past = programmes.Where(p => p.NgoId == n.Id && string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
                bool focusMatch = focus.Any(x => n.FocusArea.Contains(x, StringComparison.OrdinalIgnoreCase));
                bool near = province != null && PlaceBook.ProvinceOf(n.City) == province;
                int score = past.Count * 3 + (focusMatch ? 2 : 0) + (near ? 1 : 0);
                var why = new List<string>();
                if (past.Count > 0) why.Add($"ran {past.Count} {category} programme{(past.Count == 1 ? "" : "s")} before ({string.Join(", ", past.Select(p => p.Title))})");
                if (focusMatch) why.Add($"focus area is {n.FocusArea}");
                if (near) why.Add($"based in {n.City}, same province");
                return new { ngo_id = n.Id, name = n.Name, focus_area = n.FocusArea, city = n.City, score, reason = why.Count > 0 ? string.Join("; ", why) : "no direct match" };
            })
            .OrderByDescending(x => x.score).ThenBy(x => x.name).Take(5).ToList();

            var top = ranked.FirstOrDefault();
            return new ToolOutcome
            {
                Data = new { ngos = ranked },
                Summary = top != null && top.score > 0 ? $"Best match: {top.name} — {top.reason}." : "No associated NGO matches this kind of programme.",
                Sources = "Associated NGOs, past programmes",
                NeedsReview = top == null || top.score == 0
            };
        }

        /// <summary>Other programmes on overlapping dates (published and drafts).</summary>
        public async Task<ToolOutcome> CheckCalendarAsync(DateTime start, DateTime end, int? ngoId, string? place)
        {
            if (end < start) end = start;
            var all = await _db.Programmes.Include(p => p.Ngo).ToListAsync();
            var province = PlaceBook.ProvinceOf(place);
            var clashes = all.Where(p => p.StartDate.Date <= end.Date && (p.EndDate ?? p.StartDate).Date >= start.Date)
                .Select(p => new
                {
                    programme_id = p.Id, title = p.Title, start_date = p.StartDate.ToString("yyyy-MM-dd"),
                    end_date = (p.EndDate ?? p.StartDate).ToString("yyyy-MM-dd"), venue = p.Venue,
                    same_ngo = ngoId != null && p.NgoId == ngoId,
                    same_province = province != null && PlaceBook.ProvinceOf(p.Venue) == province,
                    is_draft = !p.IsPublished
                }).ToList();

            bool serious = clashes.Any(c => c.same_ngo);
            return new ToolOutcome
            {
                Data = new { start_date = start.ToString("yyyy-MM-dd"), end_date = end.ToString("yyyy-MM-dd"), clashes },
                Summary = clashes.Count == 0 ? "No other programme on these dates."
                        : serious ? $"Clash: the same NGO already runs \"{clashes.First(c => c.same_ngo).title}\" on these dates."
                        : $"{clashes.Count} other programme(s) on these dates, run by other NGOs — no direct clash.",
                Sources = "Programmes calendar",
                NeedsReview = serious
            };
        }

        /// <summary>
        /// Weather for the place and dates from Open-Meteo (free, no key). A real forecast only
        /// exists ~2 weeks ahead; for later dates it shows the same dates LAST YEAR as a guide.
        /// Never throws: when offline it says so and asks the admin to check by hand.
        /// </summary>
        public async Task<ToolOutcome> GetWeatherAsync(string? place, DateTime start, DateTime end, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(place))
                return new ToolOutcome { Ok = false, Summary = "No place given, so the weather could not be checked.", Sources = "—", NeedsReview = true, Data = new { error = "no place" } };
            if (end < start) end = start;
            if ((end - start).TotalDays > 15) end = start.AddDays(15);

            try
            {
                var client = _http.CreateClient("openmeteo");
                var known = PlaceBook.Get(place);
                double lat, lon; string name;
                if (known != null) { lat = known.Lat; lon = known.Lon; name = known.Name; }
                else
                {
                    var geo = JsonNode.Parse(await client.GetStringAsync(
                        $"https://geocoding-api.open-meteo.com/v1/search?name={WebUtility.UrlEncode(place)}&count=1&language=en&format=json&countryCode=PK", ct));
                    var first = geo?["results"]?[0];
                    if (first == null)
                        return new ToolOutcome { Ok = false, Summary = $"Could not find \"{place}\" on the map — check the weather by hand.", Sources = "Open-Meteo geocoding", NeedsReview = true, Data = new { error = "place not found" } };
                    lat = first["latitude"]!.GetValue<double>(); lon = first["longitude"]!.GetValue<double>(); name = first["name"]!.GetValue<string>();
                }

                bool forecast = start.Date <= DateTime.Today.AddDays(14);
                var from = forecast ? start : start.AddYears(-1);
                var to = forecast ? end : end.AddYears(-1);
                var host = forecast ? "https://api.open-meteo.com/v1/forecast" : "https://archive-api.open-meteo.com/v1/archive";
                var url = string.Create(CultureInfo.InvariantCulture,
                    $"{host}?latitude={lat:0.00}&longitude={lon:0.00}&daily=temperature_2m_max,temperature_2m_min,precipitation_sum&timezone=auto&start_date={from:yyyy-MM-dd}&end_date={to:yyyy-MM-dd}");
                var w = JsonNode.Parse(await client.GetStringAsync(url, ct))!["daily"]!;

                var days = new List<object>();
                double maxT = double.MinValue, minT = double.MaxValue, rain = 0, wettest = 0;
                var time = w["time"]!.AsArray();
                for (int i = 0; i < time.Count; i++)
                {
                    double? tx = Num(w["temperature_2m_max"]?[i]), tn = Num(w["temperature_2m_min"]?[i]), pr = Num(w["precipitation_sum"]?[i]);
                    days.Add(new { date = time[i]!.GetValue<string>(), max_c = tx, min_c = tn, rain_mm = pr });
                    if (tx.HasValue) maxT = Math.Max(maxT, tx.Value);
                    if (tn.HasValue) minT = Math.Min(minT, tn.Value);
                    if (pr.HasValue) { rain += pr.Value; wettest = Math.Max(wettest, pr.Value); }
                }

                if (time.Count == 0 || maxT == double.MinValue)
                    return new ToolOutcome { Ok = false, Summary = "The weather service returned no data for these dates — please check by hand.", Sources = "Open-Meteo", NeedsReview = true, Data = new { error = "no data" } };

                var advice = new List<string>();
                if (wettest >= 20) advice.Add("heavy rain is possible — plan covered space and dry storage");
                if (maxT >= 40) advice.Add("very hot — shade, water and ORS for patients and volunteers");
                if (minT != double.MaxValue && minT <= 5) advice.Add("cold nights — blankets for overnight stays");
                if (advice.Count == 0) advice.Add("no weather problems expected");

                string basis = forecast ? "forecast" : "same dates last year (a real forecast is only available about 2 weeks ahead)";
                return new ToolOutcome
                {
                    Data = new { place = name, basis, days, advice },
                    Summary = string.Create(CultureInfo.InvariantCulture,
                        $"{name}, {basis}: {minT:0}–{maxT:0} °C, {rain:0} mm rain in total. {char.ToUpper(advice[0][0]) + advice[0][1..]}."),
                    Sources = forecast ? "Open-Meteo forecast" : "Open-Meteo weather archive",
                    NeedsReview = wettest >= 20 || maxT >= 40
                };
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                _log.LogWarning(ex, "Weather check failed for {Place}", place);
                return new ToolOutcome
                {
                    Ok = false,
                    Data = new { error = "weather service not reachable" },
                    Summary = "Could not reach the weather service (no internet?) — please check the weather by hand.",
                    Sources = "Open-Meteo (not reachable)",
                    NeedsReview = true
                };
            }
        }

        /// <summary>
        /// Possible volunteers: people who filled "I want to take part" forms and members whose
        /// interests match. Only names, city and history are shared — never e-mails or phone numbers.
        /// Language: members do not record languages, so it matches people who mentioned it in a
        /// message or who live in the province where it is spoken (the reason says which).
        /// </summary>
        public async Task<ToolOutcome> FindVolunteersAsync(string? category, string? place, string? language, int limit = 10)
        {
            var province = PlaceBook.ProvinceOf(place);
            var langProvince = language?.ToLowerInvariant() switch
            {
                "sindhi" or "dhatki" => "Sindh", "pashto" or "hindko" => "Khyber Pakhtunkhwa",
                "balochi" or "brahui" => "Balochistan", "punjabi" or "saraiki" => "Punjab", _ => null
            };
            var focus = category != null && CategoryFocus.TryGetValue(category, out var f) ? f : Array.Empty<string>();

            var interests = await _db.ProgrammeInterests.Include(i => i.Programme).ToListAsync();
            var members = await _db.Members.Where(m => m.IsActive).ToListAsync();

            var people = new Dictionary<string, (string Key, string Name, string? City, int Score, List<string> Why)>(StringComparer.OrdinalIgnoreCase);
            void Add(string email, string key, string name, string? city, int score, string why)
            {
                if (!people.TryGetValue(email, out var p)) p = (key, name, city, 0, new List<string>());
                p.Score += score;
                if (!p.Why.Contains(why)) p.Why.Add(why);
                if (p.City == null && city != null) p.City = city;
                if (p.Key.StartsWith('i') && key.StartsWith('m')) p.Key = key;    // prefer the member record
                people[email] = p;
            }

            foreach (var i in interests)
            {
                var member = i.MemberId != null ? members.FirstOrDefault(m => m.Id == i.MemberId) : null;
                string key = member != null ? "m" + member.Id : "i" + i.Id;
                int score = i.ParticipationType == "Volunteer" ? 2 : 1;
                if (i.Programme != null && string.Equals(i.Programme.Category, category, StringComparison.OrdinalIgnoreCase)) score += 3;
                Add(i.Email, key, i.Name, member?.City, score, $"{i.ParticipationType.ToLower()} for \"{i.Programme?.Title}\"");
                if (language != null && (i.Message ?? "").Contains(language, StringComparison.OrdinalIgnoreCase))
                    Add(i.Email, key, i.Name, member?.City, 3, $"mentioned {language} in their message");
            }
            foreach (var m in members)
            {
                var interestsText = m.AreasOfInterest ?? "";
                if (focus.Any(x => interestsText.Contains(x, StringComparison.OrdinalIgnoreCase)))
                    Add(m.Email, "m" + m.Id, m.FullName, m.City, 2, $"interested in {interestsText}");
            }
            // location bonus
            foreach (var email in people.Keys.ToList())
            {
                var p = people[email];
                var prov = PlaceBook.ProvinceOf(p.City);
                if (province != null && prov == province) { p.Score += 2; p.Why.Add($"lives in {p.City}, same province"); }
                if (langProvince != null && prov == langProvince && !p.Why.Any(w => w.Contains(language!)))
                { p.Score += 1; p.Why.Add($"lives in {langProvince} (may speak {language} — not recorded)"); }
                people[email] = p;
            }

            var list = people.Values.OrderByDescending(p => p.Score).ThenBy(p => p.Name).Take(limit)
                .Select(p => new { volunteer_id = p.Key, name = p.Name, city = p.City, score = p.Score, reason = string.Join("; ", p.Why) })
                .ToList();
            int speakers = language == null ? 0 : list.Count(v => v.reason.Contains($"mentioned {language}"));
            return new ToolOutcome
            {
                Data = new { volunteers = list, note = "Languages are not stored for members; a language match comes from a message or home province." },
                Summary = list.Count == 0 ? "No volunteers found yet — share the programme to find some."
                        : $"{list.Count} possible volunteer{(list.Count == 1 ? "" : "s")}" + (language != null ? $"; {speakers} mentioned {language}" : "") + ".",
                Sources = "Programme interest forms, members",
                NeedsReview = list.Count == 0
            };
        }

        /// <summary>
        /// Budget from REAL past spending: cost per person of each line in programmes of the same
        /// category, scaled to the new number of people. Falls back to all categories (low
        /// confidence) and never makes numbers up when there is no data.
        /// </summary>
        public async Task<ToolOutcome> EstimateBudgetAsync(string? category, int? people)
        {
            if (people is null or <= 0)
                return new ToolOutcome { Ok = false, Summary = "The number of people is unknown, so no budget can be estimated — please enter it.", Sources = "—", NeedsReview = true, Data = new { error = "people unknown" } };

            var lines = await _db.ProgrammeCostLines.Include(c => c.Programme).ToListAsync();
            var usable = lines.Where(c => c.Programme?.Beneficiaries > 0).ToList();
            var same = usable.Where(c => string.Equals(c.Programme!.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();

            if (same.Count > 0)
            {
                var progs = same.Select(c => c.Programme!).DistinctBy(p => p.Id).ToList();
                // average cost per person of each item across the matching programmes
                var est = same.GroupBy(c => c.Item)
                    .Select(g => new BudgetLine
                    {
                        Item = g.Key,
                        Amount = RoundTo(g.Average(c => c.Amount / c.Programme!.Beneficiaries!.Value) * people.Value, 1000)
                    })
                    .OrderByDescending(l => l.Amount).Take(10).ToList();
                decimal total = est.Sum(l => l.Amount);
                return new ToolOutcome
                {
                    Data = new { confidence = progs.Count >= 2 ? "good" : "fair", based_on = progs.Select(p => $"{p.Title} ({p.Beneficiaries} people)"), people, lines = est, total },
                    Summary = $"About Rs {total:#,##0} for {people:#,##0} people, based on {string.Join(" and ", progs.Select(p => p.Title))}.",
                    Sources = "Past programme budgets",
                    NeedsReview = progs.Count < 2
                };
            }
            if (usable.Count > 0)
            {
                var perPerson = usable.GroupBy(c => c.ProgrammeId).Average(g => g.Sum(c => c.Amount) / g.First().Programme!.Beneficiaries!.Value);
                var est = new List<BudgetLine> { new() { Item = "Programme costs (average of all past programmes)", Amount = RoundTo(perPerson * people.Value, 1000) } };
                return new ToolOutcome
                {
                    Data = new { confidence = "low", based_on = new[] { "all past programmes (none in this category)" }, people, lines = est, total = est[0].Amount },
                    Summary = $"Rough guess Rs {est[0].Amount:#,##0} — no past {category} programme to compare with, so please check it.",
                    Sources = "Past programme budgets (other categories)",
                    NeedsReview = true
                };
            }
            return new ToolOutcome { Ok = false, Summary = "No past budgets are recorded yet, so the admin must enter the budget.", Sources = "—", NeedsReview = true, Data = new { confidence = "none", lines = Array.Empty<BudgetLine>() } };
        }

        private static double? Num(JsonNode? n) => n == null ? null : n.GetValue<double>();

        private static decimal RoundTo(decimal v, decimal step) => Math.Max(step, Math.Round(v / step) * step);

        // =================================================================
        //  WRITE PROPOSALS (validated here, executed only after approval)
        // =================================================================

        /// <summary>Server-side checks on a proposed draft. Returns a list of problems (empty = valid).</summary>
        public async Task<List<string>> ValidateDraftAsync(DraftProposal d)
        {
            var e = new List<string>();
            d.Title = (d.Title ?? "").Trim(); d.Venue = (d.Venue ?? "").Trim(); d.Summary = (d.Summary ?? "").Trim();
            d.Details = (d.Details ?? "").Trim(); d.Category = (d.Category ?? "").Trim();
            if (d.Title.Length < 3 || d.Title.Length > 150) e.Add("title must be 3 to 150 characters");
            var cat = Categories.FirstOrDefault(c => string.Equals(c, d.Category, StringComparison.OrdinalIgnoreCase));
            if (cat == null) e.Add("category must be one of: " + string.Join(", ", Categories)); else d.Category = cat;
            if (d.Venue.Length == 0 || d.Venue.Length > 150) e.Add("venue is required (max 150 characters)");
            if (d.StartDate.Date < DateTime.Today) e.Add("start_date cannot be in the past");
            if (d.EndDate.HasValue && d.EndDate.Value.Date < d.StartDate.Date) e.Add("end_date cannot be before start_date");
            if (d.EndDate.HasValue && d.EndDate.Value.Date == d.StartDate.Date) d.EndDate = null;
            if (d.Summary.Length == 0 || d.Summary.Length > 300) e.Add("summary is required (max 300 characters)");
            if (d.Details.Length == 0 || d.Details.Length > 4000) e.Add("details are required (max 4000 characters)");
            if (d.Beneficiaries is < 0 or > 10_000_000) e.Add("beneficiaries must be between 0 and 10,000,000");
            if (d.NgoId.HasValue && !await _db.Ngos.AnyAsync(n => n.Id == d.NgoId)) e.Add($"ngo_id {d.NgoId} does not exist");
            d.BudgetLines = (d.BudgetLines ?? new()).Where(l => !string.IsNullOrWhiteSpace(l.Item)).ToList();
            if (d.BudgetLines.Count > 20) e.Add("at most 20 budget lines");
            foreach (var l in d.BudgetLines)
            {
                l.Item = l.Item.Trim();
                if (l.Item.Length > 100) e.Add($"budget item \"{l.Item[..30]}...\" is longer than 100 characters");
                if (l.Amount < 0 || l.Amount > 1_000_000_000) e.Add($"budget amount for \"{l.Item}\" is not valid");
            }
            return e;
        }

        public async Task<List<string>> ValidateInvitesAsync(InviteProposal p)
        {
            var e = new List<string>();
            p.VolunteerIds = (p.VolunteerIds ?? new()).Select(x => (x ?? "").Trim()).Where(x => x.Length > 0).Distinct().ToList();
            p.Message = (p.Message ?? "").Trim();
            if (p.VolunteerIds.Count == 0) e.Add("choose at least one volunteer_id from find_volunteers");
            if (p.VolunteerIds.Count > 50) e.Add("at most 50 volunteers per invitation");
            if (p.Message.Length < 10 || p.Message.Length > 1500) e.Add("message must be 10 to 1500 characters");
            foreach (var id in p.VolunteerIds)
                if (await ResolveVolunteerAsync(id) == null) e.Add($"volunteer_id \"{id}\" does not exist");
            return e;
        }

        /// <summary>Turns a volunteer key ("m3" / "i7") into a name and e-mail (server side only).</summary>
        public async Task<(string Name, string Email)?> ResolveVolunteerAsync(string key)
        {
            if (key.Length < 2 || !int.TryParse(key[1..], out int id)) return null;
            if (key[0] == 'm')
            {
                var m = await _db.Members.FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
                return m == null ? null : (m.FullName, m.Email);
            }
            if (key[0] == 'i')
            {
                var i = await _db.ProgrammeInterests.FirstOrDefaultAsync(x => x.Id == id);
                return i == null ? null : (i.Name, i.Email);
            }
            return null;
        }

        /// <summary>Picture used for a new programme until the admin uploads one.</summary>
        public static string DefaultImageFor(string category) => category switch
        {
            "Education" => "/images/programmes/education.webp",
            "Health Care" => "/images/programmes/health.webp",
            "Specially Challenged Children" => "/images/programmes/special.webp",
            "Women Empowerment" => "/images/programmes/women.webp",
            "Youth Development" => "/images/programmes/youth.webp",
            "Elderly Care" => "/images/programmes/elderly.webp",
            _ => "/images/programmes/children.webp"
        };
    }
}
