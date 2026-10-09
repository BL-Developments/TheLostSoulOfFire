using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Game.Levels;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class BiomeRunTests
{
    private const int RunSeed = 4711;

    [TestMethod]
    public void Start_FromHomebase_BeginsAtLevelOneInLevel()
    {
        BiomeRun run = new();

        bool started = run.Start(BiomeCatalog.One, RunSeed);

        Assert.IsTrue(started);
        Assert.AreEqual(BiomeRunState.InLevel, run.State);
        Assert.AreEqual(1, run.Level);
        Assert.AreEqual(0, run.CombatRoomsEntered);
    }

    [TestMethod]
    public void Start_WhileInLevel_RefusesWithoutChange()
    {
        BiomeRun run = new();
        run.Start(BiomeCatalog.One, RunSeed);

        bool started = run.Start(BiomeCatalog.One, 1234, level: 3);

        Assert.IsFalse(started);
        Assert.AreEqual(1, run.Level);
        Assert.AreEqual(RunSeed, run.RunSeed);
    }

    [TestMethod]
    public void Start_LevelOutsideBiome_Throws()
    {
        BiomeRun run = new();

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => run.Start(BiomeCatalog.One, RunSeed, level: BiomeCatalog.One.LevelCount + 1));
    }

    [TestMethod]
    public void ReachLevelEnd_InHomebase_IsIgnored()
    {
        BiomeRun run = new();

        bool reached = run.ReachLevelEnd(3);

        Assert.IsFalse(reached);
        Assert.AreEqual(BiomeRunState.Homebase, run.State);
        Assert.AreEqual(0, run.CombatRoomsEntered);
    }

    [TestMethod]
    public void ReachLevelEnd_InFirstLevel_KeepsRoomProgress()
    {
        BiomeRun run = new();
        run.Start(BiomeCatalog.One, RunSeed);

        bool reached = run.ReachLevelEnd(5);

        Assert.IsTrue(reached);
        Assert.AreEqual(BiomeRunState.LevelEnd, run.State);
        Assert.AreEqual(5, run.CombatRoomsEntered);
    }

    [TestMethod]
    public void ReachLevelEnd_InLastLevel_IsRefused()
    {
        BiomeRun run = new();
        run.Start(BiomeCatalog.One, RunSeed, level: BiomeCatalog.One.LevelCount);

        bool reached = run.ReachLevelEnd(9);

        Assert.IsFalse(reached);
        Assert.AreEqual(BiomeRunState.InLevel, run.State);
    }

    [TestMethod]
    public void TravelOn_FromLevelEnd_EntersNextLevel()
    {
        BiomeRun run = new();
        run.Start(BiomeCatalog.One, RunSeed);
        run.ReachLevelEnd(5);

        bool traveled = run.TravelOn();

        Assert.IsTrue(traveled);
        Assert.AreEqual(2, run.Level);
        Assert.AreEqual(BiomeRunState.InLevel, run.State);
    }

    [TestMethod]
    public void TravelOn_FromLastLevel_IsRefused()
    {
        BiomeRun run = new();
        run.Start(BiomeCatalog.One, RunSeed, level: BiomeCatalog.One.LevelCount);

        bool traveled = run.TravelOn();

        Assert.IsFalse(traveled);
        Assert.AreEqual(BiomeCatalog.One.LevelCount, run.Level);
    }

    [TestMethod]
    public void TravelOn_InLevel_IsRefused()
    {
        BiomeRun run = new();
        run.Start(BiomeCatalog.One, RunSeed);

        bool traveled = run.TravelOn();

        Assert.IsFalse(traveled);
        Assert.AreEqual(1, run.Level);
    }

    [TestMethod]
    public void Extract_FromLevelEnd_EndsRun()
    {
        BiomeRun run = new();
        run.Start(BiomeCatalog.One, RunSeed);
        run.ReachLevelEnd(4);

        bool extracted = run.Extract();

        Assert.IsTrue(extracted);
        Assert.AreEqual(BiomeRunState.Extracted, run.State);
    }

    [TestMethod]
    public void Extract_InLevel_IsRefused()
    {
        BiomeRun run = new();
        run.Start(BiomeCatalog.One, RunSeed);

        bool extracted = run.Extract();

        Assert.IsFalse(extracted);
        Assert.AreEqual(BiomeRunState.InLevel, run.State);
    }

    [TestMethod]
    public void Defeat_InLevel_EndsRun()
    {
        BiomeRun run = new();
        run.Start(BiomeCatalog.One, RunSeed);

        bool defeated = run.Defeat();

        Assert.IsTrue(defeated);
        Assert.AreEqual(BiomeRunState.Defeated, run.State);
    }

    [TestMethod]
    public void Defeat_AtLevelEnd_IsRefused()
    {
        BiomeRun run = new();
        run.Start(BiomeCatalog.One, RunSeed);
        run.ReachLevelEnd(4);

        bool defeated = run.Defeat();

        Assert.IsFalse(defeated);
        Assert.AreEqual(BiomeRunState.LevelEnd, run.State);
    }

    [TestMethod]
    public void CompleteBiome_InLastLevel_CompletesBiome()
    {
        BiomeRun run = new();
        run.Start(BiomeCatalog.One, RunSeed, level: BiomeCatalog.One.LevelCount);

        bool completed = run.CompleteBiome();

        Assert.IsTrue(completed);
        Assert.AreEqual(BiomeRunState.BiomeComplete, run.State);
    }

    [TestMethod]
    public void CompleteBiome_BeforeLastLevel_IsRefused()
    {
        BiomeRun run = new();
        run.Start(BiomeCatalog.One, RunSeed);

        bool completed = run.CompleteBiome();

        Assert.IsFalse(completed);
        Assert.AreEqual(BiomeRunState.InLevel, run.State);
    }

    [TestMethod]
    public void Defeat_InLevelThree_ThenStart_BeginsAtLevelOne()
    {
        BiomeRun run = new();
        run.Start(BiomeCatalog.One, RunSeed, level: BiomeCatalog.One.LevelCount);
        run.Defeat();

        bool started = run.Start(BiomeCatalog.One, 999);

        Assert.IsTrue(started);
        Assert.AreEqual(1, run.Level);
        Assert.AreEqual(0, run.CombatRoomsEntered);
        Assert.AreEqual(999, run.RunSeed);
    }

    [TestMethod]
    public void LevelSeed_SameRunSeed_SameSequence()
    {
        BiomeRun first = new();
        BiomeRun second = new();
        first.Start(BiomeCatalog.One, RunSeed);
        second.Start(BiomeCatalog.One, RunSeed);

        List<int> firstSeeds = [first.LevelSeed];
        List<int> secondSeeds = [second.LevelSeed];
        for (int level = 2; level <= BiomeCatalog.One.LevelCount; level++)
        {
            first.ReachLevelEnd(0);
            first.TravelOn();
            second.ReachLevelEnd(0);
            second.TravelOn();
            firstSeeds.Add(first.LevelSeed);
            secondSeeds.Add(second.LevelSeed);
        }

        CollectionAssert.AreEqual(firstSeeds, secondSeeds);
        Assert.AreEqual(firstSeeds.Count, firstSeeds.Distinct().Count());
    }

    [TestMethod]
    public void TryGet_ExistingBiomeOne_ReturnsBiome()
    {
        bool found = BiomeCatalog.TryGet(1, out BiomeDefinition biome);

        Assert.IsTrue(found);
        Assert.AreEqual("I", biome.Numeral);
        Assert.AreEqual(3, biome.LevelCount);
    }

    [TestMethod]
    public void TryGet_UnknownBiome_IsRefused()
    {
        bool found = BiomeCatalog.TryGet(2, out _);

        Assert.IsFalse(found);
    }
}
