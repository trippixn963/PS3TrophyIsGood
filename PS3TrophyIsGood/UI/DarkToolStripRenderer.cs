using System.Drawing;
using System.Windows.Forms;

namespace PS3TrophyIsGood.UI
{
    /// <summary>Dark renderer for the menu bar, drop-downs and status strip.</summary>
    internal sealed class DarkToolStripRenderer : ToolStripProfessionalRenderer
    {
        public DarkToolStripRenderer()
            : base(new DarkColorTable()) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Theme.Text : Theme.TextMuted;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = Theme.Text;
            base.OnRenderArrow(e);
        }

        private sealed class DarkColorTable : ProfessionalColorTable
        {
            public DarkColorTable()
            {
                UseSystemColors = false;
            }

            public override Color MenuStripGradientBegin => Theme.Panel;
            public override Color MenuStripGradientEnd => Theme.Panel;
            public override Color ToolStripGradientBegin => Theme.Panel;
            public override Color ToolStripGradientMiddle => Theme.Panel;
            public override Color ToolStripGradientEnd => Theme.Panel;
            public override Color ToolStripDropDownBackground => Theme.Panel;
            public override Color ToolStripBorder => Theme.Border;

            public override Color ImageMarginGradientBegin => Theme.Panel;
            public override Color ImageMarginGradientMiddle => Theme.Panel;
            public override Color ImageMarginGradientEnd => Theme.Panel;

            public override Color MenuItemSelected => Theme.Hover;
            public override Color MenuItemSelectedGradientBegin => Theme.Hover;
            public override Color MenuItemSelectedGradientEnd => Theme.Hover;
            public override Color MenuItemPressedGradientBegin => Theme.Panel;
            public override Color MenuItemPressedGradientEnd => Theme.Panel;
            public override Color MenuItemBorder => Theme.Border;
            public override Color MenuBorder => Theme.Border;

            public override Color SeparatorDark => Theme.Border;
            public override Color SeparatorLight => Theme.Border;
        }
    }
}
