using Microsoft.Extensions.Logging;
using System;

namespace Runia.Services.Core.Infrastructure.Logging;

internal class FileLoggerProvider(string filePath) : ILoggerProvider
{
    private readonly string _filePath = filePath;
    private readonly object _fileLock = new();

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _filePath, _fileLock);
    }

    public void Dispose()
    {
        // nothing to dispose
    }
}
