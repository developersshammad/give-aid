using System.Globalization;
using System.Text.RegularExpressions;

namespace GiveAid.Services.Planner
{
    /// <summary>What the planner understood from the admin's goal.</summary>
    public class GoalSpec
    {
        public string Title { get; set; } = "";
        /// <summary>Short phrase for the activity, e.g. "Free Eye Camp".</summary>
        public string Activity { get; set; } = "";
        public string Category { get; set; } = "";
        public string? Place { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? People { get; set; }
        /// <summary>Who is served, e.g. "patients", "children".</summary>
        public string Audience { get; set; } = "people";
        /// <summary>Language volunteers should speak, if the goal mentions one.</summary>
        public string? Language { get; set; }
        /// <summary>Things the planner had to guess - shown to the admin.</summary>
        public List<string> Assumptions { get; set; } = new();
    }

    /// <summary>
    /// Demo-mode "understanding" of a goal, written with plain rules (no AI).
    /// Works with English and Roman Urdu, for example:
    ///   "Free eye camp in Mithi for 1,500 patients, second week of November"
    ///   "Mithi mein 1500 mareezon ka eye camp, November ka doosra hafta"
    /// When a value is missing it makes a safe guess and records it in Assumptions.
    /// </summary>
    public static class GoalParser
    {
        private static readonly (string Category, string[] Words)[] CategoryWords =
        {
            ("Disaster Relief", new[] { "flood", "selab", "sailab", "earthquake", "zalzala", "relief", "disaster", "cyclone", "heatwave", "rain emergency" }),
            ("Health Care", new[] { "eye", "medical", "health", "clinic", "doctor", "dental", "vaccin", "camp", "hospital", "ilaj", "dawai", "medicine", "diabetes", "blood" }),
            ("Specially Challenged Children", new[] { "wheelchair", "disab", "special need", "specially", "autis", "hearing aid", "therapy", "sign language", "mazoor" }),
            ("Education", new[] { "school", "education", "book", "scholarship", "admission", "taleem", "class", "teacher", "tuition", "library" }),
            ("Women Empowerment", new[] { "women", "woman", "khawateen", "aurat", "sewing", "silai", "embroidery", "skills fair", "girls" }),
            ("Youth Development", new[] { "youth", "naujawan", "football", "cricket", "sports", "career", "vocational", "internship" }),
            ("Elderly Care", new[] { "elderly", "senior", "old age", "buzurg", "aged" }),
            ("Children", new[] { "child", "children", "bachon", "bachay", "bache", "orphan", "yateem", "winter", "blanket", "kambal", "jacket" })
        };

        private static readonly (string Phrase, string[] Words)[] ActivityWords =
        {
            ("Free Eye Camp", new[] { "eye camp", "eye-camp", "eye screening", "aankh" }),
            ("Free Medical Camp", new[] { "medical camp", "health camp", "free clinic" }),
            ("Flood Relief Camp", new[] { "flood", "selab", "sailab" }),
            ("Earthquake Relief", new[] { "earthquake", "zalzala" }),
            ("Winter Blanket Drive", new[] { "blanket", "kambal", "winter" }),
            ("Wheelchair Distribution", new[] { "wheelchair" }),
            ("School Admission Drive", new[] { "admission", "enrol", "back to school", "back-to-school" }),
            ("Scholarship Drive", new[] { "scholarship" }),
            ("Book Distribution", new[] { "book" }),
            ("Food Ration Drive", new[] { "ration", "food", "khana" }),
            ("Women's Skills Fair", new[] { "skills fair", "sewing", "silai", "embroidery" }),
            ("Youth Sports League", new[] { "football", "cricket", "sports", "league" }),
            ("Senior Citizens Health Day", new[] { "senior", "elderly", "buzurg" }),
            ("Blood Donation Camp", new[] { "blood" })
        };

        private static readonly Dictionary<string, int> Months = new()
        {
            ["january"] = 1, ["jan"] = 1, ["february"] = 2, ["feb"] = 2, ["march"] = 3, ["mar"] = 3, ["april"] = 4, ["apr"] = 4,
            ["may"] = 5, ["june"] = 6, ["jun"] = 6, ["july"] = 7, ["jul"] = 7, ["august"] = 8, ["aug"] = 8,
            ["september"] = 9, ["sept"] = 9, ["sep"] = 9, ["october"] = 10, ["oct"] = 10, ["november"] = 11, ["nov"] = 11,
            ["december"] = 12, ["dec"] = 12
        };
        private const string MonthRx = "january|february|march|april|may|june|july|august|september|october|november|december|jan|feb|mar|apr|jun|jul|aug|sept|sep|oct|nov|dec";

