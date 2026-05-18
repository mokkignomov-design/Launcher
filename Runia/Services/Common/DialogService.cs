using Microsoft.Extensions.DependencyInjection;
using Runia.Ui.Windows;
using Runia.ViewModels.Windows;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace Runia.Services.Common
{
    /// </summary>
    /// Сервис для отображения диалогов и управления окнами приложения.
    /// </summary>
    public class DialogService : IDialogService
    {
        public void ShowMessage(string message, string title = "Сообщение")
        {
            Debug.WriteLine($"[DialogService.ShowMessage] Title: {title}, Message: {message}");

            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = Application.Current.MainWindow;
                var msgWin = new MessageWindow(message, title)
                {
                    Owner = (mainWindow != null && mainWindow.IsLoaded) ? mainWindow : null
                };
                msgWin.ShowDialog();
            });
        }

        public void ShowError(string message, string title = "Ошибка")
        {
            Debug.WriteLine($"[DialogService.ShowError] Title: {title}, Message: {message}");

            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = Application.Current.MainWindow;
                var msgWin = new MessageWindow(message, title)
                {
                    Owner = (mainWindow != null && mainWindow.IsLoaded) ? mainWindow : null
                };
                msgWin.ShowDialog();
            });
        }
        public void CloseMainWindow()
        {
            var existing = Application.Current.Windows.OfType<Ui.Windows.MainWindow>().FirstOrDefault();
            existing?.Close();
        }

        public bool ShowConfirmation(string message, string title = "Подтверждение")
        {
            Debug.WriteLine($"[DialogService.ShowConfirmation] Title: {title}, Message: {message}");
            var res = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return res == MessageBoxResult.Yes;
        }

        public void ShowMainWindow()
        {
            // Закрываем старое главное окно, если оно есть
            var existing = Application.Current.Windows.OfType<Ui.Windows.MainWindow>().FirstOrDefault();
            if (existing != null && existing.IsLoaded)
            {
                Debug.WriteLine("[DialogService] Closing existing MainWindow before opening new one");
                existing.Close();
            }

            Debug.WriteLine("[DialogService] Opening new MainWindow via DI");

            // Создаем новое окно (Transient)
            var mainWindow = App.Services?.GetService<Ui.Windows.MainWindow>() ?? new Ui.Windows.MainWindow();

            var vm = App.Services?.GetService<MainWindowViewModel>();
            if (vm != null) mainWindow.DataContext = vm;

            mainWindow.Show();

            CloseLoginWindow();
        }



        public void CloseLoginWindow()
        {
            Debug.WriteLine("[DialogService.CloseLoginWindow] Closing LoginWindow");

            foreach (Window window in Application.Current.Windows)
            {
                if ((window is Ui.Windows.LoginWindow))
                {
                    window.Close();
                    break;
                }
            }
        }

        public void ShowLoginWindow()
        {
            var loginWindow = App.Services?.GetService<Ui.Windows.LoginWindow>() ?? new Ui.Windows.LoginWindow();
            var vm = App.Services?.GetService<LoginWindowViewModel>();
            if (vm != null) loginWindow.DataContext = vm;

            loginWindow.Show();
            try
            {
                var nav = App.Services?.GetService<Runia.Services.Core.Navigation.INavigationService>();
                if (nav != null)
                {
                    if (loginWindow.FindName("LoginContentHost") is ContentControl host)
                    {
                        nav.SetHost(host);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DialogService] Failed to set nav host for LoginWindow: {ex}");
            }

            CloseMainWindow();
        }
    }
}
