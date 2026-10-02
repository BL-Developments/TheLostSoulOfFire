using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Core;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class RenderResolutionTests
{
    [TestMethod]
    public void Output_IsFullHd_AndLogicalStaysAtGameplayResolution()
    {
        Assert.AreEqual(1920, RenderResolution.OutputWidth);
        Assert.AreEqual(1080, RenderResolution.OutputHeight);
        Assert.AreEqual(RenderResolution.LogicalWidth, GameBalance.BackBufferWidth);
        Assert.AreEqual(RenderResolution.LogicalHeight, GameBalance.BackBufferHeight);
        Assert.AreEqual(new Rectangle(0, 0, 1920, 1080), RenderResolution.OutputBounds);
    }

    [TestMethod]
    public void ScaleMatrix_MapsLogicalCornersToOutputCorners()
    {
        Vector2 corner = Vector2.Transform(new Vector2(RenderResolution.LogicalWidth, RenderResolution.LogicalHeight), RenderResolution.ScaleMatrix);
        Assert.AreEqual(new Vector2(RenderResolution.OutputWidth, RenderResolution.OutputHeight), corner);
    }

    [TestMethod]
    public void ToOutput_AppliesLogicalTransformBeforeScaling()
    {
        // A camera that centres logical point (100, 50) on screen.
        Matrix camera = Matrix.CreateTranslation(540f, 310f, 0f);
        Vector2 output = Vector2.Transform(new Vector2(100f, 50f), RenderResolution.ToOutput(camera));
        Assert.AreEqual(new Vector2(960f, 540f), output);
    }

    [TestMethod]
    [DataRow(1, 2)]
    [DataRow(2, 3)]
    [DataRow(3, 5)]
    [DataRow(4, 6)]
    public void PixelFontCells_AreWholeOutputPixels(int scale, int expectedOutputPixels)
    {
        Assert.AreEqual(expectedOutputPixels, RenderResolution.ToOutputPixels(scale));
        Assert.AreEqual(expectedOutputPixels, PixelText.CellSize(scale) * RenderResolution.Scale, 0.0001f);
    }

    [TestMethod]
    public void PixelText_MeasureAtScaleTwo_IsUnchanged()
    {
        Assert.AreEqual(6 * 2 * 4, PixelText.Measure("SOUL", 2));
    }

    [TestMethod]
    public void SnapToOutputPixel_LandsOnWholeOutputPixels()
    {
        foreach (float logical in new[] { 0f, 0.4f, 13.3f, 639.5f, 1279f })
        {
            float output = RenderResolution.SnapToOutputPixel(logical) * RenderResolution.Scale;
            Assert.AreEqual(MathF.Round(output), output, 0.0001f, logical.ToString());
        }
    }
}
