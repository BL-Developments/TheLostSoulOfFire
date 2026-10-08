using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Core;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

public enum UiIcon
{
    Geld,
    Glut
}

/// <summary>
/// Shared interface pieces in the Warden-iron style (made by <c>tools/visuals/ui_kit.py</c>): framed
/// panels with an enamel inlay in the accent colour, keycaps, iron bars, the soul medallion,
/// currency icons and a divider. The textures sit on the output grid (1.5 px per logical unit)
/// and every piece snaps to whole output pixels, so the iron stays crisp. Without the textures
/// (tests, missing content) each piece falls back to flat shapes in the same layout.
/// </summary>
public static class UiKit
{
    private const float Density = RenderResolution.Scale;

    // Slices in output pixels; mirrored in tools/visuals/ui_kit.py.
    private const int PanelSize = 72;
    private const int PanelCorner = 24;
    private const int GlowSize = 96;
    private const int GlowCorner = 36;
    private const int GlowPad = 12;
    private const int KeySize = 36;
    private const int KeyCorner = 12;
    private const int BarWidth = 36;
    private const int BarHeight = 24;
    private const int BarCap = 15;
    private const int BarTrackTop = 6;
    private const int BarTrackHeight = 12;
    private const int DividerPart = 60;

    private static Texture2D? _panel;
    private static Texture2D? _panelInlay;
    private static Texture2D? _panelGlow;
    private static Texture2D? _key;
    private static Texture2D? _bar;
    private static Texture2D? _barFill;
    private static Texture2D? _gem;
    private static Texture2D? _gemCore;
    private static Texture2D? _coin;
    private static Texture2D? _ember;
    private static Texture2D? _divider;

    public static readonly Color Ink = new(11, 9, 17);
    public static readonly Color IronLine = new(74, 66, 88);

    public static bool Loaded => _panel is not null;

    public static void Load(ContentManager content)
    {
        try
        {
            _panel = content.Load<Texture2D>("Textures/Ui/panel");
            _panelInlay = content.Load<Texture2D>("Textures/Ui/panel_inlay");
            _panelGlow = content.Load<Texture2D>("Textures/Ui/panel_glow");
            _key = content.Load<Texture2D>("Textures/Ui/key");
            _bar = content.Load<Texture2D>("Textures/Ui/bar");
            _barFill = content.Load<Texture2D>("Textures/Ui/bar_fill");
            _gem = content.Load<Texture2D>("Textures/Ui/gem");
            _gemCore = content.Load<Texture2D>("Textures/Ui/gem_core");
            _coin = content.Load<Texture2D>("Textures/Ui/coin");
            _ember = content.Load<Texture2D>("Textures/Ui/ember");
            _divider = content.Load<Texture2D>("Textures/Ui/divider");
        }
        catch (ContentLoadException)
        {
            _panel = null;
        }
    }

    /// <summary>
    /// An iron-framed panel. <paramref name="accent"/> tints the enamel inlay at
    /// <paramref name="accentStrength"/>; <paramref name="glow"/> adds an outer glow (selection, ready).
    /// </summary>
    public static void Panel(SpriteBatch batch, Texture2D pixel, Rectangle bounds, Color accent,
        float accentStrength = 0.6f, float alpha = 1f, float glow = 0f)
    {
        if (!Loaded)
        {
            batch.FillRectangle(pixel, bounds, Ink * (0.9f * alpha));
            batch.DrawRectangle(pixel, bounds, accent * (accentStrength * alpha), 1f);
            return;
        }

        RectangleF area = Snap(bounds);
        if (glow > 0f)
        {
            float pad = GlowPad / Density;
            NineSlice(batch, _panelGlow!, new RectangleF(area.X - pad, area.Y - pad, area.Width + pad * 2, area.Height + pad * 2),
                GlowSize, GlowCorner, accent * (glow * alpha * 0.85f));
        }

        NineSlice(batch, _panel!, area, PanelSize, PanelCorner, Color.White * alpha);
        if (accentStrength > 0f)
        {
            NineSlice(batch, _panelInlay!, area, PanelSize, PanelCorner, accent * (accentStrength * alpha));
        }
    }

    /// <summary>Width of a keycap for <paramref name="key"/> at the given text size.</summary>
    public static int KeyWidth(string key, int textSize = 2) => Math.Max(22, PixelText.Measure(key, textSize) + 14);

