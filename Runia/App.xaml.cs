using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Runia.Services.Auth;
using Runia.Services.Common;
using Runia.Services.Core.Infrastructure.Configuration;
using Runia.Services.Core.Infrastructure.Network;
using Runia.Services.Core.Infrastructure.Persistence;
using Runia.Services.Core.Launcher;
using Runia.Services.Core.Navigation;
using Runia.Services.Installers;
using Runia.ViewModels.Main;
using Runia.ViewModels.Windows;
using System;
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

                    // Регистрация хранилища токенов
                    services.AddSingleton<ITokenStore>(sp =>
                    {
                        var cfg = sp.GetRequiredService<IAppConfig>();
                        return new SecureTokenStore(cfg.GameRootPath);
                    });

                    // Хендлер для JWT (Transient обязателен для HttpClient)
                    services.AddTransient<JwtHandler>();

                    // Регистрация именованного клиента специально для SkinService
                    services.AddHttpClient("SkinClient", (sp, c) =>
                    {
                        var cfg = sp.GetRequiredService<IAppConfig>();
                        c.BaseAddress = new Uri(cfg.ApiBaseUrl);
                        c.Timeout = TimeSpan.FromSeconds(60);
                    }).AddHttpMessageHandler<JwtHandler>();

                    // Регистрация BaseApiService как типизированного клиента
                    services.AddHttpClient<BaseApiService>((sp, c) =>
                    {
                        var cfg = sp.GetRequiredService<IAppConfig>();
                        c.BaseAddress = new Uri(cfg.ApiBaseUrl);
                        c.Timeout = TimeSpan.FromSeconds(30);
                    }).AddHttpMessageHandler<JwtHandler>();

                    services.AddSingleton<ImageManager>();

                    // РЕГИСТРАЦИЯ SKIN SERVICE (как Singleton, чтобы DI его видел для навигации)
                    services.AddSingleton<ISkinService, SkinService>();

                    // --- 2. ОСНОВНЫЕ СЕРВИСЫ ---
                    services.AddSingleton<AuthService>();
                    services.AddSingleton<NewsService>();
                    services.AddSingleton<VersionsManager>();
                    services.AddSingleton<DataManager>();
                    services.AddSingleton<LauncherService>();
                    services.AddSingleton<ModPackManager>();
                    services.AddSingleton<Runia.Services.Installers.JavaManager>();
                    services.AddSingleton<Runia.Services.Installers.ForgeManager>();

                    // --- 3. Вспомогательные регистрации ---
                    services.AddHttpClient("ImageClient", c => c.Timeout = TimeSpan.FromSeconds(30));
                    services.AddSingleton<Runia.Services.Core.Infrastructure.Network.IImageDownloader, Runia.Services.Core.Infrastructure.Network.ImageDownloader>();
                    services.AddSingleton<IImageProvider, ImageProvider>();

                    // ViewModels
                    services.AddSingleton<HomeViewModel>();
                    services.AddTransient<NewsDetailsViewModel>();
                    services.AddTransient<LoginWindowViewModel>();
                    services.AddSingleton<MainWindowViewModel>();

                    // Auth flow ViewModels
                    services.AddTransient<Runia.ViewModels.Auth.LoginViewModel>();
                    services.AddTransient<Runia.ViewModels.Auth.RegisterViewModel>();
                    services.AddTransient<Runia.ViewModels.Auth.ForgotPasswordViewModel>();
                    services.AddTransient<Runia.ViewModels.Auth.ResetPasswordViewModel>();
                    services.AddTransient<Runia.ViewModels.Auth.ConfirmEmailViewModel>();

                    // Main ViewModels
                    services.AddTransient<Runia.ViewModels.Main.ServerDetailsViewModel>();
                    services.AddTransient<Runia.ViewModels.Main.ServerViewModel>();
                    services.AddTransient<Runia.ViewModels.Main.SettingsViewModel>();
                    services.AddTransient<Runia.ViewModels.Main.SidebarViewModel>();
                    services.AddTransient<Runia.ViewModels.Main.UserViewModel>();

                    // Pages
                    services.AddSingleton<Runia.Ui.PagesMain.HomePage>();
                    services.AddSingleton<Runia.Ui.PagesMain.ServerPage>();
                    services.AddSingleton<Runia.Ui.PagesMain.SettingsPage>();
                    services.AddSingleton<Runia.Ui.PagesMain.UserPage>();
                    services.AddSingleton<Runia.Ui.PagesMain.NewsDetailsPage>();
                    services.AddSingleton<Runia.Ui.PagesMain.ServerDetailsPage>();
                    services.AddSingleton<Runia.Ui.PagesMain.SideBarPage>();

                    services.AddSingleton<Runia.Ui.PagesUser.LoginPage>();
                    services.AddSingleton<Runia.Ui.PagesUser.ForgotPasswordPage>();
                    services.AddSingleton<Runia.Ui.PagesUser.ConfirmEmailPage>();
                    services.AddSingleton<Runia.Ui.PagesUser.RegisterPage>();
                    services.AddSingleton<Runia.Ui.PagesUser.ResetPasswordPage>();

                    // Windows
                    services.AddTransient<Runia.Ui.Windows.LoginWindow>();
                    services.AddSingleton<Runia.Ui.Windows.MainWindow>();
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