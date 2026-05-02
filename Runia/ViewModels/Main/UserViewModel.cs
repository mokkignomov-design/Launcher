using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Runia.Services.Common;
using Runia.Services.Core.Navigation;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;

namespace Runia.ViewModels.Main
{
    public partial class UserViewModel : ObservableObject
    {
        private readonly IDialogService _dialog;
        private readonly INavigationService _nav;
        private readonly ISkinService _skinService;

        public UserViewModel(IDialogService dialog, INavigationService nav, ISkinService skinService)
        {
            _dialog = dialog;
            _nav = nav;
            _skinService = skinService;
            Debug.WriteLine("[UserViewModel] Constructor called");
            _ = InitializeLastSkinAsync();
        }

        [ObservableProperty]
        private string username = "Гость";

        [ObservableProperty]
        private BitmapSource? skinSource;

        [ObservableProperty]
        private string skinPath = string.Empty;

        [ObservableProperty]
        private bool isLoadingSkin;

        private async Task InitializeLastSkinAsync()
        {
            try
            {
                var lastSkin = await _skinService.GetLastSelectedSkinAsync();
                if (lastSkin != null && File.Exists(lastSkin.LocalPath))
                {
                    Username = lastSkin.Username;
                    SkinPath = lastSkin.LocalPath;
                    UpdateSkin(lastSkin.LocalPath);
                    Debug.WriteLine($"[UserViewModel] Loaded last selected skin: {lastSkin.Username}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UserViewModel] Failed to load last skin: {ex}");
            }
        }

        [RelayCommand]
        private async Task LoadSkinAsync()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Minecraft Skin (*.png)|*.png";
            openFileDialog.Title = "Выберите файл скина";

            if (openFileDialog.ShowDialog() != true)
                return;

            IsLoadingSkin = true;
            try
            {
                string filePath = openFileDialog.FileName;

                if (!File.Exists(filePath))
                {
                    _dialog.ShowError("Файл скина не найден", "Ошибка");
                    return;
                }

                // Копируем скин в кэш с именем пользователя
                var localPath = await _skinService.GetSkinAsync(Username, $"file:///{filePath}");

                if (localPath != null && File.Exists(localPath))
                {
                    SkinPath = localPath;
                    await _skinService.SetLastSelectedSkinAsync(Username, localPath);
                    UpdateSkin(localPath);

                    // TODO: Загрузить скин на бэк
                    // await UploadSkinToBackendAsync(localPath);

                    Debug.WriteLine($"[UserViewModel] Successfully loaded skin from file: {filePath}");
                    _dialog.ShowError("Скин успешно загружен!", "Успех");
                }
                else
                {
                    _dialog.ShowError("Не удалось загрузить скин", "Ошибка");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UserViewModel] Error loading skin: {ex}");
                _dialog.ShowError($"Ошибка: {ex.Message}", "Ошибка загрузки скина");
            }
            finally
            {
                IsLoadingSkin = false;
            }
        }

        private void UpdateSkin(string path)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.EndInit();

                this.SkinSource = bitmap;
            }
            catch (Exception ex)
            {
                _dialog.ShowError($"Ошибка: {ex.Message}", "Ошибка загрузки скина");
            }
        }

        /// <summary>
        /// TODO: Загрузить скин на бэк. Используйте httpClient для отправки файла.
        /// Пример endpoint: POST /api/user/skin
        /// </summary>
        private async Task UploadSkinToBackendAsync(string localPath)
        {
            try
            {
                // Реализация для загрузки на бэк
                // using var content = new MultipartFormDataContent();
                // var fileContent = new ByteArrayContent(File.ReadAllBytes(localPath));
                // fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                // content.Add(fileContent, "file", Path.GetFileName(localPath));
                // var response = await httpClient.PostAsync("/api/user/skin", content);

                Debug.WriteLine("[UserViewModel] Skin uploaded to backend");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[UserViewModel] Error uploading skin to backend: {ex}");
            }
        }
    }
}
