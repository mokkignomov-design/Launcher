using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace Runia.Updater;

/// <summary>
/// Runia Launcher Updater - простой апдейтер с поддержкой GitHub Releases
/// Скачивает и применяет обновления лаунчера
/// </summary>
internal static class Program
{
    // GitHub репозиторий
    private const string GitHubOrg = "mokkignomov-design";
    private const string GitHubRepo = "Launcher";
    private const string GitHubApiUrl = "https://api.github.com/repos/mokkignomov-design/Launcher/releases/latest";

    // Локальное хранилище обновлений
    private static readonly string UpdateCachePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Runia",
        "updates"
    );

    private static async Task<int> Main(string[] args)
    {
        try
        {
            // Режим 1: Применение скачанного обновления
            if (args.Length > 0 && string.Equals(args[0], "apply", StringComparison.OrdinalIgnoreCase))
            {
                return await ApplyUpdate(args);
            }

            // Режим 2: Проверка и скачивание обновлений (фоновый режим)
            await CheckAndDownloadUpdates();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Updater Error] {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Проверяет наличие новых версий на GitHub и скачивает их
    /// </summary>
    private static async Task CheckAndDownloadUpdates()
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Runia-Launcher");

            Console.WriteLine("[Updater] Проверка обновлений...");

            // Получаем информацию о последнем релизе
            var response = await httpClient.GetAsync(GitHubApiUrl);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Updater] Не удалось получить информацию о релизе: {response.StatusCode}");
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            // Извлекаем версию и ссылку на скачивание
            var tagName = root.GetProperty("tag_name").GetString();
            var version = tagName?.TrimStart('v') ?? "0.0.0";

            Console.WriteLine($"[Updater] Найдена версия: {version}");

            // Получаем первый asset (обычно это .exe или .zip)
            var assets = root.GetProperty("assets");
            if (assets.GetArrayLength() == 0)
            {
                Console.WriteLine("[Updater] В релизе нет артефактов");
                return;
            }

            var exeAsset = FindAsset(assets, ".exe");
            if (exeAsset == null)
            {
                exeAsset = FindAsset(assets, ".zip");
            }

            if (exeAsset == null)
            {
                Console.WriteLine("[Updater] Не найден исполняемый файл или архив");
                return;
            }

            var downloadUrl = exeAsset.Value.GetProperty("browser_download_url").GetString();
            var fileName = exeAsset.Value.GetProperty("name").GetString();

            if (downloadUrl == null || fileName == null)
            {
                Console.WriteLine("[Updater] Не удалось получить URL скачивания");
                return;
            }

            // Проверяем, не скачали ли уже эту версию
            Directory.CreateDirectory(UpdateCachePath);
            var cachedFilePath = Path.Combine(UpdateCachePath, fileName);

            if (File.Exists(cachedFilePath))
            {
                Console.WriteLine($"[Updater] Обновление {fileName} уже закэшировано");
                return;
            }

            // Скачиваем обновление
            Console.WriteLine($"[Updater] Скачивание {fileName}...");
            var updateData = await httpClient.GetByteArrayAsync(downloadUrl);

            File.WriteAllBytes(cachedFilePath, updateData);
            Console.WriteLine($"[Updater] Обновление сохранено: {cachedFilePath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Updater] Ошибка при проверке обновлений: {ex.Message}");
        }
    }

    /// <summary>
    /// Применяет скачанное обновление, заменяя основной exe
    /// </summary>
    private static async Task<int> ApplyUpdate(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: Updater.exe apply <targetExe> [sourceExe]");
            return 2;
        }

        string targetExe = args[1];
        string sourceExe = args.Length > 2 ? args[2] : null!;

        try
        {
            if (!File.Exists(targetExe))
            {
                Console.Error.WriteLine($"[Updater] Целевой файл не найден: {targetExe}");
                return 3;
            }

            // Если источник не указан, пытаемся найти его в кэше
            if (sourceExe == null || !File.Exists(sourceExe))
            {
                sourceExe = FindLatestUpdateInCache();
                if (sourceExe == null)
                {
                    Console.Error.WriteLine("[Updater] Исходный файл обновления не найден");
                    return 3;
                }
            }

            Console.WriteLine($"[Updater] Применяю обновление...");
            Console.WriteLine($"  Источник: {sourceExe}");
            Console.WriteLine($"  Целевой: {targetExe}");

            // Создаём резервную копию
            var backupPath = targetExe + ".backup";
            File.Copy(targetExe, backupPath, true);
            Console.WriteLine($"[Updater] Резервная копия создана: {backupPath}");

            // Заменяем основной файл
            File.Copy(sourceExe, targetExe, true);
            Console.WriteLine("[Updater] Обновление применено успешно!");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Updater] Ошибка при применении обновления: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Найти asset по расширению файла
    /// </summary>
    private static JsonElement? FindAsset(JsonElement assets, string extension)
    {
        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            if (name.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                return asset;
            }
        }
        return null;
    }

    /// <summary>
    /// Найти последнее обновление в локальном кэше
    /// </summary>
    private static string? FindLatestUpdateInCache()
    {
        if (!Directory.Exists(UpdateCachePath))
        {
            return null;
        }

        var files = Directory.GetFiles(UpdateCachePath, "*.exe");
        if (files.Length == 0)
        {
            files = Directory.GetFiles(UpdateCachePath, "*.zip");
        }

        if (files.Length == 0)
        {
            return null;
        }

        // Возвращаем самый новый файл
        return files
            .OrderByDescending(f => new FileInfo(f).LastWriteTime)
            .FirstOrDefault();
    }
}
