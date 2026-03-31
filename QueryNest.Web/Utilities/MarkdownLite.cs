using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace QueryNest.Web.Utilities;

public static class MarkdownLite
{
    private static readonly Regex HighlightRegex = new(@"==(.+?)==", RegexOptions.Compiled);
    private static readonly Regex BoldRegex = new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
    private static readonly Regex ItalicRegex = new(@"_(.+?)_", RegexOptions.Compiled);
    private static readonly Regex InlineCodeRegex = new(@"`(.+?)`", RegexOptions.Compiled);

    public static string ToHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var normalized = input.Replace("\r\n", "\n");
        var parts = normalized.Split("```", StringSplitOptions.None);

        var sb = new StringBuilder();
        for (var i = 0; i < parts.Length; i++)
        {
            var segment = parts[i];
            var isCode = i % 2 == 1;

            if (isCode)
            {
                var code = WebUtility.HtmlEncode(segment);
                sb.Append("<pre class=\"md-code\"><code>");
                sb.Append(code);
                sb.Append("</code></pre>");
                continue;
            }

            var html = WebUtility.HtmlEncode(segment);
            html = InlineCodeRegex.Replace(html, "<code class=\"md-inline\">$1</code>");
            html = BoldRegex.Replace(html, "<strong>$1</strong>");
            html = ItalicRegex.Replace(html, "<em>$1</em>");
            html = HighlightRegex.Replace(html, "<mark>$1</mark>");

            var lines = html.Split('\n');
            for (var li = 0; li < lines.Length; li++)
            {
                var line = lines[li];
                if (line.StartsWith("&gt; "))
                {
                    sb.Append("<blockquote class=\"md-quote\">");
                    sb.Append(line[5..]);
                    sb.Append("</blockquote>");
                }
                else
                {
                    sb.Append(line);
                    if (li < lines.Length - 1)
                    {
                        sb.Append("<br />");
                    }
                }
            }
        }

        return sb.ToString();
    }
}

