using System.Windows.Media;

namespace PS3TrophiesIsPerfect.Models
{
    /// <summary>
    /// One row in the trophy grid. This is the view-facing shape; the real loader will map the
    /// frozen TROPHYParser tables onto these. <see cref="TypeBrush"/> is precomputed so the XAML
    /// needs no value converter.
    /// </summary>
    public sealed class TrophyRow
    {
        public string Name { get; set; }
        public string Detail { get; set; }

        /// <summary>"P" / "G" / "S" / "B".</summary>
        public string Type { get; set; }

        public bool Got { get; set; }
        public bool Synced { get; set; }
        public string Time { get; set; }

        /// <summary>PSNProfiles-style "elapsed (+gap)" string.</summary>
        public string Elapsed { get; set; }

        public string GotText => Got ? "Yes" : "No";
        public string SyncedText => Synced ? "Yes" : "No";

        /// <summary>
        /// Trophy-type badge (the PSNProfiles platinum/gold/silver/bronze PNGs). Used as the row image
        /// until a real game folder is loaded, at which point the per-trophy TROPxxx.PNG artwork replaces it.
        /// </summary>
        public string TypeIcon
        {
            get
            {
                string name;
                switch (Type)
                {
                    case "P": name = "plat"; break;
                    case "G": name = "gold"; break;
                    case "S": name = "silver"; break;
                    case "B": name = "bronze"; break;
                    default: return null;
                }
                return "pack://application:,,,/Assets/TrophyTypes/" + name + ".png";
            }
        }

        public Brush TypeBrush
        {
            get
            {
                switch (Type)
                {
                    case "P": return new SolidColorBrush(Color.FromRgb(0x6F, 0xC8, 0xF0));
                    case "G": return new SolidColorBrush(Color.FromRgb(0xF0, 0xC4, 0x40));
                    case "S": return new SolidColorBrush(Color.FromRgb(0xC4, 0xCC, 0xD4));
                    case "B": return new SolidColorBrush(Color.FromRgb(0xCD, 0x7F, 0x32));
                    default: return Brushes.Gray;
                }
            }
        }
    }
}
