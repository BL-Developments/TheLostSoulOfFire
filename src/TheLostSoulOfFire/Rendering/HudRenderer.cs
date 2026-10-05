using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

public sealed class HudRenderer
{
    private static readonly Color Panel = new Color(7, 6, 12) * 0.82f;
    private static readonly Color Frame = new(74, 66, 88);
    private static readonly Color Empty = new(31, 28, 40);
    private static readonly Color BoundSoul = new(207, 207, 201);
    private static readonly Color BoundSoulDim = new(126, 127, 126);

    public void Draw(SpriteBatch batch, Texture2D pixel, Viewport viewport, Player player)
    {
        DrawHealth(batch, pixel, player);
        DrawDash(batch, pixel, player);
        DrawResonance(batch, pixel, viewport, player);

        if (player.Cannon.State == SoulCannonState.Charging)
        {
            DrawCannonCharge(batch, pixel, viewport, player);
        }
    }

    private static void DrawHealth(SpriteBatch batch, Texture2D pixel, Player player)
    {
        const int x = 24;
        const int y = 24;
        const int trackX = x + 37;
        const int trackY = y + 12;
        const int trackWidth = 140;
        const int trackHeight = 7;

        const int healthTextX = trackX + trackWidth + 8;
        // The panel grows with the measured width of the largest health value.
        int panelRight = healthTextX + PixelText.Measure(player.MaxHealth.ToString(), 1) + 3;
        Rectangle panelBounds = new(x + 13, y + 4, panelRight - (x + 13), 23);
        batch.FillRectangle(pixel, panelBounds, Panel);
        DrawCornerFrame(batch, pixel, panelBounds, Frame);

        Vector2 soulCenter = new(x + 14, y + 15);
        DrawDiamond(batch, pixel, soulCenter, 10, BoundSoulDim);
        DrawDiamond(batch, pixel, soulCenter, 5, BoundSoul);
        batch.DrawLine(pixel, soulCenter - new Vector2(13f, 0f), soulCenter + new Vector2(13f, 0f), new Color(25, 23, 31), 2f);
        batch.FillRectangle(pixel, new Rectangle(trackX, trackY, trackWidth, trackHeight), Empty);

        float healthFill = MathHelper.Clamp(player.Health / (float)player.MaxHealth, 0f, 1f);
        int fillWidth = (int)MathF.Round(trackWidth * healthFill);
        if (fillWidth > 0)
        {
            batch.FillRectangle(pixel, new Rectangle(trackX, trackY, fillWidth, trackHeight), BoundSoul);
            batch.FillRectangle(pixel, new Rectangle(trackX, trackY + trackHeight - 2, fillWidth, 2), BoundSoulDim);
        }

        for (int link = 1; link < 5; link++)
        {
            int linkX = trackX + link * trackWidth / 5;
            batch.FillRectangle(pixel, new Rectangle(linkX - 1, trackY - 1, 2, trackHeight + 2), new Color(15, 13, 20));
        }

        PixelText.Draw(batch, pixel, player.Health.ToString(), new Vector2(healthTextX, y + 12), 1, BoundSoul);
    }

    /// <summary>Run balances under the dash bar; a credited line glows briefly.</summary>
    public static void DrawCurrencies(SpriteBatch batch, Texture2D pixel, int geld, int glut, float geldPulse, float glutPulse)
    {
        const int x = 61;
        const int y = 72;
        DrawCurrencyLine(batch, pixel, "GELD", geld, new Vector2(x, y), GameBalance.Geld, geldPulse);
        DrawCurrencyLine(batch, pixel, "GLUT", glut, new Vector2(x, y + 16), GameBalance.Glut, glutPulse);
    }

