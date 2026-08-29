using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Rendering;

public sealed class Camera2D
{
    public Vector2 Position { get; private set; }
    public float Zoom { get; set; } = 1f;

    public Camera2D(Vector2 initialPosition)
    {
        Position = initialPosition;
    }

    public void Follow(Vector2 target, Rectangle worldBounds, Viewport viewport, float smoothing = 1f)
    {
        Position = Vector2.Lerp(Position, target, MathHelper.Clamp(smoothing, 0f, 1f));

        float halfWidth = viewport.Width / (2f * Zoom);
        float halfHeight = viewport.Height / (2f * Zoom);
        float minX = worldBounds.Left + halfWidth;
        float maxX = worldBounds.Right - halfWidth;
        float minY = worldBounds.Top + halfHeight;
        float maxY = worldBounds.Bottom - halfHeight;

        Position = new Vector2(
            minX <= maxX ? MathHelper.Clamp(Position.X, minX, maxX) : worldBounds.Center.X,
            minY <= maxY ? MathHelper.Clamp(Position.Y, minY, maxY) : worldBounds.Center.Y);
    }

    public Matrix GetTransform(Viewport viewport, Vector2 shakeOffset) =>
        Matrix.CreateTranslation(-Position.X, -Position.Y, 0f) *
        Matrix.CreateScale(Zoom, Zoom, 1f) *
        Matrix.CreateTranslation(viewport.Width * 0.5f + shakeOffset.X, viewport.Height * 0.5f + shakeOffset.Y, 0f);

    public Vector2 ScreenToWorld(Point screenPosition, Viewport viewport)
    {
        Matrix inverse = Matrix.Invert(GetTransform(viewport, Vector2.Zero));
        return Vector2.Transform(screenPosition.ToVector2(), inverse);
    }
}
