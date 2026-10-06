using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Rendering;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Game;

public enum HubDoorKind
{
    Biome,
    Final
}

/// <summary>
/// One door of the hub. The sealed flag is the hook for the later unlock logic;
/// for now it is fixed when the room is created.
/// </summary>
public sealed class HubDoor
{
    public HubDoor(HubDoorKind kind, int biomeNumber, Rectangle bounds, Rectangle interactionZone, bool isSealed)
    {
        Kind = kind;
        BiomeNumber = biomeNumber;
        Bounds = bounds;
        InteractionZone = interactionZone;
        IsSealed = isSealed;
    }

    public HubDoorKind Kind { get; }
    public int BiomeNumber { get; }
    public Rectangle Bounds { get; }
    public Rectangle InteractionZone { get; }
    public bool IsSealed { get; set; }
    public Vector2 Center => Bounds.Center.ToVector2();

    public string Numeral => Kind == HubDoorKind.Final ? string.Empty : RomanNumerals[BiomeNumber - 1];

    public string Prompt => !IsSealed
        ? $"E  ENTER BIOME {Numeral}"
        : Kind == HubDoorKind.Final
            ? "SEALED · DEFEAT ALL GUARDIANS"
            : "SEALED · DEFEAT THE PREVIOUS GUARDIAN";

    private static readonly string[] RomanNumerals = ["I", "II", "III", "IV", "V", "VI"];
}

/// <summary>
/// Authored hub room: the Ashen Antechamber with seven doors along the north
/// wall (I, II, III, final, IV, V, VI). The room deliberately has no interior
/// collision so the player can read the doors and the route immediately.
/// </summary>
public sealed class SoulFurnaceAntechamber
{
    private const int WallBottom = 340;

    private static readonly Vector2[] SoulTraces =
    [
        new(300f, 640f),
        new(265f, 580f),
        new(235f, 515f),
        new(210f, 450f),
        new(195f, 395f)
    ];

    private static readonly Vector2[] BrazierPositions =
    [
        new(560f, 610f),
        new(940f, 610f)
    ];

    /// <summary>
    /// The rendered room (tools/visuals/blender/build_hub.py): each brazier stands 72 units below
    /// its flame; the iron sconces on the six pilasters hold their flames 2.65 m up, just in front
    /// of the wall.
    /// </summary>
    public static IReadOnlyList<Vector2> BrazierFeet { get; } = [new(560f, 682f), new(940f, 682f)];

    private static readonly Vector2[] SconceFlames =
    [
        new(280f, 219f), new(460f, 219f), new(640f, 219f), new(860f, 219f), new(1040f, 219f), new(1220f, 219f)
    ];

    public SoulFurnaceAntechamber()
    {
        // Left to right: I, II, III, final, IV, V, VI. Only door I is open.
        (HubDoorKind Kind, int Biome, int CenterX)[] layout =
        [
            (HubDoorKind.Biome, 1, 190),
            (HubDoorKind.Biome, 2, 370),
            (HubDoorKind.Biome, 3, 550),
            (HubDoorKind.Final, 0, 750),
            (HubDoorKind.Biome, 4, 950),
            (HubDoorKind.Biome, 5, 1130),
            (HubDoorKind.Biome, 6, 1310)
        ];

        List<HubDoor> doors = [];
        foreach ((HubDoorKind kind, int biome, int centerX) in layout)
        {
            bool isFinal = kind == HubDoorKind.Final;
            int width = isFinal ? 220 : 120;
            int height = isFinal ? 290 : 200;
            int zoneWidth = isFinal ? 230 : 160;
            Rectangle bounds = new(centerX - width / 2, WallBottom - height, width, height);
            Rectangle zone = new(centerX - zoneWidth / 2, WallBottom, zoneWidth, 250);
            doors.Add(new HubDoor(kind, biome, bounds, zone, isSealed: !(kind == HubDoorKind.Biome && biome == 1)));
        }
        Doors = doors;
    }

    public Rectangle Bounds { get; } = new(0, 0, 1500, 900);
    public Rectangle MovementBounds { get; } = new(72, 385, 1356, 420);
    public Vector2 PlayerSpawn { get; } = new(300f, 660f);
    public IReadOnlyList<HubDoor> Doors { get; }
    public HubDoor EntryDoor => Doors[0];
    public Vector2 EntryDoorCenter => EntryDoor.Center;

    /// <summary>Returns the door whose interaction zone contains the player, if any.</summary>
    public HubDoor? DoorAt(Vector2 playerPosition)
    {
        Point point = playerPosition.ToPoint();
        foreach (HubDoor door in Doors)
        {
            if (door.InteractionZone.Contains(point))
            {
                return door;
            }
        }

        return null;
    }

