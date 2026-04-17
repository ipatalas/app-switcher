using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AppSwitcher.Extensions;

public static class LoggerExtensions
{
    public static IDisposable MeasureTime(this ILogger logger, string operationName)
    {
        return new TimingScope(logger, operationName);
    }
}

file class TimingScope(ILogger logger, string operationName) : IDisposable
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public void Dispose()
    {
        logger.LogDebug("{OperationName} completed in {ElapsedMilliseconds} ms", operationName, _stopwatch.ElapsedMilliseconds);
    }
}