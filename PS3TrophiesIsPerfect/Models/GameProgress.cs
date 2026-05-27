using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Media;
using PS3TrophiesIsPerfect.Services;

namespace PS3TrophiesIsPerfect.Models
{
    /// <summary>A trophy type's badge + how many of that type the game has — for the per-game count strip.</summary>
    public sealed class TrophyTypeCount
    {
        public ImageSource Badge { get; }
        public string Count { get; }
        public TrophyTypeCount(string asset, int count) { Badge = Cached(asset); Count = count.ToString(); }

        // The four badges are shared, frozen, embedded assets — load each once.
        private static readonly Dictionary<string, ImageSource> _cache = new Dictionary<string, ImageSource>();
        private static ImageSource Cached(string asset)
        {
            if (!_cache.TryGetValue(asset, out var img))
                _cache[asset] = img = ImageLoad.FromPack("pack://application:,,,/Assets/TrophyTypes/" + asset + ".png");
            return img;
        }
    }

    /// <summary>One PS3 game from the linked PlayStation account: banner + trophy counts + completion.</summary>
    public sealed class GameProgress : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string GameId { get; set; }
        public string IconUrl { get; set; }

        public int Earned { get; set; }
        public int Total { get; set; }
        public int Percent { get; set; }

        // Defined count of each trophy type, and whether the game has DLC (extra trophy groups).
        public int Platinum { get; set; }
        public int Gold { get; set; }
        public int Silver { get; set; }
        public int Bronze { get; set; }
        public bool HasDlc { get; set; }

        /// <summary>When trophies for this game were last earned/updated — for the "Recent" sort.</summary>
        public DateTime LastUpdated { get; set; }

        [JsonIgnore] public string CountText => Earned + " / " + Total + " trophies";
        [JsonIgnore] public string PercentText => Percent + "%";

        /// <summary>Type breakdown for the count strip (only types the game actually has). "platinum"→plat.png.</summary>
        [JsonIgnore]
        public IReadOnlyList<TrophyTypeCount> TypeCounts
        {
            get
            {
                var list = new List<TrophyTypeCount>();
                if (Platinum > 0) list.Add(new TrophyTypeCount("plat", Platinum));
                if (Gold > 0) list.Add(new TrophyTypeCount("gold", Gold));
                if (Silver > 0) list.Add(new TrophyTypeCount("silver", Silver));
                if (Bronze > 0) list.Add(new TrophyTypeCount("bronze", Bronze));
                return list;
            }
        }

        /// <summary>The game banner — set after the list shows (downloaded from Sony's CDN + cached on disk).</summary>
        private ImageSource _icon;
        [JsonIgnore]
        public ImageSource Icon
        {
            get => _icon;
            set { _icon = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon))); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
