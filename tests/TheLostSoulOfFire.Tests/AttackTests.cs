using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Gameplay;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class AttackTests
{
    [TestMethod]
    public void AimDirection_IsNormalizedAndPointsAtTarget()
    {
        Vector2 direction = AimDirection.FromTo(new Vector2(10f, 20f), new Vector2(13f, 24f));

        Assert.AreEqual(0.6f, direction.X, 0.0001f);
        Assert.AreEqual(0.8f, direction.Y, 0.0001f);
        Assert.AreEqual(Vector2.Zero, AimDirection.FromTo(Vector2.One, Vector2.One));
    }

    [TestMethod]
    public void AttackCadence_AllowsExactlyOneShotPerCooldown()
    {
        AttackCadence cadence = new(0.24f);

        Assert.IsTrue(cadence.CanFire);
        cadence.ConfirmShot();
        Assert.IsFalse(cadence.CanFire);
        cadence.Update(0.23f);
        Assert.IsFalse(cadence.CanFire);
        cadence.Update(0.01f);
        Assert.IsTrue(cadence.CanFire);
    }
}
