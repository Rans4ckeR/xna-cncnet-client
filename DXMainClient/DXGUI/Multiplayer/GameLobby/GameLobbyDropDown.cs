using ClientGUI;
using DTAClient.Domain.Multiplayer;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

/// <summary>
/// A game option drop-down for the game lobby.
/// </summary>
public class GameLobbyDropDown : XNAClientDropDown
{
    private DropDownDataWriteMode dataWriteMode = DropDownDataWriteMode.BOOLEAN;

    private string spawnIniOption = string.Empty;

    public GameLobbyDropDown(WindowManager windowManager)
            : base(windowManager)
    {
    }

    public int HostSelectedIndex { get; set; }

    public string OptionName { get; private set; }

    public int UserSelectedIndex { get; set; }

    /// <summary>
    /// Applies the drop down's associated code to the map INI file.
    /// </summary>
    /// <param name="mapIni">The map INI file.</param>
    /// <param name="gameMode">Currently selected gamemode, if set.</param>
    public void ApplyMapCode(IniFile mapIni, GameMode gameMode)
    {
        if (dataWriteMode != DropDownDataWriteMode.MAPCODE || SelectedIndex < 0 || SelectedIndex >= Items.Count)
            return;

        string customIniPath = Items[SelectedIndex].Tag != null ? Items[SelectedIndex].Tag.ToString() : Items[SelectedIndex].Text;
        MapCodeHelper.ApplyMapCode(mapIni, customIniPath, gameMode);
    }

    /// <summary>
    /// Applies the drop down's associated code to spawn.ini.
    /// </summary>
    /// <param name="spawnIni">The spawn INI file.</param>
    public void ApplySpawnIniCode(IniFile spawnIni)
    {
        if (dataWriteMode == DropDownDataWriteMode.MAPCODE || SelectedIndex < 0 || SelectedIndex >= Items.Count)
            return;

        if (string.IsNullOrEmpty(spawnIniOption))
        {
            Logger.Log("GameLobbyDropDown.WriteSpawnIniCode: " + Name + " has no associated spawn INI option!");
            return;
        }

        switch (dataWriteMode)
        {
            case DropDownDataWriteMode.BOOLEAN:
                spawnIni.SetBooleanValue("Settings", spawnIniOption, SelectedIndex > 0);
                break;

            case DropDownDataWriteMode.INDEX:
                spawnIni.SetIntValue("Settings", spawnIniOption, SelectedIndex);
                break;

            default:
            case DropDownDataWriteMode.StringValue:
                if (Items[SelectedIndex].Tag != null)
                {
                    spawnIni.SetStringValue("Settings", spawnIniOption, Items[SelectedIndex].Tag.ToString());
                }
                else
                {
                    spawnIni.SetStringValue("Settings", spawnIniOption, Items[SelectedIndex].Text);
                }

                break;
        }
    }

    public override void Initialize()
    {
        // Find the game lobby that this control belongs to and register ourselves as a game option.
        XNAControl parent = Parent;
        while (true)
        {
            if (parent == null)
                break;

            // oh no, we have a circular class reference here!
            if (parent is GameLobbyBase gameLobby)
            {
                gameLobby.DropDowns.Add(this);
                break;
            }

            parent = parent.Parent;
        }

        base.Initialize();
    }

    public override void OnLeftClick()
    {
        if (!AllowDropDown)
            return;

        base.OnLeftClick();
        UserSelectedIndex = SelectedIndex;
    }

    public override void ParseAttributeFromINI(IniFile iniFile, string key, string value)
    {
        switch (key)
        {
            case "Items":
                string[] items = value.Split(',');
                string[] itemlabels = iniFile.GetStringValue(Name, "ItemLabels", string.Empty).Split(',');
                for (int i = 0; i < items.Length; i++)
                {
                    XNADropDownItem item = new();
                    if (itemlabels.Length > i && !string.IsNullOrEmpty(itemlabels[i]))
                    {
                        item.Text = itemlabels[i];
                        item.Tag = items[i];
                    }
                    else
                    {
                        item.Text = items[i];
                    }

                    AddItem(item);
                }

                return;

            case "DataWriteMode":
                dataWriteMode = value.ToUpperInvariant() switch
                {
                    nameof(DropDownDataWriteMode.INDEX) => DropDownDataWriteMode.INDEX,
                    nameof(DropDownDataWriteMode.BOOLEAN) => DropDownDataWriteMode.BOOLEAN,
                    nameof(DropDownDataWriteMode.MAPCODE) => DropDownDataWriteMode.MAPCODE,
                    _ => DropDownDataWriteMode.StringValue,
                };
                return;

            case "SpawnIniOption":
                spawnIniOption = value;
                return;

            case "DefaultIndex":
                SelectedIndex = int.Parse(value);
                HostSelectedIndex = SelectedIndex;
                UserSelectedIndex = SelectedIndex;
                return;

            case "OptionName":
                OptionName = value;
                return;
        }

        base.ParseAttributeFromINI(iniFile, key, value);
    }
}