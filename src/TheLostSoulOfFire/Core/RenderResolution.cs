using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Core;

/// <summary>
/// Separates the logical resolution (gameplay, camera, HUD layout, pointer input) from the
/// resolution the frame is drawn at. Everything is laid out in logical units and scaled by
/// <see cref="ScaleMatrix"/> when drawn, so a different output size only changes <see cref="Scale"/>.
/// </summary>
public static class RenderResolution
{
    public const int LogicalWidth = 1280;
    public const int LogicalHeight = 720;
    public const float Scale = 1.5f;

    public const int OutputWidth = (int)(LogicalWidth * Scale);
    public const int OutputHeight = (int)(LogicalHeight * Scale);

    public static Rectangle OutputBounds => new(0, 0, OutputWidth, OutputHeight);

    public static Matrix ScaleMatrix { get; } = Matrix.CreateScale(Scale, Scale, 1f);

    /// <summary>Appends the output scale to a transform expressed in logical units, such as a camera transform.</summary>
    public static Matrix ToOutput(Matrix logicalTransform) => logicalTransform * ScaleMatrix;

    /// <summary>Snaps a logical coordinate so it lands on a whole output pixel.</summary>
    public static float SnapToOutputPixel(float logical) => MathF.Round(logical * Scale) / Scale;

    /// <summary>Size in whole output pixels of a square that is <paramref name="logicalSize"/> logical units wide; never below one pixel.</summary>
    public static int ToOutputPixels(int logicalSize) =>
        Math.Max(1, (int)MathF.Round(logicalSize * Scale, MidpointRounding.AwayFromZero));
}
