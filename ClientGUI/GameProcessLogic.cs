using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using ClientCore;
using ClientCore.INIProcessing;
using Rampastring.Tools;

namespace ClientGUI;

/// <summary>
/// A static class used for controlling the launching and exiting of the game executable.
/// </summary>
public static class GameProcessLogic
{
    public static event Action GameProcessStarted;

    public static event Action GameProcessStarting;

    public static event Action GameProcessExited;

    public static bool UseQres { get; set; }

    public static bool SingleCoreAffinity { get; set; }

    /// <summary>
    /// Starts the main game process.
    /// </summary>
    public static void StartGameProcess()
    {
        Logger.Log("About to launch main game executable.");

        // In the relatively unlikely event that INI preprocessing is still going on, just wait until it's done.
        // TODO ideally this should be handled in the UI so the client doesn't appear just frozen for the user.
        int waitTimes = 0;
        while (PreprocessorBackgroundTask.Instance.IsRunning)
        {
            Thread.Sleep(1000);
            waitTimes++;
            if (waitTimes > 10)
            {
                _ = MessageBox.Show("INI preprocessing not complete. Please try " +
                    "launching the game again. If the problem persists, " +
                    "contact the game or mod authors for support.");
                return;
            }
        }

        OSVersion osVersion = ClientConfiguration.GetOperatingSystemVersion();

        string gameExecutableName;
        string additionalExecutableName = string.Empty;

        if (osVersion == OSVersion.UNIX)
        {
            gameExecutableName = ClientConfiguration.Instance.UnixGameExecutableName;
        }
        else
        {
            string launcherExecutableName = ClientConfiguration.Instance.GameLauncherExecutableName;
            if (string.IsNullOrEmpty(launcherExecutableName))
            {
                gameExecutableName = ClientConfiguration.Instance.GetGameExecutableName();
            }
            else
            {
                gameExecutableName = launcherExecutableName;
                additionalExecutableName = "\"" + ClientConfiguration.Instance.GetGameExecutableName() + "\" ";
            }
        }

        string extraCommandLine = ClientConfiguration.Instance.ExtraExeCommandLineParameters;

        File.Delete(ProgramConstants.GamePath + "DTA.LOG");
        File.Delete(ProgramConstants.GamePath + "TI.LOG");
        File.Delete(ProgramConstants.GamePath + "TS.LOG");

        GameProcessStarting?.Invoke();

        if (UserINISettings.Instance.WindowedMode && UseQres)
        {
            Logger.Log("Windowed mode is enabled - using QRes.");
            Process qResProcess = new();
            qResProcess.StartInfo.FileName = ProgramConstants.QRESEXECUTABLE;
            qResProcess.StartInfo.UseShellExecute = false;
            qResProcess.StartInfo.Arguments = !string.IsNullOrEmpty(extraCommandLine)
                ? "c=16 /R " + "\"" + ProgramConstants.GamePath + gameExecutableName + "\" " + additionalExecutableName + "-SPAWN " + extraCommandLine
                : "c=16 /R " + "\"" + ProgramConstants.GamePath + gameExecutableName + "\" " + additionalExecutableName + "-SPAWN";
            qResProcess.EnableRaisingEvents = true;
            qResProcess.Exited += new EventHandler(Process_Exited);
            Logger.Log("Launch executable: " + qResProcess.StartInfo.FileName);
            Logger.Log("Launch arguments: " + qResProcess.StartInfo.Arguments);
            try
            {
                _ = qResProcess.Start();
            }
            catch (Exception ex)
            {
                Logger.Log("Error launching QRes: " + ex.Message);
                _ = MessageBox.Show(
                    "Error launching " + ProgramConstants.QRESEXECUTABLE + ". Please check that your anti-virus isn't blocking the CnCNet Client. " +
                    "You can also try running the client as an administrator." + Environment.NewLine + Environment.NewLine + "You are unable to participate in this match." +
                    Environment.NewLine + Environment.NewLine + "Returned error: " + ex.Message,
                    "Error launching game", MessageBoxButtons.OK);
                Process_Exited(qResProcess, EventArgs.Empty);
                return;
            }

            if (Environment.ProcessorCount > 1 && SingleCoreAffinity)
                qResProcess.ProcessorAffinity = (IntPtr)2;
        }
        else
        {
            Process dtaProcess = new();
            dtaProcess.StartInfo.FileName = gameExecutableName;
            dtaProcess.StartInfo.UseShellExecute = false;
            dtaProcess.StartInfo.Arguments = !string.IsNullOrEmpty(extraCommandLine)
                ? " " + additionalExecutableName + "-SPAWN " + extraCommandLine
                : additionalExecutableName + "-SPAWN";
            dtaProcess.EnableRaisingEvents = true;
            dtaProcess.Exited += new EventHandler(Process_Exited);
            Logger.Log("Launch executable: " + dtaProcess.StartInfo.FileName);
            Logger.Log("Launch arguments: " + dtaProcess.StartInfo.Arguments);
            try
            {
                _ = dtaProcess.Start();
                Logger.Log("GameProcessLogic: Process started.");
            }
            catch (Exception ex)
            {
                Logger.Log("Error launching " + gameExecutableName + ": " + ex.Message);
                _ = MessageBox.Show(
                    "Error launching " + gameExecutableName + ". Please check that your anti-virus isn't blocking the CnCNet Client. " +
                    "You can also try running the client as an administrator." + Environment.NewLine + Environment.NewLine + "You are unable to participate in this match." +
                    Environment.NewLine + Environment.NewLine + "Returned error: " + ex.Message,
                    "Error launching game", MessageBoxButtons.OK);
                Process_Exited(dtaProcess, EventArgs.Empty);
                return;
            }

            if (Environment.ProcessorCount > 1 && SingleCoreAffinity)
                dtaProcess.ProcessorAffinity = (IntPtr)2;
        }

        GameProcessStarted?.Invoke();

        Logger.Log("Waiting for qres.dat or " + gameExecutableName + " to exit.");
    }

    private static void Process_Exited(object sender, EventArgs e)
    {
        Logger.Log("GameProcessLogic: Process exited.");
        Process proc = (Process)sender;
        proc.Exited -= Process_Exited;
        proc.Dispose();
        GameProcessExited?.Invoke();
    }
}