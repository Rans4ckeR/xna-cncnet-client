﻿using System;
using System.Diagnostics;
using Rampastring.Tools;
using Rampastring.XNAUI;

namespace ClientGUI;

public class XNALinkButton : XNAClientButton
{
    private ToolTip toolTip;

    public XNALinkButton(WindowManager windowManager)
        : base(windowManager)
    {
    }

    public string URL { get; set; }

    public override void Initialize()
    {
        base.Initialize();

        CreateToolTip();
    }

    private void CreateToolTip()
    {
        if (toolTip == null)
            toolTip = new ToolTip(WindowManager, this);
    }

    public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
    {
        if (key == "URL")
        {
            URL = value;
            return;
        }
        else if (key == "ToolTip")
        {
            CreateToolTip();
            toolTip.Text = value.Replace("@", Environment.NewLine);
            return;
        }

        base.ParseAttributeFromINI(iniFile, key, value);
    }

    public override void OnLeftClick()
    {
        using Process _ = Process.Start(new ProcessStartInfo
        {
            FileName = URL,
            UseShellExecute = true
        });

        base.OnLeftClick();
    }
}