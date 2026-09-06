using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Four concrete authored compositions built from one restrained Death-Layer
/// material kit. This is route art, not a map editor: fixed landmarks and calm
/// combat clearings are intentional and are captured deterministically.
/// </summary>
public static class PrologueEnvironment
{
    private static readonly Color Void = new(6, 5, 11);
    private static readonly Color Deep = new(12, 12, 19);
    private static readonly Color Floor = new(23, 22, 31);
    private static readonly Color FloorLight = new(34, 32, 43);
    private static readonly Color StoneDark = new(27, 27, 36);
    private static readonly Color Stone = new(48, 47, 58);
    private static readonly Color StoneLight = new(76, 73, 86);
    private static readonly Color Iron = new(52, 51, 63);
    private static readonly Color IronLight = new(92, 88, 103);
    private static readonly Color Memory = new(93, 82, 112);

    public static void DrawGround(
        SpriteBatch batch,
        Texture2D pixel,
        PrologueDirector prologue,
        float time,
        float soulSense)
    {
        batch.FillRectangle(pixel, PrologueDirector.WorldBounds, Void);
        switch (prologue.Sector)
        {
            case PrologueSector.Emergence:
                DrawEmergenceGround(batch, pixel, time, soulSense);
                break;
            case PrologueSector.Search:
                DrawSearchGround(batch, pixel, time);
                break;
            case PrologueSector.Escape:
                DrawEscapeGround(batch, pixel, prologue, time);
                break;
            case PrologueSector.Threshold:
                DrawThresholdGround(batch, pixel, time);
                break;
        }
    }

    public static void DrawProps(
        SpriteBatch batch,
        Texture2D pixel,
        PrologueDirector prologue,
        float time,
        float soulSense)
    {
        switch (prologue.Sector)
        {
            case PrologueSector.Emergence:
                DrawMemoryPlatform(batch, pixel, soulSense);
                DrawBrokenRib(batch, pixel, new Vector2(1378f, 236f), 1f);
                DrawWardenMarker(batch, pixel, new Vector2(1535f, 515f), time, 0.62f);
                break;
            case PrologueSector.Search:
                DrawSearchTower(batch, pixel);
                DrawWardenMarker(batch, pixel, new Vector2(548f, 432f), time, 0.74f);
                DrawWardenMarker(batch, pixel, new Vector2(1038f, 652f), time + 0.7f, 0.86f);
                DrawWardenMarker(batch, pixel, PrologueDirector.BrotherMeetingPoint + new Vector2(46f, -12f), time + 1.4f, 1f);
                DrawHumanLuggage(batch, pixel, new Vector2(835f, 720f));
                DrawCollapsedArch(batch, pixel, new Vector2(1285f, 320f));
                break;
            case PrologueSector.Escape:
                if (prologue.IsVehicleRide)
                    DrawVehicle(batch, pixel, time, true);
                else
                {
                    DrawCauseway(batch, pixel);
                    DrawVehicle(batch, pixel, time, false);
                }
                break;
            case PrologueSector.Threshold:
                DrawHomebaseThreshold(batch, pixel, time);
                break;
        }
    }

    public static void DrawForeground(SpriteBatch batch, Texture2D pixel, PrologueDirector prologue)
    {
        Color near = new Color(5, 4, 9) * 0.96f;
        Color edge = new Color(33, 30, 42) * 0.82f;
        batch.FillRectangle(pixel, new Rectangle(0, 0, 1800, 30), near);
        batch.FillRectangle(pixel, new Rectangle(0, 966, 1800, 34), near);
        batch.DrawLine(pixel, new Vector2(0f, 964f), new Vector2(1800f, 964f), edge, 3f);

        if (prologue.Sector == PrologueSector.Emergence)
        {
            batch.DrawLine(pixel, new Vector2(0f, 940f), new Vector2(245f, 842f), near, 54f);
            batch.DrawLine(pixel, new Vector2(1640f, 948f), new Vector2(1800f, 865f), near, 48f);
        }
        else if (prologue.Sector == PrologueSector.Search)
        {
            batch.DrawLine(pixel, new Vector2(70f, 990f), new Vector2(278f, 902f), near, 46f);
            batch.DrawLine(pixel, new Vector2(1525f, 950f), new Vector2(1775f, 910f), near, 52f);
        }
        else if (prologue.Sector == PrologueSector.Escape && prologue.IsVehicleRide)
        {
            batch.FillRectangle(pixel, new Rectangle(390, 720, 1020, 66), near);
            batch.DrawLine(pixel, new Vector2(390f, 720f), new Vector2(1410f, 720f), edge, 5f);
        }
    }

