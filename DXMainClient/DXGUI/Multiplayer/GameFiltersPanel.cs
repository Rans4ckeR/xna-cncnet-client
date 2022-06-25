using System;
using ClientCore;
using ClientGUI;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer;

public class GameFiltersPanel : XNAPanel
{
    private const int MaxPlayerCount = 8;
    private const int MinPlayerCount = 2;
    private XNAClientCheckBox chkBoxFriendsOnly;
    private XNAClientCheckBox chkBoxHideIncompatibleGames;
    private XNAClientCheckBox chkBoxHideLockedGames;
    private XNAClientCheckBox chkBoxHidePasswordedGames;
    private XNAClientDropDown ddMaxPlayerCount;

    public GameFiltersPanel(WindowManager windowManager)
        : base(windowManager)
    {
    }

    public void Cancel()
    {
        Disable();
    }

    public override void Initialize()
    {
        base.Initialize();

        Name = "GameFiltersWindow";
        BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0), Width, Height);

        const int gap = 12;

        XNALabel lblTitle = new(WindowManager);
        lblTitle.Name = nameof(lblTitle);
        lblTitle.Text = "Game Filters".L10N("UI:Main:GameFilters");
        lblTitle.ClientRectangle = new Rectangle(
            gap, gap, 120, UIDesignConstants.ButtonHeight);

        chkBoxFriendsOnly = new XNAClientCheckBox(WindowManager);
        chkBoxFriendsOnly.Name = nameof(chkBoxFriendsOnly);
        chkBoxFriendsOnly.Text = "Show Friend Games Only".L10N("UI:Main:FriendGameOnly");
        chkBoxFriendsOnly.ClientRectangle = new Rectangle(
            gap,
            lblTitle.Y + UIDesignConstants.ButtonHeight + gap,
            0,
            0);

        chkBoxHideLockedGames = new XNAClientCheckBox(WindowManager);
        chkBoxHideLockedGames.Name = nameof(chkBoxHideLockedGames);
        chkBoxHideLockedGames.Text = "Hide Locked Games".L10N("UI:Main:HideLockedGame");
        chkBoxHideLockedGames.ClientRectangle = new Rectangle(
            gap,
            chkBoxFriendsOnly.Y + UIDesignConstants.ButtonHeight + gap,
            0,
            0);

        chkBoxHidePasswordedGames = new XNAClientCheckBox(WindowManager);
        chkBoxHidePasswordedGames.Name = nameof(chkBoxHidePasswordedGames);
        chkBoxHidePasswordedGames.Text = "Hide Passworded Games".L10N("UI:Main:HidePasswordGame");
        chkBoxHidePasswordedGames.ClientRectangle = new Rectangle(
            gap,
            chkBoxHideLockedGames.Y + UIDesignConstants.ButtonHeight + gap,
            0,
            0);

        chkBoxHideIncompatibleGames = new XNAClientCheckBox(WindowManager);
        chkBoxHideIncompatibleGames.Name = nameof(chkBoxHideIncompatibleGames);
        chkBoxHideIncompatibleGames.Text = "Hide Incompatible Games".L10N("UI:Main:HideIncompatibleGame");
        chkBoxHideIncompatibleGames.ClientRectangle = new Rectangle(
            gap,
            chkBoxHidePasswordedGames.Y + UIDesignConstants.ButtonHeight + gap,
            0,
            0);

        ddMaxPlayerCount = new XNAClientDropDown(WindowManager);
        ddMaxPlayerCount.Name = nameof(ddMaxPlayerCount);
        ddMaxPlayerCount.ClientRectangle = new Rectangle(
            gap,
            chkBoxHideIncompatibleGames.Y + UIDesignConstants.ButtonHeight + gap,
            40,
            UIDesignConstants.ButtonHeight);
        for (int i = MinPlayerCount; i <= MaxPlayerCount; i++)
        {
            ddMaxPlayerCount.AddItem(i.ToString());
        }

        XNALabel lblMaxPlayerCount = new(WindowManager);
        lblMaxPlayerCount.Name = nameof(lblMaxPlayerCount);
        lblMaxPlayerCount.Text = "Max Player Count".L10N("UI:Main:MaxPlayerCount");
        lblMaxPlayerCount.ClientRectangle = new Rectangle(
            ddMaxPlayerCount.X + ddMaxPlayerCount.Width + gap,
            ddMaxPlayerCount.Y,
            0,
            UIDesignConstants.ButtonHeight);

        XNAClientButton btnResetDefaults = new(WindowManager);
        btnResetDefaults.Name = nameof(btnResetDefaults);
        btnResetDefaults.Text = "Reset Defaults".L10N("UI:Main:ResetDefaults");
        btnResetDefaults.ClientRectangle = new Rectangle(
            gap,
            ddMaxPlayerCount.Y + UIDesignConstants.ButtonHeight + gap,
            UIDesignConstants.ButtonWidth133,
            UIDesignConstants.ButtonHeight);
        btnResetDefaults.LeftClick += BtnResetDefaults_LeftClick;

        XNAClientButton btnSave = new(WindowManager);
        btnSave.Name = nameof(btnSave);
        btnSave.Text = "Save".L10N("UI:Main:ButtonSave");
        btnSave.ClientRectangle = new Rectangle(
            gap,
            btnResetDefaults.Y + UIDesignConstants.ButtonHeight + gap,
            UIDesignConstants.ButtonWidth92,
            UIDesignConstants.ButtonHeight);
        btnSave.LeftClick += BtnSave_LeftClick;

        XNAClientButton btnCancel = new(WindowManager);
        btnCancel.Name = nameof(btnCancel);
        btnCancel.Text = "Cancel".L10N("UI:Main:ButtonCancel");
        btnCancel.ClientRectangle = new Rectangle(
            Width - gap - UIDesignConstants.ButtonWidth92,
            btnSave.Y,
            UIDesignConstants.ButtonWidth92,
            UIDesignConstants.ButtonHeight);
        btnCancel.LeftClick += BtnCancel_LeftClick;

        AddChild(lblTitle);
        AddChild(chkBoxFriendsOnly);
        AddChild(chkBoxHideLockedGames);
        AddChild(chkBoxHidePasswordedGames);
        AddChild(chkBoxHideIncompatibleGames);
        AddChild(lblMaxPlayerCount);
        AddChild(ddMaxPlayerCount);
        AddChild(btnResetDefaults);
        AddChild(btnSave);
        AddChild(btnCancel);
    }

    public void Show()
    {
        Load();
        Enable();
    }

    private void BtnCancel_LeftClick(object sender, EventArgs e)
    {
        Cancel();
    }

    private void BtnResetDefaults_LeftClick(object sender, EventArgs e)
    {
        ResetDefaults();
    }

    private void BtnSave_LeftClick(object sender, EventArgs e)
    {
        Save();
        Disable();
    }

    private void Load()
    {
        UserINISettings userIniSettings = UserINISettings.Instance;
        chkBoxFriendsOnly.Checked = userIniSettings.ShowFriendGamesOnly.Value;
        chkBoxHideLockedGames.Checked = userIniSettings.HideLockedGames.Value;
        chkBoxHidePasswordedGames.Checked = userIniSettings.HidePasswordedGames.Value;
        chkBoxHideIncompatibleGames.Checked = userIniSettings.HideIncompatibleGames.Value;
        ddMaxPlayerCount.SelectedIndex = ddMaxPlayerCount.Items.FindIndex(i => i.Text == userIniSettings.MaxPlayerCount.Value.ToString());
    }

    private void ResetDefaults()
    {
        UserINISettings.Instance.ResetGameFilters();
        Load();
    }

    private void Save()
    {
        UserINISettings userIniSettings = UserINISettings.Instance;
        userIniSettings.ShowFriendGamesOnly.Value = chkBoxFriendsOnly.Checked;
        userIniSettings.HideLockedGames.Value = chkBoxHideLockedGames.Checked;
        userIniSettings.HidePasswordedGames.Value = chkBoxHidePasswordedGames.Checked;
        userIniSettings.HideIncompatibleGames.Value = chkBoxHideIncompatibleGames.Checked;
        userIniSettings.MaxPlayerCount.Value = int.Parse(ddMaxPlayerCount.SelectedItem.Text);

        UserINISettings.Instance.SaveSettings();
    }
}