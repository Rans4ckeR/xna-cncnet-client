using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

internal class StarDisplay : XNAControl
{
    private readonly Texture2D[] rankTextures;

    public StarDisplay(WindowManager windowManager, Texture2D[] rankTextures)
        : base(windowManager)
    {
        Name = "StarDisplay";
        this.rankTextures = rankTextures;
        Width = rankTextures[1].Width;
        Height = rankTextures[1].Height;
    }

    public int Rank { get; set; }

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Draw(GameTime gameTime)
    {
        DrawTexture(rankTextures[Rank], Point.Zero, Color.White);
        base.Draw(gameTime);
    }
}