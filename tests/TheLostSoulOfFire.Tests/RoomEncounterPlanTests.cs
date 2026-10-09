using System;
using System.Linq;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Game.Levels;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class RoomEncounterPlanTests
{
    private const int HighestProgress = 40;

    public TestContext? TestContext { get; set; }

    [TestMethod]
    public void For_ProgressOne_OnlyHollow()
    {
        RoomEncounterPlan plan = RoomEncounterPlan.For(1, 4711);

        Assert.AreEqual(1, plan.Waves.Count);
        Assert.AreEqual(GameBalance.RoomEnemiesPerWaveBase, plan.TotalEnemies);
        Assert.AreEqual(0, plan.HeavyEnemies);
        Assert.AreEqual(0, plan.Devourers);
    }

    [TestMethod]
    public void For_BelowDevourerThreshold_NoDevourer()
    {
        for (int progress = 1; progress < GameBalance.RoomDevourerFromProgress; progress++)
        {
            RoomEncounterPlan plan = RoomEncounterPlan.For(progress, 4711);

            Assert.AreEqual(0, plan.Devourers, $"progress {progress}");
        }
    }

    [TestMethod]
    public void For_ProgressOneToForty_NeverDecreases()
    {
        RoomEncounterPlan previous = RoomEncounterPlan.For(1, 4711);

        for (int progress = 2; progress <= HighestProgress; progress++)
        {
            RoomEncounterPlan current = RoomEncounterPlan.For(progress, 4711);

            Assert.IsTrue(current.Waves.Count >= previous.Waves.Count, $"waves at progress {progress}");
            Assert.IsTrue(current.Waves[0].Total >= previous.Waves[0].Total, $"enemies per wave at progress {progress}");
            Assert.IsTrue(current.TotalEnemies >= previous.TotalEnemies, $"enemies at progress {progress}");
            Assert.IsTrue(current.HeavyEnemies >= previous.HeavyEnemies, $"heavy enemies at progress {progress}");
            previous = current;
        }
    }

    [TestMethod]
    public void For_HighProgress_RespectsMaxima()
    {
        RoomEncounterPlan plan = RoomEncounterPlan.For(HighestProgress, 4711);

        Assert.AreEqual(GameBalance.RoomWavesMax, plan.Waves.Count);
        Assert.IsTrue(plan.Waves.All(wave => wave.Total <= GameBalance.RoomEnemiesPerWaveMax));
    }

    [TestMethod]
    public void For_SameSeed_SamePlan()
    {
        ArenaPush[] first = RoomEncounterPlan.For(7, 11).Waves.ToArray();
        ArenaPush[] second = RoomEncounterPlan.For(7, 11).Waves.ToArray();

        CollectionAssert.AreEqual(first, second);
    }

    [TestMethod]
    public void For_DifferentSeeds_SameCountsPerKind()
    {
        RoomEncounterPlan first = RoomEncounterPlan.For(9, 1);
        RoomEncounterPlan second = RoomEncounterPlan.For(9, 2);

        Assert.AreEqual(first.TotalEnemies, second.TotalEnemies);
        Assert.AreEqual(first.HeavyEnemies, second.HeavyEnemies);
        Assert.AreEqual(first.Devourers, second.Devourers);
        Assert.AreEqual(first.Waves.Sum(wave => wave.Burning), second.Waves.Sum(wave => wave.Burning));
    }

    [TestMethod]
    public void For_AnyProgress_EveryWaveHasHollow()
    {
        for (int progress = 1; progress <= HighestProgress; progress++)
        {
            for (int seed = 0; seed < 20; seed++)
            {
                RoomEncounterPlan plan = RoomEncounterPlan.For(progress, seed);

                Assert.IsTrue(plan.Waves.All(wave => wave.Hollow >= 1), $"progress {progress}, seed {seed}");
            }
        }
    }

    [TestMethod]
    public void For_ProgressBelowOne_Throws()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => RoomEncounterPlan.For(0, 4711));
    }

    [TestMethod]
    public void For_ProgressOneToEighteen_PrintsOverview()
    {
        TestContext?.WriteLine("Fortschritt | Wellen | Gegner | Burning | Devourer");
        for (int progress = 1; progress <= 18; progress++)
        {
            RoomEncounterPlan plan = RoomEncounterPlan.For(progress, 4711);
            int burning = plan.Waves.Sum(wave => wave.Burning);
            TestContext?.WriteLine($"{progress,10} | {plan.Waves.Count,6} | {plan.TotalEnemies,7} | {burning,7} | {plan.Devourers,8}");
        }
    }
}
