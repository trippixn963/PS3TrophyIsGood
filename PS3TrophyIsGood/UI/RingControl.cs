using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PS3TrophyIsGood.UI
{
    /// <summary>A small circular completion ring (accent arc + centered percentage) for the hero header.</summary>
    internal sealed class RingControl : Panel
    {
        private int _percent;

        public RingControl()
        {
            DoubleBuffered = true;
            BackColor = Theme.Surface;
        }

        public int Percent
        {
            get => _percent;
            set
            {
                _percent = Math.Max(0, Math.Min(100, value));
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            const float thickness = 5f;
            var rect = new RectangleF(
                thickness / 2f + 1,
                thickness / 2f + 1,
                Width - thickness - 2,
                Height - thickness - 2
            );

            using (var track = new Pen(Theme.Border, thickness))
                g.DrawEllipse(track, rect);

            if (_percent > 0)
                using (var arc = new Pen(Theme.Accent, thickness) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawArc(arc, rect, -90f, _percent * 3.6f);

            using (var font = new Font("Segoe UI", 9F, FontStyle.Bold))
                TextRenderer.DrawText(
                    g,
                    _percent + "%",
                    font,
                    new Rectangle(0, 0, Width, Height),
                    Theme.Text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                );
        }
    }
}
