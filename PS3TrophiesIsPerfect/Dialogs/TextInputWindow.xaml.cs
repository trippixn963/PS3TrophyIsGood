using System.Windows;

namespace PS3TrophiesIsPerfect.Dialogs
{
    public partial class TextInputWindow : Window
    {
        public string Text => UrlBox.Text;

        public TextInputWindow(string initial = "")
        {
            InitializeComponent();
            UrlBox.Text = initial;
            Loaded += (s, e) => { UrlBox.Focus(); UrlBox.SelectAll(); };
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
