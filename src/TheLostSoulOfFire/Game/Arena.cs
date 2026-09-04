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
        DrawBackgroundMasses(batch, pixel);
        DrawFloor(batch, pixel);
        DrawWalls(batch, pixel);
        DrawGothicArches(batch, pixel);
        DrawFurnaces(batch, pixel);
        DrawPipes(batch, pixel);
        DrawChains(batch, pixel);
        DrawBrokenMachinery(batch, pixel);
        DrawCentralSeal(batch, pixel);
        DrawForegroundShapes(batch, pixel);
    }

    private static void DrawBackgroundMasses(SpriteBatch batch, Texture2D pixel)
    {
        Color deepestStone = new(12, 12, 18);
        Color distantStone = new(22, 21, 30);

        batch.FillRectangle(pixel, new Rectangle(0, 0, 1800, 116), deepestStone);
        batch.FillRectangle(pixel, new Rectangle(0, 860, 1800, 140), deepestStone);

        // Crooked cathedral ribs sit behind the playable floor.
        batch.DrawLine(pixel, new Vector2(18, 155), new Vector2(122, 42), distantStone, 52f);
        batch.DrawLine(pixel, new Vector2(122, 42), new Vector2(286, 122), distantStone, 52f);
        batch.DrawLine(pixel, new Vector2(1286, 66), new Vector2(1418, 20), distantStone, 46f);
        batch.DrawLine(pixel, new Vector2(1418, 20), new Vector2(1768, 184), distantStone, 46f);

        batch.FillCircle(pixel, new Vector2(425, 74), 78f, new Color(16, 15, 23));
        batch.DrawCircle(pixel, new Vector2(425, 74), 58f, new Color(47, 44, 57), 10f, 32);
        batch.DrawLine(pixel, new Vector2(425, 22), new Vector2(425, 126), new Color(47, 44, 57), 8f);
        batch.DrawLine(pixel, new Vector2(373, 74), new Vector2(477, 74), new Color(47, 44, 57), 8f);

        // A huge dead flywheel reinforces the industrial silhouette without adding collision.
        Vector2 wheel = new(1160, 928);
        batch.DrawCircle(pixel, wheel, 96f, new Color(28, 27, 37), 24f, 36);
        batch.DrawCircle(pixel, wheel, 36f, GameBalance.MetalColor * 0.55f, 10f, 24);
        for (int i = 0; i < 7; i++)
        {
            float angle = MathHelper.TwoPi * i / 7f + 0.18f;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            batch.DrawLine(pixel, wheel + direction * 34f, wheel + direction * 86f, new Color(38, 36, 48), 13f);
        }
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

        DrawCrack(batch, pixel, new Vector2(265, 785), new Vector2(92, -38), 5);
        DrawCrack(batch, pixel, new Vector2(1385, 248), new Vector2(-105, 54), 4);
        DrawCrack(batch, pixel, new Vector2(930, 845), new Vector2(74, -55), 4);

        // Uneven iron plates break up the collision-test grid while preserving readability.
        batch.DrawRectangle(pixel, new Rectangle(305, 530, 166, 118), new Color(48, 45, 57) * 0.52f, 5f);
        batch.DrawLine(pixel, new Vector2(322, 544), new Vector2(452, 631), new Color(58, 54, 67) * 0.42f, 3f);
        batch.DrawRectangle(pixel, new Rectangle(1300, 690, 205, 92), new Color(48, 45, 57) * 0.5f, 5f);
        batch.FillCircle(pixel, new Vector2(1320, 708), 5f, GameBalance.MetalColor * 0.6f);
        batch.FillCircle(pixel, new Vector2(1482, 758), 5f, GameBalance.MetalColor * 0.6f);
    }

    private static void DrawCrack(SpriteBatch batch, Texture2D pixel, Vector2 start, Vector2 direction, int branches)
    {
        Vector2 end = start + direction;
        batch.DrawLine(pixel, start, end, new Color(8, 7, 13), 6f);
        batch.DrawLine(pixel, start, end, GameBalance.DeepViolet * 0.18f, 2f);
        Vector2 tangent = direction.LengthSquared() > 0f
            ? Vector2.Normalize(new Vector2(-direction.Y, direction.X))
            : Vector2.UnitY;
        for (int i = 1; i <= branches; i++)
        {
            float amount = i / (branches + 1f);
            Vector2 branchStart = Vector2.Lerp(start, end, amount);
            float side = i % 2 == 0 ? -1f : 1f;
            Vector2 branchEnd = branchStart + tangent * side * (11f + i * 4f) + direction * 0.08f;
            batch.DrawLine(pixel, branchStart, branchEnd, new Color(10, 8, 15), 3f);
        }
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

    private static void DrawGothicArches(SpriteBatch batch, Texture2D pixel)
    {
        DrawArch(batch, pixel, new Vector2(92, 525), 88f, 244f, false);
        DrawArch(batch, pixel, new Vector2(1698, 354), 105f, 286f, true);
        DrawArch(batch, pixel, new Vector2(890, 84), 124f, 168f, false);

        // Deliberately mismatched buttresses make the arena feel old and rebuilt.
        batch.DrawLine(pixel, new Vector2(115, 390), new Vector2(40, 480), GameBalance.StoneColor, 22f);
        batch.DrawLine(pixel, new Vector2(1678, 232), new Vector2(1768, 330), GameBalance.StoneColor, 28f);
        batch.DrawLine(pixel, new Vector2(771, 91), new Vector2(726, 40), GameBalance.MetalColor * 0.7f, 15f);
        batch.DrawLine(pixel, new Vector2(1012, 91), new Vector2(1084, 28), GameBalance.MetalColor * 0.7f, 15f);
    }

    private static void DrawArch(SpriteBatch batch, Texture2D pixel, Vector2 baseCenter, float radius, float height, bool broken)
    {
        Color stone = new(48, 46, 57);
        float left = baseCenter.X - radius;
        float right = baseCenter.X + radius;
        float shoulder = baseCenter.Y - height + radius;
        batch.DrawLine(pixel, new Vector2(left, baseCenter.Y), new Vector2(left, shoulder), stone, 18f);
        if (!broken)
        {
            batch.DrawLine(pixel, new Vector2(right, baseCenter.Y), new Vector2(right, shoulder), stone, 18f);
        }
        else
        {
            batch.DrawLine(pixel, new Vector2(right, baseCenter.Y), new Vector2(right, shoulder + 74f), stone, 18f);
        }
        batch.DrawLine(pixel, new Vector2(left, shoulder), new Vector2(baseCenter.X, baseCenter.Y - height), stone, 18f);
        batch.DrawLine(pixel, new Vector2(baseCenter.X, baseCenter.Y - height), new Vector2(right - (broken ? 36f : 0f), shoulder + (broken ? 30f : 0f)), stone, 18f);
        batch.DrawLine(pixel, new Vector2(left + 16f, shoulder + 12f), new Vector2(baseCenter.X, baseCenter.Y - height + 27f), new Color(23, 22, 31), 7f);
    }

    private static void DrawFurnaces(SpriteBatch batch, Texture2D pixel)
    {
        DrawFurnace(batch, pixel, new Rectangle(125, 170, 125, 210), false);
        DrawFurnace(batch, pixel, new Rectangle(1515, 570, 130, 230), true);

        batch.DrawArc(pixel, new Vector2(152, 565), 70f, MathHelper.Pi, MathHelper.Pi, GameBalance.MetalColor, 14f, 20);
        batch.FillRectangle(pixel, new Rectangle(82, 565, 140, 150), new Color(25, 24, 33));
        batch.DrawRectangle(pixel, new Rectangle(82, 565, 140, 150), GameBalance.StoneColor, 8f);

        // A squat boiler and its pressure face sit opposite the tall furnaces.
        batch.FillCircle(pixel, new Vector2(1582, 224), 72f, new Color(23, 22, 30));
        batch.DrawCircle(pixel, new Vector2(1582, 224), 72f, GameBalance.MetalColor, 10f, 28);
        batch.DrawCircle(pixel, new Vector2(1582, 224), 28f, GameBalance.StoneColor, 7f, 20);
        batch.DrawLine(pixel, new Vector2(1582, 224), new Vector2(1602, 194), GameBalance.DeathFlame * 0.3f, 4f);
        batch.FillCircle(pixel, new Vector2(1538, 272), 7f, new Color(102, 94, 112));
        batch.FillCircle(pixel, new Vector2(1628, 270), 7f, new Color(102, 94, 112));
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

        DrawPipeRun(batch, pixel, new Vector2(325, 905), new Vector2(325, 825), new Vector2(470, 825), 16f);
        DrawPipeRun(batch, pixel, new Vector2(1285, 95), new Vector2(1285, 182), new Vector2(1394, 182), 13f);
        batch.DrawCircle(pixel, new Vector2(470, 825), 25f, new Color(79, 74, 88), 8f, 18);
        batch.DrawCircle(pixel, new Vector2(1394, 182), 20f, new Color(79, 74, 88), 7f, 18);
    }

    private static void DrawPipeRun(SpriteBatch batch, Texture2D pixel, Vector2 start, Vector2 corner, Vector2 end, float thickness)
    {
        batch.DrawLine(pixel, start, corner, new Color(29, 28, 37), thickness + 6f);
        batch.DrawLine(pixel, corner, end, new Color(29, 28, 37), thickness + 6f);
        batch.DrawLine(pixel, start, corner, GameBalance.MetalColor, thickness);
        batch.DrawLine(pixel, corner, end, GameBalance.MetalColor, thickness);
    }

    private static void DrawChains(SpriteBatch batch, Texture2D pixel)
    {
        DrawChain(batch, pixel, new Vector2(535, 58), 9, 20f, 0.12f);
        DrawChain(batch, pixel, new Vector2(1128, 62), 6, 22f, -0.18f);
        DrawChain(batch, pixel, new Vector2(1660, 355), 8, 18f, 0.25f);
    }

    private static void DrawChain(SpriteBatch batch, Texture2D pixel, Vector2 start, int links, float spacing, float drift)
    {
        Vector2 position = start;
        for (int i = 0; i < links; i++)
        {
            float xOffset = MathF.Sin(i * 0.9f) * drift * spacing;
            Vector2 center = position + new Vector2(xOffset, i * spacing);
            batch.DrawCircle(pixel, center, 8f, new Color(72, 68, 81) * 0.72f, 3f, 12);
            if (i > 0)
            {
                batch.DrawLine(pixel, center - new Vector2(0f, spacing * 0.55f), center - new Vector2(0f, spacing * 0.2f), new Color(72, 68, 81) * 0.72f, 3f);
            }
        }
    }

    private static void DrawBrokenMachinery(SpriteBatch batch, Texture2D pixel)
    {
        Vector2 hub = new(595, 188);
        batch.DrawCircle(pixel, hub, 42f, new Color(50, 47, 59), 10f, 18);
        batch.FillCircle(pixel, hub, 10f, new Color(84, 78, 93));
        for (int i = 0; i < 5; i++)
        {
            float angle = MathHelper.TwoPi * i / 6f + 0.35f;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            batch.DrawLine(pixel, hub + direction * 15f, hub + direction * (i == 2 ? 28f : 48f), new Color(58, 54, 66), 11f);
        }

        batch.FillRectangle(pixel, new Rectangle(1422, 838, 136, 54), new Color(22, 21, 29));
        batch.DrawRectangle(pixel, new Rectangle(1422, 838, 136, 54), GameBalance.MetalColor, 7f);
        batch.DrawLine(pixel, new Vector2(1434, 851), new Vector2(1492, 887), new Color(87, 80, 96), 8f);
        batch.DrawLine(pixel, new Vector2(1492, 887), new Vector2(1549, 844), new Color(87, 80, 96), 8f);
        batch.FillCircle(pixel, new Vector2(1436, 851), 6f, GameBalance.DeepViolet * 0.45f);
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

    private static void DrawForegroundShapes(SpriteBatch batch, Texture2D pixel)
    {
        Color silhouette = new Color(10, 9, 15) * 0.82f;
        batch.FillRectangle(pixel, new Rectangle(0, 890, 330, 110), silhouette);
        batch.DrawLine(pixel, new Vector2(205, 920), new Vector2(338, 845), silhouette, 34f);
        batch.FillRectangle(pixel, new Rectangle(1535, 918, 265, 82), silhouette);
        batch.DrawLine(pixel, new Vector2(1548, 944), new Vector2(1488, 872), silhouette, 30f);
        batch.DrawLine(pixel, new Vector2(72, 945), new Vector2(42, 865), new Color(51, 47, 60), 9f);
        batch.DrawLine(pixel, new Vector2(1712, 963), new Vector2(1760, 876), new Color(51, 47, 60), 10f);
    }

}
