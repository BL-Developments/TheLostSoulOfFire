using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TheLostSoulOfFire.Core;

public abstract class GameCore : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly int _virtualWidth;
    private readonly int _virtualHeight;
    private RenderTarget2D? _virtualTarget;

    protected GameCore(string title, int virtualWidth, int virtualHeight)
    {
        if (Instance is not null) throw new InvalidOperationException("Only one GameCore instance can exist.");
        Instance = this;
        _virtualWidth = virtualWidth;
        _virtualHeight = virtualHeight;
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
            SynchronizeWithVerticalRetrace = true
        };
        Window.Title = title;
        Window.AllowUserResizing = true;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);
    }

    public static GameCore Instance { get; private set; } = null!;
    public static SpriteBatch SpriteBatch { get; private set; } = null!;
    public static InputManager Input { get; private set; } = null!;
    public static SceneManager Scenes { get; private set; } = null!;
    public static ResolutionManager Resolution { get; private set; } = null!;
    public static GameAssets Assets { get; private set; } = null!;

    protected sealed override void Initialize()
    {
        Input = new InputManager();
        Resolution = new ResolutionManager(_virtualWidth, _virtualHeight);
        Scenes = new SceneManager(Services);
        Window.ClientSizeChanged += OnClientSizeChanged;
        base.Initialize();
        PostInitialize();
    }

    protected virtual void PostInitialize() { }

    protected sealed override void LoadContent()
    {
        SpriteBatch = new SpriteBatch(GraphicsDevice);
        _virtualTarget = new RenderTarget2D(GraphicsDevice, _virtualWidth, _virtualHeight, false, SurfaceFormat.Color, DepthFormat.None);
        Assets = GameAssets.Load(Content);
        Resolution.Update(GraphicsDevice.PresentationParameters.BackBufferWidth, GraphicsDevice.PresentationParameters.BackBufferHeight);
        Scenes.ApplyPendingChange();
    }

    protected sealed override void Update(GameTime gameTime)
    {
        Input.Update();
        if (Input.IsKeyPressed(Keys.Escape))
        {
            Exit();
            base.Update(gameTime);
            return;
        }

        Scenes.ApplyPendingChange();
        Scenes.Update(gameTime);
        base.Update(gameTime);
    }

    protected sealed override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_virtualTarget);
        GraphicsDevice.Clear(Palette.Void);
        Scenes.Draw(gameTime);
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);
        SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp);
        SpriteBatch.Draw(_virtualTarget!, Resolution.Destination, Color.White);
        SpriteBatch.End();
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Window.ClientSizeChanged -= OnClientSizeChanged;
            Scenes?.Dispose();
            _virtualTarget?.Dispose();
            SpriteBatch?.Dispose();
        }
        Instance = null!;
        base.Dispose(disposing);
    }

    private void OnClientSizeChanged(object? sender, EventArgs e) =>
        Resolution.Update(Window.ClientBounds.Width, Window.ClientBounds.Height);
}
