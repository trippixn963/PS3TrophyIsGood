using System;
using System.Windows.Media;
using PS3TrophiesIsPerfect.Services;

namespace PS3TrophiesIsPerfect.Models
{
    /// <summary>One row in the trophy list, projected from the frozen TROPHYParser tables.</summary>
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

        // ---- Card display helpers ----
        public double RowOpacity => Got ? 1.0 : 0.45;
        public string TimeDisplay =>
            Got ? (Time?.ToString("d MMM yyyy  h:mm tt") ?? "Unlocked") : "Locked";
        public string StatusText => Synced ? "Synced" : (Got ? "Unlocked" : "Locked");
        public string ElapsedDisplay => Got ? (Elapsed ?? "") : "";

        /// <summary>What the list shows: real artwork if loaded, otherwise the trophy-type badge.</summary>
        public ImageSource Display => Icon ?? TypeBadge;

        public ImageSource TypeBadge => TrophyBadges.ForCode(Type);
    }
}