    public static void DrawLight(
        SpriteBatch batch,
        Texture2D brush,
        PrologueDirector prologue,
        float time,
        float soulSense)
    {
        float breathe = 0.84f + MathF.Sin(time * 1.45f) * 0.16f;
        if (prologue.Sector == PrologueSector.Emergence)
        {
            SoftShapes.Blob(batch, brush, PrologueDirector.SoulTrace, 124f * breathe,
                GameBalance.DeepViolet * (0.1f + soulSense * 0.15f));
            SoftShapes.Blob(batch, brush, new Vector2(1535f, 515f), 78f * breathe, GameBalance.DeathFlame * 0.09f);
        }
        else if (prologue.Sector == PrologueSector.Search)
        {
            foreach (Vector2 marker in new[] { new Vector2(548f, 432f), new Vector2(1038f, 652f), PrologueDirector.BrotherMeetingPoint + new Vector2(46f, -12f) })
                SoftShapes.Blob(batch, brush, marker, 72f * breathe, GameBalance.DeathFlameBright * 0.075f);
        }
        else if (prologue.Sector == PrologueSector.Escape)
        {
            Vector2 engine = prologue.IsVehicleRide ? new Vector2(535f, 570f) : new Vector2(1510f, 626f);
            SoftShapes.Blob(batch, brush, engine, 146f * breathe, GameBalance.DeathFlame * 0.12f);
            SoftShapes.Streak(batch, brush, engine, -Vector2.UnitX, 190f, 38f, GameBalance.DeepViolet * 0.13f);
        }
        else
        {
            SoftShapes.Blob(batch, brush, new Vector2(900f, 334f), 260f * breathe, GameBalance.DeathFlame * 0.1f);
            SoftShapes.Blob(batch, brush, new Vector2(900f, 334f), 95f * breathe, GameBalance.DeathFlameBright * 0.08f);
        }
    }

    private static void DrawEmergenceGround(SpriteBatch batch, Texture2D pixel, float time, float sense)
    {
        batch.FillRectangle(pixel, new Rectangle(72, 128, 1656, 752), Deep);
        batch.FillRectangle(pixel, new Rectangle(112, 164, 1576, 674), Floor);
        batch.FillRectangle(pixel, new Rectangle(112, 164, 1576, 22), Stone);
        batch.FillRectangle(pixel, new Rectangle(112, 816, 1576, 22), new Color(9, 8, 14));

        DrawFloorSlabs(batch, pixel, new Rectangle(130, 190, 1540, 610), 7, 4,
            new Color(27, 26, 35), new Color(55, 52, 65));

        // Broad value blocks, not a grid of micro-detail.
        batch.FillRectangle(pixel, new Rectangle(180, 250, 390, 220), FloorLight * 0.42f);
        batch.FillRectangle(pixel, new Rectangle(690, 430, 430, 310), FloorLight * 0.36f);
        batch.FillRectangle(pixel, new Rectangle(1250, 225, 320, 260), FloorLight * 0.3f);
        DrawBrokenSeam(batch, pixel, new Vector2(420f, 210f), new Vector2(690f, 520f));
        DrawBrokenSeam(batch, pixel, new Vector2(1090f, 770f), new Vector2(1390f, 540f));
        DrawRubbleCluster(batch, pixel, new Vector2(210f, 760f), 0.9f);
        DrawRubbleCluster(batch, pixel, new Vector2(1510f, 250f), 0.72f);

        float flicker = 0.5f + 0.5f * MathF.Sin(time * 1.7f);
        batch.FillEllipse(pixel, new Vector2(805f, 560f), 112f, 36f, GameBalance.DeepViolet * ((0.05f + flicker * 0.025f) * (1f + sense)));
    }

