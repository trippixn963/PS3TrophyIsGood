using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ModernWpf.Controls;
using ModernWpf.Controls.Primitives;

namespace PS3TrophiesIsPerfect.Dialogs
{
    /// <summary>
    /// Themed, animated replacements for MessageBox/input windows, built on ModernWpf's ContentDialog
    /// so every popup matches the dark Fluent shell. All are awaited on the UI thread.
    /// </summary>
    public static class Modern
    {
        /// <summary>Shows the pre-sync legitimacy report: a clean ✓ or a list of critical/warning issues.</summary>
        public static async Task CheckReport(
            bool clean,
            IReadOnlyList<string> criticals,
            IReadOnlyList<string> warnings
        )
        {
            var panel = new StackPanel { MaxWidth = 480 };
            if (clean)
            {
                panel.Children.Add(HeaderLine("", "#3FB950", "No legitimacy problems found."));
                panel.Children.Add(
                    new TextBlock
                    {
                        Text =
                            "Nothing here would obviously trip a flag. You're still responsible for what "
                            + "you sync — review the risks before doing so.",
                        TextWrapping = TextWrapping.Wrap,
                        Opacity = 0.7,
                        Margin = new Thickness(0, 12, 0, 0),
                    }
                );
            }
            else
            {
                string summary =
                    (criticals.Count > 0 ? criticals.Count + " critical" : "")
                    + (criticals.Count > 0 && warnings.Count > 0 ? ", " : "")
                    + (
                        warnings.Count > 0
                            ? warnings.Count + " warning" + (warnings.Count == 1 ? "" : "s")
                            : ""
                    );
                panel.Children.Add(
                    HeaderLine("", criticals.Count > 0 ? "#F85149" : "#E3B341", summary + " found")
                );
                foreach (var c in criticals)
                    panel.Children.Add(IssueLine("#F85149", c));
                foreach (var w in warnings)
                    panel.Children.Add(IssueLine("#E3B341", w));
            }

            await new ContentDialog
            {
                Title = "Legitimacy check",
                Content = panel,
                CloseButtonText = "Close",
            }.ShowAsync();
        }

        private static StackPanel HeaderLine(string glyph, string hex, string text)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(
                new FontIcon
                {
                    Glyph = glyph,
                    Foreground = HexBrush(hex),
                    FontSize = 18,
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                }
            );
            row.Children.Add(
                new TextBlock
                {
                    Text = text,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 15,
                    VerticalAlignment = VerticalAlignment.Center,
                }
            );
            return row;
        }