    public void Draw(
        SpriteBatch batch,
        Texture2D pixel,
        ArtAssets art,
        float time,
        float soulSenseAmount,
        float doorProgress,
        bool debugVisible)
    {
        if (art.HasArt(VisualIds.HubFloor))
        {
            DrawRendered(batch, art, time, soulSenseAmount, doorProgress);
        }
        else
        {
            DrawPlaceholder(batch, pixel, time, soulSenseAmount, doorProgress);
        }

        if (debugVisible)
        {
            batch.DrawRectangle(pixel, MovementBounds, new Color(80, 220, 210) * 0.8f, 3f);
            foreach (HubDoor door in Doors)
            {
                batch.DrawRectangle(pixel, door.InteractionZone, new Color(245, 205, 90) * 0.75f, 3f);
            }
        }
    }

    /// <summary>
    /// The rendered room: painted plate, then per door its closed leaves (sliding into the wall as
    /// the door opens) and, while sealed, the iron bar and seal; the Warden flames are drawn
    /// live because a Warden flame is never completely still (S11).
    /// </summary>
    private void DrawRendered(SpriteBatch batch, ArtAssets art, float time, float soulSenseAmount, float doorProgress)
    {
        art.DrawEnvironment(batch, VisualIds.HubFloor, Vector2.Zero);
        foreach (HubDoor door in Doors)
        {
            bool isFinal = door.Kind == HubDoorKind.Final;
            string leaves = isFinal ? VisualIds.HubLeavesFinal : VisualIds.HubLeaves;
            Vector2 anchor = new(door.Center.X, WallBottom);
            float opening = door.Bounds.Width * 0.5f * Ease(door == EntryDoor ? doorProgress : 0f);
            float half = door.Bounds.Width * 0.5f;
            RectangleF left = new(door.Bounds.Left - 2f, door.Bounds.Top - 12f, half - opening + 2f, door.Bounds.Height + 24f);
            RectangleF right = new(door.Center.X + opening, door.Bounds.Top - 12f, half - opening + 2f, door.Bounds.Height + 24f);
            Color tint = door.IsSealed ? new Color(200, 196, 210) : Color.White;
            art.DrawSpriteWindow(batch, leaves, anchor, left, new Vector2(-opening, 0f), tint);
            art.DrawSpriteWindow(batch, leaves, anchor, right, new Vector2(opening, 0f), tint);
            if (door.IsSealed)
            {
                art.DrawProp(batch, isFinal ? VisualIds.HubSealFinal : VisualIds.HubSeal, anchor, Vector2.One, 1f);
                float sealPulse = 0.5f + MathF.Sin(time * 2.2f + door.Center.X * 0.01f) * 0.12f;
                Vector2 seal = new(door.Center.X, door.Bounds.Bottom - door.Bounds.Height * 0.58f);
                art.DrawSoftSpot(batch, seal, new Vector2(isFinal ? 18f : 11f), GameBalance.DeepViolet * sealPulse);
            }
            else
            {
                // The open door: Death Flame light from deep inside, through the seam and, as the
                // leaves part, out across the floor in front of it.
                float pulse = 0.62f + MathF.Sin(time * 3.4f) * 0.15f;
                float open = Ease(door == EntryDoor ? doorProgress : 0f);
                float inner = 3f + opening * 0.85f;
                art.DrawSoftSpot(batch, new Vector2(door.Center.X, door.Bounds.Bottom - door.Bounds.Height * 0.3f),
                    new Vector2(inner, door.Bounds.Height * 0.42f), GameBalance.DeathFlame * (0.28f * pulse + 0.35f * open));
                art.DrawSoftSpot(batch, new Vector2(door.Center.X, door.Bounds.Bottom - door.Bounds.Height * 0.22f),
                    new Vector2(inner * 0.45f, door.Bounds.Height * 0.3f), GameBalance.DeathFlameBright * (0.35f * pulse + 0.3f * open));
                art.DrawSoftSpot(batch, new Vector2(door.Center.X, WallBottom + 18f),
                    new Vector2(door.Bounds.Width * (0.35f + 0.5f * open), 16f + 14f * open), GameBalance.DeathFlame * (0.18f + 0.3f * open));
            }
        }

        for (int index = 0; index < SconceFlames.Length; index++)
        {
            art.DrawWardenFlame(batch, SconceFlames[index], 26f, time + index * 1.37f, 0.9f + soulSenseAmount * 0.1f);
        }
    }

