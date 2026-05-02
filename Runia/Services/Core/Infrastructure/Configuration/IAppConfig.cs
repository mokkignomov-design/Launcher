using Runia.Domain.Models;
using Runia.Services.Core.Launcher;

namespace Runia.Services.Core.Infrastructure.Configuration
{
    /// <summary>
    /// IAppConfig управляет конфигурацией приложения.
    /// 
    /// КРИТИЧЕСКИ ВАЖНО: Этот интерфейс НЕ должен содержать методы для сохранения/изменения Username, AccessToken или RefreshToken.
    /// Эти данные управляются ТОЛЬКО через AuthService и защищенное хранилище (ITokenStore).
    /// Это предотвращает попытки подделки аутентификационных данных на уровне конфигурации.
    /// </summary>
    public interface IAppConfig
    {
        string ApiBaseUrl { get; }
        Task<List<ServerPack>> GetServersAsync();
        int RamAmount { get; }
        bool ShowConsole { get; }
        bool IsFullscreen { get; }
        string JvmArguments { get; }
        string GameRootPath { get; }
        VersionsManager Versions { get; }
    }
}
