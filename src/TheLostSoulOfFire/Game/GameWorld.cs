using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Audio;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Game;

public enum ArenaLoopState
{
    Title,
    Intro,
    Combat,
    Transition,
    Complete
}

public sealed class GameWorld : IDisposable
{
    private readonly Arena _arena = new();
    private readonly AudioDirector _audio;
    private readonly Camera2D _camera;
    private readonly PresentationSettings _presentationSettings;
    private readonly ScreenEffects _screenEffects;
    private readonly ParticleSystem _particles;
    private readonly ArenaAtmosphere _arenaAtmosphere = new();
    private readonly HudRenderer _hud = new();
    private readonly SoulSensePresentation _soulSensePresentation = new();
    private readonly CinematicPresentation _presentation = new();
    private readonly ArtAssets _art;
    private readonly SpriteVfxSystem _spriteVfx;
    private readonly CombatPresentation _combatPresentation;
    private readonly WardenRoster _roster;
    private readonly TargetDirector _targeting = new();
    private readonly TeamResonance _team = new();
    private readonly List<Enemy> _enemies = [];
    private readonly List<Soul> _souls = [];
    private readonly List<CannonShot> _cannonShots = [];
    private readonly EncounterDirector _director = new();
    private readonly List<EncounterSpawn> _releasedSpawns = [];
    private Vector2 _lastMouseWorld;
    private bool _debugVisible;
    private bool _forceSoulSense;
    private int _waveNumber;
    private ArenaLoopState _loopState = ArenaLoopState.Title;

    /// <summary>
    /// Set only by the Session 3 character-study visual fixtures. Holds the beat
    /// loop open so the protagonist can be judged without arrivals, banners or
    /// transitions entering the frame. Never set in a shipped run.
    /// </summary>
    private bool _visualSandbox;
    private float _burningHandoffTimer;
    private int _burningCommittedLastFrame;
    private float _presentationTime;
    private float _fpsTimer;
    private int _fpsFrames;
    private int _fps = 60;
    private bool _audioTestFatalDamageRequested;
    private bool _endingRevealPlayed;
    private bool _teamWiped;
    private readonly bool _autoJoinSecond;
    private readonly int[] _healthLastFrame = new int[GameBalance.MaxLocalPlayers];

    /// <summary>
    /// The protagonist. Solo paths, the HUD's primary read and every existing
    /// single-player check still speak through him; co-op only adds a second slot
    /// beside him rather than replacing this.
    /// </summary>
    private Player _player => _roster.Lead;

    public string ScreenshotContext => GetScreenshotContext();
    public bool IsCooperative => _roster.IsCooperative;
    public int LocalPlayerCount => _roster.Count;
    public ArenaLoopState LoopState => _loopState;
    public int WaveNumber => _waveNumber;
    /// <summary>
    /// True when no Warden can continue. Solo: the protagonist died. Co-op: both
    /// brothers are down or out. Everything that used to branch on the single
    /// player's death now branches on this, so retry behaves identically.
    /// </summary>
    public bool PlayerDead => _roster.AllDown();
    public float PresentationStateTime => _presentation.StateTime;

