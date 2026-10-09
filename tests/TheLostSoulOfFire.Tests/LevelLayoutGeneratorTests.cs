using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Game.Levels;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class LevelLayoutGeneratorTests
{
    private const int SeedCount = 100;

    [TestMethod]
    public void Generate_AnySeed_StartsWithStartAndEndsWithLevelEnd()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            LevelLayout layout = LevelLayoutGenerator.Generate(seed, LevelLayoutSettings.Default);

            IReadOnlyList<LevelRoom> first = layout.Stages[0];
            IReadOnlyList<LevelRoom> last = layout.Stages[^1];
            Assert.AreEqual(1, first.Count);
            Assert.AreEqual(LevelRoomKind.Start, first[0].Kind);
            Assert.AreSame(first[0], layout.Start);
            Assert.AreEqual(1, last.Count);
            Assert.AreEqual(LevelRoomKind.LevelEnd, last[0].Kind);
            Assert.AreEqual(0, last[0].Exits.Count);
        }
    }

    [TestMethod]
    public void Generate_AnySeed_EachCombatStageHasOneOrTwoRooms()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            LevelLayout layout = LevelLayoutGenerator.Generate(seed, LevelLayoutSettings.Default);

            for (int stage = 1; stage < layout.Stages.Count - 1; stage++)
            {
                IReadOnlyList<LevelRoom> rooms = layout.Stages[stage];
                Assert.IsTrue(rooms.Count is 1 or 2, $"Seed {seed}, stage {stage}: {rooms.Count} rooms");
                foreach (LevelRoom room in rooms)
                {
                    Assert.AreEqual(LevelRoomKind.Combat, room.Kind);
                }
            }
        }
    }

    [TestMethod]
    public void Generate_AnySeed_EveryRoomReachableAndLeadsToLevelEnd()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            LevelLayout layout = LevelLayoutGenerator.Generate(seed, LevelLayoutSettings.Default);
            List<LevelRoom> allRooms = AllRooms(layout);
            int levelEndId = layout.Stages[^1][0].Id;

            HashSet<int> reachable = ReachableFrom(layout, layout.Start.Id);

            Assert.AreEqual(allRooms.Count, reachable.Count, $"Seed {seed}: unreachable rooms");
            foreach (LevelRoom room in allRooms)
            {
                Assert.IsTrue(ReachableFrom(layout, room.Id).Contains(levelEndId), $"Seed {seed}: room {room.Id} is a dead end");
            }
        }
    }

    [TestMethod]
    public void Generate_AnySeed_NoRoomHasMoreThanTwoExits()
    {
        for (int seed = 0; seed < SeedCount; seed++)
        {
            LevelLayout layout = LevelLayoutGenerator.Generate(seed, LevelLayoutSettings.Default);

            foreach (LevelRoom room in AllRooms(layout))
            {
                Assert.IsTrue(room.Exits.Count <= 2, $"Seed {seed}, room {room.Id}: {room.Exits.Count} exits");
            }
        }
    }

    [TestMethod]
    public void Generate_SameSeed_ReturnsSameLayout()
    {
        LevelLayout first = LevelLayoutGenerator.Generate(4711, LevelLayoutSettings.Default);

        LevelLayout second = LevelLayoutGenerator.Generate(4711, LevelLayoutSettings.Default);

        Assert.AreEqual(first.Seed, second.Seed);
        Assert.AreEqual(first.Stages.Count, second.Stages.Count);
        for (int stage = 0; stage < first.Stages.Count; stage++)
        {
            Assert.AreEqual(first.Stages[stage].Count, second.Stages[stage].Count);
            for (int index = 0; index < first.Stages[stage].Count; index++)
            {
                LevelRoom expected = first.Stages[stage][index];
                LevelRoom actual = second.Stages[stage][index];
                Assert.AreEqual(expected.Id, actual.Id);
                Assert.AreEqual(expected.Kind, actual.Kind);
                Assert.AreEqual(expected.Progress, actual.Progress);
                CollectionAssert.AreEqual(expected.Exits.ToArray(), actual.Exits.ToArray());
            }
        }
    }

    [TestMethod]
    public void Generate_ThousandSeeds_StaysWithinStageLimits()
    {
        for (int seed = 0; seed < 1000; seed++)
        {
            LevelLayout layout = LevelLayoutGenerator.Generate(seed, LevelLayoutSettings.Default);

            Assert.IsTrue(layout.CombatStageCount >= GameBalance.LevelCombatStagesMin, $"Seed {seed}: {layout.CombatStageCount}");
            Assert.IsTrue(layout.CombatStageCount <= GameBalance.LevelCombatStagesMax, $"Seed {seed}: {layout.CombatStageCount}");
        }
    }

    [TestMethod]
    public void Generate_HundredSeeds_ProducesForksAndStraightLevels()
    {
        int forked = 0;
        int straight = 0;

        for (int seed = 0; seed < SeedCount; seed++)
        {
            LevelLayout layout = LevelLayoutGenerator.Generate(seed, LevelLayoutSettings.Default);
            if (HasFork(layout))
            {
                forked++;
            }
            else
            {
                straight++;
            }
        }

        Assert.IsTrue(forked > 0, "No layout with a fork");
        Assert.IsTrue(straight > 0, "No layout without a fork");
    }

    [TestMethod]
    public void Generate_ParallelRooms_ShareProgress()
    {
        int forkedStages = 0;

        for (int seed = 0; seed < SeedCount; seed++)
        {
            LevelLayout layout = LevelLayoutGenerator.Generate(seed, LevelLayoutSettings.Default);
            for (int stage = 0; stage < layout.Stages.Count; stage++)
            {
                foreach (LevelRoom room in layout.Stages[stage])
                {
                    Assert.AreEqual(stage, room.Progress, $"Seed {seed}, room {room.Id}");
                }

                if (layout.Stages[stage].Count == 2)
                {
                    forkedStages++;
                    Assert.AreEqual(layout.Stages[stage - 1][0].Progress + 1, layout.Stages[stage][1].Progress);
                }
            }
        }

        Assert.IsTrue(forkedStages > 0, "No stage with parallel rooms");
    }

    private static List<LevelRoom> AllRooms(LevelLayout layout)
    {
        List<LevelRoom> rooms = [];
        foreach (IReadOnlyList<LevelRoom> stage in layout.Stages)
        {
            rooms.AddRange(stage);
        }

        return rooms;
    }

    private static HashSet<int> ReachableFrom(LevelLayout layout, int startId)
    {
        HashSet<int> visited = [startId];
        Queue<int> queue = new();
        queue.Enqueue(startId);
        while (queue.Count > 0)
        {
            foreach (int exit in layout.Room(queue.Dequeue()).Exits)
            {
                if (visited.Add(exit))
                {
                    queue.Enqueue(exit);
                }
            }
        }

        return visited;
    }

    private static bool HasFork(LevelLayout layout)
    {
        foreach (IReadOnlyList<LevelRoom> stage in layout.Stages)
        {
            if (stage.Count == 2)
            {
                return true;
            }
        }

        return false;
    }
}
