using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Authored, static composition for the Abandoned Soul Furnace: the recessed
/// casting basin, its landmark and the grounded props that give the combat plane
/// height, scale hierarchy and local light.
///
/// This is deliberately one concrete arena, not a level framework. Everything is
/// drawn from fixed world coordinates so the frame can be judged and re-tuned
/// directly, and nothing here participates in collision or gameplay state.
/// </summary>
public static class ArenaComposition
{
    private static readonly Color KerbTop = new(74, 70, 86);
    private static readonly Color KerbFace = new(34, 32, 42);
    private static readonly Color KerbShadow = new(6, 5, 10);
    private static readonly Color IronDark = new(23, 22, 30);
    private static readonly Color IronBody = new(41, 39, 50);
    private static readonly Color IronLit = new(86, 81, 98);
    private static readonly Color IronRim = new(126, 119, 140);
    private static readonly Color GroundShadow = new(4, 4, 8);
    private static readonly Color SlagCold = new(40, 24, 66);

    // The single memorable landmark. Everything else defers to it in size and light.
    public static readonly Vector2 LadleBase = new(286f, 662f);
    public static readonly Vector2 LadleMouth = new(556f, 494f);

    // Soul lamps: the only warm-cold local lights that touch the casting floor.
    private static readonly Vector2[] SoulLamps =
    [
        new(1206f, 168f),
        new(1620f, 566f),
        new(742f, 872f)
    ];

    /// <summary>
    /// Drawn after the floor material and before any actor. Establishes the
    /// recessed basin, light pools and prop contact shadows.
    /// </summary>
    public static void DrawGround(SpriteBatch batch, Texture2D pixel, float time, float soulSenseAmount)
    {
        float physical = MathHelper.Lerp(1f, 0.4f, MathHelper.Clamp(soulSenseAmount, 0f, 1f));
        DrawBasinKerb(batch, pixel, physical);
        DrawLightPools(batch, pixel, time, physical);
        DrawGroundDecals(batch, pixel, physical);
    }

    /// <summary>
    /// Props with visible height. Drawn before actors so the fighting plane always
    /// reads on top of the environment.
    /// </summary>
    public static void DrawProps(SpriteBatch batch, Texture2D pixel, float time, float soulSenseAmount)
    {
        float physical = MathHelper.Lerp(1f, 0.42f, MathHelper.Clamp(soulSenseAmount, 0f, 1f));
        DrawIngotStack(batch, pixel, new Vector2(1602f, 232f), physical);
        DrawIngotStack(batch, pixel, new Vector2(1520f, 176f), physical * 0.86f);
        DrawTippedCart(batch, pixel, new Vector2(1548f, 792f), physical);
        DrawPillarDrum(batch, pixel, new Vector2(686f, 878f), physical);
        DrawPillarDrum(batch, pixel, new Vector2(212f, 262f), physical * 0.9f);
        DrawFallenLadle(batch, pixel, time, physical);
        foreach (Vector2 lamp in SoulLamps)
        {
            DrawSoulLamp(batch, pixel, lamp, time, physical);
        }
    }

    /// <summary>
    /// A restrained foreground frame. It never crosses the combat bounds, so it
    /// cannot hide a telegraph, but it gives the camera something nearer than the
    /// action at the top and bottom of the world.
    /// </summary>
    public static void DrawForeground(SpriteBatch batch, Texture2D pixel)
    {
        Color near = new Color(5, 4, 9) * 0.95f;
        Color nearEdge = new Color(28, 26, 35) * 0.9f;

        // Upper gantry, broken and asymmetric.
        batch.FillRectangle(pixel, new Rectangle(0, 0, 1800, 34), near);
        batch.DrawLine(pixel, new Vector2(0f, 36f), new Vector2(612f, 36f), near, 22f);
        batch.DrawLine(pixel, new Vector2(0f, 26f), new Vector2(612f, 26f), nearEdge, 3f);
        batch.DrawLine(pixel, new Vector2(742f, 40f), new Vector2(1800f, 30f), near, 26f);
        batch.DrawLine(pixel, new Vector2(742f, 28f), new Vector2(1800f, 18f), nearEdge, 3f);
        batch.DrawLine(pixel, new Vector2(612f, 36f), new Vector2(690f, 74f), near, 16f);

        // Lower rim of the casting hall.
        batch.FillRectangle(pixel, new Rectangle(0, 962, 1800, 38), near);
        batch.DrawLine(pixel, new Vector2(0f, 958f), new Vector2(1800f, 958f), nearEdge, 3f);
        batch.DrawLine(pixel, new Vector2(226f, 962f), new Vector2(318f, 916f), near, 30f);
        batch.DrawLine(pixel, new Vector2(1402f, 962f), new Vector2(1478f, 922f), near, 26f);
    }

