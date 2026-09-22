using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Ecs;
using TheLostSoulOfFire.Gameplay;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class HealthAndEcsTests
{
    [TestMethod]
    public void Health_BlocksDamageDuringInvulnerability()
    {
        HealthBehaviour health = new(3, 0.5f);
        Assert.IsTrue(health.TakeDamage(1));
        Assert.IsFalse(health.TakeDamage(1));
        health.Update(Frame(0.6));
        Assert.IsTrue(health.TakeDamage(1));
        Assert.AreEqual(1, health.Health);
    }

    [TestMethod]
    public void Health_StopsAtZero()
    {
        HealthBehaviour health = new(2);
        Assert.IsTrue(health.TakeDamage(10));
        Assert.AreEqual(0, health.Health);
        Assert.IsTrue(health.IsDead);
        Assert.IsFalse(health.TakeDamage(1));
    }

    [TestMethod]
    public void GameWorld_InvokesLifecycleAndDefersRemoval()
    {
        using GameWorld world = new();
        ProbeBehaviour probe = new();
        GameEntity entity = world.CreateEntity("Probe", Vector2.Zero).Add(probe);
        Assert.AreEqual(1, probe.AwakeCount);
        Assert.AreEqual(0, probe.StartCount);

        world.Update(Frame(1.0 / 60.0));
        Assert.AreEqual(1, probe.StartCount);
        Assert.AreEqual(1, probe.UpdateCount);
        Assert.AreEqual(1, world.Entities.Count);

        world.Destroy(entity);
        Assert.AreEqual(1, world.Entities.Count);
        world.Update(Frame(1.0 / 60.0));
        Assert.AreEqual(1, probe.DestroyCount);
        Assert.AreEqual(0, world.Entities.Count);
    }

    private static GameTime Frame(double seconds) => new(TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(seconds));

    private sealed class ProbeBehaviour : GameBehaviour
    {
        public int AwakeCount { get; private set; }
        public int StartCount { get; private set; }
        public int UpdateCount { get; private set; }
        public int DestroyCount { get; private set; }
        public override void Awake() => AwakeCount++;
        public override void Start() => StartCount++;
        public override void Update(GameTime gameTime) => UpdateCount++;
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch) { }
        public override void OnDestroy() => DestroyCount++;
    }
}
