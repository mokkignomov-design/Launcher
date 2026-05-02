# 📦 Установщики в Runia

Документация по управлению Forge, Java и модификационными пакетами.

## 📋 Обзор

Система установщиков управляет:
- **Forge** - Модификационный фреймворк для Minecraft
- **Java** - Среда выполнения Java для игр
- **ModPacks** - Модификационные пакеты

## 🏗️ Структура установщиков

```
Services/Installers/
├── ForgeManager.cs        # Управление Forge
├── JavaManager.cs         # Управление Java
└── ModPackManager.cs      # Управление модпаками
```

## 🔧 ForgeManager

**Файл**: `Services/Installers/ForgeManager.cs`

Управляет установкой и удалением Minecraft Forge.

```csharp
public class ForgeManager
{
    private readonly ILocalDataService _localStorage;
    private readonly IDialogService _dialog;
    private const string ForgeDataKey = "forge_installations";

    public ForgeManager(
        ILocalDataService localStorage,
        IDialogService dialog)
    {
        _localStorage = localStorage;
        _dialog = dialog;
    }

    // Установить Forge
    public async Task<Result<bool>> InstallForgeAsync(
        string version,
        string installPath)
    {
        try
        {
            // Проверить, что Java установлена
            var javaCheck = JavaManager.IsJavaInstalled();
            if (!javaCheck)
            {
                return Result<bool>.Failure(
                    InstallerError.MissingDependency,
                    "Java не установлена");
            }

            // Скачать Forge installer
            var installerUrl = $"https://files.minecraftforge.net/net/minecraftforge/" +
                              $"forge/{version}/forge-{version}-installer.jar";

            // Запустить установку
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-jar forge-{version}-installer.jar --installServer",
                    WorkingDirectory = installPath,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await Task.Run(() => process.WaitForExit());

            if (process.ExitCode == 0)
            {
                // Сохранить информацию об установке
                SaveForgeInstallation(version, installPath);
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(
                InstallerError.InstallationFailed,
                "Ошибка при установке Forge");
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(
                InstallerError.Unknown,
                ex.Message);
        }
    }

    // Удалить Forge
    public async Task<Result<bool>> UninstallForgeAsync(string version)
    {
        try
        {
            var installations = GetForgeInstallations();
            var installation = installations.FirstOrDefault(i => i.Version == version);

            if (installation == null)
            {
                return Result<bool>.Failure(
                    InstallerError.NotFound,
                    "Forge версия не найдена");
            }

            // Удалить файлы
            if (Directory.Exists(installation.InstallPath))
            {
                Directory.Delete(installation.InstallPath, true);
            }

            // Обновить список установок
            installations.Remove(installation);
            SaveForgeInstallations(installations);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(
                InstallerError.Unknown,
                ex.Message);
        }
    }

    // Получить список установленных версий
    public List<ForgeInstallation> GetInstalledVersions()
    {
        return _localStorage.LoadData<List<ForgeInstallation>>(ForgeDataKey) ?? 
            new List<ForgeInstallation>();
    }

    private void SaveForgeInstallation(string version, string installPath)
    {
        var installations = GetInstalledVersions();
        installations.Add(new ForgeInstallation 
        { 
            Version = version, 
            InstallPath = installPath,
            InstalledDate = DateTime.Now
        });
        _localStorage.SaveData(ForgeDataKey, installations);
    }

    private void SaveForgeInstallations(List<ForgeInstallation> installations)
    {
        _localStorage.SaveData(ForgeDataKey, installations);
    }
}

public class ForgeInstallation
{
    public string Version { get; set; }
    public string InstallPath { get; set; }
    public DateTime InstalledDate { get; set; }
}

public enum InstallerError
{
    None,
    MissingDependency,
    InstallationFailed,
    NotFound,
    InvalidPath,
    PermissionDenied,
    Unknown
}
```

## ☕ JavaManager

**Файл**: `Services/Installers/JavaManager.cs`

Управляет установкой и конфигурацией Java.

