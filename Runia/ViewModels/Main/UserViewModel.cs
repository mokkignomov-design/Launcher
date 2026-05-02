using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Runia.Services.Common;
using Runia.Services.Core.Navigation;
using System;
using System.Diagnostics;
using System.Windows.Media.Imaging;

namespace Runia.ViewModels.Main
{
    public partial class UserViewModel : ObservableObject
    {
        private readonly IDialogService _dialog;
        private readonly INavigationService _nav;

        public UserViewModel(IDialogService dialog, INavigationService nav)
        {
            _dialog = dialog;
            _nav = nav;
            Debug.WriteLine("[UserViewModel] Constructor called");
        }

        [ObservableProperty]
        private string username = "Гость";

        [ObservableProperty]
        private string skinUrl = "https://minotar.net/skin/Steve";

        [ObservableProperty]
        private BitmapSource? skinSource;

        [RelayCommand]
        private void LoadFromUrl()
        {
            UpdateSkin(this.SkinUrl);
        }

        [RelayCommand]
        private void LoadFromFile()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Minecraft Skin (*.png)|*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                this.SkinUrl = openFileDialog.FileName;
                UpdateSkin(openFileDialog.FileName);
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
    }
}
