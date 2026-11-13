using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Windows.Foundation.Diagnostics;
using ZLogger;

namespace Danmaku.Core;

internal static class Logger
{
    private static ILogger<DanmakuRenderer>? _logger;

    public static void SetLogger(ILogger<DanmakuRenderer>? logger)
    {
        _logger = logger;
    }

    public static void Log(
        string message,
        LoggingLevel level = LoggingLevel.Information,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? filePath = null,
        [CallerLineNumber] int lineNumber = 0
    )
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            filePath = Path.GetFileName(filePath);
        }

        message =
            $"[Danmaku] {filePath ?? string.Empty}({lineNumber})->{memberName ?? string.Empty}(): {message ?? string.Empty}";

#if DEBUG
        System.Diagnostics.Debug.WriteLine(message);
#endif

        if (_logger is { } logger)
        {
            if (level == LoggingLevel.Error)
            {
                logger.ZLogError($"{message}");
            }
            else
            {
                logger.ZLogInformation($"{message}");
            }
        }
    }
}
