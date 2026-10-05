using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Rendering;

public static class ShapeRenderer
{
    public static void FillEllipse(this SpriteBatch batch, Texture2D pixel, Vector2 center, float width, float height, Color color)
    {
        for (int y = -(int)height; y <= (int)height; y += 2)
        {
            float extent = width * MathF.Sqrt(MathF.Max(0f, 1f - y * y / (height * height)));
            batch.DrawLine(pixel, center + new Vector2(-extent, y), center + new Vector2(extent, y), color, 2f);
        }
    }

    public static void FillRectangle(this SpriteBatch batch, Texture2D pixel, Rectangle rectangle, Color color) =>
        batch.Draw(pixel, rectangle, color);

    public static void DrawRectangle(this SpriteBatch batch, Texture2D pixel, Rectangle rectangle, Color color, float thickness = 2f)
    {
        batch.DrawLine(pixel, new Vector2(rectangle.Left, rectangle.Top), new Vector2(rectangle.Right, rectangle.Top), color, thickness);
        batch.DrawLine(pixel, new Vector2(rectangle.Right, rectangle.Top), new Vector2(rectangle.Right, rectangle.Bottom), color, thickness);
        batch.DrawLine(pixel, new Vector2(rectangle.Right, rectangle.Bottom), new Vector2(rectangle.Left, rectangle.Bottom), color, thickness);
        batch.DrawLine(pixel, new Vector2(rectangle.Left, rectangle.Bottom), new Vector2(rectangle.Left, rectangle.Top), color, thickness);
    }

    public static void DrawLine(this SpriteBatch batch, Texture2D pixel, Vector2 start, Vector2 end, Color color, float thickness = 2f)
    {
        Vector2 delta = end - start;
        if (delta.LengthSquared() < 0.001f)
        {
            return;
        }

        batch.Draw(
            pixel,
            start,
            null,
            color,
            MathF.Atan2(delta.Y, delta.X),
            new Vector2(0f, 0.5f),
            new Vector2(delta.Length(), thickness),
            SpriteEffects.None,
            0f);
    }

    public static void FillCircle(this SpriteBatch batch, Texture2D pixel, Vector2 center, float radius, Color color)
    {
        int roundedRadius = Math.Max(1, (int)MathF.Ceiling(radius));
        for (int y = -roundedRadius; y <= roundedRadius; y += 2)
        {
            float halfWidth = MathF.Sqrt(MathF.Max(0f, radius * radius - y * y));
            batch.DrawLine(pixel, center + new Vector2(-halfWidth, y), center + new Vector2(halfWidth, y), color, 2f);
        }
    }

    public static void DrawCircle(this SpriteBatch batch, Texture2D pixel, Vector2 center, float radius, Color color, float thickness = 2f, int segments = 32)
    {
        Vector2 previous = center + new Vector2(radius, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float angle = MathHelper.TwoPi * i / segments;
            Vector2 next = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            batch.DrawLine(pixel, previous, next, color, thickness);
            previous = next;
        }
    }

