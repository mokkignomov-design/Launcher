using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Runia.Services.Auth;
using Runia.Services.Common;
using Runia.Services.Core.Infrastructure.Configuration;
using Runia.Services.Core.Infrastructure.Network;
using Runia.Services.Core.Infrastructure.Persistence;
using Runia.Services.Core.Launcher;
using Runia.Services.Core.Navigation;
using Runia.Services.Installers;
using Runia.Ui.PagesMain;
using Runia.ViewModels.Main;
using Runia.ViewModels.Windows;
using System;
using System.Linq;
using System.Net.Http;
using System.Windows;

namespace Runia
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;
        private IHost? _host;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // --- 1. БАЗОВАЯ ИНФРАСТРУКТУРА ---
                    services.AddSingleton<IAppConfig, DefaultAppConfig>();
                    services.AddSingleton<IDialogService, DialogService>();
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<Runia.Services.Core.Infrastructure.ILocalDataService, Runia.Services.Core.Infrastructure.LocalDataService>();
                    services.AddTransient<JwtHandler>();
                    services.AddHttpClient<BaseApiService>((sp, c) =>
                    {
                        var cfg = sp.GetRequiredService<IAppConfig>();
                        c.BaseAddress = new Uri(cfg.ApiBaseUrl);
                        c.Timeout = TimeSpan.FromSeconds(30);
                    }).AddHttpMessageHandler<JwtHandler>();

                    services.AddSingleton<ImageManager>();

                    // Skin service for downloading and caching skins
                    services.AddSingleton<ISkinService, SkinService>();
                    services.AddSingleton<ITokenStore>(sp =>
                    {
                        var cfg = sp.GetRequiredService<IAppConfig>();
                        return new SecureTokenStore(cfg.GameRootPath);
                    });

                    // --- 2. ОСНОВНЫЕ СЕРВИСЫ ---
                    services.AddSingleton<AuthService>();
                    services.AddSingleton<NewsService>();
                    services.AddSingleton<VersionsManager>();
                    services.AddSingleton<DataManager>();
                    services.AddSingleton<LauncherService>();
                    services.AddSingleton<ModPackManager>();
                    services.AddSingleton<Runia.Services.Installers.JavaManager>();
                    services.AddSingleton<Runia.Services.Installers.ForgeManager>();

                    // --- 3. Explicit registrations (SoC-friendly) ---
                    // HTTP client for images
                    services.AddHttpClient("ImageClient", c => c.Timeout = TimeSpan.FromSeconds(30));

                    // Infrastructure (pure C#) responsible for downloading bytes
                    services.AddSingleton<Runia.Services.Core.Infrastructure.Network.IImageDownloader, Runia.Services.Core.Infrastructure.Network.ImageDownloader>();

                    // UI service responsible for converting local path to ImageSource
                    services.AddSingleton<IImageProvider, ImageProvider>();

                    // News service keeps cache of news (ensure it's registered)
                    services.AddSingleton<NewsService>();

                    // ViewModels: HomeViewModel is singleton to preserve its state and avoid reloading
                    services.AddSingleton<HomeViewModel>();
                    services.AddTransient<NewsDetailsViewModel>();
                    // Window/View models
                    services.AddTransient<LoginWindowViewModel>();
                    services.AddTransient<MainWindowViewModel>();

                    // Auth flow
                    services.AddTransient<Runia.ViewModels.Auth.LoginViewModel>();
                    services.AddTransient<Runia.ViewModels.Auth.RegisterViewModel>();
                    services.AddTransient<Runia.ViewModels.Auth.ForgotPasswordViewModel>();
                    services.AddTransient<Runia.ViewModels.Auth.ResetPasswordViewModel>();
                    services.AddTransient<Runia.ViewModels.Auth.ConfirmEmailViewModel>();

                    // Other main viewmodels
                    services.AddTransient<Runia.ViewModels.Main.ServerDetailsViewModel>();
                    services.AddTransient<Runia.ViewModels.Main.ServerViewModel>();
                    services.AddTransient<Runia.ViewModels.Main.SettingsViewModel>();
                    services.AddTransient<Runia.ViewModels.Main.SidebarViewModel>();
                    services.AddTransient<Runia.ViewModels.Main.UserViewModel>();
                    services.AddTransient<Runia.ViewModels.Main.SkinDownloadViewModel>();

                    // --- Pages: register as singletons so UI pages are cached and reused ---
                    services.AddSingleton<Runia.Ui.PagesMain.HomePage>();
                    services.AddSingleton<Runia.Ui.PagesMain.ServerPage>();
                    services.AddSingleton<Runia.Ui.PagesMain.SettingsPage>();
                    services.AddSingleton<Runia.Ui.PagesMain.UserPage>();
                    services.AddSingleton<Runia.Ui.PagesMain.NewsDetailsPage>();
                    services.AddSingleton<Runia.Ui.PagesMain.ServerDetailsPage>();
                    services.AddSingleton<Runia.Ui.PagesMain.SideBarPage>();
                    // Auth pages
                    services.AddSingleton<Runia.Ui.PagesUser.LoginPage>();
                    services.AddSingleton<Runia.Ui.PagesUser.ForgotPasswordPage>();
                    services.AddSingleton<Runia.Ui.PagesUser.ConfirmEmailPage>();
                    services.AddSingleton<Runia.Ui.PagesUser.RegisterPage>();
                    services.AddSingleton<Runia.Ui.PagesUser.ResetPasswordPage>();

                    // --- 4. ОКНА ---
                    services.AddTransient<Runia.Ui.Windows.LoginWindow>();
                    services.AddTransient<Runia.Ui.Windows.MainWindow>();
                })
                .Build();

            _host.Start();
            Services = _host.Services;

            try
            {
                Runia.Services.Common.ThemeManager.Initialize();
            }
            catch { }

            var loginWindow = Services.GetRequiredService<Runia.Ui.Windows.LoginWindow>();
            loginWindow.DataContext = Services.GetRequiredService<LoginWindowViewModel>();
            loginWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_host != null)
            {
                _host.StopAsync().GetAwaiter().GetResult();
                _host.Dispose();
            }
            base.OnExit(e);
        }
    }
}