    /// <summary>Called by the game after the actor band: brazier flames stand above their props.</summary>
    public void DrawBrazierFlames(SpriteBatch batch, ArtAssets art, float time)
    {
        if (!art.HasArt(VisualIds.HubFloor))
        {
            return;
        }
        for (int index = 0; index < BrazierPositions.Length; index++)
        {
            art.DrawWardenFlame(batch, BrazierPositions[index] + new Vector2(0f, 1f), 40f, time + index * 0.9f);
        }
    }

    private void DrawPlaceholder(SpriteBatch batch, Texture2D pixel, float time, float soulSenseAmount, float doorProgress)
    {
        batch.FillRectangle(pixel, Bounds, new Color(7, 7, 12));
        batch.FillRectangle(pixel, new Rectangle(0, WallBottom, Bounds.Width, Bounds.Height - WallBottom), new Color(14, 14, 21));
        batch.FillRectangle(pixel, MovementBounds, new Color(18, 18, 26));

        // Large floor slabs keep the room readable without turning it into a grid.
        for (int x = MovementBounds.Left; x < MovementBounds.Right; x += 170)
        {
            Color seam = new Color(43, 40, 52) * 0.54f;
            batch.DrawLine(pixel, new Vector2(x, MovementBounds.Top), new Vector2(x + 70f, MovementBounds.Bottom), seam, 3f);
        }
        batch.DrawLine(pixel, new Vector2(MovementBounds.Left, 590f), new Vector2(MovementBounds.Right, 590f), new Color(49, 45, 58) * 0.62f, 4f);

        DrawArchitecture(batch, pixel);
        DrawBraziers(batch, pixel, time, soulSenseAmount);
        foreach (HubDoor door in Doors)
        {
            DrawDoor(batch, pixel, door, time, door == EntryDoor ? doorProgress : 0f);
        }
    }

    public void DrawSoulSense(SpriteBatch batch, Texture2D pixel, float time, float amount, Texture2D? softSpot = null)
    {
        if (amount <= 0.001f)
        {
            return;
        }

        if (softSpot is not null)
        {
            // The echo of the last one through door I: residue drifting toward the door, a soft
            // glow where the person paused, no lines or circles.
            Vector2[] trail = [.. SoulTraces, new Vector2(EntryDoor.Center.X, EntryDoor.Bounds.Bottom - 40f)];
            Rendering.SoulSensePresentation.DrawResidueTrail(batch, softSpot, trail, time, amount, GameBalance.SoulSenseTrace, 7, additiveBatch: true);
            Vector2 origin = new(softSpot.Width * 0.5f, softSpot.Height * 0.5f);
            for (int i = 0; i < SoulTraces.Length; i += 2)
            {
                float pulse = 0.72f + MathF.Sin(time * 4.2f + i * 0.83f) * 0.18f;
                batch.Draw(softSpot, SoulTraces[i], null, GameBalance.SoulSenseTrace * (0.18f * amount * pulse), 0f, origin, 18f / softSpot.Width, SpriteEffects.None, 0f);
            }
            return;
        }

        for (int i = 0; i < SoulTraces.Length; i++)
        {
            Vector2 trace = SoulTraces[i];
            float pulse = 0.72f + MathF.Sin(time * 4.2f + i * 0.83f) * 0.18f;
            float alpha = amount * pulse;
            batch.FillCircle(pixel, trace, 5f + amount * 3f, GameBalance.SoulWhite * (0.68f * alpha));
            batch.DrawCircle(pixel, trace, 16f + i * 1.5f, GameBalance.SoulSenseTrace * (0.4f * alpha), 2f, 18);

            if (i > 0)
            {
                batch.DrawLine(pixel, SoulTraces[i - 1], trace, GameBalance.SoulSenseTrace * (0.22f * amount), 2f);
            }
        }

        batch.DrawLine(
            pixel,
            SoulTraces[^1],
            new Vector2(EntryDoor.Center.X, EntryDoor.Bounds.Bottom - 40f),
            GameBalance.DeathFlameBright * (0.24f * amount),
            3f);
    }

