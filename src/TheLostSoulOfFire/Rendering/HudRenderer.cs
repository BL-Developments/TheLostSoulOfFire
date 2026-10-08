using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// The combat HUD in Warden iron (see <see cref="UiKit"/>): the soul medallion with the health
/// bar, dash and run currencies top left, the wave plaque top right, resonance bottom centre.
/// Presentation only: every value comes straight from the player and the run.
/// </summary>
public sealed class HudRenderer
{
    private static readonly Color BoundSoul = new(207, 207, 201);
    private static readonly Color BoundSoulDim = new(126, 127, 126);
    private static readonly Color Label = new(176, 168, 188);
    private static readonly Color Muted = new(92, 84, 106);

    private const float TrailHold = 0.35f;
    private const float TrailDrain = 0.9f;

    private const int HealthTrackX = 84;
    private const int HealthTrackY = 33;
    private const int HealthTrackWidth = 176;
    private const int HealthTrackHeight = 8;

    private float _trail = 1f;
    private float _trailHold;
    private float _time;

    /// <summary>The bound soul's throb when health runs low (0–1), so the bar beats with it.</summary>
    public float Throb { get; set; }

    /// <summary>Recent damage stays visible as a pale trail that drains after a short hold.</summary>
    public void Update(float deltaTime, Player player)
    {
        _time += deltaTime;
        float health = HealthFraction(player);
        if (health >= _trail)
        {
            _trail = health;
            _trailHold = 0f;
            return;
        }

        if (_trailHold <= 0f && _trail - health > 0.001f && _trailHold > -0.5f)
        {
            _trailHold = TrailHold;
        }

        _trailHold -= deltaTime;
        if (_trailHold <= 0f)
        {
            _trail = MathF.Max(health, _trail - TrailDrain * deltaTime);
            _trailHold = -1f;
        }
    }

    public void Draw(SpriteBatch batch, Texture2D pixel, Viewport viewport, Player player)
    {
        DrawHealth(batch, pixel, player);
        DrawDash(batch, pixel, player);
        DrawResonance(batch, pixel, viewport, player);
    }

    private static float HealthFraction(Player player) =>
        MathHelper.Clamp(player.Health / (float)Math.Max(1, player.MaxHealth), 0f, 1f);

    private void DrawHealth(SpriteBatch batch, Texture2D pixel, Player player)
    {
        float health = HealthFraction(player);
        bool low = health <= 0.3f && health > 0f;
        float lowPulse = low ? Throb : 0f;

        Rectangle track = new(HealthTrackX, HealthTrackY, HealthTrackWidth, HealthTrackHeight);
        Color fill = low ? Color.Lerp(BoundSoul, GameBalance.DeathFlameBright, 0.35f + lowPulse * 0.25f) : BoundSoul;
        UiKit.Bar(batch, pixel, track, health, fill, 1f, _trail, GameBalance.DeathFlame * 0.55f);

        // Five links, as on the old chain: a dark notch every fifth of the track.
        for (int link = 1; link < 5; link++)
        {
            int linkX = track.X + link * track.Width / 5;
            batch.FillRectangle(pixel, new Rectangle(linkX - 1, track.Y, 2, track.Height), new Color(9, 8, 13) * 0.85f);
        }

        // The bound soul in its medallion dims with health and flickers when it runs low.
        Color core = Color.Lerp(BoundSoulDim * 0.7f, BoundSoul, 0.35f + health * 0.65f);
        if (low)
        {
            core = Color.Lerp(core, GameBalance.DeathFlameBright, lowPulse * 0.45f);
        }
        UiKit.Gem(batch, pixel, new Vector2(46, 37), core);

        string value = player.Health.ToString();
        PixelText.DrawFace(batch, pixel, value, new Vector2(track.Right + 18, track.Y - 3), TextFace.Display, 14f, BoundSoul);
    }

    /// <summary>Run balances under the dash bar; a credited line glows briefly.</summary>
    public static void DrawCurrencies(SpriteBatch batch, Texture2D pixel, int geld, int glut, float geldPulse, float glutPulse)
    {
        const int x = 80;
        const int y = 80;
        DrawCurrencyLine(batch, pixel, UiIcon.Geld, "GELD", geld, new Vector2(x, y), GameBalance.Geld, geldPulse);
        DrawCurrencyLine(batch, pixel, UiIcon.Glut, "GLUT", glut, new Vector2(x, y + 22), GameBalance.Glut, glutPulse);
    }