    /// <summary>
    /// Wave counter in the top right corner. Waves with reinforcements show one diamond per
    /// push below the counter: filled once the push has entered the arena.
    /// </summary>
    public static void DrawWave(SpriteBatch batch, Texture2D pixel, Viewport viewport, int wave, int waveCount, int pushesReleased, int pushCount)
    {
        if (wave <= 0)
        {
            return;
        }

        string label = "WELLE";
        string value = $"{wave}/{waveCount}";
        bool lastWave = wave >= waveCount;
        int labelWidth = PixelText.Measure(label, 1);
        int valueWidth = PixelText.Measure(value, 1);
        int right = viewport.Width - 24;
        int width = labelWidth + valueWidth + 30;
        int height = pushCount > 1 ? 34 : 23;
        Rectangle panelBounds = new(right - width, 28, width, height);
        batch.FillRectangle(pixel, panelBounds, Panel);
        DrawCornerFrame(batch, pixel, panelBounds, lastWave ? GameBalance.DeathFlame : Frame);

        int textX = panelBounds.X + 11;
        int textY = panelBounds.Y + 8;
        PixelText.Draw(batch, pixel, label, new Vector2(textX, textY), 1, lastWave ? GameBalance.DeathFlameBright : BoundSoulDim);
        PixelText.Draw(batch, pixel, value, new Vector2(textX + labelWidth + 8, textY), 1, BoundSoul);

        if (pushCount > 1)
        {
            const int spacing = 12;
            int startX = panelBounds.Center.X - (pushCount - 1) * spacing / 2;
            for (int push = 0; push < pushCount; push++)
            {
                Vector2 center = new(startX + push * spacing, panelBounds.Bottom - 8);
                bool released = push < pushesReleased;
                DrawDiamond(batch, pixel, center, 4, released ? GameBalance.DeathFlame : Frame);
                if (released)
                {
                    DrawDiamond(batch, pixel, center, 2, GameBalance.DeathFlameBright);
                }
            }
        }
    }

    private static void DrawCurrencyLine(SpriteBatch batch, Texture2D pixel, string label, int amount, Vector2 position, Color accent, float pulse)
    {
        pulse = MathHelper.Clamp(pulse, 0f, 1f);
        DrawDiamond(batch, pixel, position + new Vector2(-9f, 4f), 3 + (int)MathF.Round(pulse * 2f), accent);
        PixelText.Draw(batch, pixel, label, position, 1, Color.Lerp(BoundSoulDim, accent, 0.45f + pulse * 0.55f));
        int valueX = (int)position.X + PixelText.Measure("GELD", 1) + 8;
        PixelText.Draw(batch, pixel, amount.ToString(), new Vector2(valueX, position.Y), 1, Color.Lerp(BoundSoul, Color.White, pulse));
    }

    private static void DrawDash(SpriteBatch batch, Texture2D pixel, Player player)
    {
        const int x = 61;
        const int y = 54;
        const int width = 48;
        float ready = 1f - MathHelper.Clamp(player.DashCooldownRemaining / GameBalance.DashCooldown, 0f, 1f);
        Color dashColor = ready >= 0.999f ? GameBalance.DeathFlameBright : GameBalance.DeathFlame * 0.62f;

        // The bar follows the label's measured width, which depends on the output pixel grid.
        int barX = x + PixelText.Measure("DASH", 1) + 5;
        PixelText.Draw(batch, pixel, "DASH", new Vector2(x, y), 1, ready >= 0.999f ? BoundSoulDim : Frame);
        batch.FillRectangle(pixel, new Rectangle(barX, y + 3, width, 2), Empty);
        batch.FillRectangle(pixel, new Rectangle(barX, y + 3, (int)MathF.Round(width * ready), 2), dashColor);

        Vector2 marker = new(barX + width + 7, y + 4);
        if (ready >= 0.999f)
        {
            DrawDiamond(batch, pixel, marker, 3, GameBalance.DeathFlameBright);
        }
        else
        {
            batch.FillRectangle(pixel, new Rectangle((int)marker.X - 1, (int)marker.Y - 1, 2, 2), Frame);
        }
    }

