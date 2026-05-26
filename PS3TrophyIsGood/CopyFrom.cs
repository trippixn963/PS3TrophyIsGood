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
                MessageBox.Show(Properties.strings.MinCantBeGreaterThanMax);
            else if (
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