        private static readonly Dictionary<string, int> WeekOrdinals = new()
        {
            ["first"] = 1, ["1st"] = 1, ["pehla"] = 1, ["pehle"] = 1, ["pehlay"] = 1,
            ["second"] = 2, ["2nd"] = 2, ["doosra"] = 2, ["dusra"] = 2, ["doosre"] = 2, ["dusre"] = 2,
            ["third"] = 3, ["3rd"] = 3, ["teesra"] = 3, ["tisra"] = 3, ["teesre"] = 3,
            ["fourth"] = 4, ["4th"] = 4, ["chautha"] = 4, ["chotha"] = 4, ["chauthe"] = 4,
            ["last"] = 4, ["aakhri"] = 4, ["akhri"] = 4
        };
        private const string WeekWordRx = "week|hafta|hafte|haftay|hafty";

        private static readonly string[] Languages = { "Sindhi", "Pashto", "Punjabi", "Balochi", "Saraiki", "Urdu", "English", "Dhatki", "Brahui", "Hindko", "Shina", "Balti" };

        private static readonly (string Word, string Audience)[] AudienceWords =
        {
            ("patient", "patients"), ("mareez", "patients"), ("child", "children"), ("kid", "children"), ("bach", "children"),
            ("student", "students"), ("famil", "families"), ("ghar", "families"), ("women", "women"), ("khawateen", "women"),
            ("girl", "girls"), ("boy", "boys"), ("youth", "young people"), ("elderly", "senior citizens"), ("senior", "senior citizens"),
            ("participant", "participants"), ("people", "people"), ("person", "people"), ("log", "people"), ("afraad", "people")
        };

        public static GoalSpec Parse(string goal, DateTime today)
        {
            var spec = new GoalSpec();
            var text = goal.Trim();
            var lower = " " + text.ToLowerInvariant() + " ";

            // ---------- category: the group with the most keyword hits (first group wins a tie) ----------
            var best = CategoryWords
                .Select((c, i) => (c.Category, Hits: c.Words.Count(w => lower.Contains(w)), Index: i))
                .OrderByDescending(x => x.Hits).ThenBy(x => x.Index).First();
            if (best.Hits > 0) spec.Category = best.Category;
            else { spec.Category = "Children"; spec.Assumptions.Add("Could not tell the category, so \"Children\" was assumed."); }
            // "eye camp" talks about health even when it also mentions children
            if (Regex.IsMatch(lower, @"eye|medical camp|health camp|clinic")) spec.Category = "Health Care";

            // ---------- activity phrase + title ----------
            spec.Activity = ActivityWords.FirstOrDefault(a => a.Words.Any(w => lower.Contains(w))).Phrase
                            ?? spec.Category + " Programme";

            // ---------- number of people ----------
            var people = Regex.Match(lower, @"(\d[\d,]*)\s*\+?\s*(patients?|mareez\w*|children|child|kids|bach\w*|students?|famil\w*|ghar\w*|women|khawateen|girls?|boys?|youths?|elderly|seniors?|participants?|people|persons?|log\w*|afraad|beneficiaries)");
            if (people.Success && int.TryParse(people.Groups[1].Value.Replace(",", ""), out int n) && n > 0)
            {
                spec.People = n;
                spec.Audience = AudienceWords.FirstOrDefault(a => people.Groups[2].Value.StartsWith(a.Word)).Audience ?? "people";
            }
            else
            {
                // "for 800" without a noun
                var bare = Regex.Match(lower, @"(?:for|liye|ke liye)\s+(\d[\d,]{1,})\b");
                if (bare.Success && int.TryParse(bare.Groups[1].Value.Replace(",", ""), out n) && n >= 10) spec.People = n;
                else spec.Assumptions.Add("The goal does not say how many people will be served - the budget needs that number.");
            }

            // ---------- place ----------
            var known = PlaceBook.FindIn(text);
            if (known != null) spec.Place = known.Name;
            else
            {
                var m = Regex.Match(text, @"\b(?:in|at)\s+([A-Z][A-Za-z\-]+(?:\s+[A-Z][A-Za-z\-]+)?)");
                if (!m.Success) m = Regex.Match(text, @"\b([A-Z][A-Za-z\-]+(?:\s+[A-Z][A-Za-z\-]+)?)\s+(?:mein|main|me)\b");
                var candidate = m.Success ? m.Groups[1].Value : null;
                if (candidate != null && !Months.ContainsKey(candidate.ToLowerInvariant().Split(' ')[0])) spec.Place = candidate;
                else spec.Assumptions.Add("No place was found in the goal - please add the venue.");
            }

            // ---------- language volunteers should speak ----------
            spec.Language = Languages.FirstOrDefault(l => lower.Contains(l.ToLowerInvariant()));
            if (spec.Language == null && spec.Place != null)
            {
                // a sensible default from the province (only used to rank volunteers)
                spec.Language = PlaceBook.ProvinceOf(spec.Place) switch
                {
                    "Sindh" => "Sindhi", "Khyber Pakhtunkhwa" => "Pashto", "Balochistan" => "Balochi", _ => null
                };
            }

            // ---------- dates ----------
            ParseDates(lower, today, spec);

            spec.Title = spec.Place != null ? $"{spec.Activity} - {spec.Place}" : spec.Activity;
            if (spec.Title.Length > 150) spec.Title = spec.Title[..150];
            return spec;
        }