    private static void DrawSearchGround(SpriteBatch batch, Texture2D pixel, float time)
    {
        batch.FillRectangle(pixel, new Rectangle(64, 112, 1672, 780), new Color(10, 11, 17));
        batch.FillRectangle(pixel, new Rectangle(105, 150, 1590, 700), new Color(24, 25, 32));
        batch.FillRectangle(pixel, new Rectangle(105, 150, 1590, 26), StoneLight * 0.64f);
        batch.FillRectangle(pixel, new Rectangle(105, 824, 1590, 26), new Color(7, 7, 12));

        DrawFloorSlabs(batch, pixel, new Rectangle(126, 184, 1548, 622), 7, 4,
            new Color(28, 29, 37), new Color(57, 56, 68));

        // The route is a repaired strip through unrelated human fragments.
        batch.FillRectangle(pixel, new Rectangle(180, 420, 1450, 260), new Color(31, 31, 39));
        batch.FillRectangle(pixel, new Rectangle(180, 420, 1450, 8), Iron * 0.72f);
        batch.FillRectangle(pixel, new Rectangle(180, 672, 1450, 8), new Color(10, 10, 15));
        for (int x = 250; x < 1600; x += 230)
        {
            int offset = (x / 230) % 2 * 28;
            batch.FillRectangle(pixel, new Rectangle(x, 446 + offset, 116, 70), FloorLight * 0.36f);
        }
        DrawBrokenSeam(batch, pixel, new Vector2(690f, 410f), new Vector2(790f, 250f));
        DrawBrokenSeam(batch, pixel, new Vector2(1150f, 692f), new Vector2(1280f, 822f));
        DrawRubbleCluster(batch, pixel, new Vector2(620f, 770f), 0.7f);
        DrawRubbleCluster(batch, pixel, new Vector2(1500f, 750f), 0.84f);
    }

    private static void DrawEscapeGround(SpriteBatch batch, Texture2D pixel, PrologueDirector prologue, float time)
    {
        if (!prologue.IsVehicleRide)
        {
            batch.FillRectangle(pixel, new Rectangle(66, 118, 1668, 770), new Color(9, 8, 15));
            batch.FillRectangle(pixel, new Rectangle(105, 155, 1590, 690), new Color(22, 21, 29));
            DrawFloorSlabs(batch, pixel, new Rectangle(126, 180, 1548, 635), 7, 4,
                new Color(27, 26, 35), new Color(54, 51, 64));
            batch.FillRectangle(pixel, new Rectangle(160, 430, 1460, 310), new Color(31, 29, 38));
            batch.FillRectangle(pixel, new Rectangle(160, 430, 1460, 11), IronLight * 0.56f);
            return;
        }

        // Moving landscape: large silhouettes pass at different rates. The deck
        // stays still so player motion and combat remain readable.
        batch.FillRectangle(pixel, new Rectangle(0, 0, 1800, 1000), new Color(7, 6, 13));
        float far = (time * 46f) % 420f;
        float near = (time * 112f) % 520f;
        for (int i = -2; i < 7; i++)
        {
            float x = i * 420f - far;
            batch.DrawLine(pixel, new Vector2(x, 320f), new Vector2(x + 170f, 80f), new Color(22, 20, 31), 74f);
            batch.DrawLine(pixel, new Vector2(x + 170f, 80f), new Vector2(x + 330f, 320f), new Color(22, 20, 31), 74f);
        }
        for (int i = -2; i < 7; i++)
        {
            float x = i * 520f - near;
            batch.DrawLine(pixel, new Vector2(x, 820f), new Vector2(x + 205f, 510f), new Color(14, 12, 21), 105f);
            batch.DrawLine(pixel, new Vector2(x + 205f, 510f), new Vector2(x + 430f, 820f), new Color(14, 12, 21), 105f);
        }
    }

    private static void DrawThresholdGround(SpriteBatch batch, Texture2D pixel, float time)
    {
        batch.FillRectangle(pixel, new Rectangle(0, 0, 1800, 1000), new Color(7, 7, 12));
        batch.FillRectangle(pixel, new Rectangle(102, 170, 1596, 706), new Color(25, 25, 32));
        batch.FillRectangle(pixel, new Rectangle(102, 170, 1596, 18), StoneLight * 0.55f);
        batch.FillRectangle(pixel, new Rectangle(102, 842, 1596, 34), new Color(8, 8, 13));
        DrawFloorSlabs(batch, pixel, new Rectangle(122, 192, 1556, 628), 8, 4,
            new Color(28, 28, 36), new Color(54, 53, 65));
        for (int y = 720; y >= 350; y -= 88)
        {
            int width = 300 + (720 - y) * 2;
            batch.FillRectangle(pixel, new Rectangle(900 - width / 2, y, width, 18), new Color(37, 36, 45));
        }
    }

