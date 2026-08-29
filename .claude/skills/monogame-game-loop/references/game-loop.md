# MonoGame Game Loop Reference

## Table of Contents
1. [Timestep configuration](#timestep-configuration)
2. [GameTime properties](#gametime-properties)
3. [Timer accumulator patterns](#timer-accumulator-patterns)
4. [Variable-step movement](#variable-step-movement)
5. [Game lifecycle methods](#game-lifecycle-methods)
6. [GameComponent and DrawableGameComponent](#gamecomponent-and-drawablegamecomponent)
7. [GameService registration](#gameservice-registration)
8. [Async programming patterns](#async-programming-patterns)
9. [Window and display management](#window-and-display-management)

---

## Timestep configuration

```csharp
// In Game constructor:

// Fixed timestep (default — 60 FPS):
IsFixedTimeStep   = true;
TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60.0);

// Fixed at 30 FPS:
IsFixedTimeStep   = true;
TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 30.0);

// Variable timestep (unlimited):
IsFixedTimeStep = false;

// After a long pause or loading screen — prevents Update catch-up:
ResetElapsedTime();
```

---

## GameTime properties

```csharp
// In Update(GameTime gameTime):

// Time since last Update call (use for all delta calculations):
float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
double deltaMs = gameTime.ElapsedGameTime.TotalMilliseconds;

// Total time since game start:
double totalSeconds = gameTime.TotalGameTime.TotalSeconds;
float totalSecondsF = (float)gameTime.TotalGameTime.TotalSeconds;

// True if Update is being called extra times to catch up:
bool slow = gameTime.IsRunningSlowly;
```

---

## Timer accumulator patterns

### Cooldown timer (counts down to zero)

```csharp
private float _cooldown = 0f;
private const float CooldownMax = 0.3f; // seconds

// In Update():
float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
if (_cooldown > 0f) _cooldown -= delta;

bool CanFire => _cooldown <= 0f;

void Fire()
{
    if (!CanFire) return;
    // Preserve overshoot for accuracy at low FPS:
    _cooldown = CooldownMax + _cooldown; // NOT = CooldownMax
    DoFire();
}
```

### Elapsed timer (counts up to threshold)

```csharp
private float _elapsed = 0f;
private const float Duration = 2.0f;

// In Update():
_elapsed += delta;
if (_elapsed >= Duration)
{
    _elapsed -= Duration; // preserve overshoot
    OnTimerFired();
}
```

### One-shot delay (fire once after N seconds)

```csharp
private float _delay = 0f;
private bool  _fired = false;

void StartDelay(float seconds) { _delay = seconds; _fired = false; }

// In Update():
if (!_fired && _delay > 0f)
{
    _delay -= delta;
    if (_delay <= 0f)
    {
        _fired = true;
        OnDelayComplete();
    }
}
```

### Timeout / inactivity timer

```csharp
private double _inactiveMs = 0.0;
private const double TimeoutMs = 4000.0;

// In Update():
if (InputDetected())
    _inactiveMs = 0.0;
else
    _inactiveMs += gameTime.ElapsedGameTime.TotalMilliseconds;

if (_inactiveMs >= TimeoutMs)
    Exit();
```

---

## Variable-step movement

All movement and animation must scale by delta in variable-step mode:

```csharp
// Position:
_position += _velocity * delta;

// Rotation:
_rotation += _angularVelocity * delta;

// Lerp to target (frame-rate-independent smooth approach):
float smoothing = 1f - MathF.Pow(0.001f, delta); // exponential decay
_position = Vector2.Lerp(_position, _target, smoothing);
```

---

## Game lifecycle methods

```csharp
protected override void Initialize()
{
    // Called once before LoadContent.
    // Set up logical state, constants, component references.
    // Do NOT load assets here — no ContentManager yet.
    base.Initialize(); // calls Initialize on all Components
}

protected override void LoadContent()
{
    // Called once after Initialize.
    // Load ALL assets via Content.Load<T>().
    // Create SpriteBatch, RenderTarget2D, SoundEffectInstance here.
}

protected override void Update(GameTime gameTime)
{
    // Called every logic tick.
    // Input → physics → AI → timers → collision.
    base.Update(gameTime); // updates all Components
}

protected override void Draw(GameTime gameTime)
{
    // Called every render tick (may be skipped in fixed-step catch-up).
    // Rendering ONLY — no logic, no state mutation.
    base.Draw(gameTime); // draws all DrawableGameComponents
}

protected override void UnloadContent()
{
    // Called on exit or when Content is being replaced.
    // Dispose manually-held resources (RenderTarget2D, SoundEffectInstance).
    // ContentManager.Unload() handles assets loaded through it.
}

// Skip one Draw frame (call inside Update):
SuppressDraw();
```

---

## GameComponent and DrawableGameComponent

```csharp
// Updatable component (no rendering):
public class PhysicsSystem : GameComponent
{
    public PhysicsSystem(Game game) : base(game) { }

    public override void Initialize() { base.Initialize(); }
    public override void Update(GameTime gameTime) { /* physics logic */ }
}

// Updatable + renderable component:
public class ParticleSystem : DrawableGameComponent
{
    private SpriteBatch _sb;

    public ParticleSystem(Game game) : base(game) { }

    protected override void LoadContent()
    {
        _sb = new SpriteBatch(GraphicsDevice);
    }

    public override void Update(GameTime gameTime) { /* update particles */ }

    public override void Draw(GameTime gameTime)
    {
        _sb.Begin();
        // draw particles
        _sb.End();
    }
}

// Register in Game.Initialize():
Components.Add(new PhysicsSystem(this));
Components.Add(new ParticleSystem(this));

// DrawOrder / UpdateOrder control execution sequence:
var renderer = new ParticleSystem(this) { DrawOrder = 100 };
Components.Add(renderer);
```

Components are updated/drawn in `UpdateOrder`/`DrawOrder` ascending order. Lower = earlier.

---

## GameService registration

```csharp
// Register a service in Initialize() or LoadContent():
Services.AddService(typeof(ICollisionService), new CollisionSystem());

// Retrieve from any component or scene:
var collision = (ICollisionService)Game.Services.GetService(typeof(ICollisionService));

// Typed helper (C# generic):
T GetService<T>() where T : class
    => (T)Game.Services.GetService(typeof(T));
```

---

## Async programming patterns

### Polling pattern (simple, no callbacks)

```csharp
private Task _loadTask;

protected override void LoadContent()
{
    _loadTask = Task.Run(() => LoadHeavyData());
}

protected override void Update(GameTime gameTime)
{
    if (_loadTask is { IsCompleted: true })
    {
        _loadTask = null;
        TransitionToGame();
    }
}
```

### ConcurrentQueue for thread-safe results

```csharp
private ConcurrentQueue<SaveResult> _saveResults = new();

void SaveAsync(GameData data)
{
    Task.Run(() =>
    {
        // runs on thread pool — NO GraphicsDevice calls here
        var result = WriteToFile(data);
        _saveResults.Enqueue(result);
    });
}

// In Update():
while (_saveResults.TryDequeue(out SaveResult result))
    HandleSaveResult(result); // back on main thread — safe
```

**Never call `GraphicsDevice`, `SpriteBatch`, or `ContentManager` from a background thread.**

---

## Window and display management

```csharp
// Allow window resizing:
Window.AllowUserResizing = true;
Window.ClientSizeChanged += OnWindowResize;

private void OnWindowResize(object sender, EventArgs e)
{
    int w = GraphicsDevice.Viewport.Width;
    int h = GraphicsDevice.Viewport.Height;
    // rebuild render targets, scale matrices, etc.
}

// Set window title:
Window.Title = "My Game";

// Toggle fullscreen:
_graphics.ToggleFullScreen();
_graphics.ApplyChanges();

// Set preferred resolution:
_graphics.PreferredBackBufferWidth  = 1920;
_graphics.PreferredBackBufferHeight = 1080;
_graphics.ApplyChanges();
```
