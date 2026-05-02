using System.Windows.Controls;
using Runia.ViewModels.Main;

namespace Runia.Ui.PagesMain
{
    public partial class SettingsPage : UserControl
    {
        public SettingsPage(SettingsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}