    private static void DrawMemoryPlatform(SpriteBatch batch, Texture2D pixel, float sense)
    {
        float physical = 1f - sense * 0.35f;
        // One human place surviving as clean, legible chunks: station edge,
        // bench, departure board and a single abandoned case.
        batch.FillRectangle(pixel, new Rectangle(612, 452, 382, 21), Stone * physical);
        batch.FillRectangle(pixel, new Rectangle(612, 473, 382, 11), new Color(10, 9, 15) * physical);
        DrawBench(batch, pixel, new Vector2(760f, 555f), physical);
        batch.FillRectangle(pixel, new Rectangle(892, 308, 172, 91), new Color(20, 20, 27) * physical);
        batch.DrawRectangle(pixel, new Rectangle(892, 308, 172, 91), Iron * physical, 6f);
        batch.FillRectangle(pixel, new Rectangle(912, 330, 132, 12), Memory * (0.28f * physical));
        batch.FillRectangle(pixel, new Rectangle(912, 354, 88, 8), Memory * (0.2f * physical));
        DrawHumanLuggage(batch, pixel, new Vector2(860f, 590f));

        if (sense <= 0.04f) return;
        Color echo = GameBalance.DeathFlameBright * (0.13f * sense);
        for (int i = 0; i < 4; i++)
        {
            Vector2 seated = new(704f + i * 55f, 516f - (i % 2) * 4f);
            batch.FillCircle(pixel, seated - new Vector2(0f, 28f), 8f, echo);
            batch.DrawLine(pixel, seated - new Vector2(0f, 18f), seated + new Vector2(0f, 16f), echo, 7f);
        }
    }

    private static void DrawBench(SpriteBatch batch, Texture2D pixel, Vector2 p, float alpha)
    {
        Color body = new Color(62, 49, 48) * alpha;
        Color edge = new Color(98, 73, 65) * alpha;
        batch.FillRectangle(pixel, new Rectangle((int)p.X - 96, (int)p.Y - 22, 192, 18), body);
        batch.DrawLine(pixel, p + new Vector2(-91f, -21f), p + new Vector2(91f, -21f), edge, 3f);
        batch.DrawLine(pixel, p + new Vector2(-70f, -4f), p + new Vector2(-76f, 24f), Iron, 8f);
        batch.DrawLine(pixel, p + new Vector2(70f, -4f), p + new Vector2(76f, 24f), Iron, 8f);
    }

    private static void DrawHumanLuggage(SpriteBatch batch, Texture2D pixel, Vector2 p)
    {
        batch.FillRectangle(pixel, new Rectangle((int)p.X - 24, (int)p.Y - 30, 48, 42), new Color(47, 35, 38));
        batch.DrawRectangle(pixel, new Rectangle((int)p.X - 24, (int)p.Y - 30, 48, 42), new Color(82, 61, 62), 4f);
        batch.DrawLine(pixel, p + new Vector2(-9f, -31f), p + new Vector2(-9f, -47f), IronLight, 4f);
        batch.DrawLine(pixel, p + new Vector2(-9f, -47f), p + new Vector2(10f, -47f), IronLight, 4f);
    }

    private static void DrawBrokenSeam(SpriteBatch batch, Texture2D pixel, Vector2 a, Vector2 b)
    {
        Vector2 d = b - a;
        batch.DrawLine(pixel, a, b, new Color(7, 6, 11), 8f);
        for (int i = 1; i < 5; i++)
        {
            Vector2 p = a + d * (i / 5f);
            Vector2 branch = new(-d.Y, d.X);
            if (branch.LengthSquared() > 0f) branch.Normalize();
            batch.DrawLine(pixel, p, p + branch * (i % 2 == 0 ? 30f : -24f), new Color(8, 7, 12), 5f);
        }
    }

    private static void DrawBrokenRib(SpriteBatch batch, Texture2D pixel, Vector2 basePoint, float alpha)
    {
        Color dark = StoneDark * alpha;
        Color lit = StoneLight * (0.62f * alpha);
        batch.DrawLine(pixel, basePoint + new Vector2(-130f, 350f), basePoint + new Vector2(-82f, 30f), dark, 46f);
        batch.DrawLine(pixel, basePoint + new Vector2(-82f, 30f), basePoint + new Vector2(0f, -84f), dark, 46f);
        batch.DrawLine(pixel, basePoint + new Vector2(-108f, 344f), basePoint + new Vector2(-62f, 38f), lit, 5f);
        batch.DrawLine(pixel, basePoint + new Vector2(0f, -84f), basePoint + new Vector2(82f, 44f), dark, 42f);
        batch.DrawLine(pixel, basePoint + new Vector2(0f, -76f), basePoint + new Vector2(70f, 42f), lit, 5f);
    }

