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

        Rectangle panelBounds = new(x + 13, y + 4, 192, 23);
        batch.FillRectangle(pixel, panelBounds, Panel);
        DrawCornerFrame(batch, pixel, panelBounds, Frame);

        Vector2 soulCenter = new(x + 14, y + 15);
        DrawDiamond(batch, pixel, soulCenter, 10, BoundSoulDim);
        DrawDiamond(batch, pixel, soulCenter, 5, BoundSoul);
        batch.DrawLine(pixel, soulCenter - new Vector2(13f, 0f), soulCenter + new Vector2(13f, 0f), new Color(25, 23, 31), 2f);
        batch.FillRectangle(pixel, new Rectangle(trackX, trackY, trackWidth, trackHeight), Empty);

        float healthFill = MathHelper.Clamp(player.Health / (float)GameBalance.PlayerMaxHealth, 0f, 1f);
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

        PixelText.Draw(batch, pixel, player.Health.ToString(), new Vector2(trackX + trackWidth + 8, y + 12), 1, BoundSoul);
    }

    private static void DrawDash(SpriteBatch batch, Texture2D pixel, Player player)
    {
        const int x = 61;
        const int y = 54;
        const int width = 48;
        float ready = 1f - MathHelper.Clamp(player.DashCooldownRemaining / GameBalance.DashCooldown, 0f, 1f);
        Color dashColor = ready >= 0.999f ? GameBalance.DeathFlameBright : GameBalance.DeathFlame * 0.62f;

        PixelText.Draw(batch, pixel, "DASH", new Vector2(x, y), 1, ready >= 0.999f ? BoundSoulDim : Frame);
        batch.FillRectangle(pixel, new Rectangle(x + 29, y + 3, width, 2), Empty);
        batch.FillRectangle(pixel, new Rectangle(x + 29, y + 3, (int)MathF.Round(width * ready), 2), dashColor);

        Vector2 marker = new(x + 84, y + 4);
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
