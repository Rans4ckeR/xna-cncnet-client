using System;
using System.Runtime.InteropServices;

namespace DTAClient.DXGUI.Generic;

public partial class TaskbarProgress
{
    [Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComImport]
    private class TaskbarInstance
    {
    }
}