    /// <summary>
    /// Static environment light. Small, few and motivated by visible sources.
    /// </summary>
    public static void DrawLighting(SpriteBatch batch, SoulfireRenderer renderer, float time, float soulSenseAmount)
    {
        float physical = MathHelper.Lerp(1f, 0.34f, MathHelper.Clamp(soulSenseAmount, 0f, 1f));
        float breathe = 0.86f + MathF.Sin(time * 1.35f) * 0.14f;

        renderer.DrawGlow(batch, LadleMouth, 210f * breathe, GameBalance.DeathFlame, 0.088f * physical);
        renderer.DrawGlow(batch, LadleMouth, 92f * breathe, GameBalance.DeathFlameBright, 0.055f * physical);

        for (int i = 0; i < SoulLamps.Length; i++)
        {
            float flicker = 0.82f + MathF.Sin(time * (2.1f + i * 0.37f) + i * 1.9f) * 0.18f;
            renderer.DrawGlow(batch, SoulLamps[i], 132f * flicker, GameBalance.DeathFlame, 0.052f * physical);
            renderer.DrawGlow(batch, SoulLamps[i], 44f * flicker, GameBalance.DeathFlameBright, 0.04f * physical);
        }
    }

    /// <summary>
    /// Soft actor contact shadow. Two layers so characters sit on the floor
    /// instead of hovering, and so they stay grounded during knockback.
    /// </summary>
    public static void DrawContactShadow(SpriteBatch batch, Texture2D pixel, Vector2 footPosition, float width)
    {
        batch.FillEllipse(pixel, footPosition + new Vector2(3f, 2f), width * 1.42f, width * 0.4f, GroundShadow * 0.3f);
        batch.FillEllipse(pixel, footPosition, width, width * 0.27f, GroundShadow * 0.62f);
        batch.FillEllipse(pixel, footPosition, width * 0.52f, width * 0.16f, GroundShadow * 0.72f);
    }

    private static void DrawBasinKerb(SpriteBatch batch, Texture2D pixel, float physical)
    {
        Vector2 center = new(ArenaFloorTuning.BasinCenterX, ArenaFloorTuning.BasinCenterY);
        const float RadiusX = 726f;
        const float RadiusY = 376f;

        // The basin is sunk. Only its inward shadow is drawn: a bright painted
        // ring would read as a decal on the floor rather than as depth.
        for (int i = 0; i < 7; i++)
        {
            float inset = i * 13f;
            float fade = 1f - i / 7f;
            DrawEllipseOutline(
                batch,
                pixel,
                center,
                RadiusX - inset,
                RadiusY - inset * 0.62f,
                KerbShadow * (0.26f * physical * fade * fade),
                17f);
        }

        // A short kerb face only where the rim reads against the painted border.
        DrawEllipseOutline(batch, pixel, center, RadiusX + 12f, RadiusY + 9f, KerbFace * (0.55f * physical), 22f);
        DrawEllipseOutline(batch, pixel, center, RadiusX + 24f, RadiusY + 17f, KerbTop * (0.2f * physical), 4f);
    }

