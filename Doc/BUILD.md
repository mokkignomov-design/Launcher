# Руководство по сборке и релизу Runia Launcher

## Содержание
1. [Установка зависимостей](#установка-зависимостей)
2. [Локальная сборка](#локальная-сборка)
3. [Управление версией](#управление-версией)
4. [Подготовка к релизу](#подготовка-к-релизу)
5. [Публикация на GitHub](#публикация-на-github)
6. [Структура обновлений (Velopack)](#структура-обновлений-velopack)
7. [Помощь и поддержка](#помощь-и-поддержка)

---

## Установка зависимостей

### Требования
- **.NET 9 SDK** ([скачать](https://dotnet.microsoft.com/download/dotnet/9.0))
- **Visual Studio 2022+** или **VS Code** + .NET CLI
- **Git** для управления версиями

### Проверка установки
```powershell
dotnet --version   # должен быть 9.0.x
git --version
```

### Восстановление NuGet пакетов
```powershell
cd C:\Users\Uran\source\repos\Runia
dotnet restore
```

---

## Локальная сборка

### Debug сборка (для разработки)
```powershell
dotnet build --configuration Debug
# или через Visual Studio: Ctrl+Shift+B
```

### Release сборка (для продакшена)
```powershell
dotnet build --configuration Release
```

### Публикование автозапускаемого .exe
```powershell
# SelfContained - приложение включает .NET runtime (размер ~100+ MB)
dotnet publish Runia/Runia.csproj `
  -c Release `
  -r win-x64 `
  -p:PublishSingleFile=true `
  -p:SelfContained=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  --output bin/release/publish

# Результат: bin/release/publish/Runia.exe (~150-200 MB)
```

---

## Управление версией

### Текущая версия
Версия приложения хранится в **Runia/Runia.csproj**:
```xml
<Version>1.0.0-beta.1</Version>
```

### Правила версионирования (Semantic Versioning)
- **MAJOR.MINOR.PATCH**
  - `MAJOR` — несовместимые изменения
  - `MINOR` — новые функции (совместимо)
  - `PATCH` — исправления (совместимо)
- **Предвыпуски**: `1.0.0-beta.1`, `1.0.0-rc.1`
- **Стабильные**: `1.0.0`, `2.0.1`

### Обновление версии
1. Отредактируйте **Runia/Runia.csproj**:
   ```xml
   <Version>1.0.0</Version>  <!-- изменить здесь -->
   ```

2. Также обновите **Updater/Updater.csproj** (для консистентности):
   ```xml
   <Version>1.0.0</Version>
   ```

3. Обновите **Doc/RELEASE_NOTES.md** с описанием изменений

4. Закоммитьте:
   ```powershell
   git add -A
   git commit -m "chore: bump version to 1.0.0"
   git push origin main
   ```

---

## Подготовка к релизу

### Шаг 1: Обновите RELEASE_NOTES.md
Файл: **Doc/RELEASE_NOTES.md**

Пример:
```markdown
# Runia Launcher v1.0.0 (Stable)

## Что нового
- ✨ Новая система установки модов
- 🐛 Исправлена проблема с авторизацией
- 📈 Улучшена производительность загрузки файлов

## Известные проблемы
- Windows 7 может потребовать обновление .NET Framework

## Установка
1. Скачайте `Runia.exe` из релиза
2. Запустите и следуйте инструкциям
```

### Шаг 2: Проверьте сборку
```powershell
# Полная сборка + тесты
dotnet build --configuration Release
# dotnet test (если есть тесты)
```

### Шаг 3: Создайте артефакты для релиза
```powershell
# Создаём директорию для релиза
$version = "1.0.0"
$releaseDir = "Release-$version"
New-Item -ItemType Directory -Path $releaseDir -Force

# Публикуем приложение
dotnet publish Runia/Runia.csproj `
  -c Release `
  -r win-x64 `
  -p:PublishSingleFile=true `
  -p:SelfContained=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  --output $releaseDir/app

# Копируем Updater
dotnet publish Updater/Updater.csproj `
  -c Release `
  -r win-x64 `
  -p:PublishSingleFile=true `
  --output $releaseDir

# Копируем документацию
Copy-Item "Doc/RELEASE_NOTES.md" -Destination "$releaseDir/"
Copy-Item "THIRD_PARTY_LICENSES.txt" -Destination "$releaseDir/"

# Создаём архив
Compress-Archive -Path "$releaseDir/*" -DestinationPath "Runia-$version.zip" -Force
```

### Результат
- ✅ `Release-1.0.0/` — распакованная сборка
- ✅ `Release-1.0.0/app/Runia.exe` — основное приложение
- ✅ `Release-1.0.0/Updater.exe` — утилита обновления
- ✅ `Runia-1.0.0.zip` — архив для раздачи

---

## Публикация на GitHub

### Шаг 1: Теги Git
Создаёте тег для версии:
```powershell
$version = "1.0.0"
git tag -a "v$version" -m "Release version $version"
git push origin --tags
```

### Шаг 2: GitHub Release (вручную через веб)
1. Откройте [github.com/mokkignomov-design/Launcher](https://github.com/mokkignomov-design/Launcher)
2. Перейдите в **Releases** → **Draft a new release**
3. Укажите:
   - **Tag**: `v1.0.0`
   - **Release title**: `Runia Launcher v1.0.0 (Stable)`
   - **Description**: содержимое `Doc/RELEASE_NOTES.md`
4. Загрузите артефакты:
   - `Runia-1.0.0.zip` (основной релиз)
   - `Release-1.0.0/app/Runia.exe` (отдельно для быстрого скачивания)
   - `Release-1.0.0/Updater.exe` (отдельно)
5. ✅ Нажмите **Publish release**

### Шаг 3: Автоматизация через GitHub Actions (опционально)

Создаёте файл **.github/workflows/release.yml**:
```yaml
name: Build and Release

on:
  push:
	tags:
	  - 'v*'

jobs:
  release:
	runs-on: windows-latest
	steps:
	  - uses: actions/checkout@v3

	  - name: Setup .NET
		uses: actions/setup-dotnet@v3
		with:
		  dotnet-version: '9.0.x'

	  - name: Restore dependencies
		run: dotnet restore

	  - name: Build
		run: dotnet build --configuration Release

	  - name: Publish artifacts
		run: |
		  dotnet publish Runia/Runia.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true --output release/app
		  dotnet publish Updater/Updater.csproj -c Release -r win-x64 -p:PublishSingleFile=true --output release
		  Copy-Item "Doc/RELEASE_NOTES.md" -Destination "release/"
		  Copy-Item "THIRD_PARTY_LICENSES.txt" -Destination "release/"

	  - name: Create Archive
		run: Compress-Archive -Path "release/*" -DestinationPath "Runia-${{ github.ref_name }}.zip"

	  - name: Create Release
		uses: actions/create-release@v1
		env:
		  GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
		with:
		  tag_name: ${{ github.ref_name }}
		  release_name: Runia Launcher ${{ github.ref_name }}
		  body_path: Doc/RELEASE_NOTES.md
		  draft: false
		  prerelease: ${{ contains(github.ref_name, 'beta') || contains(github.ref_name, 'rc') }}

	  - name: Upload Release Asset (ZIP)
		uses: actions/upload-release-asset@v1
		env:
		  GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
		with:
		  upload_url: ${{ steps.create_release.outputs.upload_url }}
		  asset_path: ./Runia-${{ github.ref_name }}.zip
		  asset_name: Runia-${{ github.ref_name }}.zip
		  asset_content_type: application/zip
```

---

## Структура обновлений (Velopack)

### Как работают обновления

1. **При запуске лаунчера** (App.xaml.cs):
   - Стартует фоновый поток `CheckForUpdatesAsync()`
   - Запускает `Updater.exe` без параметров

2. **Updater проверяет GitHub Releases**:
   - Запрашивает API: `https://api.github.com/repos/mokkignomov-design/Launcher/releases/latest`
   - Сравнивает версии (текущая vs последняя)
   - Если новая версия → скачивает архив обновления

3. **При следующем запуске**:
   - Velopack распаковывает и применяет обновление
   - Перезапускает приложение новой версией

### Структура релиза для обновлений

Каждый релиз на GitHub должен содержать:

```
Runia-1.0.0/
├── Runia.exe          ← основной .exe (собран с --PublishSingleFile)
├── Updater.exe        ← утилита обновления
├── RELEASE_NOTES.md   ← описание релиза
└── THIRD_PARTY_LICENSES.txt
```

### Пример latest.json (опционально, если используете свой сервер)
```json
{
  "channel": "stable",
  "releases": [
	{
	  "version": "1.0.0",
	  "releaseDate": "2024-12-01",
	  "url": "https://github.com/mokkignomov-design/Launcher/releases/download/v1.0.0/Runia.exe",
	  "mimeType": "application/octet-stream",
	  "size": 157000000,
	  "delta": null
	}
  ]
}
```

---

## Помощь и поддержка

### Типичные проблемы

#### **Сборка падает с ошибкой "target framework not installed"**
```powershell
dotnet --list-sdks  # проверьте наличие 9.0.x
# если нет → скачайте и установите .NET 9 SDK
```

#### **Velopack выдаёт ошибку при запуске**
- Убедитесь, что приложение опубликовано с `--PublishSingleFile`
- Проверьте права доступа к папке приложения

#### **Обновления не применяются**
1. Проверьте, что GitHub репозиторий публичный
2. Убедитесь, что релизы помечены тегами (`v1.0.0`)
3. Посмотрите логи: `%APPDATA%/Runia/log.txt`

### Разработчикам

Если нужно отладить Updater локально:
```powershell
# Запуск Updater в консоли
./Updater.exe
# Или применение обновления из файла
./Updater.exe apply "path/to/update" "path/to/Runia.exe"
```

### Контакты и ссылки
- GitHub: [mokkignomov-design/Launcher](https://github.com/mokkignomov-design/Launcher)
- Releases: [github.com/.../releases](https://github.com/mokkignomov-design/Launcher/releases)
- Issues: [github.com/.../issues](https://github.com/mokkignomov-design/Launcher/issues)

---

**Последнее обновление**: December 2024  
**Версия документа**: 1.0
