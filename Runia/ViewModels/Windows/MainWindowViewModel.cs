using CommunityToolkit.Mvvm.ComponentModel;
using Runia.Services.Auth;
using Runia.Services.Common;
using Runia.Services.Core.Navigation;
using Runia.Services.Core.Launcher; // Добавлена ссылка на сервис запуска
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Runia.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IDialogService _dialog;
        private readonly INavigationService _nav;
        private readonly AuthService _auth;
        private readonly LauncherService _launcher; // Сервис для запуска игры

        [ObservableProperty] private string _userName = "Гость";
        [ObservableProperty] private string _userRank = "Игрок";
        [ObservableProperty] private string _balance = "0";

        // --- СВОЙСТВА ДЛЯ УПРАВЛЕНИЯ ОВЕРЛЕЕМ ЗАГРУЗКИ ---
        [ObservableProperty] private bool isLaunching;
        [ObservableProperty] private string currentStatus = "Подготовка...";
        [ObservableProperty] private string currentFileName = "";

        // --- СВОЙСТВА ДЛЯ УПРАВЛЕНИЯ ОТМЕНОЙ И ДЕБАУНСОМ ---
        private CancellationTokenSource? _launchCancellationTokenSource;
        private int _lastReportedPercent = -1;

        public MainWindowViewModel(IDialogService dialog, INavigationService nav, AuthService auth, LauncherService launcher)
        {
            _dialog = dialog;
            _nav = nav;
            _auth = auth;
            _launcher = launcher; // Инициализация через DI

            UpdateUserInfo();
        }

        // --- ГЛОБАЛЬНЫЙ МЕТОД ЗАПУСКА ИГРЫ ---
        // Вызывается из ServerViewModel или ServerDetailsViewModel
        public async Task StartGameGlobalAsync(string serverName)
        {
            if (IsLaunching) return;

            IsLaunching = true;
            CurrentStatus = $"ЗАПУСК {serverName.ToUpper()}";
            CurrentFileName = "Инициализация...";
            _lastReportedPercent = -1;

            // Создаём новый CancellationTokenSource для этого запуска
            _launchCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _launchCancellationTokenSource.Token;

            try
            {
                // Запуск в фоновом потоке, чтобы UI (и анимация крутилки) не фризились
                await Task.Run(async () =>
                {
                    var progressHandler = new Progress<string>(msg =>
                    {
                        // Возвращаемся в UI поток для обновления свойств
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            // Логика разделения текста: 
                            // Если в логе есть проценты или инфа о скачке — кидаем в мелкий шрифт (CurrentFileName)
                            if (msg.Contains("%") || msg.Contains("Downloading"))
                            {
                                // Парсируем процент из сообщения (например: "Downloading: 45.5%")
                                int? percent = ExtractPercentage(msg);
                                if (percent.HasValue)
                                {
                                    // Ограничиваем процент до 100% и проверяем, изменился ли он
                                    int clampedPercent = Math.Min(percent.Value, 100);

                                    // Дебаунс: обновляем только если процент изменился более чем на 5%
                                    if (Math.Abs(clampedPercent - _lastReportedPercent) >= 5 || clampedPercent == 100)
                                    {
                                        _lastReportedPercent = clampedPercent;
                                        CurrentFileName = $"Прогресс: {clampedPercent}%";
                                    }
                                }
                                else
                                {
                                    // Если это не процент, но содержит "Downloading", показываем сообщение как есть
                                    CurrentFileName = msg;
                                }
                            }
                            else
                            {
                                // Остальное (статусы установки) — в заголовок оверлея
                                CurrentStatus = msg.ToUpper();
                            }
                        });
                    });

                    // Вызов основного метода запуска из LauncherService с CancellationToken
                    await _launcher.LaunchPackAsync(serverName, progressHandler, cancellationToken);
                }, cancellationToken);

                CurrentStatus = "ИГРА ЗАПУЩЕНА";
                CurrentFileName = "Приятной игры!";
                await Task.Delay(2000, cancellationToken); // Небольшая задержка, чтобы юзер успел порадоваться
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[MainWindowViewModel] Launch cancelled by user");
                CurrentStatus = "ОТМЕНЕНО";
                CurrentFileName = "Загрузка отменена пользователем";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainWindowViewModel ERROR] {ex}");
                _dialog.ShowError(ex.Message);
            }
            finally
            {
                // Скрываем оверлей в любом случае и очищаем токен
                IsLaunching = false;
                _launchCancellationTokenSource?.Dispose();
                _launchCancellationTokenSource = null;
                _lastReportedPercent = -1;
            }
        }

        /// <summary>
        /// Извлекает процент из строки сообщения.
        /// Примеры: "Downloading: 45.5%" -> 45, "Progress: 123%" -> 100 (после ограничения)
        /// </summary>
        private static int? ExtractPercentage(string message)
        {
            try
            {
                // Регулярное выражение для поиска числа с опциональной точкой и процентом
                var match = Regex.Match(message, @"(\d+(?:\.\d+)?)\s*%");
                if (match.Success && double.TryParse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture, out double percent))
                {
                    return (int)Math.Round(percent);
                }
            }
            catch
            {
                // Игнорируем ошибки при парсировании
            }
            return null;
        }

        [RelayCommand]
        private void CancelLaunch()
        {
            Debug.WriteLine("[MainWindowViewModel] CancelLaunch command invoked");
            if (_launchCancellationTokenSource != null && !_launchCancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    _launchCancellationTokenSource.Cancel();
                    CurrentStatus = "ОТМЕНА...";
                    CurrentFileName = "Ожидание завершения процесса";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MainWindowViewModel] CancelLaunch failed: {ex}");
                }
            }
        }

        [RelayCommand]
        private static void Close()
        {
            Debug.WriteLine("[MainWindowViewModel] Close command invoked");
            try
            {
                var win = System.Windows.Application.Current?.Windows.OfType<Runia.Ui.Windows.MainWindow>().FirstOrDefault();
                win?.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainWindowViewModel] Close failed: {ex}");
            }
        }

        [RelayCommand]
        private static void Minimize()
        {
            Debug.WriteLine("[MainWindowViewModel] Minimize command invoked");
            try
            {
                var win = System.Windows.Application.Current?.Windows.OfType<Runia.Ui.Windows.MainWindow>().FirstOrDefault();
                if (win != null) win.WindowState = System.Windows.WindowState.Minimized;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MainWindowViewModel] Minimize failed: {ex}");
            }
        }

        private void UpdateUserInfo()
        {
            if (_auth.CurrentUser != null)
            {
                UserName = _auth.CurrentUser.Name;
                UserRank = _auth.CurrentUser.Rank;
                Balance = _auth.CurrentUser.Balance.ToString();
            }
        }
    }
}