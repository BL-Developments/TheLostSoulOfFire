using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Audio;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Core;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Menu;
using TheLostSoulOfFire.Rendering;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Game;

public enum GamePhase
{
    Title,
    Prologue,
    Antechamber,
    EnteringArena,
    Arena
}

public enum ArenaLoopState
{
    Intro,
    Combat,
    /// <summary>Pause after a cleared wave; the player starts the next one at the arena centre.</summary>
    Intermission,
    Transition,
    Complete
}

public sealed partial class GameWorld : IDisposable
{
    private const float DoorTransitionDuration = 1.35f;
    private readonly Arena _arena = new();
    private readonly SoulFurnaceAntechamber _antechamber = new();
    private readonly AudioDirector _audio;
    private readonly Camera2D _camera;
    private readonly ScreenEffects _screenEffects = new();
    private readonly ParticleSystem _particles = new();
    private readonly ArenaAtmosphere _arenaAtmosphere = new();
    private readonly HudRenderer _hud = new();
    private readonly LowHealthPresentation _lowHealth = new();
    private readonly SoulSensePresentation _soulSensePresentation = new();
    private readonly CinematicPresentation _presentation = new();
    private readonly ArtAssets _art;
    private readonly SpriteVfxSystem _spriteVfx;
    private readonly GroundImpacts _groundImpacts = new();
    private readonly List<SceneProp> _sceneProps = [];
    private readonly List<SceneProp> _arenaProps = Arena.Props
        .Select(placement => new SceneProp(placement.VisualId, placement.Foot, placement.FallbackSize, placement.FallbackLayer))
        .ToList();
    private readonly List<SceneProp> _hubProps = SoulFurnaceAntechamber.BrazierFeet
        .Select(foot => new SceneProp(VisualIds.HubBrazier, foot, new Vector2(48f, 76f), SceneLayer.HighProp))
        .ToList();
    private readonly List<SceneProp> _searchProps = PrologueDirector.SearchProps
        .Select(placement => new SceneProp(placement.VisualId, placement.Foot, placement.FallbackSize, placement.FallbackLayer))
        .ToList();
    private readonly Dictionary<IReadOnlyList<PropPlacement>, List<SceneProp>> _placedProps = [];

    /// <summary>Scene props for a placement list, created once and kept (they carry their occluder fade).</summary>
    private List<SceneProp> PropsOf(IReadOnlyList<PropPlacement> placements)
    {
        if (!_placedProps.TryGetValue(placements, out List<SceneProp>? props))
        {
            props = placements.Select(placement => new SceneProp(placement.VisualId, placement.Foot, placement.FallbackSize, placement.FallbackLayer)).ToList();
            _placedProps[placements] = props;
        }
        return props;
    }

    private readonly List<SceneProp> _shoreProps = PrologueDirector.ShoreProps
        .Select(placement => new SceneProp(placement.VisualId, placement.Foot, placement.FallbackSize, placement.FallbackLayer))
        .ToList();
    private readonly List<DepthItem> _actorBand = [];
    private readonly List<(RectangleF Bounds, float FootY)> _occlusionTargets = [];
    private readonly CombatPresentation _combatPresentation;
    private readonly MenuController _menu;
    private readonly MenuController _pauseMenu;
    private readonly CharacterMenu _characterMenu = new();
    private readonly GameSettings _settings;
    private readonly Action<GameSettings>? _settingsChanged;
    private readonly bool _skipMainMenu;
    private readonly bool _skipPrologue;
    private readonly PrologueDirector _prologue = new();
    private readonly Player _player;
    private readonly List<Enemy> _enemies = [];
    private readonly List<Soul> _souls = [];
    private readonly List<CannonShot> _cannonShots = [];
    private Vector2 _lastMouseWorld;
    private bool _debugVisible;
    private bool _forceSoulSense;
    private int _waveNumber;
    private ArenaWaveRun _waveRun = ArenaWaveRun.Empty;
    private readonly List<PendingArenaSpawn> _pendingSpawns = [];
    private int _reinforcementSeed;
    private GamePhase _phase = GamePhase.Title;
    private ArenaLoopState _loopState = ArenaLoopState.Intro;
    private float _phaseTime;
    private float _burningHandoffTimer;
    private int _burningCommittedLastFrame;
    private float _presentationTime;
    private float _fpsTimer;
    private int _fpsFrames;
    private int _fps = 60;
    private bool _audioTestFatalDamageRequested;
    private int _automatedDamageRequest;
    private bool _endingRevealPlayed;
    private bool _prologueEncounterArmed;
    private bool _prologueReleaseObserved;
    private int _transitWave;
    private float _transitSpawnTimer;
    private bool _transitArrivalCuePlayed;

    public string ScreenshotContext => _characterMenu.IsOpen ? GetScreenshotContext() :
        _abilities.FeedbackRemaining > 0 ? "ability_" + _abilities.Feedback : GetScreenshotContext();
    public GamePhase Phase => _phase;
    internal PrologueStage PrologueStage => _prologue.Stage;
    /// <summary>Pause, character or dev menu is open; the world is frozen under each of them.</summary>
    private bool IsGamePaused => _pauseMenu.IsOpen || _characterMenu.IsOpen || _devMenu.IsOpen;
    private bool IsCombatPhase => _phase is GamePhase.Arena or GamePhase.Prologue;

    /// <summary>The colour grade of the area being shown; areas without their own LUT fall back to neutral.</summary>
    private string CurrentGradeId => _phase switch
    {
        GamePhase.Prologue when _prologue.Sector == PrologueSector.Emergence => VisualIds.GradeShore,
        GamePhase.Arena => VisualIds.GradeArena,
        _ => VisualIds.GradeNeutral
    };
    /// <summary>The ambience and music zone of what is on screen (presentation only).</summary>
    private AudioZone CurrentAudioZone => _phase switch
    {
        GamePhase.Title => AudioZone.Title,
        GamePhase.Antechamber or GamePhase.EnteringArena => AudioZone.Hub,
        GamePhase.Prologue => _prologue.Sector switch
        {
            PrologueSector.Emergence => AudioZone.Shore,
            PrologueSector.Search => AudioZone.Harbour,
            PrologueSector.Escape => _prologue.IsVehicleRide ? AudioZone.Crossing : AudioZone.Causeway,
            _ => AudioZone.Threshold
        },
        _ => AudioZone.Arena
    };

    private Rectangle ActiveCombatBounds => _phase == GamePhase.Prologue ? _prologue.MovementBounds : _arena.CombatBounds;
    private Rectangle ActiveWorldBounds => _phase == GamePhase.Prologue ? PrologueDirector.WorldBounds : _arena.Bounds;
    private ArenaLoopState CameraLoopState => _phase == GamePhase.Prologue && _loopState == ArenaLoopState.Complete
        ? ArenaLoopState.Combat
        : _loopState;
    public ArenaLoopState LoopState => _loopState;
    public int WaveNumber => _waveNumber;
    public bool PlayerDead => _player.IsDead;
    public bool QuitRequested { get; private set; }
    public bool CombatActionsEnabled => GameFlowRules.AllowsCombat(_phase) &&
        !_player.IsDead && _loopState is ArenaLoopState.Combat or ArenaLoopState.Intermission or ArenaLoopState.Transition;

    private float DoorTransitionProgress => _phase == GamePhase.EnteringArena
        ? MathHelper.Clamp(_phaseTime / DoorTransitionDuration, 0f, 1f)
        : 0f;

    public string WindowTitle => _debugVisible
        ? $"The Lost Soul of Fire — DEBUG | {_phase.ToString().ToUpperInvariant()} {_loopState.ToString().ToUpperInvariant()} | Wave {_waveNumber}/{GameBalance.ArenaWaveCount} | HP {_player.Health} | RES {(_player.ResonanceActive ? $"ACTIVE {_player.ResonanceRemaining:0.0}s" : $"{_player.Resonance:0}/{GameBalance.ResonanceRequired:0}")} | Player {GetPlayerState()} | Enemies {_enemies.Count(enemy => enemy.IsAlive)} | Souls {_souls.Count}"
        : "The Lost Soul of Fire";

    public GameWorld(Viewport viewport, ArtAssets art, ContentManager content, bool skipMainMenu = false, bool skipPrologue = false, GameSettings? settings = null, Action<GameSettings>? settingsChanged = null, PlayerProfileStore? profileStore = null)
    {
        _skipMainMenu = skipMainMenu;
        _skipPrologue = skipPrologue;
        _settings = settings ?? new GameSettings();
        _menu = new MenuController(_settings) { AnchorX = art.HasArt(VisualIds.TitleBackdrop) ? 0.74f : 0.5f };
        _pauseMenu = new MenuController(_settings);
        _settingsChanged = settingsChanged;
        _profileStore = profileStore ?? new PlayerProfileStore();
        _wallet.LoadSecured(_profileStore.Load());
        _art = art;
        _presentation.Art = art;
        _audio = new AudioDirector(content);
        ApplyAudioSettings();
        _spriteVfx = new SpriteVfxSystem(art);
        _combatPresentation = new CombatPresentation(_particles, _screenEffects, _spriteVfx);
        _camera = new Camera2D(_arena.CombatBounds.Center.ToVector2());
        _player = new Player(_arena.CombatBounds.Center.ToVector2());
        _lastMouseWorld = _player.Position + Vector2.UnitX * 200f;
        _camera.Follow(_arena.CombatBounds.Center.ToVector2(), _arena.Bounds, viewport);
        _audio.SetArenaActive(false, true);
    }

    public void Update(GameTime gameTime, InputState input, Viewport viewport)
    {
        float deltaTime = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 1f / 20f);
        _audio.SetZone(CurrentAudioZone);
        // After the last wave the Life Flame is heard from the furnace as it kindles, and the
        // music turns to the theme in which the lost soul's motif resolves.
        _player.SinceDeath = _player.IsDead ? _player.SinceDeath + deltaTime : 0f;
        bool ending = _phase == GamePhase.Arena && _loopState == ArenaLoopState.Complete;
        _audio.SetEnding(ending);
        _audio.SetLifeFlame(ending ? _presentation.GetLifeFlameAlpha() * _presentation.GetLifeFlameKindle() : 0f,
            PanOf(_presentation.GetLifeFlamePosition()) * 0.7f);
        UpdateEnemyPresence(deltaTime);
        UpdateWardenFlames(deltaTime);
        _audio.SetResonanceRumble(IsCombatPhase && _player.ResonanceActive && !_player.IsDead, deltaTime);
        _audio.SetCannonHum(IsCombatPhase && !_player.IsDead && _player.Cannon.State == SoulCannonState.Charging
            ? _player.Cannon.ChargeProgress : null, deltaTime);
        if (_devMenu.IsOpen)
        {
            _audio.Update(deltaTime);
            UpdateDevMenu(deltaTime, input);
            return;
        }

        if (HandleAbilitySelection(input)) { _audio.Update(deltaTime); return; }
        if (_characterMenu.IsOpen)
        {
            // Same freeze as the pause menu: only the menu and the audio mix advance.
            _audio.Update(deltaTime);
            UpdateCharacterMenu(deltaTime, input, viewport);
            return;
        }

        if (_pauseMenu.IsOpen)
        {
            // Paused: only the menu and the audio mix advance; every simulation and
            // presentation clock below stays frozen until the menu closes.
            _audio.Update(deltaTime);
            UpdatePauseMenu(deltaTime, input, viewport);
            return;
        }

        if (_phase != GamePhase.Title && input.WasKeyPressed(Keys.Escape))
        {
            _pauseMenu.Open(MenuPages.Pause);
            _audio.SetPaused(true);
            _audio.Play(AudioCue.UiOpen, 0.45f);
            return;
        }

        if (_phase != GamePhase.Title && input.WasKeyPressed(Keys.Tab))
        {
            _characterMenu.Open();
            _audio.SetPaused(true);
            _audio.Play(AudioCue.UiOpen, 0.45f);
            return;
        }

        if (_sandboxActive && !_player.IsDead && input.WasKeyPressed(Keys.F))
        {
            _devMenu.Open();
            _audio.SetPaused(true);
            return;
        }

        _presentationTime += deltaTime;
        _screenEffects.MotionScale = _settings.CameraMotion switch
        {
            CameraMotionLevel.Reduced => 0.35f,
            CameraMotionLevel.Off => 0f,
            _ => 1f
        };
        _phaseTime += deltaTime;
        _presentation.Update(deltaTime, _phase);
        _hud.Update(deltaTime, _player);
        // The bound soul throbs when it runs low, in fights only (not in the ending's calm).
        _lowHealth.Update(deltaTime, _player.Health / (float)Math.Max(1, _player.MaxHealth),
            IsCombatPhase && !_player.IsDead && !(_phase == GamePhase.Arena && _loopState == ArenaLoopState.Complete));
        _hud.Throb = _lowHealth.Pulse;
        if (_lowHealth.BeatStarted)
        {
            _audio.Play(AudioCue.SoulThrob, 0.6f * _lowHealth.Amount);
        }
        _audio.Update(deltaTime);
        bool wasDashing = _player.IsDashing;
        bool wasResonanceActive = _player.ResonanceActive;
        bool wasResonanceReady = _player.IsResonanceReady;
        bool wasSoulSenseActive = _player.SoulSenseActive;
        bool wasCannonFull = _player.Cannon.IsFullCharge;
        SoulCannonState previousCannonState = _player.Cannon.State;
        int previousHealth = _player.Health;
        _art.Update(deltaTime);
        UpdateSceneProps(deltaTime);
        _spriteVfx.Update(deltaTime);
        _groundImpacts.Update(deltaTime);
        UpdateFps(deltaTime);
        _screenEffects.Update(deltaTime);
        _camera.ZoomPunch = _screenEffects.ZoomPunch;
        _combatPresentation.Update(deltaTime);
        if (_phase is GamePhase.Title or GamePhase.Arena)
        {
            _arenaAtmosphere.Update(deltaTime, _phase == GamePhase.Arena && _loopState == ArenaLoopState.Complete);
        }

        if (_phase == GamePhase.Title)
        {
            if (_skipMainMenu)
            {
                if (input.WasKeyPressed(Keys.Escape))
                {
                    QuitRequested = true;
                    return;
                }
                if (input.AnyInputPressed && !input.WasKeyPressed(Keys.F9))
                {
                    _audio.Play(AudioCue.TitleConfirm, 0.58f);
                    StartNewGame(viewport);
                    return;
                }
            }
            else
            {
                UpdateMenu(deltaTime, input, viewport);
                // The menu can leave the title phase from inside UpdateMenu; skip the
                // title camera for that frame so the next phase owns it immediately.
                if (_phase != GamePhase.Title)
                {
                    return;
                }
            }
            _soulSensePresentation.Update(deltaTime, false);
            _particles.Update(deltaTime);
            float smoothing = 1f - MathF.Exp(-deltaTime * 2.4f);
            _camera.Zoom = MathHelper.Lerp(_camera.Zoom, 0.9f, smoothing);
            _camera.Follow(_arena.CombatBounds.Center.ToVector2() + new Vector2(0f, -36f), _arena.Bounds, viewport, smoothing);
            return;
        }

        if (_phase == GamePhase.Antechamber)
        {
            if (input.WasKeyPressed(Keys.F1))
            {
                _debugVisible = !_debugVisible;
            }
            UpdateAntechamber(deltaTime, input, viewport, wasDashing, wasSoulSenseActive);
            return;
        }

        if (_phase == GamePhase.EnteringArena)
        {
            UpdateDoorTransition(deltaTime, viewport);
            return;
        }

        if (input.WasKeyPressed(Keys.F1))
        {
            _debugVisible = !_debugVisible;
        }

        if (input.WasKeyPressed(Keys.F2))
        {
            _enemies.Add(new Hollow(_player.Position + new Vector2(290f, 0f), _enemies.Count + 1));
        }

