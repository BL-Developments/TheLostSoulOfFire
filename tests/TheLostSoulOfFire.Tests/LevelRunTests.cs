using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Game.Levels;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class LevelRunTests
{
    private const int SeedSearchCount = 200;

    [TestMethod]
    public void TryTakeExit_BeforeClear_Refuses()
    {
        LevelRun run = new(LevelLayoutGenerator.Generate(ForkSeed(), LevelLayoutSettings.Default));
        Assert.IsTrue(run.TryTakeExit(0, out _));
        Assert.IsTrue(run.HasEncounter);

        bool taken = run.TryTakeExit(0, out LevelRoom next);

        Assert.IsFalse(taken);
        Assert.AreEqual(run.Current.Id, next.Id);
        Assert.IsFalse(run.IsCleared);
    }

    [TestMethod]
    public void TryTakeExit_AfterClear_EntersConnectedRoom()
    {
        LevelLayout layout = LevelLayoutGenerator.Generate(ForkSeed(), LevelLayoutSettings.Default);
        LevelRun run = new(layout);
        Assert.IsTrue(run.TryTakeExit(0, out _));
        run.MarkCleared();
        int expectedId = layout.Stages[1][0].Exits[0];

        bool taken = run.TryTakeExit(0, out LevelRoom next);

        Assert.IsTrue(taken);
        Assert.AreEqual(expectedId, next.Id);
        Assert.AreEqual(expectedId, run.Current.Id);
    }

    [TestMethod]
    public void TryTakeExit_RightExitOfFork_EntersSecondRoom()
    {
        LevelLayout layout = LevelLayoutGenerator.Generate(ForkSeed(), LevelLayoutSettings.Default);
        LevelRun run = new(layout);

        bool taken = run.TryTakeExit(1, out LevelRoom next);

        Assert.IsTrue(taken);
        Assert.AreEqual(layout.Stages[1][1].Id, next.Id);
        Assert.AreEqual(1, next.Progress);
    }

    [TestMethod]
    public void CombatRoomsEntered_CountsOnlyCombatRooms()
    {
        LevelRun run = new(LevelLayoutGenerator.Generate(ForkSeed(), LevelLayoutSettings.Default));
        int expected = 0;

        while (run.Current.Kind != LevelRoomKind.LevelEnd)
        {
            run.MarkCleared();
            Assert.IsTrue(run.TryTakeExit(0, out LevelRoom next));
            if (next.Kind == LevelRoomKind.Combat)
            {
                expected++;
            }
        }

        Assert.IsTrue(expected > 0);
        Assert.AreEqual(expected, run.CombatRoomsEntered);
    }

    [TestMethod]
    public void CombatRoomsEntered_ForkChoice_SameProgress()
    {
        LevelLayout layout = LevelLayoutGenerator.Generate(ForkSeed(), LevelLayoutSettings.Default);
        LevelRun left = new(layout);
        LevelRun right = new(layout);

        Assert.IsTrue(left.TryTakeExit(0, out _));
        Assert.IsTrue(right.TryTakeExit(1, out _));

        Assert.AreEqual(1, left.CombatRoomsEntered);
        Assert.AreEqual(right.CombatRoomsEntered, left.CombatRoomsEntered);
    }

    [TestMethod]
    public void StartAndLevelEnd_AreClearedOnEntry()
    {
        LevelRun run = new(LevelLayoutGenerator.Generate(ForkSeed(), LevelLayoutSettings.Default));
        Assert.IsTrue(run.IsCleared);

        while (run.Current.Kind != LevelRoomKind.LevelEnd)
        {
            run.MarkCleared();
            Assert.IsTrue(run.TryTakeExit(0, out _));
        }

        Assert.IsTrue(run.IsCleared);
        Assert.IsFalse(run.HasEncounter);
    }

    [TestMethod]
    public void CombatRoomsEntered_CarriesIntoNextLevel()
    {
        LevelRun run = new(LevelLayoutGenerator.Generate(ForkSeed(), LevelLayoutSettings.Default), combatRoomsEntered: 5);

        Assert.IsTrue(run.TryTakeExit(0, out LevelRoom next));

        Assert.AreEqual(LevelRoomKind.Combat, next.Kind);
        Assert.AreEqual(1, next.Progress);
        Assert.AreEqual(6, run.CombatRoomsEntered);
    }

    [TestMethod]
    public void GuardianRoom_HasEncounterAndStaysClosedUntilCleared()
    {
        LevelLayout layout = LevelLayoutGenerator.Generate(ForkSeed(), LevelLayoutSettings.Default);
        LevelRun run = new(layout, guardianAtEnd: true);

        while (run.Current.Kind != LevelRoomKind.LevelEnd)
        {
            run.MarkCleared();
            Assert.IsTrue(run.TryTakeExit(0, out _));
        }

        Assert.IsTrue(run.IsGuardianRoom);
        Assert.IsTrue(run.HasEncounter);
        Assert.IsFalse(run.IsCleared);
    }

    [TestMethod]
    public void Position_TwoExits_LeftAndRightOfCentre()
    {
        Rectangle bounds = new(0, 0, 1000, 500);

        Vector2 left = RoomExit.Position(bounds, 2, 0);
        Vector2 right = RoomExit.Position(bounds, 2, 1);
        Vector2 single = RoomExit.Position(bounds, 1, 0);

        Assert.IsTrue(left.X < bounds.Center.X);
        Assert.IsTrue(right.X > bounds.Center.X);
        Assert.AreEqual(bounds.Center.X, single.X);
        Assert.AreEqual(left.Y, right.Y);
    }

    [TestMethod]
    public void InReach_OutsideRadius_ReturnsNull()
    {
        Rectangle bounds = new(0, 0, 1000, 500);
        Vector2 exit = RoomExit.Position(bounds, 1, 0);
        Vector2 far = exit + new Vector2(GameBalance.RoomExitInteractRadius + 1f, 0f);

        Assert.IsNull(RoomExit.InReach(bounds, 1, far));
        Assert.AreEqual(0, RoomExit.InReach(bounds, 1, exit));
    }

    [TestMethod]
    public void ReturnToHubAfterDefeat_FromArena_GoesToAntechamber()
    {
        Assert.AreEqual(GamePhase.Antechamber, GameFlowRules.ReturnToHubAfterDefeat(GamePhase.Arena));
        Assert.AreEqual(GamePhase.Prologue, GameFlowRules.ReturnToHubAfterDefeat(GamePhase.Prologue));
    }

    /// <summary>The first seed whose layout has a fork, so the tests do not depend on a hard-coded seed.</summary>
    private static int ForkSeed()
    {
        for (int seed = 0; seed < SeedSearchCount; seed++)
        {
            LevelLayout layout = LevelLayoutGenerator.Generate(seed, LevelLayoutSettings.Default);
            if (layout.Stages[1].Count == 2)
            {
                return seed;
            }
        }

        throw new InvalidOperationException("No fork seed found in the search range.");
    }
}
