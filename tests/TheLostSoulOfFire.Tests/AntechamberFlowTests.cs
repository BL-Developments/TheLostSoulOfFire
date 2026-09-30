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
    public void MainFlowRequiresAntechamberAndDoorBeforeArena()
    {
        GamePhase phase = GamePhase.Title;

        phase = GameFlowRules.ConfirmTitle(phase, skipPrologue: true);
        Assert.AreEqual(GamePhase.Antechamber, phase);

        phase = GameFlowRules.EnterDoor(phase);
        Assert.AreEqual(GamePhase.EnteringArena, phase);

        phase = GameFlowRules.FinishDoorTransition(phase);
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
        Assert.IsTrue(GameFlowRules.AllowsCombat(GamePhase.Prologue));
        Assert.IsFalse(GameFlowRules.AllowsCombat(GamePhase.Antechamber));
        Assert.IsFalse(GameFlowRules.AllowsCombat(GamePhase.EnteringArena));
        Assert.IsTrue(GameFlowRules.AllowsCombat(GamePhase.Arena));
    }

    [TestMethod]
    public void HubHasSixBiomeDoorsAndAMiddleFinalDoor()
    {
        SoulFurnaceAntechamber room = new();

        Assert.AreEqual(7, room.Doors.Count);
        CollectionAssert.AreEqual(
            new[] { "I", "II", "III", "", "IV", "V", "VI" },
            room.Doors.Select(door => door.Numeral).ToArray());

        HubDoor final = room.Doors[3];
        Assert.AreEqual(HubDoorKind.Final, final.Kind);
        Assert.IsTrue(room.Doors.Where(door => door != final).All(door => door.Bounds.Width < final.Bounds.Width));
        Assert.IsTrue(room.Doors.Where(door => door != final).All(door => door.Bounds.Height < final.Bounds.Height));
        Assert.IsTrue(room.Doors.All(door => room.Bounds.Contains(door.Bounds)));
    }

    [TestMethod]
    public void OnlyDoorOneIsOpen()
    {
        SoulFurnaceAntechamber room = new();

        Assert.IsFalse(room.EntryDoor.IsSealed);
        Assert.AreEqual(1, room.EntryDoor.BiomeNumber);
        Assert.AreEqual(6, room.Doors.Count(door => door.IsSealed));
        Assert.IsTrue(room.Doors[3].IsSealed);
    }

    [TestMethod]
    public void DoorZonesAreReachableAndDoNotOverlap()
    {
        SoulFurnaceAntechamber room = new();

        Assert.IsTrue(room.MovementBounds.Contains(room.PlayerSpawn.ToPoint()));
        Assert.IsNull(room.DoorAt(room.PlayerSpawn));
        foreach (HubDoor door in room.Doors)
        {
            Vector2 inside = door.InteractionZone.Center.ToVector2();
            Assert.IsTrue(room.MovementBounds.Contains(inside.ToPoint()), "zone center must be walkable");
            Assert.AreSame(door, room.DoorAt(inside));
        }

        for (int i = 0; i < room.Doors.Count; i++)
        {
            for (int j = i + 1; j < room.Doors.Count; j++)
            {
                Assert.IsFalse(room.Doors[i].InteractionZone.Intersects(room.Doors[j].InteractionZone));
            }
        }
    }

    [TestMethod]
    public void DoorPromptsNameTheDoorOrTheSeal()
    {
        SoulFurnaceAntechamber room = new();

        Assert.AreEqual("E  ENTER BIOME I", room.Doors[0].Prompt);
        Assert.AreEqual("SEALED · DEFEAT THE PREVIOUS GUARDIAN", room.Doors[1].Prompt);
        Assert.AreEqual("SEALED · DEFEAT ALL GUARDIANS", room.Doors[3].Prompt);
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