    private static void DrawCurrencyLine(SpriteBatch batch, Texture2D pixel, UiIcon icon, string label, int amount, Vector2 position, Color accent, float pulse)
    {
        pulse = MathHelper.Clamp(pulse, 0f, 1f);
        UiKit.Icon(batch, pixel, icon, position + new Vector2(0f, 4f), 0.75f + pulse * 0.25f);
        Vector2 labelAt = position + new Vector2(14f, 0f);
        PixelText.DrawFace(batch, pixel, label, labelAt, TextFace.Body, 9.5f, Color.Lerp(Label, accent, 0.3f + pulse * 0.7f), 1f);
        int valueX = (int)labelAt.X + PixelText.MeasureFace("GELD", TextFace.Body, 9.5f, 1f) + 10;
        PixelText.DrawFace(batch, pixel, amount.ToString(), new Vector2(valueX, position.Y - 1f), TextFace.Display, 11f, Color.Lerp(BoundSoul, Color.White, pulse));
    }

    /// <summary>
    /// Wave plaque in the top right corner. Waves with reinforcements show one diamond per
    /// push below the counter: lit once the push has entered the arena. The last wave burns.
    /// </summary>
    public static void DrawWave(SpriteBatch batch, Texture2D pixel, Viewport viewport, int wave, int waveCount, int pushesReleased, int pushCount)
    {
        if (wave <= 0)
        {
            return;
        }

        const string label = "WELLE";
        string value = $"{wave}/{waveCount}";
        bool lastWave = wave >= waveCount;
        int labelWidth = PixelText.MeasureFace(label, TextFace.Body, 10f, 1.4f);
        int valueWidth = PixelText.MeasureFace(value, TextFace.Display, 16f);
        int width = Math.Max(labelWidth + valueWidth + 44, pushCount > 1 ? pushCount * 14 + 34 : 0);
        int height = pushCount > 1 ? 50 : 36;
        Rectangle panel = new(viewport.Width - 24 - width, 24, width, height);
        Color accent = lastWave ? GameBalance.DeathFlame : Muted;
        UiKit.Panel(batch, pixel, panel, accent, lastWave ? 0.9f : 0.5f, 1f, lastWave ? 0.35f : 0f);

        int contentX = panel.X + (width - labelWidth - valueWidth - 10) / 2;
        int baseline = panel.Y + 12;
        PixelText.DrawFace(batch, pixel, label, new Vector2(contentX, baseline + 3), TextFace.Body, 10f,
            lastWave ? GameBalance.DeathFlameBright : Label, 1.4f);
        PixelText.DrawFace(batch, pixel, value, new Vector2(contentX + labelWidth + 10, baseline - 3), TextFace.Display, 16f, BoundSoul);

        if (pushCount > 1)
        {
            const int spacing = 14;
            int startX = panel.Center.X - (pushCount - 1) * spacing / 2;
            for (int push = 0; push < pushCount; push++)
            {
                Vector2 center = new(startX + push * spacing, panel.Bottom - 12);
                bool released = push < pushesReleased;
                UiKit.FillDiamond(batch, pixel, center, 4, released ? GameBalance.DeathFlame : new Color(32, 28, 40));
                UiKit.FillDiamond(batch, pixel, center, 2, released ? GameBalance.DeathFlameBright : Muted * 0.8f);
            }
        }
    }

    private static void DrawDash(SpriteBatch batch, Texture2D pixel, Player player)
    {
        const int x = 84;
        const int y = 56;
        float ready = 1f - MathHelper.Clamp(player.DashCooldownRemaining / GameBalance.DashCooldown, 0f, 1f);
        bool full = ready >= 0.999f;
        Color dashColor = full ? GameBalance.DeathFlameBright : GameBalance.DeathFlame * 0.7f;

        PixelText.DrawFace(batch, pixel, "DASH", new Vector2(x, y), TextFace.Body, 9.5f, full ? Label : Muted, 1.2f);
        int barX = x + PixelText.MeasureFace("DASH", TextFace.Body, 9.5f, 1.2f) + 16;
        UiKit.Bar(batch, pixel, new Rectangle(barX, y + 2, 56, 5), ready, dashColor);

        Vector2 marker = new(barX + 56 + 16, y + 4);
        if (full)
        {
            UiKit.FillDiamond(batch, pixel, marker, 4, GameBalance.DeathFlame * 0.6f);
            UiKit.FillDiamond(batch, pixel, marker, 2, GameBalance.DeathFlameBright);
        }
        else
        {
            UiKit.FillDiamond(batch, pixel, marker, 2, Muted);
        }
    }

