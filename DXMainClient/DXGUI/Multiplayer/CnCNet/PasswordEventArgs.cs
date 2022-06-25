using System;
using DTAClient.Domain.Multiplayer.CnCNet;

namespace DTAClient.DXGUI.Multiplayer.CnCNet;

public class PasswordEventArgs : EventArgs
{
    public PasswordEventArgs(string password, HostedCnCNetGame hostedGame)
    {
        Password = password;
        HostedGame = hostedGame;
    }

    /// <summary>
    /// Gets the password input by the user.
    /// </summary>
    public string Password { get; private set; }

    /// <summary>
    /// Gets the game that the user is attempting to join.
    /// </summary>
    public HostedCnCNetGame HostedGame { get; private set; }
}