```csharp
public class JavaManager
{
    private readonly IDialogService _dialog;
    private const string JavaDataKey = "java_installations";

    public JavaManager(IDialogService dialog)
    {
        _dialog = dialog;
    }

    // Проверить, установлена ли Java
    public static bool IsJavaInstalled()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    // Получить версию Java
    public static string? GetJavaVersion()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardError.ReadLine();
            process.WaitForExit();

            // Парсировать версию из вывода
            // Пример: "java version "11.0.15" 2022-04-19 LTS"
            var match = Regex.Match(output, @"version ""([^""]+)""");
            return match.Success ? match.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
    }

    // Установить Java
    public async Task<Result<bool>> InstallJavaAsync(
        string version,
        string installPath)
    {
        try
        {
            // Скачать Java
            var downloadUrl = $"https://api.adoptopenjdk.net/v3/binary/latest/" +
                            $"{version}/ga/windows/x64/jdk/hotspot/normal/adoptopenjdk";

            // Загрузить и установить
            var installer = Path.Combine(installPath, $"java-{version}-installer.msi");

            // Проверить путь
            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }

            // Скачать установщик
            // using (var client = new HttpClient())
            // {
            //     var content = await client.GetByteArrayAsync(downloadUrl);
            //     File.WriteAllBytes(installer, content);
            // }

            // Запустить установку
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "msiexec",
                    Arguments = $"/i \"{installer}\" /quiet",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await Task.Run(() => process.WaitForExit());

            if (process.ExitCode == 0)
            {
                SaveJavaInstallation(version, installPath);
                return Result<bool>.Success(true);
            }

            return Result<bool>.Failure(
                InstallerError.InstallationFailed,
                "Ошибка при установке Java");
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(
                InstallerError.Unknown,
                ex.Message);
        }
    }

    // Получить параметры памяти для Java
    public JavaMemorySettings GetMemorySettings()
    {
        // Получить доступную память
        var totalMemory = GC.GetTotalMemory(false) / (1024 * 1024); // В МБ

        return new JavaMemorySettings
        {
            MinMemory = 512,           // Минимум 512 МБ
            MaxMemory = totalMemory / 2, // До половины доступной памяти
            Recommended = totalMemory / 3 // Рекомендуемо 1/3
        };
    }

    private void SaveJavaInstallation(string version, string installPath)
    {
        var installations = GetInstalledVersions();
        installations.Add(new JavaInstallation 
        { 
            Version = version, 
            InstallPath = installPath,
            InstalledDate = DateTime.Now
        });
        // Сохранить...
    }

    public List<JavaInstallation> GetInstalledVersions()
    {
        // Загрузить из локального хранилища
        return new List<JavaInstallation>();
    }
}

public class JavaInstallation
{
    public string Version { get; set; }
    public string InstallPath { get; set; }
    public DateTime InstalledDate { get; set; }
}

public class JavaMemorySettings
{
    public int MinMemory { get; set; }  // МБ
    public int MaxMemory { get; set; }  // МБ
    public int Recommended { get; set; } // МБ
}
```

## 📦 ModPackManager

**Файл**: `Services/Installers/ModPackManager.cs`

Управляет установкой и удалением модификационных пакетов.

```csharp
public class ModPackManager
{
    private readonly BaseApiService _apiService;
    private readonly ILocalDataService _localStorage;
    private const string ModPacksDataKey = "installed_modpacks";

    public ModPackManager(
        BaseApiService apiService,
        ILocalDataService localStorage)
    {
        _apiService = apiService;
        _localStorage = localStorage;
    }

    // Получить список доступных модпаков
    public async Task<Result<List<ModPack>>> GetAvailableModPacksAsync()
    {
        var result = await _apiService.GetAsync<List<ModPack>>(
            "api/modpacks");

        return result;
    }

    // Получить детали модпака
    public async Task<Result<ModPackDetails>> GetModPackDetailsAsync(
        string modpackId)
    {
        var result = await _apiService.GetAsync<ModPackDetails>(
            $"api/modpacks/{modpackId}");

        return result;
    }

    // Установить модпак
    public async Task<Result<bool>> InstallModPackAsync(
        string modpackId,
        string installPath)
    {
        try
        {
            // Проверить путь
            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }

            // Получить информацию о модпаке
            var detailsResult = await GetModPackDetailsAsync(modpackId);
            if (!detailsResult.IsSuccess)
            {
                return Result<bool>.Failure(
                    InstallerError.NotFound,
                    "Модпак не найден");
            }

            var details = detailsResult.Data;

            // Скачать файлы модпака
            var downloadProgress = 0.0;
            foreach (var mod in details.Mods)
            {
                await DownloadFileAsync(mod.DownloadUrl, installPath);
                downloadProgress += 1.0 / details.Mods.Count;
            }

            // Сохранить информацию об установке
            SaveModPackInstallation(modpackId, installPath, details);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(
                InstallerError.Unknown,
                ex.Message);
        }
    }

    // Удалить модпак
    public async Task<Result<bool>> UninstallModPackAsync(string modpackId)
    {
        try
        {
            var installations = GetInstalledModPacks();
            var installation = installations.FirstOrDefault(i => i.ModPackId == modpackId);

            if (installation == null)
            {
                return Result<bool>.Failure(
                    InstallerError.NotFound,
                    "Модпак не установлен");
            }

            // Удалить файлы
            if (Directory.Exists(installation.InstallPath))
            {
                Directory.Delete(installation.InstallPath, true);
            }

            // Обновить список
            installations.Remove(installation);
            SaveModPackInstallations(installations);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(
                InstallerError.Unknown,
                ex.Message);
        }
    }

    // Получить установленные модпаки
    public List<ModPackInstallation> GetInstalledModPacks()
    {
        return _localStorage.LoadData<List<ModPackInstallation>>(
            ModPacksDataKey) ?? new List<ModPackInstallation>();
    }

    // Обновить модпак
    public async Task<Result<bool>> UpdateModPackAsync(string modpackId)
    {
        // Получить текущую установку
        var current = GetInstalledModPacks()
            .FirstOrDefault(m => m.ModPackId == modpackId);

        if (current == null)
        {
            return Result<bool>.Failure(
                InstallerError.NotFound,
                "Модпак не установлен");
        }

        // Удалить старую версию
        await UninstallModPackAsync(modpackId);

        // Установить новую версию
        return await InstallModPackAsync(modpackId, current.InstallPath);
    }

    private async Task DownloadFileAsync(string url, string targetPath)
    {
        using (var client = new HttpClient())
        {
            var fileName = Path.GetFileName(url);
            var filePath = Path.Combine(targetPath, fileName);

            var response = await client.GetAsync(url);
            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                await response.Content.CopyToAsync(fs);
            }
        }
    }

    private void SaveModPackInstallation(
        string modpackId,
        string installPath,
        ModPackDetails details)
    {
        var installations = GetInstalledModPacks();
        installations.Add(new ModPackInstallation
        {
            ModPackId = modpackId,
            Name = details.Name,
            Version = details.Version,
            InstallPath = installPath,
            InstalledDate = DateTime.Now
        });
        SaveModPackInstallations(installations);
    }

    private void SaveModPackInstallations(List<ModPackInstallation> installations)
    {
        _localStorage.SaveData(ModPacksDataKey, installations);
    }
}

public class ModPack
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Version { get; set; }
    public int Downloads { get; set; }
}

public class ModPackDetails : ModPack
{
    public List<Mod> Mods { get; set; } = new();
    public string Author { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class Mod
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string DownloadUrl { get; set; }
    public long FileSize { get; set; }
}

public class ModPackInstallation
{
    public string ModPackId { get; set; }
    public string Name { get; set; }
    public string Version { get; set; }
    public string InstallPath { get; set; }
    public DateTime InstalledDate { get; set; }
}
```

