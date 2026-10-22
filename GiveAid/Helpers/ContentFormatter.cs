using System.Net;
using System.Text;
using Microsoft.AspNetCore.Html;

namespace GiveAid.Helpers
{
    /// <summary>
    /// Converts the plain text typed by the admin into safe HTML.
    /// Rules:  blank line = new paragraph,  "## Heading" = sub-heading,  "- item" = bullet list.
    /// All text is HTML-encoded, so no script can be injected through the page editor.
    /// </summary>
    public static class ContentFormatter
    {
        public static IHtmlContent ToHtml(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return HtmlString.Empty;

            var html = new StringBuilder();
            var lines = text.Replace("\r\n", "\n").Split('\n');
            var paragraph = new List<string>();
            bool inList = false;

            void FlushParagraph()
            {
                if (paragraph.Count > 0)
                {
                    html.Append("<p>").Append(string.Join("<br>", paragraph)).Append("</p>");
                    paragraph.Clear();
                }
            }
            void CloseList()
            {
                if (inList) { html.Append("</ul>"); inList = false; }
            }

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0) { FlushParagraph(); CloseList(); continue; }

                if (line.StartsWith("## "))
                {
                    FlushParagraph(); CloseList();
                    html.Append("<h3>").Append(WebUtility.HtmlEncode(line[3..])).Append("</h3>");
                }
                else if (line.StartsWith("- "))
                {
                    FlushParagraph();
                    if (!inList) { html.Append("<ul>"); inList = true; }
                    html.Append("<li>").Append(WebUtility.HtmlEncode(line[2..])).Append("</li>");
                }
                else
                {
                    CloseList();
                    paragraph.Add(WebUtility.HtmlEncode(line));
                }
            }
            FlushParagraph(); CloseList();
            return new HtmlString(html.ToString());
        }

        /// <summary>Plain-text preview (first N characters) used on cards.</summary>
        public static string Excerpt(string? text, int length)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var clean = text.Replace("## ", "").Replace("\n- ", " ").Replace("\r", " ").Replace("\n", " ");
            return clean.Length <= length ? clean : clean[..length].TrimEnd() + "...";
        }

        /// <summary>Formats a rupee amount, e.g. Rs 12,500.</summary>
        public static string Rupees(decimal amount) => "Rs " + amount.ToString("#,##0");
    }
}
