using System;
using System.Drawing;
using System.Windows.Forms;

namespace PS3TrophyIsGood.UI
{
    /// <summary>
    /// A dark, theme-matching replacement for <see cref="MessageBox"/>. Same call shape
    /// (message, title, buttons) and returns a <see cref="DialogResult"/>.
    /// </summary>
    internal static class Dialog
    {
        public static DialogResult Show(
            string message,
            string title = "PS3 Trophy Editor",
            MessageBoxButtons buttons = MessageBoxButtons.OK)
        {
            const int pad = 18;
            using (var f = new Form())
            {
                f.Text = title;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MinimizeBox = false;
                f.MaximizeBox = false;
                f.ShowInTaskbar = false;
                f.BackColor = Theme.Surface;
                f.ForeColor = Theme.Text;
                f.Font = Theme.UiFont;

                Size measured = TextRenderer.MeasureText(
                    message ?? string.Empty,
                    Theme.UiFont,
                    new Size(460, 0),
                    TextFormatFlags.WordBreak
                );
                int msgW = Math.Min(460, Math.Max(240, measured.Width));

                var label = new Label
                {
                    Text = message,
                    Location = new Point(pad, pad),
                    Size = new Size(msgW, measured.Height + 4),
                    ForeColor = Theme.Text,
                };
                f.Controls.Add(label);

                string[] texts;
                DialogResult[] results;
                switch (buttons)
                {
                    case MessageBoxButtons.OKCancel:
                        texts = new[] { "OK", "Cancel" };
                        results = new[] { DialogResult.OK, DialogResult.Cancel };
                        break;
                    case MessageBoxButtons.YesNo:
                        texts = new[] { "Yes", "No" };
                        results = new[] { DialogResult.Yes, DialogResult.No };
                        break;
                    case MessageBoxButtons.YesNoCancel:
                        texts = new[] { "Yes", "No", "Cancel" };
                        results = new[] { DialogResult.Yes, DialogResult.No, DialogResult.Cancel };
                        break;
                    default:
                        texts = new[] { "OK" };
                        results = new[] { DialogResult.OK };
                        break;
                }

                const int btnW = 86,
                    btnH = 30,
                    gap = 10;
                int groupW = texts.Length * btnW + (texts.Length - 1) * gap;
                int clientW = Math.Max(label.Right + pad, groupW + pad * 2);
                label.Width = clientW - pad * 2;
                int btnTop = label.Bottom + pad;
                int groupLeft = clientW - pad - groupW;

                for (int i = 0; i < texts.Length; i++)
                {
                    bool isDefault = i == 0; // first button = affirmative / default
                    var b = new Button
                    {
                        Text = texts[i],
                        DialogResult = results[i],
                        Size = new Size(btnW, btnH),
                        Location = new Point(groupLeft + i * (btnW + gap), btnTop),
                        FlatStyle = FlatStyle.Flat,
                        ForeColor = isDefault ? Color.White : Theme.Text,
                        BackColor = isDefault ? Theme.Accent : Theme.Input,
                    };
                    b.FlatAppearance.BorderColor = isDefault ? Theme.Accent : Theme.Border;
                    b.FlatAppearance.MouseOverBackColor = isDefault
                        ? ControlPaint.Light(Theme.Accent)
                        : Theme.Hover;
                    f.Controls.Add(b);

                    if (isDefault)
                        f.AcceptButton = b;
                    if (results[i] == DialogResult.Cancel)
                        f.CancelButton = b;
                    else if (results[i] == DialogResult.No && buttons == MessageBoxButtons.YesNo)
                        f.CancelButton = b;
                }

                f.ClientSize = new Size(clientW, btnTop + btnH + pad);
                Theme.UseDarkTitleBar(f);

                Form owner = Form.ActiveForm;
                if (owner != null && owner != f)
                {
                    f.StartPosition = FormStartPosition.CenterParent;
                    return f.ShowDialog(owner);
                }
                f.StartPosition = FormStartPosition.CenterScreen;
                return f.ShowDialog();
            }
        }
    }
}
