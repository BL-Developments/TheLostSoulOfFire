using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Core;
using TheLostSoulOfFire.Gameplay;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class CollisionAndResolutionTests
{
    [TestMethod]
    public void DiagonalInput_NormalizesToStraightSpeed()
    {
        Vector2 diagonal = new(1f, 1f);
        diagonal.Normalize();
        Assert.AreEqual(1f, diagonal.Length(), 0.0001f);
    }

    [TestMethod]
    public void Move_BlocksWallAndPreservesFreeAxis()
    {
        Rectangle bounds = new(0, 0, 500, 500);
        Rectangle[] walls = [new Rectangle(200, 100, 40, 300)];
        Vector2 result = CollisionMath.Move(new Vector2(170, 150), new Vector2(60, 70), new Point(20, 20), bounds, walls);

        Assert.AreEqual(190f, result.X, 0.001f);
        Assert.AreEqual(220f, result.Y, 0.001f);
    }

    [TestMethod]
    public void Move_KeepsHitboxInsideLevelBounds()
    {
        Vector2 result = CollisionMath.Move(new Vector2(20, 20), new Vector2(-100, -100), new Point(20, 20), new Rectangle(0, 0, 300, 200), []);
        Assert.AreEqual(new Vector2(10, 10), result);
    }

    [TestMethod]
    public void Resolution_UsesLetterboxingWithoutDistortion()
    {
        ResolutionManager resolution = new(960, 540);
        resolution.Update(1000, 1000);
        Assert.AreEqual(new Rectangle(0, 219, 1000, 562), resolution.Destination);
    }

    [TestMethod]
    public void Resolution_MapsWindowCenterToVirtualCenter()
    {
        ResolutionManager resolution = new(960, 540);
        resolution.Update(1920, 1080);
        Assert.AreEqual(new Vector2(480, 270), resolution.WindowToVirtual(new Point(960, 540)));
    }

    [TestMethod]
    public void Resolution_MapsWindowCenterToVirtualCenter_AtProductionResolution()
    {
        // Matches GameBalance.BackBufferWidth/Height so the window→virtual mapping is
        // verified at the resolution Game1 actually uses.
        ResolutionManager resolution = new(1280, 720);
        resolution.Update(1920, 1080);
        Assert.AreEqual(new Vector2(640, 360), resolution.WindowToVirtual(new Point(960, 540)));
    }

    [TestMethod]
    public void Resolution_DownscaledWindow_StillMapsProportionally()
    {
        ResolutionManager resolution = new(1280, 720);
        resolution.Update(640, 360);
        Assert.AreEqual(new Rectangle(0, 0, 640, 360), resolution.Destination);
        Assert.AreEqual(new Vector2(640, 360), resolution.WindowToVirtual(new Point(320, 180)));
    }

    [TestMethod]
    public void Resolution_PillarboxedWindow_MapsSidebarPointsOutsideVirtualBounds()
    {
        // A narrower-than-16:9 window puts black bars on top and bottom; a pointer in
        // either bar must map outside the virtual viewport so it can never hit a menu
        // entry or HUD element.
        ResolutionManager resolution = new(1280, 720);
        resolution.Update(1000, 1000);
        Assert.AreEqual(new Rectangle(0, 219, 1000, 562), resolution.Destination);

        Vector2 topBar = resolution.WindowToVirtual(new Point(500, 50));
        Assert.IsTrue(topBar.Y < 0);

        Vector2 bottomBar = resolution.WindowToVirtual(new Point(500, 950));
        Assert.IsTrue(bottomBar.Y > 720);
    }

    [TestMethod]
    public void Resolution_UltrawideWindow_MapsSidebarPointsOutsideVirtualBounds()
    {
        // A wider-than-16:9 window puts black bars on the left and right instead.
        ResolutionManager resolution = new(1280, 720);
        resolution.Update(2560, 1080);
        Assert.AreEqual(new Rectangle(320, 0, 1920, 1080), resolution.Destination);

        Vector2 leftBar = resolution.WindowToVirtual(new Point(100, 540));
        Assert.IsTrue(leftBar.X < 0);

        Vector2 rightBar = resolution.WindowToVirtual(new Point(2460, 540));
        Assert.IsTrue(rightBar.X > 1280);
    }
}
