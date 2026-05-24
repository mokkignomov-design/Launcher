using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using Runia.Services.Auth;
using Runia.Services.Core.Infrastructure.Configuration;
using Runia.Services.Installers;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using Runia.Services.Core.Infrastructure.Network.Clients;


namespace Runia.Services.Core.Launcher
{
    public class LauncherService(JavaManager javaManager, ForgeManager forgeManager, ModPackManager modPackManager, IAppConfig config, AuthService authService, SessionClient sessionClient)
    {
        public event Action? OnGameStarted;
        public event Action? OnGameExited;

        private readonly JavaManager _javaManager = javaManager ?? throw new ArgumentNullException(nameof(javaManager));
        private readonly ForgeManager _forgeManager = forgeManager ?? throw new ArgumentNullException(nameof(forgeManager));
        private readonly ModPackManager _modPackManager = modPackManager ?? throw new ArgumentNullException(nameof(modPackManager));
        private readonly IAppConfig _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly AuthService _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        private readonly SessionClient _sessionClient = sessionClient ?? throw new ArgumentNullException(nameof(sessionClient));
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        public async Task LaunchPackAsync(string packName, IProgress<string>? logProgress, CancellationToken ct)
        {
            try
            {

                ct.ThrowIfCancellationRequested();

                var servers = await _config.GetServersAsync();
                var currentServer = servers.FirstOrDefault(s => s.Title == packName)
                    ?? throw new Exception($"Сервер {packName} не найден");

                ct.ThrowIfCancellationRequested();

                int ram = _config.RamAmount;
                string instancePath = VersionsManager.GetVersionPath(packName);

                logProgress?.Report("Preparing directories...");
                VersionsManager.EnsureDirectoriesExist(packName);

                if (!string.IsNullOrWhiteSpace(currentServer.ZipUrl))
                {
                    ct.ThrowIfCancellationRequested();
                    logProgress?.Report("Checking modpack...");
                    var downloadProgress = new Progress<double>(p => logProgress?.Report($"Downloading: {p:F0}%"));
                    await _modPackManager.InstallModpackAsync(currentServer.ZipUrl, instancePath, downloadProgress, ct);
                }

                ct.ThrowIfCancellationRequested();

                var launcher = new MinecraftLauncher(new MinecraftPath(VersionsManager.Root));

                logProgress?.Report("Installing base Minecraft...");
                await launcher.InstallAsync(currentServer.McVersion, ct);

                ct.ThrowIfCancellationRequested();

                string versionId = currentServer.McVersion;
                if (!string.IsNullOrWhiteSpace(currentServer.ForgeVersion))
                {
                    ct.ThrowIfCancellationRequested();
                    logProgress?.Report("Checking Forge...");
                    versionId = await _forgeManager.InstallForgeAsync(currentServer.McVersion, currentServer.ForgeVersion, logProgress, ct);
                }

                ct.ThrowIfCancellationRequested();

                string javaPath = await _javaManager.GetJavaPathAsync() ?? throw new Exception("Java not found");
                string username = _authService.CurrentUser?.Name ?? "Player";

                // ЧИСТЫЙ БЛОК АВТОРИЗАЦИИ: обновляем access token и получаем временный токен ДО создания процесса
                var refreshed = false;
                try
                {
                    refreshed = await _authService.RefreshTokenAsync(ct);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[LauncherService] Refresh token failed: {ex}");
                }
                if (!refreshed)
                {
                    logProgress?.Report("Ошибка авторизации: не удалось обновить токен");
                    throw new Exception("Ошибка авторизации: не удалось обновить токен");
                }

                var sessionResult = await _sessionClient.CreateTemporarySessionAsync(ct);
                if (!sessionResult.IsSuccess)
                {
                    var msg = sessionResult.ErrorMessage ?? "Ошибка авторизации при создании сессии";
                    logProgress?.Report($"Ошибка авторизации: {msg}");
                    throw new Exception($"Ошибка авторизации: {msg}");
                }
                var session = sessionResult.Data!;

                var launchOption = new MLaunchOption
                {
                    JavaPath = javaPath,
                    Session = MSession.CreateOfflineSession(username),
                    MaximumRamMb = ram,
                    FullScreen = _config.IsFullscreen
                };

                // ИНИЦИАЛИЗАЦИЯ АРГУМЕНТОВ JAVA
                var jvmArgs = new List<MArgument>();
                if (!string.IsNullOrWhiteSpace(_config.JvmArguments))
                {
                    jvmArgs.AddRange(_config.JvmArguments
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Select(arg => new MArgument(arg)));
                }

                // ВАЖНО: Добавляем токен как глобальную системную переменную Java (-D)
                jvmArgs.Add(new MArgument($"-Drunia.temporaryKey={session.TemporaryKey}"));
                launchOption.ExtraJvmArguments = jvmArgs;

                // НАЙДИ В LAUNCHERSERVICE ЭТОТ КУСОК И ЗАМЕНИ:

                logProgress?.Report("Creating process...");
                var process = await launcher.CreateProcessAsync(versionId, launchOption);

                ct.ThrowIfCancellationRequested();

                // Гарантированный проброс JVM-аргумента прямо в строку запуска процесса
                string runiaJvmArg = $"-Drunia.temporaryKey={session.TemporaryKey}";
                process.StartInfo.Arguments = $"{runiaJvmArg} {process.StartInfo.Arguments}";

                // Накатываем твои пути
                process.StartInfo.WorkingDirectory = instancePath;
                process.StartInfo.Arguments = ConfigureArguments(process.StartInfo.Arguments, instancePath);

                // ВЫВОДИМ ФИНАЛЬНУЮ СТРОКУ ЗАПУСКА В ДЕБАГ (Посмотришь в выводе VS/Rider)
                Debug.WriteLine($"[RuniaLauncher] ФИНАЛЬНАЯ СТРОКА ЗАПУСКА: {process.StartInfo.FileName} {process.StartInfo.Arguments}");
                logProgress?.Report($"Запуск с ключом: {session.TemporaryKey}");

                process.StartInfo.UseShellExecute = _config.ShowConsole;
                process.StartInfo.CreateNoWindow = !_config.ShowConsole;
                // Внутри твоего LaunchService перед запуском процесса:
                string sessionFilePath = Path.Combine(instancePath, "runia_session.json");
                var sessionData = new { token = session.TemporaryKey };
                string json = JsonSerializer.Serialize(sessionData);

                // Записываем файл (если остался старый — перезапишется)
                File.WriteAllText(sessionFilePath, json);

                // ==================== ДОБАВЛЯЕМ ПРЕДУСТАНОВКУ СКИНООБЕСПЕЧЕНИЯ (ЧЕРЕЗ EXTRALIST) ====================
                try
                {
                    // CSL ищет ExtraList по пути: .minecraft/CustomSkinLoader/ExtraList/
                    string extraListDir = Path.Combine(instancePath, "CustomSkinLoader", "ExtraList");
                    if (!Directory.Exists(extraListDir))
                    {
                        Directory.CreateDirectory(extraListDir);
                    }

                    // Имя файла может быть любым, например, runia.json
                    string extraListPath = Path.Combine(extraListDir, "runia.json");

                    // Формат объекта ExtraList полностью соответствует элементу из loadlist
                    var runiaExtraSkinSource = new
                    {
                        name = "RuniaSkinAPI",
                        type = "CustomSkinAPI",
                        root = "https://api.runia.su/api/v1/skins/info/"
                    };

                    string extraListJson = JsonSerializer.Serialize(runiaExtraSkinSource, _jsonOptions);
                    File.WriteAllText(extraListPath, extraListJson);

                    Debug.WriteLine("[RuniaLauncher] Файл ExtraList для CustomSkinLoader успешно создан.");
                }
                catch (Exception ex)
                {
                    // Ошибки записи во вспомогательный конфиг скинов не должны крашить запуск самой игры
                    Debug.WriteLine($"[ERROR] Не удалось создать ExtraList для CustomSkinLoader: {ex.Message}");
                }
                // ====================================================================================================


                if (process.Start())
                {
                    logProgress?.Report("Game started");
                    OnGameStarted?.Invoke();

                    try
                    {
                        Runia.Properties.Settings.Default.LastLaunchedServer = packName;
                        Runia.Properties.Settings.Default.Save();
                    }
                    catch { }

                    if (Runia.Properties.Settings.Default.AutoCloseLauncher)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var mainWin = Application.Current.Windows.OfType<Runia.Ui.Windows.MainWindow>().FirstOrDefault();
                            mainWin?.Hide();
                        });
                    }

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await process.WaitForExitAsync(ct);
                        }
                        catch (OperationCanceledException)
                        {
                            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
                        }
                        finally
                        {
                            OnGameExited?.Invoke();

                            if (Runia.Properties.Settings.Default.AutoCloseLauncher)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    var mainWin = Application.Current.Windows.OfType<Runia.Ui.Windows.MainWindow>().FirstOrDefault();
                                    if (mainWin != null)
                                    {
                                        mainWin.Show();
                                        mainWin.WindowState = WindowState.Normal;
                                        mainWin.Activate();
                                    }
                                });
                            }
                        }
                    }, ct);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[LauncherService] Launch cancelled");
                logProgress?.Report("Отмена...");
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] {ex}");
                logProgress?.Report($"Ошибка: {ex.Message}");
                OnGameExited?.Invoke();
                throw;
            }
        }

        private static string ConfigureArguments(string args, string path)
        {
            if (string.IsNullOrEmpty(args)) return args;

            string safePath = path.Trim('\"');
            var parts = args.Split(' ');

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "--gameDir" && i + 1 < parts.Length)
                {
                    parts[i + 1] = $"\"{safePath}\"";
                }
            }
            return string.Join(" ", parts);
        }
    }
}