using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Core;

public sealed class GameAssets
{
    private GameAssets(Texture2D atlas, SpriteFont font)
    {
        Atlas = atlas;
        Font = font;
    }

    public Texture2D Atlas { get; }
    public SpriteFont Font { get; }

    public static GameAssets Load(ContentManager content) => new(
        content.Load<Texture2D>("Sprites/FireAtlas"),
        content.Load<SpriteFont>("Fonts/UIFont"));

    public Rectangle Region(AtlasRegion region)
    {
        int column = (int)region % 4;
        int row = (int)region / 4;
        int width = Atlas.Width / 4;
        int y0 = row * Atlas.Height / 3;
        int y1 = (row + 1) * Atlas.Height / 3;
        return new Rectangle(column * width, y0, width, y1 - y0);
    }
}

public enum AtlasRegion
{
    Player, Enemy, Portal, AltarLocked,
    AltarActive, Projectile, Ember, Wall,
    DungeonFloor, HubFloor, RaidFloor, Impact
}
