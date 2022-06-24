using System;
using System.Diagnostics;
using ClientCore;
using ClientGUI;
using DTAClient.Domain;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Generic;

public class ExtrasWindow : XNAWindow
{
    public ExtrasWindow(WindowManager windowManager)
        : base(windowManager)
    {
    }

    public override void Initialize()
    {
        Name = "ExtrasWindow";
        ClientRectangle = new Rectangle(0, 0, 284, 190);
        BackgroundTexture = AssetLoader.LoadTexture("extrasMenu.png");

        XNAClientButton btnExStatistics = new(WindowManager)
        {
            Name = "btnExStatistics",
            ClientRectangle = new Rectangle(76, 17, UIDesignConstants.BUTTONWIDTH133, UIDesignConstants.BUTTONHEIGHT),
            Text = "Statistics".L10N("UI:Main:Statistics")
        };
        btnExStatistics.LeftClick += BtnExStatistics_LeftClick;

        XNAClientButton btnExMapEditor = new(WindowManager)
        {
            Name = "btnExMapEditor",
            ClientRectangle = new Rectangle(76, 59, UIDesignConstants.BUTTONWIDTH133, UIDesignConstants.BUTTONHEIGHT),
            Text = "Map Editor".L10N("UI:Main:MapEditor")
        };
        btnExMapEditor.LeftClick += BtnExMapEditor_LeftClick;

        XNAClientButton btnExCredits = new(WindowManager)
        {
            Name = "btnExCredits",
            ClientRectangle = new Rectangle(76, 101, UIDesignConstants.BUTTONWIDTH133, UIDesignConstants.BUTTONHEIGHT),
            Text = "Credits".L10N("UI:Main:Credits")
        };
        btnExCredits.LeftClick += BtnExCredits_LeftClick;

        XNAClientButton btnExCancel = new(WindowManager)
        {
            Name = "btnExCancel",
            ClientRectangle = new Rectangle(76, 160, UIDesignConstants.BUTTONWIDTH133, UIDesignConstants.BUTTONHEIGHT),
            Text = "Cancel".L10N("UI:Main:ButtonCancel")
        };
        btnExCancel.LeftClick += BtnExCancel_LeftClick;

        AddChild(btnExStatistics);
        AddChild(btnExMapEditor);
        AddChild(btnExCredits);
        AddChild(btnExCancel);

        base.Initialize();

        CenterOnParent();
    }

    private void BtnExStatistics_LeftClick(object sender, EventArgs e)
    {
        MainMenuDarkeningPanel parent = (MainMenuDarkeningPanel)Parent;
        parent.Show(parent.StatisticsWindow);
    }

    private void BtnExMapEditor_LeftClick(object sender, EventArgs e)
    {
        _ = Process.Start(ProgramConstants.GamePath + ClientConfiguration.Instance.MapEditorExePath);
        Enabled = false;
    }

    private void BtnExCredits_LeftClick(object sender, EventArgs e)
    {
        using Process _ = Process.Start(new ProcessStartInfo
        {
            FileName = MainClientConstants.CREDITS_URL,
            UseShellExecute = true
        });
    }

    private void BtnExCancel_LeftClick(object sender, EventArgs e)
    {
        Enabled = false;
    }
}