        private static StackPanel IssueLine(string hex, string text)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(2, 10, 0, 0),
            };
            row.Children.Add(
                new Ellipse
                {
                    Width = 7,
                    Height = 7,
                    Fill = HexBrush(hex),
                    Margin = new Thickness(0, 6, 10, 0),
                    VerticalAlignment = VerticalAlignment.Top,
                }
            );
            row.Children.Add(
                new TextBlock
                {
                    Text = text,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 440,
                }
            );
            return row;
        }

        private static Brush HexBrush(string hex) =>
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

        public static async Task Info(string message, string title = "PS3TrophiesIsPerfect")
        {
            await new ContentDialog
            {
                Title = title,
                Content = Body(message),
                CloseButtonText = "OK",
            }.ShowAsync();
        }

        public static async Task<bool> Confirm(
            string message,
            string title,
            string yes = "Yes",
            string no = "Cancel"
        )
        {
            var r = await new ContentDialog
            {
                Title = title,
                Content = Body(message),
                PrimaryButtonText = yes,
                CloseButtonText = no,
                DefaultButton = ContentDialogButton.Primary,
            }.ShowAsync();
            return r == ContentDialogResult.Primary;
        }

        public enum SaveChoice
        {
            Save,
            Discard,
            Cancel,
        }

        public static async Task<SaveChoice> SaveDiscardCancel(string message, string title)
        {
            var r = await new ContentDialog
            {
                Title = title,
                Content = Body(message),
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Don't save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
            }.ShowAsync();
            return r == ContentDialogResult.Primary ? SaveChoice.Save
                : r == ContentDialogResult.Secondary ? SaveChoice.Discard
                : SaveChoice.Cancel;
        }

        public static Task<string> PromptUrl(string initial = "") =>
            PromptText(
                "Copy from PSNProfiles",
                "PSNProfiles game-trophy URL",
                "e.g. https://psnprofiles.com/trophies/41027-pragmata/SomeUser",
                "https://psnprofiles.com/trophies/…",
                "Scrape",
                initial
            );

        /// <summary>Walks the user through getting their PSN NPSSO token and returns it (or null if cancelled).</summary>
        public static async Task<string> PromptNpsso()
        {
            const string ssoUrl = "https://ca.account.sony.com/api/v1/ssocookie";

            var panel = new StackPanel { MaxWidth = 460 };
            panel.Children.Add(
                new TextBlock
                {
                    Text =
                        "Your games and trophies come straight from PlayStation. To connect, paste a one-time "
                        + "sign-in token (it stays on this PC and refreshes itself for ~2 months):",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 12),
                }
            );

            panel.Children.Add(
                Step("1.", "Sign in to your account at playstation.com in any browser.")
            );

            var linkText = new TextBlock { TextWrapping = TextWrapping.Wrap };
            linkText.Inlines.Add("In the SAME browser, open ");
            var link = new System.Windows.Documents.Hyperlink(
                new System.Windows.Documents.Run(ssoUrl)
            )
            {
                NavigateUri = new Uri(ssoUrl),
            };
            link.RequestNavigate += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
                }
                catch { }
                e.Handled = true;
            };
            linkText.Inlines.Add(link);
            var step2 = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8),
            };
            step2.Children.Add(
                new TextBlock
                {
                    Text = "2.",
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 8, 0),
                }
            );
            step2.Children.Add(linkText);
            panel.Children.Add(step2);

            panel.Children.Add(
                Step("3.", "Copy the 64-character value after \"npsso\" and paste it below.")
            );

            var box = new TextBox { Margin = new Thickness(0, 8, 0, 0) };
            ControlHelper.SetPlaceholderText(box, "Paste your npsso token");
            box.Loaded += (s, e) => box.Focus();
            panel.Children.Add(box);

            var r = await new ContentDialog
            {
                Title = "Link your PlayStation account",
                Content = panel,
                PrimaryButtonText = "Link",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
            }.ShowAsync();
            return r == ContentDialogResult.Primary ? box.Text?.Trim() : null;
        }

        private static StackPanel Step(string number, string text)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 8),
            };
            row.Children.Add(
                new TextBlock
                {
                    Text = number,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 8, 0),
                }
            );
            row.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap });
            return row;
        }

        public static async Task<string> PromptText(
            string title,
            string label,
            string hint,
            string placeholder,
            string okText,
            string initial = ""
        )
        {
            var box = new TextBox { Text = initial ?? string.Empty };
            ControlHelper.SetPlaceholderText(box, placeholder);
            box.Loaded += (s, e) =>
            {
                box.Focus();
                box.SelectAll();
            };

            var panel = new StackPanel();
            panel.Children.Add(
                new TextBlock
                {
                    Text = label,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 6),
                }
            );
            if (!string.IsNullOrEmpty(hint))
                panel.Children.Add(
                    new TextBlock
                    {
                        Text = hint,
                        Opacity = 0.6,
                        FontSize = 12,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 12),
                    }
                );
            panel.Children.Add(box);

            var r = await new ContentDialog
            {
                Title = title,
                Content = panel,
                PrimaryButtonText = okText,
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
            }.ShowAsync();
            return r == ContentDialogResult.Primary ? box.Text : null;
        }

        public static async Task<DateTime?> PromptDate(
            string title,
            DateTime initial,
            bool showTime
        )
        {
            var picker = new DatePicker { SelectedDate = initial.Date, Width = 160 };
            var hour = TimeBox(initial.Hour, "HH");
            var min = TimeBox(initial.Minute, "mm");
            var sec = TimeBox(initial.Second, "ss");

            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(picker);
            if (showTime)
            {
                row.Children.Add(hour);
                row.Children.Add(Colon());
                row.Children.Add(min);
                row.Children.Add(Colon());
                row.Children.Add(sec);
            }

            var r = await new ContentDialog
            {
                Title = title,
                Content = row,
                PrimaryButtonText = "OK",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
            }.ShowAsync();
            if (r != ContentDialogResult.Primary)
                return null;

            DateTime date = picker.SelectedDate ?? DateTime.Today;
            if (!showTime)
                return date.Date;
            return new DateTime(
                date.Year,
                date.Month,
                date.Day,
                Clamp(hour.Text, 0, 23),
                Clamp(min.Text, 0, 59),
                Clamp(sec.Text, 0, 59)
            );
        }

        private static TextBlock Body(string message) =>
            new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 460,
            };

        private static TextBox TimeBox(int value, string placeholder)
        {
            var b = new TextBox
            {
                Text = value.ToString("00"),
                Width = 44,
                MaxLength = 2,
                Margin = new Thickness(8, 0, 0, 0),
            };
            ControlHelper.SetPlaceholderText(b, placeholder);
            return b;
        }

        private static TextBlock Colon() =>
            new TextBlock
            {
                Text = ":",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(3, 0, 0, 0),
            };

        private static int Clamp(string text, int lo, int hi)
        {
            int.TryParse(text, out int v);
            return v < lo ? lo : (v > hi ? hi : v);
        }
    }
}
