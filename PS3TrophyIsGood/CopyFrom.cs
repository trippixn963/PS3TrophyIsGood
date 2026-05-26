using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PS3TrophyIsGood
{
    public partial class CopyFrom : Form
    {
        /// <summary>
        /// A scraped trophy: its display name and unlock time (the Id is just file order).
        /// </summary>
        public class Pair
        {
            public int Id { get; set; }
            public long Date { get; set; }

            /// <summary>Trophy display name — used to match the scraped trophy to the loaded game.</summary>
            public string Name { get; set; }

            public Pair(int id, long date)
            {
                Id = id;
                Date = date;
            }
        }

        // Trophies scraped from PSNProfiles (name + unlock time), consumed by the main form.
        private List<Pair> _localPairs;
        public IReadOnlyList<Pair> LocalPairs => _localPairs;

        public CopyFrom()
        {
            InitializeComponent();
            UI.Theme.Apply(this);
        }

        private void accept_Click(object sender, EventArgs e)
        {
            if (!Regex.IsMatch(textBox1.Text, "psnprofiles\\.com/trophies/", RegexOptions.IgnoreCase))
            {
                MessageBox.Show(
                    "Enter a PSNProfiles game-trophy URL, e.g.\n"
                        + "https://psnprofiles.com/trophies/1-super-stardust-hd/SomeUser"
                );
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                _localPairs = LoadFromPsnProfiles(textBox1.Text.Trim()).ToList();
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

            MessageBox.Show($"Scraped {_localPairs.Count} earned trophies from PSNProfiles.");
            DialogResult = DialogResult.OK;
        }

        /// <summary>
        /// Scrapes a PSNProfiles user game-trophy page (e.g.
        /// https://psnprofiles.com/trophies/1-super-stardust-hd/User) and returns the earned trophies as
        /// name-keyed <see cref="Pair"/>s carrying the real unlock time. PSNProfiles sits behind Cloudflare,
        /// so the page is fetched through the local FlareSolverr proxy. Only earned trophies (rows marked
        /// "completed", which carry a date/time) are returned.
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
        /// only unlock order and the gaps between unlocks matter for the night-session relocation.
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
    }
}
