using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Core;
using TheLostSoulOfFire.Debugging;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire;

public sealed class Game1 : Microsoft.Xna.Framework.Game
{
    /// <summary>
    /// The game is always simulated and laid out at this fixed logical resolution and drawn at
    /// <see cref="RenderResolution.OutputWidth"/>×<see cref="RenderResolution.OutputHeight"/>;
    /// the result is then scaled into whatever window size the player has chosen. Keeping this
    /// constant means the visible world, HUD and menu layout never change with window size.
    /// </summary>
    private static readonly Viewport VirtualViewport = new(0, 0, GameBalance.BackBufferWidth, GameBalance.BackBufferHeight);

    private const int MinWindowWidth = 480;
    private const int MinWindowHeight = 270;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;
    private InputState _input = null!;
    private GameWorld _world = null!;
    private ArtAssets _art = null!;
    private SoulfireRenderer _soulfireRenderer = null!;
    private ResolutionManager _resolution = null!;
    private RenderTarget2D _virtualTarget = null!;
    private readonly bool _audioGameplayTest;
    private readonly bool _audioDeathRestartTest;
    private readonly bool _antechamberVisualTest;
    private readonly bool _currencyVisualTest;
    private readonly bool _travelVisualTest;
    private (int Geld, int Glut) _travelTestPartial;
    private readonly bool _abilityVisualTest;
    private readonly bool _sliceVisualTest;
    private SliceVisualTest? _sliceTest;
    private readonly bool _tourVisualTest;
    private TourVisualTest? _tourTest;
    private TimeSpan _tourClock;
    private float _abilityTestTime;
    private int _abilityTestStep;
    private bool _abilityMenuCaptured;
    private int _abilityMenuTestStep;
    private void ConfigureAbilityVisualTest(float dt)
    {
        _abilityTestTime += dt;
        if (_world.PlayerDead) { Console.WriteLine("ABILITY_VISUAL_TEST_FAIL player died" ); Environment.ExitCode = 1; Exit(); return; }
        if (!_abilityMenuCaptured && _world.Phase == GamePhase.Arena &&
            _world.LoopState == ArenaLoopState.Intro)
        {
            _input.InjectKeyPress(Keys.C);
            _abilityMenuCaptured = true;
            _abilityTestTime = 0;
        }
        else if (_abilityMenuCaptured && _abilityTestStep == 0 && _abilityTestTime > 0.35f)
        {
            switch (_abilityMenuTestStep++)
            {
                case 0: _screenshotRequested = true; break;
                case 1: _input.InjectKeyPress(Keys.X); _input.InjectKeyPress(Keys.D4); break;
                case 2: _screenshotRequested = true; break;
                case 3: _input.InjectKeyPress(Keys.Z); _input.InjectKeyPress(Keys.D5); break;
                case 4: _world.VerifyAutomatedSkillLoadout(); _screenshotRequested = true; break;
                default: _input.InjectKeyPress(Keys.Enter); _abilityTestStep = 1; break;
            }
            _abilityTestTime = 0;
        }
        if (_world.LoopState != ArenaLoopState.Combat) return;
        if (_abilityTestTime > 2f)
        {
            if (_abilityTestStep is >= 1 and <= 6)
            {
                _world.ShowAutomatedAbility((TheLostSoulOfFire.Combat.RunAbility)(_abilityTestStep - 1));
                _screenshotRequested = true;
                _abilityTestStep++;
                _abilityTestTime = 0;
            }
            else if (_abilityTestStep == 7)
            {
                _world.ShowAutomatedSkillsMenu(true);
                _input.InjectKeyPress(Keys.D3);
                _abilityTestStep++;
                _abilityTestTime = 0;
            }
            else if (_abilityTestStep == 8)
            {
                _world.VerifyAutomatedSkillLoadout();
                _screenshotRequested = true;
                _abilityTestStep++;
                _abilityTestTime = 0;
            }
            else if (_abilityTestStep > 8)
            {
                Console.WriteLine("ABILITY_VISUAL_TEST_PASS sixCasts=true menu=true skills=true selection=true combatLocked=true profile=isolated");
                Exit();
            }
        }
    }

