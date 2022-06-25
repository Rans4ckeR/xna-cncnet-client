using System;
using System.Collections.Generic;
using System.Linq;
using ClientGUI;
using DTAClient.Domain.Multiplayer;
using Localization;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer;

public class PlayerExtraOptionsPanel : XNAWindow
{
    private const int DefaultTeamStartMappingX = UIDesignConstants.EmptySpaceSides;

    private const int DefaultX = 24;

    private const int MaxStartCount = 8;

    private const int TeamMappingPanelHeight = 22;

    private const int TeamMappingPanelWidth = 50;

    private readonly string customPresetName = "Custom".L10N("UI:Main:CustomPresetName");

    private bool _isHost;

    private Map _map;

    private XNAClientCheckBox chkBoxForceRandomColors;

    private XNAClientCheckBox chkBoxForceRandomSides;

    private XNAClientCheckBox chkBoxForceRandomStarts;

    private XNAClientCheckBox chkBoxForceRandomTeams;

    private XNAClientCheckBox chkBoxUseTeamStartMappings;

    private XNAClientDropDown ddTeamStartMappingPreset;

    private bool ignoreMappingChanges;

    private TeamStartMappingsPanel teamStartMappingsPanel;

    public PlayerExtraOptionsPanel(WindowManager windowManager)
        : base(windowManager)
    {
    }

    public EventHandler OnClose { get; set; }

    public EventHandler OptionsChanged { get; set; }

    public void EnableControls(bool enable)
    {
        chkBoxForceRandomSides.InputEnabled = enable;
        chkBoxForceRandomColors.InputEnabled = enable;
        chkBoxForceRandomStarts.InputEnabled = enable;
        chkBoxForceRandomTeams.InputEnabled = enable;
        chkBoxUseTeamStartMappings.InputEnabled = enable;

        teamStartMappingsPanel.EnableControls(enable && chkBoxUseTeamStartMappings.Checked);
    }

    public PlayerExtraOptions GetPlayerExtraOptions()
        => new()
        {
            IsForceRandomSides = IsForcedRandomSides(),
            IsForceRandomColors = IsForcedRandomColors(),
            IsForceRandomStarts = IsForcedRandomStarts(),
            IsForceRandomTeams = IsForcedRandomTeams(),
            IsUseTeamStartMappings = IsUseTeamStartMappings(),
            TeamStartMappings = GetTeamStartMappings()
        };

    public List<TeamStartMapping> GetTeamStartMappings()
        => chkBoxUseTeamStartMappings.Checked ?
            teamStartMappingsPanel.GetTeamStartMappings() : new List<TeamStartMapping>();

