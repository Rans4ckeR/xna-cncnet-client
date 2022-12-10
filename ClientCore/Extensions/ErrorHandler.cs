using System;
using System.IO;
using Localization;
using Microsoft.Extensions.Logging;
using Rampastring.Tools;

namespace ClientCore.Extensions;

public sealed class ErrorHandler
{
    public ErrorHandler(ILogger logger)
    {
        Logger = logger;
        Instance = this;
    }

    private ILogger Logger { get; }

    public static ErrorHandler Instance { get; private set; }

    /// <summary>
    /// Gets or sets the action to perform to notify the user of an error.
    /// </summary>
    public Action<string, string, bool> DisplayErrorAction { get; set; } = (title, error, exit) =>
    {
        Instance.Logger.LogError(FormattableString.Invariant($"{(title is null ? null : title + Environment.NewLine + Environment.NewLine)}{error}"));
        ProcessLauncher.StartShellProcess(ProgramConstants.LogFileName);

        if (exit)
            Environment.Exit(1);
    };

    /// <summary>
    /// Logs all details of an exception to the logfile, notifies the user, and exits the application.
    /// </summary>
    /// <param name="ex">The <see cref="Exception"/> to log.</param>
    public void HandleException(Exception ex)
    {
        Logger.LogExceptionDetails(ex, "KABOOOOOOM!!! Info:");

        string errorLogPath = SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "ClientCrashLogs", FormattableString.Invariant($"ClientCrashLog{DateTime.Now.ToString("_yyyy_MM_dd_HH_mm")}.txt"));
        bool crashLogCopied = false;

        try
        {
            DirectoryInfo crashLogsDirectoryInfo = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "ClientCrashLogs");

            if (!crashLogsDirectoryInfo.Exists)
                crashLogsDirectoryInfo.Create();

            File.Copy(SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "client.log"), errorLogPath, true);
            crashLogCopied = true;
        }
        catch
        {
            // ignored
        }

        string error = string.Format("{0} has crashed. Error message:".L10N("UI:Main:FatalErrorText1") + Environment.NewLine + Environment.NewLine +
            ex.Message + Environment.NewLine + Environment.NewLine + (crashLogCopied ?
            "A crash log has been saved to the following file:".L10N("UI:Main:FatalErrorText2") + " " + Environment.NewLine + Environment.NewLine +
            errorLogPath + Environment.NewLine + Environment.NewLine : "") +
            (crashLogCopied ? "If the issue is repeatable, contact the {1} staff at {2} and provide the crash log file.".L10N("UI:Main:FatalErrorText3") :
            "If the issue is repeatable, contact the {1} staff at {2}.".L10N("UI:Main:FatalErrorText4")),
            ProgramConstants.GAME_NAME_LONG,
            ProgramConstants.GAME_NAME_SHORT,
            ProgramConstants.SUPPORT_URL_SHORT);

        DisplayErrorAction("KABOOOOOOOM".L10N("UI:Main:FatalErrorTitle"), error, true);
    }
}