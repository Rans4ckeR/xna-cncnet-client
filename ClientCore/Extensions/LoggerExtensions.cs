using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ClientCore.Extensions;

public static class LoggerExtensions
{
    private static readonly Action<ILogger, string, Exception> ExceptionDetails = LoggerMessage.Define<string>(LogLevel.Error, new(1, nameof(LogExceptionDetails)), "{Exception}");

    public static void LogExceptionDetails(this ILogger logger, Exception exception, string message = null)
    {
        string formattedString = message is null
            ? FormattableString.Invariant($"{exception.GetDetailedExceptionInfo()}")
            : FormattableString.Invariant($"{message}{Environment.NewLine}{Environment.NewLine}{exception.GetDetailedExceptionInfo()}");

        ExceptionDetails(logger, formattedString, null);
    }

    public static async ValueTask LogExceptionDetailsAsync(this ILogger logger, Exception exception, string message = null, HttpResponseMessage httpResponseMessage = null)
    {
        string formattedString = message is null
            ? FormattableString.Invariant($"{exception.GetDetailedExceptionInfo()}")
            : FormattableString.Invariant($"{message}{Environment.NewLine}{Environment.NewLine}{exception.GetDetailedExceptionInfo()}");

        ExceptionDetails(logger, formattedString, null);

        if (httpResponseMessage is not null)
            ExceptionDetails(logger, await httpResponseMessage.GetHttpResponseMessageInfoAsync(), null);
    }
}