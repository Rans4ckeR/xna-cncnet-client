﻿using System;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer;

public class TeamStartMappingPanel : XNAPanel
{
    private const int DdWidth = 35;

    private readonly int _defaultTeamIndex = -1;
    private readonly int _start;

    // private XNAClientDropDown ddStarts;
    private XNAClientDropDown ddTeams;

    public TeamStartMappingPanel(WindowManager windowManager, int start)
        : base(windowManager)
    {
        _start = start;
        DrawBorders = false;
    }

    public event EventHandler OptionsChanged;

    public void ClearSelections() => ddTeams.SelectedIndex = _defaultTeamIndex;

    public void EnableControls(bool enable) => ddTeams.AllowDropDown = enable;

    public TeamStartMapping GetTeamStartMapping()
    {
        return new TeamStartMapping()
        {
            Team = ddTeams.SelectedItem?.Text,
            Start = _start
        };
    }

    public override void Initialize()
    {
        base.Initialize();

        XNALabel startLabel = new(WindowManager)
        {
            Text = _start.ToString(),
            ClientRectangle = new Rectangle(0, 0, 10, 22)
        };
        AddChild(startLabel);

        ddTeams = new XNAClientDropDown(WindowManager);
        ddTeams.Name = nameof(ddTeams);
        ddTeams.ClientRectangle = new Rectangle(startLabel.Right, startLabel.Y - 3, DdWidth, 22);
        TeamStartMapping.TEAMS.ForEach(ddTeams.AddItem);
        AddChild(ddTeams);

        ddTeams.SelectedIndexChanged += DD_SelectedItemChanged;
    }

    public void SetTeamStartMapping(TeamStartMapping teamStartMapping)
    {
        int teamIndex = teamStartMapping?.TeamIndex ?? _defaultTeamIndex;

        ddTeams.SelectedIndex = teamIndex >= 0 && teamIndex < ddTeams.Items.Count ?
            teamIndex : -1;
    }

    private void DD_SelectedItemChanged(object sender, EventArgs e) => OptionsChanged?.Invoke(sender, e);
}