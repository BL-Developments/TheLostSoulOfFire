using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Core;

public sealed class ResolutionManager
{
    public ResolutionManager(int virtualWidth, int virtualHeight)
    {
        VirtualWidth = virtualWidth;
        VirtualHeight = virtualHeight;
        Destination = new Rectangle(0, 0, virtualWidth, virtualHeight);
    }

    public int VirtualWidth { get; }
    public int VirtualHeight { get; }
    public Rectangle Destination { get; private set; }

    public void Update(int backBufferWidth, int backBufferHeight)
    {
        if (backBufferWidth <= 0 || backBufferHeight <= 0) return;
        int width;
        int height;
        if ((long)backBufferWidth * VirtualHeight <= (long)backBufferHeight * VirtualWidth)
        {
            width = backBufferWidth;
            height = Math.Max(1, (int)((long)backBufferWidth * VirtualHeight / VirtualWidth));
        }
        else
        {
            height = backBufferHeight;
            width = Math.Max(1, (int)((long)backBufferHeight * VirtualWidth / VirtualHeight));
        }
        Destination = new Rectangle((backBufferWidth - width) / 2, (backBufferHeight - height) / 2, width, height);
    }

    public Vector2 WindowToVirtual(Point windowPosition)
    {
        Rectangle destination = Destination;
        if (destination.Width <= 0 || destination.Height <= 0) return Vector2.Zero;
        float x = (windowPosition.X - destination.X) * VirtualWidth / (float)destination.Width;
        float y = (windowPosition.Y - destination.Y) * VirtualHeight / (float)destination.Height;
        return new Vector2(x, y);
    }
}
