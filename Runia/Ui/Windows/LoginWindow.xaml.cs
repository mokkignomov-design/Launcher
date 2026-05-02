using Runia.ViewModels.Windows;
using System.Windows;


namespace Runia.Ui.Windows
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += async (s, e) => {
                if (DataContext is LoginWindowViewModel vm) await vm.Initialize(this.LoginContentHost);
            };
        }
    }
}
