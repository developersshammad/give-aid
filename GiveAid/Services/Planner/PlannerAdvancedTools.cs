using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using GiveAid.Models;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Services.Planner
{
    /// <summary>
    /// Advanced planner tools (all read-only, they never change data):
    ///   check_funding    - is the money there? raised for the matching cause vs. the budget
    ///   plan_logistics   - volunteers, staff, shifts and supplies the programme needs
    ///   suggest_dates    - free date windows when the chosen dates clash or the weather is bad
    ///   assess_risks     - every risk found so far, with a fix and a 0-100 readiness score
    ///   build_timeline   - a task list with due dates, counting back from the start date
    ///   write_outreach   - WhatsApp (English + Roman Urdu), SMS and social-media texts
    /// The numbers come from the database and simple published rules - nothing is invented.
    /// </summary>
    public partial class PlannerToolbox
    {
        // programme category -> donation cause that pays for it
        private static readonly Dictionary<string, string[]> CategoryCause = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Education"] = new[] { "Education" },
            ["Health Care"] = new[] { "Elderly", "Children" },
            ["Specially Challenged Children"] = new[] { "Disabled" },
            ["Children"] = new[] { "Children" },
            ["Women Empowerment"] = new[] { "Women" },
            ["Youth Development"] = new[] { "Youth" },
            ["Elderly Care"] = new[] { "Elderly" },
            ["Disaster Relief"] = new[] { "Children", "Elderly" }
        };

        // how many people one volunteer can look after in a day, and the specialists needed
        private static readonly Dictionary<string, (int PerVolunteer, (string Role, int PerDay)[] Staff, string[] Supplies)> Logistics = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Health Care"] = (40, new[] { ("Doctor / specialist", 120), ("Nurse or paramedic", 80), ("Pharmacist", 300) },
                new[] { "Registration desk, tokens and patient forms", "Medicines and medical consumables", "Examination tables, screens and chairs", "Drinking water, ORS and first-aid kit", "Generator or UPS for equipment" }),
            ["Education"] = (60, new[] { ("Teacher / counsellor", 80), ("Admission officer", 150) },
                new[] { "Admission forms and student register", "School bags, books and stationery", "Tables, chairs and shade", "Drinking water" }),
            ["Specially Challenged Children"] = (15, new[] { ("Therapist / specialist", 25), ("Technician for fittings", 40) },
                new[] { "Assessment forms and consent sheets", "Wheelchairs, hearing aids or therapy kits", "Accessible ramp and waiting area", "Drinking water and snacks" }),
            ["Children"] = (50, new[] { ("Child-safety officer", 200) },
                new[] { "Beneficiary list and tokens", "Relief items (blankets, clothes, kits)", "Loading vehicle and storage", "Drinking water" }),
            ["Women Empowerment"] = (25, new[] { ("Trainer / instructor", 30), ("Female security / coordinator", 150) },
                new[] { "Registration forms", "Training materials, machines or kits", "Separate seating and prayer space", "Refreshments" }),
            ["Youth Development"] = (30, new[] { ("Coach / mentor", 40), ("First-aid officer", 200) },
                new[] { "Registration and team lists", "Sports kits or workshop materials", "Ground or hall booking", "Water and first-aid kit" }),
            ["Elderly Care"] = (30, new[] { ("Doctor", 100), ("Nurse", 70) },
                new[] { "Wheelchairs and seating with back support", "Blood-pressure and sugar test kits", "Medicines", "Water and light meals" }),
            ["Disaster Relief"] = (40, new[] { ("Relief coordinator", 300), ("Medical officer", 150) },
                new[] { "Ration packs and clean water", "Tents, tarpaulins and blankets", "Hygiene kits", "Trucks and a warehouse", "First-aid and medicines" })
        };

        // ---------------------------------------------------------------
        //  check_funding
        // ---------------------------------------------------------------
        public async Task<ToolOutcome> CheckFundingAsync(string? category, decimal? budget)
        {
            var causeNames = category != null && CategoryCause.TryGetValue(category, out var c) ? c : Array.Empty<string>();
            var causes = await _db.Causes.ToListAsync();
            var matched = causes.Where(x => causeNames.Any(n => string.Equals(n, x.Name, StringComparison.OrdinalIgnoreCase))).ToList();
            var ids = matched.Select(x => x.Id).ToHashSet();

            var donations = await _db.Donations.Where(d => d.Status == "Successful").ToListAsync();
            var forCause = donations.Where(d => ids.Contains(d.CauseId)).ToList();
            decimal raised = forCause.Sum(d => d.Amount);
            decimal last30 = forCause.Where(d => d.DonatedAt >= DateTime.Now.AddDays(-30)).Sum(d => d.Amount);

            // money already promised to other upcoming programmes of the same kind
            var lines = await _db.ProgrammeCostLines.Include(l => l.Programme).ToListAsync();
            decimal committed = lines.Where(l => l.Programme != null && string.Equals(l.Programme.Category, category, StringComparison.OrdinalIgnoreCase)
                                                 && (l.Programme.EndDate ?? l.Programme.StartDate).Date >= DateTime.Today).Sum(l => l.Amount);
            decimal available = Math.Max(0, raised - committed);

            if (budget is null or <= 0)
                return new ToolOutcome
                {
                    Ok = false, NeedsReview = true, Sources = "Donations, causes",
                    Data = new { causes = matched.Select(m => m.Name), raised, committed, available, budget = (decimal?)null },
                    Summary = $"Rs {available:#,##0} is available in {(matched.Count > 0 ? string.Join(" + ", matched.Select(m => m.Name)) : "the matching causes")}, but there is no budget to compare it with."
                };

            decimal gap = Math.Max(0, budget.Value - available);
            int covered = (int)Math.Min(100, Math.Round(available / budget.Value * 100));
            int? daysToClose = gap > 0 && last30 > 0 ? (int)Math.Ceiling(gap / (last30 / 30m)) : null;

            return new ToolOutcome
            {
                Data = new
                {
                    causes = matched.Select(m => m.Name), raised, committed, available, budget, gap, covered_percent = covered,
                    raised_last_30_days = last30, days_to_close_gap_at_current_pace = daysToClose,
                    suggested_appeal_target = gap > 0 ? RoundTo(gap * 1.1m, 5000) : 0m
                },
                Summary = gap == 0
                    ? $"Fully funded: Rs {available:#,##0} available in {string.Join(" + ", matched.Select(m => m.Name))} covers the Rs {budget:#,##0} budget."
                    : $"{covered}% funded — Rs {gap:#,##0} short." + (daysToClose != null ? $" At the last 30 days' pace that takes about {daysToClose} days to raise." : " No recent donations to this cause, so an appeal is needed."),
                Sources = "Donations, causes, upcoming programme budgets",
                NeedsReview = gap > 0
            };
        }

        // ---------------------------------------------------------------
        //  plan_logistics
        // ---------------------------------------------------------------
        public async Task<ToolOutcome> PlanLogisticsAsync(string? category, int? people, DateTime start, DateTime end, int volunteersFound)
        {
            await Task.CompletedTask;
            if (end < start) end = start;
            int days = (int)(end.Date - start.Date).TotalDays + 1;
            if (people is null or <= 0)
                return new ToolOutcome { Ok = false, NeedsReview = true, Sources = "Planning rules", Summary = "Number of people unknown, so staffing cannot be worked out.", Data = new { error = "people unknown" } };

            var rule = category != null && Logistics.TryGetValue(category, out var r) ? r : Logistics["Children"];
            int perDay = (int)Math.Ceiling(people.Value / (double)days);
            int shifts = perDay > 600 ? 2 : 1;
            int volunteers = (int)Math.Ceiling(perDay / (double)rule.PerVolunteer) + 2;   // +2 for the registration desk
            var staff = rule.Staff.Select(s => new { role = s.Role, needed = Math.Max(1, (int)Math.Ceiling(perDay / (double)s.PerDay)) }).ToList();
            int shortfall = Math.Max(0, volunteers - volunteersFound);

            return new ToolOutcome
            {
                Data = new
                {
                    days, people_per_day = perDay, shifts, volunteers_needed = volunteers, volunteers_found = volunteersFound,
                    volunteer_shortfall = shortfall, staff, supplies = rule.Supplies,
                    rule = $"1 volunteer per {rule.PerVolunteer} people a day, plus 2 at registration"
                },
                Summary = $"{days} day{(days == 1 ? "" : "s")}, about {perDay:#,##0} people a day{(shifts > 1 ? " in 2 shifts" : "")}: {volunteers} volunteers and " +
                          string.Join(", ", staff.Select(s => $"{s.needed} {s.role.ToLowerInvariant()}")) + "." +
                          (shortfall > 0 ? $" {shortfall} more volunteers are needed than were found." : " Enough volunteers were found."),
                Sources = "Planning rules for this category",
                NeedsReview = shortfall > 0
            };
        }

        // ---------------------------------------------------------------
        //  suggest_dates
        // ---------------------------------------------------------------
        public async Task<ToolOutcome> SuggestDatesAsync(DateTime start, DateTime end, int? ngoId, string? place)
        {
            if (end < start) end = start;
            int length = (int)(end.Date - start.Date).TotalDays;
            var all = await _db.Programmes.ToListAsync();
            var province = PlaceBook.ProvinceOf(place);

            bool Busy(DateTime s, DateTime e) => all.Any(p => p.StartDate.Date <= e.Date && (p.EndDate ?? p.StartDate).Date >= s.Date
                                                          && ((ngoId != null && p.NgoId == ngoId) || (province != null && PlaceBook.ProvinceOf(p.Venue) == province)));
            var options = new List<(DateTime Start, DateTime End)>();
            var from = start.Date < DateTime.Today.AddDays(7) ? DateTime.Today.AddDays(7) : start.Date;
            for (var d = from; options.Count < 3 && d <= from.AddDays(56); d = d.AddDays(1))
            {
                if (d.DayOfWeek == DayOfWeek.Friday) continue;                         // avoid starting on Friday (Jumu'ah)
                var e = d.AddDays(length);
                if (Busy(d, e)) continue;
                if (options.Any(o => Math.Abs((o.Start - d).TotalDays) < 5)) continue;  // spread the options out
                options.Add((d, e));
            }
            var list = options.Select(o => new { start_date = o.Start.ToString("yyyy-MM-dd"), end_date = o.End.ToString("yyyy-MM-dd"), label = GoalParser.FormatDates(o.Start, o.End), lead_days = (o.Start - DateTime.Today).Days }).ToList();
            bool chosenFree = !Busy(start, end);
            return new ToolOutcome
            {
                Data = new { chosen_dates_free = chosenFree, options = list },
                Summary = (chosenFree ? "The chosen dates are free. " : "The chosen dates clash with the same NGO or another programme nearby. ") +
                          (list.Count > 0 ? "Free alternatives: " + string.Join("; ", list.Select(o => o.label)) + "." : "No free window in the next 8 weeks."),
                Sources = "Programmes calendar",
                NeedsReview = !chosenFree
            };
        }

        // ---------------------------------------------------------------
        //  assess_risks - reads the results of the steps already in this plan
        // ---------------------------------------------------------------
        public ToolOutcome AssessRisks(IReadOnlyList<PlannerStep> steps, DateTime start)
        {
            JsonNode? R(string tool) { var s = steps.LastOrDefault(x => x.Tool == tool); try { return s?.ResultJson != null ? JsonNode.Parse(s.ResultJson) : null; } catch { return null; } }
            bool Review(string tool) => steps.LastOrDefault(x => x.Tool == tool)?.Status is "NeedsReview" or "Failed";
            string? Expl(string tool) => steps.LastOrDefault(x => x.Tool == tool)?.Explanation;
            var risks = new List<(string Level, string Area, string Risk, string Fix)>();

            if (R(PlannerService.ToolGoal)?["assumptions"] is JsonArray asm && asm.Count > 0)
                risks.Add(("medium", "Goal", $"{asm.Count} detail(s) were guessed: {string.Join(" ", asm.Select(a => a?.ToString()))}", "Check the facts in step 1 and edit the draft before approving."));
            var topNgo = (R(PlannerService.ToolNgos)?["ngos"] as JsonArray)?.FirstOrDefault();
            if (topNgo == null || (topNgo["score"]?.GetValue<int>() ?? 0) == 0)
                risks.Add(("high", "Partner", "No associated NGO has done this kind of work before.", "Call two NGOs to confirm who can run it, or add a new partner NGO."));
            if (R(PlannerService.ToolCalendar)?["clashes"] is JsonArray cl && cl.Any(x => x?["same_ngo"]?.GetValue<bool>() == true))
                risks.Add(("high", "Calendar", "The same NGO is busy with another programme on these dates.", "Pick one of the free dates suggested, or ask a second NGO to help."));
            if (R(PlannerService.ToolWeather)?["advice"] is JsonArray adv)
                foreach (var a in adv.Select(x => x?.ToString() ?? "").Where(x => !x.StartsWith("no weather")))
                    risks.Add((a.Contains("heavy rain") || a.Contains("very hot") ? "medium" : "low", "Weather", char.ToUpper(a[0]) + a[1..] + ".", a.Contains("rain") ? "Book a covered hall and keep supplies off the ground." : a.Contains("hot") ? "Run in the morning, add shade, water and ORS." : "Arrange blankets and heaters."));
            else if (Review(PlannerService.ToolWeather))
                risks.Add(("low", "Weather", "The weather could not be checked automatically.", "Check the forecast a week before."));
            var log = R(ToolLogistics);
            int shortfall = log?["volunteer_shortfall"]?.GetValue<int>() ?? 0;
            if (shortfall > 0)
                risks.Add((shortfall >= 10 ? "high" : "medium", "People", $"{shortfall} more volunteers are needed than were found.", "Share the outreach messages and publish the programme early so people can sign up."));
            var fund = R(ToolFunding);
            int covered = fund?["covered_percent"]?.GetValue<int>() ?? -1;
            if (covered >= 0 && covered < 100)
                risks.Add((covered < 50 ? "high" : "medium", "Money", $"Only {covered}% of the budget is available (short by Rs {fund!["gap"]!.GetValue<decimal>():#,##0}).", $"Start an appeal for about Rs {fund["suggested_appeal_target"]!.GetValue<decimal>():#,##0} and ask partner companies."));
            if (Review(PlannerService.ToolBudget) && covered < 0)
                risks.Add(("medium", "Money", "The budget is a rough guess or missing.", "Enter the real costs in the draft before approving."));
            int lead = (start.Date - DateTime.Today).Days;
            if (lead < 14)
                risks.Add((lead < 7 ? "high" : "medium", "Time", $"Only {lead} day(s) to prepare.", "Move the start date by 2–3 weeks, or cut the number of people."));

            int score = Math.Clamp(100 - risks.Sum(r => r.Level == "high" ? 20 : r.Level == "medium" ? 10 : 4), 5, 100);
            string verdict = score >= 80 ? "Ready" : score >= 60 ? "Nearly ready" : score >= 40 ? "Needs work" : "Not ready";
            return new ToolOutcome
            {
                Data = new
                {
                    readiness = score, verdict,
                    risks = risks.OrderBy(r => r.Level == "high" ? 0 : r.Level == "medium" ? 1 : 2)
                                 .Select(r => new { level = r.Level, area = r.Area, risk = r.Risk, fix = r.Fix }).ToList()
                },
                Summary = $"Readiness {score}/100 — {verdict}. " + (risks.Count == 0 ? "No risks found." : $"{risks.Count(r => r.Level == "high")} high, {risks.Count(r => r.Level == "medium")} medium and {risks.Count(r => r.Level == "low")} low risk(s)."),
                Sources = "Results of the steps above",
                NeedsReview = risks.Any(r => r.Level == "high")
            };
        }

        // ---------------------------------------------------------------
        //  build_timeline
        // ---------------------------------------------------------------
        public ToolOutcome BuildTimeline(DateTime start, DateTime end, bool needsAppeal)
        {
            if (end < start) end = start;
            var plan = new List<(int Offset, string Task, string Owner)>
            {
                (-28, "Confirm the NGO, venue and permissions from the local administration", "Programme lead"),
                (-21, "Approve and publish the programme page; share the outreach messages", "Admin"),
                (-21, needsAppeal ? "Start the fundraising appeal for the funding gap" : "Confirm the money with the finance team", "Admin / finance"),
                (-14, "Order supplies and book transport", "Logistics"),
                (-10, "Send volunteer invitations and confirm specialist staff", "Volunteer coordinator"),
                (-3, "Volunteer briefing: roles, shifts and safety", "Volunteer coordinator"),
                (-1, "Set up the venue, registration desk and signs", "Logistics + volunteers"),
                (0, "Programme starts", "Everyone"),
                (1000, "Last day: count people served and take photos", "Programme lead"),
                (1003, "Upload photos to the Gallery and thank donors and volunteers", "Admin")
            };
            var today = DateTime.Today;
            var tasks = plan.Select(p =>
            {
                var due = p.Offset >= 1000 ? end.AddDays(p.Offset - 1000) : start.AddDays(p.Offset);
                string state = due < today ? "overdue" : due <= today.AddDays(3) ? "urgent" : "planned";
                if (due < today) due = today;
                return new { due_date = due.ToString("yyyy-MM-dd"), task = p.Task, owner = p.Owner, state };
            }).ToList();
            int late = tasks.Count(t => t.state == "overdue");
            return new ToolOutcome
            {
                Data = new { tasks },
                Summary = late == 0 ? $"{tasks.Count} tasks scheduled from {DateTime.Parse(tasks[0].due_date, CultureInfo.InvariantCulture):d MMM} to {DateTime.Parse(tasks[^1].due_date, CultureInfo.InvariantCulture):d MMM}."
                                    : $"{tasks.Count} tasks; {late} should already have started, so they are due today.",
                Sources = "Standard programme checklist",
                NeedsReview = late > 2
            };
        }

        // ---------------------------------------------------------------
        //  write_outreach
        // ---------------------------------------------------------------
        public static ToolOutcome WriteOutreach(string title, string? place, DateTime start, DateTime end, int? people, string audience, string? language)
        {
            string dates = GoalParser.FormatDates(start, end);
            string where = place ?? "our venue";
            string count = people.HasValue ? $"{people:#,##0} {audience}" : audience;
            var data = new
            {
                whatsapp_english = $"🌟 {title}\n📅 {dates}\n📍 {where}\n\nGive-AID and our partner NGO are serving {count}. Volunteers and donors are welcome — every rupee and every hour helps.\n👉 Sign up or donate on the Give-AID website.",
                whatsapp_roman_urdu = $"🌟 {title}\n📅 {dates}\n📍 {where}\n\nGive-AID apni partner NGO ke saath {count} ki khidmat kar raha hai. Volunteer banein ya donate karein — har rupay aur har ghanta kaam aata hai.\n👉 Give-AID ki website par register ya donate karein.",
                sms = Cut160($"Give-AID: {title}, {dates}, {where}. Volunteer or donate on our website. Shukriya!"),
                social_post = $"This {start:MMMM}, we're bringing {title.Split(" - ")[0].ToLowerInvariant()} to {where}. With your support we can reach {count}. " +
                              $"Join as a volunteer{(language != null ? $" ({language} speakers especially welcome)" : "")} or give to the cause. #GiveAID #Pakistan #{where.Replace(" ", "")}"
            };
            return new ToolOutcome { Data = data, Summary = "WhatsApp messages in English and Roman Urdu, an SMS and a social-media post are ready to copy.", Sources = "Programme details" };
        }

        private static string Cut160(string s) => s.Length <= 160 ? s : s[..157] + "...";

        public const string ToolFunding = "check_funding", ToolLogistics = "plan_logistics", ToolDates = "suggest_dates",
            ToolRisks = "assess_risks", ToolTimeline = "build_timeline", ToolOutreach = "write_outreach";
    }
}
