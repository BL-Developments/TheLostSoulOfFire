using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Audio;
using TheLostSoulOfFire.Game.Levels;
using TheLostSoulOfFire.Rendering;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Game;

/// <summary>
/// Level (change <c>add-level-rooms</c>): a run through rooms that are each built like the arena.
/// A level runs in <see cref="GamePhase.Arena"/> while a <see cref="LevelRun"/> is active, so the
/// arena's own loop, camera, currencies and abilities apply unchanged; the few places that differ
/// check <see cref="InLevel"/>.
/// </summary>
public sealed partial class GameWorld
{
    private LevelRun? _levelRun;
    private float _roomTransitionElapsed;
    private RoomEncounterPlan? _roomPlan;
    private int _roomWaveIndex;
    private float _roomWavePause;
    private readonly List<ArenaEnemyKind> _roomSpawnOrder = [];

    internal bool InLevel => _levelRun is not null;

    internal int PlayerHealth => _player.Health;

    internal int LevelRoomProgress => _levelRun?.Current.Progress ?? 0;

    internal LevelRoomKind? CurrentLevelRoomKind => _levelRun?.Current.Kind;

    /// <summary>The waves of the current combat room, or null in rooms without an encounter.</summary>
    internal RoomEncounterPlan? LevelRoomEncounter => _roomPlan;

    /// <summary>Waves of the current room that have started; the pause before the next one does not count.</summary>
    internal int LevelRoomWavesStarted =>
        _roomPlan is null || _loopState is ArenaLoopState.Intro or ArenaLoopState.Transition
            ? 0
            : _roomWavePause > 0f ? _roomWaveIndex : _roomWaveIndex + 1;

    internal bool LevelExitsOpen => _levelRun?.IsCleared ?? false;

    /// <summary>Automated runs: stands the player at an exit of the current room (clamped to the exits it has).</summary>
    internal void PlaceAutomatedPlayerAtLevelExit(int exitIndex)
    {
        if (_levelRun is not { } run)
        {
            return;
        }

        int count = ExitCount(run.Current);
        _player.PlaceAt(RoomExit.Position(_arena.CombatBounds, count, Math.Min(exitIndex, count - 1)));
    }

    private Vector2 SouthGate => new(_arena.CombatBounds.Center.X, _arena.CombatBounds.Bottom - 90f);

    private static int ExitCount(LevelRoom room) => room.Kind == LevelRoomKind.LevelEnd ? 1 : room.Exits.Count;

    private void StartLevel(int? seed, Viewport viewport)
    {
        ClearRunState();
        int levelSeed = seed ?? Environment.TickCount;
        Console.WriteLine($"LEVEL_SEED {levelSeed}");
        BeginLevel(new LevelRun(LevelLayoutGenerator.Generate(levelSeed, LevelLayoutSettings.Default)), viewport);
    }

    /// <summary>Starts <paramref name="run"/> at its start room with a fresh currency run; the arena intro plays first.</summary>
    private void BeginLevel(LevelRun run, Viewport viewport)
    {
        _levelRun = run;
        PrepareRoomEncounter();
        _phase = GamePhase.Arena;
        BeginArenaIntro(viewport);
        _player.PlaceAt(SouthGate);
    }

    /// <summary>Removes the fight's leftovers when the player leaves a room; the player, abilities and balances stay.</summary>
    private void ClearRoomState()
    {
        _enemies.Clear();
        _souls.Clear();
        _cannonShots.Clear();
        _pendingSpawns.Clear();
        _particles.Clear();
        _spriteVfx.Clear();
        _groundImpacts.Clear();
        _waveRun = ArenaWaveRun.Empty;
        _burningHandoffTimer = 0f;
        _burningCommittedLastFrame = 0;
    }

    private void EnterCurrentLevelRoom()
    {
        ClearRoomState();
        _player.PlaceAt(SouthGate);
        _roomTransitionElapsed = 0f;
        PrepareRoomEncounter();
        _loopState = ArenaLoopState.Intro;
        _presentation.BeginIntro(true);
    }

    /// <summary>Builds the waves of a combat room when the player enters it; other rooms have none.</summary>
    private void PrepareRoomEncounter()
    {
        _roomWaveIndex = 0;
        _roomWavePause = 0f;
        ClearTravelPoint();
        if (_levelRun is not { } run)
        {
            _roomPlan = null;
            return;
        }

        _roomPlan = run.HasEncounter ? CreateRoomEncounter(run) : null;
        if (run.Current.Kind == LevelRoomKind.LevelEnd && !run.IsGuardianRoom)
        {
            PlaceLevelEndTravelPoint(run);
        }
    }

