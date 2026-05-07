using System.Windows.Controls;
using Runia.ViewModels.Main;

namespace Runia.Ui.PagesMain
{
    public partial class UserPage : UserControl
    {
        public UserPage(UserViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel; // Подвязываем ВьюМодель к интерфейсу
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}