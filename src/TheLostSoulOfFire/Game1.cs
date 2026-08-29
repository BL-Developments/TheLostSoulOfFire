using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Debugging;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire;

public sealed class Game1 : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;
    private InputState _input = null!;
    private GameWorld _world = null!;
    private ArtAssets _art = null!;
    private readonly bool _audioGameplayTest;
    private readonly bool _audioDeathRestartTest;
    private bool _screenshotRequested;
    private string _screenshotStatus = string.Empty;
    private float _audioTestTotalTime;
    private float _audioTestStateTime;
    private int _audioTestWave;
    private bool _audioTestWaveKilled;
    private bool _audioTestCompleteSeen;
    private bool _audioTestRestartInjected;
    private bool _audioTestDeathRequested;

    public Game1(bool audioGameplayTest = false, bool audioDeathRestartTest = false)
    {
        _audioGameplayTest = audioGameplayTest;
        _audioDeathRestartTest = audioDeathRestartTest;
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
        Window.Title = "The Lost Soul of Fire — Prototype";
    }

    protected override void Initialize()
    {
        _input = new InputState();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        _art = new ArtAssets(Content);
        _world = new GameWorld(GraphicsDevice.Viewport, _art, Content);
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();
        if (_audioGameplayTest || _audioDeathRestartTest)
        {
            ConfigureAutomatedTest((float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        if (_input.IsKeyDown(Keys.Escape) ||
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        {
            Exit();
            return;
        }

        if (_input.WasKeyPressed(Keys.F9))
        {
            _screenshotRequested = true;
        }

        _world.Update(gameTime, _input, GraphicsDevice.Viewport);
        if (_audioGameplayTest || _audioDeathRestartTest)
        {
            FinishAutomatedTestFrame();
        }
        Window.Title = string.IsNullOrEmpty(_screenshotStatus) ? _world.WindowTitle : _screenshotStatus;
        _screenshotStatus = string.Empty;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(GameBalance.VoidColor);
        _world.Draw(_spriteBatch, _pixel, GraphicsDevice.Viewport);

        if (_screenshotRequested)
        {
            _screenshotRequested = false;
            _screenshotStatus = ScreenshotCapture.TrySaveBackBuffer(
                GraphicsDevice,
                _world.ScreenshotContext,
                out string path)
                ? $"Screenshot saved — {path}"
                : $"Screenshot failed — {path}";
        }

        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _world.Dispose();
        _pixel.Dispose();
        _spriteBatch.Dispose();
        base.UnloadContent();
    }

    private void ConfigureAutomatedTest(float deltaTime)
    {
        _audioTestTotalTime += deltaTime;
        _audioTestStateTime += deltaTime;

        if (_world.LoopState == ArenaLoopState.Title)
        {
            _input.InjectKeyPress(Keys.Space);
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

    private void FinishAutomatedTestFrame()
    {
        if (_audioDeathRestartTest)
        {
            if (_audioTestRestartInjected && !_world.PlayerDead && _world.LoopState == ArenaLoopState.Intro)
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

        if (_audioTestRestartInjected && _world.LoopState == ArenaLoopState.Intro && _world.WaveNumber == 0)
        {
            Console.WriteLine("AUDIO_GAMEPLAY_TEST_PASS waves=4 completion=true restart=true");
            Environment.ExitCode = 0;
            Exit();
        }
    }
}