        if (input.WasKeyPressed(Keys.F3))
        {
            _enemies.Add(new Burning(_player.Position + new Vector2(310f, 0f), _enemies.Count + 1));
        }

        if (input.WasKeyPressed(Keys.F4))
        {
            _enemies.Add(new Devourer(_player.Position + new Vector2(390f, 0f)));
        }

        if (input.WasKeyPressed(Keys.F5))
        {
            _player.FillResonance();
        }

        if (input.WasKeyPressed(Keys.F6))
        {
            foreach (Enemy enemy in _enemies.Where(enemy => enemy.IsAlive))
            {
                ApplyEnemyDamage(enemy, new DamageInfo(enemy.Health + enemy.MaxHealth, Vector2.Zero, enemy.Position));
            }

            // Also drop the pushes still to come so one press clears the whole wave.
            _waveRun.DiscardRemaining();
            _pendingSpawns.Clear();
        }

        if (input.WasKeyPressed(Keys.F7))
        {
            _forceSoulSense = !_forceSoulSense;
        }

        if (input.WasKeyPressed(Keys.F8))
        {
            RetryCurrentEncounter(viewport);
        }

        if (_phase == GamePhase.Prologue)
        {
            if (input.WasKeyPressed(Keys.D1)) DebugEnterPrologueStage(PrologueStage.FindTrace);
            if (input.WasKeyPressed(Keys.D2)) DebugEnterPrologueStage(PrologueStage.SearchApproach);
            if (input.WasKeyPressed(Keys.D3)) DebugEnterPrologueStage(PrologueStage.DevourerPressure);
            if (input.WasKeyPressed(Keys.D4)) DebugEnterPrologueStage(PrologueStage.Transit);
        }

        if (_player.IsDead && input.WasKeyPressed(Keys.R))
        {
            RetryCurrentEncounter(viewport);
        }

        if (_phase == GamePhase.Arena && _loopState == ArenaLoopState.Complete && input.WasKeyPressed(Keys.R))
        {
            ResetFullRun(viewport);
            return;
        }

        if (_player.IsDead)
        {
            _soulSensePresentation.Update(deltaTime, false);
            _particles.Update(deltaTime);
            _presentation.UpdateCamera(
                _camera,
                CameraLoopState,
                true,
                _player.Position,
                ActiveWorldBounds,
                ActiveCombatBounds,
                viewport,
                deltaTime);
            return;
        }

        if (_loopState == ArenaLoopState.Complete)
        {
            if (_phase == GamePhase.Prologue)
            {
                if (_prologue.StateTime >= 1.5f && input.AnyInputPressed && !input.WasKeyPressed(Keys.F9))
                {
                    BeginAntechamber(viewport);
                    return;
                }
            }
            else
            {
                if (!_endingRevealPlayed && _presentation.StateTime >= CinematicPresentation.LifeFlameRevealTime)
                {
                    _endingRevealPlayed = true;
                    _audio.Play(AudioCue.EndingReveal, 0.72f);
                }
                // A beat after it kindles, the figure turns to the Life Flame.
                if (_presentation.StateTime >= CinematicPresentation.LifeFlameRevealTime + 0.35f)
                {
                    _player.LookToward(_presentation.GetLifeFlamePosition());
                }
            }
            UpdateLoop(deltaTime);
            _soulSensePresentation.Update(deltaTime, false);
            _particles.Update(deltaTime);
            _presentation.UpdateCamera(
                _camera,
                CameraLoopState,
                false,
                _player.Position,
                ActiveWorldBounds,
                ActiveCombatBounds,
                viewport,
                deltaTime);
            return;
        }

        if (_loopState == ArenaLoopState.Intro)
        {
            UpdateLoop(deltaTime);
            _soulSensePresentation.Update(deltaTime, false);
            _particles.Update(deltaTime);
            _presentation.UpdateCamera(
                _camera,
                CameraLoopState,
                false,
                _player.Position,
                ActiveWorldBounds,
                ActiveCombatBounds,
                viewport,
                deltaTime);
            return;
        }

        _lastMouseWorld = AutomatedAimOr(_camera.ScreenToWorld(input.MouseVirtualPosition.ToPoint(), viewport));

        if (_screenEffects.IsHitStopped)
        {
            _soulSensePresentation.Update(deltaTime, _player.SoulSenseActive);
            return;
        }

        _player.Update(deltaTime, input, _lastMouseWorld, ActiveCombatBounds, _particles, _screenEffects, _forceSoulSense);
        UpdateFootsteps();
        UpdateEnemyFootsteps();
        _soulSensePresentation.Update(deltaTime, _player.SoulSenseActive);
        if (_audioTestFatalDamageRequested)
        {
            _audioTestFatalDamageRequested = false;
            _player.ApplyDamage(GameBalance.PlayerMaxHealth, Vector2.Zero, _screenEffects, ignoreArmor: true);
        }
        if (_automatedDamageRequest > 0)
        {
            _player.ApplyDamage(_automatedDamageRequest, Vector2.Zero, _screenEffects, ignoreArmor: true);
            _automatedDamageRequest = 0;
        }
        if (_player.Scythe.StartedThisFrame)
        {
            _combatPresentation.SlashRibbons = _art.CanDrawDeathFlame && _art.HasClip(VisualIds.Player, VisualClips.Aim);
            _combatPresentation.PresentScytheSwing(
                _player.Scythe.ActiveStep,
                _player.Position,
                _player.Scythe.AttackDirection);
        }
        if (!wasDashing && _player.IsDashing)
        {
            _spriteVfx.Spawn(
                VisualIds.DashIgnition,
                _player.Position - _player.DashDirection * 24f,
                MathF.Atan2(_player.DashDirection.Y, _player.DashDirection.X),
                0.72f);
        }
        if (!wasResonanceActive && _player.ResonanceActive)
        {
            _combatPresentation.BeginResonance(_player.Position);
            _arenaAtmosphere.ReactToResonance();
        }
        PlayPlayerActionAudio(wasDashing, wasResonanceActive, wasSoulSenseActive, wasCannonFull, previousCannonState);
        UpdateAbilities(deltaTime, input);
        SpawnCannonShot();
        ResolveScytheStrike();
        UpdateCannonShots(deltaTime);
        UpdateBurningHandoff();
        ConfigureBurningAggression(deltaTime);
        foreach (Enemy enemy in _enemies)
        {
            HollowState? previousHollowState = enemy is Hollow hollowBefore ? hollowBefore.State : null;
            BurningState? previousBurningState = enemy is Burning burningBefore ? burningBefore.State : null;
            DevourerState? previousDevourerState = enemy is Devourer devourerBefore ? devourerBefore.State : null;
            enemy.Update(deltaTime, _player, _souls, ActiveCombatBounds, _particles, _screenEffects);
            if (enemy is Hollow windingHollow && previousHollowState != HollowState.Telegraph && windingHollow.State == HollowState.Telegraph)
            {
                // The mask creaks and it draws breath as the arm goes back: the swipe is coming.
                _audio.Play(AudioCue.HollowWindup, 0.8f, 0f, PanOf(enemy.Position));
            }
            if (enemy is Burning rushingBurning && previousBurningState != BurningState.Charge && rushingBurning.State == BurningState.Charge)
            {
                _audio.Play(AudioCue.BurningRush, 0.86f, 0f, PanOf(enemy.Position));
            }
            if (enemy is Devourer liftingDevourer && previousDevourerState != DevourerState.SlamTelegraph && liftingDevourer.State == DevourerState.SlamTelegraph)
            {
                _audio.Play(AudioCue.DevourerWindup, 0.72f, 0f, PanOf(enemy.Position) * 0.6f);
            }
            if (enemy is Devourer gatheringDevourer && gatheringDevourer.State == DevourerState.SlamTelegraph)
            {
                _groundImpacts.WindUp(gatheringDevourer, gatheringDevourer.Position, GameBalance.DevourerSlamRange, gatheringDevourer.SlamWindup, deltaTime);
            }
            if (enemy is Hollow hollowAfter && previousHollowState != HollowState.Swipe && hollowAfter.State == HollowState.Swipe)
            {
                // The grab is a danger signal: clearly above the room, below a hit.
                _audio.Play(AudioCue.HollowSwipe, 0.7f, 0f, PanOf(enemy.Position));
            }
            if (enemy is Burning burningAfter && previousBurningState != BurningState.Telegraph && burningAfter.State == BurningState.Telegraph)
            {
                _audio.Play(AudioCue.BurningCharge, 0.85f, 0f, PanOf(enemy.Position));
            }
            if (enemy is Devourer devourerAfter)
            {
                if (previousDevourerState != DevourerState.Slam && devourerAfter.State == DevourerState.Slam)
                {
                    _audio.Play(AudioCue.DevourerSlam, 0.76f, 0f, PanOf(devourerAfter.Position) * 0.6f);
                    PresentDevourerSlam(devourerAfter);
                }
                if (previousDevourerState != DevourerState.Devour && devourerAfter.State == DevourerState.Devour)
                {
                    _audio.Play(AudioCue.DevourerDevour, 0.6f, 0f, PanOf(devourerAfter.Position));
                }
            }
            if (enemy.TryConsumeSoulSpawn(out Vector2 soulPosition))
            {
                _souls.Add(new Soul(soulPosition));
            }

            if (enemy is Burning burning && burning.TryConsumeDetonation(out Vector2 detonationPosition))
            {
                ResolveBurningDetonation(burning, detonationPosition);
            }

            if (enemy is Devourer devourer && devourer.TryConsumeExtractionEffect(out Vector2 extractionPosition))
            {
                _spriteVfx.Spawn(VisualIds.SoulRelease, extractionPosition, 0f, 0.72f);
                _particles.EmitBurst(extractionPosition, Vector2.UnitY, 28, GameBalance.SoulWhite, 260f, 9f);
                _particles.EmitDeathFlame(extractionPosition, 18, 1.25f);
                _screenEffects.AddShake(0.18f, 8f);
                _screenEffects.Flash(0.08f, 0.26f);
            }
        }

        UpdateBurningHandoff();

        _enemies.RemoveAll(enemy => enemy.IsFinished);
        foreach (Soul soul in _souls)
        {
            SoulState previousSoulState = soul.State;
            soul.Update(deltaTime, _player, _particles);
            if (previousSoulState != SoulState.Releasing && soul.State == SoulState.Releasing)
            {
                _spriteVfx.Spawn(VisualIds.SoulRelease, soul.Position, 0f, 0.62f);
                _audio.Play(AudioCue.SoulRelease, 0.62f, 0f, PanOf(soul.Position) * 0.6f);
            }
        }

        _souls.RemoveAll(soul => soul.IsFinished);
        UpdateCurrency(deltaTime, input);
        UpdateLoop(deltaTime);
        if (_loopState == ArenaLoopState.Complete) _abilities.Clear(_player);
        if (previousHealth > _player.Health)
        {
            if (_player.IsDead)
            {
                _abilities.Clear(_player);
                LoseRunCurrencies();
                _audio.SetCalm(true);
                _audio.SetSoulSense(false);
                _presentation.BeginDeath();
            }
            // Taking damage must never hide under the player's own swings (mix review 06.10.2026).
            _audio.Play(_player.IsDead ? AudioCue.PlayerDeath : AudioCue.PlayerHit, _player.IsDead ? 0.82f : 0.95f);
            // The blow itself lands at once; the Ludo hurt sound swells in just after it.
            _audio.Play(AudioCue.BodyHit, _player.IsDead ? 0.85f : 0.72f);
        }
        if (!wasResonanceReady && _player.IsResonanceReady)
        {
            _audio.Play(AudioCue.ResonanceReady, 0.72f);
        }
        _particles.Update(deltaTime);