    private static void DrawLightPools(SpriteBatch batch, Texture2D pixel, float time, float physical)
    {
        float breathe = 0.88f + MathF.Sin(time * 1.35f) * 0.12f;
        DrawPool(batch, pixel, LadleMouth + new Vector2(28f, 86f), 268f * breathe, 122f * breathe, new Color(96, 62, 150), 0.2f * physical);
        DrawPool(batch, pixel, LadleMouth + new Vector2(10f, 30f), 132f * breathe, 62f * breathe, new Color(132, 92, 196), 0.16f * physical);

        for (int i = 0; i < SoulLamps.Length; i++)
        {
            float flicker = 0.85f + MathF.Sin(time * (2.1f + i * 0.37f) + i * 1.9f) * 0.15f;
            DrawPool(batch, pixel, SoulLamps[i] + new Vector2(0f, 46f), 138f * flicker, 62f * flicker, new Color(86, 56, 138), 0.15f * physical);
        }
    }

    private static void DrawGroundDecals(SpriteBatch batch, Texture2D pixel, float physical)
    {
        // Dragged ash trails and old spill stains, aligned to the pour direction.
        Color ash = new Color(58, 55, 66) * (0.16f * physical);
        batch.DrawLine(pixel, new Vector2(468f, 704f), new Vector2(742f, 762f), ash, 26f);
        batch.DrawLine(pixel, new Vector2(1210f, 806f), new Vector2(1424f, 748f), ash, 20f);
        batch.DrawLine(pixel, new Vector2(1352f, 268f), new Vector2(1512f, 236f), ash, 17f);

        // Old spill marks are edges only. Filled ellipses were inspected in the
        // frame and read as dropped shadows under invisible objects.
        Color stain = new Color(14, 12, 19) * (0.5f * physical);
        DrawStain(batch, pixel, new Vector2(536f, 340f), 82f, 28f, stain);
        DrawStain(batch, pixel, new Vector2(1318f, 618f), 64f, 22f, stain);
        DrawStain(batch, pixel, new Vector2(884f, 726f), 98f, 32f, stain);
    }

