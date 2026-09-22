---
name: monogame-game-loop
description: MonoGame game loop implementation guide covering fixed vs variable timestep, timing patterns, timer accumulators, async programming, and GameComponent architecture. Use this skill whenever the user asks about frame rate, game timing, Update/Draw lifecycle, IsFixedTimeStep, delta time, timers, async tasks, GameComponent, or any game loop architecture question in MonoGame.
---

# MonoGame Game Loop Implementation Guide

This skill guides game loop architecture in MonoGame. For the `GameComponent` and `GameService` patterns, see `references/game-loop.md`.

## Fixed vs Variable Timestep

### Fixed timestep (default — recommended for most games)

```csharp
// In Game constructor:
IsFixedTimeStep   = true;
TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0); // 60 FPS
```

MonoGame calls `Update()` at exactly 60 Hz regardless of how long rendering takes. If the game falls behind, it calls `Update()` multiple times in a row and sets `gameTime.IsRunningSlowly = true`. Use fixed timestep for physics, deterministic simulations, and networked games.

### Variable timestep (use for smooth rendering at high refresh rates)

```csharp
// In Game constructor:
IsFixedTimeStep = false;
```

`Update()` and `Draw()` are called as fast as the hardware allows. **Every movement, animation, and physics calculation must be multiplied by delta time** — otherwise they run faster on faster machines.

```csharp
float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
_position += _velocity * delta;
```

## Handling `IsRunningSlowly`

When the game can't keep up with the target FPS, MonoGame sets `gameTime.IsRunningSlowly = true`. React by reducing work:

```csharp
protected override void Update(GameTime gameTime)
{
    if (gameTime.IsRunningSlowly)
    {
        // Skip non-critical updates (particle effects, background animations)
        UpdateCriticalSystems(gameTime);
        return;
    }
    UpdateAllSystems(gameTime);
}
```

Do not suppress the flag — let MonoGame manage catch-up. Call `ResetElapsedTime()` after loading screens or long pauses to prevent a burst of Update calls.

## Timer Pattern

Never use `DateTime.Now`, `Environment.TickCount`, or `Thread.Sleep` for in-game timers. Use a `float` accumulator fed by `gameTime`:

```csharp
// Fields:
private float _shootCooldown;
private const float ShootCooldownMax = 0.25f; // seconds

// In Update():
float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
if (_shootCooldown > 0f)
    _shootCooldown -= delta;

// Trigger:
if (IsPressed(Keys.Space) && _shootCooldown <= 0f)
{
    Shoot();
    _shootCooldown = ShootCooldownMax;
}
```

For repeating timers, use subtraction rather than resetting to zero — this preserves leftover time and keeps timing accurate at low frame rates.

## Lifecycle Method Responsibilities

| Method | Responsibility |
|--------|----------------|
| `Initialize()` | Set up logical state, constants, and references that don't need assets |
| `LoadContent()` | Load all textures, sounds, effects, and fonts via `Content.Load<T>()` |
| `Update(GameTime)` | Advance game state: input, physics, AI, timers, collision |
| `Draw(GameTime)` | Render only — no logic, no state changes |
| `UnloadContent()` | Dispose manually-held resources (RenderTarget2D, SoundEffectInstance) |

**Never put logic in `Draw()`.** If you need to know whether to draw something, compute it in `Update()` and store the result in a field.

## Async Programming

For loading screens or long background operations (save games, network calls), use `Task.Run()` to move work off the game thread:

```csharp
// Polling approach (simple):
private Task _loadTask;

protected override void LoadContent()
{
    _loadTask = Task.Run(() => LoadHeavyDataAsync());
}

protected override void Update(GameTime gameTime)
{
    if (_loadTask != null && _loadTask.IsCompleted)
    {
        _loadTask = null;
        TransitionToGameScene();
    }
}
```

Synchronize results using a `ConcurrentQueue<T>` or a `volatile` flag — never access the `GraphicsDevice` from a background thread.

## SuppressDraw

Call `SuppressDraw()` inside `Update()` to skip the current frame's `Draw()` call. Useful during:
- Loading screens where nothing has changed visually
- Paused game-over states
- Scene transitions driven by Update logic only

```csharp
if (_isPaused && !_inputChanged)
    SuppressDraw();
```

## Core Base Class (Alca.MonoGame.Kernel)

Rather than inheriting from `Game` directly, derive from `Core` — the library's singleton that wires up all subsystems automatically.

```csharp
public sealed class MyGame : Core
{
    // Core constructor: title, width, height, fullScreen
    public MyGame() : base("My Game", 1920, 1080, false) { }

    // Optional hooks (called in order: PreInitialize → ConfigureServices → PostInitialize):
    protected override void PreInitialize() { /* pre-subsystem setup */ }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IMyService, MyService>();
    }

    protected override void PostInitialize()
    {
        Core.SceneManager.RequestChange(new MainMenuScene());
    }
}
```

`Core` wires the game loop, boots all subsystems, and handles:
- **F11** — toggle fullscreen
- **Escape** — exit the game if `Core.ExitOnEscape == true`

### Static Service Accessors

All subsystems are available as static properties on `Core`:

| Property | Type | Description |
|----------|------|-------------|
| `Core.Instance` | `Core` | The singleton game instance |
| `Core.Input` | `InputManager` | Keyboard / mouse / gamepad |
| `Core.Audio` | `AudioController` | Sound effects and music |
| `Core.SceneManager` | `SceneManager` | Scene push/pop/change |
| `Core.UIInteraction` | `UIInteractionManager` | Pointer event routing |
| `Core.UIFocus` | `UIFocusManager` | Keyboard focus tracking |
| `Core.UIOverlay` | `UIOverlayManager` | Overlay panel rendering |
| `Core.Localization` | `LocalizationManager` | Localized strings |
| `Core.Tweening` | tween engine | MonoGame.Extended tweening |
| `Core.Resolution` | resolution manager | Virtual resolution & scale |
| `Core.Platform` | platform info | Platform detection |
| `Core.SpriteBatch` | `SpriteBatch` | Shared sprite batch |
| `Core.GraphicsDevice` | `GraphicsDevice` | Graphics device |
| `Core.Content` | `ContentManager` | Root content manager |

These are safe to access from any scene or component after `PostInitialize()` completes.

## Rules

- Use the `float delta` accumulator pattern for all timers — never `DateTime.Now`.
- Multiply all position/velocity changes by `delta` in variable-step mode.
- Call `ResetElapsedTime()` after long pauses or loading to prevent Update catch-up bursts.
- Never call `GraphicsDevice` methods from a background `Task` — it is not thread-safe.
- Keep `Draw()` pure rendering — all decisions about what to draw must be made in `Update()`.

## Reference

For `GameComponent`, `DrawableGameComponent`, and `GameService` registration patterns, see `references/game-loop.md`.