    public override void Initialize()
    {
        Name = nameof(PlayerExtraOptionsPanel);
        BackgroundTexture = AssetLoader.CreateTexture(new Color(0, 0, 0, 255), 1, 1);
        Visible = false;

        XNAClientButton btnClose = new(WindowManager);
        btnClose.Name = nameof(btnClose);
        btnClose.ClientRectangle = new Rectangle(0, 0, 0, 0);
        btnClose.IdleTexture = AssetLoader.LoadTexture("optionsButtonClose.png");
        btnClose.HoverTexture = AssetLoader.LoadTexture("optionsButtonClose_c.png");
        btnClose.LeftClick += (sender, args) => Disable();
        AddChild(btnClose);

        XNALabel lblHeader = new(WindowManager);
        lblHeader.Name = nameof(lblHeader);
        lblHeader.Text = "Extra Player Options".L10N("UI:Main:ExtraPlayerOptions");
        lblHeader.ClientRectangle = new Rectangle(DefaultX, 4, 0, 18);
        AddChild(lblHeader);

        chkBoxForceRandomSides = new XNAClientCheckBox(WindowManager);
        chkBoxForceRandomSides.Name = nameof(chkBoxForceRandomSides);
        chkBoxForceRandomSides.Text = "Force Random Sides".L10N("UI:Main:ForceRandomSides");
        chkBoxForceRandomSides.ClientRectangle = new Rectangle(DefaultX, lblHeader.Bottom + 4, 0, 0);
        chkBoxForceRandomSides.CheckedChanged += Options_Changed;
        AddChild(chkBoxForceRandomSides);

        chkBoxForceRandomColors = new XNAClientCheckBox(WindowManager);
        chkBoxForceRandomColors.Name = nameof(chkBoxForceRandomColors);
        chkBoxForceRandomColors.Text = "Force Random Colors".L10N("UI:Main:ForceRandomColors");
        chkBoxForceRandomColors.ClientRectangle = new Rectangle(DefaultX, chkBoxForceRandomSides.Bottom + 4, 0, 0);
        chkBoxForceRandomColors.CheckedChanged += Options_Changed;
        AddChild(chkBoxForceRandomColors);

        chkBoxForceRandomTeams = new XNAClientCheckBox(WindowManager);
        chkBoxForceRandomTeams.Name = nameof(chkBoxForceRandomTeams);
        chkBoxForceRandomTeams.Text = "Force Random Teams".L10N("UI:Main:ForceRandomTeams");
        chkBoxForceRandomTeams.ClientRectangle = new Rectangle(DefaultX, chkBoxForceRandomColors.Bottom + 4, 0, 0);
        chkBoxForceRandomTeams.CheckedChanged += Options_Changed;
        AddChild(chkBoxForceRandomTeams);

        chkBoxForceRandomStarts = new XNAClientCheckBox(WindowManager);
        chkBoxForceRandomStarts.Name = nameof(chkBoxForceRandomStarts);
        chkBoxForceRandomStarts.Text = "Force Random Starts".L10N("UI:Main:ForceRandomStarts");
        chkBoxForceRandomStarts.ClientRectangle = new Rectangle(DefaultX, chkBoxForceRandomTeams.Bottom + 4, 0, 0);
        chkBoxForceRandomStarts.CheckedChanged += Options_Changed;
        AddChild(chkBoxForceRandomStarts);

        /////////////////////////////

        chkBoxUseTeamStartMappings = new XNAClientCheckBox(WindowManager);
        chkBoxUseTeamStartMappings.Name = nameof(chkBoxUseTeamStartMappings);
        chkBoxUseTeamStartMappings.Text = "Enable Auto Allying:".L10N("UI:Main:EnableAutoAllying");
        chkBoxUseTeamStartMappings.ClientRectangle = new Rectangle(chkBoxForceRandomSides.X, chkBoxForceRandomStarts.Bottom + 20, 0, 0);
        chkBoxUseTeamStartMappings.CheckedChanged += ChkBoxUseTeamStartMappings_Changed;
        AddChild(chkBoxUseTeamStartMappings);

        XNAClientButton btnHelp = new(WindowManager);
        btnHelp.Name = nameof(btnHelp);
        btnHelp.IdleTexture = AssetLoader.LoadTexture("questionMark.png");
        btnHelp.HoverTexture = AssetLoader.LoadTexture("questionMark_c.png");
        btnHelp.LeftClick += BtnHelp_LeftClick;
        btnHelp.ClientRectangle = new Rectangle(chkBoxUseTeamStartMappings.Right + 4, chkBoxUseTeamStartMappings.Y - 1, 0, 0);
        AddChild(btnHelp);

        XNALabel lblPreset = new(WindowManager);
        lblPreset.Name = nameof(lblPreset);
        lblPreset.Text = "Presets:".L10N("UI:Main:Presets");
        lblPreset.ClientRectangle = new Rectangle(chkBoxUseTeamStartMappings.X, chkBoxUseTeamStartMappings.Bottom + 8, 0, 0);
        AddChild(lblPreset);

        ddTeamStartMappingPreset = new XNAClientDropDown(WindowManager);
        ddTeamStartMappingPreset.Name = nameof(ddTeamStartMappingPreset);
        ddTeamStartMappingPreset.ClientRectangle = new Rectangle(lblPreset.X + 50, lblPreset.Y - 2, 160, 0);
        ddTeamStartMappingPreset.SelectedIndexChanged += DdTeamMappingPreset_SelectedIndexChanged;
        ddTeamStartMappingPreset.AllowDropDown = true;
        AddChild(ddTeamStartMappingPreset);

        teamStartMappingsPanel = new TeamStartMappingsPanel(WindowManager);
        teamStartMappingsPanel.Name = nameof(teamStartMappingsPanel);
        teamStartMappingsPanel.ClientRectangle = new Rectangle(lblPreset.X, ddTeamStartMappingPreset.Bottom + 8, Width, Height - ddTeamStartMappingPreset.Bottom + 4);
        AddChild(teamStartMappingsPanel);

        AddLocationAssignments();

        base.Initialize();

        RefreshTeamStartMappingsPanel();
    }

