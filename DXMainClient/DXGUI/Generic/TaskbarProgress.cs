using System;

namespace DTAClient.DXGUI.Generic;

/// <summary>
/// For utilizing the taskbar progress bar introduced in Windows 7:
/// http://stackoverflow.com/questions/1295890/windows-7-progress-bar-in-taskbar-in-c.
/// </summary>
public partial class TaskbarProgress
{
    private readonly ITaskbarList3 taskbarInstance = (ITaskbarList3)new TaskbarInstance();

    public enum TaskbarStates
    {
        NoProgress = 0,
        Indeterminate = 0x1,
        Normal = 0x2,
        Error = 0x4,
        Paused = 0x8
    }

    public void SetState(IntPtr windowHandle, TaskbarStates taskbarState)
    {
        taskbarInstance.SetProgressState(windowHandle, taskbarState);
    }

    public void SetValue(IntPtr windowHandle, double progressValue, double progressMax)
    {
        taskbarInstance.SetProgressValue(windowHandle, (ulong)progressValue, (ulong)progressMax);
    }
}