    public static void DrawArc(this SpriteBatch batch, Texture2D pixel, Vector2 center, float radius, float startAngle, float sweep, Color color, float thickness, int segments = 24)
    {
        Vector2 previous = center + new Vector2(MathF.Cos(startAngle), MathF.Sin(startAngle)) * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = startAngle + sweep * i / segments;
            Vector2 next = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            batch.DrawLine(pixel, previous, next, color, thickness);
            previous = next;
        }
    }

    // Dummies for Visual-IDs without graphics. Deliberately unlike any authored art:
    // a flat grey body with a magenta outline, so a missing asset is never mistaken
    // for a finished one, while size, foot point and facing stay readable.
    private static readonly Color DummyFill = new(86, 84, 96);
    private static readonly Color DummyLine = new(232, 72, 196);

    /// <summary>A character silhouette in world size, centred like a sprite, with a facing marker.</summary>
    public static void DrawCharacterDummy(this SpriteBatch batch, Texture2D pixel, Vector2 center, Vector2 worldSize, Vector2 facing, Color tint)
    {
        float halfWidth = worldSize.X * 0.2f;
        float halfHeight = worldSize.Y * 0.36f;
        Vector2 foot = center + new Vector2(0f, halfHeight);
        batch.FillEllipse(pixel, foot, halfWidth * 1.2f, halfWidth * 0.36f, Color.Black * 0.45f);
        batch.FillEllipse(pixel, center + new Vector2(0f, halfHeight * 0.18f), halfWidth, halfHeight * 0.82f, DummyFill.MultiplyBy(tint));
        float headRadius = halfWidth * 0.62f;
        Vector2 head = center - new Vector2(0f, halfHeight * 0.72f);
        batch.FillCircle(pixel, head, headRadius, DummyFill.MultiplyBy(tint));
        batch.DrawCircle(pixel, head, headRadius, DummyLine, 2f, 16);
        batch.DrawRectangle(pixel, new Rectangle((int)(center.X - worldSize.X * 0.5f), (int)(center.Y - worldSize.Y * 0.5f), (int)worldSize.X, (int)worldSize.Y), DummyLine * 0.45f, 1f);

        Vector2 direction = facing.LengthSquared() > 0.0001f ? Vector2.Normalize(facing) : Vector2.UnitY;
        Vector2 tip = center + direction * worldSize.X * 0.46f;
        batch.DrawLine(pixel, center, tip, DummyLine, 3f);
        batch.FillCircle(pixel, tip, 4f, DummyLine);
    }

    /// <summary>A burst of spokes for an effect; <paramref name="progress"/> 0..1 fades it out.</summary>
    public static void DrawEffectDummy(this SpriteBatch batch, Texture2D pixel, Vector2 center, float radius, float rotation, float progress)
    {
        float alpha = 1f - MathHelper.Clamp(progress, 0f, 1f);
        float r = radius * (0.5f + 0.5f * MathHelper.Clamp(progress, 0f, 1f));
        batch.DrawCircle(pixel, center, r, DummyLine * alpha, 2f, 20);
        for (int i = 0; i < 8; i++)
        {
            float angle = rotation + MathHelper.TwoPi * i / 8f;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            batch.DrawLine(pixel, center + direction * r * 0.35f, center + direction * r, DummyLine * (0.7f * alpha), 2f);
        }
    }

    /// <summary>A box standing on its foot point, for props, environment layers and sprites.</summary>
    public static void DrawPropDummy(this SpriteBatch batch, Texture2D pixel, Rectangle bounds, Vector2 foot, float alpha = 1f)
    {
        batch.FillRectangle(pixel, bounds, DummyFill * (0.85f * alpha));
        batch.DrawRectangle(pixel, bounds, DummyLine * alpha, 2f);
        batch.DrawLine(pixel, new Vector2(bounds.Left, bounds.Top), new Vector2(bounds.Right, bounds.Bottom), DummyLine * (0.5f * alpha), 1f);
        batch.DrawLine(pixel, new Vector2(bounds.Right, bounds.Top), new Vector2(bounds.Left, bounds.Bottom), DummyLine * (0.5f * alpha), 1f);
        batch.DrawLine(pixel, foot - new Vector2(8f, 0f), foot + new Vector2(8f, 0f), DummyLine * alpha, 3f);
        batch.DrawLine(pixel, foot - new Vector2(0f, 8f), foot + new Vector2(0f, 8f), DummyLine * alpha, 3f);
    }

    /// <summary>
    /// Draws a weapon or other rotated sprite centred on <paramref name="position"/>; without a
    /// texture, a dummy bar of <paramref name="nominalWidth"/> × <paramref name="scale"/> marks it.
    /// </summary>
    public static void DrawSpriteOrDummy(this SpriteBatch batch, Texture2D pixel, Texture2D? texture, Vector2 position, float rotation, float scale, float nominalWidth = 256f)
    {
        if (texture is null)
        {
            Vector2 half = new Vector2(MathF.Cos(rotation), MathF.Sin(rotation)) * nominalWidth * scale * 0.5f;
            batch.DrawLine(pixel, position - half, position + half, DummyFill, 6f);
            batch.DrawLine(pixel, position - half, position + half, DummyLine, 2f);
            return;
        }

        batch.Draw(texture, position, null, Color.White, rotation, new Vector2(texture.Width, texture.Height) * 0.5f, scale, SpriteEffects.None, 0f);
    }

    private static Color MultiplyBy(this Color color, Color tint) =>
        new(color.R * tint.R / 255, color.G * tint.G / 255, color.B * tint.B / 255, color.A * tint.A / 255);
}
