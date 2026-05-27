using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using PS3TrophiesIsPerfect.Models;

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

    /// <summary>The earned trophies plus the profile owner's avatar URL.</summary>
    public sealed class ScrapeResult
    {
        public List<ScrapedTrophy> Trophies { get; set; } = new List<ScrapedTrophy>();
        public string AvatarUrl { get; set; }
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

        /// <summary>Fetches just the avatar URL for a profile page (e.g. https://psnprofiles.com/User).</summary>
        public static string FetchAvatar(string profileUrl) => ExtractAvatar(FetchViaFlareSolverr(profileUrl.Trim()));

        /// <summary>
        /// Scrapes the linked account's profile (https://psnprofiles.com/{user}) and returns the PS3
        /// games with earned/total trophies and completion %, parsed from the #gamesTable.
        /// </summary>
        public static List<GameProgress> FetchPs3Games(string user)
        {
            string html = FetchViaFlareSolverr("https://psnprofiles.com/" + user.Trim());

            var tableM = Regex.Match(html, "id=\"gamesTable\".*?</table>", RegexOptions.Singleline);
            string table = tableM.Success ? tableM.Value : html;

            var games = new List<GameProgress>();
            foreach (Match row in Regex.Matches(table, "<tr[^>]*>.*?</tr>", RegexOptions.Singleline))
            {
                string r = row.Value;
                if (r.IndexOf("platform ps3", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var titleM = Regex.Match(r,
                    "<a[^>]*class=\"title\"[^>]*href=\"(/trophies/[^\"]*)\"[^>]*>(.*?)</a>",
                    RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (!titleM.Success)
                    continue;

                string name = StripHtml(titleM.Groups[2].Value);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var iconM = Regex.Match(r, "https://img\\.psnprofiles\\.com/game/[^\\s\"'<>]+?\\.(?:png|jpg)", RegexOptions.IgnoreCase);
                var countM = Regex.Match(r, "<b>(\\d+)</b>\\s*of\\s*<b>(\\d+)</b>", RegexOptions.IgnoreCase);
                int earned = countM.Success ? int.Parse(countM.Groups[1].Value) : 0;
                int total = countM.Success ? int.Parse(countM.Groups[2].Value) : 0;
                var pctM = Regex.Match(r, "class=\"progress-bar\">\\s*<span>(\\d+)%", RegexOptions.IgnoreCase);
                int pct = pctM.Success ? int.Parse(pctM.Groups[1].Value) : (total > 0 ? earned * 100 / total : 0);

                games.Add(new GameProgress
                {
                    Name = name,
                    Url = "https://psnprofiles.com" + titleM.Groups[1].Value,
                    IconUrl = iconM.Success ? iconM.Value : null,
                    Earned = earned,
                    Total = total,
                    Percent = pct,
                });
            }
            return games;
        }

        private static string ExtractAvatar(string html)
        {
            var m = Regex.Match(html,
                "https://i\\.psnprofiles\\.com/avatars/[^\\s\"'<>]+?\\.(?:png|jpg|jpeg|gif)",
                RegexOptions.IgnoreCase);
            return m.Success ? m.Value : null;
        }

        public static ScrapeResult Load(string url)
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
            return new ScrapeResult { Trophies = results, AvatarUrl = ExtractAvatar(html) };
        }

        private static string FetchViaFlareSolverr(string targetUrl)
        {
            // Give an auto-started FlareSolverr time to come up before the first request.
            Utility.servingReady.WaitOne(TimeSpan.FromSeconds(60));

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
