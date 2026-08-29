---
name: monogame-input
description: MonoGame input implementation guide covering keyboard, mouse, gamepad, and touch patterns. Use this skill whenever the user asks about detecting key presses, button presses, mouse position, gamepad input, touch gestures, or any player input handling in MonoGame — including cases where they say "how do I detect when the player presses X" or "my input isn't working".
---

# MonoGame Input Implementation Guide

This skill guides implementation of player input in MonoGame. For platform-specific details, see `references/input.md`.

## The Core Pattern: Previous + Current State

MonoGame input is purely polled — there are no events for keyboard or gamepad in the main game loop. To detect "just pressed" and "just released", store both the previous and current state as class fields.

```csharp
// Fields:
private KeyboardState _prevKeys;
private KeyboardState _currKeys;
private MouseState    _prevMouse;
private MouseState    _currMouse;
private GamePadState  _prevPad;
private GamePadState  _currPad;

// In Initialize():
_prevKeys  = Keyboard.GetState();
_currKeys  = _prevKeys;
_prevMouse = Mouse.GetState();
_currMouse = _prevMouse;
_prevPad   = GamePad.GetState(PlayerIndex.One);
_currPad   = _prevPad;

// At the TOP of Update(), before any input reads:
_prevKeys  = _currKeys;
_currKeys  = Keyboard.GetState();
_prevMouse = _currMouse;
_currMouse = Mouse.GetState();
_prevPad   = _currPad;
_currPad   = GamePad.GetState(PlayerIndex.One);
```

## Detecting Key Events

```csharp
// Held down this frame:
bool held     = _currKeys.IsKeyDown(Keys.Space);

// Just pressed (this frame, not last):
bool pressed  = _currKeys.IsKeyDown(Keys.Space) && !_prevKeys.IsKeyDown(Keys.Space);

// Just released:
bool released = !_currKeys.IsKeyDown(Keys.Space) && _prevKeys.IsKeyDown(Keys.Space);
```

Wrap these in helper methods to avoid repeating the pattern throughout the codebase:

```csharp
bool IsPressed(Keys key)  => _currKeys.IsKeyDown(key) && !_prevKeys.IsKeyDown(key);
bool IsReleased(Keys key) => !_currKeys.IsKeyDown(key) && _prevKeys.IsKeyDown(key);
bool IsHeld(Keys key)     => _currKeys.IsKeyDown(key);
```

## Mouse Input

```csharp
// Position (screen pixels):
Vector2 mousePos = new Vector2(_currMouse.X, _currMouse.Y); // store as field, not inline

// Left button events:
bool leftPressed  = _currMouse.LeftButton  == ButtonState.Pressed  && _prevMouse.LeftButton  == ButtonState.Released;
bool leftReleased = _currMouse.LeftButton  == ButtonState.Released && _prevMouse.LeftButton  == ButtonState.Pressed;
bool leftHeld     = _currMouse.LeftButton  == ButtonState.Pressed;

// Scroll wheel delta:
int scrollDelta = _currMouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;
```

To convert mouse position to world/virtual coordinates, apply the inverse of the camera matrix (see `monogame-graphics` skill).

## Gamepad Input

```csharp
// Check connection first:
if (!_currPad.IsConnected) return;

// Button pressed:
bool jumpPressed = _currPad.Buttons.A == ButtonState.Pressed && _prevPad.Buttons.A == ButtonState.Released;

// Thumbstick (Vector2, range -1 to 1):
Vector2 leftStick = _currPad.ThumbSticks.Left;   // (X: left/right, Y: up/down — Y is inverted on some platforms)

// Triggers (float, range 0 to 1):
float rightTrigger = _currPad.Triggers.Right;

// Vibration:
GamePad.SetVibration(PlayerIndex.One, leftMotor: 0.5f, rightMotor: 0.5f);
```

Thumbstick Y axis: positive = up in MonoGame (unlike screen coordinates). Apply deadzone by checking `Math.Abs(axis) > 0.15f` before using stick values.

## Touch Input (Mobile)

