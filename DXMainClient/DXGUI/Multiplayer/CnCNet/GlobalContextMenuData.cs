using DTAClient.Online;

namespace DTAClient.DXGUI.Multiplayer.CnCNet;

public class GlobalContextMenuData
{
    /// <summary>
    /// Gets or sets the ChannelUser to show the menu for.
    /// </summary>
    public ChannelUser ChannelUser { get; set; }

    /// <summary>
    /// Gets or sets the ChatMessage to show the menu for.
    /// </summary>
    public ChatMessage ChatMessage { get; set; }

    /// <summary>
    /// Gets or sets the IRCUser to show the menu for.
    /// </summary>
    public IRCUser IrcUser { get; set; }

    /// <summary>
    /// Gets or sets the player to show the menu for. This is used to determine the IRCUser internally.
    /// </summary>
    public string PlayerName { get; set; }

    /// <summary>
    /// Gets or sets the invite properties are used for the Invite option in the menu.
    /// </summary>
    public string inviteChannelName { get; set; }

    public string inviteGameName { get; set; }

    public string inviteChannelPassword { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether prevent the Join option from showing in the menu.
    /// </summary>
    public bool PreventJoinGame { get; set; }
}