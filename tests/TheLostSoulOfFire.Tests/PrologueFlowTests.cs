using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class PrologueFlowTests
{
    [TestMethod]
    public void NewGameRunsPrologueThenAntechamberThenGateToArena()
    {
        GamePhase phase = GameFlowRules.ConfirmTitle(GamePhase.Title);
        Assert.AreEqual(GamePhase.Prologue, phase);

        phase = GameFlowRules.FinishPrologue(phase);
        Assert.AreEqual(GamePhase.Antechamber, phase);

        phase = GameFlowRules.EnterGate(phase);
        phase = GameFlowRules.FinishGateTransition(phase);
        Assert.AreEqual(GamePhase.Arena, phase);
    }

    [TestMethod]
    public void PrologueCanBeSkippedForAutomatedRuns()
    {
        Assert.AreEqual(GamePhase.Antechamber, GameFlowRules.ConfirmTitle(GamePhase.Title, skipPrologue: true));
    }

    [TestMethod]
    public void FinishPrologueLeavesOtherPhasesUntouched()
    {
        Assert.AreEqual(GamePhase.Title, GameFlowRules.FinishPrologue(GamePhase.Title));
        Assert.AreEqual(GamePhase.Arena, GameFlowRules.FinishPrologue(GamePhase.Arena));
    }

    [TestMethod]
    public void StartingTheDirectorEntersWakingInTheEmergenceSector()
    {
        PrologueDirector director = new();
        Assert.IsFalse(director.Started);

        director.Start();

        Assert.AreEqual(PrologueStage.Waking, director.Stage);
        Assert.AreEqual(PrologueSector.Emergence, director.Sector);
        Assert.IsTrue(director.Started);
        Assert.IsFalse(director.IsComplete);
    }

    [TestMethod]
    public void StagesMapToTheFourSectorsInOrder()
    {
        PrologueDirector director = new();
        PrologueSector previous = PrologueSector.Emergence;
        foreach (PrologueStage stage in System.Enum.GetValues<PrologueStage>())
        {
            director.Enter(stage);
            Assert.IsTrue(director.Sector >= previous, $"{stage} moved backwards to {director.Sector}");
            previous = director.Sector;
        }

        director.Enter(PrologueStage.LeaveSearch);
        Assert.AreEqual(PrologueSector.Search, director.Sector);
        director.Enter(PrologueStage.Transit);
        Assert.AreEqual(PrologueSector.Escape, director.Sector);
        Assert.IsTrue(director.IsVehicleRide);
        director.Enter(PrologueStage.Arrival);
        Assert.AreEqual(PrologueSector.Threshold, director.Sector);
    }

    [TestMethod]
    public void SpawnPointsLieInsideTheirMovementBounds()
    {
        PrologueDirector director = new();

        director.Enter(PrologueStage.Waking);
        Assert.IsTrue(director.MovementBounds.Contains(PrologueDirector.EmergenceSpawn.ToPoint()));

        director.Enter(PrologueStage.SearchApproach);
        Assert.IsTrue(director.MovementBounds.Contains(PrologueDirector.SearchSpawn.ToPoint()));

        director.Enter(PrologueStage.DevourerPressure);
        Assert.IsTrue(director.MovementBounds.Contains(PrologueDirector.EscapeSpawn.ToPoint()));

        director.Enter(PrologueStage.Transit);
        Assert.IsTrue(director.MovementBounds.Contains(PrologueDirector.VehicleSpawn.ToPoint()));

        director.Enter(PrologueStage.Arrival);
        Assert.IsTrue(director.MovementBounds.Contains(PrologueDirector.ThresholdSpawn.ToPoint()));
    }

    [TestMethod]
    public void RoutePointsAreReachableInsideTheWorld()
    {
        Rectangle world = PrologueDirector.WorldBounds;
        Assert.IsTrue(world.Contains(PrologueDirector.SoulTrace.ToPoint()));
        Assert.IsTrue(world.Contains(PrologueDirector.VehicleDock.ToPoint()));
        Assert.IsTrue(PrologueDirector.ExplorationBounds.Contains(PrologueDirector.SoulTrace.ToPoint()));
        Assert.IsTrue(PrologueDirector.ExplorationBounds.Contains(PrologueDirector.VehicleDock.ToPoint()));
        Assert.IsTrue(PrologueDirector.ExplorationBounds.Right >= 1550, "the sector exit trigger at x=1550 must be reachable");
    }

    [TestMethod]
    public void EveryStageHasAnObjectiveExceptTheSilentOnes()
    {
        PrologueDirector director = new();
        foreach (PrologueStage stage in System.Enum.GetValues<PrologueStage>())
        {
            director.Enter(stage);
            if (stage != PrologueStage.Dormant)
            {
                Assert.IsFalse(string.IsNullOrEmpty(director.Objective), $"{stage} has no objective");
            }
        }
    }

    [TestMethod]
    public void StageAndRunTimersAdvanceUntilCompletion()
    {
        PrologueDirector director = new();
        director.Start();
        director.Update(2f);
        Assert.AreEqual(2f, director.StateTime, 0.001f);
        Assert.AreEqual(2f, director.RunTime, 0.001f);

        director.Enter(PrologueStage.FindTrace);
        Assert.AreEqual(0f, director.StateTime);
        Assert.AreEqual(2f, director.RunTime, 0.001f);

        director.Enter(PrologueStage.Complete);
        director.Update(5f);
        Assert.AreEqual(2f, director.RunTime, 0.001f);
    }

    [TestMethod]
    public void SeededDevourerHoldsItsSoulUntilAFullCannonExpelsIt()
    {
        Devourer carrier = new(new Vector2(880f, 545f));
        Soul held = new(carrier.Position);

        carrier.SeedHeldSoul(held);

        Assert.AreEqual(1, carrier.ConsumedSoulCount);
        Assert.AreEqual(SoulState.Consumed, held.State);

        carrier.ApplyDamage(new DamageInfo(1, Vector2.Zero, carrier.Position, IsFullCannon: true));

        Assert.AreEqual(0, carrier.ConsumedSoulCount);
        Assert.AreEqual(SoulState.Exposed, held.State);
    }
}
