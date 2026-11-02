using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Runia.Services.Auth;
using Runia.Services.Common;
using Runia.Services.Core.Infrastructure.Configuration;
using Runia.Services.Core.Infrastructure.Persistence;
using Runia.Services.Core.Launcher;
using Runia.Services.Core.Navigation;
using Runia.Services.Core.Infrastructure.Network;
using Runia.Services.Installers;
using System.IO;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Runia.Services.Core.Infrastructure.Logging;
using Runia.ViewModels.Main;
using Runia.ViewModels.Windows;
using System.Windows;
using Runia.Services.Core.Infrastructure.Network.Clients;
using Velopack;
using Velopack.Sources;

namespace Runia
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;
        private IHost? _host;
        private readonly TaskCompletionSource<bool> _updateCheckCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _launcherStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Runia.Ui.Windows.UpdateStatusWindow? _updateStatusWindow;
        private bool _launcherHasStarted;
        private string? _currentUpdateStatus;

        protected override async void OnStartup(StartupEventArgs e)
        {
          
            VelopackApp.Build().Run();

            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // --- 1. БАЗОВАЯ ИНФРАСТРУКТУРА ---
                    services.AddSingleton<IAppConfig, DefaultAppConfig>();
                    services.AddSingleton<IDialogService, DialogService>();
                    services.AddSingleton<INavigationService, NavigationService>();
                    services.AddSingleton<Runia.Services.Core.Infrastructure.ILocalDataService, Runia.Services.Core.Infrastructure.LocalDataService>();

                    services.AddSingleton<ITokenStore>(sp =>
                    {
                        var cfg = sp.GetRequiredService<IAppConfig>();
                        return new SecureTokenStore(cfg.GameRootPath);
                    });

                    services.AddTransient<JwtHandler>();

                    services.AddHttpClient("SkinClient", (sp, c) =>
                    {
                        var cfg = sp.GetRequiredService<IAppConfig>();
                        c.BaseAddress = new Uri(cfg.ApiBaseUrl);
                        c.Timeout = TimeSpan.FromSeconds(60);
                    }).AddHttpMessageHandler<JwtHandler>();
                    services.AddHttpClient<BaseApiService>((sp, c) =>
                    {
                        var cfg = sp.GetRequiredService<IAppConfig>();
                        c.BaseAddress = new Uri(cfg.ApiBaseUrl);
                        c.Timeout = TimeSpan.FromSeconds(30);
                    }).AddHttpMessageHandler<JwtHandler>();

                    // --- РЕГИСТРАЦИЯ NETWORK КЛИЕНТОВ ---
                    services.AddSingleton<ServerClient>();
                    services.AddSingleton<NewsClient>(sp =>
                    {
                        var apiService = sp.GetRequiredService<BaseApiService>();
                        var localData = sp.GetService<Runia.Services.Core.Infrastructure.ILocalDataService>();
                        return new NewsClient(apiService, localData);
                    });
                    services.AddSingleton<UserClient>(sp =>
                    {
                        var api = sp.GetRequiredService<BaseApiService>();
                        var tokenStore = sp.GetRequiredService<Runia.Services.Core.Infrastructure.Network.ITokenStore>();
                        var auth = sp.GetRequiredService<AuthService>();
                        return new UserClient(api, tokenStore, auth);
                    });
                    services.AddSingleton<SessionClient>(sp =>
                    {
                        var api = sp.GetRequiredService<BaseApiService>();
                        return new SessionClient(api);
                    });
                    services.AddSingleton<AuthClient>();
                    services.AddSingleton<ImageManager>();
                    services.AddSingleton<ISkinService, SkinService>();

                    // --- 2. ОСНОВНЫЕ СЕРВИСЫ ---
                    services.AddSingleton<AuthService>();
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

                    // Register file logger provider to capture logs into log.txt at app root
                    var logPath = Path.Combine(AppContext.BaseDirectory, "log.txt");
                    var fileLoggerProvider = new FileLoggerProvider(logPath);
                    services.AddSingleton<ILoggerProvider>(sp => fileLoggerProvider);

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
                    services.AddSingleton<Runia.ViewModels.Main.ServerViewModel>();
                    services.AddTransient<Runia.ViewModels.Main.SettingsViewModel>();
                    services.AddTransient<Runia.ViewModels.Main.SidebarViewModel>();
                    services.AddTransient<Runia.ViewModels.Main.UserViewModel>();
                    services.AddTransient<Runia.ViewModels.Main.SecurityUserViewModel>();
                    services.AddTransient<Runia.ViewModels.Main.PaymentHistoryUserViewModel>();

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

            // Ждем проверку не дольше двух секунд; проверка и загрузка затем продолжаются в фоне.
            _ = CheckForUpdatesAsync();
            await Task.WhenAny(_updateCheckCompleted.Task, Task.Delay(TimeSpan.FromSeconds(2)));

            try
            {
                var auth = Services.GetRequiredService<AuthService>();
                var dialog = Services.GetRequiredService<IDialogService>();

                var loadResult = await auth.LoadAndRefreshTokensAsync().ConfigureAwait(false);
                if (loadResult == AuthResult.Success)
                {
                    var refreshed = await auth.RefreshTokenAsync().ConfigureAwait(false);
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (refreshed)
                        {
                            dialog.ShowMainWindow();
                        }
                        else
                        {
                            dialog.ShowLoginWindow();
                        }
                        MarkLauncherStarted();
                    });
                }
                else
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        dialog.ShowLoginWindow();
                        MarkLauncherStarted();
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] Auto-login failed: {ex}");
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var loginWindow = Services.GetRequiredService<Runia.Ui.Windows.LoginWindow>();
                    loginWindow.DataContext = Services.GetRequiredService<LoginWindowViewModel>();
                    loginWindow.Show();
                    MarkLauncherStarted();
                });
            }
        }

        private void MarkLauncherStarted()
        {
            _launcherHasStarted = true;
            _launcherStarted.TrySetResult(true);
            if (_currentUpdateStatus is not null)
            {
                SetUpdateStatus(_currentUpdateStatus);
            }
        }

        private void SetUpdateStatus(string? status)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(() => SetUpdateStatus(status));
                return;
            }

            _currentUpdateStatus = status;
            if (!_launcherHasStarted)
            {
                return;
            }

            if (status is null)
            {
                _updateStatusWindow?.Close();
                _updateStatusWindow = null;
                return;
            }

            _updateStatusWindow ??= new Runia.Ui.Windows.UpdateStatusWindow();
            _updateStatusWindow.SetStatus(status);
            if (!_updateStatusWindow.IsVisible)
            {
                _updateStatusWindow.Show();
            }
        }

        /// <summary>
        /// Проверяет обновления сразу, а загрузку начинает только после появления окна лаунчера.
        /// </summary>
        private async Task CheckForUpdatesAsync()
        {
            SetUpdateStatus("Проверяем обновления...");
            try
            {
                Debug.WriteLine("[App] Velopack: Проверка обновлений на GitHub...");
                var source = new GithubSource("https://github.com/mokkignomov-design/Launcher", null, false);
                var mgr = new UpdateManager(source);
                var newVersion = await mgr.CheckForUpdatesAsync();
                _updateCheckCompleted.TrySetResult(true);

                if (newVersion is null)
                {
                    Debug.WriteLine("[App] Velopack: Установлена актуальная версия.");
                    SetUpdateStatus(null);
                    return;
                }

                Debug.WriteLine($"[App] Velopack: Найдена новая версия {newVersion.TargetFullRelease.Version}.");
                await _launcherStarted.Task;

                SetUpdateStatus("Загружаем обновление...");
                await mgr.DownloadUpdatesAsync(newVersion);

                Debug.WriteLine("[App] Velopack: Обновление скачано! Перезапускаю лаунчер...");
                SetUpdateStatus("Устанавливаем обновление...");
                mgr.ApplyUpdatesAndRestart(newVersion);
            }
            catch (Exception ex)
            {
                _updateCheckCompleted.TrySetResult(true);
                SetUpdateStatus(null);
                Debug.WriteLine($"[App] Velopack Update Error: {ex.Message}");
            }
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