    private static void DrawWardenMarker(SpriteBatch batch, Texture2D pixel, Vector2 p, float time, float strength)
    {
        float breathe = 0.84f + MathF.Sin(time * 2.4f) * 0.16f;
        batch.FillRectangle(pixel, new Rectangle((int)p.X - 9, (int)p.Y - 58, 18, 72), Iron);
        batch.FillRectangle(pixel, new Rectangle((int)p.X - 5, (int)p.Y - 52, 10, 56), new Color(24, 23, 31));
        batch.FillRectangle(pixel, new Rectangle((int)p.X - 3, (int)p.Y - 45, 6, 35), GameBalance.DeathFlameBright * (0.62f * breathe * strength));
        batch.FillRectangle(pixel, new Rectangle((int)p.X - 17, (int)p.Y + 10, 34, 8), StoneDark);
    }

    private static void DrawSearchTower(SpriteBatch batch, Texture2D pixel)
    {
        Vector2 p = new(282f, 475f);
        batch.DrawLine(pixel, p + new Vector2(-80f, 208f), p + new Vector2(-18f, -128f), StoneDark, 58f);
        batch.DrawLine(pixel, p + new Vector2(34f, 206f), p + new Vector2(-18f, -128f), Stone, 42f);
        batch.DrawLine(pixel, p + new Vector2(-18f, -128f), p + new Vector2(102f, -28f), StoneDark, 38f);
        batch.DrawLine(pixel, p + new Vector2(-5f, -112f), p + new Vector2(86f, -30f), StoneLight * 0.52f, 5f);
        batch.FillCircle(pixel, p + new Vector2(-18f, -128f), 24f, new Color(12, 10, 18));
    }

    private static void DrawCollapsedArch(SpriteBatch batch, Texture2D pixel, Vector2 p)
    {
        batch.DrawLine(pixel, p + new Vector2(-150f, 280f), p + new Vector2(-112f, -86f), StoneDark, 64f);
        batch.DrawLine(pixel, p + new Vector2(154f, 280f), p + new Vector2(112f, -76f), StoneDark, 64f);
        batch.DrawLine(pixel, p + new Vector2(-112f, -86f), p + new Vector2(4f, -178f), Stone, 54f);
        batch.DrawLine(pixel, p + new Vector2(4f, -178f), p + new Vector2(74f, -122f), Stone, 50f);
        batch.DrawLine(pixel, p + new Vector2(74f, -122f), p + new Vector2(112f, -76f), StoneDark, 24f);
        batch.DrawLine(pixel, p + new Vector2(-91f, -78f), p + new Vector2(7f, -151f), StoneLight * 0.55f, 5f);
    }

    private static void DrawCauseway(SpriteBatch batch, Texture2D pixel)
    {
        for (int x = 230; x < 1470; x += 190)
        {
            batch.FillRectangle(pixel, new Rectangle(x, 474 + (x / 190 % 2) * 18, 126, 22), Iron * 0.58f);
            batch.DrawLine(pixel, new Vector2(x + 20f, 496f), new Vector2(x + 8f, 685f), StoneDark, 16f);
        }
        DrawBrokenRib(batch, pixel, new Vector2(610f, 275f), 0.72f);
        DrawRubbleCluster(batch, pixel, new Vector2(1285f, 770f), 0.8f);
    }

