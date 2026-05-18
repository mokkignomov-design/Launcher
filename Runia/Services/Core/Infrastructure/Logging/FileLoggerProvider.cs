using Microsoft.Extensions.Logging;
using System;

namespace Runia.Services.Core.Infrastructure.Logging;

internal class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    private readonly object _fileLock = new object();

    public FileLoggerProvider(string filePath)
    {
        _filePath = filePath;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _filePath, _fileLock);
    }

    public void Dispose()
    {
        // nothing to dispose
    }
}
