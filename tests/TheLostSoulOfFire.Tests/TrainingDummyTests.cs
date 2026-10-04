using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class TrainingDummyTests
{
    private static readonly Vector2 Anchor = GameBalance.CombatBounds.Center.ToVector2();

    private static void Step(TrainingDummy dummy, float seconds)
    {
        Player player = new(Anchor + new Vector2(300f, 0f));
        ParticleSystem particles = new();
        ScreenEffects screenEffects = new();
        for (float t = 0f; t < seconds; t += 0.05f)
        {
            dummy.Update(0.05f, player, [], GameBalance.CombatBounds, particles, screenEffects);
        }
    }

    [TestMethod]
    public void Hits_LowerHealth_AndAddUp()
    {
        TrainingDummy dummy = new(Anchor);
        dummy.ApplyDamage(new DamageInfo(40, Vector2.Zero, Anchor));
        dummy.ApplyDamage(new DamageInfo(25, Vector2.Zero, Anchor, IsSoulCoreHit: true));

        Assert.AreEqual(GameBalance.TrainingDummyMaxHealth - 65, dummy.Health);
        Assert.AreEqual(65, dummy.DamageSinceRefill);
        Assert.AreEqual(25, dummy.LastHit);
        Assert.AreEqual(2, dummy.Numbers.Count);
        Assert.IsTrue(dummy.Numbers[1].IsCoreHit);
    }

    [TestMethod]
    public void NeverDies_AndGivesNoGlut()
    {
        TrainingDummy dummy = new(Anchor);
        dummy.ApplyDamage(new DamageInfo(GameBalance.TrainingDummyMaxHealth * 3, Vector2.Zero, Anchor));

        Assert.IsTrue(dummy.IsAlive);
        Assert.AreEqual(1, dummy.Health);
        Assert.AreEqual(GameBalance.TrainingDummyMaxHealth * 3, dummy.DamageSinceRefill);
        Assert.IsFalse(dummy.TryClaimReward(out _));
        Assert.AreEqual(0, dummy.GlutReward);
    }

    [TestMethod]
    public void Refills_AfterPauseWithoutHits()
    {
        TrainingDummy dummy = new(Anchor);
        dummy.ApplyDamage(new DamageInfo(100, Vector2.Zero, Anchor));

        Step(dummy, GameBalance.TrainingDummyRefillDelay - 0.5f);
        Assert.AreEqual(GameBalance.TrainingDummyMaxHealth - 100, dummy.Health);

        dummy.ApplyDamage(new DamageInfo(10, Vector2.Zero, Anchor));
        Step(dummy, GameBalance.TrainingDummyRefillDelay - 0.5f);
        Assert.AreEqual(110, dummy.DamageSinceRefill, "a new hit restarts the pause");

        Step(dummy, 1f);
        Assert.AreEqual(GameBalance.TrainingDummyMaxHealth, dummy.Health);
        Assert.AreEqual(0, dummy.DamageSinceRefill);
        Assert.AreEqual(0, dummy.Numbers.Count);
    }

    [TestMethod]
    public void Knockback_DoesNotMoveIt()
    {
        TrainingDummy dummy = new(Anchor);
        dummy.ApplyDamage(new DamageInfo(20, new Vector2(900f, 300f), Anchor));
        Step(dummy, 0.5f);

        Assert.AreEqual(Anchor, dummy.Position);
    }

    [TestMethod]
    public void SandboxSpawner_OffersTheDummy()
    {
        Assert.IsInstanceOfType<TrainingDummy>(SandboxSpawner.Create(SandboxEnemyKind.TrainingDummy, Anchor, 1));
        Assert.AreEqual("TRAININGSPUPPE", SandboxSpawner.Label(SandboxEnemyKind.TrainingDummy));
    }
}
