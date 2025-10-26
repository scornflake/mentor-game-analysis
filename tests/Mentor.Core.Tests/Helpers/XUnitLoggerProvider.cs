using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Mentor.Core.Tests.Helpers;

/// <summary>
/// Logger provider that bridges Microsoft.Extensions.Logging to xUnit ITestOutputHelper.
/// </summary>
public class XUnitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly LogLevel _minLevel;

    public XUnitLoggerProvider(ITestOutputHelper testOutputHelper, LogLevel minLevel = LogLevel.Debug)
    {
        _testOutputHelper = testOutputHelper;
        _minLevel = minLevel;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new XUnitLogger(_testOutputHelper, categoryName, _minLevel);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}

/// <summary>
/// Logger implementation that writes to xUnit ITestOutputHelper.
/// </summary>
public class XUnitLogger : ILogger
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string _categoryName;
    private readonly LogLevel _minLevel;

    public XUnitLogger(ITestOutputHelper testOutputHelper, string categoryName, LogLevel minLevel)
    {
        _testOutputHelper = testOutputHelper;
        _categoryName = categoryName;
        _minLevel = minLevel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= _minLevel;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        var logLevelString = logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT",
            _ => logLevel.ToString()
        };

        var logLine = $"[{logLevelString}] {_categoryName}: {message}";
        
        try
        {
            _testOutputHelper.WriteLine(logLine);
            
            if (exception != null)
            {
                _testOutputHelper.WriteLine($"Exception: {exception}");
            }
        }
        catch
        {
            // ITestOutputHelper can throw if called after test completion
            // Silently ignore these cases
        }
    }
}

