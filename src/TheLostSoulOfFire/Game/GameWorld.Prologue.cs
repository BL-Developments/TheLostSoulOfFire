using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Audio;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Game;

/// <summary>
/// The solo Death-Layer prologue that runs between the main menu and the Soul
/// Furnace antechamber. It drives only the authored story spine and composes the
/// existing enemies and Soul lifecycle; it is intentionally not a quest system.
/// </summary>
public sealed partial class GameWorld
{
    private void BeginPrologue(Viewport viewport)
    {
        ClearRunState();
        _phase = GameFlowRules.ConfirmTitle(_phase);
        _phaseTime = 0f;
        _prologueEncounterArmed = false;
        _prologueReleaseObserved = false;
        _player.Reset(PrologueDirector.EmergenceSpawn);
        _lastMouseWorld = _player.Position + Vector2.UnitX * 200f;
        _camera.Zoom = 1f;
        _camera.Follow(_player.Position, PrologueDirector.WorldBounds, viewport, 1f);
        _prologue.Start();
        _loopState = ArenaLoopState.Intro;
        _presentation.BeginIntro(false);
        _audio.SetCalm(true);
        _audio.SetSoulSense(false);
        _audio.SetArenaActive(false, true);
        _particles.EmitDeathFlame(PrologueDirector.EmergenceSpawn, 18, 1.15f);
    }

    /// <summary>
    /// Sector retry is authored rather than inferred from story flags: the
    /// player restarts at the start of the sector they failed in.
    /// </summary>
    private void RestartPrologueSector(Viewport viewport)
    {
        switch (_prologue.Sector)
        {
            case PrologueSector.Emergence:
                BeginPrologue(viewport);
                return;

            case PrologueSector.Search:
                EnterSearchSector();
                break;

            case PrologueSector.Escape:
                EnterEscapeSector();
                break;

            case PrologueSector.Threshold:
                EnterThreshold();
                break;
        }

        _camera.Follow(_player.Position, PrologueDirector.WorldBounds, viewport, 1f);
    }

    /// <summary>Developer review shortcuts for the authored checkpoints.</summary>
    private void DebugEnterPrologueStage(PrologueStage stage)
    {
        ClearPrologueActivity();
        switch (stage)
        {
            case PrologueStage.FindTrace:
                _player.Reset(PrologueDirector.EmergenceSpawn);
                _prologue.Enter(stage);
                _loopState = ArenaLoopState.Combat;
                _audio.SetCalm(true);
                break;

            case PrologueStage.SearchApproach:
                EnterSearchSector();
                break;

            case PrologueStage.DevourerPressure:
                EnterEscapeSector();
                break;

            case PrologueStage.Transit:
                BeginTransit();
                break;
        }
    }

