namespace GiveAid.Services.Planner
{
    /// <summary>
    /// Small built-in gazetteer of Pakistani places: coordinates (for the weather check, so it
    /// works without a geocoding service) and province (to match NGOs and volunteers nearby).
    /// Unknown places are looked up online through the Open-Meteo geocoding service.
    /// </summary>
    public static class PlaceBook
    {
        public record Place(string Name, double Lat, double Lon, string Province);

        private static readonly Place[] Places =
        {
            new("Karachi", 24.86, 67.01, "Sindh"), new("Orangi Town", 24.95, 66.99, "Sindh"), new("Baldia", 24.92, 66.97, "Sindh"),
            new("Saddar", 24.85, 67.03, "Sindh"), new("Lyari", 24.87, 66.99, "Sindh"), new("Hyderabad", 25.40, 68.37, "Sindh"),
            new("Sukkur", 27.71, 68.85, "Sindh"), new("Larkana", 27.56, 68.21, "Sindh"), new("Dadu", 26.73, 67.78, "Sindh"),
            new("Mithi", 24.74, 69.80, "Sindh"), new("Tharparkar", 24.74, 69.80, "Sindh"), new("Thar", 24.74, 69.80, "Sindh"),
            new("Badin", 24.66, 68.84, "Sindh"), new("Thatta", 24.75, 67.92, "Sindh"), new("Jacobabad", 28.28, 68.44, "Sindh"),
            new("Nawabshah", 26.25, 68.41, "Sindh"), new("Mirpurkhas", 25.53, 69.01, "Sindh"), new("Umerkot", 25.36, 69.74, "Sindh"),
            new("Lahore", 31.55, 74.34, "Punjab"), new("Model Town", 31.48, 74.32, "Punjab"), new("Faisalabad", 31.42, 73.08, "Punjab"),
            new("Rawalpindi", 33.60, 73.04, "Punjab"), new("Multan", 30.20, 71.47, "Punjab"), new("Gujranwala", 32.16, 74.19, "Punjab"),
            new("Sialkot", 32.49, 74.53, "Punjab"), new("Bahawalpur", 29.40, 71.68, "Punjab"), new("Sargodha", 32.08, 72.67, "Punjab"),
            new("Dera Ghazi Khan", 30.05, 70.63, "Punjab"), new("Rajanpur", 29.10, 70.33, "Punjab"),
            new("Islamabad", 33.68, 73.05, "Islamabad"),
            new("Peshawar", 34.01, 71.58, "Khyber Pakhtunkhwa"), new("Swat", 35.22, 72.43, "Khyber Pakhtunkhwa"),
            new("Mardan", 34.20, 72.05, "Khyber Pakhtunkhwa"), new("Abbottabad", 34.15, 73.22, "Khyber Pakhtunkhwa"),
            new("Dera Ismail Khan", 31.83, 70.90, "Khyber Pakhtunkhwa"), new("Chitral", 35.85, 71.79, "Khyber Pakhtunkhwa"),
            new("Quetta", 30.18, 66.98, "Balochistan"), new("Gwadar", 25.13, 62.32, "Balochistan"), new("Turbat", 26.00, 63.04, "Balochistan"),
            new("Khuzdar", 27.80, 66.61, "Balochistan"), new("Gilgit", 35.92, 74.31, "Gilgit-Baltistan"), new("Skardu", 35.30, 75.63, "Gilgit-Baltistan"),
            new("Hunza", 36.32, 74.65, "Gilgit-Baltistan"), new("Muzaffarabad", 34.37, 73.47, "Azad Kashmir")
        };

        /// <summary>Finds the longest known place name mentioned in the text (e.g. "Orangi Town" before "Karachi").</summary>
        public static Place? FindIn(string text)
        {
            var lower = " " + text.ToLowerInvariant() + " ";
            return Places.OrderByDescending(p => p.Name.Length)
                .FirstOrDefault(p => System.Text.RegularExpressions.Regex.IsMatch(lower, $@"[^a-z]{System.Text.RegularExpressions.Regex.Escape(p.Name.ToLowerInvariant())}[^a-z]"));
        }

        public static Place? Get(string? name) =>
            string.IsNullOrWhiteSpace(name) ? null : FindIn(name);

        /// <summary>Province of a place or city name, or null when unknown.</summary>
        public static string? ProvinceOf(string? name) => Get(name)?.Province;
    }
}
