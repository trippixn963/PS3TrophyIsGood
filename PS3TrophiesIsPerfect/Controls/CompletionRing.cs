using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace PS3TrophiesIsPerfect.Controls
{
    /// <summary>A circular completion indicator: a track ring plus an accent arc proportional to Percent.</summary>
    public sealed class CompletionRing : FrameworkElement
    {
        private static readonly Brush TrackBrush = Frozen(Color.FromRgb(0x26, 0x26, 0x26));
        private static readonly Brush AccentBrush = Frozen(Color.FromRgb(0x2F, 0x81, 0xF7));
        private static readonly Brush TextBrush = Frozen(Color.FromRgb(0xF0, 0xF0, 0xF0));

        public static readonly DependencyProperty PercentProperty = DependencyProperty.Register(
            nameof(Percent),
            typeof(int),
            typeof(CompletionRing),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public int Percent
        {
            get => (int)GetValue(PercentProperty);
            set => SetValue(PercentProperty, value);
        }

        protected override void OnRender(DrawingContext dc)
        {
            const double thickness = 5;
            double w = ActualWidth,
                h = ActualHeight;
            if (w <= 0 || h <= 0)
                return;

            var center = new Point(w / 2, h / 2);
            double radius = (Math.Min(w, h) - thickness) / 2;

            var track = new Pen(TrackBrush, thickness);
            dc.DrawEllipse(null, track, center, radius, radius);

            int pct = Math.Max(0, Math.Min(100, Percent));
            if (pct > 0)
            {
                var arc = new Pen(AccentBrush, thickness)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                };
                if (pct >= 100)
                {
                    dc.DrawEllipse(null, arc, center, radius, radius);
                }
                else
                {
                    double sweep = pct / 100.0 * 360.0;
                    Point start = PointOnCircle(center, radius, 0); // 12 o'clock
                    Point end = PointOnCircle(center, radius, sweep);
                    var geo = new StreamGeometry();
                    using (var ctx = geo.Open())
                    {
                        ctx.BeginFigure(start, false, false);
                        ctx.ArcTo(
                            end,
                            new Size(radius, radius),
                            0,
                            sweep > 180,
                            SweepDirection.Clockwise,
                            true,
                            false
                        );
                    }
                    geo.Freeze();
                    dc.DrawGeometry(null, arc, geo);
                }
            }

            var ft = new FormattedText(
                pct + "%",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(
                    new FontFamily("Segoe UI"),
                    FontStyles.Normal,
                    FontWeights.SemiBold,
                    FontStretches.Normal
                ),
                15,
                TextBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip
            );
            dc.DrawText(ft, new Point(center.X - ft.Width / 2, center.Y - ft.Height / 2));
        }

        private static Point PointOnCircle(Point center, double radius, double angleDegFromTop)
        {
            double rad = (angleDegFromTop - 90) * Math.PI / 180.0; // 0° = top, clockwise
            return new Point(center.X + radius * Math.Cos(rad), center.Y + radius * Math.Sin(rad));
        }

        private static Brush Frozen(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }
    }
}
