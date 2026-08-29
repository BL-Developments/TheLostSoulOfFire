# MonoGame Scene Management Reference

## Table of Contents
1. [Scene base class](#scene-base-class)
2. [SceneManager — state machine](#scenemanager--state-machine)
3. [SceneManager — stack (pause overlay)](#scenemanager--stack-pause-overlay)
4. [Scene lifecycle sequence](#scene-lifecycle-sequence)
5. [Transition effect (fade)](#transition-effect-fade)
6. [Loading screen pattern](#loading-screen-pattern)
7. [UI layering in Draw](#ui-layering-in-draw)
8. [Game state management within a scene](#game-state-management-within-a-scene)
9. [Input buffering](#input-buffering)

---

## Scene base class

```csharp
public abstract class Scene : IDisposable
{
    protected Game          Game        { get; }
    protected SpriteBatch   SpriteBatch { get; }
    protected ContentManager Content    { get; }
    protected GraphicsDevice GraphicsDevice => Game.GraphicsDevice;

    protected Scene(Game game, SpriteBatch spriteBatch)
    {
        Game        = game;
        SpriteBatch = spriteBatch;
        Content     = new ContentManager(game.Services, "Content");
    }

    public virtual  void Initialize()               { }
    public virtual  void LoadContent()              { }
    public abstract void Update(GameTime gameTime);
    public abstract void Draw(GameTime gameTime);

    public virtual void UnloadContent()
    {
        Content.Unload();
    }

    public void Dispose()
    {
        UnloadContent();
        Content.Dispose();
    }
}
```

---

## SceneManager — state machine

Replaces the current scene. Use for non-overlapping transitions (Title → Game, Game → GameOver).

```csharp
public class SceneManager
{
    private Scene _current;
    private Scene _pending;   // queued — applied at start of next frame

    public void SwitchTo(Scene next)
    {
        _pending = next;
    }

    public void Update(GameTime gameTime)
    {
        // Apply deferred switch BEFORE any scene logic:
        if (_pending != null)
        {
            _current?.Dispose();
            _current = _pending;
            _pending = null;
            _current.Initialize();
            _current.LoadContent();
        }
        _current?.Update(gameTime);
    }

    public void Draw(GameTime gameTime) => _current?.Draw(gameTime);

    public void Dispose() => _current?.Dispose();
}
```

---

## SceneManager — stack (pause overlay)

Keeps multiple scenes active; only the top one updates, all draw bottom-to-top.

```csharp
public class SceneStackManager
{
    private readonly Stack<Scene> _stack = new();

    // Pending operations — never modify stack mid-Update:
    private Scene _pendingPush;
    private bool  _pendingPop;

    public void Push(Scene scene) => _pendingPush = scene;
    public void Pop()             => _pendingPop  = true;

    public void Update(GameTime gameTime)
    {
        // Flush pending at top of frame:
        if (_pendingPop && _stack.Count > 0)
        {
            _pendingPop = false;
            _stack.Peek().Dispose();
            _stack.Pop();
        }

        if (_pendingPush != null)
        {
            var scene = _pendingPush;
            _pendingPush = null;
            scene.Initialize();
            scene.LoadContent();
            _stack.Push(scene);
        }

        // Only top scene updates:
        if (_stack.Count > 0)
            _stack.Peek().Update(gameTime);
    }

    public void Draw(GameTime gameTime)
    {
        // All scenes draw, bottom to top:
        foreach (var scene in _stack.Reverse())
            scene.Draw(gameTime);
    }

    public void Dispose()
    {
        foreach (var scene in _stack)
            scene.Dispose();
        _stack.Clear();
    }
}
```

---

## Scene lifecycle sequence

```
Constructor
    │
    ▼
Initialize()         ← set up state without assets
    │
    ▼
LoadContent()        ← create ContentManager, load all textures/sounds/fonts
    │
    ▼ (loop)
Update(GameTime)     ← input, physics, AI, timers
Draw(GameTime)       ← rendering only
    │
    ▼ (on transition)
UnloadContent()      ← Content.Unload(), stop music, stop SoundEffectInstances
    │
    ▼
Dispose()            ← Content.Dispose(), free any other managed resources
```

---

## Transition effect (fade)

```csharp
public class FadeTransition
{
    private float _alpha     = 0f;
    private float _speed     = 1.5f;   // 1 / fade duration in seconds
    private bool  _fadingOut = true;   // true = fading to black
    private Action _onComplete;

    // Start fade-out → action → fade-in:
    public void Start(float speed, Action onMidpoint)
    {
        _speed      = speed;
        _alpha      = 0f;
        _fadingOut  = true;
        _onComplete = onMidpoint;
    }

    // Call in Update():
    public void Update(float delta)
    {
        if (_fadingOut)
        {
            _alpha += delta * _speed;
            if (_alpha >= 1f)
            {
                _alpha = 1f;
                _onComplete?.Invoke();  // e.g. switch scene here
                _onComplete = null;
                _fadingOut  = false;
            }
        }
        else
        {
            _alpha -= delta * _speed;
            _alpha = MathF.Max(_alpha, 0f);
        }
    }

    // Call at the END of Draw(), after all other rendering:
    public void Draw(SpriteBatch sb, Texture2D pixel, Rectangle screen)
    {
        if (_alpha <= 0f) return;
        sb.Begin();
        sb.Draw(pixel, screen, Color.Black * _alpha);
        sb.End();
    }

    public bool IsActive => _fadingOut || _alpha > 0f;
}
```

Usage:

```csharp
_fade.Start(speed: 2f, onMidpoint: () => _sceneManager.SwitchTo(new GameScene(Game, _sb)));
```

---

## Loading screen pattern

```csharp
public class LoadingScene : Scene
{
    private Task   _loadTask;
    private bool   _ready;
    private Scene  _nextScene;

    public LoadingScene(Game game, SpriteBatch sb, Scene nextScene) : base(game, sb)
    {
        _nextScene = nextScene;
    }

    public override void LoadContent()
    {
        base.LoadContent();
        _loadFont = Content.Load<SpriteFont>("Fonts/UI");

        // Heavy loading on background thread:
        _loadTask = Task.Run(() =>
        {
            _nextScene.LoadContent(); // loads on thread pool
        });
    }

    public override void Update(GameTime gameTime)
    {
        if (_loadTask is { IsCompleted: true } && !_ready)
        {
            _ready = true;
            // Re-init must happen on main thread (GraphicsDevice):
            _nextScene.Initialize();
            _sceneManager.SwitchTo(_nextScene);
        }
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        SpriteBatch.Begin();
        SpriteBatch.DrawString(_loadFont, "Loading...", new Vector2(400, 300), Color.White);
        SpriteBatch.End();
    }
}
```

---

## UI layering in Draw

Always separate world and UI into different `SpriteBatch.Begin`/`End` pairs. UI uses screen space — no camera transform.

```csharp
public override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(Color.CornflowerBlue);

    // ── World layer (with camera) ──────────────────────────────
    SpriteBatch.Begin(
        SpriteSortMode.BackToFront,
        BlendState.AlphaBlend,
        SamplerState.PointClamp,
        transformMatrix: _camera.TransformMatrix
    );
    DrawTilemap();
    DrawEntities();
    SpriteBatch.End();

    // ── Particle layer (additive, with camera) ─────────────────
    SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive,
        transformMatrix: _camera.TransformMatrix);
    DrawParticles();
    SpriteBatch.End();

    // ── UI layer (screen space — NO camera transform) ──────────
    SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
    DrawHUD();
    DrawDialogue();
    DrawMinimapOverlay();
    SpriteBatch.End();
}
```

---

## Game state management within a scene

```csharp
public enum GameState { Playing, Paused, GameOver }

public class GameScene : Scene
{
    private GameState _state = GameState.Playing;

    public override void Update(GameTime gameTime)
    {
        switch (_state)
        {
            case GameState.Playing:
                UpdatePlaying(gameTime);
                if (_input.IsPressed(Keys.Escape))
                    _state = GameState.Paused;
                break;

            case GameState.Paused:
                UpdatePauseMenu(gameTime);
                if (_input.IsPressed(Keys.Escape))
                    _state = GameState.Playing;
                break;

            case GameState.GameOver:
                UpdateGameOver(gameTime);
                break;
        }
    }

    private void OnPlayerDied()
    {
        _gameOverTimer = 1.5f;
        // Set state in Update when timer expires, not here
    }
}
```

---

## Input buffering

For grid-based or turn-based games where input must survive across multiple update ticks:

```csharp
private Queue<Direction> _inputBuffer = new(capacity: 3);

// In Update():
if (_input.IsPressed(Keys.Right) && _inputBuffer.Count < 3)
    _inputBuffer.Enqueue(Direction.Right);

// Process one input per movement tick:
if (_movementReady && _inputBuffer.Count > 0)
{
    Direction next = _inputBuffer.Dequeue();
    StartMovement(next);
}
```