    public void DrawLighting(
        SpriteBatch batch,
        SoulfireRenderer renderer,
        float time,
        float soulSenseAmount,
        float doorProgress)
    {
        for (int index = 0; index < BrazierPositions.Length; index++)
        {
            // Each bowl breathes on its own, with a small quicker flicker: a pool on the floor
            // around the stand and a hotter core at the flame.
            float phase = time + index * 0.7f;
            float breathe = 0.9f + MathF.Sin(phase * 3.1f) * 0.07f + MathF.Sin(phase * 8.3f + 1.1f) * 0.03f;
            Vector2 flame = BrazierPositions[index];
            renderer.DrawGlow(batch, flame + new Vector2(0f, 40f), 210f * breathe, GameBalance.DeathFlame, 0.15f);
            renderer.DrawGlow(batch, flame - Vector2.UnitY * 10f, 96f * breathe, GameBalance.DeathFlameBright, 0.16f);
        }

        // The Warden flames on the pilasters light the stone around them.
        for (int index = 0; index < SconceFlames.Length; index++)
        {
            float phase = time + index * 1.37f;
            float flicker = 0.92f + MathF.Sin(phase * 2.7f) * 0.05f + MathF.Sin(phase * 7.9f + index) * 0.03f;
            renderer.DrawGlow(batch, SconceFlames[index] - Vector2.UnitY * 8f, 84f * flicker, GameBalance.DeathFlame, 0.13f);
        }

        renderer.DrawGlow(batch, EntryDoorCenter, 96f + doorProgress * 110f, GameBalance.DeathFlameBright, 0.2f + doorProgress * 0.25f);
        foreach (HubDoor door in Doors)
        {
            if (door.IsSealed)
            {
                renderer.DrawGlow(batch, door.Center, door.Kind == HubDoorKind.Final ? 120f : 56f, GameBalance.DeepViolet, door.Kind == HubDoorKind.Final ? 0.22f : 0.1f);
            }
        }

        if (soulSenseAmount <= 0.001f)
        {
            return;
        }

        foreach (Vector2 trace in SoulTraces)
        {
            renderer.DrawGlow(batch, trace, 42f, GameBalance.SoulSenseTrace, 0.2f * soulSenseAmount);
        }
    }

    private void DrawArchitecture(SpriteBatch batch, Texture2D pixel)
    {
        // North wall that carries the doors.
        batch.FillRectangle(pixel, new Rectangle(0, 0, Bounds.Width, WallBottom), new Color(10, 9, 16));
        batch.FillRectangle(pixel, new Rectangle(0, WallBottom - 14, Bounds.Width, 14), new Color(28, 26, 36));
        batch.DrawLine(pixel, new Vector2(0f, WallBottom), new Vector2(Bounds.Width, WallBottom), new Color(53, 48, 62), 6f);
        batch.FillRectangle(pixel, new Rectangle(0, MovementBounds.Bottom, Bounds.Width, Bounds.Height - MovementBounds.Bottom), new Color(10, 9, 16));

        // Pillars between the doors.
        foreach (int x in new[] { 280, 460, 625, 875, 1030, 1220 })
        {
            batch.FillRectangle(pixel, new Rectangle(x - 10, 60, 20, WallBottom - 60), new Color(24, 22, 32));
            batch.DrawRectangle(pixel, new Rectangle(x - 10, 60, 20, WallBottom - 60), GameBalance.MetalColor * 0.55f, 3f);
        }

        // Furnace pipes and hanging chains make this recognisably the same place
        // as the arena while preserving a calm, traversable silhouette.
        for (int x = 100; x < 1450; x += 310)
        {
            batch.DrawLine(pixel, new Vector2(x, 0f), new Vector2(x, 50f), new Color(58, 54, 67), 12f);
            batch.FillCircle(pixel, new Vector2(x, 46f), 13f, new Color(31, 29, 39));
        }
        batch.DrawLine(pixel, new Vector2(40f, 28f), new Vector2(1460f, 28f), new Color(53, 48, 62), 9f);
    }

    private static void DrawBraziers(SpriteBatch batch, Texture2D pixel, float time, float soulSenseAmount)
    {
        DrawBrazier(batch, pixel, BrazierPositions[0], time, soulSenseAmount);
        DrawBrazier(batch, pixel, BrazierPositions[1], time + 0.7f, soulSenseAmount);
    }

    private static void DrawBrazier(SpriteBatch batch, Texture2D pixel, Vector2 position, float time, float soulSenseAmount)
    {
        float flame = 18f + MathF.Sin(time * 5.1f) * 4f;
        batch.FillRectangle(pixel, new Rectangle((int)position.X - 23, (int)position.Y + 8, 46, 16), new Color(54, 50, 63));
        batch.DrawLine(pixel, position + new Vector2(-14f, 24f), position + new Vector2(-20f, 72f), GameBalance.MetalColor, 7f);
        batch.DrawLine(pixel, position + new Vector2(14f, 24f), position + new Vector2(20f, 72f), GameBalance.MetalColor, 7f);
        batch.FillCircle(pixel, position - Vector2.UnitY * flame * 0.35f, flame, GameBalance.DeepViolet * 0.9f);
        batch.FillCircle(pixel, position - Vector2.UnitY * flame * 0.52f, flame * 0.5f, GameBalance.DeathFlameBright * (0.72f + soulSenseAmount * 0.2f));
    }

