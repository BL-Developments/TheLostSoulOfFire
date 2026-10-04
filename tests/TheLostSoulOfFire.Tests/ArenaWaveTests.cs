using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class ArenaWaveTests
{
    [TestMethod]
    public void Arena_HasTenWaves()
    {
        Assert.AreEqual(10, GameBalance.ArenaWaveCount);
    }

    [TestMethod]
    public void EarlyWaves_AreOneAuthoredPush_MatchingTheTable()
    {
        for (int wave = 1; wave <= 4; wave++)
        {
            Assert.IsTrue(ArenaWaves.HasAuthoredLayout(wave));
            Assert.AreEqual(1, ArenaWaves.Pushes(wave).Count, $"wave {wave}");
            ArenaPush push = ArenaWaves.Pushes(wave)[0];
            var layout = ArenaWaves.AuthoredLayout(wave);
            Assert.AreEqual(push.Hollow, layout.Count(entry => entry.Kind == ArenaEnemyKind.Hollow), $"wave {wave}");
            Assert.AreEqual(push.Burning, layout.Count(entry => entry.Kind == ArenaEnemyKind.Burning), $"wave {wave}");
            Assert.AreEqual(push.Devourer, layout.Count(entry => entry.Kind == ArenaEnemyKind.Devourer), $"wave {wave}");
        }
    }

    [TestMethod]
    public void EarlyWaves_KeepTheirPreviousComposition()
    {
        Assert.AreEqual(new ArenaPush(3, 0, 0), ArenaWaves.Pushes(1)[0]);
        Assert.AreEqual(new ArenaPush(2, 2, 0), ArenaWaves.Pushes(2)[0]);
        Assert.AreEqual(new ArenaPush(2, 2, 1), ArenaWaves.Pushes(3)[0]);
        Assert.AreEqual(new ArenaPush(2, 3, 1), ArenaWaves.Pushes(4)[0]);
    }

    [TestMethod]
    public void LaterWaves_HaveSeveralPushes_AndTotalsNeverDrop()
    {
        int previous = 0;
        for (int wave = 1; wave <= GameBalance.ArenaWaveCount; wave++)
        {
            int total = ArenaWaves.Pushes(wave).Sum(push => push.Total);
            Assert.IsTrue(total >= previous, $"wave {wave} has {total} enemies, fewer than {previous}");
            previous = total;
            if (wave >= 5)
            {
                Assert.IsTrue(ArenaWaves.Pushes(wave).Count >= 2, $"wave {wave}");
            }

            foreach (ArenaPush push in ArenaWaves.Pushes(wave))
            {
                Assert.IsTrue(push.Total <= GameBalance.ArenaMaxAliveEnemies, $"wave {wave} has a push that can never fit");
            }
        }

        int last = ArenaWaves.Pushes(GameBalance.ArenaWaveCount).Sum(push => push.Total);
        for (int wave = 1; wave < GameBalance.ArenaWaveCount; wave++)
        {
            Assert.IsTrue(ArenaWaves.Pushes(wave).Sum(push => push.Total) < last, $"wave {wave}");
        }
    }

    [TestMethod]
    public void NextPush_ComesAfterTheInterval()
    {
        ArenaWaveRun run = new([new(3, 0, 0), new(2, 0, 0)]);
        run.TakeFirst();

        Assert.IsFalse(run.TryTakeNext(GameBalance.ArenaPushInterval - 0.1f, 3, out _));
        Assert.IsTrue(run.TryTakeNext(0.2f, 3, out ArenaPush push));
        Assert.AreEqual(new ArenaPush(2, 0, 0), push);
        Assert.IsTrue(run.AllPushesReleased);
    }

    [TestMethod]
    public void NextPush_ComesEarly_WhenTheFieldIsAlmostEmpty()
    {
        ArenaWaveRun run = new([new(3, 0, 0), new(2, 0, 0)]);
        run.TakeFirst();

        Assert.IsFalse(run.TryTakeNext(0.1f, GameBalance.ArenaPushEarlyAlive + 1, out _));
        Assert.IsTrue(run.TryTakeNext(0.1f, GameBalance.ArenaPushEarlyAlive, out _));
    }

    [TestMethod]
    public void NextPush_Waits_WhileItWouldExceedTheLimit()
    {
        ArenaWaveRun run = new([new(3, 0, 0), new(4, 0, 0)]);
        run.TakeFirst();
        int crowded = GameBalance.ArenaMaxAliveEnemies - 3;

        Assert.IsFalse(run.TryTakeNext(GameBalance.ArenaPushInterval + 1f, crowded, out _));
        Assert.IsFalse(run.AllPushesReleased);
        Assert.IsTrue(run.TryTakeNext(0.1f, GameBalance.ArenaMaxAliveEnemies - 4, out _));
    }

    [TestMethod]
    public void Wave_IsNotClearedUntilAllPushesAreOut_AndDiscardEndsIt()
    {
        ArenaWaveRun run = new([new(3, 0, 0), new(2, 0, 0), new(2, 0, 0)]);
        run.TakeFirst();
        Assert.IsFalse(run.AllPushesReleased);

        run.DiscardRemaining();
        Assert.IsTrue(run.AllPushesReleased);
        Assert.IsFalse(run.TryTakeNext(100f, 0, out _));
    }

    [TestMethod]
    public void SpawnPositions_KeepTheirDistanceToThePlayer_AndStayInside()
    {
        Rectangle bounds = GameBalance.CombatBounds;
        Vector2[] players = [bounds.Center.ToVector2(), ArenaWaves.SpawnPoints(bounds)[0], ArenaWaves.SpawnPoints(bounds)[4]];
        foreach (Vector2 player in players)
        {
            List<Vector2> positions = ArenaWaves.ChooseSpawnPositions(bounds, player, 9);
            Assert.AreEqual(9, positions.Count);
            foreach (Vector2 position in positions)
            {
                Assert.IsTrue(bounds.Contains(position), $"{position} lies outside");
                Assert.IsTrue(Vector2.Distance(position, player) >= GameBalance.ArenaSpawnMinPlayerDistance - 1f, $"{position} is too close to {player}");
            }

            Assert.AreEqual(positions.Count, positions.Distinct().Count(), "two enemies share a spot");
        }
    }

    [TestMethod]
    public void Chests_AppearAfterWavesThreeSixAndNine()
    {
        CollectionAssert.AreEqual(new[] { 3, 6, 9 }, GameBalance.ArenaChestWaves);
    }
}
