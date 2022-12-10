using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Extensions;
using ClientCore.INIProcessing;
using Microsoft.Extensions.Logging;
using Rampastring.Tools;

namespace ClientGUI
{
    /// <summary>
    /// A static class used for controlling the launching and exiting of the game executable.
    /// </summary>
    public sealed class GameProcessLogic
    {
        private readonly ILogger logger;
        private readonly PreprocessorBackgroundTask preprocessorBackgroundTask;
        private readonly UserINISettings userIniSettings;
        private readonly XNAMessageBox xnaMessageBox;

        public GameProcessLogic(
            ILogger logger,
            PreprocessorBackgroundTask preprocessorBackgroundTask,
            UserINISettings userIniSettings,
            XNAMessageBox xnaMessageBox)
        {
            this.logger = logger;
            this.preprocessorBackgroundTask = preprocessorBackgroundTask;
            this.userIniSettings = userIniSettings;
            this.xnaMessageBox = xnaMessageBox;
        }

        public event Action GameProcessStarted;

        public event Action GameProcessStarting;

        public event Action GameProcessExited;

        public bool UseQres { get; set; }
        public bool SingleCoreAffinity { get; set; }

        /// <summary>
        /// Starts the main game process.
        /// </summary>
        public async ValueTask StartGameProcessAsync()
        {
            logger.LogInformation("About to launch main game executable.");

            // In the relatively unlikely event that INI preprocessing is still going on, just wait until it's done.
            // TODO ideally this should be handled in the UI so the client doesn't appear just frozen for the user.
            int waitTimes = 0;
            while (preprocessorBackgroundTask.IsRunning)
            {
                await Task.Delay(1000);
                waitTimes++;
                if (waitTimes > 10)
                {
                    xnaMessageBox.Caption = "INI preprocessing not complete";
                    xnaMessageBox.Description = "INI preprocessing not complete. Please try " +
                        "launching the game again. If the problem persists, " +
                        "contact the game or mod authors for support.";
                    xnaMessageBox.MessageBoxButtons = XNAMessageBoxButtons.OK;

                    xnaMessageBox.Show();
                    return;
                }
            }

            OSVersion osVersion = ClientConfiguration.Instance.GetOperatingSystemVersion();

            string gameExecutableName;
            string additionalExecutableName = string.Empty;

            if (osVersion == OSVersion.UNIX)
                gameExecutableName = ClientConfiguration.Instance.UnixGameExecutableName;
            else
            {
                string launcherExecutableName = ClientConfiguration.Instance.GameLauncherExecutableName;
                if (string.IsNullOrEmpty(launcherExecutableName))
                    gameExecutableName = ClientConfiguration.Instance.GetGameExecutableName();
                else
                {
                    gameExecutableName = launcherExecutableName;
                    additionalExecutableName = "\"" + ClientConfiguration.Instance.GetGameExecutableName() + "\" ";
                }
            }

            string extraCommandLine = ClientConfiguration.Instance.ExtraExeCommandLineParameters;

            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "DTA.LOG");
            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "TI.LOG");
            SafePath.DeleteFileIfExists(ProgramConstants.GamePath, "TS.LOG");

            GameProcessStarting?.Invoke();

            if (userIniSettings.WindowedMode && UseQres && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                logger.LogInformation("Windowed mode is enabled - using QRes.");
                Process QResProcess = new Process();
                QResProcess.StartInfo.FileName = ProgramConstants.QRES_EXECUTABLE;

                if (!string.IsNullOrEmpty(extraCommandLine))
                    QResProcess.StartInfo.Arguments = "c=16 /R " + "\"" + SafePath.CombineFilePath(ProgramConstants.GamePath, gameExecutableName) + "\" " + additionalExecutableName + "-SPAWN " + extraCommandLine;
                else
                    QResProcess.StartInfo.Arguments = "c=16 /R " + "\"" + SafePath.CombineFilePath(ProgramConstants.GamePath, gameExecutableName) + "\" " + additionalExecutableName + "-SPAWN";
                QResProcess.EnableRaisingEvents = true;
                QResProcess.Exited += new EventHandler(Process_Exited);
                logger.LogInformation("Launch executable: " + QResProcess.StartInfo.FileName);
                logger.LogInformation("Launch arguments: " + QResProcess.StartInfo.Arguments);
                try
                {
                    QResProcess.Start();
                }
                catch (Exception ex)
                {
                    logger.LogExceptionDetails(ex, "Error launching QRes");

                    xnaMessageBox.Caption = "Error launching game";
                    xnaMessageBox.Description = "Error launching " + ProgramConstants.QRES_EXECUTABLE + ". Please check that your anti-virus isn't blocking the CnCNet Client. " +
                        "You can also try running the client as an administrator." + Environment.NewLine + Environment.NewLine + "You are unable to participate in this match." +
                        Environment.NewLine + Environment.NewLine + "Returned error: " + ex.Message;
                    xnaMessageBox.MessageBoxButtons = XNAMessageBoxButtons.OK;

                    xnaMessageBox.Show();
                    Process_Exited(QResProcess, EventArgs.Empty);
                    return;
                }

                if (Environment.ProcessorCount > 1 && SingleCoreAffinity)
                    QResProcess.ProcessorAffinity = (IntPtr)2;
            }
            else
            {
                Process DtaProcess = new Process();
                DtaProcess.StartInfo.FileName = gameExecutableName;

                if (!string.IsNullOrEmpty(extraCommandLine))
                    DtaProcess.StartInfo.Arguments = " " + additionalExecutableName + "-SPAWN " + extraCommandLine;
                else
                    DtaProcess.StartInfo.Arguments = additionalExecutableName + "-SPAWN";
                DtaProcess.EnableRaisingEvents = true;
                DtaProcess.Exited += new EventHandler(Process_Exited);
                logger.LogInformation("Launch executable: " + DtaProcess.StartInfo.FileName);
                logger.LogInformation("Launch arguments: " + DtaProcess.StartInfo.Arguments);
                try
                {
                    DtaProcess.Start();
                    logger.LogInformation("GameProcessLogic: Process started.");
                }
                catch (Exception ex)
                {
                    logger.LogExceptionDetails(ex, "Error launching " + gameExecutableName);

                    xnaMessageBox.Caption = "Error launching game";
                    xnaMessageBox.Description = "Error launching " + gameExecutableName + ". Please check that your anti-virus isn't blocking the CnCNet Client. " +
                        "You can also try running the client as an administrator." + Environment.NewLine + Environment.NewLine + "You are unable to participate in this match." +
                        Environment.NewLine + Environment.NewLine + "Returned error: " + ex.Message;
                    xnaMessageBox.MessageBoxButtons = XNAMessageBoxButtons.OK;

                    xnaMessageBox.Show();
                    Process_Exited(DtaProcess, EventArgs.Empty);
                    return;
                }

                if ((RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    && Environment.ProcessorCount > 1 && SingleCoreAffinity)
                {
                    DtaProcess.ProcessorAffinity = 2;
                }
            }

            GameProcessStarted?.Invoke();

            logger.LogInformation("Waiting for qres.dat or " + gameExecutableName + " to exit.");
        }

        private void Process_Exited(object sender, EventArgs e)
        {
            logger.LogInformation("GameProcessLogic: Process exited.");
            Process proc = (Process)sender;
            proc.Exited -= Process_Exited;
            proc.Dispose();
            GameProcessExited?.Invoke();
        }
    }
}