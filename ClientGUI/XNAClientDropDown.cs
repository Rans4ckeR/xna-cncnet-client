using System;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace ClientGUI;

public class XNAClientDropDown : XNADropDown
{
    public XNAClientDropDown(WindowManager windowManager)
        : base(windowManager)
    {
    }

    public ToolTip ToolTip { get; set; }

    public override void Initialize()
    {
        ClickSoundEffect = new EnhancedSoundEffect("dropdown.wav");

        CreateToolTip();

        base.Initialize();
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

    public override void OnMouseLeftDown()
    {
        base.OnMouseLeftDown();
        UpdateToolTipBlock();
    }

    protected override void CloseDropDown()
    {
        base.CloseDropDown();
        UpdateToolTipBlock();
    }

    protected void UpdateToolTipBlock()
    {
        ToolTip.Blocked = DropDownState != DropDownState.CLOSED;
    }
}