    private static void DrawVehicle(SpriteBatch batch, Texture2D pixel, float time, bool riding)
    {
        Vector2 center = riding ? new Vector2(900f, 570f) : new Vector2(1510f, 610f);
        float pulse = 0.84f + MathF.Sin(time * 2.8f) * 0.16f;
        // Contact mass and deck.
        batch.FillEllipse(pixel, center + new Vector2(0f, 88f), riding ? 470f : 180f, riding ? 72f : 48f, new Color(3, 3, 7) * 0.72f);
        int width = riding ? 820 : 330;
        batch.FillRectangle(pixel, new Rectangle((int)center.X - width / 2, (int)center.Y - 54, width, 118), new Color(25, 24, 32));
        batch.DrawLine(pixel, center + new Vector2(-width / 2f, -54f), center + new Vector2(width / 2f, -54f), IronLight, 8f);
        batch.DrawLine(pixel, center + new Vector2(-width / 2f, 64f), center + new Vector2(width / 2f, 64f), new Color(9, 8, 14), 18f);
        batch.DrawLine(pixel, center + new Vector2(-width / 2f + 34f, 50f), center + new Vector2(width / 2f - 55f, 50f), StoneDark, 14f);
        int ribCount = riding ? 7 : 3;
        for (int i = 1; i < ribCount; i++)
        {
            float x = MathHelper.Lerp(center.X - width * 0.43f, center.X + width * 0.38f, i / (float)ribCount);
            batch.DrawLine(pixel, new Vector2(x, center.Y - 47f), new Vector2(x + 16f, center.Y + 48f), Iron * 0.72f, 5f);
        }
        // Prow, engine cage and two Soul Cannon housings.
        Vector2 prowRoot = center + new Vector2(width / 2f - 70f, -48f);
        Vector2 prowTip = center + new Vector2(width / 2f + 90f, 16f);
        batch.DrawLine(pixel, prowRoot, prowTip, Iron, 42f);
        batch.DrawLine(pixel, prowRoot + new Vector2(0f, 40f), prowTip, StoneDark, 30f);
        batch.DrawLine(pixel, prowRoot, prowTip - new Vector2(0f, 18f), IronLight * 0.58f, 5f);
        batch.FillCircle(pixel, center + new Vector2(-width / 2f + 65f, 12f), riding ? 54f : 34f, new Color(13, 10, 20));
        batch.DrawCircle(pixel, center + new Vector2(-width / 2f + 65f, 12f), riding ? 56f : 36f, IronLight * 0.65f, 9f, 20);
        batch.FillCircle(pixel, center + new Vector2(-width / 2f + 65f, 12f), riding ? 22f : 14f, GameBalance.DeepViolet * (0.7f * pulse));
        if (riding)
        {
            for (int i = -3; i <= 3; i++)
            {
                float railX = center.X + i * 108f;
                batch.DrawLine(pixel, new Vector2(railX, center.Y - 54f), new Vector2(railX, center.Y - 92f), Iron, 7f);
            }
            batch.DrawLine(pixel, center + new Vector2(-330f, -90f), center + new Vector2(330f, -90f), IronLight * 0.58f, 5f);
            DrawMountedCannon(batch, pixel, center + new Vector2(-180f, -45f), -1f);
            DrawMountedCannon(batch, pixel, center + new Vector2(180f, -45f), 1f);
        }
    }

    private static void DrawMountedCannon(SpriteBatch batch, Texture2D pixel, Vector2 p, float side)
    {
        batch.FillCircle(pixel, p + new Vector2(0f, 34f), 28f, new Color(18, 17, 24));
        batch.DrawCircle(pixel, p + new Vector2(0f, 34f), 28f, Iron, 7f, 16);
        batch.DrawLine(pixel, p + new Vector2(0f, 12f), p + new Vector2(side * 64f, -20f), IronLight, 17f);
        batch.DrawLine(pixel, p + new Vector2(side * 58f, -18f), p + new Vector2(side * 92f, -30f), Iron, 28f);
    }

