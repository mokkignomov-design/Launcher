using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;

namespace Runia.Services.Core.Infrastructure.Logging;

internal class FileLogger : ILogger, IDisposable
{
    private readonly string _category;
    private readonly string _filePath;
    private readonly object _fileLock;

    public FileLogger(string category, string filePath, object fileLock)
    {
        _category = category;
        _filePath = filePath;
        _fileLock = fileLock;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            var message = formatter(state, exception);
            var sb = new StringBuilder();
            sb.Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            sb.Append(' ');
            sb.Append(logLevel.ToString());
            sb.Append(' ');
            sb.Append(_category);
            sb.Append(' ');
            sb.Append(message);
            if (exception != null)
            {
                sb.AppendLine();
                sb.Append(exception);
            }
            sb.AppendLine();

            var text = sb.ToString();

            lock (_fileLock)
            {
                // Ensure directory exists
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.AppendAllText(_filePath, text, Encoding.UTF8);
            }
        }
        catch
        {
            // Swallow any logging exceptions to avoid crashing app
        }
    }

    public void Dispose()
    {
        // nothing to dispose
    }
}
