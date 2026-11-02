using System.Windows;

namespace Runia.Ui.Windows
{
    public partial class UpdateStatusWindow : Window
    {
        public UpdateStatusWindow()
        {
            InitializeComponent();
            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = SystemParameters.WorkArea.Bottom - Height - 20;
        }

        public void SetStatus(string status)
        {
            StatusText.Text = status;
        }
    }
}