    /// <summary>
    /// A keycap with its label centred: <paramref name="accent"/> colours the label as given (fade it
    /// by the caller), <paramref name="alpha"/> fades the cap.
    /// </summary>
    public static void Key(SpriteBatch batch, Texture2D pixel, Vector2 topLeft, string key, Color accent,
        float alpha = 1f, int textSize = 2, int height = 22)
    {
        int width = KeyWidth(key, textSize);
        Rectangle bounds = new((int)topLeft.X, (int)topLeft.Y, width, height);
        if (Loaded)
        {
            NineSlice(batch, _key!, Snap(bounds), KeySize, KeyCorner, Color.White * alpha);
        }
        else
        {
            batch.FillRectangle(pixel, bounds, accent * 0.16f);
        }

        float capHeight = textSize >= 2 ? 12f : 8.5f;
        PixelText.DrawCentered(batch, pixel, key, bounds.Center.X, bounds.Y + (height - capHeight) * 0.5f, textSize, accent);
    }

    /// <summary>
    /// An iron bar whose sunken track is <paramref name="track"/> (logical units). The fill grows
    /// from the left; an optional <paramref name="trail"/> (0–1, above the fill) shows recent loss.
    /// </summary>
    public static void Bar(SpriteBatch batch, Texture2D pixel, Rectangle track, float fill, Color fillColour,
        float alpha = 1f, float trail = 0f, Color trailColour = default)
    {
        fill = MathHelper.Clamp(fill, 0f, 1f);
        trail = MathHelper.Clamp(trail, fill, 1f);
        if (!Loaded)
        {
            batch.FillRectangle(pixel, track, Ink * (0.9f * alpha));
            batch.FillRectangle(pixel, new Rectangle(track.X, track.Y, (int)MathF.Round(track.Width * trail), track.Height), trailColour * alpha);
            batch.FillRectangle(pixel, new Rectangle(track.X, track.Y, (int)MathF.Round(track.Width * fill), track.Height), fillColour * alpha);
            return;
        }

        RectangleF inner = Snap(track);
        float scaleY = inner.Height / (BarTrackHeight / Density);
        float capWidth = BarCap / Density * scaleY;
        float top = inner.Y - BarTrackTop / Density * scaleY;
        float height = BarHeight / Density * scaleY;
        // The caps reach past the track ends by the cap width.
        RectangleF frame = new(inner.X - capWidth, top, inner.Width + capWidth * 2f, height);
        DrawPart(batch, _bar!, new Rectangle(0, 0, BarCap, BarHeight), new RectangleF(frame.X, frame.Y, capWidth, frame.Height), Color.White * alpha);
        DrawPart(batch, _bar!, new Rectangle(BarCap, 0, BarWidth - BarCap * 2, BarHeight), new RectangleF(frame.X + capWidth, frame.Y, inner.Width, frame.Height), Color.White * alpha);
        DrawPart(batch, _bar!, new Rectangle(BarWidth - BarCap, 0, BarCap, BarHeight), new RectangleF(frame.Right - capWidth, frame.Y, capWidth, frame.Height), Color.White * alpha);

        if (trail > fill)
        {
            DrawPart(batch, _barFill!, null, new RectangleF(inner.X, inner.Y, inner.Width * trail, inner.Height), trailColour * alpha);
        }

        if (fill > 0f)
        {
            float width = RenderResolution.SnapToOutputPixel(inner.Width * fill);
            DrawPart(batch, _barFill!, null, new RectangleF(inner.X, inner.Y, width, inner.Height), fillColour * alpha);
        }
    }

    /// <summary>The soul medallion: iron setting, crystal tinted by <paramref name="core"/>.</summary>
    public static void Gem(SpriteBatch batch, Texture2D pixel, Vector2 center, Color core, float scale = 1f, float alpha = 1f)
    {
        if (!Loaded)
        {
            FillDiamond(batch, pixel, center, (int)(20 * scale), IronLine * alpha);
            FillDiamond(batch, pixel, center, (int)(12 * scale), core * alpha);
            return;
        }

        DrawCentered(batch, _gem!, center, scale, Color.White * alpha);
        DrawCentered(batch, _gemCore!, center, scale, core * alpha);
    }

    public static void Icon(SpriteBatch batch, Texture2D pixel, UiIcon icon, Vector2 center, float scale = 1f, float alpha = 1f)
    {
        if (!Loaded)
        {
            FillDiamond(batch, pixel, center, (int)(5 * scale), (icon == UiIcon.Geld ? GameBalance.Geld : GameBalance.Glut) * alpha);
            return;
        }

        DrawCentered(batch, icon == UiIcon.Geld ? _coin! : _ember!, center, scale, Color.White * alpha);
    }

