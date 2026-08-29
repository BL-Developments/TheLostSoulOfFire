---
name: monogame-platform
description: MonoGame platform and window management guide covering desktop window resizing (AllowUserResizing, ClientSizeChanged, aspect ratio), mobile device orientation (SupportedOrientations, DisplayOrientation, backbuffer vs display), back button behavior, auto-save on interruption, touch-first control design, and audio/polish best practices for mobile. Use this skill whenever the user asks about resizing the game window, handling resolution changes, supporting landscape or portrait mode, the back button, mobile best practices, auto-save on exit, display orientation, or platform-specific window behavior — even if they just say "my game doesn't handle resize" or "how do I support mobile orientation".
---

# MonoGame Platform & Window Management Guide

This skill covers platform-specific concerns: desktop window resizing and mobile orientation, back button handling, and mobile best practices. For code patterns and API signatures, see `references/platform.md`.

## Desktop: Window Resizing

By default, MonoGame desktop windows are not resizable. To allow the user to drag the window border:

```csharp
// In Initialize():
Window.AllowUserResizing = true;
Window.ClientSizeChanged += OnClientSizeChanged;
```

When the window is resized, the `ClientSizeChanged` event fires. Use it to recalculate any viewport-dependent values (camera, UI anchors, render targets):

```csharp
private void OnClientSizeChanged(object sender, EventArgs e)
{
    // New dimensions are available immediately:
    int newWidth  = GraphicsDevice.Viewport.Width;
    int newHeight = GraphicsDevice.Viewport.Height;

    // Rebuild render targets, recalculate scale matrix, reposition UI:
    RecalculateScaleMatrix();
    RebuildRenderTargets();
}
```

### Aspect Ratio

Always design at a fixed **virtual resolution** and scale to the actual window. This keeps game-world coordinates stable regardless of window size. Combine this with the resolution-independence pattern from `monogame-graphics`:

```csharp
private void RecalculateScaleMatrix()
{
    float scaleX = GraphicsDevice.Viewport.Width  / (float)VirtualWidth;
    float scaleY = GraphicsDevice.Viewport.Height / (float)VirtualHeight;
    _scaleMatrix = Matrix.CreateScale(scaleX, scaleY, 1f);
}
```

On resize, **dispose and recreate any `RenderTarget2D`** that was sized to the window — they do not resize automatically.

## Mobile: Device Orientation

### Automatic rotation

MonoGame handles rotation in hardware at zero performance cost. When the device is rotated, it fires `Window.OrientationChanged` and automatically rescales the back buffer.

**Back buffer vs display resolution** — these are different values after rotation:
- `_graphics.PreferredBackBufferWidth / Height` — the scaled render resolution
- `GraphicsDevice.DisplayMode.Width / Height` — the physical screen resolution
- `Window.ClientBounds.Width / Height` — same as DisplayMode after rotation

### Setting supported orientations

```csharp
// In the Game constructor (before Initialize):
_graphics.SupportedOrientations =
    DisplayOrientation.LandscapeLeft |
    DisplayOrientation.LandscapeRight;
```

Setting `DisplayOrientation.Default` lets MonoGame infer from the back buffer dimensions:
- `PreferredBackBufferWidth > PreferredBackBufferHeight` → landscape (auto switches Left/Right)
- `PreferredBackBufferWidth < PreferredBackBufferHeight` → portrait

To **lock** orientation, set only one flag:

```csharp
_graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft;
```

### Available `DisplayOrientation` values

| Value | Description |
|-------|-------------|
| `Default` | Inferred from back buffer dimensions |
| `LandscapeLeft` | Home button on the left |
| `LandscapeRight` | Home button on the right |
| `Portrait` | Home button at the bottom |
| `PortraitDown` | Home button at the top (rare) |

## Back Button Handling

On mobile (Android) and console, the **Back** button must be handled correctly — the OS may terminate the app if it is not consumed. The expected behavior is:

| Context | Back button action |
|---------|-------------------|
| During gameplay | Show pause menu |
| In pause menu | Resume game |
| In any menu | Return to previous screen |
| At the start screen | Exit the game |

```csharp
// In Update():
bool backPressed = GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
                   && _prevPad.Buttons.Back == ButtonState.Released;

if (backPressed)
{
    if (_state == GameState.Playing)
        PushPauseMenu();
    else if (_state == GameState.Paused)
        PopPauseMenu(); // resume
    else if (_state == GameState.MainMenu)
        Exit();
}
```

On desktop, **Escape** should follow the same pattern. On Android, not consuming the Back button causes the activity to finish via the OS, bypassing your save logic.

## Auto-Save on Interruption

Mobile games can be interrupted at any time (phone call, notification, Home button). Always save game state:

1. Hook into the `Deactivated` event (app loses focus / goes to background):

```csharp
// In Initialize():
Activated   += OnActivated;
Deactivated += OnDeactivated;

private void OnDeactivated(object sender, EventArgs e)
{
    // Save automatically — the game might not come back
    _saveSystem.SaveQuick(_currentState);
}

private void OnActivated(object sender, EventArgs e)
{
    // Restore or prompt to resume from auto-save
}
```

2. Also save when the Back button shows a pause menu (before the player can navigate away).

3. Distinguish **auto-save** from **manual save** — on resume, let the player choose which to load.

4. Show a **"do not exit"** visual cue during save to warn the player not to press Home/Search.

## Mobile Control Best Practices

- **Use gestures and direct touch** — never simulate physical thumbsticks on screen. Virtual sticks waste gameplay area and feel unnatural.
- **Design gameplay around touch**: drag to move, tap to select, pinch to zoom, two-finger rotate.
- **Back button = pause/back-navigate**, never an in-game action (attack, confirm).
- **Average audio volume** — don't ship with max-volume sound effects; players should not need to adjust device volume mid-game.
- **Allow sound toggle** — both music and SFX should be individually disableable from within the game.
- **Polish matters**: animated button presses, smooth screen transitions, and styled UI are the difference between a shipped game and a shipped good game.

## Anti-Patterns to Avoid

- **Never ignore `ClientSizeChanged`** on desktop if `AllowUserResizing = true` — game world will appear stretched or clipped.
- **Never recreate `RenderTarget2D` every frame** — only on resize. Store them as fields and dispose the old one first.
- **Never let the Back button go unhandled on Android** — causes OS-level app termination, skipping your save code.
- **Never save synchronously on the main thread** in `Deactivated` — use `Task.Run` or a queued write to avoid hangs (see `monogame-async` skill).
- **Never use `DisplayMode` to get the back buffer size after rotation** — use `PreferredBackBufferWidth/Height` instead; `DisplayMode` reflects the physical screen, not the scaled render target.

## Reference

For full API signatures, `ClientSizeChanged` + `OrientationChanged` event patterns, and `SupportedOrientations` setup examples, see `references/platform.md`.
