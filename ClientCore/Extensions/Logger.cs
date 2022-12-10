using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClientCore.Extensions;

public sealed class Logger : ILogger
{
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
        => null;

    public bool IsEnabled(LogLevel logLevel)
#if DEBUG
        => logLevel >= LogLevel.Trace;
#else
        => logLevel >= LogLevel.Information;
#endif

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        => Task.Run(() => LogInternal(logLevel, eventId, state, exception, formatter));

    private void LogInternal<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        string message = formatter(state, exception);
        string categoryCode = Enum.GetName(typeof(LogLevel), logLevel);

        Task.Run(() =>
        {
            try
            {
                Rampastring.Tools.Logger.Log(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(FormattableString.Invariant($"Could not log {nameof(eventId)}: '{eventId}', {nameof(message)}: '{message}', {nameof(categoryCode)}: '{categoryCode}', {Environment.NewLine}Exception from logger: '{ex.GetDetailedExceptionInfo()}'"));
            }
        });
    }
}