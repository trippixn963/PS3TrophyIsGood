using System;
using System.Windows;

namespace PS3TrophiesIsPerfect.Dialogs
{
    public partial class DateInputWindow : Window
    {
        public DateInputWindow(string prompt, DateTime initial, bool showTime = true)
        {
            InitializeComponent();
            Prompt.Text = prompt;
            DatePick.SelectedDate = initial.Date;
            HourBox.Text = initial.Hour.ToString("00");
            MinBox.Text = initial.Minute.ToString("00");
            SecBox.Text = initial.Second.ToString("00");
            if (!showTime)
            {
                HourBox.Visibility = MinBox.Visibility = SecBox.Visibility = Visibility.Collapsed;
            }
        }

        public DateTime SelectedDateTime { get; private set; }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DateTime date = DatePick.SelectedDate ?? DateTime.Today;
            int.TryParse(HourBox.Text, out int h);
            int.TryParse(MinBox.Text, out int m);
            int.TryParse(SecBox.Text, out int s);
            h = Clamp(h, 0, 23); m = Clamp(m, 0, 59); s = Clamp(s, 0, 59);
            SelectedDateTime = new DateTime(date.Year, date.Month, date.Day, h, m, s);
            DialogResult = true;
        }

        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
