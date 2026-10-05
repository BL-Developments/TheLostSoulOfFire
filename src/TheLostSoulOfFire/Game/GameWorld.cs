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
    private readonly SoulSensePresentation _soulSensePresentation = new();
    private readonly CinematicPresentation _presentation = new();
    private readonly ArtAssets _art;
    private readonly SpriteVfxSystem _spriteVfx;
    private readonly List<SceneProp> _sceneProps = [];
    private readonly List<SceneProp> _arenaProps = Arena.Props
        .Select(placement => new SceneProp(placement.VisualId, placement.Foot, placement.FallbackSize, placement.FallbackLayer))
        .ToList();
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
        _menu = new MenuController(_settings);
        _pauseMenu = new MenuController(_settings);
        _settingsChanged = settingsChanged;
        _profileStore = profileStore ?? new PlayerProfileStore();
        _wallet.LoadSecured(_profileStore.Load());
        _art = art;
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
            return;
        }

        if (_phase != GamePhase.Title && input.WasKeyPressed(Keys.Tab))
        {
            _characterMenu.Open();
            _audio.SetPaused(true);
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
        UpdateFps(deltaTime);
        _screenEffects.Update(deltaTime);
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
            else if (!_endingRevealPlayed && _presentation.StateTime >= CinematicPresentation.LifeFlameRevealTime)
            {
                _endingRevealPlayed = true;
                _audio.Play(AudioCue.EndingReveal, 0.72f);
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
        _soulSensePresentation.Update(deltaTime, _player.SoulSenseActive);
        if (_audioTestFatalDamageRequested)
        {
            _audioTestFatalDamageRequested = false;
            _player.ApplyDamage(GameBalance.PlayerMaxHealth, Vector2.Zero, _screenEffects, ignoreArmor: true);
        }
        if (_player.Scythe.StartedThisFrame)
        {
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
            if (enemy is Hollow hollowAfter && previousHollowState != HollowState.Swipe && hollowAfter.State == HollowState.Swipe)
            {
                _audio.Play(AudioCue.HollowSwipe, 0.48f);
            }
            if (enemy is Burning burningAfter && previousBurningState != BurningState.Telegraph && burningAfter.State == BurningState.Telegraph)
            {
                _audio.Play(AudioCue.BurningCharge, 0.72f);
            }
            if (enemy is Devourer devourerAfter)
            {
                if (previousDevourerState != DevourerState.Slam && devourerAfter.State == DevourerState.Slam)
                {
                    _audio.Play(AudioCue.DevourerSlam, 0.76f);
                }
                if (previousDevourerState != DevourerState.Devour && devourerAfter.State == DevourerState.Devour)
                {
                    _audio.Play(AudioCue.DevourerDevour, 0.6f);
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
                _audio.Play(AudioCue.SoulRelease, 0.62f);
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
            _audio.Play(_player.IsDead ? AudioCue.PlayerDeath : AudioCue.PlayerHit, _player.IsDead ? 0.78f : 0.6f);
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
            deltaTime);
    }

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
            return;
        }

        if (input.WasKeyPressed(Keys.Left) || input.WasKeyPressed(Keys.A)) _characterMenu.SelectPrevious();
        else if (input.WasKeyPressed(Keys.Right) || input.WasKeyPressed(Keys.D)) _characterMenu.SelectNext();

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
    private MenuActionResult UpdateMenuInput(MenuController menu, float deltaTime, InputState input, Viewport viewport)
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
        if (valueChanged) ApplySettingsChanges();

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
            _antechamber.DrawSoulSense(batch, pixel, _presentationTime, _soulSensePresentation.SoulEmergence);
            batch.End();
        }
        renderer.DrawVignette(batch, viewport, _soulSensePresentation.WorldSuppression, _player.ResonanceActive);
        if (_automatedHideHud)
        {
            return;
        }
        DrawScreenFeedback(batch, pixel, viewport);
        DrawHud(batch, pixel, viewport);
    }

    private void DrawScene(SpriteBatch batch, Texture2D pixel, Viewport viewport, IReadOnlyList<SceneLight> lights)
    {
        Matrix sceneTransform = RenderResolution.ToOutput(_camera.GetTransform(viewport, _screenEffects.CameraOffset));
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
                _presentationTime,
                _soulSensePresentation.SoulEmergence,
                DoorTransitionProgress,
                _debugVisible);
        }
        else
        {
            if (_phase == GamePhase.Prologue)
            {
                PrologueEnvironment.DrawGround(batch, pixel, _prologue, _presentationTime, _soulSensePresentation.WorldSuppression);
                if (_prologue.Sector == PrologueSector.Emergence)
                {
                    _art.DrawEnvironment(batch, VisualIds.ShoreFloor, Vector2.Zero);
                }
                PrologueEnvironment.DrawProps(batch, pixel, _prologue, _presentationTime, _soulSensePresentation.WorldSuppression);
            }
            else
            {
                _art.DrawEnvironment(batch, VisualIds.ArenaWall, Arena.WallFoot);
                _art.DrawEnvironment(batch, VisualIds.ArenaFloor, Arena.FloorTopLeft);
                _arenaAtmosphere.DrawBackground(batch, pixel, _soulSensePresentation.WorldSuppression);
            }
            DrawSceneProps(batch, layer => layer < SceneLayer.Actor);
            if (IsCombatPhase && _phase == GamePhase.Arena)
            {
                DrawArenaLoop(batch, pixel);
                DrawCurrencyWorld(batch, pixel);
            }
        }

        _player.DrawAfterimages(batch, pixel);
        if (IsCombatPhase)
        {
            _art.DrawDissolves(batch);
        }
        DrawActorBand(batch, pixel, shouldDrawPlayer && (inAntechamber || IsCombatPhase));
        DrawAutomatedStaging(batch);

        DrawSceneProps(batch, layer => layer is SceneLayer.Occluder or SceneLayer.Foreground);
        if (_phase == GamePhase.Prologue)
        {
            PrologueEnvironment.DrawForeground(batch, pixel, _prologue);
        }

        if (IsCombatPhase)
        {
            foreach (Soul soul in _souls)
            {
                _art.DrawLostSoul(batch, soul);
                soul.Draw(batch, pixel, _player, false, true);
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
        _particles.Draw(batch, pixel);
        _spriteVfx.Draw(batch);
        batch.End();
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, transformMatrix: sceneTransform);
        _art.BeginLitScene(sceneTransform, lights);

        DrawSceneProps(batch, layer => layer == SceneLayer.Atmosphere);
        _presentation.DrawWorldAccents(batch, pixel, _art, _phase, _loopState, _player.IsDead, _player, ActiveCombatBounds);

        if (IsCombatPhase && _presentation.ShouldDrawAim(_loopState, _player.IsDead))
        {
            batch.DrawCircle(pixel, _lastMouseWorld, 9f, GameBalance.DeathFlameBright * 0.75f, 2f, 16);
            batch.DrawLine(pixel, _lastMouseWorld - Vector2.UnitX * 13f, _lastMouseWorld + Vector2.UnitX * 13f, GameBalance.DeathFlame * 0.6f, 1f);
            batch.DrawLine(pixel, _lastMouseWorld - Vector2.UnitY * 13f, _lastMouseWorld + Vector2.UnitY * 13f, GameBalance.DeathFlame * 0.6f, 1f);
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

    /// <summary>Props of the room being shown plus props staged by automated tests.</summary>
    private IEnumerable<SceneProp> ActiveSceneProps => _phase switch
    {
        GamePhase.Arena => _arenaProps.Concat(_sceneProps),
        GamePhase.Prologue when _prologue.Sector == PrologueSector.Emergence => _shoreProps.Concat(_sceneProps),
        _ => _sceneProps
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
        _art.DrawPlayer(batch, _player);
        _player.Draw(batch, pixel, _art, _debugVisible, _soulSensePresentation.SoulEmergence);
        if (IsCombatPhase && _player.Cannon.State == SoulCannonState.Charging)
        {
            Vector2 muzzle = rendered
                ? FigureHeights.MuzzleOf(_player.Position, _player.FacingDirection)
                : _player.Position + _player.FacingDirection * 74f;
            float charge = _player.Cannon.ChargeProgress;
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
            _presentation.GetLifeFlamePosition(_arena.CombatBounds),
            _presentation.GetLifeFlameAlpha(),
            drawArenaFurnaces: _phase != GamePhase.Prologue,
            renderedPlayer: _art.HasClip(VisualIds.Player, VisualClips.Aim),
            renderedEnemy: enemy => _art.IsRendered(enemy.VisualId));

        if (_automatedLightSource is not null)
        {
            renderer.BeginLighting(batch, RenderResolution.ToOutput(_camera.GetTransform(viewport, _screenEffects.CameraOffset)));
            DrawAutomatedLights(renderer, batch);
            batch.End();
        }
    }

    private void DrawScreenFeedback(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, transformMatrix: RenderResolution.ScaleMatrix);

        if (_screenEffects.ImpactFrameAlpha > 0f)
        {
            batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * (_screenEffects.ImpactFrameAlpha * 0.82f));
        }

        if (_screenEffects.FlashAlpha > 0f)
        {
            batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), GameBalance.DeathFlameBright * _screenEffects.FlashAlpha);
        }

        if (_player.ResonanceActivationRemaining > 0f)
        {
            float activationFade = MathHelper.Clamp(_player.ResonanceActivationRemaining / 0.5f, 0f, 1f);
            batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * (activationFade * 0.38f));
        }

        batch.End();
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
                ProloguePresentation.DrawOverlay(batch, pixel, viewport, _prologue, _player.IsDead, _settings.OptionalHints);
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
        PixelText.DrawCentered(
            batch,
            pixel,
            "ASHEN ANTECHAMBER",
            centerX,
            viewport.Height * 0.14f,
            2,
            GameBalance.SoulWhite * (0.7f * placeAlpha));

        if (_phase == GamePhase.Antechamber)
        {
            DrawSecuredSummary(batch, pixel, viewport, "GESICHERT", _wallet.Secured(Currency.Geld), _wallet.Secured(Currency.Glut), 1f);
        }

        HubDoor? nearbyDoor = _phase == GamePhase.Antechamber ? _antechamber.DoorAt(_player.Position) : null;
        if (nearbyDoor is not null)
        {
            float pulse = 0.68f + MathF.Sin(_presentationTime * 4f) * 0.14f;
            string prompt = nearbyDoor.Prompt;
            int textScale = PixelText.Measure(prompt, 2) + 48 <= viewport.Width ? 2 : 1;
            int promptWidth = PixelText.Measure(prompt, textScale) + 48;
            Rectangle panel = new((viewport.Width - promptWidth) / 2, viewport.Height - 104, promptWidth, 48);
            Color accent = nearbyDoor.IsSealed ? GameBalance.DeepViolet : GameBalance.DeathFlame;
            batch.FillRectangle(pixel, panel, Color.Black * 0.72f);
            batch.DrawRectangle(pixel, panel, accent * (0.52f * pulse), 2f);
            PixelText.DrawCentered(
                batch,
                pixel,
                prompt,
                centerX,
                panel.Y + 24f - textScale * 3.5f,
                textScale,
                GameBalance.SoulWhite * (nearbyDoor.IsSealed ? 0.72f : pulse));
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
        PixelText.DrawCentered(batch, pixel, "THE DOOR AWAKENS", centerX, viewport.Height * 0.78f, 2, GameBalance.DeathFlameBright * labelAlpha);
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

        _combatPresentation.PresentScytheImpact(strike.Step, strike.Direction);
        _audio.Play(AudioCue.ScytheHit, strike.Step == 3 ? 0.72f : 0.48f, strike.Step == 2 ? 0.08f : 0f);
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
        _camera.Zoom = MathHelper.Lerp(_camera.Zoom, 1f, smoothing);
        Vector2 target = Vector2.Lerp(_player.Position, _antechamber.EntryDoorCenter, 0.11f) + new Vector2(0f, -40f);
        _camera.Follow(target, _antechamber.Bounds, viewport, smoothing);

        HubDoor? door = _antechamber.DoorAt(_player.Position);
        if (door is { IsSealed: false } && input.WasKeyPressed(Keys.E))
        {
            BeginDoorTransition();
        }
    }

    private void BeginDoorTransition()
    {
        _phase = GameFlowRules.EnterDoor(_phase);
        _phaseTime = 0f;
        _player.SettleForCompletion();
        _soulSensePresentation.Reset();
        _audio.SetSoulSense(false);
        _audio.SetCalm(false);
        _audio.SetArenaActive(true);
        _audio.Play(AudioCue.TitleConfirm, 0.48f, -0.14f);
    }

    private void UpdateDoorTransition(float deltaTime, Viewport viewport)
    {
        _soulSensePresentation.Update(deltaTime, false);
        _particles.Update(deltaTime);

        float eased = Ease(DoorTransitionProgress);
        float smoothing = 1f - MathF.Exp(-deltaTime * 7f);
        _camera.Zoom = MathHelper.Lerp(_camera.Zoom, MathHelper.Lerp(1f, 0.88f, eased), smoothing);
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
        _particles.Clear();
        _spriteVfx.Clear();
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
            batch.DrawCircle(pixel, center, GameBalance.WaveTriggerRadius, GameBalance.DeathFlameBright * (0.22f + pulse * 0.22f), 4f, 48);
            batch.DrawCircle(pixel, center, GameBalance.WaveTriggerRadius * 0.55f, GameBalance.DeathFlame * (0.14f + pulse * 0.14f), 3f, 36);
        }

        foreach (PendingArenaSpawn spawn in _pendingSpawns)
        {
            // Gathering death flame: the ring closes in while the enemy is about to appear.
            float progress = 1f - spawn.Remaining / GameBalance.ArenaSpawnTelegraphDuration;
            float pulse = 0.5f + 0.5f * MathF.Sin(_presentationTime * 14f);
            float radius = MathHelper.Lerp(62f, 26f, progress);
            batch.DrawCircle(pixel, spawn.Position, radius, GameBalance.DeathFlameBright * (0.25f + progress * 0.45f), 4f, 32);
            batch.DrawCircle(pixel, spawn.Position, radius * 0.55f + pulse * 4f, GameBalance.DeathFlame * (0.2f + progress * 0.4f), 3f, 24);
        }

        if (_loopState is ArenaLoopState.Intro or ArenaLoopState.Transition)
        {
            float pulse = 0.5f + 0.5f * MathF.Sin(_presentation.StateTime * 8f);
            batch.DrawCircle(pixel, _arena.CombatBounds.Center.ToVector2(), 118f + pulse * 14f, GameBalance.DeathFlame * (0.18f + pulse * 0.18f), 5f, 40);
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
        _combatPresentation.PresentCannonFire(origin, request);
        if (request.IsFullCharge)
        {
            _arenaAtmosphere.ReactToForce(origin, 460f, 135f);
        }
        _audio.Play(AudioCue.CannonFire, request.IsFullCharge ? 0.9f : 0.58f, request.IsFullCharge ? -0.08f : 0.08f);
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
                    _audio.Play(AudioCue.CannonImpact, shot.IsFullCharge ? 0.72f : 0.48f);
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
        _audio.Play(AudioCue.BurningDetonation, 0.9f);

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
            _audio.Play(cue, _player.Scythe.ActiveStep == 3 ? 0.78f : 0.5f);
        }

        if (!wasDashing && _player.IsDashing)
        {
            _audio.Play(AudioCue.Dash, 0.62f);
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
            _audio.Play(AudioCue.SoulSenseOff, 0.3f);
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
            CreditDefeatedEnemy(enemy);
            float volume = enemy is Devourer ? 0.72f : 0.52f;
            _audio.Play(AudioCue.EnemyDeath, volume);
        }
    }
}
