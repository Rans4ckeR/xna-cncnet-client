using System;

namespace DTAClient.DXGUI.Generic;

public class UpdateFailureEventArgs : EventArgs
{
    public UpdateFailureEventArgs(string reason)
    {
        Reason = reason;
    }

    /// <summary>
    /// Gets the returned error message from the update failure.
    /// </summary>
    public string Reason { get; private set; } = string.Empty;
}