    // Used only by VisualScenarioRunner. Arrange actual entities; their normal
    // Update methods still own AI, damage, timings and knockback.
    internal void ArrangeVisualSubject(string scenario)
    {
        // The Session 3 character fixtures want the protagonist alone on a quiet
        // floor: the only thing being judged is the Warden, his gait and his
        // facing. The beat loop is held open so no banner, arrival or transition
        // can wander into the frame.
        if (scenario is "facing-sweep" or "run-cycle" or "strafe-read" or "combo-chain" or "dash-cancel")
        {
            _visualSandbox = true;
            _waveNumber = 1;
            _loopState = ArenaLoopState.Combat;
            _enemies.Clear();
            _souls.Clear();
            _cannonShots.Clear();
            _audio.SetCalm(true);
            return;
        }

        Enemy subject = scenario switch
        {
            "hollow-swipe" => new Hollow(_player.Position + new Vector2(165f, 0f), 1),
            "scythe-combo" => new Hollow(_player.Position + new Vector2(94f, 0f), 1),
            "burning-charge" => new Burning(_player.Position + new Vector2(310f, 0f), 1),
            "devourer-slam" => new Devourer(_player.Position + new Vector2(210f, 0f)),
            "severance-window" or "severance-cut" => new Devourer(_player.Position + new Vector2(205f, 0f)),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        _enemies.Clear();
        _souls.Clear();

        // The Severance fixtures use a Devourer that already holds a Soul, because
        // freeing it is the point of the mechanic being captured.
        if (subject is Devourer carrier && scenario.StartsWith("severance", StringComparison.Ordinal))
        {
            Soul held = new(carrier.Position);
            carrier.SeedHeldSoul(held);
            _souls.Add(held);
        }

        _enemies.Add(subject);
    }

    /// <summary>
    /// Read-only capture seam: a Burning is coming apart right now. The
    /// detonation fixture reacts to the real state instead of guessing a tick.
    /// </summary>
    internal bool BurningDetonating =>
        _enemies.Any(enemy => enemy is Burning { State: BurningState.Detonating });

    /// <summary>Read-only capture seam: a Burning is mid-charge and can be burst.</summary>
    internal bool BurningCharging =>
        _enemies.Any(enemy => enemy is Burning { State: BurningState.Charge });

    /// <summary>
    /// Read-only capture seam: true on the frames where a dash would open a
    /// Severance Window. Fixtures react to the real combat state instead of
    /// hard-coded tick numbers, so the evidence stays honest if timings change.
    /// </summary>
    internal bool SeveranceOpportunityReady
    {
        get
        {
            if (PlayerDead || _loopState != ArenaLoopState.Combat)
            {
                return false;
            }

            foreach (Enemy enemy in _enemies)
            {
                if (!enemy.IsAlive)
                {
                    continue;
                }

                float remaining = enemy.CommitmentRemaining;
                if (remaining < 0f || remaining > GameBalance.SeveranceReadTime)
                {
                    continue;
                }

                foreach (PlayerSlot slot in _roster.Slots)
                {
                    if (!slot.Warden.CanBeTargeted)
                    {
                        continue;
                    }

                    float threat = enemy.CommitmentThreatRange + slot.Warden.Radius + GameBalance.SeveranceThreatPadding;
                    if (Vector2.DistanceSquared(enemy.Position, slot.Warden.Position) <= threat * threat)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    internal bool SeveranceWindowOpen
    {
        get
        {
            foreach (PlayerSlot slot in _roster.Slots)
            {
                if (slot.Warden.SeveranceReady)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public object VisualSnapshot => new
    {
        fixtureVersion = 2,
        loopState = _loopState.ToString(), wave = _waveNumber,
        stateTime = _presentation.StateTime, playerDead = _player.IsDead,
        player = new { x = _player.Position.X, y = _player.Position.Y, health = _player.Health,
            scythe = _player.Scythe.StateLabel, cannon = _player.Cannon.State.ToString(),
            resonance = _player.ResonanceActive, sense = _player.SoulSenseActive },
        enemies = _enemies.Select(enemy => new { family = enemy.GetType().Name, state = enemy.StateLabel,
            x = enemy.Position.X, y = enemy.Position.Y }).ToArray(),
        souls = _souls.Select(soul => new { state = soul.State.ToString(), x = soul.Position.X, y = soul.Position.Y }).ToArray(),
        particles = _particles.ActiveCount, spriteEffects = _spriteVfx.ActiveCount,
        settings = _presentationSettings.Summary
    };

    public string WindowTitle => _debugVisible
        ? $"The Lost Soul of Fire — DEBUG | Wave {_waveNumber}/4 {_loopState.ToString().ToUpperInvariant()} | HP {_player.Health} | RES {(_player.ResonanceActive ? $"ACTIVE {_player.ResonanceRemaining:0.0}s" : $"{_player.Resonance:0}/{GameBalance.ResonanceRequired:0}")} | Player {GetPlayerState()} | Enemies {_enemies.Count(enemy => enemy.IsAlive)} | Souls {_souls.Count}"
        : "The Lost Soul of Fire";

    public GameWorld(
        Viewport viewport,
        ArtAssets art,
        ContentManager content,
        PresentationSettings presentationSettings,
        int localPlayers = 1)
    {
        _art = art;
        _presentationSettings = presentationSettings;
        _audio = new AudioDirector(content);
        _screenEffects = new ScreenEffects(presentationSettings);
        _particles = new ParticleSystem(presentationSettings);
        _spriteVfx = new SpriteVfxSystem(art, presentationSettings);
        _combatPresentation = new CombatPresentation(_particles, _screenEffects, _spriteVfx);
        _camera = new Camera2D(_arena.CombatBounds.Center.ToVector2());
        _roster = new WardenRoster(_arena.CombatBounds.Center.ToVector2());
        _autoJoinSecond = localPlayers >= GameBalance.MaxLocalPlayers;
        _lastMouseWorld = _player.Position + Vector2.UnitX * 200f;
        Array.Fill(_healthLastFrame, GameBalance.PlayerMaxHealth);
        _camera.SnapTo(_arena.CombatBounds.Center.ToVector2(), _arena.Bounds, viewport);
    }

    /// <summary>
    /// Brings the brother into the encounter. Called from the join input, from
    /// the two-player launch flag and from deterministic co-op scenarios.
    /// </summary>
    public bool TryJoinSecondWarden(IPlayerInputSource source)
    {
        Vector2 spawn = _player.Position + new Vector2(-118f, 26f);
        if (!_roster.TryJoinSecond(spawn, source))
        {
            return false;
        }

        _combatPresentation.PresentArrivalFlame(spawn);
        _spriteVfx.Spawn("dash_ignition", spawn, 0f, 0.8f, WardenIdentity.Elder.FlameBright * 0.6f);
        _particles.EmitDeathFlame(spawn, 16, 1.2f);
        _audio.Play(AudioCue.WardenStabilize, 0.6f);
        return true;
    }

    internal PlayerSlot GetSlot(int index) => _roster.Slots[index];

    /// <summary>
    /// Joins the brother under scenario control. Used only by deterministic
    /// captures; real play goes through <see cref="UpdateSecondWardenJoin"/>.
    /// </summary>
    internal ScriptedInput JoinScriptedSecondWarden()
    {
        ScriptedInput scripted = new();
        return TryJoinSecondWarden(scripted) ? scripted : null;
    }

    /// <summary>Capture seam: force a Warden into the guttering state.</summary>
    internal void ForceWardenDown(int index)
    {
        if (index < 0 || index >= _roster.Count)
        {
            return;
        }

        Player warden = _roster.Slots[index].Warden;
        warden.ApplyDamage(GameBalance.PlayerMaxHealth, Vector2.Zero, _screenEffects);
        if (_roster.IsCooperative && AnyOtherStanding(index))
        {
            warden.Down();
            _healthLastFrame[index] = warden.Health;
            _combatPresentation.PresentWardenDown(warden.Position, warden.Identity.Flame);
        }
    }

    internal Vector2 SecondWardenPosition => _roster.Count > 1 ? _roster.Slots[1].Warden.Position : _player.Position;

    /// <summary>
    /// Arranges a deterministic co-op fixture. Real entities and real positions;
    /// combat timings, AI and damage remain owned by the systems under test.
    /// </summary>
    internal void ArrangeCoopSubject(string scenario)
    {
        _enemies.Clear();
        _souls.Clear();
        Vector2 centre = _arena.CombatBounds.Center.ToVector2();

        switch (scenario)
        {
            case "coop-idle":
            case "coop-reduced":
                PlaceWarden(0, centre + new Vector2(70f, 20f));
                PlaceWarden(1, centre + new Vector2(-70f, 20f));
                _enemies.Add(new Hollow(centre + new Vector2(300f, -60f), 1));
                _enemies.Add(new Devourer(centre + new Vector2(-330f, -40f)));
                break;

            case "coop-split-targets":
                PlaceWarden(0, centre + new Vector2(150f, 40f));
                PlaceWarden(1, centre + new Vector2(-150f, 40f));
                _enemies.Add(new Hollow(centre + new Vector2(255f, 30f), 1));
                _enemies.Add(new Hollow(centre + new Vector2(-255f, 30f), 2));
                break;

            case "coop-severance":
                PlaceWarden(0, centre + new Vector2(120f, 30f));
                PlaceWarden(1, centre + new Vector2(-120f, 30f));
                _enemies.Add(new Devourer(centre + new Vector2(0f, -20f)));
                break;

            case "coop-down":
            case "coop-stabilize":
                PlaceWarden(0, centre + new Vector2(150f, 30f));
                PlaceWarden(1, centre + new Vector2(-40f, 30f));
                _enemies.Add(new Hollow(centre + new Vector2(360f, -70f), 1));
                break;

            case "coop-separation":
                // Deliberately past the tether range, to show the strain and the
                // bounded zoom rather than a comfortable frame.
                PlaceWarden(0, centre + new Vector2(430f, -120f));
                PlaceWarden(1, centre + new Vector2(-430f, 150f));
                _enemies.Add(new Hollow(centre + new Vector2(520f, -80f), 1));
                _enemies.Add(new Hollow(centre + new Vector2(-520f, 190f), 2));
                break;

            case "coop-soul-release":
                PlaceWarden(0, centre + new Vector2(110f, 40f));
                PlaceWarden(1, centre + new Vector2(-110f, 40f));
                _enemies.Add(new Hollow(centre + new Vector2(0f, -110f), 1));
                break;

            case "coop-resonance":
                PlaceWarden(0, centre + new Vector2(90f, 30f));
                PlaceWarden(1, centre + new Vector2(-90f, 30f));
                _enemies.Add(new Devourer(centre + new Vector2(340f, -60f)));
                break;
        }
    }

    /// <summary>Capture seam: place both brothers at fixed points in the arena.</summary>
    internal void PlaceWarden(int index, Vector2 position)
    {
        if (index < 0 || index >= _roster.Count)
        {
            return;
        }

        Player warden = _roster.Slots[index].Warden;
        warden.Nudge(position - warden.Position, _arena.CombatBounds);
    }

    internal bool AnyWardenDowned
    {
        get
        {
            foreach (PlayerSlot slot in _roster.Slots)
            {
                if (slot.Warden.IsDowned)
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal float StabilizeProgress => _roster.Count > 1
        ? MathF.Max(_roster.Slots[0].Warden.StabilizeProgress, _roster.Slots[1].Warden.StabilizeProgress)
        : 0f;

    internal bool TeamResonanceReady => _team.IsReady;

    /// <summary>
    /// How the brother arrives during real play: any connected gamepad's Start or
    /// A button brings him in, and F12 brings him in on the number-pad layout so
    /// the design can be reviewed without controller hardware.
    /// </summary>
    private void UpdateSecondWardenJoin(InputState input)
    {
        if (_roster.IsCooperative)
        {
            return;
        }

        if (_autoJoinSecond)
        {
            PlayerIndex? autoPad = WardenRoster.FindFreeGamePad(input);
            TryJoinSecondWarden(autoPad.HasValue
                ? new GamePadInput(autoPad.Value)
                : new SecondaryKeyboardInput());
            return;
        }

        for (int index = 0; index < 4; index++)
        {
            PlayerIndex pad = (PlayerIndex)index;
            if (input.IsGamePadConnected(pad) &&
                (input.WasGamePadPressed(pad, Buttons.Start) || input.WasGamePadPressed(pad, Buttons.A)))
            {
                TryJoinSecondWarden(new GamePadInput(pad));
                return;
            }
        }

        if (input.WasKeyPressed(Keys.F12))
        {
            TryJoinSecondWarden(new SecondaryKeyboardInput());
        }
    }

    public void Update(GameTime gameTime, InputState input, Viewport viewport)
    {
        float deltaTime = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 1f / 20f);
        _presentationTime += deltaTime;
        _presentation.Update(deltaTime, _loopState);
        _audio.Update(deltaTime);
        _art.Update(deltaTime);
        _spriteVfx.Update(deltaTime);
        UpdateFps(deltaTime);
        _screenEffects.Update(deltaTime);
        _combatPresentation.Update(deltaTime);
        _arenaAtmosphere.Update(deltaTime, _loopState == ArenaLoopState.Complete);

        if (_loopState == ArenaLoopState.Title)
        {
            if (input.AnyInputPressed &&
                !input.WasKeyPressed(Keys.F9) &&
                !input.WasKeyPressed(Keys.F10) &&
                !input.WasKeyPressed(Keys.F11))
            {
                _audio.Play(AudioCue.TitleConfirm, 0.58f);
                _loopState = ArenaLoopState.Intro;
                _presentation.BeginIntro(false);
            }
            _soulSensePresentation.Update(deltaTime, false);
            _particles.Update(deltaTime);
            UpdateCamera(deltaTime, false, viewport);
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
            _team.Fill();
            SyncTeamResonance();
        }

        if (input.WasKeyPressed(Keys.F6))
        {
            foreach (Enemy enemy in _enemies.Where(enemy => enemy.IsAlive))
            {
                ApplyEnemyDamage(enemy, new DamageInfo(enemy.Health + enemy.MaxHealth, Vector2.Zero, enemy.Position));
            }
        }

        if (input.WasKeyPressed(Keys.F7))
        {
            _forceSoulSense = !_forceSoulSense;
        }

        if (input.WasKeyPressed(Keys.F8))
        {
            ResetEncounter();
        }

        UpdateSecondWardenJoin(input);

        if (PlayerDead && input.WasKeyPressed(Keys.R))
        {
            ResetEncounter();
        }

        if (_loopState == ArenaLoopState.Complete && input.WasKeyPressed(Keys.R))
        {
            ResetEncounter();
        }

        if (PlayerDead)
        {
            _soulSensePresentation.Update(deltaTime, false);
            _particles.Update(deltaTime);
            UpdateCamera(deltaTime, true, viewport);
            return;
        }

        if (_loopState == ArenaLoopState.Complete)
        {
            if (!_endingRevealPlayed && _presentation.StateTime >= CinematicPresentation.LifeFlameRevealTime)
            {
                _endingRevealPlayed = true;
                _audio.Play(AudioCue.EndingReveal, 0.72f);
            }
            UpdateArenaLoop(deltaTime);
            _soulSensePresentation.Update(deltaTime, false);
            _particles.Update(deltaTime);
            UpdateCamera(deltaTime, false, viewport);
            return;
        }

        if (_loopState == ArenaLoopState.Intro)
        {
            UpdateArenaLoop(deltaTime);
            _soulSensePresentation.Update(deltaTime, false);
            _particles.Update(deltaTime);
            UpdateCamera(deltaTime, false, viewport);
            return;
        }

        _roster.ReadCommands(input, _camera, viewport);
        _lastMouseWorld = _roster.LeadSlot.AimPoint;

        if (_screenEffects.IsHitStopped)
        {
            _soulSensePresentation.Update(deltaTime, AnySoulSenseActive());
            return;
        }

        UpdateWardens(deltaTime);
        _soulSensePresentation.Update(deltaTime, AnySoulSenseActive());
        UpdateCannonShots(deltaTime);
        UpdateBurningHandoff();
        ConfigureBurningAggression(deltaTime);

        // Targets are chosen once for the whole floor, before any enemy acts, so
        // the choice cannot depend on iteration order.
        _targeting.Update(deltaTime, _enemies, _roster.Field);
        foreach (Enemy enemy in _enemies)
        {
            HollowState? previousHollowState = enemy is Hollow hollowBefore ? hollowBefore.State : null;
            BurningState? previousBurningState = enemy is Burning burningBefore ? burningBefore.State : null;
            DevourerState? previousDevourerState = enemy is Devourer devourerBefore ? devourerBefore.State : null;
            _roster.Field.SetTarget(_targeting.TargetFor(enemy, _roster.Field));
            enemy.Update(deltaTime, _roster.Field, _souls, _arena.CombatBounds, _particles, _screenEffects);
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
                _audio.Play(AudioCue.SoulExposed, 0.5f);
            }

            if (enemy is Burning burning && burning.TryConsumeDetonation(out Vector2 detonationPosition))
            {
                ResolveBurningDetonation(burning, detonationPosition);
            }

            if (enemy is Devourer devourer && devourer.TryConsumeExtractionEffect(out Vector2 extractionPosition))
            {
                _spriteVfx.Spawn("soul_release", extractionPosition, 0f, 0.72f);
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
            soul.Update(deltaTime, _roster.Field, _particles);
            if (previousSoulState != SoulState.Releasing && soul.State == SoulState.Releasing)
            {
                _spriteVfx.Spawn("soul_release", soul.Position, 0f, 0.62f);
                _audio.Play(AudioCue.SoulRelease, 0.62f);
            }
            if (soul.TryConsumeResidueReceiver(out Player _))
            {
                // Residue always feeds the brothers' shared pool, so there is
                // nothing for two Players to race each other for.
                AddTeamResonance(GameBalance.ResonancePerSoulRelease);
            }
        }

        _souls.RemoveAll(soul => soul.IsFinished);
        ResolveWardenCasualties(deltaTime);
        UpdateArenaLoop(deltaTime);
        _particles.Update(deltaTime);
        UpdateCamera(deltaTime, false, viewport);
    }

    private readonly record struct WardenSnapshot(
        bool Dashing,
        bool ResonanceActive,
        bool ResonanceReady,
        bool SoulSense,
        bool CannonFull,
        SoulCannonState CannonState,
        int Health,
        bool Downed,
        bool Dead);

    private static WardenSnapshot Snapshot(Player warden) => new(
        warden.IsDashing,
        warden.ResonanceActive,
        warden.IsResonanceReady,
        warden.SoulSenseActive,
        warden.Cannon.IsFullCharge,
        warden.Cannon.State,
        warden.Health,
        warden.IsDowned,
        warden.IsDead);

    private bool AnySoulSenseActive()
    {
        foreach (PlayerSlot slot in _roster.Slots)
        {
            if (slot.Warden.SoulSenseActive)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Runs every local Warden through one frame: intent, stabilisation, combat
    /// resolution and feedback. Solo is the one-slot case of exactly this loop,
    /// so there is no separate single-player path to drift out of sync.
    /// </summary>
    private void UpdateWardens(float deltaTime)
    {
        bool teamResonanceReadyBefore = _team.IsReady;

        foreach (PlayerSlot slot in _roster.Slots)
        {
            Player warden = slot.Warden;
            WardenSnapshot before = Snapshot(warden);

            Player rescue = FindStabilizeTarget(slot);
            slot.StabilizeTarget = rescue;
            bool stabilizing = rescue is not null && slot.Command.StabilizeHeld;

            warden.Update(
                deltaTime,
                slot.Command,
                _arena.CombatBounds,
                _particles,
                _screenEffects,
                _forceSoulSense,
                stabilizing);

            if (rescue is not null)
            {
                AdvanceStabilization(deltaTime, warden, rescue, stabilizing);
            }

            if (_audioTestFatalDamageRequested)
            {
                // Applied to every Warden, so the check exercises the real
                // failure condition in both modes: solo death, or both brothers
                // down at once.
                warden.ApplyDamage(GameBalance.PlayerMaxHealth, Vector2.Zero, _screenEffects);
            }

            TryOpenSeveranceWindow(warden);
            SpawnCannonShot(warden);
            ResolveScytheStrike(warden);
            PresentWardenFrame(slot, before);
        }

        _audioTestFatalDamageRequested = false;
        ResolveTeamResonanceRequests();
        UpdateWardenSeparation(deltaTime);
        UpdateWardenTether(deltaTime);
        SyncTeamResonance();

        if (!teamResonanceReadyBefore && _team.IsReady)
        {
            _audio.Play(AudioCue.ResonanceReady, 0.72f);
        }
    }

    /// <summary>
    /// Feedback for one Warden's frame. Kept per slot so a second brother's dash,
    /// charge and Resonance read as his own rather than doubling Player 1's.
    /// </summary>
    private void PresentWardenFrame(PlayerSlot slot, WardenSnapshot before)
    {
        Player warden = slot.Warden;

        if (warden.Scythe.StartedThisFrame)
        {
            _combatPresentation.PresentScytheSwing(
                warden.Scythe.ActiveStep,
                warden.Position,
                warden.Scythe.AttackDirection);
        }

        if (!before.Dashing && warden.IsDashing)
        {
            _spriteVfx.Spawn(
                "dash_ignition",
                warden.Position - warden.DashDirection * 30f,
                MathF.Atan2(warden.DashDirection.Y, warden.DashDirection.X),
                0.46f,
                warden.Identity.FlameBright * 0.5f);
        }

        if (!before.ResonanceActive && warden.ResonanceActive)
        {
            _combatPresentation.BeginResonance(warden.Position);
            _arenaAtmosphere.ReactToResonance();
        }

        PlayWardenActionAudio(slot, before);

    }

    /// <summary>
    /// Either brother may spend the shared pool, and spending it lights both.
    /// This is the cooperative shape of the mechanic: Resonance is agreement, so
    /// it cannot be hoarded by one Warden.
    /// </summary>
    private void ResolveTeamResonanceRequests()
    {
        bool requested = false;
        foreach (PlayerSlot slot in _roster.Slots)
        {
            requested |= slot.Warden.ResonanceRequestedThisFrame && !slot.Warden.ResonanceActive;
        }

        if (!requested || !_team.TrySpendForResonance())
        {
            return;
        }

        foreach (PlayerSlot slot in _roster.Slots)
        {
            if (slot.Warden.CanBeTargeted)
            {
                slot.Warden.StartResonance();
            }
        }

        _audio.Play(AudioCue.ResonanceActivate, 0.88f);
    }

    private void SyncTeamResonance()
    {
        foreach (PlayerSlot slot in _roster.Slots)
        {
            slot.Warden.SyncResonance(_team.Charge);
        }
    }

    private void AddTeamResonance(float amount)
    {
        _team.Add(amount);
        SyncTeamResonance();
    }

    /// <summary>
    /// The downed brother this Warden is standing over, or null. Only one brother
    /// can be reached at a time and never himself.
    /// </summary>
    private Player FindStabilizeTarget(PlayerSlot slot)
    {
        if (!slot.Warden.CanBeTargeted)
        {
            return null;
        }

        foreach (PlayerSlot other in _roster.Slots)
        {
            if (other.Index == slot.Index || !other.Warden.IsDowned)
            {
                continue;
            }

            float reach = GameBalance.StabilizeRange;
            if (Vector2.DistanceSquared(slot.Warden.Position, other.Warden.Position) <= reach * reach)
            {
                return other.Warden;
            }
        }

        return null;
    }

    private void AdvanceStabilization(float deltaTime, Player rescuer, Player downed, bool held)
    {
        if (held)
        {
            // The danger window made visible: a thin line of the rescuer's own
            // flame poured into his brother while he cannot fight.
            _combatPresentation.PresentStabilizeHold(
                rescuer.Position,
                downed.Position,
                rescuer.Identity.FlameBright,
                downed.StabilizeProgress);
        }

        if (!downed.AdvanceStabilization(deltaTime, held, _team.StabilizeSeconds))
        {
            return;
        }

        _team.ChargeStabilization();
        SyncTeamResonance();
        _combatPresentation.PresentStabilizeComplete(downed.Position, downed.Identity.FlameBright);
        _audio.Play(AudioCue.WardenStabilize, 0.86f);
    }

    /// <summary>
    /// End-of-frame casualty pass. Damage is applied by enemies after the Wardens
    /// have already run, so health is compared here against the previous frame —
    /// the same ordering the single-player version used.
    ///
    /// A Warden who runs out of health while a brother is still standing goes
    /// down instead of dying. Both down ends the encounter. Solo never enters the
    /// downed state at all, so death and retry are unchanged.
    /// </summary>
    private void ResolveWardenCasualties(float deltaTime)
    {
        for (int index = 0; index < _roster.Count; index++)
        {
            Player warden = _roster.Slots[index].Warden;
            int previousHealth = _healthLastFrame[index];
            _healthLastFrame[index] = warden.Health;

            if (warden.Health >= previousHealth)
            {
                continue;
            }

            if (warden.Health > 0)
            {
                _audio.Play(AudioCue.PlayerHit, 0.6f);
                continue;
            }

            if (_roster.IsCooperative && AnyOtherStanding(index))
            {
                warden.Down();
                _healthLastFrame[index] = warden.Health;
                _combatPresentation.PresentWardenDown(warden.Position, warden.Identity.Flame);
                _audio.Play(AudioCue.WardenDown, 0.8f);
            }
        }

        foreach (PlayerSlot slot in _roster.Slots)
        {
            Player warden = slot.Warden;
            if (warden.IsDowned && warden.DownRemaining <= 0f)
            {
                warden.ExtinguishFlame();
            }
        }

        if (_teamWiped || !_roster.AllDown())
        {
            return;
        }

        // Both flames are out or going out. The encounter is over; the lead
        // Warden carries the existing death presentation so retry is unchanged.
        _teamWiped = true;
        foreach (PlayerSlot slot in _roster.Slots)
        {
            if (slot.Warden.IsDowned)
            {
                slot.Warden.ExtinguishFlame();
            }
        }

        _audio.SetCalm(true);
        _audio.SetSoulSense(false);
        _presentation.BeginDeath();
        _audio.Play(AudioCue.PlayerDeath, 0.78f);
    }

    private bool AnyOtherStanding(int index)
    {
        for (int other = 0; other < _roster.Count; other++)
        {
            if (other != index && _roster.Slots[other].Warden.CanBeTargeted)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Keeps two brothers from occupying the same point. Without this, melee in
    /// co-op degenerates into body blocking: both Wardens end up inside each
    /// other and neither can read which silhouette is theirs.
    /// </summary>
    private void UpdateWardenSeparation(float deltaTime)
    {
        if (!_roster.IsCooperative)
        {
            return;
        }

        Player first = _roster.Slots[0].Warden;
        Player second = _roster.Slots[1].Warden;
        if (first.IsDead || second.IsDead)
        {
            return;
        }

        Vector2 delta = second.Position - first.Position;
        float distance = delta.Length();
        // A brother on the floor still occupies space. Without this the rescuer
        // stands exactly on top of him and the whole rescue is invisible.
        bool eitherDowned = first.IsDowned || second.IsDowned;
        float minimum = eitherDowned
            ? GameBalance.WardenSeparationRadius * 2.4f
            : GameBalance.WardenSeparationRadius * 2f;
        if (distance >= minimum || distance <= 0.0001f)
        {
            return;
        }

        Vector2 push = delta / distance * (minimum - distance);
        float step = MathHelper.Clamp(deltaTime * GameBalance.WardenSeparationStrength, 0f, 1f);
        // A guttering Warden cannot be shoved around; the standing brother yields.
        if (first.IsDowned)
        {
            second.Nudge(push * step, _arena.CombatBounds);
        }
        else if (second.IsDowned)
        {
            first.Nudge(-push * step, _arena.CombatBounds);
        }
        else
        {
            first.Nudge(-push * (step * 0.5f), _arena.CombatBounds);
            second.Nudge(push * (step * 0.5f), _arena.CombatBounds);
        }
    }

    /// <summary>
    /// The brothers share one Death Flame. Past the tether range it strains, and
    /// past the limit the trailing Warden is drawn back — gradually, never
    /// teleported — which is what keeps the group camera inside a readable zoom.
    /// </summary>
    private void UpdateWardenTether(float deltaTime)
    {
        if (!_roster.IsCooperative)
        {
            TetherStrain = 0f;
            return;
        }

        Player first = _roster.Slots[0].Warden;
        Player second = _roster.Slots[1].Warden;
        Vector2 delta = second.Position - first.Position;
        float distance = delta.Length();
        TetherStrain = MathHelper.Clamp(
            (distance - GameBalance.CoopTetherRange) / (GameBalance.CoopTetherLimit - GameBalance.CoopTetherRange),
            0f,
            1f);

        if (distance <= GameBalance.CoopTetherLimit || distance <= 0.0001f)
        {
            return;
        }

        Vector2 direction = delta / distance;
        float pull = GameBalance.CoopTetherPull * deltaTime;
        if (first.CanBeTargeted)
        {
            first.Nudge(direction * (pull * 0.5f), _arena.CombatBounds);
        }
        if (second.CanBeTargeted)
        {
            second.Nudge(-direction * (pull * 0.5f), _arena.CombatBounds);
        }
    }

    /// <summary>Strain on the shared flame, 0 to 1. Drives the tether presentation.</summary>
    public float TetherStrain { get; private set; }

    /// <summary>
    /// Group camera. One Warden behaves exactly as Session 1 did; two Wardens
    /// share a frame whose zoom is bounded on both ends, so the pair is always
    /// visible without the arena ever becoming a diagram again.
    /// </summary>
    private void UpdateCamera(float deltaTime, bool anyoneDead, Viewport viewport)
    {
        float groupZoom = GameBalance.CombatCameraZoom;
        if (_roster.IsCooperative)
        {
            Vector2 span = _roster.FrameSpan();
            float needX = span.X + GameBalance.CoopFramePadding;
            float needY = span.Y + GameBalance.CoopFramePadding;
            float fit = MathF.Min(viewport.Width / MathF.Max(needX, 1f), viewport.Height / MathF.Max(needY, 1f));
            groupZoom = MathHelper.Clamp(fit, GameBalance.CoopMinCameraZoom, GameBalance.CombatCameraZoom);
        }

        _presentation.UpdateCamera(
            _camera,
            _loopState,
            anyoneDead,
            _roster.FrameCentre(),
            FrameVelocity(),
            FrameFacing(),
            ThreatCentre(),
            _arena.Bounds,
            _arena.CombatBounds,
            viewport,
            deltaTime,
            groupZoom,
            _screenEffects.ZoomPunch);
    }

    /// <summary>Average movement of the standing Wardens. Drives camera look-ahead.</summary>
    private Vector2 FrameVelocity()
    {
        Vector2 total = Vector2.Zero;
        int count = 0;
        foreach (PlayerSlot slot in _roster.Slots)
        {
            if (slot.Warden.CanBeTargeted)
            {
                total += slot.Warden.Velocity;
                count++;
            }
        }

        return count == 0 ? Vector2.Zero : total / count;
    }

    /// <summary>
    /// Where the Wardens are looking. The smoothed body facing is used rather
    /// than the raw aim, so a mouse flick cannot snap the frame.
    /// </summary>
    private Vector2 FrameFacing()
    {
        Vector2 total = Vector2.Zero;
        foreach (PlayerSlot slot in _roster.Slots)
        {
            if (slot.Warden.CanBeTargeted)
            {
                total += slot.Warden.BodyFacing;
            }
        }

        return total;
    }

    /// <summary>
    /// The centroid of the fight the camera should acknowledge: living
    /// manifestations near enough to matter, with a committed attacker counted
    /// twice because that is the one about to arrive.
    /// </summary>
    private Vector2? ThreatCentre()
    {
        if (_loopState != ArenaLoopState.Combat)
        {
            return null;
        }

        Vector2 centre = _roster.FrameCentre();
        Vector2 total = Vector2.Zero;
        float weight = 0f;
        foreach (Enemy enemy in _enemies)
        {
            if (!enemy.IsAlive || Vector2.DistanceSquared(enemy.Position, centre) > 560f * 560f)
            {
                continue;
            }

            float w = enemy.CommitmentRemaining >= 0f ? 2f : 1f;
            total += enemy.Position * w;
            weight += w;
        }

        return weight <= 0f ? null : total / weight;
    }

    public void Dispose()
    {
        _audio.Dispose();
        GC.SuppressFinalize(this);
    }

    internal void RequestAudioTestFatalDamage() => _audioTestFatalDamageRequested = true;

    public void Draw(SpriteBatch batch, Texture2D pixel, Viewport viewport, SoulfireRenderer renderer)
    {
        renderer.BeginScene(viewport);
        DrawScene(batch, pixel, viewport);
        DrawSoulfireLighting(batch, renderer, viewport);
        renderer.DrawVignette(batch, viewport, _soulSensePresentation.WorldSuppression, _player.ResonanceActive);
        DrawScreenFeedback(batch, pixel, viewport);
        _soulSensePresentation.DrawSoulLayer(
            batch,
            pixel,
            _camera.GetTransform(viewport, _screenEffects.CameraOffset),
            _player,
            _enemies,
            _souls,
            _presentationTime);
        DrawHud(batch, pixel, viewport);
    }

    private void DrawScene(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        Matrix worldTransform = _camera.GetTransform(viewport, _screenEffects.CameraOffset);
        batch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            transformMatrix: worldTransform);

        _art.DrawArena(batch);
        ArenaComposition.DrawGround(batch, pixel, _presentationTime, _soulSensePresentation.WorldSuppression);
        ArenaComposition.DrawProps(batch, pixel, _presentationTime, _soulSensePresentation.WorldSuppression);
        _arenaAtmosphere.DrawBackground(batch, pixel, _soulSensePresentation.WorldSuppression);
        DrawArenaLoop(batch, pixel);
        foreach (Enemy enemy in _enemies)
        {
            if (!enemy.IsAlive) continue;
            float width = enemy is Devourer ? 46f : enemy is Burning ? 25f : 21f;
            float foot = enemy is Devourer ? 65f : 43f;
            ArenaComposition.DrawContactShadow(batch, pixel, enemy.Position + new Vector2(0f, foot), width);
        }
        if (_presentation.ShouldDrawPlayer(_loopState, PlayerDead))
        {
            foreach (PlayerSlot slot in _roster.Slots)
            {
                if (slot.Warden.IsDead) continue;
                ArenaComposition.DrawContactShadow(batch, pixel, slot.Warden.Position + new Vector2(0f, 40f), 21f);
            }
        }
        batch.End();

        // Actor light sits between the room and the fighting plane, so every
        // silhouette is lit from behind rather than pasted onto the floor.
        DrawActorLight(batch, worldTransform);

        // Threat cues are light on the floor, so they belong *under* the actors.
        // They were drawn last, over everything, which meant a Hollow's swipe
        // band was painted straight across the Warden standing in it: the frame
        // where the Player most needs to read his own position was the frame
        // where he was hardest to see. Same cues, same alpha, correct layer.
        batch.Begin(SpriteSortMode.Deferred, SoftShapes.AdditiveLight, SamplerState.LinearClamp,
            transformMatrix: worldTransform);
        ThreatPresentation.Draw(batch, _art.SoftBrush, _enemies, _presentationTime);
        batch.End();

        batch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            transformMatrix: worldTransform);

        foreach (Enemy enemy in _enemies)
        {
            _art.DrawEnemy(batch, enemy);
            enemy.Draw(batch, pixel, _debugVisible, false, true);
        }
        foreach (Soul soul in _souls)
        {
            _art.DrawLostSoul(batch, soul);
            soul.Draw(batch, pixel, false, true);
        }
        foreach (CannonShot shot in _cannonShots)
        {
            shot.Draw(batch, pixel, true);
            _art.DrawCannonProjectile(batch, shot);
        }
        _particles.Draw(batch, pixel);
        if (_presentation.ShouldDrawPlayer(_loopState, PlayerDead))
        {
            foreach (PlayerSlot slot in _roster.Slots)
            {
                if (slot.Warden.IsDowned) DrawWarden(batch, pixel, slot.Warden);
            }
            foreach (PlayerSlot slot in _roster.Slots)
            {
                if (!slot.Warden.IsDowned) DrawWarden(batch, pixel, slot.Warden);
            }
        }
        _presentation.DrawWorldAccents(batch, pixel, _art, _loopState, PlayerDead, _player, _arena.CombatBounds);
        _spriteVfx.DrawAlpha(batch);
        batch.End();

        DrawCombatLight(batch, worldTransform);
        _spriteVfx.DrawAdditive(batch, worldTransform);

        batch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            transformMatrix: worldTransform);

        ArenaComposition.DrawForeground(batch, pixel);

        if (_debugVisible)
        {
            batch.DrawRectangle(pixel, _arena.CombatBounds, new Color(80, 220, 210) * 0.8f, 3f);
            Vector2 center = _arena.CombatBounds.Center.ToVector2();
            batch.DrawLine(pixel, center - Vector2.UnitX * 28f, center + Vector2.UnitX * 28f, new Color(80, 220, 210), 2f);
            batch.DrawLine(pixel, center - Vector2.UnitY * 28f, center + Vector2.UnitY * 28f, new Color(80, 220, 210), 2f);
        }

        batch.End();
    }

    /// <summary>
    /// One Warden's body, weapon and charge glow. Both brothers use the same
    /// sheet; the body tint and the Cannon's charge colour come from the
    /// identity, which is the whole of the visual difference in the alpha pass.
    /// </summary>
    private void DrawWarden(SpriteBatch batch, Texture2D pixel, Player warden)
    {
        if (warden.IsDead)
        {
            return;
        }

        _art.DrawPlayer(batch, warden);
        warden.Draw(batch, pixel, _art, _debugVisible, _soulSensePresentation.SoulEmergence);
        if (warden.Cannon.State != SoulCannonState.Charging)
        {
            return;
        }

        Vector2 muzzle = warden.Position + warden.FacingDirection * 74f;
        float charge = warden.Cannon.ChargeProgress;
        Color chargeColor = warden.Cannon.IsFullCharge
            ? Color.White
            : warden.Cannon.ChargeStage >= 3
                ? warden.Identity.FlameBright
                : warden.Cannon.ChargeStage == 2
                    ? warden.Identity.Accent
                    : warden.Identity.Flame;
        _art.DrawLoopingEffect(
            batch,
            warden.Cannon,
            "cannon_charge_loop",
            muzzle,
            0f,
            warden.Cannon.IsFullCharge ? 0.68f : MathHelper.Lerp(0.28f, 0.61f, charge),
            chargeColor);
    }

    /// <summary>
    /// Silhouette and contact light for every actor, drawn under the sprites.
    /// See <see cref="ActorLighting"/> for the grammar this establishes.
    /// </summary>
    private void DrawActorLight(SpriteBatch batch, Matrix worldTransform)
    {
        batch.Begin(
            SpriteSortMode.Deferred,
            SoftShapes.AdditiveLight,
            SamplerState.LinearClamp,
            transformMatrix: worldTransform);

        ActorLighting.DrawManifestations(batch, _art, _enemies, _presentationTime);
        if (_presentation.ShouldDrawPlayer(_loopState, PlayerDead))
        {
            foreach (PlayerSlot slot in _roster.Slots)
            {
                if (slot.Warden.IsDead) continue;
                ActorLighting.DrawWarden(batch, _art, _art.SoftBrush, slot.Warden, _presentationTime);
            }
        }

        batch.End();
    }

    /// <summary>
    /// The shared Death Flame between the brothers. Invisible while they fight
    /// together; it only appears as it strains, which is how separation is
    /// communicated without a marker or an off-screen arrow.
    /// </summary>
    private void DrawWardenTether(SpriteBatch batch, Texture2D brush)
    {
        if (!_roster.IsCooperative || TetherStrain <= 0.01f)
        {
            return;
        }

        Player first = _roster.Slots[0].Warden;
        Player second = _roster.Slots[1].Warden;
        if (!first.CanBeTargeted || !second.CanBeTargeted)
        {
            return;
        }

        Vector2 delta = second.Position - first.Position;
        float distance = delta.Length();
        if (distance < 1f)
        {
            return;
        }

        Vector2 direction = delta / distance;
        int steps = Math.Max(8, (int)(distance / 34f));
        float strain = TetherStrain;
        for (int i = 0; i < steps; i++)
        {
            float amount = (i + 0.5f) / steps;
            float taper = MathF.Sin(amount * MathHelper.Pi);
            Vector2 point = first.Position + direction * (distance * amount)
                + new Vector2(-direction.Y, direction.X) * MathF.Sin(amount * 9f + _presentationTime * 3f) * (7f * strain);
            Color flame = Color.Lerp(first.Identity.Flame, second.Identity.Flame, amount);
            SoftShapes.Blob(batch, brush, point, (5f + strain * 5f) * taper, flame * (0.3f * strain * taper));
        }
    }

    /// <summary>
    /// Every piece of combat feedback that used to be a hard vector stroke is
    /// painted here with the feathered brush, additively, in world space. Keeping
    /// it in one pass means combat light can never be point-sampled into crisp
    /// edges by the pixel-art sampler used for sprites and the floor.
    /// </summary>
    private void DrawCombatLight(SpriteBatch batch, Matrix worldTransform)
    {
        batch.Begin(
            SpriteSortMode.Deferred,
            SoftShapes.AdditiveLight,
            SamplerState.LinearClamp,
            transformMatrix: worldTransform);

        Texture2D brush = _art.SoftBrush;
        bool sense = AnySoulSenseActive();

        foreach (Enemy enemy in _enemies)
        {
            switch (enemy)
            {
                case Hollow hollow:
                    hollow.DrawCombatLight(batch, brush, sense);
                    break;
                case Burning burning:
                    burning.DrawCombatLight(batch, brush, sense);
                    break;
                case Devourer devourer:
                    devourer.DrawCombatLight(batch, brush, sense);
                    break;
            }
        }

        foreach (Soul soul in _souls)
        {
            soul.DrawCombatLight(batch, brush, sense);
        }

        if (_loopState != ArenaLoopState.Title)
        {
            foreach (PlayerSlot slot in _roster.Slots)
            {
                slot.Warden.DrawAfterimages(batch, brush);
            }
        }

        if (_presentation.ShouldDrawPlayer(_loopState, PlayerDead))
        {
            foreach (PlayerSlot slot in _roster.Slots)
            {
                slot.Warden.DrawCombatLight(batch, brush, _soulSensePresentation.SoulEmergence);
            }
        }

        DrawWardenTether(batch, brush);
        _combatPresentation.DrawStabilizeLink(batch, brush, _presentationTime);

        // Aim mark: a soft ember where the Warden is looking, not a crosshair.
        if (_presentation.ShouldDrawAim(_loopState, PlayerDead))
        {
            float breathe = 0.72f + 0.28f * MathF.Sin(_presentationTime * 5.4f);
            foreach (PlayerSlot slot in _roster.Slots)
            {
                if (!slot.Warden.CanBeTargeted) continue;
                Vector2 mark = slot.AimPoint;
                SoftShapes.Blob(batch, brush, mark, 17f * breathe, slot.Identity.Flame * 0.2f);
                SoftShapes.Blob(batch, brush, mark, 5f * breathe, slot.Identity.FlameBright * 0.38f);
            }
        }

        batch.End();
    }

    private void DrawSoulfireLighting(SpriteBatch batch, SoulfireRenderer renderer, Viewport viewport)
    {
        renderer.BeginEmission(viewport);
        SoulfireLighting.Draw(
            batch,
            renderer,
            _camera.GetTransform(viewport, _screenEffects.CameraOffset),
            _player,
            _enemies,
            _souls,
            _cannonShots,
            _particles,
            _spriteVfx,
            _arenaAtmosphere,
            _presentationTime,
            _soulSensePresentation.SoulEmergence,
            _loopState == ArenaLoopState.Complete,
            _presentation.GetLifeFlamePosition(_arena.CombatBounds),
            _presentation.GetLifeFlameAlpha());
        renderer.CompositeEmission(batch, viewport, _soulSensePresentation.WorldSuppression);
    }

    private void DrawScreenFeedback(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        if (_screenEffects.ImpactFrameAlpha > 0f)
        {
            batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * (_screenEffects.ImpactFrameAlpha * 0.3f));
        }

        if (_screenEffects.FlashAlpha > 0f)
        {
            batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), _screenEffects.FlashColor * _screenEffects.FlashAlpha);
        }

        if (_player.ResonanceActivationRemaining > 0f)
        {
            float activationFade = MathHelper.Clamp(_player.ResonanceActivationRemaining / 0.5f, 0f, 1f);
            batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * (activationFade * 0.1f * _presentationSettings.FlashScale));
        }

        batch.End();
    }

    private void DrawHud(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

        if (_presentation.ShouldDrawCombatHud(_loopState, PlayerDead))
        {
            _hud.Draw(batch, pixel, viewport, _roster, _team);
        }

        _presentation.DrawOverlay(batch, pixel, viewport, _loopState, PlayerDead, _waveNumber);

        if (_debugVisible && _loopState != ArenaLoopState.Title)
        {
            DrawDebugOverlay(batch, pixel, viewport);
        }

        batch.End();
    }

    /// <summary>
    /// Opens a Severance Window when the Player dashes into a committed attack
    /// late enough to be a read. The window is granted by the enemy's commitment,
    /// not by a global cooldown, which keeps it valid for a second Warden later.
    /// </summary>
    private void TryOpenSeveranceWindow(Player warden)
    {
        if (!warden.DashStartedThisFrame || !warden.CanBeTargeted)
        {
            return;
        }

        foreach (Enemy enemy in _enemies)
        {
            if (!enemy.IsAlive)
            {
                continue;
            }

            float remaining = enemy.CommitmentRemaining;
            if (remaining < -GameBalance.SeveranceLateGrace || remaining > GameBalance.SeveranceReadTime)
            {
                continue;
            }

            float threat = enemy.CommitmentThreatRange + warden.Radius + GameBalance.SeveranceThreatPadding;
            if (Vector2.DistanceSquared(enemy.Position, warden.Position) > threat * threat)
            {
                continue;
            }

            // Each Warden holds his own window against the same telegraph, so two
            // brothers can both read one Devourer slam without stealing it from
            // each other.
            warden.OpenSeveranceWindow();
            _combatPresentation.PresentSeveranceWindow(warden.Position, enemy.AnchorPosition);
            _audio.Play(AudioCue.SeveranceWindow, 0.62f);
            return;
        }
    }

    private void ResolveScytheStrike(Player warden)
    {
        if (warden.Scythe.ConsumedSeveranceThisFrame)
        {
            warden.ConsumeSeveranceWindow();
        }

        if (!warden.Scythe.TryConsumeStrike(out ScytheStrike strike))
        {
            return;
        }

        bool hitAnything = false;
        bool severedAnything = false;
        foreach (Enemy enemy in _enemies.Where(enemy => enemy.IsAlive))
        {
            Vector2 toTarget = enemy.Position - warden.Position;
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

            // A Severance cut finds the Anchor without Soul Sense. That is the
            // reward: the read replaces the resource the Player would otherwise
            // have to be already spending.
            Vector2 weakPoint = strike.IsSeverance ? enemy.AnchorPosition : FindStrikeWeakPoint(warden, enemy, strike);
            bool coreHit = strike.IsSeverance || (warden.SoulSenseActive && weakPoint != Vector2.Zero);
            int damage = coreHit && !strike.IsSeverance
                ? (int)MathF.Round(strike.Damage * GameBalance.SoulSenseCoreDamageMultiplier)
                : strike.Damage;
            ApplyEnemyDamage(enemy, new DamageInfo(
                damage,
                targetDirection * strike.Knockback,
                coreHit ? weakPoint : enemy.Position,
                coreHit));

            if (strike.IsSeverance)
            {
                enemy.ApplySeverance();
                _combatPresentation.PresentSeveranceCut(weakPoint, targetDirection);
                AddTeamResonance(GameBalance.SeveranceResonanceGain);
                _arenaAtmosphere.ReactToForce(weakPoint, 300f, 88f);
                severedAnything = true;
            }

            Vector2 contactPosition = coreHit
                ? weakPoint
                : enemy.Position - targetDirection * enemy.Radius * 0.35f;
            _combatPresentation.SpawnScytheContact(
                strike.Step,
                contactPosition,
                targetDirection,
                coreHit);
            if (coreHit && !strike.IsSeverance)
            {
                AddTeamResonance(GameBalance.ResonancePerCoreHit);
                _audio.Play(AudioCue.CoreHit, 0.7f);
            }
            hitAnything = true;
        }

        if (severedAnything)
        {
            _audio.Play(AudioCue.SeveranceCut, 0.92f);
            return;
        }

        if (!hitAnything)
        {
            return;
        }

        _combatPresentation.PresentScytheImpact(strike.Step, strike.Direction);
        _audio.Play(AudioCue.ScytheHit, strike.Step == 3 ? 0.72f : 0.48f, strike.Step == 2 ? 0.08f : 0f);
    }

    private void BeginBeat(int beatNumber)
    {
        _waveNumber = beatNumber;
        _loopState = ArenaLoopState.Combat;
        _director.BeginBeat(beatNumber);
        _burningHandoffTimer = 0f;
        _burningCommittedLastFrame = 0;
        _screenEffects.AddShake(0.14f, 3f + beatNumber);
        _screenEffects.Flash(0.07f, 0.09f + beatNumber * 0.025f);
        _audio.SetCalm(false);
        _audio.Play(AudioCue.WaveStart, 0.6f, MathF.Min(0.18f, beatNumber * 0.03f));
        UpdateEncounterSpawns(0f);
    }

    /// <summary>
    /// Releases the beat's staged arrivals. Each Lost Soul enters through its own
    /// Death Flame rather than appearing, so the Player can read where pressure is
    /// coming from before it is on top of them.
    /// </summary>
    private void UpdateEncounterSpawns(float deltaTime)
    {
        bool floorEmpty = !_enemies.Any(enemy => enemy.IsAlive) && _souls.Count == 0;
        _director.Update(deltaTime, floorEmpty, _releasedSpawns);
        if (_releasedSpawns.Count == 0)
        {
            return;
        }

        Vector2 center = _arena.CombatBounds.Center.ToVector2();
        foreach (EncounterSpawn spawn in _releasedSpawns)
        {
            Vector2 position = center + spawn.Offset;
            Enemy arrival = spawn.Role switch
            {
                EncounterRole.Hollow => new Hollow(position, spawn.Seed),
                EncounterRole.Burning => new Burning(position, spawn.Seed),
                _ => new Devourer(position)
            };

            if (arrival is Devourer devourer && spawn.HeldSouls > 0)
            {
                for (int i = 0; i < spawn.HeldSouls; i++)
                {
                    Soul held = new(position);
                    devourer.SeedHeldSoul(held);
                    _souls.Add(held);
                }
            }

            _enemies.Add(arrival);
            PresentArrival(position, arrival);
        }
    }

    private void PresentArrival(Vector2 position, Enemy arrival)
    {
        bool heavy = arrival is Devourer;
        _spriteVfx.Spawn("dash_ignition", position, 0f, heavy ? 1.15f : 0.7f, GameBalance.DeathFlame * 0.6f);
        _particles.EmitDeathFlame(position, heavy ? 22 : 11, heavy ? 1.45f : 1f);
        _particles.EmitConvergence(position, heavy ? 20 : 12, heavy ? 128f : 84f, GameBalance.DeathFlameBright, 0.3f, heavy ? 6f : 4f);
        _arenaAtmosphere.ReactToForce(position, heavy ? 320f : 190f, heavy ? 96f : 54f);
        _screenEffects.AddShake(heavy ? 0.2f : 0.07f, heavy ? 6.5f : 1.6f);
        _screenEffects.AddZoomPunch(heavy ? 0.6f : 0.14f);
    }

    private void ResetEncounter()
    {
        _roster.Reset(_arena.CombatBounds.Center.ToVector2());
        _team.Reset();
        _targeting.Reset();
        _teamWiped = false;
        Array.Fill(_healthLastFrame, GameBalance.PlayerMaxHealth);
        SyncTeamResonance();
        _enemies.Clear();
        _souls.Clear();
        _cannonShots.Clear();
        _particles.Clear();
        _spriteVfx.Clear();
        _combatPresentation.Clear();
        _screenEffects.Clear();
        _arenaAtmosphere.Reset();
        _director.Reset();
        _releasedSpawns.Clear();
        _waveNumber = 0;
        _loopState = ArenaLoopState.Intro;
        _presentation.BeginIntro(true);
        _burningHandoffTimer = 0f;
        _burningCommittedLastFrame = 0;
        _forceSoulSense = false;
        _soulSensePresentation.Reset();
        _audioTestFatalDamageRequested = false;
        _endingRevealPlayed = false;
        _audio.SetCalm(false);
        _audio.SetSoulSense(false);
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
            .OrderBy(burning => Vector2.DistanceSquared(burning.Position, NearestWardenPosition(burning.Position)))
            .Take(maximumCommitments - committed))
        {
            burning.SetAggressionSlot(true);
        }
    }

    private Vector2 NearestWardenPosition(Vector2 from)
    {
        Player nearest = _roster.Field.ClosestStanding(from);
        return nearest?.Position ?? _player.Position;
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
        // Character-study fixtures hold the beat open. Nothing else reads this
        // flag, and it is only ever set from the visual-capture seam.
        if (_visualSandbox)
        {
            return;
        }

        switch (_loopState)
        {
            case ArenaLoopState.Intro:
                if (_presentation.TransitionComplete)
                {
                    BeginBeat(_waveNumber + 1);
                }
                break;

            case ArenaLoopState.Transition:
                if (_presentation.WaveTransitionComplete)
                {
                    BeginBeat(_waveNumber + 1);
                }
                break;

            case ArenaLoopState.Combat:
                UpdateEncounterSpawns(deltaTime);
                if (_enemies.Count == 0 && _souls.Count == 0 && _director.AllSpawnsReleased)
                {
                    _audio.Play(AudioCue.WaveClear, _waveNumber >= EncounterDirector.BeatCount ? 0.74f : 0.62f);
                    if (_waveNumber >= EncounterDirector.BeatCount)
                    {
                        _loopState = ArenaLoopState.Complete;
                        foreach (PlayerSlot slot in _roster.Slots)
                        {
                            slot.Warden.SettleForCompletion();
                        }
                        _cannonShots.Clear();
                        _presentation.BeginCompletion();
                        _endingRevealPlayed = false;
                        _audio.SetCalm(true);
                        _audio.SetSoulSense(false);
                    }
                    else
                    {
                        _loopState = ArenaLoopState.Transition;
                        _presentation.BeginWaveTransition();
                        _particles.EmitDeathFlame(_arena.CombatBounds.Center.ToVector2(), 12, 0.8f);
                    }
                }
                break;
        }
    }

    private void DrawArenaLoop(SpriteBatch batch, Texture2D pixel)
    {
        // The gate is part of the room, so it stays as authored geometry. The old
        // pulsing ring that marked the arena centre was a hard vector circle over
        // the combat plane and has been removed; the encounter's staged arrivals
        // now carry that beat instead.
        if (_loopState == ArenaLoopState.Complete)
        {
            return;
        }

        Rectangle gate = new(_arena.CombatBounds.Center.X - 92, _arena.CombatBounds.Bottom - 14, 184, 20);
        batch.FillRectangle(pixel, gate, new Color(24, 22, 30));
        batch.DrawRectangle(pixel, gate, GameBalance.MetalColor, 5f);
        for (int x = gate.Left + 18; x < gate.Right; x += 24)
        {
            batch.DrawLine(pixel, new Vector2(x, gate.Top - 17), new Vector2(x, gate.Bottom + 17), GameBalance.StoneColor, 7f);
        }
    }

    private void DrawDebugOverlay(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        int x = viewport.Width - 324;
        int y = 24;
        batch.FillRectangle(pixel, new Rectangle(x - 14, y - 12, 308, 264), new Color(5, 5, 9) * 0.9f);
        batch.DrawRectangle(pixel, new Rectangle(x - 14, y - 12, 308, 264), new Color(80, 220, 210) * 0.72f, 2f);

        Color label = new(189, 231, 226);
        PixelText.Draw(batch, pixel, $"FPS: {_fps}", new Vector2(x, y), 2, label);
        PixelText.Draw(batch, pixel, $"HP: {_player.Health}/{GameBalance.PlayerMaxHealth}", new Vector2(x, y + 24), 2, label);
        string resonance = _player.ResonanceActive
            ? $"RESONANCE: {_player.ResonanceRemaining:0.0}"
            : $"RESONANCE: {_player.Resonance:0}/{GameBalance.ResonanceRequired:0}";
        PixelText.Draw(batch, pixel, resonance, new Vector2(x, y + 48), 2, label);
        PixelText.Draw(batch, pixel, $"WAVE: {_waveNumber}/4 {_loopState}", new Vector2(x, y + 72), 2, label);
        PixelText.Draw(batch, pixel, $"ENEMIES: {_enemies.Count(enemy => enemy.IsAlive)}", new Vector2(x, y + 96), 2, label);
        PixelText.Draw(batch, pixel, $"SOULS: {_souls.Count}", new Vector2(x, y + 120), 2, label);
        PixelText.Draw(batch, pixel, $"PLAYER: {GetPlayerState()}", new Vector2(x, y + 144), 2, label);
        PixelText.Draw(batch, pixel, $"SENSE FORCE: {(_forceSoulSense ? "ON" : "OFF")}", new Vector2(x, y + 168), 2, label);
        PixelText.Draw(batch, pixel, $"VISUAL: {_presentationSettings.Summary}", new Vector2(x, y + 192), 2, label);
        PixelText.Draw(batch, pixel, $"VFX: {_spriteVfx.ActiveCount}/{SpriteVfxSystem.Capacity} DROP {_spriteVfx.DroppedCount}", new Vector2(x, y + 216), 2, label);
        PixelText.Draw(batch, pixel, $"PART: {_particles.ActiveCount}/{ParticleSystem.Capacity} DROP {_particles.DroppedCount}", new Vector2(x, y + 240), 2, label);
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

    private static bool IsPointInsideStrike(Player warden, Vector2 point, ScytheStrike strike)
    {
        Vector2 toPoint = point - warden.Position;
        if (toPoint.LengthSquared() > MathF.Pow(strike.Range + GameBalance.HollowCoreRadius, 2f))
        {
            return false;
        }

        Vector2 direction = toPoint.LengthSquared() > 0.001f ? Vector2.Normalize(toPoint) : strike.Direction;
        return Vector2.Dot(strike.Direction, direction) >= MathF.Cos(strike.ArcRadians * 0.5f);
    }

    private static Vector2 FindStrikeWeakPoint(Player warden, Enemy enemy, ScytheStrike strike)
    {
        if (!warden.SoulSenseActive)
        {
            return Vector2.Zero;
        }

        if (enemy is Hollow hollow && IsPointInsideStrike(warden, hollow.CorePosition, strike))
        {
            return hollow.CorePosition;
        }

        if (enemy is Burning burning)
        {
            foreach (Vector2 fracture in burning.GetFracturePositions())
            {
                if (IsPointInsideStrike(warden, fracture, strike))
                {
                    return fracture;
                }
            }
        }


        if (enemy is Devourer devourer && IsPointInsideStrike(warden, devourer.TorsoPosition, strike))
        {
            return devourer.TorsoPosition;
        }

        return Vector2.Zero;
    }

    private string GetScreenshotContext()
    {
        if (_player.IsDead) return "phase05_player_down";
        if (_loopState == ArenaLoopState.Title) return "phase15_title";
        if (_loopState == ArenaLoopState.Complete) return "phase15_soul_free";
        if (_loopState == ArenaLoopState.Transition) return $"phase12_wave_{_waveNumber}_clear";
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

    private void SpawnCannonShot(Player warden)
    {
        if (!warden.Cannon.TryConsumeShot(out CannonShotRequest request))
        {
            return;
        }

        Vector2 origin = warden.Position + request.Direction * 74f;
        _cannonShots.Add(new CannonShot(origin, request));
        _combatPresentation.PresentCannonFire(origin, request);
        if (request.IsFullCharge)
        {
            _arenaAtmosphere.ReactToForce(origin, 460f, 135f);
        }
        _audio.Play(AudioCue.CannonFire, request.IsFullCharge ? 0.9f : 0.58f, request.IsFullCharge ? -0.08f : 0.08f);
        warden.ApplyCannonRecoil(request.Direction, request.Charge);
    }

    private void UpdateCannonShots(float deltaTime)
    {
        foreach (CannonShot shot in _cannonShots)
        {
            shot.Update(deltaTime, _arena.Bounds);
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
                ApplyEnemyDamage(enemy, new DamageInfo(
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
                    AddTeamResonance(GameBalance.ResonancePerCoreHit * (shot.IsFullCharge ? 2f : 1f));
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

    private void PlayWardenActionAudio(PlayerSlot slot, WardenSnapshot before)
    {
        Player warden = slot.Warden;

        if (warden.Scythe.StartedThisFrame)
        {
            AudioCue cue = warden.Scythe.ActiveStep switch
            {
                2 => AudioCue.ScytheSwing2,
                3 => AudioCue.SoulCleave,
                _ => AudioCue.ScytheSwing1
            };
            // The brother's flame is older and steadier; pitching his weapon
            // slightly down keeps two simultaneous swings from phasing into one.
            _audio.Play(cue, warden.Scythe.ActiveStep == 3 ? 0.78f : 0.5f, slot.Index == 0 ? 0f : -0.09f);
        }

        if (!before.Dashing && warden.IsDashing)
        {
            _audio.Play(AudioCue.Dash, 0.62f, slot.Index == 0 ? 0f : -0.07f);
        }
        if (before.CannonState != SoulCannonState.Charging && warden.Cannon.State == SoulCannonState.Charging)
        {
            _audio.Play(AudioCue.CannonCharge, 0.42f);
        }
        if (!before.CannonFull && warden.Cannon.IsFullCharge)
        {
            _audio.Play(AudioCue.CannonFull, 0.72f);
        }
        if (!before.SoulSense && warden.SoulSenseActive && !warden.ResonanceActive)
        {
            _audio.Play(AudioCue.SoulSenseOn, 0.38f);
        }
        else if (before.SoulSense && !warden.SoulSenseActive)
        {
            _audio.Play(AudioCue.SoulSenseOff, 0.3f);
        }

        if (slot.Index == 0 && before.SoulSense != warden.SoulSenseActive)
        {
            _audio.SetSoulSense(warden.SoulSenseActive);
        }
    }

    private void ApplyEnemyDamage(Enemy enemy, DamageInfo damage)
    {
        bool wasAlive = enemy.IsAlive;
        enemy.ApplyDamage(damage);
        if (wasAlive && !enemy.IsAlive)
        {
            float volume = enemy is Devourer ? 0.72f : 0.52f;
            _audio.Play(AudioCue.EnemyDeath, volume);
        }
    }
}