## ⚙️ Регистрация установщиков в DI

**Файл**: `App.xaml.cs`

```csharp
services.AddSingleton<ForgeManager>();
services.AddSingleton<JavaManager>();
services.AddSingleton<ModPackManager>();
```

## 🔄 Использование в ViewModels

```csharp
public partial class InstallersViewModel : ObservableObject
{
    private readonly ForgeManager _forgeManager;
    private readonly JavaManager _javaManager;
    private readonly ModPackManager _modPackManager;

    [ObservableProperty]
    private string installationStatus = "Готово";

    [RelayCommand]
    private async Task InstallForge()
    {
        InstallationStatus = "Установка Forge...";

        var result = await _forgeManager.InstallForgeAsync(
            "1.19.2",
            @"C:\Games\Minecraft\Forge");

        if (result.IsSuccess)
        {
            InstallationStatus = "Forge успешно установлен";
        }
        else
        {
            InstallationStatus = $"Ошибка: {result.ErrorMessage}";
        }
    }

    [RelayCommand]
    private async Task CheckJava()
    {
        if (JavaManager.IsJavaInstalled())
        {
            var version = JavaManager.GetJavaVersion();
            InstallationStatus = $"Java {version} установлена";
        }
        else
        {
            InstallationStatus = "Java не установлена";
        }
    }

    [RelayCommand]
    private async Task InstallModPack()
    {
        InstallationStatus = "Загрузка модпака...";

        var result = await _modPackManager.InstallModPackAsync(
            "modpack-123",
            @"C:\Games\Minecraft\ModPacks\MyModPack");

        if (result.IsSuccess)
        {
            InstallationStatus = "Модпак успешно установлен";
        }
    }
}
```

## 📊 Типичные сценарии использования

### Сценарий 1: Установка новой игровой сборки

```
1. Проверить Java → JavaManager.IsJavaInstalled()
2. Если Java не установлена → JavaManager.InstallJavaAsync()
3. Установить Forge → ForgeManager.InstallForgeAsync()
4. Установить модпак → ModPackManager.InstallModPackAsync()
5. Показать успешное сообщение
```

### Сценарий 2: Обновление модпака

```
1. Показать список установленных модпаков
2. Пользователь выбирает модпак
3. Показать доступные обновления
4. ModPackManager.UpdateModPackAsync()
5. Показать результат
```

## 🔗 Связанные документы

- [Сервисы и инфраструктура](./SERVICES.md)
- [Архитектура](./ARCHITECTURE.md)

---

**Примечание**: Полная функциональность установщиков требует наличия API сервера для предоставления информации о версиях, скачиваний и конфигурации.
