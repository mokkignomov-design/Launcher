using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;
using Runia.Services.Auth;
using Runia.Services.Core.Infrastructure.Configuration;
using Runia.Services.Installers;
using System.Diagnostics;
using System.Windows;

namespace Runia.Services.Core.Launcher
{
    public class LauncherService(JavaManager javaManager, ForgeManager forgeManager, ModPackManager modPackManager, IAppConfig config, AuthService authService)
    {
        public event Action? OnGameStarted;
        public event Action? OnGameExited;

        private readonly JavaManager _javaManager = javaManager ?? throw new ArgumentNullException(nameof(javaManager));
        private readonly ForgeManager _forgeManager = forgeManager ?? throw new ArgumentNullException(nameof(forgeManager));
        private readonly ModPackManager _modPackManager = modPackManager ?? throw new ArgumentNullException(nameof(modPackManager));
        private readonly IAppConfig _config = config ?? throw new ArgumentNullException(nameof(config));
        private readonly AuthService _authService = authService ?? throw new ArgumentNullException(nameof(authService));

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

                var launchOption = new MLaunchOption
                {
                    JavaPath = javaPath,
                    Session = MSession.CreateOfflineSession(username),
                    MaximumRamMb = ram,
                    FullScreen = _config.IsFullscreen
                };

                if (!string.IsNullOrWhiteSpace(_config.JvmArguments))
                {
                    launchOption.ExtraJvmArguments = _config.JvmArguments
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Select(arg => new MArgument(arg));
                }

                logProgress?.Report("Creating process...");
                var process = await launcher.CreateProcessAsync(versionId, launchOption);

                ct.ThrowIfCancellationRequested();

                process.StartInfo.Arguments = ConfigureArguments(process.StartInfo.Arguments, instancePath);
                process.StartInfo.WorkingDirectory = instancePath;

                process.StartInfo.UseShellExecute = _config.ShowConsole;
                process.StartInfo.CreateNoWindow = !_config.ShowConsole;

                if (process.Start())
                {
                    logProgress?.Report("Game started");
                    OnGameStarted?.Invoke();

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
                            try
                            {
                                if (!process.HasExited) process.Kill(entireProcessTree: true);
                            }
                            catch { }
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