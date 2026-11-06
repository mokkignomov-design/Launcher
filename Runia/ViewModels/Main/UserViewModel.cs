using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Runia.Services.Common;
using Runia.Services.Core.Infrastructure.Configuration;
using Runia.Services.Core.Infrastructure.Network.Clients;
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
        private readonly UserClient _userClient;
        private readonly Runia.Services.Auth.AuthService _authService;
        private readonly Runia.Services.Core.Infrastructure.Network.ITokenStore _tokenStore;

        public UserViewModel(
            IDialogService dialog,
            INavigationService nav,
            ISkinService skinService,
            IAppConfig config,
            ILogger<UserViewModel> logger,
            UserClient userClient,
            Runia.Services.Auth.AuthService authService,
            Runia.Services.Core.Infrastructure.Network.ITokenStore tokenStore)
        {
            _dialog = dialog;
            _nav = nav;
            _skinService = skinService;
            _config = config;
            _logger = logger;
            _userClient = userClient;
            _authService = authService;
            _tokenStore = tokenStore;

            _logger.LogInformation("[UserViewModel] Initializing...");

            _ = InitializeAsync();
        }

        // --- Данные пользователя ---
        [ObservableProperty] private string username = string.Empty;
        [ObservableProperty] private string primaryEmail = string.Empty;
        [ObservableProperty] private string rank = string.Empty;
        [ObservableProperty] private string regDate = string.Empty;
        [ObservableProperty] private string balance = string.Empty;
        [ObservableProperty] private double playedHours;
        [ObservableProperty] private int kills;
        [ObservableProperty] private int deaths;
        [ObservableProperty] private string role = string.Empty;
        [ObservableProperty] private bool isLoadingProfile;
        [ObservableProperty] private string profileLoadError = string.Empty;
        [ObservableProperty] private BitmapSource? skinSource;
        [ObservableProperty] private string skinPath = string.Empty;
        [ObservableProperty] private bool isLoadingSkin;
        [ObservableProperty] private int selectedTabIndex = 0;

        private async Task InitializeAsync()
        {
            try
            {
                IsLoadingProfile = true;
                ProfileLoadError = string.Empty;

                var profileResult = await _userClient.GetCurrentUserProfileAsync();
                string? profileSkinUrl = null;
                if (profileResult.IsSuccess && profileResult.Data != null)
                {
                    Username = profileResult.Data.Username;
                    profileSkinUrl = profileResult.Data.SkinUrl;
                    PrimaryEmail = profileResult.Data.PrimaryEmail ?? "Не указана";
                    Rank = profileResult.Data.Rank;
                    var donation = profileResult.Data.Stats?.DonationBalance ?? profileResult.Data.Balance;
                    Balance = donation.ToString("F2");

                    PlayedHours = profileResult.Data.Stats?.PlayedHours ?? 0;
                    Kills = profileResult.Data.Stats?.Kills ?? 0;
                    Deaths = profileResult.Data.Stats?.Deaths ?? 0;
                    Role = profileResult.Data.Stats?.Role ?? string.Empty;
                    RegDate = profileResult.Data.RegistrationDate.ToString("dd.MM.yyyy");

                    // Previously there was an override of Server from settings; not required because server information
                    // is not returned by current backend profile stats.

                    _logger.LogInformation("[UserViewModel] Profile loaded: {Username}", Username);
                }
                else
                {
                    ProfileLoadError = profileResult.ErrorMessage ?? "Не удалось загрузить профиль.";
                    _logger.LogWarning("[UserViewModel] Failed to load profile: {Error}", profileResult.ErrorMessage);
                }

                _ = InitializeSkinDataAsync(profileSkinUrl);
            }
            catch (Exception ex)
            {
                ProfileLoadError = "Не удалось загрузить профиль.";
                _logger.LogError(ex, "[UserViewModel] Failed to initialize");
            }
            finally
            {
                IsLoadingProfile = false;
            }
        }

        private async Task InitializeSkinDataAsync(string? profileSkinUrl)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(profileSkinUrl))
                {
                    var localPath = await _skinService.GetSkinAsync(Username, profileSkinUrl);
                    if (localPath != null)
                    {
                        SkinPath = localPath;
                        UpdateSkin(localPath);
                        await _skinService.SetLastSelectedSkinAsync(Username, localPath);
                        return;
                    }
                }

                var info = await _skinService.GetSkinInfoAsync(Username);
                if (info != null && !string.IsNullOrEmpty(info.Url))
                {
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
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.EndInit();
                bitmap.Freeze();

                SkinSource = bitmap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create BitmapSource from {Path}", path);
            }
        }
    }
}