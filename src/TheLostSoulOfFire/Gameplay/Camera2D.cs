using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Gameplay;

public sealed class Camera2D
{
    public Matrix Matrix { get; private set; } = Matrix.Identity;
    public Matrix Inverse { get; private set; } = Matrix.Identity;

    public void Follow(Vector2 position, int viewportWidth, int viewportHeight, Rectangle levelBounds)
    {
        float halfWidth = viewportWidth * 0.5f;
        float halfHeight = viewportHeight * 0.5f;
        float minX = levelBounds.Left + halfWidth;
        float maxX = levelBounds.Right - halfWidth;
        float minY = levelBounds.Top + halfHeight;
        float maxY = levelBounds.Bottom - halfHeight;
        float x = maxX < minX ? levelBounds.Center.X : MathHelper.Clamp(position.X, minX, maxX);
        float y = maxY < minY ? levelBounds.Center.Y : MathHelper.Clamp(position.Y, minY, maxY);
        Matrix = Matrix.CreateTranslation(-x, -y, 0f) * Matrix.CreateTranslation(halfWidth, halfHeight, 0f);
        Inverse = Matrix.Invert(Matrix);
    }

    public Vector2 ScreenToWorld(Vector2 screenPosition) => Vector2.Transform(screenPosition, Inverse);
}
