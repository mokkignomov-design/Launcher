using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Runia.Services.Core.Infrastructure.Network
{
   
    public class SecureTokenStore : ITokenStore
    {
        private readonly string _filePath;

        public SecureTokenStore(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentNullException(nameof(directory));
            Directory.CreateDirectory(directory);
            _filePath = Path.Combine(directory, "tokens.sec");
        }

        public Task ClearAsync()
        {
            try
            {
                if (File.Exists(_filePath)) File.Delete(_filePath);
            }
            catch { }
            return Task.CompletedTask;
        }

        public async Task<(string? AccessToken, string? RefreshToken, string? Username)> GetTokensAsync()
        {
            try
            {
                if (!File.Exists(_filePath)) return (null, null, null);

                var bytes = await File.ReadAllBytesAsync(_filePath);
                var decryptedBytes = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(decryptedBytes);

                var doc = JsonSerializer.Deserialize<TokenDto>(json);
                if (doc == null || !doc.Remember) return (null, null, null);

                return (doc.AccessToken, doc.RefreshToken, doc.Username);
            }
            catch
            {
                return (null, null, null);
            }
        }

        public async Task SaveTokensAsync(string? accessToken, string? refreshToken, bool remember, string? username = null)
        {
            try
            {
                var dto = new TokenDto { AccessToken = accessToken, RefreshToken = refreshToken, Remember = remember, Username = username };
                var json = JsonSerializer.Serialize(dto);
                var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(json), null, DataProtectionScope.CurrentUser);
                await File.WriteAllBytesAsync(_filePath, bytes);
            }
            catch { }
        }

        private class TokenDto { public string? AccessToken { get; set; } public string? RefreshToken { get; set; } public bool Remember { get; set; } public string? Username { get; set; } }
    }
}
