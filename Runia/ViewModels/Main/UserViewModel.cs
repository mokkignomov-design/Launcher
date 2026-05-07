using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Runia.Services.Common;
using Runia.Services.Core.Infrastructure.Configuration;
using Runia.Services.Core.Navigation;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Runia.ViewModels.Main
{
    public partial class UserViewModel : ObservableObject
    {
        private readonly IDialogService _dialog;
        private readonly INavigationService _nav;
        private readonly ISkinService _skinService;
        private readonly IAppConfig _config;
        private readonly ILogger<UserViewModel> _logger;

        public UserViewModel(
            IDialogService dialog,
            INavigationService nav,
            ISkinService skinService,
            IAppConfig config,
            ILogger<UserViewModel> logger)
        {
            _dialog = dialog;
            _nav = nav;
            _skinService = skinService;
            _config = config;
            _logger = logger;

            _logger.LogInformation("[UserViewModel] Initializing...");

            // Запускаем инициализацию без блокировки конструктора
            _ = InitializeSkinDataAsync();
        }

        // --- Данные пользователя ---
        [ObservableProperty] private string username = "testuser123"; // В будущем тянуть из IAuthService
        [ObservableProperty] private string email = "g******2@bk.ru";
        [ObservableProperty] private string regDate = "01.02.2024";
        [ObservableProperty] private string balance = "25 791.00 AP";

        // --- Состояние скина ---
        [ObservableProperty] private BitmapSource? skinSource;
        [ObservableProperty] private string skinPath = string.Empty;
        [ObservableProperty] private bool isLoadingSkin;

        // --- Состояние вкладок (только для переключения ВНУТРИ страницы User) ---
        [ObservableProperty] private int selectedTabIndex = 0;

        /// <summary>
        /// Умная инициализация: API -> Локальный кэш -> Пусто
        /// </summary>
        private async Task InitializeSkinDataAsync()
        {
            try
            {
                // 1. Пробуем получить актуальную инфу с сервера[cite: 9]
                var info = await _skinService.GetSkinInfoAsync(Username);
                if (info != null && !string.IsNullOrEmpty(info.Url))
                {
                    // Собираем полный URL, используя базовый адрес из конфига
                    string fullUrl = info.Url.StartsWith("http")
                        ? info.Url
                        : $"{_config.ApiBaseUrl.TrimEnd('/')}/{info.Url.TrimStart('/')}";

                    var localPath = await _skinService.GetSkinAsync(Username, fullUrl);
                    if (localPath != null)
                    {
                        SkinPath = localPath;
                        UpdateSkin(localPath);
                        await _skinService.SetLastSelectedSkinAsync(Username, localPath);
                        return;
                    }
                }

                // 2. Если API недоступно, берем последний успешно загруженный скин
                var lastSkin = await _skinService.GetLastSelectedSkinAsync();
                if (lastSkin != null && File.Exists(lastSkin.LocalPath))
                {
                    SkinPath = lastSkin.LocalPath;
                    UpdateSkin(lastSkin.LocalPath);
                    _logger.LogInformation("Loaded skin from local history: {Path}", lastSkin.LocalPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize skin data");
            }
        }

        /// <summary>
        /// Команда загрузки нового скина. 
        /// В XAML привязка: Command="{Binding LoadSkinCommand}"
        /// </summary>
        [RelayCommand]
        private async Task LoadSkinAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Minecraft Skin (*.png)|*.png",
                Title = "Выберите файл скина (64x64)"
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            IsLoadingSkin = true;
            try
            {
                string filePath = openFileDialog.FileName;

                // Загружаем на сервер через сервис
                bool success = await _skinService.UploadSkinAsync(filePath, Username);

                if (success)
                {
                    SkinPath = filePath;
                    UpdateSkin(filePath);
                    await _skinService.SetLastSelectedSkinAsync(Username, filePath);
                    _dialog.ShowMessage("Скин успешно обновлен!", "Успех");
                }
                else
                {
                    _dialog.ShowError("Сервер отклонил файл. Проверьте формат PNG и размер.", "Ошибка");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Skin upload command failed");
                _dialog.ShowError("Не удалось загрузить скин: " + ex.Message, "Ошибка");
            }
            finally
            {
                IsLoadingSkin = false;
            }
        }

        private void UpdateSkin(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                // Важно для WPF, чтобы файл не блокировался и обновлялся в UI сразу
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.EndInit();
                bitmap.Freeze(); // Для безопасности потоков

                SkinSource = bitmap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create BitmapSource from {Path}", path);
            }
        }
    }
}