    private void UpdatePrologueFlow(float deltaTime)
    {
        _prologue.Update(deltaTime);
        switch (_prologue.Stage)
        {
            case PrologueStage.Waking:
                if (_prologue.StateTime >= 3.6f)
                {
                    _prologue.Enter(PrologueStage.FindTrace);
                    _loopState = ArenaLoopState.Combat;
                }
                break;

            case PrologueStage.FindTrace:
                if (Vector2.DistanceSquared(_player.Position, PrologueDirector.SoulTrace) <= 185f * 185f &&
                    (_player.SoulSenseActive || _forceSoulSense))
                {
                    _prologue.Enter(PrologueStage.TraceWitnessed);
                    _audio.Play(AudioCue.SoulSenseOn, 0.58f);
                    _particles.EmitConvergence(PrologueDirector.SoulTrace, 18, 118f, GameBalance.DeathFlameBright, 0.45f, 5f);
                }
                break;

            case PrologueStage.TraceWitnessed:
                if (_prologue.StateTime >= 5.6f)
                {
                    SpawnPrologueEnemy(new Hollow(new Vector2(1160f, 545f), 31));
                    _prologueEncounterArmed = true;
                    _prologue.Enter(PrologueStage.EmergenceThreat);
                    _audio.SetCalm(false);
                    _audio.Play(AudioCue.WaveStart, 0.48f);
                }
                break;

            case PrologueStage.EmergenceThreat:
                if (_prologueEncounterArmed && PrologueFloorClear())
                {
                    _prologue.Enter(PrologueStage.LeaveEmergence);
                    _audio.SetCalm(true);
                    _audio.Play(AudioCue.WaveClear, 0.54f);
                }
                break;

            case PrologueStage.LeaveEmergence:
                if (_player.Position.X >= 1550f)
                {
                    EnterSearchSector();
                }
                break;

            case PrologueStage.SearchApproach:
                if (_player.Position.X >= 520f)
                {
                    SpawnPrologueEnemy(new Hollow(new Vector2(760f, 470f), 41));
                    SpawnPrologueEnemy(new Hollow(new Vector2(805f, 650f), 42));
                    _prologueEncounterArmed = true;
                    _prologue.Enter(PrologueStage.HollowLesson);
                    _audio.SetCalm(false);
                    _audio.Play(AudioCue.WaveStart, 0.52f);
                }
                break;

            case PrologueStage.HollowLesson:
                if (_prologueEncounterArmed && PrologueFloorClear())
                {
                    _prologueEncounterArmed = false;
                    _prologue.Enter(PrologueStage.BurningLesson);
                    _audio.SetCalm(true);
                }
                break;

            case PrologueStage.BurningLesson:
                if (!_prologueEncounterArmed && _player.Position.X >= 930f)
                {
                    SpawnPrologueEnemy(new Burning(new Vector2(1145f, 540f), 51));
                    SpawnPrologueEnemy(new Hollow(new Vector2(1215f, 680f), 52));
                    _prologueEncounterArmed = true;
                    _audio.SetCalm(false);
                    _audio.Play(AudioCue.WaveStart, 0.56f);
                }
                else if (_prologueEncounterArmed && PrologueFloorClear())
                {
                    _prologue.Enter(PrologueStage.LeaveSearch);
                    _audio.SetCalm(true);
                    _audio.Play(AudioCue.WaveClear, 0.54f);
                }
                break;

            case PrologueStage.LeaveSearch:
                if (_player.Position.X >= 1550f)
                {
                    EnterEscapeSector();
                }
                break;

            case PrologueStage.DevourerPressure:
                if (_souls.Any(soul => soul.State is SoulState.Exposed or SoulState.Releasing or SoulState.Residue))
                {
                    _prologueReleaseObserved = true;
                }
                if (_prologueEncounterArmed && _prologueReleaseObserved && PrologueFloorClear())
                {
                    _prologue.Enter(PrologueStage.ReleaseWitness);
                    _audio.SetCalm(true);
                    _audio.Play(AudioCue.WaveClear, 0.6f);
                }
                break;

            case PrologueStage.ReleaseWitness:
                if (_prologue.StateTime >= 5.2f)
                {
                    _prologue.Enter(PrologueStage.BoardVehicle);
                }
                break;

            case PrologueStage.BoardVehicle:
                if (Vector2.DistanceSquared(_player.Position, PrologueDirector.VehicleDock) <= 175f * 175f)
                {
                    BeginTransit();
                }
                break;

            case PrologueStage.Transit:
                UpdateTransit(deltaTime);
                break;

            case PrologueStage.Arrival:
                if (_prologue.StateTime >= 4.5f && _player.Position.Y <= 620f)
                {
                    _prologue.Enter(PrologueStage.Complete);
                    _loopState = ArenaLoopState.Complete;
                    _player.SettleForCompletion();
                    _presentation.BeginCompletion();
                    _audio.Play(AudioCue.EndingReveal, 0.7f);
                }
                break;
        }
    }

    private void EnterSearchSector()
    {
        ClearPrologueActivity();
        _player.Reset(PrologueDirector.SearchSpawn);
        _prologue.Enter(PrologueStage.SearchApproach);
        _loopState = ArenaLoopState.Combat;
        _audio.SetCalm(true);
        _screenEffects.Flash(0.13f, 0.1f);
        _particles.EmitDeathFlame(new Vector2(548f, 432f), 10, 0.75f);
    }

    private void EnterEscapeSector()
    {
        ClearPrologueActivity();
        _player.Reset(PrologueDirector.EscapeSpawn);
        _prologue.Enter(PrologueStage.DevourerPressure);
        _loopState = ArenaLoopState.Combat;
        _prologueEncounterArmed = true;
        _prologueReleaseObserved = false;
        _audio.SetCalm(false);
        _screenEffects.Flash(0.15f, 0.12f);

        Devourer carrier = new(new Vector2(880f, 545f));
        Soul held = new(carrier.Position);
        carrier.SeedHeldSoul(held);
        _souls.Add(held);
        SpawnPrologueEnemy(carrier);
        SpawnPrologueEnemy(new Hollow(new Vector2(690f, 690f), 61));
        SpawnPrologueEnemy(new Burning(new Vector2(1050f, 410f), 62));
        _audio.Play(AudioCue.WaveStart, 0.66f);
    }