    public bool IsForcedRandomColors() => chkBoxForceRandomColors.Checked;

    public bool IsForcedRandomSides() => chkBoxForceRandomSides.Checked;

    public bool IsForcedRandomStarts() => chkBoxForceRandomStarts.Checked;

    public bool IsForcedRandomTeams() => chkBoxForceRandomTeams.Checked;

    public bool IsUseTeamStartMappings() => chkBoxUseTeamStartMappings.Checked;

    public void SetIsHost(bool isHost)
    {
        _isHost = isHost;
        RefreshPresetDropdown();
        EnableControls(_isHost);
    }

    public void SetPlayerExtraOptions(PlayerExtraOptions playerExtraOptions)
    {
        chkBoxForceRandomSides.Checked = playerExtraOptions.IsForceRandomSides;
        chkBoxForceRandomColors.Checked = playerExtraOptions.IsForceRandomColors;
        chkBoxForceRandomTeams.Checked = playerExtraOptions.IsForceRandomTeams;
        chkBoxForceRandomStarts.Checked = playerExtraOptions.IsForceRandomStarts;
        chkBoxUseTeamStartMappings.Checked = playerExtraOptions.IsUseTeamStartMappings;
        teamStartMappingsPanel.SetTeamStartMappings(playerExtraOptions.TeamStartMappings);
    }

    public void UpdateForMap(Map map)
    {
        if (_map == map)
            return;

        _map = map;

        RefreshTeamStartMappingPanels();
    }

    private void AddLocationAssignments()
    {
        for (int i = 0; i < MaxStartCount; i++)
        {
            TeamStartMappingPanel teamStartMappingPanel = new(WindowManager, i + 1)
            {
                ClientRectangle = GetTeamMappingPanelRectangle(i)
            };

            teamStartMappingsPanel.AddMappingPanel(teamStartMappingPanel);
        }

        teamStartMappingsPanel.MappingChanged += Mapping_Changed;
    }

    private void BtnHelp_LeftClick(object sender, EventArgs args)
    {
        string desc = ("Auto allying allows the host to assign starting locations to teams, not players.\n" +
                "When players are assigned to spawn locations, they will be auto assigned to teams based on these mappings.\n" +
                "This is best used with random teams and random starts. However, only random teams is required.\n" +
                "Manually specified starts will take precedence.\n\n").L10N("UI:Main:AutoAllyingText1") +
                $"{TeamStartMapping.NOTEAM} : " + "Block this location from being assigned to a player.".L10N("UI:Main:AutoAllyingTextNoTeam") + "\n" +
                $"{TeamStartMapping.RANDOMTEAM} : " + "Allow a player here, but don't assign a team.".L10N("UI:Main:AutoAllyingTextRandomTeam");
        XNAMessageBox.Show(
            WindowManager,
            "Auto Allying".L10N("UI:Main:AutoAllyingTitle"),
            desc);
    }

    private void ChkBoxUseTeamStartMappings_Changed(object sender, EventArgs e)
    {
        RefreshTeamStartMappingsPanel();
        chkBoxForceRandomTeams.Checked = chkBoxForceRandomTeams.Checked || chkBoxUseTeamStartMappings.Checked;
        chkBoxForceRandomTeams.AllowChecking = !chkBoxUseTeamStartMappings.Checked;

        // chkBoxForceRandomStarts.Checked = chkBoxForceRandomStarts.Checked ||
        // chkBoxUseTeamStartMappings.Checked; chkBoxForceRandomStarts.AllowChecking = !chkBoxUseTeamStartMappings.Checked;
        RefreshPresetDropdown();

        Options_Changed(sender, e);
    }