    /// <summary>The guardian room of a biome's last level is a harder combat room with extra waves (change <c>add-biome-run-flow</c>).</summary>
    private static RoomEncounterPlan CreateRoomEncounter(LevelRun run) =>
        run.IsGuardianRoom
            ? RoomEncounterPlan.For(run.CombatRoomsEntered + GameBalance.GuardianProgressBonus, run.RoomSeed, GameBalance.GuardianExtraWaves)
            : RoomEncounterPlan.For(run.CombatRoomsEntered, run.RoomSeed);

    /// <summary>The level end of levels 1 and 2 in a biome is a travel point where the exit used to stand.</summary>
    private void PlaceLevelEndTravelPoint(LevelRun run)
    {
        _travelPoint = new TravelPoint(RoomExit.Position(_arena.CombatBounds, 1, 0));
        _biomeRun?.ReachLevelEnd(run.CombatRoomsEntered);
    }

    private void AfterLevelRoomIntro()
    {
        if (_levelRun is not { } run)
        {
            return;
        }

        if (run.HasEncounter)
        {
            SpawnRoomWave(run.Current.Progress);
        }
        else
        {
            _loopState = ArenaLoopState.Intermission;
        }
    }

    /// <summary>Starts the current wave of the room: its single push appears at once, as a wave of the arena does.</summary>
    private void SpawnRoomWave(int progress)
    {
        if (_roomPlan is not { } plan)
        {
            return;
        }

        Vector2 center = _arena.CombatBounds.Center.ToVector2();
        _waveRun = new ArenaWaveRun([plan.Waves[_roomWaveIndex]]);
        _pendingSpawns.Clear();
        _reinforcementSeed = 0;
        ArenaPush push = _waveRun.TakeFirst();
        List<Vector2> positions = ArenaWaves.ChooseSpawnPositions(_arena.CombatBounds, _player.Position, push.Total);
        int roomSeed = _levelRun?.RoomSeed ?? 0;
        RoomEncounterPlan.FillSpawnOrder(push, unchecked(roomSeed * 31 + _roomWaveIndex), _roomSpawnOrder);
        int seed = progress * 10;
        for (int index = 0; index < _roomSpawnOrder.Count; index++)
        {
            _enemies.Add(CreateArenaEnemy(_roomSpawnOrder[index], positions[index], ref seed));
        }

        _loopState = ArenaLoopState.Combat;
        _burningHandoffTimer = 0f;
        _burningCommittedLastFrame = 0;
        _particles.EmitDeathFlame(center, 18 + progress * 5, 1f + progress * 0.12f);
        _screenEffects.AddShake(0.16f, 4f + progress);
        _screenEffects.Flash(0.08f, 0.12f + progress * 0.035f);
        _audio.SetCalm(false);
        _audio.Play(AudioCue.WaveStart, 0.62f);
    }

    /// <summary>After a wave of a combat room: the next wave comes after a short pause, the last one clears the room.</summary>
    private bool HasNextRoomWave => _roomPlan is { } plan && _roomWaveIndex + 1 < plan.Waves.Count;

    private void BeginNextRoomWavePause()
    {
        _roomWaveIndex++;
        _roomWavePause = GameBalance.RoomWavePause;
        _audio.Play(AudioCue.WaveClear, 0.45f);
    }

    private void UpdateRoomWavePause(float deltaTime)
    {
        _roomWavePause -= deltaTime;
        if (_roomWavePause > 0f)
        {
            return;
        }

        _roomWavePause = 0f;
        if (_levelRun is { } run)
        {
            SpawnRoomWave(run.Current.Progress);
        }
    }

    private void ClearLevelRoomEncounter()
    {
        bool guardian = _levelRun is { IsGuardianRoom: true };
        _levelRun?.MarkCleared();
        _audio.Play(AudioCue.WaveClear, 0.62f);
        _loopState = ArenaLoopState.Intermission;
        _particles.EmitDeathFlame(_arena.CombatBounds.Center.ToVector2(), 12, 0.8f);
        if (guardian)
        {
            CompleteBiomeRun();
        }
    }

    /// <summary>The exit the player stands at, while the room is open for leaving.</summary>
    private int? LevelExitInReach()
    {
        // A level end has no exit: its travel point or the guardian takes over.
        if (_levelRun is not { } run || _loopState != ArenaLoopState.Intermission || run.Current.Kind == LevelRoomKind.LevelEnd)
        {
            return null;
        }

        return RoomExit.InReach(_arena.CombatBounds, ExitCount(run.Current), _player.Position);
    }

