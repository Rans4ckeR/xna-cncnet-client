using System;

namespace DTAConfig;

public partial class HotkeyConfigurationWindow
{
    [Flags]
    private enum KeyModifiers
    {
        None = 0,
        Shift = 1,
        Ctrl = 2,
        Alt = 4
    }
}