    private static void DrawResonance(SpriteBatch batch, Texture2D pixel, Viewport viewport, Player player)
    {
        const int trackWidth = 224;
        const int trackHeight = 5;
        int centerX = viewport.Width / 2;
        int trackX = centerX - trackWidth / 2;
        int trackY = viewport.Height - 34;
        bool ready = player.IsResonanceReady;
        bool active = player.ResonanceActive;
        float fill = active
            ? player.ResonanceRemaining / GameBalance.ResonanceDuration
            : player.Resonance / GameBalance.ResonanceRequired;
        fill = MathHelper.Clamp(fill, 0f, 1f);

        string label = ready ? "R RESONATE" : "RESONANCE";
        Color labelColor = ready || active ? GameBalance.SoulWhite : new Color(142, 119, 171);
        PixelText.DrawCentered(batch, pixel, label, centerX, trackY - 13, 1, labelColor);

        batch.FillRectangle(pixel, new Rectangle(trackX, trackY, trackWidth, trackHeight), Panel);
        batch.FillRectangle(pixel, new Rectangle(trackX + 2, trackY + 2, trackWidth - 4, 1), Empty);

        Color resonanceColor = ready || active
            ? GameBalance.SoulWhite
            : Color.Lerp(GameBalance.DeepViolet, GameBalance.DeathFlame, 0.62f);
        int fillWidth = (int)MathF.Round((trackWidth - 4) * fill);
        if (fillWidth > 0)
        {
            batch.FillRectangle(pixel, new Rectangle(trackX + 2, trackY + 1, fillWidth, 3), resonanceColor);
        }

        Color frameColor = ready ? GameBalance.SoulWhite * 0.82f : active ? GameBalance.DeathFlameBright * 0.74f : Frame;
        batch.DrawLine(pixel, new Vector2(trackX, trackY), new Vector2(trackX + 16, trackY), frameColor, 1f);
        batch.DrawLine(pixel, new Vector2(trackX + trackWidth - 16, trackY), new Vector2(trackX + trackWidth, trackY), frameColor, 1f);
        batch.DrawLine(pixel, new Vector2(trackX, trackY + trackHeight), new Vector2(trackX + 16, trackY + trackHeight), frameColor, 1f);
        batch.DrawLine(pixel, new Vector2(trackX + trackWidth - 16, trackY + trackHeight), new Vector2(trackX + trackWidth, trackY + trackHeight), frameColor, 1f);
        DrawDiamond(batch, pixel, new Vector2(trackX - 7, trackY + 2), ready ? 5 : 3, frameColor);
        DrawDiamond(batch, pixel, new Vector2(trackX + trackWidth + 7, trackY + 2), ready ? 5 : 3, frameColor);

        if (ready)
        {
            DrawDiamond(batch, pixel, new Vector2(centerX, trackY + 2), 4, GameBalance.SoulWhite);
            batch.DrawLine(pixel, new Vector2(centerX - 7, trackY - 5), new Vector2(centerX, trackY - 9), GameBalance.DeathFlameBright * 0.52f, 1f);
            batch.DrawLine(pixel, new Vector2(centerX, trackY - 9), new Vector2(centerX + 7, trackY - 5), GameBalance.DeathFlameBright * 0.52f, 1f);
        }
    }

    private static void DrawCannonCharge(SpriteBatch batch, Texture2D pixel, Viewport viewport, Player player)
    {
        int x = viewport.Width - 43;
        const int y = 28;
        PixelText.DrawCentered(batch, pixel, "CANNON", x, y, 1, Frame);

        for (int stage = 0; stage < 3; stage++)
        {
            bool filled = player.Cannon.ChargeStage > stage;
            bool full = stage == 2 && player.Cannon.IsFullCharge;
            Color color = full
                ? GameBalance.SoulWhite
                : filled
                    ? GameBalance.DeathFlameBright
                    : Empty;
            DrawDiamond(batch, pixel, new Vector2(x - 14 + stage * 14, y + 15), full ? 5 : 4, color);
        }
    }

    private static void DrawCornerFrame(SpriteBatch batch, Texture2D pixel, Rectangle bounds, Color color)
    {
        const int corner = 8;
        batch.DrawLine(pixel, new Vector2(bounds.Left, bounds.Top + corner), new Vector2(bounds.Left + corner, bounds.Top), color, 1f);
        batch.DrawLine(pixel, new Vector2(bounds.Left + corner, bounds.Top), new Vector2(bounds.Right - corner, bounds.Top), color, 1f);
        batch.DrawLine(pixel, new Vector2(bounds.Right - corner, bounds.Top), new Vector2(bounds.Right, bounds.Top + corner), color, 1f);
        batch.DrawLine(pixel, new Vector2(bounds.Right, bounds.Bottom - corner), new Vector2(bounds.Right - corner, bounds.Bottom), color, 1f);
        batch.DrawLine(pixel, new Vector2(bounds.Right - corner, bounds.Bottom), new Vector2(bounds.Left + corner, bounds.Bottom), color, 1f);
        batch.DrawLine(pixel, new Vector2(bounds.Left + corner, bounds.Bottom), new Vector2(bounds.Left, bounds.Bottom - corner), color, 1f);
    }

    private static void DrawDiamond(SpriteBatch batch, Texture2D pixel, Vector2 center, int radius, Color color)
    {
        for (int offset = -radius; offset <= radius; offset++)
        {
            int halfWidth = radius - Math.Abs(offset);
            batch.FillRectangle(
                pixel,
                new Rectangle((int)center.X - halfWidth, (int)center.Y + offset, halfWidth * 2 + 1, 1),
                color);
        }
    }
}
