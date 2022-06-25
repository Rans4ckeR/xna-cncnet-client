using System;
using ClientGUI;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

public class GameLaunchButton : XNAClientButton
{
    private StarDisplay starDisplay;

    public GameLaunchButton(WindowManager windowManager)
        : base(windowManager)
    {
    }

    public override string Text
    {
        get => base.Text;
        set
        {
            base.Text = value;
            UpdateStarPosition();
        }
    }

    public void InitStarDisplay(Texture2D[] rankTextures)
    {
        if (starDisplay != null)
            throw new InvalidOperationException("The star display is already initialized!");

        starDisplay = new StarDisplay(WindowManager, rankTextures)
        {
            InputEnabled = false
        };
        AddChild(starDisplay);
        ClientRectangleUpdated += (e, sender) => UpdateStarPosition();
        UpdateStarPosition();
    }

    public override void Initialize()
    {
        base.Initialize();
    }

    public void SetRank(int rank)
    {
        starDisplay.Rank = rank;
        UpdateStarPosition();
    }

    private void UpdateStarPosition()
    {
        if (starDisplay == null)
            return;

        starDisplay.Y = (Height - starDisplay.Height) / 2;
        starDisplay.X = (Width / 2) + (int)(Renderer.GetTextDimensions(Text, FontIndex).X / 2) + 3;
    }
}