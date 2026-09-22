using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Core;
using TheLostSoulOfFire.Levels;

namespace TheLostSoulOfFire.Presentation;

public static class SceneRenderer
{
    public static void DrawLevel(SpriteBatch spriteBatch, LevelRuntime level, AtlasRegion floorRegion)
    {
        spriteBatch.Draw(GameCore.Assets.Atlas, level.Bounds, GameCore.Assets.Region(floorRegion), Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.95f);
        Rectangle wallSource = GameCore.Assets.Region(AtlasRegion.Wall);
        for (int i = 0; i < level.Walls.Length; i++)
        {
            spriteBatch.Draw(GameCore.Assets.Atlas, level.Walls[i], wallSource, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0.75f);
        }
    }

    public static void DrawAtlas(SpriteBatch spriteBatch, AtlasRegion region, Rectangle destination, float layer, Color color)
    {
        spriteBatch.Draw(GameCore.Assets.Atlas, destination, GameCore.Assets.Region(region), color, 0f, Vector2.Zero, SpriteEffects.None, layer);
    }

    public static void DrawCenteredText(SpriteBatch spriteBatch, string text, float y, Color color)
    {
        Vector2 size = GameCore.Assets.Font.MeasureString(text);
        Vector2 position;
        position.X = (GameCore.Resolution.VirtualWidth - size.X) * 0.5f;
        position.Y = y;
        spriteBatch.DrawString(GameCore.Assets.Font, text, position + Vector2.One, Color.Black);
        spriteBatch.DrawString(GameCore.Assets.Font, text, position, color);
    }
}
