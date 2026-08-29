using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Gameplay;

public static class CollisionMath
{
    public static Rectangle BoundsAt(Vector2 center, Point size) => new(
        (int)MathF.Round(center.X - size.X * 0.5f),
        (int)MathF.Round(center.Y - size.Y * 0.5f),
        size.X,
        size.Y);

    public static Vector2 Move(Vector2 position, Vector2 delta, Point size, Rectangle levelBounds, ReadOnlySpan<Rectangle> walls)
    {
        position.X += delta.X;
        Rectangle bounds = BoundsAt(position, size);
        ResolveBoundsX(ref position, bounds, levelBounds);
        bounds = BoundsAt(position, size);
        for (int i = 0; i < walls.Length; i++)
        {
            Rectangle wall = walls[i];
            if (!bounds.Intersects(wall)) continue;
            position.X = delta.X > 0f ? wall.Left - size.X * 0.5f : wall.Right + size.X * 0.5f;
            bounds = BoundsAt(position, size);
        }

        position.Y += delta.Y;
        bounds = BoundsAt(position, size);
        ResolveBoundsY(ref position, bounds, levelBounds);
        bounds = BoundsAt(position, size);
        for (int i = 0; i < walls.Length; i++)
        {
            Rectangle wall = walls[i];
            if (!bounds.Intersects(wall)) continue;
            position.Y = delta.Y > 0f ? wall.Top - size.Y * 0.5f : wall.Bottom + size.Y * 0.5f;
            bounds = BoundsAt(position, size);
        }
        return position;
    }

    private static void ResolveBoundsX(ref Vector2 position, Rectangle bounds, Rectangle levelBounds)
    {
        if (bounds.Left < levelBounds.Left) position.X += levelBounds.Left - bounds.Left;
        else if (bounds.Right > levelBounds.Right) position.X -= bounds.Right - levelBounds.Right;
    }

    private static void ResolveBoundsY(ref Vector2 position, Rectangle bounds, Rectangle levelBounds)
    {
        if (bounds.Top < levelBounds.Top) position.Y += levelBounds.Top - bounds.Top;
        else if (bounds.Bottom > levelBounds.Bottom) position.Y -= bounds.Bottom - levelBounds.Bottom;
    }
}