    /// <summary>
    /// The Fallen Ladle. A toppled Soul-iron casting vessel: the largest object
    /// in the room, the reason the west side of the basin is lit, and the fixed
    /// landmark the encounter stages itself against. It is built as an oriented
    /// volume with a real rim and cavity so it reads as heavy iron, not a decal.
    /// </summary>
    private static void DrawFallenLadle(SpriteBatch batch, Texture2D pixel, float time, float physical)
    {
        Vector2 heel = LadleBase;
        Vector2 mouth = LadleMouth;
        Vector2 axis = Vector2.Normalize(mouth - heel);
        Vector2 side = new(-axis.Y, axis.X);
        float length = Vector2.Distance(heel, mouth);

        Color dark = IronDark * physical;
        Color body = IronBody * physical;
        Color lit = IronLit * physical;
        Color rim = IronRim * physical;

        // Contact shadow. The vessel is half-sunk, so the shadow is tight and offset.
        batch.FillEllipse(pixel, (heel + mouth) * 0.5f + new Vector2(30f, 34f), 214f, 74f, GroundShadow * (0.4f * physical));
        batch.FillEllipse(pixel, (heel + mouth) * 0.5f + new Vector2(12f, 16f), 172f, 54f, GroundShadow * (0.58f * physical));

        const int Slices = 64;
        float step = length / Slices + 4f;

        // Iron body. Dark by default; light only lands on the upper edge and near
        // the glowing mouth, which is what makes the form read as a cylinder.
        for (int i = 0; i <= Slices; i++)
        {
            float amount = i / (float)Slices;
            Vector2 slice = heel + axis * (length * amount);
            float halfWidth = Profile(amount);
            float toward = MathF.Pow(amount, 2.2f);

            batch.DrawLine(pixel, slice - side * halfWidth, slice + side * halfWidth, Color.Lerp(dark, body, 0.35f + toward * 0.3f), step);
            batch.DrawLine(pixel, slice + side * (halfWidth - 30f), slice + side * halfWidth, GroundShadow * (0.42f * physical), step);
            batch.DrawLine(
                pixel,
                slice - side * halfWidth,
                slice - side * (halfWidth - 13f),
                Color.Lerp(lit * 0.55f, rim * 0.7f, toward),
                step);
            if (toward > 0.34f)
            {
                batch.DrawLine(
                    pixel,
                    slice - side * (halfWidth - 12f),
                    slice - side * (halfWidth - 22f),
                    GameBalance.DeepViolet * ((toward - 0.34f) * 0.5f * physical),
                    step);
            }
        }

        // Cast reinforcement bands.
        for (int i = 0; i < 3; i++)
        {
            float amount = 0.2f + i * 0.23f;
            Vector2 band = heel + axis * (length * amount);
            float halfWidth = Profile(amount);
            batch.DrawLine(pixel, band - side * halfWidth, band + side * halfWidth, dark, 9f);
            batch.DrawLine(pixel, band - side * halfWidth, band - side * (halfWidth * 0.2f), lit * 0.5f, 3f);
        }

        // Trunnions: the pivots the ladle hung from, now bent into the floor.
        DrawTrunnion(batch, pixel, heel + axis * (length * 0.44f) - side * (Profile(0.44f) + 12f), lit, dark, physical);
        DrawTrunnion(batch, pixel, heel + axis * (length * 0.44f) + side * (Profile(0.44f) + 12f), dark, dark, physical);

        // Heel cap so the vessel is closed at the far end.
        DrawOrientedEllipse(batch, pixel, heel + axis * 6f, axis, side, Profile(0f), 26f, dark, true);

        // Mouth: rim, cavity and the cold violet pour still held inside.
        float glow = 0.5f + 0.5f * MathF.Sin(time * 1.55f);
        float mouthHalf = Profile(1f);
        DrawOrientedEllipse(batch, pixel, mouth + axis * 4f, axis, side, mouthHalf + 12f, 40f, rim * 0.38f, false);
        DrawOrientedEllipse(batch, pixel, mouth + axis * 7f, axis, side, mouthHalf + 6f, 36f, dark, false);
        DrawOrientedEllipse(batch, pixel, mouth + axis * 4f, axis, side, mouthHalf, 34f, new Color(9, 8, 14) * physical, true);
        DrawOrientedEllipse(
            batch,
            pixel,
            mouth + axis * 2f,
            axis,
            side,
            mouthHalf * 0.72f,
            23f,
            GameBalance.DeepViolet * ((0.66f + glow * 0.22f) * physical),
            true);
        DrawOrientedEllipse(
            batch,
            pixel,
            mouth,
            axis,
            side,
            mouthHalf * 0.34f,
            11f,
            GameBalance.DeathFlame * ((0.46f + glow * 0.26f) * physical),
            true);

        // Cooled crust floating on the residue. Death Flame here is spent, not molten.
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 1.87f + 0.4f;
            Vector2 crust = mouth +
                side * (MathF.Cos(angle) * mouthHalf * 0.46f) +
                axis * (MathF.Sin(angle) * 14f + 2f);
            batch.FillEllipse(pixel, crust, 13f + (i % 3) * 5f, 6f + (i % 2) * 3f, new Color(16, 13, 22) * (0.88f * physical));
        }

        // The cooled spill running from the lip into the pour channel.
        Vector2 spillEnd = new(392f, 546f);
        for (int i = 0; i <= 12; i++)
        {
            float amount = i / 12f;
            Vector2 point = Vector2.Lerp(mouth - side * (mouthHalf * 0.55f), spillEnd, amount);
            float width = MathHelper.Lerp(26f, 58f, amount);
            batch.FillEllipse(
                pixel,
                point,
                width,
                width * 0.4f,
                Color.Lerp(GameBalance.DeepViolet, SlagCold, MathF.Pow(amount, 0.6f)) * ((0.2f - amount * 0.08f) * physical));
        }

