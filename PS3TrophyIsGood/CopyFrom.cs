using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace PS3TrophyIsGood
{
    public partial class CopyFrom : Form
    {
        /// <summary>
        /// Holds a (trophy id, timestamp) pair. Could be a generic Pair like in C++, but kept simple.
        /// </summary>
        public class Pair
        {
            public int Id { get; set; }
            public long Date { get; set; }

            /// <summary>
            /// Trophy display name; populated only for name-keyed local JSON imports.
            /// Null for the web-scrape and Id-keyed paths.
            /// </summary>
            public string Name { get; set; }

            public Pair(int id, long date)
            {
                Id = id;
                Date = date;
            }
        }

        /// <summary>
        /// Plain DTO used only for deserializing the local JSON file. Kept separate from
        /// <see cref="Pair"/> (which has no parameterless constructor) so the on-disk format
        /// can evolve independently of the class the rest of the app consumes.
        /// </summary>
        private class PairDto
        {
            public int Id { get; set; }
            public long Date { get; set; }

            // Present in name-keyed files. "trophyName" is accepted as an alias for "name".
            public string Name { get; set; }
            public string TrophyName
            {
                get { return Name; }
                set { Name = value; }
            }
        }

        // Holds timestamps imported from a local JSON file (null when the web-scrape path is used).
        private List<Pair> _localPairs;
        public IReadOnlyList<Pair> LocalPairs => _localPairs;

        // True when the loaded JSON identifies trophies by name rather than by Id/position.
        public bool LocalPairsAreNameKeyed { get; private set; }

        public CopyFrom()
        {
            InitializeComponent();
            groupBox1.Visible = false;
        }

        private void loadJsonButton_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                dlg.Title = "Select a trophy timestamp JSON file";
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    _localPairs = LoadFromLocalJson(dlg.FileName).ToList();
                    LocalPairsAreNameKeyed = _localPairs.Any(p => !string.IsNullOrWhiteSpace(p.Name));
                    MessageBox.Show(
                        $"Loaded {_localPairs.Count} timestamps "
                            + (LocalPairsAreNameKeyed ? "(matched by name)." : "(matched by Id).")
                    );
                    DialogResult = DialogResult.OK; // closes the dialog like the existing Accept button
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to load JSON: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Gets the timestamps from a profile (assuming it's a legit one), then modifies them
        /// so they still look legitimate without being a complete paste of the source profile.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<long> smartCopy()
        {
            var trophies = copyFrom(textBox1.Text).ToList();
            trophies.Sort((a, b) => a.Date.CompareTo(b.Date));
            var rand = new Random();
            var time =
                TimeSpan.FromDays(
                    (long)(yearsNumeric.Value * 365 + monthNumeric.Value * 30 + daysNumeric.Value)
                ) + TimeSpan.FromSeconds(rand.Next((int)minMinutes.Value, (int)maxMinutes.Value));
            var delta = Convert.ToInt64(time.TotalSeconds);

            // "Sleeping/blackout" window expressed in local hours: the zone [blackoutStart, blackoutEnd).
            // Any unlock that would land inside this zone gets pushed forward by a random block of hours,
            // so timestamps end up in a believable afternoon/evening slot instead of the small hours.
            // Adjust these four constants to customize the window and how far we jump past it.
            const int blackoutStart = 3; // 3:00 AM
            const int blackoutEnd = 13; // 1:00 PM
            const int skipMinSeconds = 10 * 3600; // jump at least 10h
            const int skipMaxSeconds = 14 * 3600; // jump at most 14h

            // Bumps delta until baseDate + delta no longer falls inside the blackout window.
            long skipBlackout(long baseDate, long currentDelta)
            {
                int hour = DateTimeOffset
                    .FromUnixTimeSeconds(baseDate + currentDelta)
                    .ToLocalTime()
                    .Hour;
                while (hour >= blackoutStart && hour < blackoutEnd)
                {
                    currentDelta += rand.Next(skipMinSeconds, skipMaxSeconds);
                    hour = DateTimeOffset
                        .FromUnixTimeSeconds(baseDate + currentDelta)
                        .ToLocalTime()
                        .Hour;
                }
                return currentDelta;
            }

            for (int i = 0; i < trophies.Count - 1; ++i)
            {
                if (trophies[i].Date == 0)
                    continue;
                delta = skipBlackout(trophies[i].Date, delta);
                trophies[i].Date += delta;
                if (trophies[i + 1].Date - trophies[i].Date > 60)
                    delta += rand.Next((int)minMinutes.Value, (int)maxMinutes.Value);
            }
            delta = skipBlackout(trophies[trophies.Count - 1].Date, delta);
            trophies[trophies.Count - 1].Date += delta;
            trophies.Sort((a, b) => a.Id.CompareTo(b.Id));
            return trophies.Select(d => d.Date);
        }

        public IEnumerable<long> copyFrom() => copyFrom(textBox1.Text).Select(p => p.Date);

        /// <summary>
        /// Alternative to web scraping: import custom IDs and exact Unix timestamps from a local
        /// JSON file. This avoids depending on PSNTrophyLeaders' HTML layout (and FlareSolverr),
        /// which the regex-based <see cref="copyFrom(string)"/> path is fragile against.
        /// Accepts two interchangeable formats (auto-detected by the caller):
        /// Id-keyed (matched by position) —
        /// [
        ///   { "id": 0, "date": 1776978641 },
        ///   { "id": 1, "date": 1776980732 }
        /// ]
        /// Name-keyed (matched by trophy display name; "trophyName" also accepted) —
        /// [
        ///   { "name": "The Beginning", "date": 1776978641 },
        ///   { "name": "First Blood",   "date": 1776980732 }
        /// ]
        /// </summary>
        /// <param name="filePath">Absolute or relative path to the JSON file on disk.</param>
        /// <returns>The parsed entries mapped to <see cref="Pair"/>, in file order.</returns>
        public IEnumerable<Pair> LoadFromLocalJson(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                throw new FileNotFoundException("Timestamp JSON file not found.", filePath);

            string json = File.ReadAllText(filePath);

            // Case-insensitive so both "id"/"Id" and "date"/"Date" keys are accepted.
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var entries =
                JsonSerializer.Deserialize<List<PairDto>>(json, options) ?? new List<PairDto>();

            return entries.Select(e => new Pair(e.Id, e.Date) { Name = e.Name }).ToList();
        }

        /// <summary>
        /// Scrapes a PSNProfiles user game-trophy page (e.g.
        /// https://psnprofiles.com/trophies/1-super-stardust-hd/User) and returns the earned trophies as
        /// name-keyed <see cref="Pair"/>s carrying the real unlock time. PSNProfiles sits behind Cloudflare,
        /// so the page is fetched through the local FlareSolverr proxy (same one the psntrophyleaders scrape
        /// uses). Only earned trophies (rows marked "completed", which carry a date/time) are returned.
        /// </summary>
        public IEnumerable<Pair> LoadFromPsnProfiles(string url)
        {
            string html = FetchViaFlareSolverr(url);

            // An earned trophy is a <tr class="...completed..."> row; within it:
            //   name -> <a class="title" href="/trophy/...">NAME</a>
            //   date -> <span class="typo-top-date"><nobr>16<sup>th</sup> Jul 2008</nobr></span>
            //   time -> <span class="typo-bottom-date"><nobr>1:50:59 AM</nobr></span>
            var rowRegex = new Regex(
                "<tr[^>]*class=\"[^\"]*completed[^\"]*\"[^>]*>(.*?)</tr>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase
            );
            var nameRegex = new Regex(
                "<a[^>]*class=\"title\"[^>]*href=\"/trophy/[^\"]*\"[^>]*>(.*?)</a>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase
            );
            var dateRegex = new Regex(
                "typo-top-date[^>]*>\\s*<nobr>(.*?)</nobr>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase
            );
            var timeRegex = new Regex(
                "typo-bottom-date[^>]*>\\s*<nobr>(.*?)</nobr>",
                RegexOptions.Singleline | RegexOptions.IgnoreCase
            );

            var results = new List<Pair>();
            int id = 0;
            foreach (Match row in rowRegex.Matches(html))
            {
                string block = row.Groups[1].Value;
                Match nm = nameRegex.Match(block);
                Match dm = dateRegex.Match(block);
                Match tm = timeRegex.Match(block);
                if (!nm.Success || !dm.Success || !tm.Success)
                    continue;

                string name = StripHtml(nm.Groups[1].Value);
                string dateText = RemoveDayOrdinal(StripHtml(dm.Groups[1].Value)); // "16 Jul 2008"
                string timeText = StripHtml(tm.Groups[1].Value); // "1:50:59 AM"

                if (string.IsNullOrWhiteSpace(name) || !TryParsePsnUnlock(dateText, timeText, out long unix))
                    continue;

                results.Add(new Pair(id++, unix) { Name = name });
            }
            return results;
        }

        /// <summary>Fetches a Cloudflare-protected page's HTML through the local FlareSolverr proxy.</summary>
        private string FetchViaFlareSolverr(string targetUrl)
        {
            // ManualResetEvent: stays set once FlareSolverr reports ready, so this is safe to call repeatedly.
            if (!Utility.servingReady.WaitOne(TimeSpan.FromSeconds(60)))
                throw new Exception(
                    "FlareSolverr isn't running (required to get past PSNProfiles' Cloudflare). "
                        + "Make sure the 'flaresolverr' folder sits next to the program."
                );

            string jsonPayload =
                $@"{{ ""cmd"": ""request.get"", ""url"": ""{targetUrl}"", ""maxTimeout"": 60000 }}";

            using (WebClient client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/json");
                client.Encoding = System.Text.Encoding.UTF8;
                string response = client.UploadString("http://localhost:8191/v1", jsonPayload);
                var json = System.Text.Json.JsonDocument.Parse(response);
                return json.RootElement.GetProperty("solution").GetProperty("response").GetString();
            }
        }

        /// <summary>Strips HTML tags, decodes entities, and collapses whitespace in a fragment.</summary>
        private static string StripHtml(string fragment)
        {
            string text = Regex.Replace(fragment, "<[^>]+>", string.Empty);
            text = WebUtility.HtmlDecode(text);
            return Regex.Replace(text, "\\s+", " ").Trim();
        }

        /// <summary>Turns "16th Jul 2008" into "16 Jul 2008" so DateTime can parse it.</summary>
        private static string RemoveDayOrdinal(string date)
        {
            return Regex.Replace(date, "(\\d+)(st|nd|rd|th)", "$1", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Parses PSNProfiles' "16 Jul 2008" + "1:50:59 AM" into a Unix timestamp (seconds), matching the
        /// 1970-UTC convention the rest of the app uses. The displayed timezone is intentionally ignored —
        /// only unlock order and the gaps between unlocks matter for the anchor-to-start-date remap.
        /// </summary>
        private static bool TryParsePsnUnlock(string dateText, string timeText, out long unix)
        {
            unix = 0;
            string[] formats = { "d MMM yyyy h:mm:ss tt", "d MMM yyyy h:mm tt" };
            if (
                DateTime.TryParseExact(
                    (dateText + " " + timeText).Trim(),
                    formats,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime dt
                )
            )
            {
                unix = (long)(dt - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Just parse and get the timestamps from a profile from https://psntrophyleaders.com
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private IEnumerable<Pair> copyFrom(string targetUrl)
        {
            // Wait until FlareSolverr prints "Serving on" before issuing any request.
            Console.WriteLine("Waiting for FlareSolverr to start...");
            Utility.servingReady.WaitOne();

            Console.WriteLine("FlareSolverr is ready; continuing.");

            int i = 0;
            Regex regex = new Regex(
                "<td class=\"date_earned\">\\s+<span class=\"sort\">\\d+</span>"
            );

            // Build the FlareSolverr request payload.
            string jsonPayload =
                $@"
            {{
                ""cmd"": ""request.get"",
                ""url"": ""{targetUrl}"",
                ""maxTimeout"": 60000
            }}";

            using (WebClient client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/json");
                string response = client.UploadString("http://localhost:8191/v1", jsonPayload);

                // Parse the HTML that FlareSolverr returned.
                var json = System.Text.Json.JsonDocument.Parse(response);
                string html = json
                    .RootElement.GetProperty("solution")
                    .GetProperty("response")
                    .GetString();

                var matches = regex.Matches(html);
                foreach (Match match in matches)
                {
                    yield return new Pair(
                        i++,
                        long.Parse(Regex.Match(match.Value, "\\d+").ToString())
                    );
                }
            }

            MessageBox.Show(Properties.strings.CopiedSuccessfully);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
                groupBox1.Visible = true;
            else
            {
                groupBox1.Visible = false;
                daysNumeric.Value = 0;
                monthNumeric.Value = 0;
                yearsNumeric.Value = 0;
                minMinutes.Value = 0;
                maxMinutes.Value = 0;
            }
        }

        private void accept_Click(object sender, EventArgs e)
        {
            if (minMinutes.Value > maxMinutes.Value)
            {
                MessageBox.Show(Properties.strings.MinCantBeGreaterThanMax);
                return;
            }

            // PSNProfiles URL: scrape the page (via FlareSolverr) into name-keyed pairs and route it
            // through the same matching path as the JSON import.
            if (Regex.IsMatch(textBox1.Text, "psnprofiles\\.com/trophies/", RegexOptions.IgnoreCase))
            {
                try
                {
                    Cursor = Cursors.WaitCursor;
                    _localPairs = LoadFromPsnProfiles(textBox1.Text.Trim()).ToList();
                    LocalPairsAreNameKeyed = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("PSNProfiles import failed: " + ex.Message);
                    return;
                }
                finally
                {
                    Cursor = Cursors.Default;
                }

                if (_localPairs.Count == 0)
                {
                    MessageBox.Show("No earned trophies were found on that PSNProfiles page.");
                    return;
                }
                MessageBox.Show(
                    $"Scraped {_localPairs.Count} earned trophies from PSNProfiles (matched by name)."
                );
                DialogResult = DialogResult.OK;
                return;
            }

            if (
                Regex.IsMatch(
                    textBox1.Text,
                    "https://psntrophyleaders.com/user/view/" + "\\S+/\\S+"
                )
            )
                DialogResult = DialogResult.OK;
            else
                MessageBox.Show(Properties.strings.CantFindGame);
        }
    }
}
