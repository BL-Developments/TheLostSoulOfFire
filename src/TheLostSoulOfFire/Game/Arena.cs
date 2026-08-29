using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Game;

public sealed class Arena
{
    public Rectangle Bounds => GameBalance.ArenaBounds;
    public Rectangle CombatBounds => GameBalance.CombatBounds;

    public void Draw(SpriteBatch batch, Texture2D pixel)
    {
        batch.FillRectangle(pixel, Bounds, GameBalance.FloorColor);
        DrawFloor(batch, pixel);
        DrawWalls(batch, pixel);
        DrawFurnaces(batch, pixel);
        DrawPipes(batch, pixel);
        DrawCentralSeal(batch, pixel);
    }

    private static void DrawFloor(SpriteBatch batch, Texture2D pixel)
    {
        Rectangle combat = GameBalance.CombatBounds;
        for (int x = combat.Left; x <= combat.Right; x += 100)
        {
            batch.DrawLine(pixel, new Vector2(x, combat.Top), new Vector2(x, combat.Bottom), GameBalance.FloorDetailColor * 0.55f, 2f);
        }

        for (int y = combat.Top; y <= combat.Bottom; y += 100)
        {
            batch.DrawLine(pixel, new Vector2(combat.Left, y), new Vector2(combat.Right, y), GameBalance.FloorDetailColor * 0.55f, 2f);
        }

        batch.DrawLine(pixel, new Vector2(340, 260), new Vector2(510, 325), GameBalance.DeepViolet * 0.35f, 4f);
        batch.DrawLine(pixel, new Vector2(510, 325), new Vector2(575, 455), GameBalance.DeepViolet * 0.35f, 4f);
        batch.DrawLine(pixel, new Vector2(1310, 610), new Vector2(1440, 555), GameBalance.DeepViolet * 0.3f, 4f);
    }

    private static void DrawWalls(SpriteBatch batch, Texture2D pixel)
    {
        Rectangle combat = GameBalance.CombatBounds;
        batch.DrawRectangle(pixel, combat, GameBalance.MetalColor, 12f);
        batch.DrawRectangle(pixel, new Rectangle(54, 45, 1692, 910), GameBalance.StoneColor, 42f);

        for (int x = 180; x < 1700; x += 210)
        {
            batch.FillRectangle(pixel, new Rectangle(x, 55, 38, 38), GameBalance.MetalColor);
            batch.FillCircle(pixel, new Vector2(x + 19, 74), 6f, new Color(114, 105, 129));
            batch.FillRectangle(pixel, new Rectangle(x + 70, 907, 48, 30), GameBalance.StoneColor);
        }
    }

    private static void DrawFurnaces(SpriteBatch batch, Texture2D pixel)
    {
        DrawFurnace(batch, pixel, new Rectangle(125, 170, 125, 210), false);
        DrawFurnace(batch, pixel, new Rectangle(1515, 570, 130, 230), true);

        batch.DrawArc(pixel, new Vector2(152, 565), 70f, MathHelper.Pi, MathHelper.Pi, GameBalance.MetalColor, 14f, 20);
        batch.FillRectangle(pixel, new Rectangle(82, 565, 140, 150), new Color(25, 24, 33));
        batch.DrawRectangle(pixel, new Rectangle(82, 565, 140, 150), GameBalance.StoneColor, 8f);
    }

    private static void DrawFurnace(SpriteBatch batch, Texture2D pixel, Rectangle body, bool bright)
    {
        batch.FillRectangle(pixel, body, new Color(25, 24, 31));
        batch.DrawRectangle(pixel, body, GameBalance.MetalColor, 8f);
        Rectangle mouth = new(body.X + 26, body.Y + 68, body.Width - 52, body.Height - 105);
        batch.FillRectangle(pixel, mouth, new Color(8, 7, 13));
        batch.DrawRectangle(pixel, mouth, GameBalance.StoneColor, 6f);
        Color ember = bright ? GameBalance.DeepViolet * 0.34f : GameBalance.DeepViolet * 0.2f;
        batch.FillCircle(pixel, mouth.Center.ToVector2(), 16f, ember);
    }

    private static void DrawPipes(SpriteBatch batch, Texture2D pixel)
    {
        batch.DrawLine(pixel, new Vector2(270, 95), new Vector2(270, 205), GameBalance.MetalColor, 18f);
        batch.DrawLine(pixel, new Vector2(270, 205), new Vector2(355, 205), GameBalance.MetalColor, 18f);
        batch.DrawLine(pixel, new Vector2(1460, 905), new Vector2(1460, 770), GameBalance.MetalColor, 22f);
        batch.DrawLine(pixel, new Vector2(1460, 770), new Vector2(1550, 770), GameBalance.MetalColor, 22f);

        for (int i = 0; i < 5; i++)
        {
            float x = 595 + i * 150;
            batch.DrawLine(pixel, new Vector2(x, 95), new Vector2(x, 130 + (i % 2) * 32), GameBalance.StoneColor, 5f);
            batch.FillCircle(pixel, new Vector2(x, 138 + (i % 2) * 32), 8f, GameBalance.MetalColor);
        }
    }

    private static void DrawCentralSeal(SpriteBatch batch, Texture2D pixel)
    {
        Vector2 center = GameBalance.CombatBounds.Center.ToVector2();
        batch.DrawCircle(pixel, center, 108f, GameBalance.FloorDetailColor, 5f, 48);
        batch.DrawCircle(pixel, center, 76f, GameBalance.DeepViolet * 0.28f, 3f, 32);
        for (int i = 0; i < 8; i++)
        {
            float angle = MathHelper.TwoPi * i / 8f;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            batch.DrawLine(pixel, center + direction * 76f, center + direction * 100f, GameBalance.FloorDetailColor, 4f);
        }
    }
}