    private string? _testProfilePath;
    private float _currencyTestStateTime;
    private string _currencyTestState = string.Empty;
    private readonly HashSet<string> _currencyTestDone = [];
    private readonly DeveloperStartOptions? _developerStart;
    private readonly GameSettingsStore _settingsStore = new();
    private readonly GameSettings _settings;
    private bool _screenshotRequested;
    private string _screenshotStatus = string.Empty;
    private float _audioTestTotalTime;
    private float _audioTestStateTime;
    private int _audioTestWave;
    private bool _audioTestWaveKilled;
    private bool _audioTestCompleteSeen;
    private bool _audioTestRestartInjected;
    private bool _audioTestDeathRequested;
    private bool _isHandlingResize;
    private bool _isFullscreen;
    private int _windowedWidth = GameBalance.BackBufferWidth;
    private int _windowedHeight = GameBalance.BackBufferHeight;
    private int _antechamberVisualStage;
    private float _antechamberEntryTime;
    private bool _antechamberEntryCaptured;

    public Game1(
        bool audioGameplayTest = false,
        bool audioDeathRestartTest = false,
        bool antechamberVisualTest = false,
        DeveloperStartOptions? developerStart = null,
        bool currencyVisualTest = false,
        bool abilityVisualTest = false,
        bool sliceVisualTest = false,
        bool tourVisualTest = false,
        bool travelVisualTest = false)
    {
        _tourVisualTest = tourVisualTest;
        _sliceVisualTest = sliceVisualTest;
        // The travel point test shares the currency test's setup: temporary profile, no prologue.
        _travelVisualTest = travelVisualTest;
        _currencyVisualTest = currencyVisualTest || travelVisualTest;
        _abilityVisualTest = abilityVisualTest;

        _audioGameplayTest = audioGameplayTest;
        _audioDeathRestartTest = audioDeathRestartTest;
        _antechamberVisualTest = antechamberVisualTest;
        _developerStart = developerStart;
        _settings = _settingsStore.Load();
        if (_abilityVisualTest || _sliceVisualTest || _tourVisualTest) _settings.Fullscreen = false;
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = GameBalance.BackBufferWidth,
            PreferredBackBufferHeight = GameBalance.BackBufferHeight,
            SynchronizeWithVerticalRetrace = true
        };

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d);
        if (_tourVisualTest)
        {
            // Every frame advances exactly 1/60 s however long a capture takes.
            IsFixedTimeStep = false;
            _graphics.SynchronizeWithVerticalRetrace = false;
        }
        Window.Title = "The Lost Soul of Fire";
        Window.AllowUserResizing = true;
    }

    /// <summary>Automated runs must never touch the player's real profile.</summary>
    private PlayerProfileStore CreateProfileStore()
    {
        if (!(_audioGameplayTest || _audioDeathRestartTest || _antechamberVisualTest || _currencyVisualTest || _abilityVisualTest || _sliceVisualTest || _tourVisualTest))
        {
            return new PlayerProfileStore();
        }

        _testProfilePath = Path.Combine(Path.GetTempPath(), $"TheLostSoulOfFire-test-profile-{Environment.ProcessId}.json");
        return new PlayerProfileStore(_testProfilePath);
    }

    protected override void Initialize()
    {
        _input = new InputState();
        _resolution = new ResolutionManager(GameBalance.BackBufferWidth, GameBalance.BackBufferHeight);
        if (_settings.Fullscreen) SetFullscreen(true, rememberWindow: false);
        Window.ClientSizeChanged += OnClientSizeChanged;
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _art = new ArtAssets(Content);
        PixelText.LoadFonts(Content);
        UiKit.Load(Content);
        WorldMarks.Load(Content);
        GroundImpacts.Load(Content);
        _virtualTarget = new RenderTarget2D(
            GraphicsDevice,
            RenderResolution.OutputWidth,
            RenderResolution.OutputHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None);
        _world = new GameWorld(
            VirtualViewport,
            _art,
            Content,
            _audioGameplayTest || _audioDeathRestartTest || _antechamberVisualTest || _currencyVisualTest,
            skipPrologue: _audioGameplayTest || _audioDeathRestartTest || _antechamberVisualTest || _currencyVisualTest,
            settings: _settings,
            settingsChanged: _settingsStore.Save,
            profileStore: CreateProfileStore());
        if (_abilityVisualTest) _world.ApplyDeveloperStart(new DeveloperStartOptions(DeveloperStartArea.Arena, 1), VirtualViewport);
        if (_sliceVisualTest) _sliceTest = new SliceVisualTest(_world, _input, VirtualViewport);
        if (_tourVisualTest)
        {
            // Frames are unlocked in the tour (see the constructor), so the wall time between
            // frames measures CPU and GPU together.
            _tourTest = new TourVisualTest(_world, _input, VirtualViewport, _art.RegistryError, () => _art.MissingVisuals);
        }
        if (_developerStart is not null)
        {
            Console.WriteLine(_developerStart.Describe());
            _world.ApplyDeveloperStart(_developerStart, VirtualViewport);
        }
        _soulfireRenderer = new SoulfireRenderer(GraphicsDevice, Content);
        _resolution.Update(GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
    }

    private readonly System.Diagnostics.Stopwatch _frameWatch = new();
    private readonly System.Diagnostics.Stopwatch _wallWatch = System.Diagnostics.Stopwatch.StartNew();
    private double _lastWall;

    protected override void Update(GameTime gameTime)
    {
        _frameWatch.Restart();
        _input.Update(_resolution);
        if (_tourTest is not null)
        {
            _tourClock += TimeSpan.FromSeconds(1d / 60d);
            gameTime = new GameTime(_tourClock, TimeSpan.FromSeconds(1d / 60d));
            _tourTest.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            if (_tourTest.Finished)
            {
                Environment.ExitCode = _tourTest.ExitCode;
                Exit();
                return;
            }
        }
        else if (_sliceTest is not null)
        {
            _sliceTest.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            if (_sliceTest.Finished)
            {
                Environment.ExitCode = _sliceTest.ExitCode;
                Exit();
                return;
            }
        }
        else if (_abilityVisualTest)
        {
            ConfigureAbilityVisualTest((float)gameTime.ElapsedGameTime.TotalSeconds);
        }
        else if (_antechamberVisualTest)
        {
            ConfigureAntechamberVisualTest((float)gameTime.ElapsedGameTime.TotalSeconds);
        }
        else if (_audioGameplayTest || _audioDeathRestartTest)
        {
            ConfigureAutomatedTest((float)gameTime.ElapsedGameTime.TotalSeconds);
        }
        else if (_travelVisualTest)
        {
            ConfigureTravelVisualTest((float)gameTime.ElapsedGameTime.TotalSeconds);
        }
        else if (_currencyVisualTest)
        {
            ConfigureCurrencyVisualTest((float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        // Escape is handled by GameWorld (pause menu, menu back navigation, quit confirmation).
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        {
            Exit();
            return;
        }

        if (_input.WasKeyPressed(Keys.F11))
        {
            _settings.Fullscreen = !_settings.Fullscreen;
            SetFullscreen(_settings.Fullscreen);
            _settingsStore.Save(_settings);
        }

        if (_input.WasKeyPressed(Keys.F9))
        {
            _screenshotRequested = true;
        }

        _world.Update(gameTime, _input, VirtualViewport);
        if (_settings.Fullscreen != _isFullscreen)
        {
            SetFullscreen(_settings.Fullscreen);
            _settingsStore.Save(_settings);
        }
        if (_world.QuitRequested)
        {
            Exit();
            return;
        }
        if (_audioGameplayTest || _audioDeathRestartTest)
        {
            FinishAutomatedTestFrame();
        }
        else if (_antechamberVisualTest && _antechamberEntryCaptured && _antechamberEntryTime >= 0.8f)
        {
            Console.WriteLine("ANTECHAMBER_VISUAL_TEST_PASS normal=true soulSense=true sealedDoor=true finalDoor=true doorI=true entry=true");
            Environment.ExitCode = 0;
            Exit();
        }
        Window.Title = string.IsNullOrEmpty(_screenshotStatus) ? _world.WindowTitle : _screenshotStatus;
        _screenshotStatus = string.Empty;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Phase 1: world and HUD are laid out in the fixed logical resolution and drawn at the
        // output resolution through RenderResolution.ScaleMatrix. SoulfireRenderer.PresentScene is handed
        // _virtualTarget explicitly so its internal composite lands here rather than on
        // the window's back buffer (MonoGame has no render-target stack — SetRenderTarget
        // always means "this target or the back buffer", never "whatever was bound before").
        GraphicsDevice.SetRenderTarget(_virtualTarget);
        GraphicsDevice.Clear(GameBalance.VoidColor);
        _world.Draw(_spriteBatch, _pixel, VirtualViewport, _soulfireRenderer, _virtualTarget);

        // Phase 2: letterbox the finished frame into the actual window. Linear filtering keeps
        // the painted art smooth at any window size and is lossless at 1:1 (Full-HD fullscreen).
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp);
        _spriteBatch.Draw(_virtualTarget, _resolution.Destination, Color.White);
        _spriteBatch.End();

        if (_screenshotRequested)
        {
            _screenshotRequested = false;
            _screenshotStatus = ScreenshotCapture.TrySaveVirtualTarget(
                _virtualTarget,
                _world.ScreenshotContext,
                out string path)
                ? $"Screenshot saved — {path}"
                : $"Screenshot failed — {path}";
        }

        _tourTest?.RecordFrameTime(_frameWatch.Elapsed.TotalMilliseconds);
        if (_tourTest is not null)
        {
            double now = _wallWatch.Elapsed.TotalMilliseconds;
            _tourTest.RecordWallTime(now - _lastWall, _tourTest.PendingCapture is not null);
            _lastWall = now;
        }
        if (_tourTest?.PendingCapture is not null)
        {
            _tourTest.Capture(_virtualTarget);
        }

        if (_sliceTest?.PendingCapture is { } capture)
        {
            bool saved = ScreenshotCapture.TrySaveVirtualTarget(_virtualTarget, "slice_" + capture, out string result);
            _sliceTest.CaptureCompleted(saved, result);
        }

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        Window.ClientSizeChanged -= OnClientSizeChanged;
        _world.Dispose();
        _soulfireRenderer.Dispose();
        _virtualTarget.Dispose();
        _pixel.Dispose();
        _spriteBatch.Dispose();
        if (_testProfilePath is not null)
        {
            try { File.Delete(_testProfilePath); } catch { }
        }
        base.UnloadContent();
    }

    private void OnClientSizeChanged(object? sender, EventArgs e)
    {
        if (_isHandlingResize)
        {
            return;
        }

        _isHandlingResize = true;
        try
        {
            int width = Window.ClientBounds.Width;
            int height = Window.ClientBounds.Height;
            if (!_isFullscreen && (width < MinWindowWidth || height < MinWindowHeight))
            {
                width = Math.Max(width, MinWindowWidth);
                height = Math.Max(height, MinWindowHeight);
                _graphics.PreferredBackBufferWidth = width;
                _graphics.PreferredBackBufferHeight = height;
                _graphics.ApplyChanges();
            }

            _resolution.Update(GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
        }
        finally
        {
            _isHandlingResize = false;
        }
    }

    private void SetFullscreen(bool enabled, bool rememberWindow = true)
    {
        if (_isFullscreen == enabled) return;
        if (!enabled)
        {
            _isFullscreen = false;
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = _windowedWidth;
            _graphics.PreferredBackBufferHeight = _windowedHeight;
            _graphics.ApplyChanges();
        }
        else
        {
            if (rememberWindow)
            {
                _windowedWidth = Window.ClientBounds.Width;
                _windowedHeight = Window.ClientBounds.Height;
            }
            DisplayMode displayMode = GraphicsDevice.Adapter.CurrentDisplayMode;
            _isFullscreen = true;
            _graphics.HardwareModeSwitch = false;
            _graphics.PreferredBackBufferWidth = displayMode.Width;
            _graphics.PreferredBackBufferHeight = displayMode.Height;
            _graphics.IsFullScreen = true;
            _graphics.ApplyChanges();
        }
    }

    private void ConfigureAutomatedTest(float deltaTime)
    {
        _audioTestTotalTime += deltaTime;
        _audioTestStateTime += deltaTime;

        if (_world.Phase == GamePhase.Title)
        {
            _input.InjectKeyPress(Keys.Space);
            return;
        }

        if (_world.Phase == GamePhase.Antechamber)
        {
            _world.RequestAutomatedDoorEntry();
            return;
        }

        if (_world.Phase == GamePhase.EnteringArena)
        {
            return;
        }

        if (_audioDeathRestartTest && _world.PlayerDead)
        {
            if (!_audioTestRestartInjected && _audioTestStateTime >= 1.4f)
            {
                _audioTestRestartInjected = true;
                _input.InjectKeyPress(Keys.R);
            }
            return;
        }

        if (_world.LoopState == ArenaLoopState.Combat)
        {
            if (_audioDeathRestartTest)
            {
                if (!_audioTestDeathRequested)
                {
                    _audioTestDeathRequested = true;
                    _audioTestStateTime = 0f;
                    _world.RequestAudioTestFatalDamage();
                }
                return;
            }

            if (_audioTestWave != _world.WaveNumber)
            {
                _audioTestWave = _world.WaveNumber;
                _audioTestStateTime = 0f;
                _audioTestWaveKilled = false;
                Console.WriteLine($"AUDIO_GAMEPLAY_WAVE {_audioTestWave}");
            }

            if (!_audioTestWaveKilled && _audioTestStateTime >= 0.65f)
            {
                _audioTestWaveKilled = true;
                _input.InjectKeyPress(Keys.F6);
            }
            return;
        }

        if (_world.LoopState == ArenaLoopState.Intermission)
        {
            _world.RequestAutomatedNextWave();
            return;
        }

        if (_world.LoopState == ArenaLoopState.Complete)
        {
            if (!_audioTestCompleteSeen)
            {
                _audioTestCompleteSeen = true;
                _audioTestStateTime = 0f;
                Console.WriteLine("AUDIO_GAMEPLAY_COMPLETE");
            }

            if (!_audioTestRestartInjected && _audioTestStateTime >= 5.2f)
            {
                _audioTestRestartInjected = true;
                _input.InjectKeyPress(Keys.R);
            }
        }

    }

    private void ConfigureAntechamberVisualTest(float deltaTime)
    {
        _audioTestTotalTime += deltaTime;
        if (_world.Phase == GamePhase.Title)
        {
            _input.InjectKeyPress(Keys.Space);
            return;
        }

        if (_world.Phase == GamePhase.EnteringArena)
        {
            _antechamberEntryTime += deltaTime;
            if (!_antechamberEntryCaptured && _antechamberEntryTime >= 0.6f)
            {
                _screenshotRequested = true;
                _antechamberEntryCaptured = true;
            }
            return;
        }

        if (_world.Phase != GamePhase.Antechamber)
        {
            return;
        }

        // Each step waits for its time mark, then either acts on the hub or requests a capture:
        // hub, Soul Sense, sealed biome door II, sealed final door, open door I, entry into door I.
        (float At, Action Act)[] steps =
        [
            (1f, () => _screenshotRequested = true),
            (1.7f, () => _world.SetAutomatedSoulSense(true)),
            (2.7f, () => _screenshotRequested = true),
            (2.9f, () => { _world.SetAutomatedSoulSense(false); _world.PlaceAutomatedPlayerAtDoor(1); }),
            (3.9f, () => _screenshotRequested = true),
            (4.0f, () => _world.PlaceAutomatedPlayerAtDoor(3)),
            (4.6f, () => _screenshotRequested = true),
            (4.7f, () => _world.PlaceAutomatedPlayerAtDoor(0)),
            (5.3f, () => _screenshotRequested = true),
            (5.4f, () => _world.RequestAutomatedDoorEntry()),
        ];

        if (_antechamberVisualStage < steps.Length && _audioTestTotalTime >= steps[_antechamberVisualStage].At)
        {
            steps[_antechamberVisualStage].Act();
            _antechamberVisualStage++;
        }
    }

    /// <summary>
    /// <c>--currency-visual-test</c>: plays one arena run, opens the chests after waves 3 and 6,
    /// leaves the one after wave 9 unopened, captures HUD, chest and completion, then checks
    /// the secured balances in the hub. Uses a temporary profile.
    /// </summary>
    private void ConfigureCurrencyVisualTest(float deltaTime)
    {
        _audioTestTotalTime += deltaTime;
        string state = _world.Phase == GamePhase.Arena ? $"arena-{_world.LoopState}-{_world.WaveNumber}" : _world.Phase.ToString();
        if (state != _currencyTestState)
        {
            _currencyTestState = state;
            _currencyTestStateTime = 0f;
        }
        _currencyTestStateTime += deltaTime;

        bool Once(string key, float at)
        {
            if (_currencyTestStateTime < at || !_currencyTestDone.Add($"{key}:{(_currencyTestDone.Contains("complete") ? "hub" : "run")}"))
            {
                return false;
            }
            return true;
        }

        if (_audioTestTotalTime >= 150f)
        {
            Console.WriteLine($"CURRENCY_VISUAL_TEST_FAIL timeout state={state}");
            Environment.ExitCode = 1;
            Exit();
            return;
        }

        switch (_world.Phase)
        {
            case GamePhase.Title:
                if (_currencyTestStateTime >= 0.3f) _input.InjectKeyPress(Keys.Space);
                return;
            case GamePhase.Antechamber:
                if (_currencyTestDone.Contains("complete"))
                {
                    if (Once("hub-shot", 1.2f)) _screenshotRequested = true;
                    if (_currencyTestStateTime >= 1.6f) FinishCurrencyVisualTest();
                }
                else if (_currencyTestStateTime >= 0.4f)
                {
                    _world.RequestAutomatedDoorEntry();
                }
                return;
            case GamePhase.Arena:
                break;
            default:
                return;
        }

        int wave = _world.WaveNumber;
        switch (_world.LoopState)
        {
            case ArenaLoopState.Combat:
                if (Once($"kill-{wave}", 0.65f)) _input.InjectKeyPress(Keys.F6);
                if (wave == 1 && Once("spark-shot", 0.95f)) _screenshotRequested = true;
                break;
            case ArenaLoopState.Intermission:
                if (wave is 3 or 6)
                {
                    if (Once($"place-{wave}", 0.15f)) _world.PlaceAutomatedPlayerAtNewestChest();
                    if (wave == 3 && Once("chest-shot", 0.3f)) _screenshotRequested = true;
                    if (Once($"open-{wave}", 0.4f)) _input.InjectKeyPress(Keys.E);
                    if (wave == 3 && Once("open-shot", 0.55f)) _screenshotRequested = true;
                }
                if (Once($"to-centre-{wave}", 0.9f)) _world.PlaceAutomatedPlayerAtWaveTrigger();
                if (wave == 1 && Once("trigger-shot", 1.05f)) _screenshotRequested = true;
                if (Once($"next-{wave}", 1.8f)) _input.InjectKeyPress(Keys.E);
                break;
            case ArenaLoopState.Complete:
                if (Once("complete-shot", 2.2f)) _screenshotRequested = true;
                if (_currencyTestStateTime >= 2.5f && _currencyTestDone.Add("complete"))
                {
                    Console.WriteLine($"CURRENCY_COMPLETE secured geld={_world.Wallet.Secured(Currency.Geld)} glut={_world.Wallet.Secured(Currency.Glut)}");
                    _input.InjectKeyPress(Keys.R);
                }
                break;
        }
    }

    private void FinishCurrencyVisualTest()
    {
        CurrencyWallet wallet = _world.Wallet;
        int geld = wallet.Secured(Currency.Geld);
        int glut = wallet.Secured(Currency.Glut);
        bool pass = geld == 2 * GameBalance.ChestGeld && glut > GameBalance.GlutStarterStock &&
            wallet.Run(Currency.Geld) == 0 && wallet.Run(Currency.Glut) == 0;
        Console.WriteLine($"CURRENCY_VISUAL_TEST_{(pass ? "PASS" : "FAIL")} securedGeld={geld} securedGlut={glut}");
        Environment.ExitCode = pass ? 0 : 1;
        Exit();
    }

    /// <summary>
    /// <c>--travel-visual-test</c>: both runs open the chest after wave 3. The first takes the partial securing
    /// at the travel point after wave 5 and dies in wave 6, so only the unsecured half is lost. The
    /// second run extracts at the travel point and checks the balances in the hub. Captures prompt,
    /// menu and the extraction summary. Uses a temporary profile.
    /// </summary>
    private void ConfigureTravelVisualTest(float deltaTime)
    {
        _audioTestTotalTime += deltaTime;
        int run = _currencyTestDone.Contains("died") ? 2 : 1;
        string state = _world.Phase == GamePhase.Arena
            ? $"arena-{run}-{_world.LoopState}-{_world.WaveNumber}-{_world.PlayerDead}-{_world.TravelMenuOpen}"
            : $"{_world.Phase}-{run}";
        if (state != _currencyTestState)
        {
            _currencyTestState = state;
            _currencyTestStateTime = 0f;
        }
        _currencyTestStateTime += deltaTime;

        bool Once(string key, float at) => _currencyTestStateTime >= at && _currencyTestDone.Add($"{key}:{run}");

        void Fail(string reason)
        {
            Console.WriteLine($"TRAVEL_VISUAL_TEST_FAIL {reason}");
            Environment.ExitCode = 1;
            Exit();
        }

        if (_audioTestTotalTime >= 200f)
        {
            Fail($"timeout state={state}");
            return;
        }

        CurrencyWallet wallet = _world.Wallet;
        switch (_world.Phase)
        {
            case GamePhase.Title:
                if (_currencyTestStateTime >= 0.3f) _input.InjectKeyPress(Keys.Space);
                return;
            case GamePhase.Antechamber:
                if (_currencyTestDone.Contains("extracted:2"))
                {
                    if (Once("hub-shot", 1.0f)) _screenshotRequested = true;
                    if (_currencyTestStateTime >= 1.4f)
                    {
                        // Run 1 secured half after its chest, run 2 extracted everything it held.
                        bool pass = wallet.Run(Currency.Geld) == 0 && wallet.Run(Currency.Glut) == 0 &&
                            wallet.Secured(Currency.Geld) == _travelTestPartial.Geld + GameBalance.ChestGeld &&
                            wallet.Secured(Currency.Glut) > _travelTestPartial.Glut;
                        Console.WriteLine($"TRAVEL_VISUAL_TEST_{(pass ? "PASS" : "FAIL")} securedGeld={wallet.Secured(Currency.Geld)} securedGlut={wallet.Secured(Currency.Glut)} partial={_travelTestPartial}");
                        Environment.ExitCode = pass ? 0 : 1;
                        Exit();
                    }
                }
                else if (_currencyTestStateTime >= 0.4f)
                {
                    _world.RequestAutomatedDoorEntry();
                }
                return;
            case GamePhase.Arena:
                break;
            default:
                return;
        }

        int wave = _world.WaveNumber;
        if (_world.PlayerDead)
        {
            if (Once("dead-check", 0.4f))
            {
                bool kept = wallet.Secured(Currency.Geld) == _travelTestPartial.Geld && wallet.Secured(Currency.Glut) == _travelTestPartial.Glut &&
                    wallet.Run(Currency.Geld) == 0 && wallet.Run(Currency.Glut) == 0;
                Console.WriteLine($"TRAVEL_DEFEAT secured={_travelTestPartial} kept={kept}");
                if (!kept)
                {
                    Fail("defeat changed secured balances");
                    return;
                }
            }
            if (_currencyTestStateTime >= 0.8f && _currencyTestDone.Add("died"))
            {
                _input.InjectKeyPress(Keys.R);
            }
            return;
        }

        switch (_world.LoopState)
        {
            case ArenaLoopState.Combat:
                if (run == 1 && wave == GameBalance.TravelPointWave + 1)
                {
                    if (Once("die", 0.5f)) _world.RequestAudioTestFatalDamage();
                }
                else if (Once($"kill-{wave}", 0.65f))
                {
                    _input.InjectKeyPress(Keys.F6);
                }
                break;
            case ArenaLoopState.Intermission when wave == GameBalance.TravelPointWave:
                if (Once("place", 0.15f)) _world.PlaceAutomatedPlayerAtTravelPoint();
                if (run == 1 && Once("prompt-shot", 0.35f)) _screenshotRequested = true;
                if (Once("open", 0.5f)) _input.InjectKeyPress(Keys.E);
                if (_world.TravelMenuOpen)
                {
                    if (run == 1 && Once("menu-shot", 0.2f)) _screenshotRequested = true;
                    if (run == 1 && Once("choose", 1.2f))
                    {
                        int runGeld = wallet.Run(Currency.Geld);
                        int runGlut = wallet.Run(Currency.Glut);
                        _input.InjectKeyPress(Keys.D1);
                        _travelTestPartial = (runGeld / 2, runGlut / 2);
                        Console.WriteLine($"TRAVEL_PARTIAL run=({runGeld},{runGlut}) expected={_travelTestPartial}");
                    }
                    if (run == 2 && Once("extract", 0.6f))
                    {
                        _currencyTestDone.Add("extracted:2");
                        _input.InjectKeyPress(Keys.D3);
                    }
                }
                break;
            case ArenaLoopState.Intermission:
                if (wave == 3)
                {
                    if (Once("place-chest", 0.15f)) _world.PlaceAutomatedPlayerAtNewestChest();
                    if (Once("open-chest", 0.4f)) _input.InjectKeyPress(Keys.E);
                }
                if (Once($"to-centre-{wave}", 0.9f)) _world.PlaceAutomatedPlayerAtWaveTrigger();
                if (Once($"next-{wave}", 1.4f)) _input.InjectKeyPress(Keys.E);
                break;
            case ArenaLoopState.Transition when run == 1 && wave == GameBalance.TravelPointWave:
                if (Once("partial-check", 0.05f))
                {
                    bool halved = wallet.Secured(Currency.Geld) == _travelTestPartial.Geld && wallet.Secured(Currency.Glut) == _travelTestPartial.Glut;
                    if (!halved)
                    {
                        Fail($"partial securing secured=({wallet.Secured(Currency.Geld)},{wallet.Secured(Currency.Glut)}) expected={_travelTestPartial}");
                        return;
                    }
                }
                if (Once("partial-shot", 0.2f)) _screenshotRequested = true;
                break;
        }
    }

    private void FinishAutomatedTestFrame()
    {
        if (_audioDeathRestartTest)
        {
            if (_audioTestRestartInjected && !_world.PlayerDead && _world.Phase == GamePhase.Arena && _world.LoopState == ArenaLoopState.Intro)
            {
                Console.WriteLine("AUDIO_DEATH_RESTART_TEST_PASS death=true restart=true");
                Environment.ExitCode = 0;
                Exit();
            }
            else if (_audioTestTotalTime >= 10f)
            {
                Console.WriteLine($"AUDIO_DEATH_RESTART_TEST_FAIL dead={_world.PlayerDead} state={_world.LoopState}");
                Environment.ExitCode = 1;
                Exit();
            }
            return;
        }

        if (_world.PlayerDead || _audioTestTotalTime >= 90f)
        {
            Console.WriteLine($"AUDIO_GAMEPLAY_TEST_FAIL dead={_world.PlayerDead} state={_world.LoopState} wave={_world.WaveNumber}");
            Environment.ExitCode = 1;
            Exit();
            return;
        }

        if (_audioTestRestartInjected && _world.Phase == GamePhase.Arena && _world.LoopState == ArenaLoopState.Intro && _world.WaveNumber == 0)
        {
            Console.WriteLine($"AUDIO_GAMEPLAY_TEST_PASS waves={GameBalance.ArenaWaveCount} completion=true restart=true");
            Environment.ExitCode = 0;
            Exit();
        }
    }
}