    /// <summary>
    /// An interaction prompt on an iron plate centred at <paramref name="centerX"/>. A leading key
    /// written as "E  TEXT" (two spaces) becomes a keycap; <paramref name="pulse"/> (0–1) breathes
    /// the glow and the text.
    /// </summary>
    public static void Prompt(SpriteBatch batch, Texture2D pixel, float centerX, float top, string prompt, Color accent, float pulse, float textAlpha = 1f)
    {
        string key = "";
        string text = prompt;
        int split = prompt.IndexOf("  ", StringComparison.Ordinal);
        if (split is > 0 and <= 5)
        {
            key = prompt[..split];
            text = prompt[(split + 2)..];
        }

        int keyWidth = key.Length > 0 ? KeyWidth(key, 2) + 12 : 0;
        int textWidth = PixelText.Measure(text, 2);
        int width = keyWidth + textWidth + 48;
        Rectangle plate = new((int)(centerX - width / 2f), (int)top, width, 46);
        Panel(batch, pixel, plate, accent, 0.55f + pulse * 0.35f, 1f, 0.12f + pulse * 0.16f);
        float x = plate.X + 24;
        if (key.Length > 0)
        {
            Key(batch, pixel, new Vector2(x, plate.Y + 11), key, GameBalance.SoulWhite * textAlpha, 1f, 2, 24);
            x += keyWidth;
        }
        PixelText.Draw(batch, pixel, text, new Vector2(x, plate.Y + 17), 2, GameBalance.SoulWhite * (textAlpha * (0.8f + pulse * 0.2f)));
    }

    public static float KeyLineWidth(string key, string action, int textSize = 2) =>
        KeyWidth(key, textSize) + 8 + PixelText.Measure(action, textSize);

    /// <summary>A keycap and its action on one line, centred on <paramref name="centerX"/>.</summary>
    public static void KeyLine(SpriteBatch batch, Texture2D pixel, float centerX, float y, string key, string action, Color color, int textSize = 2)
    {
        float left = centerX - KeyLineWidth(key, action, textSize) * 0.5f;
        int height = textSize >= 2 ? 24 : 18;
        float capHeight = textSize >= 2 ? 12f : 8.5f;
        Key(batch, pixel, new Vector2(left, y - (height - capHeight) * 0.5f), key, color, color.A / 255f, textSize, height);
        PixelText.Draw(batch, pixel, action, new Vector2(left + KeyWidth(key, textSize) + 8, y), textSize, color);
    }

    /// <summary>A dark band for spoken lines that fades out toward both ends.</summary>
    public static void CaptionBand(SpriteBatch batch, Texture2D pixel, Rectangle area, float alpha)
    {
        const int steps = 24;
        int fade = Math.Min(area.Width / 4, 220);
        Color ink = new Color(6, 5, 10) * alpha;
        batch.FillRectangle(pixel, new Rectangle(area.X + fade, area.Y, area.Width - fade * 2, area.Height), ink);
        for (int step = 0; step < steps; step++)
        {
            float t = (step + 0.5f) / steps;
            int x0 = area.X + fade * step / steps;
            int x1 = area.X + fade * (step + 1) / steps;
            Color c = ink * (t * t);
            batch.FillRectangle(pixel, new Rectangle(x0, area.Y, x1 - x0, area.Height), c);
            batch.FillRectangle(pixel, new Rectangle(area.Right - (x1 - area.X), area.Y, x1 - x0, area.Height), c);
        }
        Divider(batch, pixel, area.Center.X, area.Y, area.Width * 0.6f, GameBalance.DeathFlameBright * (0.28f * alpha));
    }

    /// <summary>Secured or run balances as one line: a label, then each currency with its icon.</summary>
    public static void Balances(SpriteBatch batch, Texture2D pixel, float centerX, float y, string label, int geld, int glut, float alpha)
    {
        string geldText = geld.ToString();
        string glutText = glut.ToString();
        int labelWidth = PixelText.MeasureFace(label, TextFace.Body, 9.5f, 1.5f);
        int geldWidth = PixelText.MeasureFace(geldText, TextFace.Display, 11f);
        int glutWidth = PixelText.MeasureFace(glutText, TextFace.Display, 11f);
        float width = labelWidth + 18 + 16 + geldWidth + 18 + 16 + glutWidth;
        float x = centerX - width / 2f;
        PixelText.DrawFace(batch, pixel, label, new Vector2(x, y), TextFace.Body, 9.5f, GameBalance.Geld * (0.75f * alpha), 1.5f);
        x += labelWidth + 18;
        Icon(batch, pixel, UiIcon.Geld, new Vector2(x + 6, y + 4.5f), 0.7f, alpha);
        PixelText.DrawFace(batch, pixel, geldText, new Vector2(x + 16, y - 1f), TextFace.Display, 11f, GameBalance.SoulWhite * (0.9f * alpha));
        x += 16 + geldWidth + 18;
        Icon(batch, pixel, UiIcon.Glut, new Vector2(x + 6, y + 4.5f), 0.7f, alpha);
        PixelText.DrawFace(batch, pixel, glutText, new Vector2(x + 16, y - 1f), TextFace.Display, 11f, GameBalance.SoulWhite * (0.9f * alpha));
    }