    private static void DrawHomebaseThreshold(SpriteBatch batch, Texture2D pixel, float time)
    {
        Vector2 center = new(900f, 380f);
        // The exterior is a city silhouette, not a completed hub.
        for (int i = -4; i <= 4; i++)
        {
            int x = 900 + i * 185;
            int h = 230 + (Math.Abs(i * 37) % 150);
            batch.FillRectangle(pixel, new Rectangle(x - 68, 190 - h / 3, 136, 520 + h / 3), new Color(16, 16, 23));
            batch.DrawLine(pixel, new Vector2(x - 68f, 190f - h / 3f), new Vector2(x, 105f - h / 3f), new Color(32, 31, 40), 22f);
            batch.DrawLine(pixel, new Vector2(x, 105f - h / 3f), new Vector2(x + 68f, 190f - h / 3f), new Color(32, 31, 40), 22f);
            for (int y = 240; y < 690; y += 122)
            {
                batch.FillRectangle(pixel, new Rectangle(x - 68, y, 136, 10), new Color(43, 42, 52) * 0.48f);
            }
        }
        // One monumental threshold and a small human-scale door inside it.
        batch.DrawLine(pixel, center + new Vector2(-260f, 360f), center + new Vector2(-220f, -180f), StoneDark, 92f);
        batch.DrawLine(pixel, center + new Vector2(260f, 360f), center + new Vector2(220f, -180f), StoneDark, 92f);
        batch.DrawLine(pixel, center + new Vector2(-220f, -180f), center + new Vector2(0f, -340f), Stone, 84f);
        batch.DrawLine(pixel, center + new Vector2(0f, -340f), center + new Vector2(220f, -180f), Stone, 84f);
        batch.DrawRectangle(pixel, new Rectangle(792, 390, 216, 330), IronLight * 0.62f, 12f);
        batch.FillRectangle(pixel, new Rectangle(812, 412, 176, 308), new Color(8, 8, 13));
        // Human-scale inset. The surrounding architecture may be inhuman, but
        // Wardens still pass through a door made for shoulders and hands.
        batch.DrawRectangle(pixel, new Rectangle(854, 548, 92, 172), StoneLight * 0.8f, 7f);
        batch.FillRectangle(pixel, new Rectangle(866, 566, 68, 154), new Color(13, 12, 18));
        batch.FillCircle(pixel, new Vector2(922f, 644f), 4f, GameBalance.DeathFlameBright * 0.7f);
        float pulse = 0.8f + MathF.Sin(time * 1.4f) * 0.2f;
        batch.FillRectangle(pixel, new Rectangle(894, 256, 12, 92), GameBalance.DeathFlameBright * (0.62f * pulse));
        DrawWardenMarker(batch, pixel, new Vector2(754f, 660f), time, 0.75f);
        DrawWardenMarker(batch, pixel, new Vector2(1046f, 660f), time + 0.6f, 0.75f);
    }

    /// <summary>
    /// Large hand-set value groups give the floors a pixel-authentic material
    /// cadence without noisy one-pixel texture or painted gradients.
    /// </summary>
    private static void DrawFloorSlabs(
        SpriteBatch batch,
        Texture2D pixel,
        Rectangle area,
        int columns,
        int rows,
        Color body,
        Color seam)
    {
        int cellWidth = area.Width / columns;
        int cellHeight = area.Height / rows;
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int left = area.Left + column * cellWidth + 3;
                int top = area.Top + row * cellHeight + 3;
                int width = cellWidth - 7;
                int height = cellHeight - 7;
                float shift = ((row * 5 + column * 3) % 4) * 0.035f;
                Color slab = Color.Lerp(body, FloorLight, shift);
                batch.FillRectangle(pixel, new Rectangle(left, top, width, height), slab);
                batch.DrawLine(pixel, new Vector2(left, top), new Vector2(left + width, top), seam * 0.34f, 3f);
                batch.DrawLine(pixel, new Vector2(left, top + height), new Vector2(left + width, top + height), new Color(8, 8, 13) * 0.64f, 5f);

                if ((row * 7 + column * 11) % 6 == 0)
                {
                    Vector2 crack = new(left + width * 0.62f, top + height * 0.26f);
                    batch.DrawLine(pixel, crack, crack + new Vector2(-24f, 28f), new Color(10, 9, 15) * 0.7f, 4f);
                    batch.DrawLine(pixel, crack + new Vector2(-24f, 28f), crack + new Vector2(-6f, 51f), new Color(10, 9, 15) * 0.7f, 4f);
                }
            }
        }
    }

    private static void DrawRubbleCluster(SpriteBatch batch, Texture2D pixel, Vector2 p, float scale)
    {
        Color shadow = new Color(6, 6, 10) * 0.65f;
        batch.FillEllipse(pixel, p + new Vector2(8f, 10f), 86f * scale, 24f * scale, shadow);
        for (int i = 0; i < 5; i++)
        {
            int width = (int)((30 + i * 9) * scale);
            int height = (int)((18 + (i % 3) * 8) * scale);
            int x = (int)(p.X + (i - 2) * 31f * scale - width * 0.5f);
            int y = (int)(p.Y - height + Math.Abs(i - 2) * 5f * scale);
            batch.FillRectangle(pixel, new Rectangle(x, y, width, height), i % 2 == 0 ? StoneDark : Stone);
            batch.DrawLine(pixel, new Vector2(x, y), new Vector2(x + width, y), StoneLight * 0.42f, 3f);
        }
    }
}
