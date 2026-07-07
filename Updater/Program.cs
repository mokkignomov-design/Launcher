using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace Runia.Updater;

internal static class Program
{
    private static readonly string PrimaryBase = "https://raw.githubusercontent.com/your-org/your-repo/main/updates"; // заменить
    private static readonly string FallbackBase = "https://your-vps.example.com/updates"; // заменить

    private static async Task<int> Main(string[] args)
    {
        try
        {
            // Подкоманда apply: Updater.exe apply <sourceFile> <targetExe> [restartArgs]
            if (args.Length > 0 && string.Equals(args[0], "apply", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 3)
                {
                    Console.Error.WriteLine("Usage: updater apply <sourceFile> <targetExe> [restartArgs]");
                    return 2;
                }

                var src = args[1];
                var target = args[2];
                var restartArgs = args.Length >= 4 ? args[3] : string.Empty;
                return await ApplyAndRestartAsync(src, target, restartArgs);
            }

            using var http = new HttpClient();
            var baseUrl = await ChooseBaseUrl(http);

            string assetUrl = string.Empty;
            string expectedSha = string.Empty;

            if (args.Length >= 2)
            {
                // arg0 = assetUrl, arg1 = expectedSha
                assetUrl = args[0];
                expectedSha = args[1];
            }
            else
            {
                var metaUrl = new Uri(new Uri(baseUrl), "latest.json");
                try
                {
                    var meta = await http.GetStringAsync(metaUrl);
                    using var doc = JsonDocument.Parse(meta);
                    if (doc.RootElement.TryGetProperty("assetUrl", out var a)) assetUrl = a.GetString() ?? string.Empty;
                    if (doc.RootElement.TryGetProperty("sha256", out var s)) expectedSha = s.GetString() ?? string.Empty;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Не удалось получить metadata: {ex.Message}");
                    return 2;
                }
            }

            if (string.IsNullOrEmpty(assetUrl))
            {
                Console.Error.WriteLine("assetUrl не задан");
                return 3;
            }

            var updatesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Runia", "updates");
            Directory.CreateDirectory(updatesDir);

            var fileName = Path.GetFileName(new Uri(assetUrl).LocalPath);
            var destPath = Path.Combine(updatesDir, fileName);

            Console.WriteLine($"Скачивание {assetUrl} -> {destPath}");
            await DownloadFileAsync(http, assetUrl, destPath);

            if (!string.IsNullOrEmpty(expectedSha))
            {
                var actual = ComputeSha256(destPath);
                Console.WriteLine($"Ожидаемый SHA256: {expectedSha}");
                Console.WriteLine($"Фактический SHA256:  {actual}");
                if (!string.Equals(actual, expectedSha, StringComparison.OrdinalIgnoreCase))
                {
                    Console.Error.WriteLine("SHA256 не совпадает, отмена");
                    return 4;
                }
            }

            Console.WriteLine("Файл загружен и проверен. Чтобы применить, запустите: updater apply <source> <targetExe> [args]");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unhandled: {ex}");
            return 10;
        }
    }

    private static async Task<int> ApplyAndRestartAsync(string sourceFile, string targetExe, string restartArgs)
    {
        if (!File.Exists(sourceFile))
        {
            Console.Error.WriteLine($"Source file not found: {sourceFile}");
            return 5;
        }

        // Ждём, пока целевой процесс не завершится
        var procName = Path.GetFileNameWithoutExtension(targetExe);
        Console.WriteLine($"Ожидание завершения процессов: {procName}");
        var waited = TryWaitForProcessExit(procName, TimeSpan.FromSeconds(30));
        if (!waited)
        {
            Console.Error.WriteLine("Процесс не завершился в ожидание, отмена");
            return 6;
        }

        try
        {
            ReplaceExecutable(sourceFile, targetExe);
            Console.WriteLine("Замена выполнена. Перезапуск приложения...");
            RestartTarget(targetExe, restartArgs);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Ошибка при применении обновления: {ex}");
            return 7;
        }
    }

    private static bool TryWaitForProcessExit(string processName, TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            var procs = Process.GetProcessesByName(processName);
            if (procs.Length == 0) return true;
            Thread.Sleep(500);
        }
        return false;
    }

    private static void ReplaceExecutable(string sourceFile, string targetExe)
    {
        var backup = targetExe + ".bak";

        // Если целевой файл существует, используем File.Replace (atomic на NTFS), иначе просто перемещаем
        if (File.Exists(targetExe))
        {
            // File.Replace требует, чтобы целевой файл существовал
            File.Replace(sourceFile, targetExe, backup, ignoreMetadataErrors: true);
            // Удаляем backup
            if (File.Exists(backup)) File.Delete(backup);
        }
        else
        {
            File.Move(sourceFile, targetExe);
        }
    }

    private static void RestartTarget(string targetExe, string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = targetExe,
            Arguments = args,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(targetExe) ?? string.Empty,
        };
        Process.Start(psi);
    }

    private static async Task<string> ChooseBaseUrl(HttpClient http)
    {
        using var cts = new CancellationTokenSource(2000);
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Head, "https://github.com");
            var resp = await http.SendAsync(req, cts.Token);
            if (resp.IsSuccessStatusCode)
            {
                Console.WriteLine("Primary (GitHub) доступен, используем основной URL");
                return PrimaryBase;
            }
        }
        catch
        {
        }

        Console.WriteLine("Primary недоступен, переключаемся на fallback");
        return FallbackBase;
    }

    private static async Task DownloadFileAsync(HttpClient http, string url, string destPath)
    {
        using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();
        await using var src = await resp.Content.ReadAsStreamAsync();
        await using var dst = File.Open(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await src.CopyToAsync(dst);
    }

    private static string ComputeSha256(string filePath)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha.ComputeHash(stream);
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }
}
