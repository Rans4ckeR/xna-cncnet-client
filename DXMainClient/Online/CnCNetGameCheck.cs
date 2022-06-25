﻿using System.Diagnostics;
using System.Threading;
using ClientCore;

namespace DTAClient.Online;

public class CnCNetGameCheck
{
    private static readonly int REFRESH_INTERVAL = 15000; // 15 seconds

    public void InitializeService(CancellationTokenSource cts)
    {
        _ = ThreadPool.QueueUserWorkItem(new WaitCallback(RunService), cts);
    }

    private static void KillGameInstance()
    {
        try
        {
            string gameExecutableName = ClientConfiguration.GetOperatingSystemVersion() == OSVersion.UNIX ?
                ClientConfiguration.Instance.UnixGameExecutableName :
                ClientConfiguration.Instance.GameExecutableName;

            gameExecutableName = gameExecutableName.Replace(".exe", string.Empty);

            Process[] processlist = Process.GetProcesses();
            foreach (Process process in processlist)
            {
                try
                {
                    if (process.ProcessName.Contains(gameExecutableName))
                    {
                        process.Kill();
                    }
                }
                catch
                {
                }

                process.Dispose();
            }
        }
        catch
        {
        }
    }

    private static void CheatEngineWatchEvent()
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
                    CnCNetGameCheck.KillGameInstance();
                }
            }
            catch
            {
            }

            process.Dispose();
        }
    }

    private void RunService(object tokenObj)
    {
        WaitHandle waitHandle = ((CancellationTokenSource)tokenObj).Token.WaitHandle;

        while (true)
        {
            if (waitHandle.WaitOne(REFRESH_INTERVAL))
            {
                // Cancellation signaled
                return;
            }
            else
            {
                CnCNetGameCheck.CheatEngineWatchEvent();
            }
        }
    }
}