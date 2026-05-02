using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Runia.Domain.Models;
using Runia.Services.Common;
using Runia.Services.Core.Navigation;
using System;
using System.Threading.Tasks;

namespace Runia.ViewModels.Main
{
    /// <summary>
    /// ViewModel для загрузки скинов с API бэка
    /// </summary>
    public partial class SkinDownloadViewModel : ObservableObject
    {
        private readonly ISkinService _skinService;
        private readonly IDialogService _dialog;
        private readonly ILogger<SkinDownloadViewModel> _logger;

        public SkinDownloadViewModel(ISkinService skinService, IDialogService dialog, ILogger<SkinDownloadViewModel> logger)
        {
            _skinService = skinService ?? throw new ArgumentNullException(nameof(skinService));
            _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [ObservableProperty]
        private string username = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [RelayCommand]
        private async Task DownloadSkinAsync()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                _dialog.ShowError("Пожалуйста, введите ник игрока", "Ошибка");
                return;
            }

            IsLoading = true;
            StatusMessage = "Загрузка скина...";

            try
            {
                // Здесь вы должны подставить URL вашего API
                // Пример: https://api.yourserver.com/skins/{username}
                string skinUrl = $"https://minotar.net/skin/{Username}";

                var localPath = await _skinService.GetSkinAsync(Username, skinUrl);

                if (localPath != null)
                {
                    await _skinService.SetLastSelectedSkinAsync(Username, localPath);
                    StatusMessage = "Скин успешно загружен!";
                    _logger.LogInformation("Successfully downloaded skin for {Username}", Username);
                    _dialog.ShowError($"Скин игрока {Username} успешно загружен!", "Успех");
                }
                else
                {
                    StatusMessage = "Ошибка: не удалось загрузить скин";
                    _logger.LogWarning("Failed to download skin for {Username}", Username);
                    _dialog.ShowError("Не удалось загрузить скин игрока", "Ошибка");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Ошибка загрузки";
                _logger.LogError(ex, "Error downloading skin for {Username}", Username);
                _dialog.ShowError($"Ошибка: {ex.Message}", "Ошибка загрузки скина");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task CleanCacheAsync()
        {
            try
            {
                StatusMessage = "Очистка кэша...";
                await _skinService.CleanOldCacheAsync();
                StatusMessage = "Кэш очищен";
                _dialog.ShowError("Старые скины успешно удалены", "Успех");
                _logger.LogInformation("Cache cleaned successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning cache");
                _dialog.ShowError($"Ошибка: {ex.Message}", "Ошибка очистки кэша");
            }
        }
    }
}
