using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Effects;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class ScreenEffectsTests
{
    [TestMethod]
    public void MotionScale_AffectsCameraShakeAndKickOnly()
    {
        ScreenEffects effects = new();
        effects.AddShake(1f, 10f);
        effects.AddCameraKick(Vector2.UnitX, 8f);
        effects.BeginHitstop(0.5f);
        effects.Update(0.01f);
        Vector2 normal = effects.CameraOffset;
        bool hitStopped = effects.IsHitStopped;

        effects.MotionScale = 0.35f;
        Vector2 reduced = effects.CameraOffset;
        Assert.AreEqual(normal.Length() * 0.35f, reduced.Length(), 0.001f);
        Assert.AreEqual(hitStopped, effects.IsHitStopped);

        effects.MotionScale = 0f;
        Assert.AreEqual(Vector2.Zero, effects.CameraOffset);
        Assert.IsTrue(effects.IsHitStopped);
    }
}
