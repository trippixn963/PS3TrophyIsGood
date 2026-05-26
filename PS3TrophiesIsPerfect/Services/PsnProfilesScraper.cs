using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace PS3TrophiesIsPerfect.Services
{
    /// <summary>A scraped trophy: its display name and unlock time (Unix seconds).</summary>
    public sealed class ScrapedTrophy
    {
        public long Date { get; set; }
        public string Name { get; set; }

        public ScrapedTrophy(long date, string name)
        {
            Date = date;
            Name = name;
        }
    }

    /// <summary>
    /// Scrapes a PSNProfiles user game-trophy page through the local FlareSolverr proxy (PSNProfiles
    /// sits behind Cloudflare). Returns the earned trophies as name-keyed entries carrying the real
    /// unlock time. Ported from the WinForms CopyFrom dialog.
    /// </summary>
    public static class PsnProfilesScraper
    {
        public static bool LooksLikeTrophyUrl(string url) =>
            url != null && Regex.IsMatch(url, "psnprofiles\\.com/trophies/", RegexOptions.IgnoreCase);

        public static List<ScrapedTrophy> Load(string url)
        {
            string html = FetchViaFlareSolverr(url.Trim());

            var rowRegex = new Regex(
                "<tr[^>]*class=\"[^\"]*completed[^\"]*\"[^>]*>(.*?)</tr>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var nameRegex = new Regex(
                "<a[^>]*class=\"title\"[^>]*href=\"/trophy/[^\"]*\"[^>]*>(.*?)</a>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var dateRegex = new Regex(
                "typo-top-date[^>]*>\\s*<nobr>(.*?)</nobr>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var timeRegex = new Regex(
                "typo-bottom-date[^>]*>\\s*<nobr>(.*?)</nobr>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            var results = new List<ScrapedTrophy>();
            foreach (Match row in rowRegex.Matches(html))
            {
                string block = row.Groups[1].Value;
                Match nm = nameRegex.Match(block);
                Match dm = dateRegex.Match(block);
                Match tm = timeRegex.Match(block);
                if (!nm.Success || !dm.Success || !tm.Success)
                    continue;

                string name = StripHtml(nm.Groups[1].Value);
                string dateText = RemoveDayOrdinal(StripHtml(dm.Groups[1].Value));
                string timeText = StripHtml(tm.Groups[1].Value);

                if (string.IsNullOrWhiteSpace(name) || !TryParsePsnUnlock(dateText, timeText, out long unix))
                    continue;

                results.Add(new ScrapedTrophy(unix, name));
            }
            return results;
        }

        private static string FetchViaFlareSolverr(string targetUrl)
        {
            string jsonPayload =
                $@"{{ ""cmd"": ""request.get"", ""url"": ""{targetUrl}"", ""maxTimeout"": 60000 }}";

            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("Content-Type", "application/json");
                    client.Encoding = System.Text.Encoding.UTF8;
                    string response = client.UploadString("http://localhost:8191/v1", jsonPayload);
                    var json = System.Text.Json.JsonDocument.Parse(response);
                    return json.RootElement.GetProperty("solution").GetProperty("response").GetString();
                }
            }
            catch (WebException)
            {
                throw new Exception(
                    "FlareSolverr isn't reachable on localhost:8191 (needed to get past PSNProfiles' "
                        + "Cloudflare). Start FlareSolverr and try again.");
            }
        }

        private static string StripHtml(string fragment)
        {
            string text = Regex.Replace(fragment, "<[^>]+>", string.Empty);
            text = WebUtility.HtmlDecode(text);
            return Regex.Replace(text, "\\s+", " ").Trim();
        }

        private static string RemoveDayOrdinal(string date) =>
            Regex.Replace(date, "(\\d+)(st|nd|rd|th)", "$1", RegexOptions.IgnoreCase);

        private static bool TryParsePsnUnlock(string dateText, string timeText, out long unix)
        {
            unix = 0;
            string[] formats = { "d MMM yyyy h:mm:ss tt", "d MMM yyyy h:mm tt" };
            if (DateTime.TryParseExact(
                    (dateText + " " + timeText).Trim(),
                    formats,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime dt))
            {
                unix = (long)(dt - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                return true;
            }
            return false;
        }
    }
}
