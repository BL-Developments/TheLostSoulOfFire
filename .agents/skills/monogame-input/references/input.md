# MonoGame Input Reference

## Table of Contents
1. [Keyboard](#keyboard)
2. [Mouse](#mouse)
3. [GamePad](#gamepad)
4. [Touch (mobile)](#touch-mobile)
5. [Gestures](#gestures)
6. [Text input](#text-input)
7. [Keys enum — common values](#keys-enum--common-values)

---

## Keyboard

```csharp
// Fields:
private KeyboardState _prevKeys;
private KeyboardState _currKeys;

// Initialize() — seed both so frame 0 has valid data:
_prevKeys = _currKeys = Keyboard.GetState();

// TOP of Update():
_prevKeys = _currKeys;
_currKeys = Keyboard.GetState();

// Query helpers:
bool IsHeld(Keys k)     => _currKeys.IsKeyDown(k);
bool IsPressed(Keys k)  => _currKeys.IsKeyDown(k)  && _prevKeys.IsKeyUp(k);
bool IsReleased(Keys k) => _currKeys.IsKeyUp(k)    && _prevKeys.IsKeyDown(k);

// Get all currently pressed keys:
Keys[] held = _currKeys.GetPressedKeys();
```

---

## Mouse

```csharp
// Fields:
private MouseState _prevMouse;
private MouseState _currMouse;

// TOP of Update():
_prevMouse = _currMouse;
_currMouse = Mouse.GetState();

// Position (screen pixels):
int mx = _currMouse.X;
int my = _currMouse.Y;

// Buttons:
bool leftPressed   = _currMouse.LeftButton   == ButtonState.Pressed  && _prevMouse.LeftButton   == ButtonState.Released;
bool leftReleased  = _currMouse.LeftButton   == ButtonState.Released && _prevMouse.LeftButton   == ButtonState.Pressed;
bool leftHeld      = _currMouse.LeftButton   == ButtonState.Pressed;
bool rightHeld     = _currMouse.RightButton  == ButtonState.Pressed;
bool middleHeld    = _currMouse.MiddleButton == ButtonState.Pressed;

// Scroll wheel (cumulative value — use delta):
int scrollDelta = _currMouse.ScrollWheelValue - _prevMouse.ScrollWheelValue;

// Move cursor programmatically:
Mouse.SetPosition(screenX, screenY);

// Show/hide cursor:
IsMouseVisible = true;
```

### Screen → world coordinates

```csharp
// Cache the inverted camera matrix as a field (not per-call):
_inverseCamera = Matrix.Invert(_cameraMatrix);

Vector2 WorldMousePos()
{
    var screen = new Vector2(_currMouse.X, _currMouse.Y);
    return Vector2.Transform(screen, _inverseCamera);
}
```

---

## GamePad

```csharp
// Fields:
private GamePadState _prevPad;
private GamePadState _currPad;

// TOP of Update():
_prevPad = _currPad;
_currPad = GamePad.GetState(PlayerIndex.One);

// Always check connection:
if (!_currPad.IsConnected) return;

// Button events (same pattern as keyboard):
bool APressed  = _currPad.Buttons.A == ButtonState.Pressed  && _prevPad.Buttons.A == ButtonState.Released;
bool AReleased = _currPad.Buttons.A == ButtonState.Released && _prevPad.Buttons.A == ButtonState.Pressed;
bool AHeld     = _currPad.Buttons.A == ButtonState.Pressed;

// All Buttons properties:
// A, B, X, Y, Start, Back, BigButton
// LeftShoulder, RightShoulder, LeftStick, RightStick
// DPadUp, DPadDown, DPadLeft, DPadRight

// DPad as buttons:
bool dpadLeft = _currPad.DPad.Left == ButtonState.Pressed;

// Thumbsticks (Vector2, range -1 to 1):
Vector2 leftStick  = _currPad.ThumbSticks.Left;   // Y positive = UP (MonoGame convention)
Vector2 rightStick = _currPad.ThumbSticks.Right;

// Apply deadzone (common threshold: 0.15):
if (MathF.Abs(leftStick.X) < 0.15f) leftStick.X = 0f;
if (MathF.Abs(leftStick.Y) < 0.15f) leftStick.Y = 0f;

// Triggers (float, range 0 to 1):
float leftTrigger  = _currPad.Triggers.Left;
float rightTrigger = _currPad.Triggers.Right;

// Rumble vibration:
GamePad.SetVibration(PlayerIndex.One, leftMotor: 0.5f, rightMotor: 0.3f);
// Stop vibration:
GamePad.SetVibration(PlayerIndex.One, 0f, 0f);

// Query supported features:
GamePadCapabilities caps = GamePad.GetCapabilities(PlayerIndex.One);
bool hasVibration = caps.HasLeftVibrationMotor;
```

### Multi-player

```csharp
for (int i = 0; i < 4; i++)
{
    GamePadState state = GamePad.GetState((PlayerIndex)i);
    if (state.IsConnected) { /* handle player i */ }
}
```

---

## Touch (mobile)

```csharp
// Check hardware availability:
TouchPanelCapabilities caps = TouchPanel.GetCapabilities();
if (!caps.IsConnected) return;

// Read all active touches each frame:
TouchCollection touches = TouchPanel.GetState();

foreach (TouchLocation touch in touches)
{
    Vector2 pos = touch.Position; // screen pixels

    switch (touch.State)
    {
        case TouchLocationState.Pressed:  /* finger down */  break;
        case TouchLocationState.Moved:    /* finger moved */ break;
        case TouchLocationState.Released: /* finger up */    break;
        case TouchLocationState.Invalid:  /* ignore */       break;
    }

    // Get previous position for velocity calculation:
    if (touch.TryGetPreviousLocation(out TouchLocation prev))
    {
        Vector2 delta = touch.Position - prev.Position;
    }
}

// Max simultaneous touches:
int maxTouches = caps.MaximumTouchCount;
```

---

## Gestures

```csharp
// Enable desired gestures BEFORE reading them (usually in Initialize):
TouchPanel.EnabledGestures = GestureType.Tap
                           | GestureType.DoubleTap
                           | GestureType.FreeDrag
                           | GestureType.Flick
                           | GestureType.Pinch;

// Read gestures each frame (drain the queue):
while (TouchPanel.IsGestureAvailable)
{
    GestureSample gesture = TouchPanel.ReadGesture();

    switch (gesture.GestureType)
    {
        case GestureType.Tap:
            Vector2 tapPos = gesture.Position;
            break;

        case GestureType.FreeDrag:
            Vector2 dragDelta = gesture.Delta;  // movement since last sample
            break;

        case GestureType.Flick:
            Vector2 velocity = gesture.Delta;   // pixels/second at release
            break;

        case GestureType.Pinch:
            Vector2 finger1 = gesture.Position;
            Vector2 finger2 = gesture.Position2;
            break;
    }
}
```

**Gesture ordering**: `DoubleTap` is always preceded by a `Tap`. `PinchComplete` follows `Pinch`. Design logic to handle intermediate gestures gracefully.

---

## Text input

```csharp
// Wire in Initialize() — fires once per character entered:
Window.TextInput += OnTextInput;

private string _inputText = "";

private void OnTextInput(object sender, TextInputEventArgs e)
{
    char c = e.Character;

    if (c == '\b' && _inputText.Length > 0)       // Backspace
        _inputText = _inputText[..^1];
    else if (c == '\r' || c == '\n')               // Enter
        SubmitText(_inputText);
    else if (!char.IsControl(c))                   // Printable character
        _inputText += c;
}

// Limit length:
else if (!char.IsControl(c) && _inputText.Length < MaxLength)
    _inputText += c;
```

---

## Game exit

```csharp
// In Initialize() — wire the exit event for cleanup:
Exiting += OnExiting;

// In Update() — check both keyboard and gamepad:
bool exitRequested =
    (_currKeys.IsKeyDown(Keys.Escape) && _prevKeys.IsKeyUp(Keys.Escape))
    || (_currPad.Buttons.Back == ButtonState.Pressed && _prevPad.Buttons.Back == ButtonState.Released);

if (exitRequested)
{
    Exit();           // queues exit — fires Exiting at end of this tick
    base.Update(gameTime);
    return;           // stop further logic this frame
}

// Cleanup handler — runs before the process ends:
private void OnExiting(object sender, ExitingEventArgs e)
{
    SaveSettings();
    StopBackgroundTasks();
    // Do NOT call Exit() here again
}
```

Platform behavior:
- **Desktop (Windows/Linux/macOS)** — `Exit()` terminates the process normally.
- **Android** — finishes the Activity; OS may still keep the process alive briefly.
- **iOS** — `Exit()` is a no-op (Apple rejects apps that self-terminate). Show a quit dialog instead.
- **Console** — returns to the dashboard.

---

## Keys enum — common values

```csharp
Keys.A - Keys.Z          // Letters
Keys.D0 - Keys.D9        // Number row (D = digit)
Keys.NumPad0 - Keys.NumPad9
Keys.F1  - Keys.F24
Keys.Left, Right, Up, Down
Keys.Space, Keys.Enter, Keys.Escape, Keys.Tab
Keys.LeftShift, Keys.RightShift
Keys.LeftControl, Keys.RightControl
Keys.LeftAlt, Keys.RightAlt
Keys.Back                // Backspace
Keys.Delete, Keys.Insert, Keys.Home, Keys.End
Keys.PageUp, Keys.PageDown
```