        _presentation.UpdateCamera(
            _camera,
            CameraLoopState,
            false,
            _player.Position,
            ActiveWorldBounds,
            ActiveCombatBounds,
            viewport,
            deltaTime,
            CameraLead);
    }

    /// <summary>How far the combat camera leads toward the aim and the running direction (world units).</summary>
    private Vector2 CameraLead => _player.IsDead
        ? Vector2.Zero
        : _player.FacingDirection * 22f + _player.Velocity * 0.045f;

    private void UpdateMenu(float deltaTime, InputState input, Viewport viewport)
    {
        if (!_menu.IsOpen)
        {
            if (input.WasKeyPressed(Keys.Escape))
            {
                _menu.OpenQuitConfirmation();
                return;
            }
            if (input.AnyInputPressed && !input.WasKeyPressed(Keys.F9))
            {
                _audio.Play(AudioCue.TitleConfirm, 0.58f);
                _menu.Open();
            }
            return;
        }

        switch (UpdateMenuInput(_menu, deltaTime, input, viewport))
        {
            case MenuActionResult.NewGame:
                _menu.Close();
                StartNewGame(viewport);
                break;
            case MenuActionResult.Quit:
                QuitRequested = true;
                break;
        }
    }

    private void UpdatePauseMenu(float deltaTime, InputState input, Viewport viewport)
    {
        switch (UpdateMenuInput(_pauseMenu, deltaTime, input, viewport))
        {
            case MenuActionResult.Resume:
                _pauseMenu.Close();
                _audio.SetPaused(false);
                _audio.Play(AudioCue.UiClose, 0.45f);
                break;
            case MenuActionResult.QuitToMainMenu:
                _pauseMenu.Close();
                _audio.StopEffects();
                _audio.SetPaused(false);
                ResetFullRun(viewport);
                _menu.Open();
                break;
            case MenuActionResult.Quit:
                QuitRequested = true;
                break;
        }
    }

    private void UpdateCharacterMenu(float deltaTime, InputState input, Viewport viewport)
    {
        _characterMenu.Tick(deltaTime);
        if (input.WasKeyPressed(Keys.Tab) || input.WasKeyPressed(Keys.Escape) ||
            _characterMenu.SelectedTab == CharacterMenuTab.Abilities && (input.WasKeyPressed(Keys.C) || input.WasKeyPressed(Keys.Enter)))
        {
            _characterMenu.Close();
            _audio.SetPaused(false);
            _audio.Play(AudioCue.UiClose, 0.45f);
            return;
        }

        CharacterMenuTab tabBefore = _characterMenu.SelectedTab;
        if (input.WasKeyPressed(Keys.Left) || input.WasKeyPressed(Keys.A)) _characterMenu.SelectPrevious();
        else if (input.WasKeyPressed(Keys.Right) || input.WasKeyPressed(Keys.D)) _characterMenu.SelectNext();
        if (_characterMenu.SelectedTab != tabBefore)
        {
            _audio.Play(AudioCue.UiMove, 0.55f);
        }

        if (input.WasLeftMousePressed)
        {
            IReadOnlyList<Rectangle> bounds = _presentation.GetCharacterTabBounds(viewport);
            for (int i = 0; i < bounds.Count; i++)
            {
                if (bounds[i].Contains(input.MouseVirtualPosition))
                {
                    _characterMenu.Select(CharacterMenu.Tabs[i]);
                    break;
                }
            }
        }
        if (_characterMenu.SelectedTab == CharacterMenuTab.Abilities)
            UpdateSkillSelection(input, viewport);
    }

    private CharacterSheet CurrentCharacterSheet => new(
        _player.Health,
        _player.MaxHealth,
        _player.Attributes,
        _phase == GamePhase.Arena && !_sandboxActive,
        _wallet.Run(Currency.Geld),
        _wallet.Secured(Currency.Geld),
        _wallet.Run(Currency.Glut),
        _wallet.Secured(Currency.Glut));

    /// <summary>
    /// Shared mouse/keyboard handling for the title and pause menus. Navigation and value
    /// changes are applied here; results that leave the menu are returned to the caller.
    /// </summary>
    /// <summary>Menu input plus its sounds: a tick when the selection or a value moves, a softer one back.</summary>
    private MenuActionResult UpdateMenuInput(MenuController menu, float deltaTime, InputState input, Viewport viewport)
    {
        MenuPage? pageBefore = menu.IsOpen ? menu.CurrentPage : null;
        int indexBefore = menu.SelectedIndex;
        MenuActionResult result = UpdateMenuInputCore(menu, deltaTime, input, viewport);
        MenuPage? pageAfter = menu.IsOpen ? menu.CurrentPage : null;
        if (pageAfter != pageBefore && pageBefore is not null)
        {
            _audio.Play(input.WasKeyPressed(Keys.Escape) ? AudioCue.UiBack : AudioCue.UiMove, 0.6f);
        }
        else if (pageAfter is not null && menu.SelectedIndex != indexBefore)
        {
            _audio.Play(AudioCue.UiMove, 0.5f);
        }
        return result;
    }

    private MenuActionResult UpdateMenuInputCore(MenuController menu, float deltaTime, InputState input, Viewport viewport)
    {
        menu.Tick(deltaTime);
        if (input.WasKeyPressed(Keys.Escape))
        {
            bool leavingSettings = menu.IsSettingsPage;
            MenuActionResult escapeResult = menu.HandleEscape();
            if (leavingSettings)
            {
                _settingsChanged?.Invoke(_settings);
            }
            return escapeResult;
        }
        if (!menu.AcceptsInput)
        {
            return MenuActionResult.None;
        }

        IReadOnlyList<Rectangle> bounds = _presentation.GetMenuEntryBounds(viewport, menu.CurrentPage, menu);

        if (input.MouseMoved)
        {
            for (int i = 0; i < bounds.Count; i++)
            {
                if (bounds[i].Contains(input.MouseVirtualPosition))
                {
                    menu.SetHoverIndex(i);
                    break;
                }
            }
        }

        if (input.WasKeyPressed(Keys.Up) || input.WasKeyPressed(Keys.W))
        {
            menu.MoveSelection(-1);
        }
        else if (input.WasKeyPressed(Keys.Down) || input.WasKeyPressed(Keys.S))
        {
            menu.MoveSelection(1);
        }

        bool valueChanged = false;
        if (input.WasKeyPressed(Keys.Left) || input.WasKeyPressed(Keys.A)) valueChanged = menu.AdjustSelectedValue(-1);
        else if (input.WasKeyPressed(Keys.Right) || input.WasKeyPressed(Keys.D)) valueChanged = menu.AdjustSelectedValue(1);
        if (valueChanged)
        {
            ApplySettingsChanges();
            _audio.Play(AudioCue.UiMove, 0.45f, 0.1f);
        }

        bool confirmedByKeyboard = input.WasKeyPressed(Keys.Enter);
        bool confirmedByMouse = false;
        if (input.WasLeftMousePressed)
        {
            for (int i = 0; i < bounds.Count; i++)
            {
                if (bounds[i].Contains(input.MouseVirtualPosition))
                {
                    menu.SetHoverIndex(i);
                    if (MenuController.IsValueEntry(menu.CurrentPage.Entries[i].Id))
                    {
                        menu.AdjustSelectedValue(input.MouseVirtualPosition.X < viewport.Width * 0.5f ? -1 : 1);
                        ApplySettingsChanges();
                        return MenuActionResult.None;
                    }
                    confirmedByMouse = true;
                    break;
                }
            }
        }

        if (!confirmedByKeyboard && !confirmedByMouse)
        {
            return MenuActionResult.None;
        }

        MenuActionResult result = menu.Confirm();
        if (result == MenuActionResult.SettingsChanged)
        {
            ApplySettingsChanges();
            return MenuActionResult.None;
        }
        return result;
    }

    private void ApplySettingsChanges()
    {
        ApplyAudioSettings();
        _settingsChanged?.Invoke(_settings);
    }

    private void ApplyAudioSettings() => _audio.SetVolumes(
        _settings.MasterVolume / 100f,
        _settings.MusicVolume / 100f,
        _settings.EffectsVolume / 100f);

    public void Dispose()
    {
        _audio.Dispose();
        GC.SuppressFinalize(this);
    }

    internal void RequestAudioTestFatalDamage() => _audioTestFatalDamageRequested = true;

    internal void SetAutomatedSoulSense(bool active) => _forceSoulSense = active;

    internal void PlaceAutomatedPlayerAtDoor(int doorIndex)
    {
        if (_phase == GamePhase.Antechamber)
        {
            _player.Reset(_antechamber.Doors[doorIndex].InteractionZone.Center.ToVector2());
        }
    }

    internal void RequestAutomatedDoorEntry()
    {
        if (_phase == GamePhase.Antechamber)
        {
            BeginDoorTransition();
        }
    }

    public void Draw(SpriteBatch batch, Texture2D pixel, Viewport viewport, SoulfireRenderer renderer, RenderTarget2D? rootTarget = null)
    {
        ApplyAutomatedOverview(viewport);
        renderer.BeginScene(viewport);
        DrawScene(batch, pixel, viewport, renderer.SceneLights);
        renderer.PresentScene(
            batch,
            rootTarget,
            viewport,
            _soulSensePresentation.WorldSuppression,
            _art.GetSpriteTexture(CurrentGradeId) ?? _art.GetSpriteTexture(VisualIds.GradeNeutral),
            _art.GetSpriteTexture(VisualIds.GradeSoulSense));
        DrawSoulfireLighting(batch, renderer, viewport);
        _soulSensePresentation.DrawSoulLayer(
            batch,
            pixel,
            RenderResolution.ToOutput(_camera.GetTransform(viewport, _screenEffects.ShakeOffset)),
            _player,
            _enemies,
            _souls,
            _presentationTime,
            _art);
        if (_phase is GamePhase.Antechamber or GamePhase.EnteringArena)
        {
            batch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.LinearClamp,
                transformMatrix: RenderResolution.ToOutput(_camera.GetTransform(viewport, _screenEffects.ShakeOffset)));
            _antechamber.DrawSoulSense(batch, pixel, _presentationTime, _soulSensePresentation.SoulEmergence, _art.SoftSpot);
            batch.End();
        }
        renderer.DrawVignette(batch, viewport, _soulSensePresentation.WorldSuppression, _player.ResonanceActive);
        DrawLowHealth(batch, renderer);
        if (_automatedHideHud)
        {
            return;
        }
        DrawScreenFeedback(batch, pixel, viewport, renderer);
        DrawHud(batch, pixel, viewport);
    }

    private void DrawScene(SpriteBatch batch, Texture2D pixel, Viewport viewport, IReadOnlyList<SceneLight> lights)
    {
        Matrix sceneTransform = RenderResolution.ToOutput(_camera.GetTransform(viewport, _screenEffects.CameraOffset));
        (Vector3 backLight, Vector2 backLightFrom) = BackLight;
        _art.SetBackLight(backLight, backLightFrom);
        batch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.LinearClamp,
            transformMatrix: sceneTransform);
        _art.BeginLitScene(sceneTransform, lights);

        bool inAntechamber = _phase is GamePhase.Antechamber or GamePhase.EnteringArena;
        bool shouldDrawPlayer = inAntechamber ||
            IsCombatPhase && _presentation.ShouldDrawPlayer(_loopState, _player.IsDead);

        // Back to front, as in VISUAL-ART-DIRECTION §4: ground and low props, then actors and
        // high props by foot point, then occluders and foreground, then emission and effects.
        if (inAntechamber)
        {
            _antechamber.Draw(
                batch,
                pixel,
                _art,
                _presentationTime,
                _soulSensePresentation.SoulEmergence,
                DoorTransitionProgress,
                _debugVisible);
        }
        else
        {
            if (_phase == GamePhase.Prologue)
            {
                string? plate = PrologueEnvironment.PlateOf(_prologue);
                bool painted = plate is not null && _art.HasArt(plate);
                PrologueEnvironment.DrawGround(batch, pixel, _prologue, _presentationTime, _soulSensePresentation.WorldSuppression, painted);
                if (painted && _prologue.IsVehicleRide)
                {
                    PrologueEnvironment.DrawCrossing(batch, _art, _prologue.StateTime);
                }
                else if (painted)
                {
                    _art.DrawEnvironment(batch, plate!, Vector2.Zero);
                }
                PrologueEnvironment.DrawProps(batch, pixel, _prologue, _presentationTime, _soulSensePresentation.WorldSuppression, painted, _art);
            }
            else
            {
                _art.DrawEnvironment(batch, VisualIds.ArenaWall, Arena.WallFoot);
                _art.DrawEnvironment(batch, VisualIds.ArenaFloor, Arena.FloorTopLeft);
                DrawArenaShading(batch);
                _arenaAtmosphere.DrawBackground(batch, pixel, _soulSensePresentation.WorldSuppression, _art.HasArt(VisualIds.ArenaFloor) ? _art : null);
            }
            DrawSceneProps(batch, layer => layer < SceneLayer.Actor);
            DrawFloorDepth(batch, viewport);
            _groundImpacts.DrawFloor(batch);
            if (IsCombatPhase && _phase == GamePhase.Arena)
            {
                DrawArenaLoop(batch, pixel);
                DrawCurrencyWorld(batch, pixel);
            }
        }

        DrawGroundMist(batch);
        DrawFigureShadows(batch, lights, shouldDrawPlayer && (inAntechamber || IsCombatPhase));
        _player.DrawAfterimages(batch, pixel, _art);
        if (IsCombatPhase)
        {
            _art.DrawDissolves(batch);
        }
        DrawActorBand(batch, pixel, shouldDrawPlayer && (inAntechamber || IsCombatPhase));
        _groundImpacts.DrawAir(batch);
        DrawAutomatedStaging(batch);

        DrawSceneProps(batch, layer => layer is SceneLayer.Occluder or SceneLayer.Foreground);
        if (inAntechamber)
        {
            _antechamber.DrawBrazierFlames(batch, _art, _presentationTime);
        }
        if (_phase == GamePhase.Prologue && PrologueEnvironment.PlateOf(_prologue) is { } dressed && _art.HasArt(dressed))
        {
            int index = 0;
            foreach ((Vector2 flameBase, float height) in PrologueDirector.WardenFlames(_prologue.Sector, _prologue.IsVehicleRide))
            {
                _art.DrawWardenFlame(batch, flameBase, height, _presentationTime + index++ * 1.13f);
            }
        }
        if (_phase == GamePhase.Prologue)
        {
            PrologueEnvironment.DrawShoreEchoes(batch, _art, _prologue, _presentationTime, _soulSensePresentation.WorldSuppression);
            PrologueEnvironment.DrawForeground(batch, pixel, _prologue,
                PrologueEnvironment.PlateOf(_prologue) is { } foregroundPlate && _art.HasArt(foregroundPlate));
        }

        if (IsCombatPhase)
        {
            foreach (Soul soul in _souls)
            {
                _art.DrawLostSoul(batch, soul, _player);
                soul.Draw(batch, pixel, _player, false, true, _art.SoftSpot);
            }
        }
        DrawAbilityWorld(batch, pixel);

        // What flies (shots, sparks, slashes) is drawn at body height above its gameplay
        // position, because figures stand with their feet on their positions.
        batch.End();
        Matrix airTransform = Matrix.CreateTranslation(0f, -FigureHeights.Air, 0f) * sceneTransform;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, transformMatrix: airTransform);
        _art.BeginLitScene(airTransform, lights);
        if (IsCombatPhase)
        {
            foreach (CannonShot shot in _cannonShots)
            {
                shot.Draw(batch, pixel, true);
                _art.DrawCannonProjectile(batch, shot);
            }
        }
        _particles.Draw(batch, pixel, _art.SoftSpot);
        _spriteVfx.Draw(batch);
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, transformMatrix: sceneTransform);
        _art.BeginLitScene(sceneTransform, lights);

        DrawSceneProps(batch, layer => layer == SceneLayer.Atmosphere);
        if (MotesLook is { } motes)
        {
            ForegroundMotes.Draw(batch, _art, _camera, viewport, _presentationTime, motes, 53 + (int)_phase);
        }
        _presentation.DrawWorldAccents(batch, pixel, _art, _phase, _loopState, _player.IsDead, _player, ActiveCombatBounds);

        if (IsCombatPhase && _presentation.ShouldDrawAim(_loopState, _player.IsDead))
        {
            HudRenderer.DrawReticle(batch, pixel, _lastMouseWorld, _player.Cannon, _presentationTime);
        }

        if (_debugVisible && IsCombatPhase)
        {
            batch.DrawRectangle(pixel, ActiveCombatBounds, new Color(80, 220, 210) * 0.8f, 3f);
            Vector2 center = ActiveCombatBounds.Center.ToVector2();
            batch.DrawLine(pixel, center - Vector2.UnitX * 28f, center + Vector2.UnitX * 28f, new Color(80, 220, 210), 2f);
            batch.DrawLine(pixel, center - Vector2.UnitY * 28f, center + Vector2.UnitY * 28f, new Color(80, 220, 210), 2f);
        }

        _art.EndLitScene();
        batch.End();
    }

    /// <summary>
    /// The room's back light on the figures (colour times strength, and the screen direction it
    /// comes from): the furnace and Warden flames in the north walls of the foundry and the
    /// antechamber, the cold sea light over the prologue. A thin rim, never a second key light.
    /// </summary>
    private (Vector3 Color, Vector2 From) BackLight => _phase switch
    {
        GamePhase.Antechamber or GamePhase.EnteringArena => (GameBalance.DeathFlame.ToVector3() * 0.26f, new Vector2(0f, -1f)),
        GamePhase.Prologue => (new Vector3(0.42f, 0.52f, 0.72f) * 0.3f, new Vector2(-0.35f, -1f)),
        _ => (GameBalance.DeathFlame.ToVector3() * 0.3f, new Vector2(0.15f, -1f))
    };

    /// <summary>
    /// Painted shade over the evenly lit foundry floor (presentation only): the foot of the north
    /// wall lies in its shadow, the sides and the south fall off into the dark, a few broad
    /// blotches break the floor's even value; the middle, where the fights are, stays lit.
    /// </summary>
    private void DrawArenaShading(SpriteBatch batch)
    {
        if (!_art.HasArt(VisualIds.ArenaFloor))
        {
            return;
        }

        Color shade = new(4, 3, 9);
        _art.DrawShade(batch, new Rectangle(0, (int)Arena.WallFoot.Y, 1800, 150), shade * 0.5f);
        _art.DrawSoftSpot(batch, new Vector2(-40f, 520f), new Vector2(420f, 760f), shade * 0.55f);
        _art.DrawSoftSpot(batch, new Vector2(1840f, 520f), new Vector2(420f, 760f), shade * 0.55f);
        _art.DrawSoftSpot(batch, new Vector2(900f, 1080f), new Vector2(1150f, 300f), shade * 0.5f);
        _art.DrawSoftSpot(batch, new Vector2(60f, 1000f), new Vector2(380f, 300f), shade * 0.4f);
        _art.DrawSoftSpot(batch, new Vector2(1740f, 1000f), new Vector2(380f, 300f), shade * 0.4f);
        foreach ((Vector2 at, Vector2 size) in ArenaBlotches)
        {
            _art.DrawSoftSpot(batch, at, size, shade * 0.14f);
        }
    }

    /// <summary>
    /// Depth on the floor, under every figure (presentation only). Aerial perspective: the part
    /// of the floor farther from the camera (the top of the view) sits in a faint haze, the near
    /// edge sinks into shadow. In the foundry, the rose window throws a pale pool of light onto
    /// the middle of the hall, where the fights happen, with dust turning slowly in it.
    /// </summary>
    private void DrawFloorDepth(SpriteBatch batch, Viewport viewport)
    {
        bool arena = _phase == GamePhase.Arena && _art.HasArt(VisualIds.ArenaFloor);
        bool prologue = _phase == GamePhase.Prologue && PrologueEnvironment.PlateOf(_prologue) is { } plate && _art.HasArt(plate);
        if (!arena && !prologue)
        {
            return;
        }

        // The visible floor in world units.
        float zoom = MathF.Max(0.1f, _camera.Zoom);
        Vector2 half = new(viewport.Width * 0.5f / zoom, viewport.Height * 0.5f / zoom);
        Rectangle view = new((int)(_camera.Position.X - half.X) - 40, (int)(_camera.Position.Y - half.Y) - 40,
            (int)(half.X * 2f) + 80, (int)(half.Y * 2f) + 80);
        Color haze = arena ? new Color(150, 140, 178) : new Color(120, 136, 170);
        _art.DrawShade(batch, new Rectangle(view.X, view.Y, view.Width, (int)(view.Height * 0.42f)), haze * 0.07f);
        Rectangle near = new(view.X, view.Y + (int)(view.Height * 0.62f), view.Width, (int)(view.Height * 0.38f) + 2);
        DrawShadeUp(batch, near, new Color(4, 3, 9) * 0.16f);

        if (!arena)
        {
            return;
        }

        batch.End();
        batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, transformMatrix: _art.SceneTransform);
        float breathe = 1f + 0.04f * MathF.Sin(_presentationTime * 0.21f);
        Color window = new(205, 196, 236);
        // The landmark's light: a clear island in the nave, brightest under the window, so the hall
        // has a lit heart and darker aisles instead of one even value. (The additive blend weighs
        // colour by alpha, so the strength goes into the colour at full alpha.)
        _art.DrawSoftSpot(batch, RoseWindowPool, new Vector2(560f, 270f) * breathe, new Color(window.ToVector3() * 0.09f));
        _art.DrawSoftSpot(batch, RoseWindowPool + new Vector2(-20f, -30f), new Vector2(300f, 140f) * breathe, new Color(window.ToVector3() * 0.07f));
        // Dust turning slowly in the light: only where the pool is, fading in and out.
        for (int index = 0; index < 16; index++)
        {
            float seed = index * 12.9898f;
            float life = (_presentationTime * 0.05f + Fraction(MathF.Sin(seed) * 43758.5453f)) % 1f;
            Vector2 at = RoseWindowPool + new Vector2(
                (Fraction(MathF.Sin(seed * 1.7f) * 24634.6345f) - 0.5f) * 760f + MathF.Sin(_presentationTime * 0.13f + seed) * 30f,
                (Fraction(MathF.Sin(seed * 2.3f) * 15731.743f) - 0.5f) * 340f - life * 60f);
            Vector2 offset = (at - RoseWindowPool) / new Vector2(470f, 230f);
            float inside = MathHelper.Clamp(1f - offset.Length(), 0f, 1f);
            float fade = MathF.Sin(life * MathF.PI);
            _art.DrawSoftSpot(batch, at - new Vector2(0f, 40f + 30f * Fraction(seed)), new Vector2(2.2f), window * (0.5f * inside * fade));
        }
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, transformMatrix: _art.SceneTransform);
    }

    /// <summary>What floats between camera and room: ash in the foundry and the antechamber, sea mist over the prologue.</summary>
    private ForegroundMotes.Look? MotesLook => _phase switch
    {
        GamePhase.Arena when !_sandboxActive => new ForegroundMotes.Look(new Color(236, 226, 255), 0.34f, new Vector2(5f, 9f), 12),
        GamePhase.Antechamber or GamePhase.EnteringArena => new ForegroundMotes.Look(new Color(226, 214, 250), 0.28f, new Vector2(3f, 7f), 9),
        GamePhase.Prologue when _prologue.IsVehicleRide => new ForegroundMotes.Look(new Color(206, 222, 250), 0.26f, new Vector2(-46f, 4f), 10),
        GamePhase.Prologue => new ForegroundMotes.Look(new Color(206, 222, 250), 0.24f, new Vector2(-9f, 3f), 10),
        _ => null
    };

    /// <summary>Where the rose window's light falls on the foundry floor.</summary>
    private static readonly Vector2 RoseWindowPool = new(930f, 420f);

    private static float Fraction(float value) => value - MathF.Floor(value);

    /// <summary>Like <see cref="ArtAssets.DrawShade"/>, but darkest at the bottom edge.</summary>
    private void DrawShadeUp(SpriteBatch batch, Rectangle area, Color bottom) =>
        batch.Draw(_art.ShadeTexture, area, null, bottom, 0f, Vector2.Zero, SpriteEffects.FlipVertically, 0f);

    private static readonly (Vector2 At, Vector2 Size)[] ArenaBlotches =
    [
        (new Vector2(420f, 330f), new Vector2(260f, 150f)),
        (new Vector2(1310f, 300f), new Vector2(300f, 140f)),
        (new Vector2(760f, 760f), new Vector2(340f, 170f)),
        (new Vector2(1420f, 690f), new Vector2(240f, 160f)),
        (new Vector2(300f, 760f), new Vector2(220f, 150f))
    ];

    private static readonly Color SeaMist = new(150, 164, 200);
    private static readonly Color AshHaze = new(138, 128, 160);

    /// <summary>
    /// Mist and haze drifting low over each room (presentation only): sea mist over the water of
    /// the prologue, moving with the sea during the crossing; haze at the foot of the Warden wall;
    /// ash in the antechamber; smoke under the foundry's north wall.
    /// </summary>
    private void DrawGroundMist(SpriteBatch batch)
    {
        float t = _presentationTime;
        if (_phase is GamePhase.Antechamber or GamePhase.EnteringArena)
        {
            Atmosphere.Draw(batch, _art, -200f, 1700f, t, 31,
                new Atmosphere.Band(330f, 430f, 7, 6f, AshHaze, 0.09f, 380f, 50f),
                new Atmosphere.Band(450f, 860f, 6, 4f, AshHaze, 0.035f, 420f, 70f));
            return;
        }

        if (_phase == GamePhase.Arena && !_sandboxActive)
        {
            Atmosphere.Draw(batch, _art, -200f, 2000f, t, 37,
                new Atmosphere.Band(95f, 230f, 9, 7f, AshHaze, 0.13f, 400f, 60f),
                new Atmosphere.Band(260f, 980f, 6, 5f, AshHaze, 0.04f, 480f, 80f));
            return;
        }

        if (_phase != GamePhase.Prologue || PrologueEnvironment.PlateOf(_prologue) is not { } plate || !_art.HasArt(plate))
        {
            return;
        }

        switch (_prologue.Sector)
        {
            case PrologueSector.Escape when _prologue.IsVehicleRide:
                // The mist moves with the sea, so the crossing reads as speed.
                Atmosphere.Draw(batch, _art, -400f, 2200f, _prologue.StateTime, 41,
                    new Atmosphere.Band(120f, 320f, 10, -120f, SeaMist, 0.18f, 380f, 55f),
                    new Atmosphere.Band(730f, 1000f, 10, -150f, SeaMist, 0.2f, 380f, 55f));
                break;
            case PrologueSector.Threshold:
                Atmosphere.Draw(batch, _art, -200f, 2000f, t, 43,
                    new Atmosphere.Band(520f, 620f, 9, 5f, SeaMist, 0.16f, 400f, 55f),
                    new Atmosphere.Band(640f, 960f, 6, 4f, SeaMist, 0.05f, 480f, 75f));
                break;
            default:
                Atmosphere.Draw(batch, _art, -200f, 2000f, t, 47 + (int)_prologue.Sector,
                    new Atmosphere.Band(0f, 125f, 10, 9f, SeaMist, 0.22f, 380f, 55f),
                    new Atmosphere.Band(905f, 1010f, 10, 11f, SeaMist, 0.24f, 380f, 55f),
                    new Atmosphere.Band(160f, 860f, 7, 6f, SeaMist, 0.05f, 480f, 75f));
                break;
        }
    }

    /// <summary>Props of the room being shown plus props staged by automated tests.</summary>
    private IEnumerable<SceneProp> ActiveSceneProps => _phase switch
    {
        GamePhase.Antechamber or GamePhase.EnteringArena => _hubProps.Concat(_sceneProps),
        GamePhase.Arena => _arenaProps.Concat(_sceneProps),
        GamePhase.Prologue when _prologue.Sector == PrologueSector.Emergence => _shoreProps.Concat(_sceneProps),
        GamePhase.Prologue when _prologue.Sector == PrologueSector.Search && _art.HasArt(VisualIds.SearchFloor) => _searchProps.Concat(_sceneProps),
        GamePhase.Prologue when PrologueEnvironment.PlateOf(_prologue) is { } plate && _art.HasArt(plate) =>
            PropsOf(PrologueDirector.SectorProps(_prologue.Sector, _prologue.IsVehicleRide)).Concat(_sceneProps),
        _ => _sceneProps
    };

    private readonly List<ArtAssets.ShadowCaster> _shadowCasters = [];

    /// <summary>Cast shadows of the player and the enemies on the floor, under every figure.</summary>
    private void DrawFigureShadows(SpriteBatch batch, IReadOnlyList<SceneLight> lights, bool drawPlayer)
    {
        _shadowCasters.Clear();
        if (drawPlayer && _art.IsRendered(VisualIds.Player) && !_player.IsDead)
        {
            _shadowCasters.Add(new ArtAssets.ShadowCaster(_player, _player.Position, 1f));
        }
        if (IsCombatPhase)
        {
            foreach (Enemy enemy in _enemies)
            {
                if (enemy.DrawnAsFigure && enemy.VisualClip is not null && enemy is not TrainingDummy)
                {
                    // A dying figure's shadow fades with it.
                    _shadowCasters.Add(new ArtAssets.ShadowCaster(enemy, enemy.Position, enemy.IsAlive ? 1f : 0.6f));
                }
            }
        }
        (Vector2 direction, float strength) = KeyShadow;
        _art.DrawCastShadows(batch, _shadowCasters, lights, direction, strength);
    }

    /// <summary>
    /// The key light's shadow per area: where a point one unit above the floor lands (toward
    /// the lower right, like the shadows baked into the props) and how dark it is.
    /// </summary>
    private (Vector2 Direction, float Strength) KeyShadow => _phase switch
    {
        GamePhase.Antechamber or GamePhase.EnteringArena => (new Vector2(0.5f, 0.28f), 0.5f),
        GamePhase.Prologue => (new Vector2(0.62f, 0.32f), 0.55f),
        _ => (new Vector2(0.58f, 0.3f), 0.55f)
    };

    /// <summary>Enemies, the player and high props, drawn back to front by foot point.</summary>
    private void DrawActorBand(SpriteBatch batch, Texture2D pixel, bool drawPlayer)
    {
        _actorBand.Clear();
        int order = 0;
        if (IsCombatPhase)
        {
            foreach (Enemy enemy in _enemies)
            {
                Enemy current = enemy;
                _actorBand.Add(new DepthItem(enemy.Position.Y + enemy.Radius, order++, () =>
                {
                    _art.DrawEnemy(batch, current);
                    current.Draw(batch, pixel, _debugVisible, false, true);
                }));
            }
        }
        if (drawPlayer)
        {
            _actorBand.Add(new DepthItem(_player.Position.Y + GameBalance.PlayerRadius, order++, () => DrawPlayerActor(batch, pixel)));
        }
        foreach (SceneProp prop in ActiveSceneProps)
        {
            SceneProp current = prop;
            SceneLayer layer = _art.LayerOf(prop.VisualId, prop.FallbackLayer);
            if (layer is SceneLayer.Actor or SceneLayer.HighProp)
            {
                _actorBand.Add(new DepthItem(prop.Foot.Y, order++, () => _art.DrawProp(batch, current.VisualId, current.Foot, current.FallbackSize, current.Alpha)));
            }
        }

        DepthSort.Sort(_actorBand);
        foreach (DepthItem item in _actorBand)
        {
            item.Draw();
        }
    }

    private void DrawPlayerActor(SpriteBatch batch, Texture2D pixel)
    {
        bool rendered = _art.HasClip(VisualIds.Player, VisualClips.Aim);
        if (rendered)
        {
            // Contact shadow under the feet, cast toward the lower right (key light upper left).
            _art.DrawSoftSpot(batch, _player.Position + new Vector2(8f, 2f), new Vector2(38f, 13f), new Color(3, 3, 7) * 0.7f);
        }
        else
        {
            batch.FillCircle(pixel, _player.Position + new Vector2(3f, 8f), 24f, new Color(3, 3, 7) * 0.55f);
        }
        if (rendered && !_player.IsDead)
        {
            _player.Scythe.DrawBehindFigure(batch, _player.Position, _art);
        }
        _art.DrawPlayer(batch, _player);
        _player.Draw(batch, pixel, _art, _debugVisible, _soulSensePresentation.SoulEmergence);
        if (IsCombatPhase && _player.Cannon.State == SoulCannonState.Charging)
        {
            float charge = _player.Cannon.ChargeProgress;
            Vector2 muzzle = rendered
                ? FigureHeights.MuzzleOf(_player.Position, _player.FacingDirection, charge)
                : _player.Position + _player.FacingDirection * 74f;
            Color chargeColor = _player.Cannon.IsFullCharge
                ? Color.White
                : _player.Cannon.ChargeStage >= 3
                    ? new Color(238, 219, 255)
                    : _player.Cannon.ChargeStage == 2
                        ? GameBalance.DeathFlameBright
                        : new Color(155, 94, 220);
            _art.DrawLoopingEffect(
                batch,
                _player.Cannon,
                VisualIds.CannonChargeLoop,
                muzzle,
                0f,
                _player.Cannon.IsFullCharge ? 0.68f : MathHelper.Lerp(0.28f, 0.61f, charge),
                chargeColor);
        }
    }

    private void DrawSceneProps(SpriteBatch batch, Func<SceneLayer, bool> inBand)
    {
        foreach (SceneProp prop in ActiveSceneProps)
        {
            if (inBand(_art.LayerOf(prop.VisualId, prop.FallbackLayer)))
            {
                _art.DrawProp(batch, prop.VisualId, prop.Foot, prop.FallbackSize, prop.Alpha);
            }
        }
    }

    /// <summary>Fades occluders while they hide the player, an enemy or a telegraph.</summary>
    private void UpdateSceneProps(float deltaTime)
    {
        if (!ActiveSceneProps.Any())
        {
            return;
        }

        _occlusionTargets.Clear();
        _occlusionTargets.Add((RectangleF.Around(_player.Position - new Vector2(0f, 20f), new Vector2(60f, 110f)), _player.Position.Y + GameBalance.PlayerRadius));
        foreach (Enemy enemy in _enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }
            float size = enemy.Radius * 2.6f;
            _occlusionTargets.Add((RectangleF.Around(enemy.Position, new Vector2(size)), enemy.Position.Y + enemy.Radius));
            if (enemy.TelegraphRadius > 0f)
            {
                _occlusionTargets.Add((RectangleF.Around(enemy.Position, new Vector2(enemy.TelegraphRadius * 2f)), enemy.Position.Y + enemy.Radius));
            }
        }

        foreach (SceneProp prop in ActiveSceneProps)
        {
            SceneLayer layer = _art.LayerOf(prop.VisualId, prop.FallbackLayer);
            float target = layer is SceneLayer.HighProp or SceneLayer.Occluder or SceneLayer.Foreground
                ? OccluderFade.TargetAlpha(_art.PropBounds(prop.VisualId, prop.Foot, prop.FallbackSize), prop.Foot.Y, layer, _occlusionTargets)
                : 1f;
            prop.Alpha = OccluderFade.Approach(prop.Alpha, target, deltaTime);
        }
    }

    /// <summary>Test hook: stands an arena pillar (a dummy until it has graphics) on <paramref name="foot"/>.</summary>
    internal SceneProp PlaceAutomatedOccluder(Vector2 foot)
    {
        SceneProp pillar = new(VisualIds.ArenaPillar, foot, new Vector2(120f, 330f), SceneLayer.HighProp);
        _sceneProps.Add(pillar);
        return pillar;
    }

    private void DrawSoulfireLighting(SpriteBatch batch, SoulfireRenderer renderer, Viewport viewport)
    {
        if (_phase is GamePhase.Antechamber or GamePhase.EnteringArena)
        {
            SoulfireLighting.DrawAntechamber(
                batch,
                renderer,
                RenderResolution.ToOutput(_camera.GetTransform(viewport, _screenEffects.CameraOffset)),
                _player,
                _particles,
                _antechamber,
                _presentationTime,
                _soulSensePresentation.SoulEmergence,
                DoorTransitionProgress,
                renderedPlayer: _art.HasClip(VisualIds.Player, VisualClips.Aim));
            return;
        }

        SoulfireLighting.Draw(
            batch,
            renderer,
            RenderResolution.ToOutput(_camera.GetTransform(viewport, _screenEffects.CameraOffset)),
            _player,
            _enemies,
            _souls,
            _cannonShots,
            _particles,
            _arenaAtmosphere,
            _presentationTime,
            _soulSensePresentation.SoulEmergence,
            _phase == GamePhase.Arena && _loopState == ArenaLoopState.Complete,
            _presentation.GetLifeFlamePosition(),
            _presentation.GetLifeFlameAlpha() * _presentation.GetLifeFlameKindle() * _presentation.GetLifeFlameBreath(),
            drawArenaFurnaces: _phase != GamePhase.Prologue,
            renderedPlayer: _art.HasClip(VisualIds.Player, VisualClips.Aim),
            renderedEnemy: enemy => _art.IsRendered(enemy.VisualId));

        renderer.BeginLighting(batch, RenderResolution.ToOutput(_camera.GetTransform(viewport, _screenEffects.CameraOffset)));
        _groundImpacts.DrawLighting(batch, renderer);
        if (_automatedLightSource is not null)
        {
            DrawAutomatedLights(renderer, batch);
        }
        batch.End();
    }

    /// <summary>
    /// The Devourer's fists hit the floor: it breaks out to the edge of the blow, slabs and stones
    /// fly, the view drops with the weight (<see cref="GroundImpacts"/>). The hit itself is
    /// resolved by the Devourer and unchanged.
    /// </summary>
    private void PresentDevourerSlam(Devourer devourer)
    {
        Vector2 center = devourer.Position;
        float radius = GameBalance.DevourerSlamRange;
        _groundImpacts.Slam(devourer, center, radius);
        _audio.Play(AudioCue.GroundBreak, 0.82f, 0f, PanOf(center) * 0.6f);

        // Slabs from the crater and stones from all over the broken floor.
        _particles.EmitDebris(center + new Vector2(0f, FigureHeights.Air), center.Y + FigureHeights.Air, 12, new Color(78, 71, 86), 360f, 11f, 0.85f);
        for (int chip = 0; chip < 12; chip++)
        {
            float angle = chip * MathHelper.TwoPi / 12f + 0.4f;
            float reach = radius * (chip % 2 == 0 ? 0.45f : 0.8f);
            Vector2 at = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * reach;
            _particles.EmitDebris(at + new Vector2(0f, FigureHeights.Air), at.Y + FigureHeights.Air, 3, new Color(70, 64, 78), 210f, 4.5f, 0.6f);
        }

        // The weight lands in the view too, less the further away the player stands.
        float near = MathHelper.Clamp(1.2f - Vector2.Distance(_player.Position, center) / 900f, 0.35f, 1f);
        _screenEffects.AddShake(0.34f, 9f * near);
        _screenEffects.AddCameraKick(Vector2.UnitY, 7f * near);
        _screenEffects.AddZoomPunch(0.012f * near);
    }

    /// <summary>
    /// The bound soul running low: the world's edges close in, and on each throb they glow
    /// faintly with the Death Flame (<see cref="LowHealthPresentation"/>).
    /// </summary>
    private void DrawLowHealth(SpriteBatch batch, SoulfireRenderer renderer)
    {
        float amount = _lowHealth.Amount;
        if (amount <= 0.001f)
        {
            return;
        }

        float pulse = _lowHealth.Pulse;
        Color glow = GameBalance.DeathFlame;
        glow.A = 0;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
        batch.Draw(renderer.VignetteTexture, RenderResolution.OutputBounds, Color.White * (amount * (0.3f + 0.16f * pulse)));
        batch.Draw(renderer.VignetteGlowTexture, RenderResolution.OutputBounds, glow * (amount * (0.05f + 0.16f * pulse)));
        batch.End();
    }

    /// <summary>
    /// Impact frames darken the picture toward its edges for an instant (the middle, where the
    /// blow lands, stays readable); flashes burst as light from where they happened and only
    /// lightly wash the rest.
    /// </summary>
    private void DrawScreenFeedback(SpriteBatch batch, Texture2D pixel, Viewport viewport, SoulfireRenderer renderer)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, transformMatrix: RenderResolution.ScaleMatrix);
        Rectangle screen = new(0, 0, viewport.Width, viewport.Height);

        if (_screenEffects.ImpactFrameAlpha > 0f)
        {
            float impact = _screenEffects.ImpactFrameAlpha;
            batch.FillRectangle(pixel, screen, Color.Black * (impact * 0.32f));
            batch.Draw(renderer.VignetteTexture, screen, Color.White * MathHelper.Clamp(impact * 1.6f, 0f, 1f));
        }

        float flash = _screenEffects.FlashAlpha;
        if (flash > 0f && _screenEffects.FlashCenter is null)
        {
            batch.FillRectangle(pixel, screen, GameBalance.DeathFlameBright * flash);
        }

        if (_player.ResonanceActivationRemaining > 0f)
        {
            float activationFade = MathHelper.Clamp(_player.ResonanceActivationRemaining / 0.5f, 0f, 1f);
            batch.FillRectangle(pixel, screen, Color.Black * (activationFade * 0.38f));
        }

        batch.End();

        if (flash > 0f && _screenEffects.FlashCenter is { } center)
        {
            Vector2 at = Vector2.Transform(center, _camera.GetTransform(viewport, _screenEffects.CameraOffset));
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, transformMatrix: RenderResolution.ScaleMatrix);
            batch.FillRectangle(pixel, screen, GameBalance.DeathFlameBright * (flash * 0.3f));
            batch.End();
            batch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, transformMatrix: RenderResolution.ScaleMatrix);
            _art.DrawSoftSpot(batch, at, new Vector2(viewport.Height * 0.62f), GameBalance.DeathFlameBright * (flash * 0.85f));
            _art.DrawSoftSpot(batch, at, new Vector2(viewport.Height * 0.2f), GameBalance.SoulWhite * (flash * 0.7f));
            batch.End();
        }
    }

    private void DrawHud(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, transformMatrix: RenderResolution.ScaleMatrix);

        if (_phase == GamePhase.Prologue)
        {
            bool showPrologueHud = !_player.IsDead && _loopState == ArenaLoopState.Combat &&
                _prologue.Stage is not (PrologueStage.Arrival or PrologueStage.Complete);
            if (showPrologueHud)
            {
                _hud.Draw(batch, pixel, viewport, _player);
            }

            if (!IsGamePaused)
            {
                ProloguePresentation.DrawOverlay(batch, pixel, viewport, _prologue, _player.IsDead, _settings.OptionalHints, _presentation.StateTime);
            }
        }
        else
        {
            if (_phase == GamePhase.Arena && _sandboxActive && !_player.IsDead)
            {
                _hud.Draw(batch, pixel, viewport, _player);
                DrawSandboxHud(batch, pixel, viewport);
            }
            else if (_phase == GamePhase.Arena && _presentation.ShouldDrawCombatHud(_loopState, _player.IsDead))
            {
                _hud.Draw(batch, pixel, viewport, _player);
                DrawCurrencyHud(batch, pixel, viewport);
                if (_waveNumber > 0)
                {
                    HudRenderer.DrawWave(batch, pixel, viewport, _waveNumber, GameBalance.ArenaWaveCount, _waveRun.PushesReleased, ArenaWaves.Pushes(_waveNumber).Count);
                }
            }

            if (!IsGamePaused)
            {
                _presentation.DrawOverlay(batch, pixel, viewport, _phase, _loopState, _player.IsDead, _waveNumber, _menu);
                if (_phase == GamePhase.Arena && _loopState == ArenaLoopState.Complete && !_player.IsDead)
                {
                    float reveal = MathHelper.Clamp((_presentation.StateTime - 0.8f) / 0.6f, 0f, 1f);
                    DrawSecuredSummary(batch, pixel, viewport, "GESICHERT", _lastSecured.Geld, _lastSecured.Glut, reveal);
                }
            }
        }

        DrawAbilityHud(batch, pixel, viewport);

        // Story, prompt and cinematic text would compete with the pause menu's type;
        // the paused world and HUD stay visible under the veil.
        if (_phase is GamePhase.Antechamber or GamePhase.EnteringArena && !IsGamePaused)
        {
            DrawAntechamberOverlay(batch, pixel, viewport);
        }

        if (_debugVisible && _phase != GamePhase.Title)
        {
            DrawDebugOverlay(batch, pixel, viewport);
        }

        if (_pauseMenu.IsOpen)
        {
            _presentation.DrawPauseMenu(batch, pixel, viewport, _pauseMenu);
        }

        if (_characterMenu.IsOpen)
        {
            _presentation.DrawCharacterMenu(batch, pixel, viewport, _characterMenu, CurrentCharacterSheet, CurrentAbilityCards(), AbilityChoicePhase);
        }

        if (_devMenu.IsOpen)
        {
            DrawDevMenu(batch, pixel, viewport);
        }

        batch.End();
    }

    private void DrawAntechamberOverlay(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        float centerX = viewport.Width * 0.5f;
        float placeIn = Ease(_phaseTime / 0.45f);
        float placeOut = 1f - Ease((_phaseTime - 2.2f) / 0.55f);
        float placeAlpha = _phase == GamePhase.Antechamber ? placeIn * placeOut : 0f;
        if (placeAlpha > 0f)
        {
            UiKit.Divider(batch, pixel, centerX, viewport.Height * 0.14f - 18f, 420f, GameBalance.DeathFlameBright * (0.55f * placeAlpha));
            PixelText.DrawCentered(batch, pixel, "ASHEN ANTECHAMBER", centerX, viewport.Height * 0.14f, 4, GameBalance.SoulWhite * (0.85f * placeAlpha));
        }

        if (_phase == GamePhase.Antechamber)
        {
            DrawSecuredSummary(batch, pixel, viewport, "GESICHERT", _wallet.Secured(Currency.Geld), _wallet.Secured(Currency.Glut), 1f);
        }

        HubDoor? nearbyDoor = _phase == GamePhase.Antechamber ? _antechamber.DoorAt(_player.Position) : null;
        if (nearbyDoor is not null)
        {
            float pulse = nearbyDoor.IsSealed ? 0f : 0.5f + MathF.Sin(_presentationTime * 4f) * 0.5f;
            Color accent = nearbyDoor.IsSealed ? GameBalance.DeepViolet : GameBalance.DeathFlame;
            UiKit.Prompt(batch, pixel, centerX, viewport.Height - 206, nearbyDoor.Prompt, accent, pulse, nearbyDoor.IsSealed ? 0.72f : 1f);
        }

        if (_phase != GamePhase.EnteringArena)
        {
            return;
        }

        float progress = DoorTransitionProgress;
        int barHeight = (int)MathHelper.Lerp(18f, 58f, Ease(progress));
        batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, barHeight), Color.Black * 0.9f);
        batch.FillRectangle(pixel, new Rectangle(0, viewport.Height - barHeight, viewport.Width, barHeight), Color.Black * 0.9f);
        float labelAlpha = Ease(progress / 0.35f) * (1f - Ease((progress - 0.68f) / 0.25f));
        UiKit.Divider(batch, pixel, centerX, viewport.Height * 0.78f - 16f, 380f, GameBalance.DeathFlameBright * (0.5f * labelAlpha));
        PixelText.DrawCentered(batch, pixel, "THE DOOR AWAKENS", centerX, viewport.Height * 0.78f, 3, GameBalance.DeathFlameBright * labelAlpha);
        float fade = Ease((progress - 0.72f) / 0.28f);
        batch.FillRectangle(pixel, viewport.Bounds, Color.Black * fade);
    }

    private void ResolveScytheStrike()
    {
        if (!_player.Scythe.TryConsumeStrike(out ScytheStrike strike))
        {
            return;
        }

        bool hitAnything = false;
        Vector2 firstContact = _player.Position + strike.Direction * 60f;
        foreach (Enemy enemy in _enemies.Where(enemy => enemy.IsAlive))
        {
            Vector2 toTarget = enemy.Position - _player.Position;
            float combinedRange = strike.Range + enemy.Radius;
            if (toTarget.LengthSquared() > combinedRange * combinedRange)
            {
                continue;
            }

            Vector2 targetDirection = toTarget.LengthSquared() > 0.001f ? Vector2.Normalize(toTarget) : strike.Direction;
            if (Vector2.Dot(strike.Direction, targetDirection) < MathF.Cos(strike.ArcRadians * 0.5f))
            {
                continue;
            }

            Vector2 weakPoint = FindStrikeWeakPoint(enemy, strike);
            bool coreHit = _player.SoulSenseActive && weakPoint != Vector2.Zero;
            int damage = coreHit
                ? (int)MathF.Round(strike.Damage * GameBalance.SoulSenseCoreDamageMultiplier)
                : strike.Damage;
            ApplyWeaponDamage(enemy, new DamageInfo(
                damage,
                targetDirection * strike.Knockback,
                coreHit ? weakPoint : enemy.Position,
                coreHit));
            Vector2 contactPosition = coreHit
                ? weakPoint
                : enemy.Position - targetDirection * enemy.Radius * 0.35f;
            _combatPresentation.SpawnScytheContact(
                strike.Step,
                contactPosition,
                targetDirection,
                coreHit);
            if (!hitAnything)
            {
                firstContact = contactPosition;
            }
            PlayMaterialHit(enemy, strike.Step switch { 1 => 0.5f, 2 => 0.62f, _ => 0.78f }, strike.Step == 3 ? -0.06f : 0f);
            if (coreHit)
            {
                _automatedCoreHits++;
            }
            if (coreHit)
            {
                _player.AddResonance(GameBalance.ResonancePerCoreHit);
                _audio.Play(AudioCue.CoreHit, 0.7f);
            }
            hitAnything = true;
        }

        if (!hitAnything)
        {
            return;
        }

        _combatPresentation.PresentScytheImpact(strike.Step, strike.Direction, firstContact);
        // Every landed blow has body: the pressure stroke under the Soul Cleave, a lighter one under
        // the first two swings.
        _audio.Play(AudioCue.HitHeavy, strike.Step switch { 1 => 0.32f, 2 => 0.42f, _ => 0.62f }, 0f, PanOf(firstContact) * 0.5f);
        // A landed hit sits above the swing that carried it.
        _audio.Play(AudioCue.ScytheHit, strike.Step == 3 ? 0.95f : 0.82f, strike.Step == 2 ? 0.08f : 0f);
    }

    private void SpawnWave(int waveNumber)
    {
        Vector2 center = _arena.CombatBounds.Center.ToVector2();
        _waveRun = new ArenaWaveRun(ArenaWaves.Pushes(waveNumber));
        _pendingSpawns.Clear();
        _reinforcementSeed = 0;
        ArenaPush firstPush = _waveRun.TakeFirst();
        int seed = waveNumber * 10;
        if (ArenaWaves.HasAuthoredLayout(waveNumber))
        {
            foreach ((ArenaEnemyKind kind, Vector2 offset) in ArenaWaves.AuthoredLayout(waveNumber))
            {
                _enemies.Add(CreateArenaEnemy(kind, center + offset, ref seed));
            }
        }
        else
        {
            List<Vector2> positions = ArenaWaves.ChooseSpawnPositions(_arena.CombatBounds, _player.Position, firstPush.Total);
            int index = 0;
            foreach (ArenaEnemyKind kind in firstPush.Kinds())
            {
                _enemies.Add(CreateArenaEnemy(kind, positions[index++], ref seed));
            }
        }

        _waveNumber = waveNumber;
        _loopState = ArenaLoopState.Combat;
        _burningHandoffTimer = 0f;
        _burningCommittedLastFrame = 0;
        _particles.EmitDeathFlame(center, 18 + waveNumber * 5, 1f + waveNumber * 0.12f);
        _screenEffects.AddShake(0.16f, 4f + waveNumber);
        _screenEffects.Flash(0.08f, 0.12f + waveNumber * 0.035f);
        _audio.SetCalm(false);
        _audio.Play(AudioCue.WaveStart, 0.62f, MathF.Min(0.18f, waveNumber * 0.03f));
    }

    private static Enemy CreateArenaEnemy(ArenaEnemyKind kind, Vector2 position, ref int seed) => kind switch
    {
        ArenaEnemyKind.Hollow => new Hollow(position, ++seed),
        ArenaEnemyKind.Burning => new Burning(position, ++seed),
        _ => new Devourer(position)
    };

    private void UpdateReinforcements(float deltaTime)
    {
        int alive = _enemies.Count(enemy => enemy.IsAlive) + _pendingSpawns.Count;
        if (_waveRun.TryTakeNext(deltaTime, alive, out ArenaPush push))
        {
            List<Vector2> positions = ArenaWaves.ChooseSpawnPositions(_arena.CombatBounds, _player.Position, push.Total);
            int index = 0;
            foreach (ArenaEnemyKind kind in push.Kinds())
            {
                _pendingSpawns.Add(new PendingArenaSpawn(kind, positions[index++], GameBalance.ArenaSpawnTelegraphDuration));
            }

            _audio.Play(AudioCue.WaveStart, 0.34f, 0.12f);
        }

        for (int i = _pendingSpawns.Count - 1; i >= 0; i--)
        {
            PendingArenaSpawn spawn = _pendingSpawns[i];
            spawn.Remaining -= deltaTime;
            if (spawn.Remaining > 0f)
            {
                continue;
            }

            _pendingSpawns.RemoveAt(i);
            int seed = _waveNumber * 100 + _reinforcementSeed++;
            _enemies.Add(CreateArenaEnemy(spawn.Kind, spawn.Position, ref seed));
            _particles.EmitDeathFlame(spawn.Position, 10, 0.7f);
            // Death Flame gathers and lets a figure go: heard where it stands.
            _audio.Play(AudioCue.EnemyEmerge, 0.6f, 0f, PanOf(spawn.Position));
        }
    }

    private sealed class PendingArenaSpawn(ArenaEnemyKind kind, Vector2 position, float remaining)
    {
        public ArenaEnemyKind Kind { get; } = kind;
        public Vector2 Position { get; } = position;
        public float Remaining { get; set; } = remaining;
    }

    private void StartNewGame(Viewport viewport)
    {
        if (_skipPrologue)
        {
            BeginAntechamber(viewport);
        }
        else
        {
            BeginPrologue(viewport);
        }
    }

    private void RetryCurrentEncounter(Viewport viewport)
    {
        if (_phase == GamePhase.Prologue)
        {
            RestartPrologueSector(viewport);
        }
        else if (_sandboxActive)
        {
            ResetSandbox();
        }
        else
        {
            ResetEncounter();
        }
    }

    private void UpdateLoop(float deltaTime)
    {
        if (_phase == GamePhase.Prologue)
        {
            UpdatePrologueFlow(deltaTime);
        }
        else
        {
            UpdateArenaLoop(deltaTime);
        }
    }

    private void BeginAntechamber(Viewport viewport)
    {
        ClearRunState();
        _phase = GameFlowRules.FinishPrologue(GameFlowRules.ConfirmTitle(_phase, skipPrologue: true));
        _phaseTime = 0f;
        _player.Reset(_antechamber.PlayerSpawn);
        _lastMouseWorld = _player.Position + Vector2.UnitX * 200f;
        _camera.Zoom = 1f;
        _camera.Follow(_player.Position + new Vector2(115f, -22f), _antechamber.Bounds, viewport, 1f);
        _audio.SetCalm(true);
        _audio.SetSoulSense(false);
        _audio.SetArenaActive(false, true);
    }

    private void UpdateAntechamber(
        float deltaTime,
        InputState input,
        Viewport viewport,
        bool wasDashing,
        bool wasSoulSenseActive)
    {
        _lastMouseWorld = AutomatedAimOr(_camera.ScreenToWorld(input.MousePosition, viewport));
        _player.Update(
            deltaTime,
            input,
            _lastMouseWorld,
            _antechamber.MovementBounds,
            _particles,
            _screenEffects,
            _forceSoulSense,
            combatEnabled: false);
        UpdateFootsteps();

        _soulSensePresentation.Update(deltaTime, _player.SoulSenseActive);
        _particles.Update(deltaTime);

        if (!wasDashing && _player.IsDashing)
        {
            _spriteVfx.Spawn(
                VisualIds.DashIgnition,
                _player.Position - _player.DashDirection * 24f,
                MathF.Atan2(_player.DashDirection.Y, _player.DashDirection.X),
                0.72f);
            _audio.Play(AudioCue.Dash, 0.5f);
        }

        if (wasSoulSenseActive != _player.SoulSenseActive)
        {
            _audio.Play(_player.SoulSenseActive ? AudioCue.SoulSenseOn : AudioCue.SoulSenseOff, 0.42f);
            _audio.SetSoulSense(_player.SoulSenseActive);
        }

        float smoothing = 1f - MathF.Exp(-deltaTime * 6.5f);
        // The hub frames the whole door wall: slightly wider than a combat room and held at the
        // height of the doors, following the player mostly sideways.
        _camera.Zoom = MathHelper.Lerp(_camera.Zoom, HubCameraZoom, smoothing);
        Vector2 target = new(MathHelper.Lerp(_player.Position.X, _antechamber.EntryDoorCenter.X, 0.11f),
            MathHelper.Lerp(HubCameraHeight, _player.Position.Y, 0.15f));
        _camera.Follow(target, _antechamber.Bounds, viewport, smoothing);

        HubDoor? door = _antechamber.DoorAt(_player.Position);
        if (door is { IsSealed: false } && input.WasKeyPressed(Keys.E))
        {
            BeginDoorTransition();
        }
    }

    /// <summary>World units per step: half the run cycle of the rendered figure (move clip, 180 per cycle).</summary>
    private const float FootstepStride = 90f;
    private Vector2 _footstepFrom;
    private float _footstepDistance;

    /// <summary>
    /// Footsteps from the distance the player actually covers (never during a dash or while dead);
    /// on the skiff's deck the planks answer.
    /// </summary>
    private float? _playerStepPhase;
    private readonly Dictionary<Enemy, float> _enemyStepPhases = new();
    private static readonly float[] Footfalls = [0.25f, 0.75f];
    private static readonly float[] BurningFootfalls = [0.2f, 0.7f];

    /// <summary>
    /// True when a walk or run cycle passed one of its footfalls since the last check: the
    /// rendered clips set a heel down at a quarter and three quarters of their cycle
    /// (key_run/key_move in tools/visuals/blender/build_*.py; the Burning slightly earlier).
    /// </summary>
    private static bool CrossedFootfall(float? previous, float current, float[] footfalls)
    {
        if (previous is not { } before)
        {
            return false;
        }

        float travelled = current - before;
        if (travelled < 0f)
        {
            travelled += 1f;
        }
        if (travelled <= 0f || travelled > 0.5f)
        {
            return false;
        }

        foreach (float contact in footfalls)
        {
            float ahead = contact - before;
            if (ahead <= 0f)
            {
                ahead += 1f;
            }
            if (ahead <= travelled)
            {
                return true;
            }
        }
        return false;
    }

    private void UpdateFootsteps()
    {
        Vector2 position = _player.Position;
        float moved = Vector2.Distance(position, _footstepFrom);
        _footstepFrom = position;
        bool wood = _phase == GamePhase.Prologue && _prologue.IsVehicleRide;
        if (!_player.IsDead && !_player.IsDashing && _art.CyclePhase(_player, VisualClips.Move) is { } phase)
        {
            // The rendered run: a step on every drawn footfall.
            if (CrossedFootfall(_playerStepPhase, phase, Footfalls))
            {
                _audio.Play(wood ? AudioCue.FootstepWood : AudioCue.Footstep, 0.42f);
            }
            _playerStepPhase = phase;
            return;
        }

        _playerStepPhase = null;
        if (_player.IsDead || _player.IsDashing || moved > 60f || _player.Velocity.LengthSquared() < 120f)
        {
            _footstepDistance = MathF.Min(_footstepDistance, FootstepStride * 0.6f);
            return;
        }
        _footstepDistance += moved;
        if (_footstepDistance >= FootstepStride)
        {
            _footstepDistance -= FootstepStride;
            _audio.Play(wood ? AudioCue.FootstepWood : AudioCue.Footstep, 0.42f);
        }
    }

    /// <summary>Stereo position of a sound source left or right of the player (presentation only).</summary>
    private float PanOf(Vector2 source) => MathHelper.Clamp((source.X - _player.Position.X) / 700f, -0.8f, 0.8f);

    /// <summary>
    /// Each enemy kind is heard where the nearest of its kind stands (presentation only): louder
    /// the closer it is, a little fuller with more of them, a Burning flaring up as it winds up and
    /// runs. Nothing while the player is dead or outside a fight.
    /// </summary>
    private void UpdateEnemyPresence(float deltaTime)
    {
        bool heard = IsCombatPhase && !_player.IsDead;
        foreach (PresenceSource kind in PresenceKinds)
        {
            Enemy? nearest = null;
            float best = float.MaxValue;
            int count = 0;
            if (heard)
            {
                foreach (Enemy enemy in _enemies)
                {
                    if (!enemy.IsAlive || PresenceOf(enemy) != kind)
                    {
                        continue;
                    }
                    count++;
                    float distance = Vector2.DistanceSquared(enemy.Position, _player.Position);
                    if (distance < best)
                    {
                        best = distance;
                        nearest = enemy;
                    }
                }
            }
            float level = 0f;
            if (nearest is not null)
            {
                float nearness = MathHelper.Clamp(1f - MathF.Sqrt(best) / 850f, 0f, 1f);
                level = nearness * nearness * MathF.Min(1.3f, 1f + 0.1f * (count - 1));
                if (nearest is Burning { IsAggressionCommitted: true })
                {
                    level *= 1.5f;
                }
            }
            _audio.SetPresence(kind, MathHelper.Clamp(level, 0f, 1.5f), nearest is null ? 0f : PanOf(nearest.Position), deltaTime);
        }
    }

    /// <summary>
    /// The Warden flames are heard where they burn (presentation only): each louder the closer the
    /// player walks by, the hall's sconces high on the pilasters fainter, larger flames of the
    /// prologue fuller, the sum panned toward the nearer flames.
    /// </summary>
    private void UpdateWardenFlames(float deltaTime)
    {
        _flameLevel = 0f;
        _flamePan = 0f;
        if (_phase is GamePhase.Antechamber or GamePhase.EnteringArena)
        {
            foreach ((Vector2 flame, float weight) in SoulFurnaceAntechamber.HeardFlames)
            {
                HearFlame(flame, weight);
            }
        }
        else if (_phase == GamePhase.Prologue && PrologueEnvironment.PlateOf(_prologue) is { } dressed && _art.HasArt(dressed))
        {
            foreach ((Vector2 flameBase, float height) in PrologueDirector.WardenFlames(_prologue.Sector, _prologue.IsVehicleRide))
            {
                HearFlame(flameBase, MathHelper.Clamp(height / 40f, 0.3f, 1.2f));
            }
        }
        _audio.SetPresence(PresenceSource.WardenFlames, MathHelper.Clamp(_flameLevel, 0f, 1.2f),
            _flameLevel > 0.001f ? _flamePan / _flameLevel : 0f, deltaTime);

        // The furnace in the north wall of the foundry, louder toward the wall; in the ending the
        // Life Flame burns there instead.
        float furnace = 0f;
        if (_phase == GamePhase.Arena && _loopState != ArenaLoopState.Complete)
        {
            float nearness = MathHelper.Clamp(1f - Vector2.Distance(Arena.FurnaceHearth, _player.Position) / 1000f, 0f, 1f);
            furnace = 0.15f + 0.85f * nearness * nearness;
        }
        _audio.SetPresence(PresenceSource.Furnace, furnace, PanOf(Arena.FurnaceHearth) * 0.8f, deltaTime);
    }

    private float _flameLevel;
    private float _flamePan;

    private void HearFlame(Vector2 flame, float weight)
    {
        float nearness = MathHelper.Clamp(1f - Vector2.Distance(flame, _player.Position) / 620f, 0f, 1f);
        float share = weight * nearness * nearness;
        _flameLevel += share;
        _flamePan += share * PanOf(flame);
    }

    private static readonly PresenceSource[] PresenceKinds = [PresenceSource.Hollow, PresenceSource.Burning, PresenceSource.Devourer];

    private static PresenceSource? PresenceOf(Enemy enemy) => enemy switch
    {
        Hollow => PresenceSource.Hollow,
        Burning => PresenceSource.Burning,
        Devourer => PresenceSource.Devourer,
        _ => null
    };

    private readonly Dictionary<Enemy, (Vector2 From, float Distance)> _enemySteps = new();
    private readonly List<Enemy> _goneStepEnemies = [];

    /// <summary>
    /// Enemies are heard walking (presentation only): each kind has its own step every stride of
    /// ground covered, quieter with distance from the player and placed left or right of them.
    /// </summary>
    private void UpdateEnemyFootsteps()
    {
        if (!IsCombatPhase)
        {
            _enemySteps.Clear();
            _enemyStepPhases.Clear();
            return;
        }

        foreach (Enemy enemy in _enemies)
        {
            (AudioCue cue, float stride, float loudness) = enemy switch
            {
                Devourer => (AudioCue.DevourerStep, 105f, 0.5f),
                Burning => (AudioCue.BurningStep, 70f, 0.4f),
                Hollow => (AudioCue.HollowStep, 72f, 0.4f),
                _ => (AudioCue.Footstep, 0f, 0f)
            };
            if (stride <= 0f || !enemy.IsAlive)
            {
                continue;
            }

            if (enemy.DrawnAsFigure && _art.CyclePhase(enemy, VisualClips.Move) is { } enemyPhase)
            {
                // A rendered walk: the step lands on the drawn footfall.
                float? lastPhase = _enemyStepPhases.TryGetValue(enemy, out float last) ? last : null;
                _enemyStepPhases[enemy] = enemyPhase;
                if (CrossedFootfall(lastPhase, enemyPhase, enemy is Burning ? BurningFootfalls : Footfalls))
                {
                    float nearness = MathHelper.Clamp(1f - Vector2.Distance(enemy.Position, _player.Position) / 900f, 0f, 1f);
                    if (nearness > 0.05f)
                    {
                        _audio.Play(cue, loudness * nearness * nearness, 0f, PanOf(enemy.Position));
                    }
                }
                continue;
            }
            _enemyStepPhases.Remove(enemy);

            if (!_enemySteps.TryGetValue(enemy, out (Vector2 From, float Distance) step))
            {
                _enemySteps[enemy] = (enemy.Position, stride * 0.5f);
                continue;
            }

            float moved = Vector2.Distance(enemy.Position, step.From);
            float distance = moved > 40f ? stride * 0.5f : step.Distance + moved;
            if (distance >= stride)
            {
                distance -= stride;
                Vector2 offset = enemy.Position - _player.Position;
                float near = MathHelper.Clamp(1f - offset.Length() / 900f, 0f, 1f);
                if (near > 0.05f)
                {
                    _audio.Play(cue, loudness * near * near, 0f, PanOf(enemy.Position));
                }
            }
            _enemySteps[enemy] = (enemy.Position, distance);
        }

        _goneStepEnemies.Clear();
        foreach (Enemy known in _enemySteps.Keys)
        {
            if (!known.IsAlive || !_enemies.Contains(known))
            {
                _goneStepEnemies.Add(known);
            }
        }
        foreach (Enemy known in _enemyStepPhases.Keys)
        {
            if (!known.IsAlive || !_enemies.Contains(known))
            {
                _goneStepEnemies.Add(known);
            }
        }
        foreach (Enemy gone in _goneStepEnemies)
        {
            _enemySteps.Remove(gone);
            _enemyStepPhases.Remove(gone);
        }
    }

    private const float HubCameraZoom = 0.88f;
    private const float HubCameraHeight = 470f;

    private void BeginDoorTransition()
    {
        _phase = GameFlowRules.EnterDoor(_phase);
        _phaseTime = 0f;
        _player.SettleForCompletion();
        _soulSensePresentation.Reset();
        _audio.SetSoulSense(false);
        _audio.SetCalm(false);
        _audio.SetArenaActive(true);
        _audio.Play(AudioCue.DoorAwaken, 0.85f);
    }

    private void UpdateDoorTransition(float deltaTime, Viewport viewport)
    {
        _soulSensePresentation.Update(deltaTime, false);
        _particles.Update(deltaTime);

        float eased = Ease(DoorTransitionProgress);
        float smoothing = 1f - MathF.Exp(-deltaTime * 7f);
        _camera.Zoom = MathHelper.Lerp(_camera.Zoom, MathHelper.Lerp(HubCameraZoom, 0.8f, eased), smoothing);
        _camera.Follow(
            Vector2.Lerp(_player.Position, _antechamber.EntryDoorCenter + new Vector2(0f, 90f), eased),
            _antechamber.Bounds,
            viewport,
            smoothing);

        if (_phaseTime >= DoorTransitionDuration)
        {
            EnterArena(viewport);
        }
    }

    private void EnterArena(Viewport viewport)
    {
        ClearRunState();
        _phase = GameFlowRules.FinishDoorTransition(_phase);
        BeginArenaIntro(viewport);
    }

    private void BeginArenaIntro(Viewport viewport)
    {
        _phaseTime = 0f;
        _loopState = ArenaLoopState.Intro;
        BeginCurrencyRun();
        _player.Reset(_arena.CombatBounds.Center.ToVector2());
        _lastMouseWorld = _player.Position + Vector2.UnitX * 200f;
        _camera.Zoom = 0.9f;
        _camera.Follow(_arena.CombatBounds.Center.ToVector2(), _arena.Bounds, viewport, 1f);
        _presentation.BeginIntro(false);
        _audio.SetCalm(false);
        _audio.SetSoulSense(false);
        _audio.SetArenaActive(true);
    }

    private void ResetEncounter()
    {
        ClearRunState();
        _phase = GameFlowRules.RetryAfterDeath();
        _phaseTime = 0f;
        BeginCurrencyRun();
        _player.Reset(_arena.CombatBounds.Center.ToVector2());
        _loopState = ArenaLoopState.Intro;
        _presentation.BeginIntro(true);
        _audio.SetCalm(false);
        _audio.SetSoulSense(false);
        _audio.SetArenaActive(true);
    }

    private void ResetFullRun(Viewport viewport)
    {
        ClearRunState();
        _phase = GameFlowRules.RestartAfterCompletion();
        _phaseTime = 0f;
        _loopState = ArenaLoopState.Intro;
        _player.Reset(_arena.CombatBounds.Center.ToVector2());
        _camera.Zoom = 0.9f;
        _camera.Follow(_arena.CombatBounds.Center.ToVector2(), _arena.Bounds, viewport, 1f);
        _presentation.ResetTitle();
        _audio.SetCalm(true);
        _audio.SetSoulSense(false);
        _audio.SetArenaActive(false, true);
    }

    /// <param name="stayInSandbox">Keeps the sandbox (and its character values) for a reset inside it.</param>
    private void ClearRunState(bool stayInSandbox = false)
    {
        _abilities.Clear(_player);
        _enemies.Clear();
        _souls.Clear();
        _cannonShots.Clear();
        _chests.Clear();
        _glutSparks.Clear();
        _openedChests.Clear();
        _particles.Clear();
        _spriteVfx.Clear();
        _groundImpacts.Clear();
        _art.ClearTransient();
        _combatPresentation.Clear();
        _screenEffects.Clear();
        _arenaAtmosphere.Reset();
        _waveNumber = 0;
        _waveRun = ArenaWaveRun.Empty;
        _pendingSpawns.Clear();
        _burningHandoffTimer = 0f;
        _burningCommittedLastFrame = 0;
        _forceSoulSense = false;
        _soulSensePresentation.Reset();
        _audioTestFatalDamageRequested = false;
        _endingRevealPlayed = false;
        if (_sandboxActive && !stayInSandbox)
        {
            RestoreSandboxStartValues();
        }
        _sandboxActive = stayInSandbox;
    }

    private void ConfigureBurningAggression(float deltaTime)
    {
        _burningHandoffTimer = MathF.Max(0f, _burningHandoffTimer - deltaTime);
        List<Burning> burnings = _enemies
            .OfType<Burning>()
            .Where(burning => burning.IsAlive)
            .ToList();

        foreach (Burning burning in burnings)
        {
            burning.SetAggressionSlot(false);
        }

        int maximumCommitments = _waveNumber >= 4 ? 2 : 1;
        int committed = burnings.Count(burning => burning.IsAggressionCommitted);
        if (_burningHandoffTimer > 0f || committed >= maximumCommitments)
        {
            return;
        }

        foreach (Burning burning in burnings
            .Where(burning => burning.State == BurningState.Approach)
            .OrderBy(burning => Vector2.DistanceSquared(burning.Position, _player.Position))
            .Take(maximumCommitments - committed))
        {
            burning.SetAggressionSlot(true);
        }
    }

    private void UpdateBurningHandoff()
    {
        int committed = _enemies
            .OfType<Burning>()
            .Count(burning => burning.IsAlive && burning.IsAggressionCommitted);
        if (committed < _burningCommittedLastFrame)
        {
            _burningHandoffTimer = GameBalance.BurningAggressionHandoffDelay;
        }

        _burningCommittedLastFrame = committed;
    }

    private void UpdateArenaLoop(float deltaTime)
    {
        if (_sandboxActive)
        {
            return;
        }

        switch (_loopState)
        {
            case ArenaLoopState.Intro:
                if (_presentation.TransitionComplete)
                {
                    SpawnWave(_waveNumber + 1);
                }
                break;

            case ArenaLoopState.Transition:
                if (_presentation.WaveTransitionComplete)
                {
                    SpawnWave(_waveNumber + 1);
                }
                break;

            case ArenaLoopState.Combat:
                UpdateReinforcements(deltaTime);
                if (_enemies.Count == 0 && _souls.Count == 0 && _waveRun.AllPushesReleased && _pendingSpawns.Count == 0)
                {
                    bool lastWave = _waveNumber >= GameBalance.ArenaWaveCount;
                    _audio.Play(AudioCue.WaveClear, lastWave ? 0.74f : 0.62f);
                    if (lastWave)
                    {
                        _loopState = ArenaLoopState.Complete;
                        SecureRunCurrencies();
                        _player.SettleForCompletion();
                        _cannonShots.Clear();
                        _presentation.BeginCompletion();
                        _endingRevealPlayed = false;
                        _audio.SetCalm(true);
                        _audio.SetSoulSense(false);
                    }
                    else
                    {
                        _loopState = ArenaLoopState.Intermission;
                        SpawnChestAfterWave(_waveNumber);
                        _particles.EmitDeathFlame(_arena.CombatBounds.Center.ToVector2(), 12, 0.8f);
                    }
                }
                break;
        }
    }

    private void DrawArenaLoop(SpriteBatch batch, Texture2D pixel)
    {
        if (_loopState != ArenaLoopState.Complete)
        {
            Rectangle gate = new(_arena.CombatBounds.Center.X - 92, _arena.CombatBounds.Bottom - 14, 184, 20);
            batch.FillRectangle(pixel, gate, new Color(24, 22, 30));
            batch.DrawRectangle(pixel, gate, GameBalance.MetalColor, 5f);
            for (int x = gate.Left + 18; x < gate.Right; x += 24)
            {
                batch.DrawLine(pixel, new Vector2(x, gate.Top - 17), new Vector2(x, gate.Bottom + 17), GameBalance.StoneColor, 7f);
            }
        }

        if (_loopState == ArenaLoopState.Intermission)
        {
            float pulse = 0.5f + 0.5f * MathF.Sin(_presentationTime * 3f);
            Vector2 center = _arena.CombatBounds.Center.ToVector2();
            // A breathing pool of Death Flame light where the next wave is called (the prompt
            // appears inside it); no edge line.
            Color pool = GameBalance.DeathFlame * (0.16f + pulse * 0.08f);
            pool.A = 0;
            _art.DrawSoftSpot(batch, center, new Vector2(GameBalance.WaveTriggerRadius * 1.15f), pool);
            _art.DrawSoftSpot(batch, center, new Vector2(GameBalance.WaveTriggerRadius * 0.5f), GameBalance.DeathFlame * (0.05f + pulse * 0.04f));
        }

        foreach (PendingArenaSpawn spawn in _pendingSpawns)
        {
            // Gathering Death Flame: light is drawn in from all around the place where the enemy is
            // about to appear and pools there, tighter and brighter as it comes; no ring.
            float progress = 1f - spawn.Remaining / GameBalance.ArenaSpawnTelegraphDuration;
            float pulse = 0.5f + 0.5f * MathF.Sin(_presentationTime * 14f);
            float radius = MathHelper.Lerp(62f, 26f, progress);
            for (int strand = 0; strand < 6; strand++)
            {
                float angle = strand * MathHelper.TwoPi / 6f + _presentationTime * 0.7f + spawn.Position.X * 0.01f;
                Vector2 from = spawn.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (radius * 1.5f);
                WorldMarks.Stream(batch, _art.SoftSpot, from, spawn.Position, _presentationTime, 0.35f + progress * 0.5f,
                    GameBalance.DeathFlameBright, 80f + progress * 120f, 11f, strand);
            }
            Color pool = GameBalance.DeathFlame * (0.14f + progress * 0.26f);
            pool.A = 0;
            _art.DrawSoftSpot(batch, spawn.Position, new Vector2(radius * 1.1f), pool);
            _art.DrawSoftSpot(batch, spawn.Position, new Vector2(6f + progress * 8f + pulse * 2f), GameBalance.SoulWhite * (0.15f + progress * 0.35f));
        }

        if (_loopState is ArenaLoopState.Intro or ArenaLoopState.Transition)
        {
            // The Death Flame gathers in the middle of the hall as the wave arrives: soft light
            // breathing on the floor, no ring.
            float pulse = 0.5f + 0.5f * MathF.Sin(_presentation.StateTime * 8f);
            Color gather = GameBalance.DeathFlame * (0.1f + pulse * 0.06f);
            gather.A = 0;
            _art.DrawSoftSpot(batch, _arena.CombatBounds.Center.ToVector2(), new Vector2(150f + pulse * 14f, 90f + pulse * 8f), gather);
        }
    }

    private void DrawDebugOverlay(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        int x = viewport.Width - 324;
        int y = 24;
        IReadOnlyList<string> missingVisuals = _art.MissingVisuals;
        const int maxMissingLines = 12;
        int missingLines = missingVisuals.Count == 0 && _art.RegistryError is null
            ? 0
            : 1 + Math.Min(missingVisuals.Count, maxMissingLines) + (missingVisuals.Count > maxMissingLines ? 1 : 0);
        int panelHeight = 216 + missingLines * 16;
        batch.FillRectangle(pixel, new Rectangle(x - 14, y - 12, 308, panelHeight), new Color(5, 5, 9) * 0.9f);
        batch.DrawRectangle(pixel, new Rectangle(x - 14, y - 12, 308, panelHeight), new Color(80, 220, 210) * 0.72f, 2f);

        Color label = new(189, 231, 226);
        PixelText.Draw(batch, pixel, $"FPS: {_fps}", new Vector2(x, y), 2, label);
        PixelText.Draw(batch, pixel, $"HP: {_player.Health}/{_player.MaxHealth}", new Vector2(x, y + 24), 2, label);
        string resonance = _player.ResonanceActive
            ? $"RESONANCE: {_player.ResonanceRemaining:0.0}"
            : $"RESONANCE: {_player.Resonance:0}/{GameBalance.ResonanceRequired:0}";
        PixelText.Draw(batch, pixel, resonance, new Vector2(x, y + 48), 2, label);
        PixelText.Draw(batch, pixel, $"FLOW: {_phase}/{_loopState}", new Vector2(x, y + 72), 2, label);
        PixelText.Draw(batch, pixel, $"ENEMIES: {_enemies.Count(enemy => enemy.IsAlive)}", new Vector2(x, y + 96), 2, label);
        PixelText.Draw(batch, pixel, $"SOULS: {_souls.Count}", new Vector2(x, y + 120), 2, label);
        PixelText.Draw(batch, pixel, $"PLAYER: {GetPlayerState()}", new Vector2(x, y + 144), 2, label);
        PixelText.Draw(batch, pixel, $"SENSE FORCE: {(_forceSoulSense ? "ON" : "OFF")}", new Vector2(x, y + 168), 2, label);

        if (missingLines == 0)
        {
            return;
        }

        // Each missing Visual-ID or clip is listed once, in the order it was first drawn.
        Color warning = new(232, 72, 196);
        int lineY = y + 200;
        PixelText.Draw(batch, pixel, _art.RegistryError is null ? $"GRAFIK FEHLT: {missingVisuals.Count}" : "REGISTRY FEHLERHAFT", new Vector2(x, lineY), 1, warning);
        for (int index = 0; index < Math.Min(missingVisuals.Count, maxMissingLines); index++)
        {
            lineY += 16;
            PixelText.Draw(batch, pixel, missingVisuals[index], new Vector2(x, lineY), 1, label);
        }
        if (missingVisuals.Count > maxMissingLines)
        {
            PixelText.Draw(batch, pixel, $"+{missingVisuals.Count - maxMissingLines} WEITERE", new Vector2(x, lineY + 16), 1, label);
        }
    }

    private void UpdateFps(float deltaTime)
    {
        _fpsFrames++;
        _fpsTimer += deltaTime;
        if (_fpsTimer >= 0.5f)
        {
            _fps = (int)MathF.Round(_fpsFrames / _fpsTimer);
            _fpsFrames = 0;
            _fpsTimer = 0f;
        }
    }

    private string GetPlayerState()
    {
        if (_player.IsDead) return "DEAD";
        if (_player.ResonanceActive) return "RESONANCE";
        if (_player.IsDashing) return "DASH";
        if (_player.Cannon.IsHandling) return "CANNON";
        if (_player.Scythe.ActiveStep > 0) return $"SCYTHE {_player.Scythe.ActiveStep}";
        if (_player.SoulSenseActive) return "SOUL SENSE";
        return "NORMAL";
    }

    private bool IsPointInsideStrike(Vector2 point, ScytheStrike strike)
    {
        Vector2 toPoint = point - _player.Position;
        if (toPoint.LengthSquared() > MathF.Pow(strike.Range + GameBalance.HollowCoreRadius, 2f))
        {
            return false;
        }

        Vector2 direction = toPoint.LengthSquared() > 0.001f ? Vector2.Normalize(toPoint) : strike.Direction;
        return Vector2.Dot(strike.Direction, direction) >= MathF.Cos(strike.ArcRadians * 0.5f);
    }

    private Vector2 FindStrikeWeakPoint(Enemy enemy, ScytheStrike strike)
    {
        if (!_player.SoulSenseActive)
        {
            return Vector2.Zero;
        }

        if (enemy is Hollow hollow && IsPointInsideStrike(hollow.CorePosition, strike))
        {
            return hollow.CorePosition;
        }

        if (enemy is Burning burning)
        {
            foreach (Vector2 fracture in burning.GetFracturePositions())
            {
                if (IsPointInsideStrike(fracture, strike))
                {
                    return fracture;
                }
            }
        }


        if (enemy is Devourer devourer && IsPointInsideStrike(devourer.TorsoPosition, strike))
        {
            return devourer.TorsoPosition;
        }

        return Vector2.Zero;
    }

    private string GetScreenshotContext()
    {
        if (_pauseMenu.IsOpen) return $"phase15_pause_{_pauseMenu.CurrentPage.Id}";
        if (_devMenu.IsOpen) return "sandbox_dev_menu";
        if (_characterMenu.IsOpen) return $"character_{_characterMenu.SelectedTab.ToString().ToLowerInvariant()}";
        if (_phase == GamePhase.Title) return _menu.IsOpen ? $"phase15_menu_{_menu.CurrentPage.Id}" : "phase15_title";
        if (_phase == GamePhase.Prologue) return $"prologue_{_prologue.Stage.ToString().ToLowerInvariant()}";
        if (_phase == GamePhase.Antechamber)
        {
            if (_player.SoulSenseActive) return "phase16_antechamber_soul_sense";
            HubDoor? door = _antechamber.DoorAt(_player.Position);
            if (door is null) return "phase16_antechamber";
            string name = door.Kind == HubDoorKind.Final ? "final" : door.Numeral.ToLowerInvariant();
            return $"phase16_antechamber_door_{name}_{(door.IsSealed ? "sealed" : "open")}";
        }
        if (_phase == GamePhase.EnteringArena) return "phase16_entering_arena";
        if (_sandboxActive) return _player.IsDead ? "sandbox_player_down" : "sandbox";
        if (_player.IsDead) return "phase05_player_down";
        if (_loopState == ArenaLoopState.Complete) return "phase15_soul_free";
        if (_loopState is ArenaLoopState.Intermission or ArenaLoopState.Transition) return $"phase12_wave_{_waveNumber}_clear";
        if (_loopState == ArenaLoopState.Intro) return "phase12_arena_intro";
        if (_player.ResonanceActive) return "phase11_resonance_active";
        if (_player.IsResonanceReady) return "phase11_resonance_ready";
        if (_cannonShots.Any(shot => shot.IsFullCharge && !shot.IsFinished)) return "phase08_full_cannon_shot";
        if (_player.Cannon.IsFullCharge) return "phase08_cannon_full_charge";
        if (_player.Cannon.ChargeStage == 3) return "phase08_cannon_charge_stage_3";
        if (_player.Cannon.ChargeStage == 2) return "phase08_cannon_charge_stage_2";
        if (_player.Cannon.ChargeStage == 1) return "phase08_cannon_charge_stage_1";
        if (_enemies.OfType<Burning>().Any(burning => burning.State == BurningState.Detonating)) return "phase09_burning_detonation";
        if (_enemies.OfType<Burning>().Any(burning => burning.State == BurningState.Charge)) return "phase09_burning_charge";
        if (_player.SoulSenseActive && _enemies.OfType<Burning>().Any(burning => burning.IsAlive)) return "phase09_burning_fractures";
        if (_player.SoulSenseActive && _enemies.OfType<Devourer>().Any(devourer => devourer.ConsumedSoulCount > 0)) return "phase10_devourer_trapped_souls";
        if (_enemies.OfType<Devourer>().Any(devourer => devourer.State == DevourerState.Devour)) return "phase10_devourer_devouring";
        if (_enemies.OfType<Devourer>().Any(devourer => devourer.State == DevourerState.ApproachSoul)) return "phase10_devourer_soul_target";
        if (_player.SoulSenseActive && _enemies.Any(enemy => enemy.IsAlive)) return "phase07_soul_sense_hollow_cores";
        if (_player.SoulSenseActive) return "phase07_soul_sense_arena";
        if (_souls.Any(soul => soul.State == SoulState.Releasing)) return "phase06_soul_release";
        if (_souls.Any(soul => soul.State == SoulState.Residue)) return "phase06_residue_to_player";
        if (_souls.Any(soul => soul.State == SoulState.Exposed)) return "phase06_exposed_soul";
        if (_enemies.OfType<Hollow>().Any(hollow => hollow.State == HollowState.Telegraph)) return "phase05_hollow_swipe_telegraph";
        if (_enemies.OfType<Hollow>().Any(hollow => hollow.State == HollowState.Dying)) return "phase05_hollow_death";
        if (_player.Scythe.ActiveStep > 0) return $"phase05_scythe_hit_{_player.Scythe.ActiveStep}";
        return _debugVisible ? $"phase12_wave_{_waveNumber}_debug" : $"phase12_wave_{_waveNumber}_combat";
    }

    private static float Ease(float amount)
    {
        float value = MathHelper.Clamp(amount, 0f, 1f);
        return value * value * (3f - 2f * value);
    }

    private void SpawnCannonShot()
    {
        if (!_player.Cannon.TryConsumeShot(out CannonShotRequest request))
        {
            return;
        }

        Vector2 origin = _player.Position + request.Direction * 74f;
        _cannonShots.Add(new CannonShot(origin, request));
        // The flash bursts from the drawn muzzle, which has grown with the charge (presentation
        // only; the shot itself starts at its gameplay origin, inside the flash).
        Vector2 flash = _art.HasClip(VisualIds.Player, VisualClips.Aim)
            ? _player.Position + request.Direction * FigureHeights.MuzzleReach(request.Charge)
            : origin;
        _combatPresentation.PresentCannonFire(flash, request);
        if (request.IsFullCharge)
        {
            _arenaAtmosphere.ReactToForce(origin, 460f, 135f);
        }
        _audio.Play(AudioCue.CannonFire, request.IsFullCharge ? 0.8f : 0.58f, request.IsFullCharge ? -0.08f : 0.08f);
        _player.ApplyCannonRecoil(request.Direction, request.Charge);
    }

    private void UpdateCannonShots(float deltaTime)
    {
        foreach (CannonShot shot in _cannonShots)
        {
            shot.Update(deltaTime, ActiveWorldBounds);
            if (shot.IsFinished)
            {
                continue;
            }

            foreach (Enemy enemy in _enemies.Where(enemy => enemy.IsAlive))
            {
                float bodyRadius = enemy.Radius + shot.Radius;
                if (DistanceSquaredToSegment(enemy.Position, shot.PreviousPosition, shot.Position) > bodyRadius * bodyRadius)
                {
                    continue;
                }

                if (enemy is Burning chargingBurning && chargingBurning.IsCharging)
                {
                    chargingBurning.Detonate();
                    _combatPresentation.BeginBurningCompression(chargingBurning.Position, shot.Direction);
                    shot.MarkHit();
                    break;
                }

                Vector2 weakPoint = FindCannonWeakPoint(enemy, shot);
                bool coreHit = weakPoint != Vector2.Zero;
                int damage = coreHit
                    ? (int)MathF.Round(shot.Damage * GameBalance.CannonCoreDamageMultiplier)
                    : shot.Damage;
                float knockback = MathHelper.Lerp(330f, 760f, shot.Charge);
                ApplyWeaponDamage(enemy, new DamageInfo(
                    damage,
                    shot.Direction * knockback,
                    coreHit ? weakPoint : enemy.Position,
                    coreHit,
                    shot.IsFullCharge));

                Vector2 impactPosition = coreHit ? weakPoint : enemy.Position;
                PlayMaterialHit(enemy, shot.IsFullCharge ? 0.75f : 0.5f, shot.IsFullCharge ? -0.05f : 0.03f);
                if (shot.IsFullCharge)
                {
                    _audio.Play(AudioCue.HitHeavy, 0.6f, -0.04f, PanOf(impactPosition) * 0.5f);
                }
                _combatPresentation.PresentCannonImpact(
                    impactPosition,
                    shot.Direction,
                    shot.IsFullCharge,
                    coreHit);
                if (coreHit)
                {
                    _player.AddResonance(GameBalance.ResonancePerCoreHit * (shot.IsFullCharge ? 2f : 1f));
                    _audio.Play(AudioCue.CoreHit, shot.IsFullCharge ? 0.86f : 0.66f);
                }
                else
                {
                    _audio.Play(AudioCue.CannonImpact, shot.IsFullCharge ? 0.72f : 0.48f, 0f, PanOf(shot.Position) * 0.7f);
                }

                shot.MarkHit();
                break;
            }
        }

        _cannonShots.RemoveAll(shot => shot.IsFinished);
    }

    private Vector2 FindCannonWeakPoint(Enemy enemy, CannonShot shot)
    {
        if (!shot.SoulSenseAtFire)
        {
            return Vector2.Zero;
        }

        if (enemy is Hollow hollow)
        {
            float coreRadius = GameBalance.HollowCoreRadius + shot.Radius;
            if (DistanceSquaredToSegment(hollow.CorePosition, shot.PreviousPosition, shot.Position) <= coreRadius * coreRadius)
            {
                return hollow.CorePosition;
            }
        }

        if (enemy is Burning burning)
        {
            foreach (Vector2 fracture in burning.GetFracturePositions())
            {
                float fractureRadius = GameBalance.BurningFractureRadius + shot.Radius;
                if (DistanceSquaredToSegment(fracture, shot.PreviousPosition, shot.Position) <= fractureRadius * fractureRadius)
                {
                    return fracture;
                }
            }
        }


        if (enemy is Devourer devourer)
        {
            float torsoRadius = GameBalance.DevourerTorsoRadius + shot.Radius;
            if (DistanceSquaredToSegment(devourer.TorsoPosition, shot.PreviousPosition, shot.Position) <= torsoRadius * torsoRadius)
            {
                return devourer.TorsoPosition;
            }
        }

        return Vector2.Zero;
    }

    private void ResolveBurningDetonation(Burning source, Vector2 position)
    {
        _combatPresentation.PresentBurningDetonation(position);
        _arenaAtmosphere.ReactToForce(position, 560f, 190f);
        _audio.Play(AudioCue.BurningDetonation, 0.9f, 0f, PanOf(position) * 0.6f);

        foreach (Enemy enemy in _enemies.Where(enemy => enemy != source && enemy.IsAlive))
        {
            Vector2 away = enemy.Position - position;
            float combinedRadius = GameBalance.BurningDetonationRadius + enemy.Radius;
            if (away.LengthSquared() > combinedRadius * combinedRadius)
            {
                continue;
            }

            Vector2 direction = away.LengthSquared() > 0.001f ? Vector2.Normalize(away) : Vector2.UnitX;
            ApplyEnemyDamage(enemy, new DamageInfo(
                GameBalance.BurningDetonationDamage,
                direction * GameBalance.BurningDetonationKnockback,
                enemy.Position));
            _particles.EmitBurst(enemy.Position, direction, 18, GameBalance.DeathFlame, 260f, 8f);
        }
    }

    /// <summary>The target's material under a landed blow: cloth and porcelain, ember crust, flesh, wood.</summary>
    private void PlayMaterialHit(Enemy enemy, float volume, float pitch)
    {
        AudioCue? cue = enemy switch
        {
            Hollow => AudioCue.HitHollow,
            Burning => AudioCue.HitBurning,
            Devourer => AudioCue.HitDevourer,
            TrainingDummy => AudioCue.HitDummy,
            _ => null
        };
        if (cue is { } material)
        {
            _audio.Play(material, volume, pitch, PanOf(enemy.Position) * 0.7f);
        }
    }

    private static float DistanceSquaredToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float lengthSquared = segment.LengthSquared();
        if (lengthSquared <= 0.001f)
        {
            return Vector2.DistanceSquared(point, start);
        }

        float amount = MathHelper.Clamp(Vector2.Dot(point - start, segment) / lengthSquared, 0f, 1f);
        return Vector2.DistanceSquared(point, start + segment * amount);
    }

    private void PlayPlayerActionAudio(
        bool wasDashing,
        bool wasResonanceActive,
        bool wasSoulSenseActive,
        bool wasCannonFull,
        SoulCannonState previousCannonState)
    {
        if (_player.Scythe.StartedThisFrame)
        {
            AudioCue cue = _player.Scythe.ActiveStep switch
            {
                2 => AudioCue.ScytheSwing2,
                3 => AudioCue.SoulCleave,
                _ => AudioCue.ScytheSwing1
            };
            _audio.Play(cue, _player.Scythe.ActiveStep == 3 ? 0.72f : 0.42f);
            // The weight of the big blade under the light Ludo whoosh, peaking at contact.
            (AudioCue weight, float level) = _player.Scythe.ActiveStep switch
            {
                2 => (AudioCue.ScytheWeight2, 0.58f),
                3 => (AudioCue.ScytheWeight3, 0.6f),
                _ => (AudioCue.ScytheWeight1, 0.64f)
            };
            _audio.Play(weight, level);
        }

        if (!wasDashing && _player.IsDashing)
        {
            _audio.Play(AudioCue.Dash, 0.62f);
        }
        if (previousCannonState == SoulCannonState.Stored && _player.Cannon.State == SoulCannonState.Drawing)
        {
            _audio.Play(AudioCue.CannonDraw, 0.9f);
        }
        else if (previousCannonState == SoulCannonState.Returning && _player.Cannon.State == SoulCannonState.Stored)
        {
            _audio.Play(AudioCue.CannonStow, 0.78f);
        }
        if (previousCannonState != SoulCannonState.Charging && _player.Cannon.State == SoulCannonState.Charging)
        {
            _audio.Play(AudioCue.CannonCharge, 0.42f);
        }
        if (!wasCannonFull && _player.Cannon.IsFullCharge)
        {
            _audio.Play(AudioCue.CannonFull, 0.72f);
        }
        if (!wasResonanceActive && _player.ResonanceActive)
        {
            _audio.Play(AudioCue.ResonanceActivate, 0.88f);
        }
        if (!wasSoulSenseActive && _player.SoulSenseActive && !_player.ResonanceActive)
        {
            _audio.Play(AudioCue.SoulSenseOn, 0.38f);
        }
        else if (wasSoulSenseActive && !_player.SoulSenseActive)
        {
            _audio.Play(AudioCue.SoulSenseOff, 0.5f);
        }

        if (wasSoulSenseActive != _player.SoulSenseActive)
        {
            _audio.SetSoulSense(_player.SoulSenseActive);
        }
    }

    private void ApplyWeaponDamage(Enemy enemy, DamageInfo damage)
    {
        if (!enemy.IsAlive) return;
        ApplyEnemyDamage(enemy, RunAbilities.ResolveWeaponHit(_player, enemy, damage));
    }

    private void ApplyEnemyDamage(Enemy enemy, DamageInfo damage)
    {
        bool wasAlive = enemy.IsAlive;
        enemy.ApplyDamage(damage);
        if (wasAlive && !enemy.IsAlive)
        {
            if (enemy is Hollow && _art.IsRendered(enemy.VisualId))
            {
                // The porcelain mask cracks: shards break off at head height and fall around the
                // body. Particles live in the air pass, 70 units above the floor they land on.
                Vector2 head = enemy.Position - new Vector2(0f, 98f - FigureHeights.Air);
                _particles.EmitDebris(head, enemy.Position.Y + FigureHeights.Air + 6f, 9, new Color(214, 208, 198), 170f, 4.5f);
            }
            CreditDefeatedEnemy(enemy);
            float volume = enemy is Devourer ? 0.62f : 0.42f;
            _audio.Play(AudioCue.EnemyDeath, volume, 0f, PanOf(enemy.Position) * 0.7f);
            AudioCue? death = enemy switch
            {
                Hollow => AudioCue.DeathHollow,
                Burning => AudioCue.DeathBurning,
                Devourer => AudioCue.DeathDevourer,
                _ => null
            };
            if (death is { } layer)
            {
                _audio.Play(layer, enemy is Devourer ? 0.64f : 0.66f, 0f, PanOf(enemy.Position) * 0.7f);
            }
        }
    }
}
