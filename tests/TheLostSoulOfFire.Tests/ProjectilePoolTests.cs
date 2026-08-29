using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Ecs;
using TheLostSoulOfFire.Gameplay;
using TheLostSoulOfFire.Levels;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class ProjectilePoolTests
{
    [TestMethod]
    public void Projectile_HitsOnlyOnceAndReturnsToPool()
    {
        using GameWorld world = new();
        GameEntity enemyEntity = world.CreateEntity("Enemy", new Vector2(114, 100));
        HitboxBehaviour hitbox = new(30, 30);
        HealthBehaviour health = new(3);
        enemyEntity.Add(hitbox).Add(health);
        world.FlushChanges();
        EnemyActor actor = new(enemyEntity, hitbox, health);
        List<EnemyActor> enemies = [actor];
        int hitCount = 0;
        ProjectilePool pool = new(1, (enemy, _) =>
        {
            hitCount++;
            enemy.Health.TakeDamage(1);
        });
        LevelRuntime level = new("Test", new Rectangle(0, 0, 500, 300), Vector2.Zero, [], Rectangle.Empty, Rectangle.Empty, []);

        Assert.IsTrue(pool.TrySpawn(new Vector2(10, 100), Vector2.UnitX));
        Assert.IsFalse(pool.TrySpawn(new Vector2(10, 100), Vector2.UnitX));
        pool.Update(Frame(0.2), level, enemies);
        pool.Update(Frame(0.2), level, enemies);

        Assert.AreEqual(1, hitCount);
        Assert.AreEqual(2, health.Health);
        Assert.AreEqual(0, pool.ActiveCount);
        Assert.IsTrue(pool.TrySpawn(new Vector2(10, 100), Vector2.UnitX));
    }

    private static GameTime Frame(double seconds) => new(TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(seconds));
}
