using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PS3TrophyIsGood.UI
{
    /// <summary>
    /// Near-black ("OLED") dark theme with a PlayStation-blue accent. <see cref="Apply"/> recursively
    /// styles a form and its controls; <see cref="DarkToolStripRenderer"/> handles menus/strips.
    /// </summary>
    internal static class Theme
    {
        // Surfaces (darkest → lightest)
        public static readonly Color Surface = Color.FromArgb(0x0D, 0x0D, 0x0D);
        public static readonly Color Panel = Color.FromArgb(0x16, 0x16, 0x16);
        public static readonly Color Input = Color.FromArgb(0x1C, 0x1C, 0x1C);
        public static readonly Color Hover = Color.FromArgb(0x2A, 0x2A, 0x2A);
        public static readonly Color Border = Color.FromArgb(0x2E, 0x2E, 0x2E);

        // Text
        public static readonly Color Text = Color.FromArgb(0xF0, 0xF0, 0xF0);
        public static readonly Color TextMuted = Color.FromArgb(0x8A, 0x8A, 0x8A);

        // Accent (PlayStation blue)
        public static readonly Color Accent = Color.FromArgb(0x2F, 0x81, 0xF7);

        // Trophy row states (background + text), tuned for the near-black surface
        public static readonly Color RowUnlockedBack = Color.FromArgb(0x1E, 0x1E, 0x1E); // lifted off the surface
        public static readonly Color RowUnlockedText = Text;
        public static readonly Color RowSyncedBack = Color.FromArgb(0x3A, 0x20, 0x26); // muted rose
        public static readonly Color RowSyncedText = Text;
        public static readonly Color RowLockedBack = Surface;
        public static readonly Color RowLockedText = TextMuted; // dimmed = clearly "not yet earned"

        public static readonly Font UiFont = new Font("Segoe UI", 9F);

        public static void Apply(Form form)
        {
            form.BackColor = Surface;
            form.ForeColor = Text;
            form.Font = UiFont;
            UseDarkTitleBar(form);
            ApplyToControls(form.Controls);
        }

        private static void ApplyToControls(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                // NOTE: order matters — match derived types before their bases (StatusStrip/MenuStrip are
                // ToolStrips; LinkLabel is a Label) or the base case swallows them.
                switch (c)
                {
                    case MenuStrip ms:
                        ms.BackColor = Panel;
                        ms.ForeColor = Text;
                        ms.Renderer = new DarkToolStripRenderer();
                        break;
                    case StatusStrip ss:
                        ss.BackColor = Panel;
                        ss.ForeColor = TextMuted;
                        ss.Renderer = new DarkToolStripRenderer();
                        break;
                    case ToolStrip ts:
                        ts.BackColor = Panel;
                        ts.ForeColor = Text;
                        ts.Renderer = new DarkToolStripRenderer();
                        break;
                    case Button b:
                        b.FlatStyle = FlatStyle.Flat;
                        b.FlatAppearance.BorderColor = Border;
                        b.FlatAppearance.MouseOverBackColor = Hover;
                        b.BackColor = Input;
                        b.ForeColor = Text;
                        break;
                    case TextBox tb:
                        tb.BackColor = Input;
                        tb.ForeColor = Text;
                        tb.BorderStyle = BorderStyle.FixedSingle;
                        break;
                    case ComboBox cb:
                        cb.FlatStyle = FlatStyle.Flat;
                        cb.BackColor = Input;
                        cb.ForeColor = Text;
                        break;
                    case NumericUpDown nud:
                        nud.BackColor = Input;
                        nud.ForeColor = Text;
                        break;
                    case ListView lv:
                        lv.BackColor = Surface;
                        lv.ForeColor = Text;
                        lv.BorderStyle = BorderStyle.None;
                        break;
                    case ProgressBar pb:
                        pb.BackColor = Input;
                        pb.ForeColor = Accent;
                        break;
                    case LinkLabel ll:
                        ll.BackColor = Color.Transparent;
                        ll.LinkColor = Accent;
                        ll.ActiveLinkColor = Accent;
                        ll.VisitedLinkColor = Accent;
                        ll.ForeColor = TextMuted;
                        break;
                    case Label lbl:
                        lbl.BackColor = Color.Transparent;
                        lbl.ForeColor = Text;
                        break;
                    case Panel pnl:
                        pnl.BackColor = Surface;
                        pnl.ForeColor = Text;
                        break;
                    case DateTimePicker dtp:
                        dtp.CalendarForeColor = Text;
                        dtp.CalendarMonthBackground = Panel;
                        dtp.CalendarTitleBackColor = Panel;
                        break;
                }

                if (c.HasChildren)
                    ApplyToControls(c.Controls);
            }
        }

        // --- Windows 11 immersive dark title bar ---
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        private static void UseDarkTitleBar(Form form)
        {
            void Set()
            {
                int on = 1;
                // 20 = Win11 / late Win10; 19 = earlier Win10 builds. Wrong one just returns an error.
                DwmSetWindowAttribute(form.Handle, 20, ref on, sizeof(int));
                DwmSetWindowAttribute(form.Handle, 19, ref on, sizeof(int));
            }

            if (form.IsHandleCreated)
                Set();
            else
                form.HandleCreated += (s, e) => Set();
        }
    }
}
