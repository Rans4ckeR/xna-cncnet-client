using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Extensions;
using Microsoft.Extensions.Logging;

namespace DTAClient.Online
{
    internal sealed class CnCNetGameCheck
    {
        private const int REFRESH_INTERVAL = 15000; // 15 seconds

        private readonly ILogger logger;

        public CnCNetGameCheck(ILogger logger)
        {
            this.logger = logger;
        }

        public async ValueTask RunServiceAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(REFRESH_INTERVAL, cancellationToken);

                    CheatEngineWatchEvent();
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private void CheatEngineWatchEvent()
        {
            Process[] processlist = Process.GetProcesses();

            foreach (Process process in processlist)
            {
                try
                {
                    if (process.ProcessName.Contains("cheatengine") ||
                        process.MainWindowTitle.ToLower().Contains("cheat engine") ||
                        process.MainWindowHandle.ToString().ToLower().Contains("cheat engine"))
                    {
                        KillGameInstance();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogExceptionDetails(ex);
                }

                process.Dispose();
            }
        }

        private void KillGameInstance()
        {
            string gameExecutableName = ClientConfiguration.Instance.GetOperatingSystemVersion() == OSVersion.UNIX ?
                ClientConfiguration.Instance.UnixGameExecutableName :
                ClientConfiguration.Instance.GetGameExecutableName();

            foreach (Process process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(gameExecutableName)))
            {
                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {
                    logger.LogExceptionDetails(ex);
                }

                process.Dispose();
            }
        }
    }
}