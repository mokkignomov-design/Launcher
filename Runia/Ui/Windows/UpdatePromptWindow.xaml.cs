using System.Windows;

namespace Runia.Ui.Windows
{
    public partial class UpdatePromptWindow : Window
    {
        public UpdatePromptWindow(string version)
        {
            InitializeComponent();
            VersionText.Text = $"Версия {version}";
        }

        private void UpdateClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
