
namespace Runia.Services.Core.Infrastructure.Network
{
    public interface ITokenStore
    {
        Task<(string? AccessToken, string? RefreshToken, string? Username)> GetTokensAsync();
        Task SaveTokensAsync(string? accessToken, string? refreshToken, bool remember, string? username = null);
        Task ClearAsync();
    }
}