    private void BeginTransit()
    {
        ClearPrologueActivity();
        _player.Reset(PrologueDirector.VehicleSpawn);
        _prologue.Enter(PrologueStage.Transit);
        _loopState = ArenaLoopState.Combat;
        _transitWave = 0;
        _transitSpawnTimer = 4.5f;
        _transitArrivalCuePlayed = false;
        _audio.SetCalm(false);
        _screenEffects.AddShake(0.36f, 4.5f);
        _audio.Play(AudioCue.WaveStart, 0.62f, -0.08f);
    }

    private void UpdateTransit(float deltaTime)
    {
        _transitSpawnTimer -= deltaTime;
        if (_transitWave < 5 && _transitSpawnTimer <= 0f)
        {
            SpawnTransitWave(_transitWave);
            _transitWave++;
            _transitSpawnTimer = _transitWave switch
            {
                1 => 10.5f,
                2 => 11.5f,
                3 => 11.5f,
                _ => 11f
            };
        }

        if (!_transitArrivalCuePlayed && _prologue.StateTime >= 54f)
        {
            _transitArrivalCuePlayed = true;
            _audio.Play(AudioCue.WaveClear, 0.48f);
        }

        if (_prologue.StateTime >= 62f && PrologueFloorClear())
        {
            EnterThreshold();
        }
    }

    private void SpawnTransitWave(int wave)
    {
        switch (wave)
        {
            case 0:
                SpawnPrologueEnemy(new Hollow(new Vector2(560f, 430f), 71));
                SpawnPrologueEnemy(new Hollow(new Vector2(1240f, 650f), 72));
                break;
            case 1:
                SpawnPrologueEnemy(new Burning(new Vector2(1235f, 460f), 73));
                break;
            case 2:
                SpawnPrologueEnemy(new Hollow(new Vector2(560f, 650f), 74));
                SpawnPrologueEnemy(new Hollow(new Vector2(1240f, 430f), 75));
                break;
            case 3:
                SpawnPrologueEnemy(new Burning(new Vector2(570f, 450f), 76));
                SpawnPrologueEnemy(new Hollow(new Vector2(1220f, 650f), 77));
                break;
            default:
                SpawnPrologueEnemy(new Devourer(new Vector2(1190f, 535f)));
                break;
        }

        _audio.Play(AudioCue.WaveStart, 0.42f, MathF.Min(0.16f, wave * 0.03f));
    }

    private void EnterThreshold()
    {
        ClearPrologueActivity();
        _player.Reset(PrologueDirector.ThresholdSpawn);
        _prologue.Enter(PrologueStage.Arrival);
        _loopState = ArenaLoopState.Combat;
        _audio.SetCalm(true);
        _screenEffects.Flash(0.18f, 0.16f);
        _particles.EmitConvergence(new Vector2(900f, 334f), 28, 180f, GameBalance.DeathFlameBright, 0.65f, 6f);
    }

    private void SpawnPrologueEnemy(Enemy enemy)
    {
        _enemies.Add(enemy);
        bool heavy = enemy is Devourer;
        _spriteVfx.Spawn(VisualIds.DashIgnition, enemy.Position, 0f, heavy ? 1.15f : 0.7f, GameBalance.DeathFlame * 0.6f);
        _particles.EmitDeathFlame(enemy.Position, heavy ? 22 : 11, heavy ? 1.45f : 1f);
        _particles.EmitConvergence(enemy.Position, heavy ? 20 : 12, heavy ? 128f : 84f, GameBalance.DeathFlameBright, 0.3f, heavy ? 6f : 4f);
        _arenaAtmosphere.ReactToForce(enemy.Position, heavy ? 320f : 190f, heavy ? 96f : 54f);
        _screenEffects.AddShake(heavy ? 0.2f : 0.07f, heavy ? 6.5f : 1.6f);
    }

    private bool PrologueFloorClear() =>
        !_enemies.Any(enemy => enemy.IsAlive) && _souls.Count == 0;

    /// <summary>Clears one sector's encounter state and revives the player for the next.</summary>
    private void ClearPrologueActivity()
    {
        ClearRunState();
        _prologueEncounterArmed = false;
        _prologueReleaseObserved = false;
    }
}
