using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore.Extensions;

public static class ExceptionExtensions
{
    /// <summary>
    /// Gets detailed exception information.
    /// </summary>
    /// <param name="ex">The exception to parse.</param>
    /// <returns>The detailed exception information.</returns>
    public static string GetDetailedExceptionInfo(this Exception ex)
    {
        var exceptionStringBuilder = new StringBuilder();

        GetExceptionInfo(ex, exceptionStringBuilder);

        return exceptionStringBuilder.ToString();
    }

    /// <summary>
    /// Gets user friendly exception information.
    /// </summary>
    /// <param name="ex">The exception to parse.</param>
    /// <returns>The user friendly exception information.</returns>
    public static string GetUserFriendlyExceptionInfo(this Exception ex)
    {
        var exceptionStringBuilder = new StringBuilder();

        GetExceptionInfo(ex, exceptionStringBuilder, false);

        return exceptionStringBuilder.ToString();
    }

    public static async ValueTask<string> GetHttpResponseMessageInfoAsync(this HttpResponseMessage httpResponseMessage)
    {
        if (httpResponseMessage is null)
            return null;

        var sb = new StringBuilder();
        string content = await httpResponseMessage.Content.ReadAsStringAsync();

        sb.Append(FormattableString.Invariant($"{nameof(HttpResponseMessage)}: {httpResponseMessage}"));
        sb.AppendLine(FormattableString.Invariant($"{nameof(HttpResponseMessage)}.{nameof(HttpResponseMessage.Content)}: {content}"));

        return sb.ToString();
    }

    private static void GetExceptionInfo(Exception ex, StringBuilder sb, bool includeDetails = true)
    {
        sb.AppendLine(FormattableString.Invariant($"{nameof(Exception)}.{nameof(Exception.GetType)}: {ex.GetType()}"));
        sb.AppendLine(FormattableString.Invariant($"{nameof(Exception)}.{nameof(Exception.Message)}: {ex.Message}"));

        if (includeDetails)
            GetExceptionDetails(ex, sb);

        if (ex is AggregateException aggregateException)
        {
            foreach (Exception innerException in aggregateException.InnerExceptions)
            {
                sb.AppendLine(FormattableString.Invariant($"{nameof(AggregateException)}.{nameof(AggregateException.InnerExceptions)}:"));
                GetExceptionInfo(innerException, sb, includeDetails);
            }
        }
        else if (ex.InnerException is not null)
        {
            sb.AppendLine(FormattableString.Invariant($"{nameof(Exception)}.{nameof(Exception.InnerException)}:"));
            GetExceptionInfo(ex.InnerException, sb, includeDetails);
        }
    }

    private static void GetExceptionDetails(Exception ex, StringBuilder sb)
    {
        sb.AppendLine(FormattableString.Invariant($"{nameof(Exception)}.{nameof(Exception.Source)}: {ex.Source}"));
        sb.AppendLine(FormattableString.Invariant($"{nameof(Exception)}.{nameof(Exception.TargetSite)}: {ex.TargetSite}"));
        GetFileLoadExceptionDetails(ex, sb);
        GetHttpRequestExceptionDetails(ex, sb);
        GetSocketExceptionDetails(ex, sb);
        GetExternalExceptionDetails(ex, sb);
        sb.AppendLine(FormattableString.Invariant($"{nameof(Exception)}.{nameof(Exception.StackTrace)}: {ex.StackTrace}"));
    }

    private static void GetExternalExceptionDetails(Exception ex, StringBuilder sb)
    {
        if (ex is not ExternalException externalException)
            return;

        var win32Exception = new Win32Exception(externalException.ErrorCode);

        sb.AppendLine(FormattableString.Invariant($"{nameof(ExternalException)}.{nameof(ExternalException.ErrorCode)}: {externalException.ErrorCode}"));
        sb.AppendLine(FormattableString.Invariant($"{nameof(ExternalException)}.{nameof(ExternalException.ErrorCode)} Hex: 0x{externalException.ErrorCode:X8}"));
        sb.AppendLine(FormattableString.Invariant($"{nameof(Win32Exception)}.{nameof(Exception.Message)}: {win32Exception.Message}"));
    }

    private static void GetSocketExceptionDetails(Exception ex, StringBuilder sb)
    {
        if (ex is SocketException socketException)
            sb.AppendLine(FormattableString.Invariant($"{nameof(SocketException)}.{nameof(SocketException.SocketErrorCode)}: {socketException.SocketErrorCode}"));
    }

    private static void GetHttpRequestExceptionDetails(Exception ex, StringBuilder sb)
    {
        if (ex is not HttpRequestException httpRequestException)
            return;

        sb.AppendLine(FormattableString.Invariant($"{nameof(HttpRequestException)}.{nameof(HttpRequestException.StatusCode)}: {httpRequestException.StatusCode}"));
    }

    private static void GetFileLoadExceptionDetails(Exception ex, StringBuilder sb)
    {
        if (ex is not FileLoadException fileLoadException)
            return;

        sb.AppendLine(FormattableString.Invariant($"{nameof(FileLoadException)}.{nameof(FileLoadException.FileName)}: {fileLoadException.FileName}"));
        sb.AppendLine(FormattableString.Invariant($"{nameof(FileLoadException)}.{nameof(FileLoadException.FusionLog)}: {fileLoadException.FusionLog}"));
    }
}