    private void TakeLevelExit(int exitIndex, Viewport viewport)
    {
        if (_levelRun is not { } run)
        {
            return;
        }

        if (!run.TryTakeExit(exitIndex, out _))
        {
            return;
        }

        _roomTransitionElapsed = 0f;
        _loopState = ArenaLoopState.Transition;
        _presentation.BeginWaveTransition();
        _audio.Play(AudioCue.UiOpen, 0.45f);
    }

    /// <summary>A defeat in a level ends the run; the balances were already lost at the moment of death.</summary>
    private void ReturnLevelToHubAfterDefeat(Viewport viewport)
    {
        _biomeRun?.Defeat();
        _phase = GameFlowRules.ReturnToHubAfterDefeat(_phase);
        BeginAntechamber(viewport);
    }

    /// <summary>Leaves the level for the hub with the balances that the travel choice secured.</summary>
    private void ReturnLevelToHub(Viewport viewport, (int Geld, int Glut) secured)
    {
        _lastSecured = secured;
        _phase = GameFlowRules.ExtractToHub(_phase);
        BeginAntechamber(viewport);
        _extractedAt = _presentationTime;
    }

    private BiomeDefinition CurrentBiome => _biomeRun?.Biome ?? BiomeCatalog.One;

    /// <summary>
    /// Wall and floor of a level room (change <c>add-level-visual-slots</c>). Each slot shows its painted
    /// plate once the registry has one and the biome's grey box until then, so slots swap one by one.
    /// </summary>
    private void DrawLevelEnvironment(SpriteBatch batch, Texture2D pixel)
    {
        BiomeDefinition biome = CurrentBiome;
        if (_art.HasArt(biome.WallId))
        {
            _art.DrawEnvironment(batch, biome.WallId, Arena.WallFoot);
        }
        else
        {
            LevelGreyboxRenderer.DrawWall(batch, pixel, _arena.Bounds, _arena.CombatBounds, biome.Palette);
        }

        if (_art.HasArt(biome.RoomId))
        {
            _art.DrawEnvironment(batch, biome.RoomId, Arena.FloorTopLeft);
        }
        else
        {
            LevelGreyboxRenderer.DrawRoom(batch, pixel, _arena.CombatBounds, biome.Palette);
        }
    }

    private void DrawRoomExits(SpriteBatch batch, Texture2D pixel)
    {
        if (_levelRun is not { } run || run.Current.Kind == LevelRoomKind.LevelEnd)
        {
            return;
        }

        BiomeDefinition biome = CurrentBiome;
        int count = ExitCount(run.Current);
        float pulse = 0.5f + 0.5f * MathF.Sin(_presentationTime * 3f);
        for (int index = 0; index < count; index++)
        {
            Vector2 at = RoomExit.Position(_arena.CombatBounds, count, index);
            string clip = run.IsCleared ? LevelExitClips.Open : LevelExitClips.Closed;
            if (_art.DrawPropFrame(batch, biome.ExitId, clip, at, 0f, Color.White))
            {
                continue;
            }

            if (run.IsCleared)
            {
                _art.DrawSoftSpot(batch, at, new Vector2(54f + pulse * 6f), biome.Palette.ExitLight * (0.2f + pulse * 0.1f));
            }

            LevelGreyboxRenderer.DrawExit(batch, pixel, at, run.IsCleared, pulse, biome.Palette);
        }
    }

    private void DrawLevelRoomHud(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        if (_levelRun is not { } run)
        {
            return;
        }

        int waveCount = _roomPlan?.Waves.Count ?? 0;
        // A biome run counts the progress of the whole run, not of the current level's stages.
        int progress = _biomeRoomLabel is null ? run.Current.Progress : run.CombatRoomsEntered;
        HudRenderer.DrawRoom(batch, pixel, viewport, progress, waveCount, LevelRoomWavesStarted, _biomeRoomLabel);
        // The guardian room is a level end too, but the level is only done once the biome is.
        if (run.Current.Kind == LevelRoomKind.LevelEnd && !run.IsGuardianRoom)
        {
            PixelText.DrawCentered(batch, pixel, "LEVEL GESCHAFFT", viewport.Width * 0.5f, 120f, 4, GameBalance.SoulWhite);
        }
    }
}
