using System.IO;
using System.Text.Json;
using System.Diagnostics;

namespace Runia.Services.Core.Infrastructure
{
    public interface ILocalDataService
    {
        Task<T?> LoadFromJsonAsync<T>(string relativePath) where T : class;
        Task SaveJsonAsync<T>(string relativePath, T obj);
    }

    public class LocalDataService : ILocalDataService
    {
        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        public async Task<T?> LoadFromJsonAsync<T>(string relativePath) where T : class
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, relativePath);
                if (!File.Exists(path))
                {
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
                    if (!File.Exists(path)) return null;
                }

                var text = await File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<T>(text, s_jsonOptions);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LocalDataService] LoadFromJsonAsync error: {ex}");
                return null;
            }
        }

        public async Task SaveJsonAsync<T>(string relativePath, T obj)
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, relativePath);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(obj, s_jsonOptions);
                await File.WriteAllTextAsync(path, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LocalDataService] SaveJsonAsync error: {ex}");
            }
        }
    }
}
