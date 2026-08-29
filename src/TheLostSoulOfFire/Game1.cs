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
    private bool _screenshotRequested;
    private string _screenshotStatus = string.Empty;

    public Game1()
    {
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
}
