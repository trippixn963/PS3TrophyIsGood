using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using PS3TrophiesIsPerfect.Models;

namespace PS3TrophiesIsPerfect.Services
{
    /// <summary>
    /// Per-game on-disk cache of trophy lists — one JSON file per npCommunicationId under %AppData%. A
    /// completed game's trophies never change, so opening it stays instant across app restarts (no PSN
    /// re-fetch); in-progress games are refreshed in the background after the cached copy is shown.
    /// </summary>
    public static class TrophyCache
    {
        private static readonly string Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PS3TrophiesIsPerfect",
            "trophycache"
        );

        public static List<TrophyDetail> Load(string npCommId)
        {
            if (string.IsNullOrEmpty(npCommId))
                return null;
            try
            {
                string file = Path.Combine(Dir, Sanitize(npCommId) + ".json");
                if (!File.Exists(file))
                    return null;
                return JsonSerializer.Deserialize<List<TrophyDetail>>(File.ReadAllText(file));
            }
            catch
            {
                return null;
            }
        }

        public static void Save(string npCommId, List<TrophyDetail> trophies)
        {
            if (string.IsNullOrEmpty(npCommId) || trophies == null)
                return;
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllText(
                    Path.Combine(Dir, Sanitize(npCommId) + ".json"),
                    JsonSerializer.Serialize(trophies)
                );
            }
            catch
            { /* best effort — a cache miss just re-fetches */
            }
        }

        private static string Sanitize(string key) => Regex.Replace(key, "[^A-Za-z0-9_.-]", "_");
    }
}
