using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Game;

/// <summary>
/// Authored exploration room before the combat arena. The room deliberately has
/// no interior collision so the player can read the gate and the left-to-right
/// route immediately.
/// </summary>
public sealed class SoulFurnaceAntechamber
{
    private static readonly Vector2[] SoulTraces =
    [
        new(360f, 560f),
        new(530f, 510f),
        new(720f, 470f),
        new(910f, 430f),
        new(1080f, 410f)
    ];

    public Rectangle Bounds { get; } = new(0, 0, 1500, 820);
    public Rectangle MovementBounds { get; } = new(72, 105, 1356, 640);
    public Vector2 PlayerSpawn { get; } = new(245f, 535f);
    public Rectangle Gate { get; } = new(1214, 184, 172, 512);
    public Rectangle InteractionZone { get; } = new(1035, 260, 330, 420);
    public Vector2 GateCenter => Gate.Center.ToVector2();

    public bool IsPlayerAtGate(Vector2 playerPosition) =>
        InteractionZone.Contains(playerPosition.ToPoint());

    public void Draw(
        SpriteBatch batch,
        Texture2D pixel,
        float time,
        float soulSenseAmount,
        float gateProgress,
        bool debugVisible)
    {
        batch.FillRectangle(pixel, Bounds, new Color(7, 7, 12));
        batch.FillRectangle(pixel, MovementBounds, new Color(18, 18, 26));

        // Large floor slabs keep the room readable without turning it into a grid.
        for (int x = MovementBounds.Left; x < MovementBounds.Right; x += 170)
        {
            Color seam = new Color(43, 40, 52) * 0.54f;
            batch.DrawLine(pixel, new Vector2(x, 410f), new Vector2(x + 70f, MovementBounds.Bottom), seam, 3f);
        }
        batch.DrawLine(pixel, new Vector2(MovementBounds.Left, 410f), new Vector2(MovementBounds.Right, 410f), new Color(49, 45, 58) * 0.62f, 4f);

        DrawArchitecture(batch, pixel);
        DrawBraziers(batch, pixel, time, soulSenseAmount);
        DrawGate(batch, pixel, time, gateProgress);

        if (debugVisible)
        {
            batch.DrawRectangle(pixel, MovementBounds, new Color(80, 220, 210) * 0.8f, 3f);
            batch.DrawRectangle(pixel, InteractionZone, new Color(245, 205, 90) * 0.75f, 3f);
        }
    }

    public void DrawSoulSense(SpriteBatch batch, Texture2D pixel, float time, float amount)
    {
        if (amount <= 0.001f)
        {
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
            GateCenter,
            GameBalance.DeathFlameBright * (0.24f * amount),
            3f);
    }

    public void DrawLighting(
        SpriteBatch batch,
        SoulfireRenderer renderer,
        float time,
        float soulSenseAmount,
        float gateProgress)
    {
        float breathe = 0.9f + MathF.Sin(time * 3.1f) * 0.1f;
        renderer.DrawGlow(batch, new Vector2(430f, 320f), 110f * breathe, GameBalance.DeathFlame, 0.16f);
        renderer.DrawGlow(batch, new Vector2(865f, 320f), 110f * breathe, GameBalance.DeathFlame, 0.14f);
        renderer.DrawGlow(batch, GateCenter, 96f + gateProgress * 110f, GameBalance.DeathFlameBright, 0.2f + gateProgress * 0.25f);

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
        batch.FillRectangle(pixel, new Rectangle(0, 0, Bounds.Width, 112), new Color(10, 9, 16));
        batch.FillRectangle(pixel, new Rectangle(0, 710, Bounds.Width, 110), new Color(10, 9, 16));

        for (int x = 110; x <= 1120; x += 250)
        {
            batch.FillRectangle(pixel, new Rectangle(x, 118, 52, 570), new Color(28, 26, 36));
            batch.DrawRectangle(pixel, new Rectangle(x, 118, 52, 570), GameBalance.MetalColor * 0.72f, 4f);
            batch.FillRectangle(pixel, new Rectangle(x - 18, 118, 88, 24), new Color(51, 47, 61));
            batch.FillRectangle(pixel, new Rectangle(x - 18, 664, 88, 24), new Color(51, 47, 61));
        }

        // Furnace pipes and hanging chains make this recognisably the same place
        // as the arena while preserving a calm, traversable silhouette.
        for (int x = 210; x < 1180; x += 310)
        {
            batch.DrawLine(pixel, new Vector2(x, 0f), new Vector2(x, 118f), new Color(58, 54, 67), 12f);
            batch.FillCircle(pixel, new Vector2(x, 105f), 13f, new Color(31, 29, 39));
        }
        batch.DrawLine(pixel, new Vector2(70f, 160f), new Vector2(1150f, 160f), new Color(53, 48, 62), 11f);
        batch.DrawLine(pixel, new Vector2(70f, 176f), new Vector2(1150f, 176f), new Color(19, 18, 26), 4f);
    }

    private static void DrawBraziers(SpriteBatch batch, Texture2D pixel, float time, float soulSenseAmount)
    {
        DrawBrazier(batch, pixel, new Vector2(430f, 330f), time, soulSenseAmount);
        DrawBrazier(batch, pixel, new Vector2(865f, 330f), time + 0.7f, soulSenseAmount);
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

    private void DrawGate(SpriteBatch batch, Texture2D pixel, float time, float gateProgress)
    {
        int opening = (int)(Gate.Width * 0.42f * Ease(gateProgress));
        Rectangle frame = new(Gate.Left - 28, Gate.Top - 35, Gate.Width + 56, Gate.Height + 60);
        batch.FillRectangle(pixel, frame, new Color(17, 15, 24));
        batch.DrawRectangle(pixel, frame, new Color(83, 75, 93), 9f);

        Rectangle leftDoor = new(Gate.Left, Gate.Top, Math.Max(3, Gate.Width / 2 - opening), Gate.Height);
        Rectangle rightDoor = new(Gate.Center.X + opening, Gate.Top, Math.Max(3, Gate.Width / 2 - opening), Gate.Height);
        batch.FillRectangle(pixel, leftDoor, new Color(35, 31, 43));
        batch.FillRectangle(pixel, rightDoor, new Color(35, 31, 43));
        batch.DrawRectangle(pixel, leftDoor, GameBalance.MetalColor, 5f);
        batch.DrawRectangle(pixel, rightDoor, GameBalance.MetalColor, 5f);

        for (int y = Gate.Top + 38; y < Gate.Bottom; y += 52)
        {
            batch.DrawLine(pixel, new Vector2(leftDoor.Left + 8f, y), new Vector2(leftDoor.Right - 5f, y), new Color(68, 61, 78), 4f);
            batch.DrawLine(pixel, new Vector2(rightDoor.Left + 5f, y), new Vector2(rightDoor.Right - 8f, y), new Color(68, 61, 78), 4f);
        }

        float pulse = 0.62f + MathF.Sin(time * 3.4f) * 0.15f;
        batch.DrawLine(pixel, new Vector2(Gate.Center.X, Gate.Top + 12f), new Vector2(Gate.Center.X, Gate.Bottom - 12f), GameBalance.DeathFlameBright * pulse, 5f);
        batch.FillCircle(pixel, GateCenter, 12f + pulse * 3f, GameBalance.SoulWhite * 0.8f);
    }

    private static float Ease(float amount)
    {
        float value = MathHelper.Clamp(amount, 0f, 1f);
        return value * value * (3f - 2f * value);
    }
}
