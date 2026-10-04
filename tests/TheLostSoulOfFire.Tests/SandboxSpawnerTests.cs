using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Menu;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class SandboxSpawnerTests
{
    private static readonly Rectangle Bounds = GameBalance.CombatBounds;

    [TestMethod]
    public void EveryEnemyKind_HasASpawnEntry_BeforeRemoveAll()
    {
        DevMenuEntry[] enemies = SandboxDevMenuEntries.All.Where(entry => entry.Section == DevMenuSection.Enemies).ToArray();
        SandboxEnemyKind[] kinds = Enum.GetValues<SandboxEnemyKind>();

        Assert.AreEqual(kinds.Length + 1, enemies.Length);
        CollectionAssert.AreEqual(kinds, enemies.Take(kinds.Length).Select(entry => SandboxDevMenuEntries.SpawnKind(entry)!.Value).ToArray());
        Assert.AreEqual(SandboxDevMenuEntries.RemoveEnemies, enemies[^1].Id);
        Assert.IsTrue(enemies.All(entry => entry.Kind == DevMenuEntryKind.Action));
        Assert.IsNull(SandboxDevMenuEntries.SpawnKind(SandboxDevMenuEntries.All[0]));
    }

    [TestMethod]
    public void Create_BuildsTheMatchingType()
    {
        foreach (SandboxEnemyKind kind in Enum.GetValues<SandboxEnemyKind>())
        {
            Enemy enemy = SandboxSpawner.Create(kind, new Vector2(400f, 400f), 7);
            Assert.IsTrue(SandboxSpawner.IsKind(enemy, kind), kind.ToString());
            Assert.AreEqual(SandboxSpawner.Radius(kind), enemy.Radius, kind.ToString());
            Assert.IsTrue(enemy.IsAlive);
        }
    }

    [TestMethod]
    public void Positions_StayInsideBounds_AndAwayFromPlayer()
    {
        Vector2[] players =
        [
            Bounds.Center.ToVector2(),
            new(Bounds.Left + 30f, Bounds.Top + 30f),
            new(Bounds.Right - 30f, Bounds.Bottom - 30f),
            new(Bounds.Center.X, Bounds.Bottom - 30f)
        ];

        foreach (Vector2 player in players)
        {
            for (int i = 0; i < 12; i++)
            {
                float radius = GameBalance.DevourerRadius;
                Vector2 position = SandboxSpawner.ChoosePosition(Bounds, player, i, radius);
                Assert.IsTrue(position.X - radius >= Bounds.Left && position.X + radius <= Bounds.Right, $"{player} #{i} x");
                Assert.IsTrue(position.Y - radius >= Bounds.Top && position.Y + radius <= Bounds.Bottom, $"{player} #{i} y");
                Assert.IsTrue(Vector2.Distance(position, player) >= SandboxSpawner.MinPlayerDistance, $"{player} #{i} too close");
            }
        }
    }

    [TestMethod]
    public void ConsecutiveSpawns_DoNotStack()
    {
        Vector2 player = Bounds.Center.ToVector2();
        Vector2[] positions = Enumerable.Range(0, 6)
            .Select(i => SandboxSpawner.ChoosePosition(Bounds, player, i, GameBalance.HollowRadius))
            .ToArray();

        for (int i = 0; i < positions.Length; i++)
        {
            for (int j = i + 1; j < positions.Length; j++)
            {
                Assert.IsTrue(Vector2.Distance(positions[i], positions[j]) > GameBalance.HollowRadius * 2f, $"{i} and {j} overlap");
            }
        }
    }
}
