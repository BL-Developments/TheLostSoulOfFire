using System;
using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Tests.Visuals;

[TestClass]
public sealed class DeathFlameRampTests
{
    [TestMethod]
    public void Ramp_RunsFromDarkVioletToNearWhite()
    {
        Assert.AreEqual(GameBalance.DeepViolet, DeathFlameRamp.Evaluate(0f).Color);
        Assert.AreEqual(0f, DeathFlameRamp.Evaluate(0f).Alpha);
        Assert.AreEqual(GameBalance.DeathFlame, DeathFlameRamp.Evaluate(0.6f).Color);
        Assert.AreEqual(GameBalance.SoulWhite, DeathFlameRamp.Evaluate(1f).Color);
    }

    [TestMethod]
    public void Ramp_BrightensMonotonically_AndStaysViolet()
    {
        Color[] pixels = DeathFlameRamp.Pixels();
        float previous = -1f;
        foreach (Color pixel in pixels)
        {
            float luminance = 0.299f * pixel.R + 0.587f * pixel.G + 0.114f * pixel.B;
            Assert.IsTrue(luminance >= previous - 0.5f, "ramp must not get darker");
            previous = luminance;

            // Blue at least as strong as red and green: never orange, yellow or green.
            Assert.IsTrue(pixel.B >= pixel.R && pixel.B >= pixel.G, $"ramp colour {pixel} leaves the violet range");
        }
    }
}
