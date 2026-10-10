using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Game.Levels;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Placeholder look of a level room while the registry has no painted art for a slot (change
/// <c>add-level-visual-slots</c>). Flat colours from the biome's palette, no allocations per frame.
/// </summary>
public static class LevelGreyboxRenderer
{
    private const int GridSpacing = 100;
    private const int ExitWidth = 52;
    private const int ExitHeight = 24;
    private const int LightSlitWidth = 8;

    /// <summary>The wall mass around the combat area, with the south gate left open as a dark gap.</summary>
    public static void DrawWall(SpriteBatch batch, Texture2D pixel, Rectangle bounds, Rectangle combat, LevelPalette palette)
    {
        batch.FillRectangle(pixel, bounds, palette.Wall);
        Rectangle gate = new(
            combat.Center.X - GameBalance.RoomSouthGateWidth / 2,
            combat.Bottom,
            GameBalance.RoomSouthGateWidth,
            bounds.Bottom - combat.Bottom);
        batch.FillRectangle(pixel, gate, Color.Black * 0.55f);
    }

    /// <summary>The floor of the combat area with a coarse grid, so movement stays readable on a flat colour.</summary>
    public static void DrawRoom(SpriteBatch batch, Texture2D pixel, Rectangle combat, LevelPalette palette)
    {
        batch.FillRectangle(pixel, combat, palette.Floor);
        for (int x = combat.Left + GridSpacing; x < combat.Right; x += GridSpacing)
        {
            batch.DrawLine(pixel, new Vector2(x, combat.Top), new Vector2(x, combat.Bottom), palette.Grid, 2f);
        }

        for (int y = combat.Top + GridSpacing; y < combat.Bottom; y += GridSpacing)
        {
            batch.DrawLine(pixel, new Vector2(combat.Left, y), new Vector2(combat.Right, y), palette.Grid, 2f);
        }

        batch.DrawRectangle(pixel, combat, palette.Grid, 4f);
    }

    /// <summary>An exit on the north wall: a dark grille while shut, a slit of cold light once open.</summary>
    public static void DrawExit(SpriteBatch batch, Texture2D pixel, Vector2 foot, bool open, float pulse, LevelPalette palette)
    {
        Rectangle gate = new((int)foot.X - ExitWidth / 2, (int)foot.Y - ExitHeight / 2, ExitWidth, ExitHeight);
        if (open)
        {
            batch.FillRectangle(pixel, gate, palette.Wall);
            batch.DrawRectangle(pixel, gate, palette.ExitLight, 2f);
            batch.FillRectangle(
                pixel,
                new Rectangle(gate.Center.X - LightSlitWidth / 2, gate.Top + 3, LightSlitWidth, gate.Height - 6),
                palette.ExitLight * (0.75f + pulse * 0.25f));
            return;
        }

        batch.FillRectangle(pixel, gate, Color.Black * 0.8f);
        batch.DrawRectangle(pixel, gate, palette.Grid, 3f);
        for (int x = gate.Left + 6; x < gate.Right; x += 10)
        {
            batch.DrawLine(pixel, new Vector2(x, gate.Top), new Vector2(x, gate.Bottom), palette.Grid, 2f);
        }
    }
}
