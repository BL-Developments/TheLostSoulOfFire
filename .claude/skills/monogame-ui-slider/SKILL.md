---
name: monogame-ui-slider
description: MonoGame UI Slider / TrackBar control — horizontal or vertical range selector with float value, optional step snapping, drag with mouse, keyboard increment/decrement, and visual thumb + track rendering. Use this skill whenever the user asks about a slider, trackbar, range selector, volume control, brightness control, "how do I let the user pick a value between min and max", or any draggable value control in MonoGame UI.
---

# MonoGame UI Slider

A slider lets the user pick a float value within a [MinValue, MaxValue] range by dragging a thumb along a track. Supports horizontal and vertical orientation, optional step snapping, keyboard navigation, and gamepad control.

For complete code templates, read `references/ui-slider.md`.

## Assumed Contract

Extends `UIElement` from `monogame-ui-core`. Receives pointer events from `monogame-ui-interaction` (`OnPointerDown`, `OnPointerUp`, pointer position). Participates in focus via `monogame-ui-focus` for keyboard/gamepad increment.

## Constructor

```csharp
var slider = new Slider(pixel);          // pixel = 1×1 white Texture2D
slider.MinValue = 0f;
slider.MaxValue = 100f;
slider.Step     = 1f;                    // 0 = continuous float
slider.Orientation = Orientation.Horizontal;
```

## Anatomy

```
Track (background rectangle)
 └─ Fill (colored portion from Min to current value)
 └─ Thumb (draggable handle, centered on the current value position)
```

**Value → pixel position:**
```csharp
float normalized = (Value - MinValue) / (MaxValue - MinValue);  // 0.0–1.0
int   thumbX     = trackRect.X + (int)(normalized * trackRect.Width);
```

**Pixel position → value (on drag):**
```csharp
float normalized = MathHelper.Clamp((mouseX - trackRect.X) / (float)trackRect.Width, 0f, 1f);
float rawValue   = MinValue + normalized * (MaxValue - MinValue);
Value = Step > 0 ? MathF.Round(rawValue / Step) * Step : rawValue;
```

## Drag State

Track drag state with two bool fields — never allocate state objects:
- `_isDragging` — set to true in `OnPointerDown` when the thumb or track is hit
- Cleared in `OnPointerUp`

During drag, re-read mouse position every `Update()` frame and recalculate `Value`. Do not wait for a click event — drag updates happen continuously.

## Step Snapping

`Step = 0` → continuous float (no snapping).  
`Step > 0` → value snaps to nearest multiple of `Step`:

```csharp
// Integer slider (0, 1, 2, ... 10):
slider.MinValue = 0; slider.MaxValue = 10; slider.Step = 1;

// Float slider in 0.25 increments:
slider.MinValue = 0; slider.MaxValue = 1; slider.Step = 0.25f;
```

## Keyboard / Gamepad

When focused:
- **Left / Right (or Down / Up for vertical)** — decrement/increment by `Step` (or 1% of range if `Step == 0`)
- **Home / End** — jump to `Min` / `Max`

Synthesize these in `HandleKeyboardInput` and route gamepad left-stick X to the same logic.

## Thumb Hit Area

Extend the thumb's hit area to at least 20×20 px for accessibility even if the visual thumb is smaller. Store the hit `Rectangle` separately from the visual rectangle.

## Events

- `event Action<float>? ValueChanged` — fires whenever `Value` changes (drag or keyboard); receives the new value

## Anti-Patterns

- Do not fire `ValueChanged` if the value did not actually change (compare to previous before invoking).
- Do not use float equality (`value == prev`) to check — use `MathF.Abs(value - prev) > 0.0001f`.

## Reference

Complete `Slider` template: `references/ui-slider.md`.
