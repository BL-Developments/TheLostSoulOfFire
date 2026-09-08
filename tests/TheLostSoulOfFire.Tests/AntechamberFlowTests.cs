using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class AntechamberFlowTests
{
    [TestMethod]
    public void MainFlowRequiresAntechamberAndGateBeforeArena()
    {
        GamePhase phase = GamePhase.Title;

        phase = GameFlowRules.ConfirmTitle(phase);
        Assert.AreEqual(GamePhase.Antechamber, phase);

        phase = GameFlowRules.EnterGate(phase);
        Assert.AreEqual(GamePhase.EnteringArena, phase);

        phase = GameFlowRules.FinishGateTransition(phase);
        Assert.AreEqual(GamePhase.Arena, phase);
    }

    [TestMethod]
    public void RetryAndCompletionUseDifferentResetScopes()
    {
        Assert.AreEqual(GamePhase.Arena, GameFlowRules.RetryAfterDeath());
        Assert.AreEqual(GamePhase.Title, GameFlowRules.RestartAfterCompletion());
    }

    [TestMethod]
    public void CombatIsOnlyAvailableInsideArena()
    {
        Assert.IsFalse(GameFlowRules.AllowsCombat(GamePhase.Title));
        Assert.IsFalse(GameFlowRules.AllowsCombat(GamePhase.Antechamber));
        Assert.IsFalse(GameFlowRules.AllowsCombat(GamePhase.EnteringArena));
        Assert.IsTrue(GameFlowRules.AllowsCombat(GamePhase.Arena));
    }

    [TestMethod]
    public void RoomHasReadableSpawnToGateRouteAndBoundedInteraction()
    {
        SoulFurnaceAntechamber room = new();

        Assert.IsTrue(room.MovementBounds.Contains(room.PlayerSpawn.ToPoint()));
        Assert.IsTrue(room.Bounds.Contains(room.Gate));
        Assert.IsTrue(room.InteractionZone.Intersects(room.Gate));
        Assert.IsTrue(room.GateCenter.X > room.PlayerSpawn.X);
        Assert.IsFalse(room.IsPlayerAtGate(room.PlayerSpawn));
        Assert.IsTrue(room.IsPlayerAtGate(room.InteractionZone.Center.ToVector2()));
    }

    [TestMethod]
    public void PreLevelInputKeepsExplorationActionsAndSuppressesWeapons()
    {
        SoulFurnaceAntechamber room = new();
        Player player = new(room.PlayerSpawn);
        InputState input = new();
        ParticleSystem particles = new();
        ScreenEffects screenEffects = new();
        input.InjectKeyDown(Keys.D);
        input.InjectKeyDown(Keys.Q);
        input.InjectKeyPress(Keys.Space);
        input.InjectMousePresses(left: true, right: true);

        float startX = player.Position.X;
        player.Update(
            1f / 60f,
            input,
            player.Position + Vector2.UnitX * 200f,
            room.MovementBounds,
            particles,
            screenEffects,
            combatEnabled: false);

        Assert.IsTrue(player.Position.X > startX);
        Assert.IsTrue(player.IsDashing);
        Assert.IsTrue(player.SoulSenseActive);
        Assert.AreEqual(0, player.Scythe.ActiveStep);
        Assert.AreEqual(SoulCannonState.Stored, player.Cannon.State);
    }
}
