using System;
using System.Collections.Generic;
using DTAClient.Online;
using Localization;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.CnCNet;

public class RecentPlayerTable : XNAMultiColumnListBox
{
    private readonly CnCNetManager connectionManager;

    public RecentPlayerTable(WindowManager windowManager, CnCNetManager connectionManager)
        : base(windowManager)
    {
        this.connectionManager = connectionManager;
    }

    public EventHandler<RecentPlayerTableRightClickEventArgs> PlayerRightClick { get; set; }

    public void AddRecentPlayer(RecentPlayer recentPlayer)
    {
        IRCUser iu = connectionManager.UserList.Find(u => u.Name == recentPlayer.PlayerName);
        bool isOnline = true;

        if (iu == null)
        {
            iu = new IRCUser(recentPlayer.PlayerName);
            isOnline = false;
        }

        Microsoft.Xna.Framework.Color textColor = isOnline ? UISettings.ActiveSettings.AltColor : UISettings.ActiveSettings.DisabledItemColor;
        AddItem(new List<XNAListBoxItem>()
        {
            new XNAListBoxItem(recentPlayer.PlayerName, textColor)
            {
                Tag = iu
            },
            new XNAListBoxItem(recentPlayer.GameName, textColor),
            new XNAListBoxItem(recentPlayer.GameTime.ToLocalTime().ToString("ddd, MMM d, yyyy @ h:mm tt"), textColor)
        });
    }

    public override void Initialize()
    {
        AllowRightClickUnselect = false;

        base.Initialize();

        AddColumn("Player".L10N("UI:Main:RecentPlayerPlayer"));
        AddColumn("Game".L10N("UI:Main:RecentPlayerGame"));
        AddColumn("Date/Time".L10N("UI:Main:RecentPlayerDateTime"));
    }

    private void AddColumn(string headerText)
    {
        XNAPanel header = CreateColumnHeader(headerText);
        XNAListBox xnaListBox = new(WindowManager);
        xnaListBox.RightClick += ListBox_RightClick;
        AddColumn(header, xnaListBox);
    }

    private XNAPanel CreateColumnHeader(string headerText)
    {
        XNALabel xnaLabel = new(WindowManager)
        {
            FontIndex = HeaderFontIndex,
            X = 3,
            Y = 2,
            Text = headerText
        };
        XNAPanel header = new(WindowManager)
        {
            Height = xnaLabel.Height + 3
        };
        int width = Width / 3;
        header.Width = DrawListBoxBorders ? width + 1 : width;
        header.AddChild(xnaLabel);

        return header;
    }

    private void ListBox_RightClick(object sender, EventArgs e)
    {
        if (HoveredIndex < 0 || HoveredIndex >= ItemCount)
            return;

        SelectedIndex = HoveredIndex;

        XNAListBoxItem selectedItem = GetItem(0, SelectedIndex);
        PlayerRightClick?.Invoke(this, new RecentPlayerTableRightClickEventArgs((IRCUser)selectedItem.Tag));
    }
}