using System;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI;

public class XNAClientCheckBox : XNACheckBox
{
    public XNAClientCheckBox(WindowManager windowManager)
        : base(windowManager)
    {
    }

    public ToolTip ToolTip { get; set; }

    public override void Initialize()
    {
        CheckSoundEffect = new EnhancedSoundEffect("checkbox.wav");

        base.Initialize();

        CreateToolTip();
    }

    private void CreateToolTip()
    {
        if (ToolTip == null)
            ToolTip = new ToolTip(WindowManager, this);
    }

    public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
    {
        if (key == "ToolTip")
        {
            CreateToolTip();
            ToolTip.Text = value.Replace("@", Environment.NewLine);
            return;
        }

        base.ParseAttributeFromINI(iniFile, key, value);
    }
}