using System.Windows;

namespace Runia.Ui.Windows
{
    public partial class MessageWindow : Window
    {
        public MessageWindow(string message, string title)
        {
            InitializeComponent();
            MessageTxt.Text = message;
            TitleTxt.Text = title.ToUpper();
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}