    /// <summary>A hairline with a diamond ornament at its centre, fading toward both ends.</summary>
    public static void Divider(SpriteBatch batch, Texture2D pixel, float centerX, float y, float width, Color color)
    {
        if (!Loaded)
        {
            batch.FillRectangle(pixel, new Rectangle((int)(centerX - width / 2), (int)y, (int)width, 1), color * 0.6f);
            return;
        }

        float part = DividerPart / Density;
        float height = _divider!.Height / Density;
        float left = RenderResolution.SnapToOutputPixel(centerX - width / 2f);
        float middle = RenderResolution.SnapToOutputPixel(centerX - part / 2f);
        float top = RenderResolution.SnapToOutputPixel(y - height / 2f);
        float side = MathF.Max(0f, middle - left);
        DrawPart(batch, _divider, new Rectangle(0, 0, DividerPart, _divider.Height), new RectangleF(left, top, side, height), color);
        DrawPart(batch, _divider, new Rectangle(DividerPart, 0, DividerPart, _divider.Height), new RectangleF(middle, top, part, height), color);
        DrawPart(batch, _divider, new Rectangle(DividerPart * 2, 0, DividerPart, _divider.Height), new RectangleF(middle + part, top, side, height), color);
    }

    public static void FillDiamond(SpriteBatch batch, Texture2D pixel, Vector2 center, int radius, Color color)
    {
        for (int offset = -radius; offset <= radius; offset++)
        {
            int halfWidth = radius - Math.Abs(offset);
            batch.FillRectangle(pixel, new Rectangle((int)center.X - halfWidth, (int)center.Y + offset, halfWidth * 2 + 1, 1), color);
        }
    }

    private static RectangleF Snap(Rectangle bounds)
    {
        float left = RenderResolution.SnapToOutputPixel(bounds.X);
        float top = RenderResolution.SnapToOutputPixel(bounds.Y);
        return new RectangleF(left, top, RenderResolution.SnapToOutputPixel(bounds.Right) - left, RenderResolution.SnapToOutputPixel(bounds.Bottom) - top);
    }

    private static void DrawCentered(SpriteBatch batch, Texture2D texture, Vector2 center, float scale, Color color)
    {
        Vector2 size = new Vector2(texture.Width, texture.Height) / Density * scale;
        Vector2 topLeft = new(RenderResolution.SnapToOutputPixel(center.X - size.X / 2f), RenderResolution.SnapToOutputPixel(center.Y - size.Y / 2f));
        batch.Draw(texture, topLeft, null, color, 0f, Vector2.Zero, scale / Density, SpriteEffects.None, 0f);
    }

    private static void NineSlice(SpriteBatch batch, Texture2D texture, RectangleF area, int size, int corner, Color color)
    {
        float c = corner / Density;
        // Panels smaller than two corners shrink their corners rather than overlap.
        float cx = MathF.Min(c, area.Width / 2f);
        float cy = MathF.Min(c, area.Height / 2f);
        int middle = size - corner * 2;
        float[] xs = [area.X, area.X + cx, area.Right - cx, area.Right];
        float[] ys = [area.Y, area.Y + cy, area.Bottom - cy, area.Bottom];
        int[] sx = [0, corner, size - corner];
        int[] sw = [corner, middle, corner];
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                float width = xs[column + 1] - xs[column];
                float height = ys[row + 1] - ys[row];
                if (width <= 0f || height <= 0f)
                {
                    continue;
                }

                DrawPart(batch, texture, new Rectangle(sx[column], sx[row], sw[column], sw[row]), new RectangleF(xs[column], ys[row], width, height), color);
            }
        }
    }

    private static void DrawPart(SpriteBatch batch, Texture2D texture, Rectangle? source, RectangleF destination, Color color)
    {
        Rectangle from = source ?? texture.Bounds;
        Vector2 scale = new(destination.Width / from.Width, destination.Height / from.Height);
        batch.Draw(texture, new Vector2(destination.X, destination.Y), from, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}
