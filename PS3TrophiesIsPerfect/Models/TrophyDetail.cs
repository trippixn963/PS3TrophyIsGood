using System;
using System.ComponentModel;
using System.Windows.Media;
using PS3TrophiesIsPerfect.Services;

namespace PS3TrophiesIsPerfect.Models
{
    /// <summary>One trophy in a game's detail view: icon, name, description, type, rarity, the group it
    /// belongs to (base game / DLC), and this account's earned status + unlock time (from Sony's data).</summary>
    public sealed class TrophyDetail : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Detail { get; set; }
        public string IconUrl { get; set; }
        public string Type { get; set; } // bronze | silver | gold | platinum

        public string GroupId { get; set; } // "default" = base game, "001"+ = DLC
        public string GroupName { get; set; } // section header text

        public bool Earned { get; set; }
        public DateTime? EarnedUtc { get; set; }
        public double EarnedRate { get; set; } // % of players who own this trophy

        public string TypeText =>
            string.IsNullOrEmpty(Type) ? "" : char.ToUpper(Type[0]) + Type.Substring(1);

        public string EarnedText =>
            Earned
                ? (EarnedUtc?.ToLocalTime().ToString("d MMM yyyy  h:mm tt") ?? "Earned")
                : "Locked";

        public string EarnedAgo =>
            Earned && EarnedUtc.HasValue ? Ago(EarnedUtc.Value.ToLocalTime()) : "";

        public double EarnedOpacity => Earned ? 1.0 : 0.45;

        // ---- Rarity ----
        public string RarityText => EarnedRate <= 0 ? "" : EarnedRate.ToString("0.#") + "%";
        public string RarityLabel =>
            EarnedRate <= 0 ? ""
            : EarnedRate < 5 ? "Ultra Rare"
            : EarnedRate < 20 ? "Rare"
            : EarnedRate < 50 ? "Uncommon"
            : "Common";
        public Brush RarityBrush =>
            EarnedRate <= 0 ? Muted
            : EarnedRate < 5 ? Gold
            : EarnedRate < 20 ? Blue
            : EarnedRate < 50 ? Green
            : Muted;

        private static readonly Brush Gold = Freeze("#F0C440");
        private static readonly Brush Blue = Freeze("#2F81F7");
        private static readonly Brush Green = Freeze("#4FB477");
        private static readonly Brush Muted = Freeze("#8A8A8A");

        private static Brush Freeze(string hex)
        {
            var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            b.Freeze();
            return b;
        }

        private static string Ago(DateTime when)
        {
            var span = DateTime.Now - when;
            if (span.TotalSeconds < 0)
                return "just now";
            if (span.TotalHours < 1)
                return Math.Max(1, (int)span.TotalMinutes) + " min ago";
            if (span.TotalHours < 24)
                return (int)span.TotalHours + (span.TotalHours < 2 ? " hour ago" : " hours ago");
            int days = (int)span.TotalDays;
            if (days == 1)
                return "yesterday";
            if (days < 7)
                return days + " days ago";
            if (days < 30)
                return (days / 7) + (days < 14 ? " week ago" : " weeks ago");
            if (days < 365)
                return (days / 30) + (days < 60 ? " month ago" : " months ago");
            return (days / 365) + (days < 730 ? " year ago" : " years ago");
        }

        /// <summary>The trophy's type badge — a shared embedded asset, instant.</summary>
        public ImageSource TypeBadge => TrophyBadges.ForType(Type);

        /// <summary>The trophy graphic — downloaded from Sony's open CDN and cached, set after the list shows.</summary>
        private ImageSource _icon;
        public ImageSource Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