        private static void ParseDates(string lower, DateTime today, GoalSpec spec)
        {
            int days = 1;
            var dur = Regex.Match(lower, @"(\d+)\s*(?:-\s*)?(?:days?|din)\b");
            if (dur.Success) days = Math.Clamp(int.Parse(dur.Groups[1].Value), 1, 60);
            else if (Regex.IsMatch(lower, @"\b(one|ek|1)\s*(week|hafta)\b|week-long|hafte bhar")) days = 7;
            else if (Regex.IsMatch(lower, @"\b(two|do)\s*(days|din)\b")) days = 2;
            else if (Regex.IsMatch(lower, @"\b(three|teen)\s*(days|din)\b")) days = 3;

            DateTime? start = null, end = null;
            Match m;

            // "12-14 December", "12 to 14 dec", "12 se 14 december"
            if ((m = Regex.Match(lower, $@"\b(\d{{1,2}})\s*(?:-|–|to|se|till)\s*(\d{{1,2}})\s*(?:st|nd|rd|th)?\s*(?:of\s+)?({MonthRx})\b")).Success)
            {
                start = MakeDate(today, Months[m.Groups[3].Value], int.Parse(m.Groups[1].Value));
                if (start != null) end = start.Value.AddDays(Math.Max(0, int.Parse(m.Groups[2].Value) - int.Parse(m.Groups[1].Value)));
            }
            // "15 January", "15th of jan"
            else if ((m = Regex.Match(lower, $@"\b(\d{{1,2}})\s*(?:st|nd|rd|th)?\s*(?:of\s+)?({MonthRx})\b")).Success)
                start = MakeDate(today, Months[m.Groups[2].Value], int.Parse(m.Groups[1].Value));
            // "January 15"
            else if ((m = Regex.Match(lower, $@"\b({MonthRx})\s+(\d{{1,2}})(?:st|nd|rd|th)?\b(?!\s*(?:{AudienceAlternation()}))")).Success)
                start = MakeDate(today, Months[m.Groups[1].Value], int.Parse(m.Groups[2].Value));
            // "second week of November", "November ka doosra hafta", "november ke doosre haftay"
            else if ((m = Regex.Match(lower, $@"\b(\w+)\s+(?:{WeekWordRx})\s+(?:of|in)?\s*({MonthRx})\b")).Success && WeekOrdinals.ContainsKey(m.Groups[1].Value)
                  || (m = Regex.Match(lower, $@"\b({MonthRx})\s+(?:ka|ke|ki|mein|main)?\s*(\w+)\s+(?:{WeekWordRx})\b")).Success)
            {
                string ord = WeekOrdinals.ContainsKey(m.Groups[1].Value) ? m.Groups[1].Value : m.Groups[2].Value;
                string mon = Months.ContainsKey(m.Groups[2].Value) ? m.Groups[2].Value : m.Groups[1].Value;
                if (WeekOrdinals.TryGetValue(ord, out int week) && Months.TryGetValue(mon, out int month))
                {
                    start = MakeDate(today, month, 1 + 7 * (week - 1));
                    if (!dur.Success && days == 1) days = 3;                // a "week" camp: assume 3 days
                }
            }
            // "in November", "November mein"
            else if ((m = Regex.Match(lower, $@"\b({MonthRx})\b")).Success)
            {
                start = MakeDate(today, Months[m.Groups[1].Value], 10);
                spec.Assumptions.Add("Only the month was given, so the 10th was assumed as the start day.");
            }
            else if (Regex.IsMatch(lower, @"next week|agle hafte|agla hafta|aglay haftay"))
            {
                int toMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
                start = today.AddDays(toMonday == 0 ? 7 : toMonday);
            }
            else if (Regex.IsMatch(lower, @"next month|agle mahine|aglay mahine|agla mahina"))
                start = new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(9);

            if (start == null)
            {
                start = today.AddDays(30);
                spec.Assumptions.Add("No date was found, so a start date 30 days from today was assumed.");
            }
            spec.StartDate = start.Value;
            spec.EndDate = end ?? start.Value.AddDays(days - 1);
        }

        private static string AudienceAlternation() => "patients?|people|children|families|students|log";

        /// <summary>The next date with this month/day that is not in the past.</summary>
        private static DateTime? MakeDate(DateTime today, int month, int day)
        {
            int year = today.Year;
            for (int i = 0; i < 2; i++, year++)
            {
                if (day < 1 || day > DateTime.DaysInMonth(year, month)) return null;
                var d = new DateTime(year, month, day);
                if (d >= today) return d;
            }
            return null;
        }

        public static string FormatDates(DateTime start, DateTime end) =>
            start.Date == end.Date ? start.ToString("d MMM yyyy", CultureInfo.InvariantCulture)
            : start.Month == end.Month && start.Year == end.Year ? $"{start.Day}–{end.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}"
            : $"{start.ToString("d MMM", CultureInfo.InvariantCulture)} – {end.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}";
    }
}
