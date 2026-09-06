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

    /// <summary>
    /// One block per Warden and one shared Resonance track.
    ///
    /// Player 1 keeps the exact position and shape he had in the Golden Slice.
    /// The brother's block mirrors it into the opposite corner, tinted with his
    /// own flame, so neither player has to hunt for their own bar. The Resonance
    /// track stays centred because it belongs to both of them.
    /// </summary>
    public void Draw(SpriteBatch batch, Texture2D pixel, Viewport viewport, WardenRoster roster, TeamResonance team)
    {
        foreach (PlayerSlot slot in roster.Slots)
        {
            bool mirrored = slot.Index > 0;
            DrawWardenBlock(batch, pixel, viewport, slot, mirrored, roster.IsCooperative, team);
        }

        DrawResonance(batch, pixel, viewport, roster, team);

        foreach (PlayerSlot slot in roster.Slots)
        {
            if (slot.Warden.Cannon.State == SoulCannonState.Charging)
            {
                DrawCannonCharge(batch, pixel, viewport, slot.Warden, slot.Index > 0);
            }
        }
    }

    private static void DrawWardenBlock(
        SpriteBatch batch,
        Texture2D pixel,
        Viewport viewport,
        PlayerSlot slot,
        bool mirrored,
        bool cooperative,
        TeamResonance team)
    {
        int originX = mirrored ? viewport.Width - 24 - 205 : 24;
        DrawHealth(batch, pixel, slot, originX, cooperative);

        if (slot.Warden.IsDowned)
        {
            DrawDownState(batch, pixel, slot, originX);
            return;
        }

        DrawDash(batch, pixel, slot.Warden, originX + 37, 54);
    }

    /// <summary>
    /// A guttering brother needs two numbers at a glance: how long he has, and how
    /// far the rescue has got. Both are drawn on his own block in his own flame.
    /// </summary>
    private static void DrawDownState(SpriteBatch batch, Texture2D pixel, PlayerSlot slot, int originX)
    {
        const int trackWidth = 140;
        int trackX = originX + 37;
        int trackY = 24 + 12;

        float remaining = MathHelper.Clamp(slot.Warden.DownRemaining / GameBalance.WardenDownDuration, 0f, 1f);
        Color flame = slot.Identity.FlameBright;
        batch.FillRectangle(pixel, new Rectangle(trackX, trackY, trackWidth, 7), Empty);
        batch.FillRectangle(pixel, new Rectangle(trackX, trackY, (int)MathF.Round(trackWidth * remaining), 7), flame * 0.72f);

        float rescue = MathHelper.Clamp(slot.Warden.StabilizeProgress, 0f, 1f);
        if (rescue > 0f)
        {
            batch.FillRectangle(pixel, new Rectangle(trackX, trackY + 8, (int)MathF.Round(trackWidth * rescue), 3), GameBalance.SoulWhite);
        }

        PixelText.Draw(batch, pixel, "GUTTERING", new Vector2(originX + 37, 54), 1, flame);
        PixelText.Draw(
            batch,
            pixel,
            $"{slot.Warden.DownRemaining:0}S",
            new Vector2(originX + 37 + trackWidth + 8, 54),
            1,
            flame);
    }

    private static void DrawHealth(SpriteBatch batch, Texture2D pixel, PlayerSlot slot, int x, bool cooperative)
    {
        Player player = slot.Warden;
        const int y = 24;
        int trackX = x + 37;
        const int trackY = y + 12;
        const int trackWidth = 140;
        const int trackHeight = 7;

        Rectangle panelBounds = new(x + 13, y + 4, 192, 23);
        batch.FillRectangle(pixel, panelBounds, Panel);
        DrawCornerFrame(batch, pixel, panelBounds, cooperative ? slot.Identity.Accent * 0.72f : Frame);

        // The bound Soul mark takes the Warden's own flame in co-op, so the block
        // is identifiable before any text is read.
        Color soulColor = cooperative ? slot.Identity.FlameBright : BoundSoul;
        Vector2 soulCenter = new(x + 14, y + 15);
        DrawDiamond(batch, pixel, soulCenter, 10, cooperative ? slot.Identity.Flame : BoundSoulDim);
        DrawDiamond(batch, pixel, soulCenter, 5, soulColor);
        batch.DrawLine(pixel, soulCenter - new Vector2(13f, 0f), soulCenter + new Vector2(13f, 0f), new Color(25, 23, 31), 2f);
        batch.FillRectangle(pixel, new Rectangle(trackX, trackY, trackWidth, trackHeight), Empty);

        float healthFill = MathHelper.Clamp(player.Health / (float)GameBalance.PlayerMaxHealth, 0f, 1f);
        int fillWidth = (int)MathF.Round(trackWidth * healthFill);
        if (fillWidth > 0 && !player.IsDowned)
        {
            batch.FillRectangle(pixel, new Rectangle(trackX, trackY, fillWidth, trackHeight), cooperative ? soulColor : BoundSoul);
            batch.FillRectangle(pixel, new Rectangle(trackX, trackY + trackHeight - 2, fillWidth, 2), BoundSoulDim);
        }

        for (int link = 1; link < 5; link++)
        {
            int linkX = trackX + link * trackWidth / 5;
            batch.FillRectangle(pixel, new Rectangle(linkX - 1, trackY - 1, 2, trackHeight + 2), new Color(15, 13, 20));
        }

        PixelText.Draw(batch, pixel, player.Health.ToString(), new Vector2(trackX + trackWidth + 8, y + 12), 1, BoundSoul);
        if (cooperative)
        {
            PixelText.Draw(batch, pixel, slot.Identity.ShortName, new Vector2(x + 6, y + 26), 1, slot.Identity.Accent);
        }
    }

    private static void DrawDash(SpriteBatch batch, Texture2D pixel, Player player, int x, int y)
    {
        const int width = 48;
        float ready = 1f - MathHelper.Clamp(player.DashCooldownRemaining / GameBalance.DashCooldown, 0f, 1f);
        bool severance = player.SeveranceReady;
        Color dashColor = severance
            ? GameBalance.SoulWhite
            : ready >= 0.999f ? GameBalance.DeathFlameBright : GameBalance.DeathFlame * 0.62f;

        // While a Severance Window is open the dash track becomes its timer. The
        // read is confirmed on the character; the HUD only reports how long it holds.
        float track = severance
            ? MathHelper.Clamp(player.SeveranceRemaining / GameBalance.SeveranceWindowDuration, 0f, 1f)
            : ready;

        PixelText.Draw(
            batch,
            pixel,
            severance ? "SEVER" : "DASH",
            new Vector2(x, y),
            1,
            severance ? GameBalance.SoulWhite : ready >= 0.999f ? BoundSoulDim : Frame);
        batch.FillRectangle(pixel, new Rectangle(x + 29, y + 3, width, 2), Empty);
        batch.FillRectangle(pixel, new Rectangle(x + 29, y + 3, (int)MathF.Round(width * track), 2), dashColor);

        Vector2 marker = new(x + 84, y + 4);
        if (severance)
        {
            DrawDiamond(batch, pixel, marker, 4, GameBalance.SoulWhite);
        }
        else if (ready >= 0.999f)
        {
            DrawDiamond(batch, pixel, marker, 3, GameBalance.DeathFlameBright);
        }
        else
        {
            batch.FillRectangle(pixel, new Rectangle((int)marker.X - 1, (int)marker.Y - 1, 2, 2), Frame);
        }
    }

    private static void DrawResonance(SpriteBatch batch, Texture2D pixel, Viewport viewport, WardenRoster roster, TeamResonance team)
    {
        const int trackWidth = 224;
        const int trackHeight = 5;
        Player player = roster.Lead;
        int centerX = viewport.Width / 2;
        int trackX = centerX - trackWidth / 2;
        int trackY = viewport.Height - 34;
        bool active = player.ResonanceActive;
        bool ready = !active && team.IsReady;
        float fill = active
            ? player.ResonanceRemaining / GameBalance.ResonanceDuration
            : team.Charge / GameBalance.ResonanceRequired;
        fill = MathHelper.Clamp(fill, 0f, 1f);

        // One track for both brothers: it is a shared pool, so showing two would
        // imply a competition that does not exist.
        string label = ready
            ? roster.IsCooperative ? "RESONATE TOGETHER" : "R RESONATE"
            : roster.IsCooperative ? "SHARED RESONANCE" : "RESONANCE";
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

    private static void DrawCannonCharge(SpriteBatch batch, Texture2D pixel, Viewport viewport, Player player, bool mirrored)
    {
        int x = mirrored ? 43 : viewport.Width - 43;
        int y = mirrored ? 96 : 28;
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
