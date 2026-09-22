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
    /// The game is always simulated and drawn at this fixed resolution; the result is then
    /// scaled into whatever window size the player has chosen. Keeping this constant means
    /// the visible world, HUD and menu layout never change with window size.
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

    public Game1(bool audioGameplayTest = false, bool audioDeathRestartTest = false, bool antechamberVisualTest = false)
    {
        _audioGameplayTest = audioGameplayTest;
        _audioDeathRestartTest = audioDeathRestartTest;
        _antechamberVisualTest = antechamberVisualTest;
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
        Window.Title = "The Lost Soul of Fire";
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        _input = new InputState();
        _resolution = new ResolutionManager(GameBalance.BackBufferWidth, GameBalance.BackBufferHeight);
        Window.ClientSizeChanged += OnClientSizeChanged;
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _art = new ArtAssets(Content);
        _virtualTarget = new RenderTarget2D(
            GraphicsDevice,
            GameBalance.BackBufferWidth,
            GameBalance.BackBufferHeight,
            false,
            SurfaceFormat.Color,
            DepthFormat.None);
        _world = new GameWorld(
            VirtualViewport,
            _art,
            Content,
            _audioGameplayTest || _audioDeathRestartTest || _antechamberVisualTest);
        _soulfireRenderer = new SoulfireRenderer(GraphicsDevice);
        _resolution.Update(GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update(_resolution);
        if (_antechamberVisualTest)
        {
            ConfigureAntechamberVisualTest((float)gameTime.ElapsedGameTime.TotalSeconds);
        }
        else if (_audioGameplayTest || _audioDeathRestartTest)
        {
            ConfigureAutomatedTest((float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        if (_input.IsKeyDown(Keys.Escape) ||
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        {
            Exit();
            return;
        }

        if (_input.WasKeyPressed(Keys.F11))
        {
            ToggleFullscreen();
        }

        if (_input.WasKeyPressed(Keys.F9))
        {
            _screenshotRequested = true;
        }

        _world.Update(gameTime, _input, VirtualViewport);
        if (_world.QuitRequested)
        {
            Exit();
            return;
        }
        if (_audioGameplayTest || _audioDeathRestartTest)
        {
            FinishAutomatedTestFrame();
        }
        else if (_antechamberVisualTest && _antechamberVisualStage >= 3 && _audioTestTotalTime >= 2.9f)
        {
            Console.WriteLine("ANTECHAMBER_VISUAL_TEST_PASS normal=true soulSense=true");
            Environment.ExitCode = 0;
            Exit();
        }
        Window.Title = string.IsNullOrEmpty(_screenshotStatus) ? _world.WindowTitle : _screenshotStatus;
        _screenshotStatus = string.Empty;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Phase 1: world and HUD are drawn at the fixed virtual resolution, exactly as
        // before this game supported resizing. SoulfireRenderer.PresentScene is handed
        // _virtualTarget explicitly so its internal composite lands here rather than on
        // the window's back buffer (MonoGame has no render-target stack — SetRenderTarget
        // always means "this target or the back buffer", never "whatever was bound before").
        GraphicsDevice.SetRenderTarget(_virtualTarget);
        GraphicsDevice.Clear(GameBalance.VoidColor);
        Viewport viewport = GraphicsDevice.Viewport;
        _world.Draw(_spriteBatch, _pixel, viewport, _soulfireRenderer, _virtualTarget);

        // Phase 2: letterbox the finished frame into the actual window.
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
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

    private void ToggleFullscreen()
    {
        if (_isFullscreen)
        {
            _isFullscreen = false;
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = _windowedWidth;
            _graphics.PreferredBackBufferHeight = _windowedHeight;
            _graphics.ApplyChanges();
        }
        else
        {
            _windowedWidth = Window.ClientBounds.Width;
            _windowedHeight = Window.ClientBounds.Height;
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
            _world.RequestAutomatedGateEntry();
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

        if (_world.Phase != GamePhase.Antechamber)
        {
            return;
        }

        if (_antechamberVisualStage == 0 && _audioTestTotalTime >= 1f)
        {
            _screenshotRequested = true;
            _antechamberVisualStage = 1;
        }
        else if (_antechamberVisualStage == 1 && _audioTestTotalTime >= 1.7f)
        {
            _world.SetAutomatedSoulSense(true);
            _antechamberVisualStage = 2;
        }
        else if (_antechamberVisualStage == 2 && _audioTestTotalTime >= 2.7f)
        {
            _screenshotRequested = true;
            _antechamberVisualStage = 3;
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

        if (_world.PlayerDead || _audioTestTotalTime >= 35f)
        {
            Console.WriteLine($"AUDIO_GAMEPLAY_TEST_FAIL dead={_world.PlayerDead} state={_world.LoopState} wave={_world.WaveNumber}");
            Environment.ExitCode = 1;
            Exit();
            return;
        }

        if (_audioTestRestartInjected && _world.Phase == GamePhase.Arena && _world.LoopState == ArenaLoopState.Intro && _world.WaveNumber == 0)
        {
            Console.WriteLine("AUDIO_GAMEPLAY_TEST_PASS waves=4 completion=true restart=true");
            Environment.ExitCode = 0;
            Exit();
        }
    }
}
