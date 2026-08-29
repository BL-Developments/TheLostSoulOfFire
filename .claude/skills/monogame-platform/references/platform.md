# MonoGame Platform & Window Reference

## Table of Contents
1. [Window resizing (desktop)](#window-resizing-desktop)
2. [Full-screen toggle](#full-screen-toggle)
3. [Virtual resolution + scale matrix](#virtual-resolution--scale-matrix)
4. [Device orientation (mobile)](#device-orientation-mobile)
5. [OrientationChanged event](#orientationchanged-event)
6. [Back button handling](#back-button-handling)
7. [App lifecycle events (Activated / Deactivated)](#app-lifecycle-events-activated--deactivated)
8. [Auto-save on interruption](#auto-save-on-interruption)
9. [DisplayOrientation enum values](#displayorientation-enum-values)
10. [Back-buffer vs display resolution cheat sheet](#back-buffer-vs-display-resolution-cheat-sheet)

---

## Window resizing (desktop)

```csharp
protected override void Initialize()
{
    Window.AllowUserResizing = true;
    Window.ClientSizeChanged += OnClientSizeChanged;
    base.Initialize();
}

private void OnClientSizeChanged(object sender, EventArgs e)
{
    int w = GraphicsDevice.Viewport.Width;
    int h = GraphicsDevice.Viewport.Height;

    // Rebuild anything that depends on window size:
    _scaleMatrix = ComputeScaleMatrix(w, h);

    // Dispose old render targets before recreating:
    _sceneTarget?.Dispose();
    _sceneTarget = new RenderTarget2D(GraphicsDevice, w, h);

    // Reposition fixed UI anchors:
    _hudBottomLeft = new Vector2(8, h - 40);
}
```

---

## Full-screen toggle

```csharp
// Toggle full-screen at runtime:
private void ToggleFullScreen()
{
    _graphics.IsFullScreen = !_graphics.IsFullScreen;
    _graphics.ApplyChanges();
}

// Set at startup in constructor:
_graphics = new GraphicsDeviceManager(this)
{
    PreferredBackBufferWidth  = 1920,
    PreferredBackBufferHeight = 1080,
    IsFullScreen              = false
};
```

---

## Virtual resolution + scale matrix

Design at a fixed virtual canvas. Scale to any real window on every resize.

```csharp
private const int VirtualWidth  = 1280;
private const int VirtualHeight = 720;
private Matrix    _scaleMatrix;

private Matrix ComputeScaleMatrix(int realW, int realH)
{
    float sx = realW / (float)VirtualWidth;
    float sy = realH / (float)VirtualHeight;
    return Matrix.CreateScale(sx, sy, 1f);
}

// In Draw() — all SpriteBatch calls use this matrix:
_spriteBatch.Begin(transformMatrix: _scaleMatrix);
```

To preserve aspect ratio with letterboxing instead of stretching:

```csharp
private Matrix ComputeLetterboxMatrix(int realW, int realH)
{
    float scale   = Math.Min(realW / (float)VirtualWidth, realH / (float)VirtualHeight);
    float offsetX = (realW - VirtualWidth  * scale) / 2f;
    float offsetY = (realH - VirtualHeight * scale) / 2f;

    return Matrix.CreateScale(scale, scale, 1f) *
           Matrix.CreateTranslation(offsetX, offsetY, 0f);
}
```

---

## Device orientation (mobile)

```csharp
// In Game constructor — before Initialize():

// Allow landscape both ways (auto-detects from back buffer if Default):
_graphics.SupportedOrientations =
    DisplayOrientation.LandscapeLeft |
    DisplayOrientation.LandscapeRight;

// Lock to portrait only:
_graphics.SupportedOrientations = DisplayOrientation.Portrait;

// Let MonoGame decide from back buffer dimensions:
_graphics.SupportedOrientations = DisplayOrientation.Default;
// (landscape if PreferredBackBufferWidth > Height, portrait otherwise)
```

Setting a specific back buffer forces the inferred orientation when using `Default`:

```csharp
_graphics.PreferredBackBufferWidth  = 1280;  // → landscape
_graphics.PreferredBackBufferHeight = 720;
```

---

## OrientationChanged event

```csharp
protected override void Initialize()
{
    Window.OrientationChanged += OnOrientationChanged;
    base.Initialize();
}

private void OnOrientationChanged(object sender, EventArgs e)
{
    // Back buffer dimensions have already been updated by MonoGame:
    int w = _graphics.PreferredBackBufferWidth;
    int h = _graphics.PreferredBackBufferHeight;

    _scaleMatrix = ComputeScaleMatrix(w, h);
    // Recreate render targets if size-dependent:
    _sceneTarget?.Dispose();
    _sceneTarget = new RenderTarget2D(GraphicsDevice, w, h);
}
```

---

## Back button handling

```csharp
// Fields:
private GamePadState _prevPad;
private GamePadState _currPad;

// TOP of Update():
_prevPad = _currPad;
_currPad = GamePad.GetState(PlayerIndex.One);

// Detect Back (or Escape on desktop):
bool backPressed =
    (_currPad.Buttons.Back == ButtonState.Pressed && _prevPad.Buttons.Back == ButtonState.Released)
    || IsKeyJustPressed(Keys.Escape);

if (backPressed)
{
    switch (_currentState)
    {
        case GameState.Playing:
            PushScene(new PauseMenuScene(this));
            break;
        case GameState.PauseMenu:
            PopScene(); // back to gameplay
            break;
        case GameState.AnyMenu:
            PopScene(); // back to previous menu
            break;
        case GameState.MainMenu:
            Exit();     // at root — quit the game
            break;
    }
}
```

---

## App lifecycle events (Activated / Deactivated)

```csharp
protected override void Initialize()
{
    Activated   += OnActivated;
    Deactivated += OnDeactivated;
    base.Initialize();
}

private void OnDeactivated(object sender, EventArgs e)
{
    // App going to background (call, notification, Home button).
    // Save NOW — the app may not return.
    PauseGame();
    QueueAutoSave(); // async save via Task.Run (see monogame-async skill)
}

private void OnActivated(object sender, EventArgs e)
{
    // App returned to foreground.
    // Music may need to be restarted on some platforms.
    if (_wasPlayingMusic)
        MediaPlayer.Resume();
}
```

---

## Auto-save on interruption

```csharp
// Minimal auto-save pattern:
private Task? _saveTask;

private void QueueAutoSave()
{
    if (_saveTask?.IsCompleted == false) return; // save already in progress

    var snapshot = _gameState.TakeSnapshot(); // capture a plain-data copy
    _saveTask = Task.Run(async () =>
    {
        var json = JsonSerializer.Serialize(snapshot);
        await File.WriteAllTextAsync(_autoSavePath, json);
    });
}

// In Update() — check result to surface errors:
if (_saveTask?.IsCompleted == true)
{
    if (_saveTask.IsFaulted)
        ShowSaveError(_saveTask.Exception!.GetBaseException().Message);
    _saveTask = null;
}
```

---

## DisplayOrientation enum values

```csharp
DisplayOrientation.Default       // Inferred from back buffer aspect ratio
DisplayOrientation.LandscapeLeft  // Physical home button on the left  (90° CCW from portrait)
DisplayOrientation.LandscapeRight // Physical home button on the right (90° CW  from portrait)
DisplayOrientation.Portrait       // Home button at the bottom (vertical)
DisplayOrientation.PortraitDown   // Home button at the top (upside-down portrait — rare)
```

Combine with bitwise OR for multi-orientation support:
```csharp
DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight
```

---

## Back-buffer vs display resolution cheat sheet

| Property | What it returns | Changes on rotation? |
|----------|----------------|----------------------|
| `_graphics.PreferredBackBufferWidth/Height` | Render target dimensions (scaled) | Yes — swaps W and H |
| `GraphicsDevice.Viewport.Width/Height` | Active viewport (usually same as back buffer) | Yes |
| `GraphicsDevice.DisplayMode.Width/Height` | Physical screen dimensions | No — always physical |
| `Window.ClientBounds.Width/Height` | Window client area in screen pixels | Yes |

**Rule**: use `PreferredBackBufferWidth/Height` for rendering math after rotation; use `DisplayMode` only when you need the native screen resolution for DPI calculations.