```csharp
// Must be enabled before GetState:
TouchPanel.EnabledGestures = GestureType.Tap | GestureType.FreeDrag;

// In Update():
TouchCollection touches = TouchPanel.GetState();
foreach (TouchLocation touch in touches)
{
    if (touch.State == TouchLocationState.Pressed)
    {
        Vector2 touchPos = touch.Position;
        // handle tap
    }
}

// Gesture reading (separate from raw touch):
while (TouchPanel.IsGestureAvailable)
{
    GestureSample gesture = TouchPanel.ReadGesture();
    if (gesture.GestureType == GestureType.Tap)
    {
        // gesture.Position = tap location
    }
}
```

Always check `TouchPanel.GetCapabilities().IsConnected` before using touch on non-touch platforms.

## Text Input

For text entry (chat, name entry), use the `TextInput` event — do not poll character keys manually:

```csharp
// In Initialize():
Window.TextInput += OnTextInput;

private void OnTextInput(object sender, TextInputEventArgs e)
{
    if (e.Character == '\b')  // backspace
        RemoveLastChar();
    else if (!char.IsControl(e.Character))
        _inputText += e.Character;
}
```

## Exiting the Game

Call `Exit()` in response to input (Escape key, gamepad Back button). Return immediately from `Update()` after calling it so no further game logic runs that frame. The `Exiting` event fires at the end of the same tick — use it for any final cleanup:

```csharp
// In Initialize():
Exiting += OnExiting;

// In Update():
bool shouldExit = _currKeys.IsKeyDown(Keys.Escape)
               || _currPad.Buttons.Back == ButtonState.Pressed;

if (shouldExit)
{
    Exit();
    base.Update(gameTime);
    return; // stop processing this frame
}

// Cleanup before the process ends:
private void OnExiting(object sender, ExitingEventArgs e)
{
    SaveSettings();
    // Do NOT call Exit() again here — it's already in progress
}
```

> **Platform note:** `Game.Exit()` behavior varies. On Android it finishes the Activity; on iOS it is a no-op (Apple guidelines forbid forced exit). Always test on device. For mobile, prefer showing a quit-confirmation dialog over calling `Exit()` directly.

## InputManager (Alca.MonoGame.Kernel)

The library wraps all raw polling into `InputManager`, accessible via `Core.Input`. It handles state updates automatically — no need to store previous/current state manually.

```csharp
// InputManager is updated by Core each frame — do not call Update() yourself.

// Keyboard — single-key helpers:
bool jumped  = Core.Input.IsKeyPressed(Keys.Space);   // just pressed this frame
bool running = Core.Input.IsKeyHeld(Keys.LeftShift);  // held
bool dropped = Core.Input.IsKeyReleased(Keys.S);      // just released

// Mouse:
Vector2 mousePos = Core.Input.MousePosition;
bool clicked = Core.Input.Mouse.IsLeftButtonPressed();

// Gamepad (index 0–3):
GamePadInfo pad = Core.Input.GamePads[0];
bool aPressed = pad.IsButtonPressed(Buttons.A);

// Load / unload binding maps at runtime:
Core.Input.LoadMap(_gameplayMap);
Core.Input.UnloadMap();
```

### InputActionMap — Configurable Bindings

`InputActionMap` lets you define named actions backed by configurable `InputBinding` objects. Use this for rebindable controls.

```csharp
// Define a map (e.g. in LoadContent):
var map = new InputActionMap("Gameplay");

var jumpAction = new InputAction("Jump");
jumpAction.AddBinding(new InputBinding(DeviceType.Keyboard, (int)Keys.Space));
jumpAction.AddBinding(new InputBinding(DeviceType.Gamepad,  (int)Buttons.A));
map.Add(jumpAction);

Core.Input.LoadMap(map);

// In Update():
if (map["Jump"].WasPressed)
    DoJump();
```

`InputSerializer` can save/load `InputActionMap` bindings to JSON — useful for a key-rebinding settings screen.

## Rules

- Always update previous state at the **top** of `Update()`, before reading input — never at the bottom.
- Never use `Thread.Sleep` or blocking waits for input polling.
- Store the `Vector2` mouse position as a field, not `new Vector2(...)` inline in `Update()`.
- On console platforms, check `GamePad.IsConnected` before reading state — disconnection mid-game is expected.
- Thumbstick Y is positive-up in MonoGame. Invert if needed: `moveDir.Y = -leftStick.Y`.
- After calling `Exit()`, always return from `Update()` immediately — remaining game logic in the same frame may access already-invalid state.

## Reference

For gesture types, touch capabilities, and multi-player gamepad patterns, see `references/input.md`.
