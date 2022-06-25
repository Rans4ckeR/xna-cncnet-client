using Microsoft.Xna.Framework;

namespace DTAClient.Domain.Multiplayer;

public struct ExtraMapPreviewTexture
{
    public ExtraMapPreviewTexture(string textureName, Point point, int level, bool toggleable)
    {
        TextureName = textureName;
        Point = point;
        Level = level;
        Toggleable = toggleable;
    }

    public int Level { get; set; }

    public Point Point { get; set; }

    public string TextureName { get; set; }

    public bool Toggleable { get; set; }
}