        // Broken hoist above the vessel: vertical interest without blocking play.
        batch.DrawLine(pixel, new Vector2(506f, 196f), new Vector2(534f, 372f), dark, 15f);
        batch.DrawLine(pixel, new Vector2(509f, 196f), new Vector2(537f, 372f), lit * 0.4f, 3f);
        batch.DrawCircle(pixel, new Vector2(536f, 388f), 21f, lit * 0.56f, 6f, 16);
        for (int i = 0; i < 5; i++)
        {
            Vector2 link = new(538f + MathF.Sin(i * 0.9f) * 4f, 406f + i * 17f);
            batch.DrawCircle(pixel, link, 7f, lit * 0.44f, 3f, 10);
        }
    }

    private static float Profile(float amount) =>
        70f + MathF.Sin(MathHelper.Clamp(amount, 0f, 1f) * MathHelper.Pi * 0.82f + 0.28f) * 42f;

    private static void DrawTrunnion(SpriteBatch batch, Texture2D pixel, Vector2 center, Color face, Color edge, float physical)
    {
        batch.FillEllipse(pixel, center + new Vector2(4f, 6f), 30f, 12f, GroundShadow * (0.4f * physical));
        batch.DrawCircle(pixel, center, 27f, edge, 11f, 18);
        batch.DrawCircle(pixel, center, 27f, face * 0.8f, 4f, 18);
        batch.FillCircle(pixel, center, 9f, edge);
    }

    private static void DrawOrientedEllipse(
        SpriteBatch batch,
        Texture2D pixel,
        Vector2 center,
        Vector2 axis,
        Vector2 side,
        float halfWidth,
        float halfDepth,
        Color color,
        bool filled)
    {
        const int Segments = 30;
        Vector2 previous = center + side * halfWidth;
        for (int i = 1; i <= Segments; i++)
        {
            float angle = MathHelper.TwoPi * i / Segments;
            Vector2 next = center + side * (MathF.Cos(angle) * halfWidth) + axis * (MathF.Sin(angle) * halfDepth);
            if (filled)
            {
                batch.DrawLine(pixel, center, next, color, MathHelper.Max(3f, halfWidth * 0.14f));
            }
            batch.DrawLine(pixel, previous, next, color, filled ? 4f : 6f);
            previous = next;
        }
    }

    private static void DrawStain(SpriteBatch batch, Texture2D pixel, Vector2 center, float width, float height, Color color)
    {
        Vector2 previous = Vector2.Zero;
        for (int i = 0; i <= 13; i++)
        {
            float angle = MathHelper.TwoPi * i / 13f;
            float wobble = 0.74f + MathF.Sin(angle * 3f + center.X * 0.02f) * 0.26f;
            Vector2 point = center + new Vector2(
                MathF.Cos(angle) * width * wobble,
                MathF.Sin(angle) * height * wobble);
            if (i > 0)
            {
                batch.DrawLine(pixel, previous, point, color, 4f);
            }
            previous = point;
        }
    }

    private static void DrawIngotStack(SpriteBatch batch, Texture2D pixel, Vector2 baseCenter, float physical)
    {
        batch.FillEllipse(pixel, baseCenter + new Vector2(8f, 10f), 96f, 27f, GroundShadow * (0.5f * physical));

        for (int i = 0; i < 4; i++)
        {
            float lift = i * 26f;
            float half = 78f - i * 9f;
            Vector2 top = baseCenter - new Vector2(i * 5f, lift + 26f);
            batch.FillRectangle(
                pixel,
                new Rectangle((int)(top.X - half), (int)top.Y, (int)(half * 2f), 30),
                (IronBody * physical) with { A = 255 });
            batch.DrawLine(pixel, top - new Vector2(half, 0f), top + new Vector2(half, 0f), IronLit * (0.66f * physical), 4f);
            batch.DrawLine(
                pixel,
                top + new Vector2(-half, 28f),
                top + new Vector2(half, 28f),
                IronDark * physical,
                6f);
        }
    }

    private static void DrawTippedCart(SpriteBatch batch, Texture2D pixel, Vector2 baseCenter, float physical)
    {
        batch.FillEllipse(pixel, baseCenter + new Vector2(10f, 8f), 104f, 30f, GroundShadow * (0.5f * physical));

        batch.DrawLine(pixel, baseCenter + new Vector2(-82f, 4f), baseCenter + new Vector2(64f, -68f), IronBody * physical, 46f);
        batch.DrawLine(pixel, baseCenter + new Vector2(-82f, -14f), baseCenter + new Vector2(64f, -86f), IronLit * (0.52f * physical), 5f);
        batch.DrawCircle(pixel, baseCenter + new Vector2(-56f, 2f), 27f, IronDark * physical, 11f, 18);
        batch.DrawCircle(pixel, baseCenter + new Vector2(36f, -52f), 24f, IronDark * physical, 10f, 18);
        batch.DrawLine(pixel, baseCenter + new Vector2(-116f, 22f), baseCenter + new Vector2(112f, -4f), IronDark * (0.9f * physical), 9f);
        batch.DrawLine(pixel, baseCenter + new Vector2(-116f, 18f), baseCenter + new Vector2(112f, -8f), IronLit * (0.32f * physical), 2f);
    }

    private static void DrawPillarDrum(SpriteBatch batch, Texture2D pixel, Vector2 baseCenter, float physical)
    {
        batch.FillEllipse(pixel, baseCenter + new Vector2(7f, 7f), 84f, 25f, GroundShadow * (0.48f * physical));
        batch.FillEllipse(pixel, baseCenter, 68f, 21f, IronDark * physical);
        batch.FillRectangle(pixel, new Rectangle((int)baseCenter.X - 68, (int)baseCenter.Y - 54, 136, 54), (IronBody * physical) with { A = 255 });
        batch.FillEllipse(pixel, baseCenter - new Vector2(0f, 54f), 68f, 21f, IronLit * (0.5f * physical));
        batch.FillEllipse(pixel, baseCenter - new Vector2(0f, 54f), 44f, 13f, IronDark * (0.85f * physical));
        batch.DrawLine(pixel, baseCenter + new Vector2(-68f, -20f), baseCenter + new Vector2(68f, -20f), IronDark * (0.7f * physical), 4f);
        batch.DrawLine(pixel, baseCenter + new Vector2(-70f, -50f), baseCenter + new Vector2(-70f, -4f), IronLit * (0.3f * physical), 3f);

        // Fallen shards keep the silhouette from reading as a clean cylinder.
        batch.DrawLine(pixel, baseCenter + new Vector2(62f, 10f), baseCenter + new Vector2(112f, 20f), IronDark * physical, 13f);
        batch.DrawLine(pixel, baseCenter + new Vector2(-74f, 14f), baseCenter + new Vector2(-118f, 8f), IronDark * physical, 10f);
    }

    private static void DrawSoulLamp(SpriteBatch batch, Texture2D pixel, Vector2 position, float time, float physical)
    {
        float flicker = 0.5f + 0.5f * MathF.Sin(time * 3.4f + position.X * 0.01f);
        batch.FillEllipse(pixel, position + new Vector2(4f, 52f), 30f, 10f, GroundShadow * (0.44f * physical));
        batch.DrawLine(pixel, position + new Vector2(0f, 50f), position + new Vector2(0f, 6f), IronDark * physical, 9f);
        batch.DrawLine(pixel, position + new Vector2(-2f, 50f), position + new Vector2(-2f, 6f), IronLit * (0.34f * physical), 2f);
        batch.DrawCircle(pixel, position, 15f, IronLit * (0.6f * physical), 4f, 14);
        batch.FillCircle(pixel, position, 8f + flicker * 2f, GameBalance.DeepViolet * (0.78f * physical));
        batch.FillCircle(pixel, position, 3.4f + flicker * 1.2f, GameBalance.DeathFlameBright * (0.9f * physical));
    }

    private static void DrawPool(
        SpriteBatch batch,
        Texture2D pixel,
        Vector2 center,
        float width,
        float height,
        Color color,
        float strength)
    {
        const int Rings = 6;
        for (int i = Rings; i >= 1; i--)
        {
            float amount = i / (float)Rings;
            batch.FillEllipse(
                pixel,
                center,
                width * amount,
                height * amount,
                color * (strength / Rings));
        }
    }

    private static void DrawEllipseOutline(
        SpriteBatch batch,
        Texture2D pixel,
        Vector2 center,
        float radiusX,
        float radiusY,
        Color color,
        float thickness)
    {
        const int Segments = 72;
        Vector2 previous = center + new Vector2(radiusX, 0f);
        for (int i = 1; i <= Segments; i++)
        {
            float angle = MathHelper.TwoPi * i / Segments;
            Vector2 next = center + new Vector2(MathF.Cos(angle) * radiusX, MathF.Sin(angle) * radiusY);
            batch.DrawLine(pixel, previous, next, color, thickness);
            previous = next;
        }
    }
}
