using System.Windows.Media;

namespace PS3TrophiesIsPerfect.Models
{
    /// <summary>
    /// One row of the merged donor-vs-you comparison: a single trophy with the donor's time/gap and
    /// your applied time/gap side by side, plus a match verdict on the gap.
    /// </summary>
    public sealed class ComparisonRow
    {
        public int Order { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public ImageSource Icon { get; set; }

        public string DonorTimeText { get; set; }
        public string DonorGapText { get; set; }
        public string MyTimeText { get; set; }
        public string MyGapText { get; set; }

        /// <summary>"exact" / "slower" / "faster" / "first" / "missing".</summary>
        public string Match { get; set; }
        public string MatchText { get; set; }

        public ImageSource Display => Icon ?? TypeBadge;

        public ImageSource TypeBadge => Services.TrophyBadges.ForCode(Type);

        public Brush MatchBrush
        {
            get
            {
                switch (Match)
                {
                    case "exact":
                        return Green;
                    case "faster":
                        return Red;
                    default:
                        return Muted; // slower / first / missing
                }
            }
        }

        private static readonly Brush Green = Frozen(Color.FromRgb(0x3F, 0xB9, 0x50));
        private static readonly Brush Red = Frozen(Color.FromRgb(0xF8, 0x51, 0x49));
        private static readonly Brush Muted = Frozen(Color.FromRgb(0x8A, 0x8A, 0x8A));

        private static Brush Frozen(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }
    }
}
