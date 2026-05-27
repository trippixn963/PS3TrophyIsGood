using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace PS3TrophiesIsPerfect.Services
{
    /// <summary>
    /// Two-level cache for open-CDN art (Sony's trophy/game images): an in-memory map of already-decoded,
    /// frozen <see cref="ImageSource"/>s on top of a permanent on-disk file cache. So the first request
    /// downloads + decodes, later requests this session are instant from memory, and across sessions they
    /// load from disk without re-downloading. Completed-game art never changes, so caches stay valid.
    /// </summary>
    public static class ImageCache
    {
        private static readonly string Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PS3TrophiesIsPerfect",
            "imgcache"
        );

        // Decoded, frozen images keyed by cacheKey — frozen ImageSources are safe to share across threads.
        private static readonly ConcurrentDictionary<string, ImageSource> Memory =
            new ConcurrentDictionary<string, ImageSource>();

        public static ImageSource Get(string url, string cacheKey)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(cacheKey))
                return null;
            if (Memory.TryGetValue(cacheKey, out var cached))
                return cached;
            try
            {
                Directory.CreateDirectory(Dir);
                // The extension is irrelevant — BitmapImage sniffs the format from the bytes.
                string file = Path.Combine(Dir, Sanitize(cacheKey) + ".img");
                if (!File.Exists(file))
                {
                    using (var wc = new WebClient())
                        wc.DownloadFile(url, file);
                }
                var img = ImageLoad.FromFile(file);
                if (img != null)
                    Memory[cacheKey] = img;
                return img;
            }
            catch
            {
                return null;
            }
        }

        private static string Sanitize(string key) => Regex.Replace(key, "[^A-Za-z0-9_.-]", "_");
    }
}
