using System;
using System.Windows.Media;
using PS3TrophiesIsPerfect.Services;

namespace PS3TrophiesIsPerfect.Models
{
    /// <summary>One row in the trophy grid, projected from the frozen TROPHYParser tables.</summary>
    public sealed class TrophyRow
    {
        /// <summary>Trophy id == index in the parser tables (Platinum = 0).</summary>
        public int Id { get; set; }

        /// <summary>Unlock-order position (1-based); used by the donor-comparison panel's "#" column.</summary>
        public int Order { get; set; }

        public string Name { get; set; }
        public string Detail { get; set; }

        /// <summary>"P" / "G" / "S" / "B".</summary>
        public string Type { get; set; }

        public bool Got { get; set; }
        public bool Synced { get; set; }

        public DateTime? Time { get; set; }

        /// <summary>PSNProfiles-style "elapsed (+gap)" string.</summary>
        public string Elapsed { get; set; }

        /// <summary>Real per-trophy artwork (TROPxxx.PNG). Null until a game is loaded.</summary>
        public ImageSource Icon { get; set; }

        public string TimeText => Time.HasValue ? Time.Value.ToString("yyyy/MM/dd  HH:mm:ss") : string.Empty;
        public string GotText => Got ? "Yes" : "No";
        public string SyncedText => Synced ? "Yes" : "No";

        /// <summary>What the grid shows: real artwork if loaded, otherwise the trophy-type badge.</summary>
        public ImageSource Display => Icon ?? TypeBadge;

        public ImageSource TypeBadge
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
                return ImageLoad.FromPack("pack://application:,,,/Assets/TrophyTypes/" + name + ".png");
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
