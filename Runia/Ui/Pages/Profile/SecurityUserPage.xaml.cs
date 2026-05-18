using System.Windows.Controls;
using Runia.ViewModels.Main;

namespace Runia.Ui.Pages.Profile
{
    public partial class SecurityUserPage : UserControl
    {
        public SecurityUserPage()
        {
            InitializeComponent();
            if (App.Services?.GetService(typeof(SecurityUserViewModel)) is SecurityUserViewModel vm)
            {
                DataContext = vm;
            }
        }
    }
}