    private void DrawResonance(SpriteBatch batch, Texture2D pixel, Viewport viewport, Player player)
    {
        const int trackWidth = 236;
        const int trackHeight = 6;
        int centerX = viewport.Width / 2;
        int trackX = centerX - trackWidth / 2;
        int trackY = viewport.Height - 32;
        bool ready = player.IsResonanceReady;
        bool active = player.ResonanceActive;
        float fill = active
            ? player.ResonanceRemaining / GameBalance.ResonanceDuration
            : player.Resonance / GameBalance.ResonanceRequired;
        fill = MathHelper.Clamp(fill, 0f, 1f);

        float breathe = 0.5f + 0.5f * MathF.Sin(_time * 3.2f);
        Color resonanceColor = ready || active
            ? GameBalance.SoulWhite
            : Color.Lerp(GameBalance.DeepViolet, GameBalance.DeathFlame, 0.62f);
        // A plate keeps label and bar readable over props at the bottom edge; a ready
        // resonance breathes so it can be noticed without reading the label.
        Rectangle plate = new(trackX - 28, trackY - 27, trackWidth + 56, trackHeight + 38);
        UiKit.Panel(batch, pixel, plate, ready ? GameBalance.SoulWhite : active ? GameBalance.DeathFlameBright : Muted,
            ready || active ? 0.8f : 0.35f, 0.92f, ready ? 0.2f + breathe * 0.25f : 0f);
        UiKit.Bar(batch, pixel, new Rectangle(trackX, trackY, trackWidth, trackHeight), fill, resonanceColor);

        if (ready)
        {
            const string key = "R";
            const string word = "RESONANZ";
            int keyWidth = UiKit.KeyWidth(key, 1);
            int wordWidth = PixelText.MeasureFace(word, TextFace.Display, 10f, 1.5f);
            int left = centerX - (keyWidth + 8 + wordWidth) / 2;
            UiKit.Key(batch, pixel, new Vector2(left, trackY - 22), key, GameBalance.SoulWhite, 1f, 1, 15);
            PixelText.DrawFace(batch, pixel, word, new Vector2(left + keyWidth + 8, trackY - 19.5f), TextFace.Display, 10f,
                Color.Lerp(GameBalance.SoulWhite, GameBalance.DeathFlameBright, breathe * 0.35f), 1.5f);
        }
        else
        {
            Color labelColor = active ? GameBalance.DeathFlameBright : new Color(142, 119, 171);
            int width = PixelText.MeasureFace("RESONANZ", TextFace.Display, 10f, 1.5f);
            PixelText.DrawFace(batch, pixel, "RESONANZ", new Vector2(centerX - width / 2f, trackY - 18), TextFace.Display, 10f, labelColor, 1.5f);
        }
    }

    /// <summary>
    /// The aim reticle in the world: a ring with four ticks. While the Soul Cannon charges, three
    /// arcs around it fill stage by stage (formerly a separate HUD readout) and the full charge
    /// closes them into a white ring.
    /// </summary>
    public static void DrawReticle(SpriteBatch batch, Texture2D pixel, Vector2 at, SoulCannon cannon, float time)
    {
        bool charging = cannon.State == SoulCannonState.Charging;
        Color ring = GameBalance.DeathFlameBright * (charging ? 0.85f : 0.7f);
        batch.DrawCircle(pixel, at, 8f, ring, 1.5f, 20);
        for (int tick = 0; tick < 4; tick++)
        {
            Vector2 direction = new(MathF.Cos(tick * MathHelper.PiOver2), MathF.Sin(tick * MathHelper.PiOver2));
            batch.DrawLine(pixel, at + direction * 11f, at + direction * 16f, GameBalance.DeathFlame * 0.7f, 1.5f);
        }
        batch.FillRectangle(pixel, new Rectangle((int)at.X - 1, (int)at.Y - 1, 2, 2), GameBalance.SoulWhite * 0.85f);

        if (!charging)
        {
            return;
        }

        const float radius = 21f;
        const float gap = 0.32f;
        float span = MathHelper.TwoPi / 3f - gap;
        bool full = cannon.IsFullCharge;
        float pulse = 0.5f + 0.5f * MathF.Sin(time * 14f);
        for (int stage = 0; stage < 3; stage++)
        {
            bool filled = cannon.ChargeStage > stage;
            Color color = full
                ? GameBalance.SoulWhite * (0.75f + pulse * 0.25f)
                : filled
                    ? GameBalance.DeathFlameBright
                    : new Color(60, 48, 78) * 0.8f;
            float start = -MathHelper.PiOver2 + gap / 2f + stage * MathHelper.TwoPi / 3f;
            DrawArc(batch, pixel, at, radius, start, full ? MathHelper.TwoPi / 3f : span, color, filled || full ? 3f : 2f);
        }
    }

    private static void DrawArc(SpriteBatch batch, Texture2D pixel, Vector2 center, float radius, float start, float sweep, Color color, float thickness)
    {
        const int segments = 10;
        Vector2 previous = center + new Vector2(MathF.Cos(start), MathF.Sin(start)) * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = start + sweep * i / segments;
            Vector2 next = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            batch.DrawLine(pixel, previous, next, color, thickness);
            previous = next;
        }
    }
}
