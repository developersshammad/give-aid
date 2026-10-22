using System.Text.RegularExpressions;

namespace GiveAid.Helpers
{
    /// <summary>
    /// Dummy payment validation (no real money is charged).
    /// Checks: card brand, length, Luhn check-digit, expiry date, CVV and holder name.
    /// The same rules are repeated in JavaScript (wwwroot/js/donate.js) for instant feedback.
    /// </summary>
    public static class CardValidator
    {
        /// <summary>Removes spaces and dashes from a typed card number.</summary>
        public static string Clean(string? number) =>
            Regex.Replace(number ?? "", @"[\s-]", "");

        /// <summary>Detects the card brand from its starting digits (IIN range).</summary>
        public static string DetectBrand(string digits)
        {
            if (Regex.IsMatch(digits, @"^4")) return "Visa";
            if (Regex.IsMatch(digits, @"^(5[1-5]|2(2[2-9]|[3-6]\d|7[01]|720))")) return "MasterCard";
            if (Regex.IsMatch(digits, @"^3[47]")) return "American Express";
            if (Regex.IsMatch(digits, @"^(6011|65|64[4-9])")) return "Discover";
            if (Regex.IsMatch(digits, @"^62")) return "UnionPay";
            return "";
        }

        /// <summary>Allowed number lengths for each brand.</summary>
        private static int[] LengthsFor(string brand) => brand switch
        {
            "American Express" => new[] { 15 },
            "Visa" => new[] { 13, 16, 19 },
            "UnionPay" => new[] { 16, 17, 18, 19 },
            _ => new[] { 16 }
        };

        /// <summary>
        /// Luhn (mod 10) algorithm: starting from the right, double every second digit,
        /// subtract 9 if the result is above 9, add everything - the total must end in 0.
        /// </summary>
        public static bool PassesLuhn(string digits)
        {
            int sum = 0;
            bool doubleIt = false;
            for (int i = digits.Length - 1; i >= 0; i--)
            {
                int d = digits[i] - '0';
                if (doubleIt)
                {
                    d *= 2;
                    if (d > 9) d -= 9;
                }
                sum += d;
                doubleIt = !doubleIt;
            }
            return sum % 10 == 0;
        }

        /// <summary>Validates the complete card. Returns a list of (field, message) errors.</summary>
        public static List<(string Field, string Error)> Validate(
            string? number, string? holder, string? expiry, string? cvv)
        {
            var errors = new List<(string, string)>();
            string digits = Clean(number);
            string brand = "";

            // 1) card number
            if (!Regex.IsMatch(digits, @"^\d{12,19}$"))
                errors.Add(("CardNumber", "Enter a valid card number (digits only)."));
            else
            {
                brand = DetectBrand(digits);
                if (brand == "")
                    errors.Add(("CardNumber", "Card type not supported. Use Visa, MasterCard, American Express, Discover or UnionPay."));
                else if (!LengthsFor(brand).Contains(digits.Length))
                    errors.Add(("CardNumber", $"A {brand} card number cannot have {digits.Length} digits."));
                else if (!PassesLuhn(digits))
                    errors.Add(("CardNumber", "This card number is not valid (check-digit failed)."));
            }

            // 2) card holder name
            if (string.IsNullOrWhiteSpace(holder) || !Regex.IsMatch(holder.Trim(), @"^[A-Za-z][A-Za-z .'-]{1,98}$"))
                errors.Add(("CardHolder", "Enter the name exactly as printed on the card (letters only)."));

            // 3) expiry MM/YY - must be this month or later, and not more than 20 years ahead
            var m = Regex.Match(expiry ?? "", @"^\s*(0[1-9]|1[0-2])\s*/\s*(\d{2})\s*$");
            if (!m.Success)
                errors.Add(("Expiry", "Use the format MM/YY."));
            else
            {
                int month = int.Parse(m.Groups[1].Value);
                int year = 2000 + int.Parse(m.Groups[2].Value);
                var lastDayOfExpiryMonth = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);
                if (lastDayOfExpiryMonth < DateTime.Today)
                    errors.Add(("Expiry", "This card has expired."));
                else if (year > DateTime.Today.Year + 20)
                    errors.Add(("Expiry", "Expiry year is not valid."));
            }

            // 4) CVV - 4 digits for American Express, 3 for others
            int cvvLength = brand == "American Express" ? 4 : 3;
            if (!Regex.IsMatch(cvv ?? "", $@"^\d{{{cvvLength}}}$"))
                errors.Add(("Cvv", $"CVV must be {cvvLength} digits."));

            return errors;
        }
    }
}
