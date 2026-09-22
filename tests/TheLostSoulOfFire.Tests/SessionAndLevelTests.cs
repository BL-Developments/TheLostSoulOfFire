using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Gameplay;
using TheLostSoulOfFire.Levels;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class SessionAndLevelTests
{
    [TestMethod]
    public void StartAndEndRun_UsesFreshRunState()
    {
        GameSession session = new();
        RunState first = session.StartRun();
        session.EndRun();
        Assert.IsNull(session.CurrentRun);

        RunState second = session.StartRun();
        Assert.AreNotSame(first, second);
        Assert.AreEqual(first.Sequence + 1, second.Sequence);
    }

    [TestMethod]
    public void LevelState_CompletesAfterAllRequiredEnemies()
    {
        LevelState state = new("TestRaid", 2);
        state.EnemyDefeated();
        Assert.IsFalse(state.IsComplete);
        state.EnemyDefeated();
        Assert.IsTrue(state.IsComplete);
        state.EnemyDefeated();
        Assert.AreEqual(0, state.RemainingRequiredEnemies);
    }

    [TestMethod]
    public void LevelFactory_RecreatesDefinitionDeterministically()
    {
        LevelDefinition definition = CreateDefinition();
        LevelRuntime first = LevelFactory.Create(definition);
        LevelRuntime second = LevelFactory.Create(definition);

        Assert.AreEqual(first.Bounds, second.Bounds);
        Assert.AreEqual(first.PlayerSpawn, second.PlayerSpawn);
        CollectionAssert.AreEqual(first.Walls, second.Walls);
        CollectionAssert.AreEqual(first.EnemySpawns, second.EnemySpawns);
        Assert.AreNotSame(first.Walls, second.Walls);
    }

    private static LevelDefinition CreateDefinition() => new()
    {
        Name = "TestRaid",
        Bounds = new RectangleData { X = 0, Y = 0, Width = 800, Height = 600 },
        PlayerSpawn = new PointData { X = 100, Y = 200 },
        Portal = new RectangleData(),
        Altar = new RectangleData { X = 700, Y = 250, Width = 50, Height = 100 },
        Walls = { new RectangleData { X = 300, Y = 100, Width = 50, Height = 300 } },
        EnemySpawns = { new PointData { X = 500, Y = 300 } }
    };
}
