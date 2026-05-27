using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace PS3TrophiesIsPerfect.Services
{
    /// <summary>
    /// Downloads an image from an open CDN (Sony's trophy/game art) and keeps it on disk forever —
    /// completed-game art never changes, so a cached file is permanently valid. Returns a frozen
    /// <see cref="ImageSource"/>. Unlike PSNProfiles, these URLs need no cookie, so this is reliable.
    /// </summary>
    public static class ImageCache
    {
        private static readonly string Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PS3TrophiesIsPerfect", "imgcache");

        public static ImageSource Get(string url, string cacheKey)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(cacheKey)) return null;
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
                return ImageLoad.FromFile(file);
            }
            catch { return null; }
        }

        private static string Sanitize(string key) => Regex.Replace(key, "[^A-Za-z0-9_.-]", "_");
    }
}
