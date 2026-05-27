using System;
using System.IO;
using System.Text.Json;

namespace PS3TrophiesIsPerfect.Services
{
    /// <summary>One donor (cloned-from) trophy: display name + unlock time (Unix seconds).</summary>
    public sealed class DonorEntry
    {
        public string Name { get; set; }
        public long Date { get; set; }
    }

    /// <summary>Small JSON-backed user settings stored in %AppData%\PS3TrophiesIsPerfect\settings.json.</summary>
    public sealed class AppSettings
    {
        public string LastFolder { get; set; } = "";
        public string LastProfile { get; set; } = "";
        public double WinWidth { get; set; }
        public double WinHeight { get; set; }
        public double WinLeft { get; set; } = double.NaN;
        public double WinTop { get; set; } = double.NaN;
        public bool WinMaximized { get; set; }

        /// <summary>The last cloned-from list, for the side-by-side comparison panel.</summary>
        public string DonorTitle { get; set; } = "";
        public System.Collections.Generic.List<DonorEntry> Donor { get; set; } = new System.Collections.Generic.List<DonorEntry>();

        private static string Dir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PS3TrophiesIsPerfect");
        private static string FilePath => Path.Combine(Dir, "settings.json");

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
            }
            catch { /* corrupt/missing → defaults */ }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { /* best effort */ }
        }
    }
}