    private static void DrawDoor(SpriteBatch batch, Texture2D pixel, HubDoor door, float time, float openProgress)
    {
        bool isFinal = door.Kind == HubDoorKind.Final;
        Rectangle gate = door.Bounds;
        int opening = (int)(gate.Width * 0.42f * Ease(openProgress));
        int framePad = isFinal ? 14 : 10;
        Rectangle frame = new(gate.Left - framePad, gate.Top - framePad, gate.Width + framePad * 2, gate.Height + framePad);
        Color frameColor = isFinal ? new Color(118, 104, 138) : door.IsSealed ? new Color(66, 60, 76) : new Color(83, 75, 93);
        Color leafColor = door.IsSealed ? new Color(24, 22, 31) : new Color(35, 31, 43);

        batch.FillRectangle(pixel, frame, new Color(17, 15, 24));
        batch.DrawRectangle(pixel, frame, frameColor, isFinal ? 10f : 6f);
        if (isFinal)
        {
            Rectangle inner = new(frame.Left + 14, frame.Top + 14, frame.Width - 28, frame.Height - 14);
            batch.DrawRectangle(pixel, inner, GameBalance.DeepViolet * 0.8f, 4f);
        }

        Rectangle leftDoor = new(gate.Left, gate.Top, Math.Max(3, gate.Width / 2 - opening), gate.Height);
        Rectangle rightDoor = new(gate.Center.X + opening, gate.Top, Math.Max(3, gate.Width / 2 - opening), gate.Height);
        batch.FillRectangle(pixel, leftDoor, leafColor);
        batch.FillRectangle(pixel, rightDoor, leafColor);
        batch.DrawRectangle(pixel, leftDoor, GameBalance.MetalColor * (door.IsSealed ? 0.6f : 1f), 4f);
        batch.DrawRectangle(pixel, rightDoor, GameBalance.MetalColor * (door.IsSealed ? 0.6f : 1f), 4f);

        Color bandColor = door.IsSealed ? new Color(44, 40, 52) : new Color(68, 61, 78);
        for (int y = gate.Top + 34; y < gate.Bottom; y += 46)
        {
            batch.DrawLine(pixel, new Vector2(leftDoor.Left + 6f, y), new Vector2(leftDoor.Right - 4f, y), bandColor, 3f);
            batch.DrawLine(pixel, new Vector2(rightDoor.Left + 4f, y), new Vector2(rightDoor.Right - 6f, y), bandColor, 3f);
        }

        if (door.IsSealed)
        {
            // A dim seal over the seam marks the door as closed for now.
            float sealPulse = 0.5f + MathF.Sin(time * 2.2f + door.Center.X * 0.01f) * 0.1f;
            float radius = isFinal ? 34f : 20f;
            Vector2 sealCenter = door.Center + new Vector2(0f, gate.Height * 0.08f);
            batch.FillCircle(pixel, sealCenter, radius, new Color(12, 11, 18));
            batch.DrawCircle(pixel, sealCenter, radius, GameBalance.DeepViolet * sealPulse, 3f, 24);
            batch.DrawLine(pixel, sealCenter + new Vector2(-radius * 0.55f, -radius * 0.55f), sealCenter + new Vector2(radius * 0.55f, radius * 0.55f), GameBalance.DeepViolet * sealPulse, 3f);
            batch.DrawLine(pixel, sealCenter + new Vector2(radius * 0.55f, -radius * 0.55f), sealCenter + new Vector2(-radius * 0.55f, radius * 0.55f), GameBalance.DeepViolet * sealPulse, 3f);
        }
        else
        {
            float pulse = 0.62f + MathF.Sin(time * 3.4f) * 0.15f;
            batch.DrawLine(pixel, new Vector2(gate.Center.X, gate.Top + 12f), new Vector2(gate.Center.X, gate.Bottom - 12f), GameBalance.DeathFlameBright * pulse, 4f);
            batch.FillCircle(pixel, door.Center, 10f + pulse * 3f, GameBalance.SoulWhite * 0.8f);
        }

        if (!isFinal)
        {
            PixelText.DrawCentered(
                batch,
                pixel,
                door.Numeral,
                door.Center.X,
                frame.Top - 30f,
                3,
                door.IsSealed ? GameBalance.SoulWhite * 0.45f : GameBalance.SoulWhite * 0.9f);
        }
    }

    private static float Ease(float amount)
    {
        float value = MathHelper.Clamp(amount, 0f, 1f);
        return value * value * (3f - 2f * value);
    }
}