    private void ClearTeamStartMappingSelections()
        => teamStartMappingsPanel.GetTeamStartMappingPanels().ForEach(panel => panel.ClearSelections());

    private void DdTeamMappingPreset_SelectedIndexChanged(object sender, EventArgs e)
    {
        XNADropDownItem selectedItem = ddTeamStartMappingPreset.SelectedItem;
        if (selectedItem?.Text == customPresetName)
            return;

        List<TeamStartMapping> teamStartMappings = selectedItem?.Tag as List<TeamStartMapping>;

        ignoreMappingChanges = true;
        teamStartMappingsPanel.SetTeamStartMappings(teamStartMappings);
        ignoreMappingChanges = false;
    }

    private Rectangle GetTeamMappingPanelRectangle(int index)
    {
        const int maxColumnCount = 2;
        const int mappingPanelDefaultX = 4;
        const int mappingPanelDefaultY = 0;
        if (index > 0 && index % maxColumnCount == 0) // need to start a new column
            return new Rectangle((index / maxColumnCount * (TeamMappingPanelWidth + mappingPanelDefaultX)) + 3, mappingPanelDefaultY, TeamMappingPanelWidth, TeamMappingPanelHeight);

        TeamStartMappingPanel lastControl = index > 0 ? teamStartMappingsPanel.GetTeamStartMappingPanels()[index - 1] : null;
        return new Rectangle(lastControl?.X ?? mappingPanelDefaultX, lastControl?.Bottom + 4 ?? mappingPanelDefaultY, TeamMappingPanelWidth, TeamMappingPanelHeight);
    }

    private void Mapping_Changed(object sender, EventArgs e)
    {
        Options_Changed(sender, e);
        if (ignoreMappingChanges)
            return;

        ddTeamStartMappingPreset.SelectedIndex = 0;
    }

    private void Options_Changed(object sender, EventArgs e) => OptionsChanged?.Invoke(sender, e);

    private void RefreshPresetDropdown() => ddTeamStartMappingPreset.AllowDropDown = _isHost && chkBoxUseTeamStartMappings.Checked;

    private void RefreshTeamStartMappingPanels()
    {
        ClearTeamStartMappingSelections();
        List<TeamStartMappingPanel> teamStartMappingPanels = teamStartMappingsPanel.GetTeamStartMappingPanels();
        for (int i = 0; i < teamStartMappingPanels.Count; i++)
        {
            TeamStartMappingPanel teamStartMappingPanel = teamStartMappingPanels[i];
            teamStartMappingPanel.ClearSelections();
            if (!IsUseTeamStartMappings())
                continue;

            teamStartMappingPanel.EnableControls(_isHost && chkBoxUseTeamStartMappings.Checked && i < _map?.MaxPlayers);
            RefreshTeamStartMappingPresets(_map?.TeamStartMappingPresets);
        }
    }

    private void RefreshTeamStartMappingPresets(List<TeamStartMappingPreset> teamStartMappingPresets)
    {
        ddTeamStartMappingPreset.Items.Clear();
        ddTeamStartMappingPreset.AddItem(new XNADropDownItem
        {
            Text = customPresetName,
            Tag = new List<TeamStartMapping>()
        });
        ddTeamStartMappingPreset.SelectedIndex = 0;

        if (!(teamStartMappingPresets?.Any() ?? false))
            return;

        teamStartMappingPresets.ForEach(preset => ddTeamStartMappingPreset.AddItem(new XNADropDownItem
        {
            Text = preset.Name,
            Tag = preset.TeamStartMappings
        }));
        ddTeamStartMappingPreset.SelectedIndex = 1;
    }

    private void RefreshTeamStartMappingsPanel()
    {
        teamStartMappingsPanel.EnableControls(_isHost && chkBoxUseTeamStartMappings.Checked);

        RefreshTeamStartMappingPanels();
    }
}