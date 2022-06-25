using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

internal struct ExtraMapPreviewTexture
{
    public Texture2D Texture;
    public Point Point;
    public bool Toggleable;

    public ExtraMapPreviewTexture(Texture2D texture, Point point, bool toggleable)
    {
        Texture = texture;
        Point = point;
        Toggleable = toggleable;
    }
}