---
name: monogame-ui-colorpicker
description: MonoGame UI Color Picker — RGB and HSV color selectors using three sliders or a hue wheel with saturation/value square, plus a preview swatch. Use this skill whenever the user asks about a color picker, color selector, RGB slider, HSV picker, "how do I let the user pick a color" in MonoGame UI, or any color selection control.
---

# MonoGame UI Color Picker

A color picker lets the user select a `Color` value. Two common forms:

1. **RGB Sliders** — three `Slider` instances (R, G, B) from 0–255 plus an optional Alpha slider. Simple to implement, easy to read.
2. **HSV Picker** — a hue bar (or wheel) + saturation/value square. More ergonomic for artists but requires HSV↔RGB conversion math.

For complete code templates, read `references/ui-colorpicker.md`.

## Assumed Contract

`ColorPickerRGB` is a `UIContainer` that composes three `Slider` instances from `monogame-ui-slider`. Constructor: `ColorPickerRGB(SpriteFont? font, Texture2D? pixel)`. It uses `monogame-ui-layout`'s `StackPanel` internally. The preview swatch is rendered by tinting a 1×1 pixel texture.

## RGB Slider Picker

The simplest form — compose three pre-built sliders:

```
┌──────────────────────┐
│ R ────●──────────    │  slider 0–255
│ G ──────●────────    │
│ B ──────────●────    │
│ A ─────────────●     │  optional
│ ┌────┐                │
│ │    │  #FF8040       │  preview swatch + hex label
│ └────┘                │
└──────────────────────┘
```

Each slider's `ValueChanged` callback updates the combined `SelectedColor` and fires `ColorChanged`.

**Hex input:** Add a `TextBox` that accepts 6-character hex strings (`RRGGBB`). On commit (Enter or focus-lost), parse with `Convert.ToInt32(hex, 16)` and push values to the three sliders.

## HSV Picker

More complex. Two rendering components:

### Hue Bar
A 1D gradient strip going from 0° to 360° hue (S=1, V=1). Render by drawing `n` thin vertical rectangles (or using a precomputed `Texture2D`). On click/drag, read `hue = (mouseX - barX) / barWidth * 360`.

**Precomputed texture (recommended):** In `LoadContent()`, create a `Texture2D(gd, width, 1)`, fill pixel colors with HSV→RGB for each column, call `SetData`. This is done once — not every frame.

### Saturation/Value Square
A 2D gradient: X axis = saturation (0→1), Y axis = value (1→0). Render similarly with a precomputed `Texture2D`. Size: typically 200×200 px. The crosshair cursor is drawn as two 1-px lines (no GC).

### HSV ↔ RGB Conversion

```csharp
// HSV to RGB (H: 0–360, S: 0–1, V: 0–1) → Color
static Color HsvToRgb(float h, float s, float v)
{
    if (s == 0f) { byte g = (byte)(v * 255); return new Color(g, g, g); }
    h /= 60f;
    int i = (int)h;
    float f = h - i;
    float p = v * (1f - s);
    float q = v * (1f - s * f);
    float t = v * (1f - s * (1f - f));
    float r, g, b;
    switch (i % 6)
    {
        case 0: r=v; g=t; b=p; break; case 1: r=q; g=v; b=p; break;
        case 2: r=p; g=v; b=t; break; case 3: r=p; g=q; b=v; break;
        case 4: r=t; g=p; b=v; break; default: r=v; g=p; b=q; break;
    }
    return new Color((byte)(r*255), (byte)(g*255), (byte)(b*255));
}

// RGB to HSV
static void RgbToHsv(Color c, out float h, out float s, out float v)
{
    float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f;
    float max = MathF.Max(r, MathF.Max(g, b));
    float min = MathF.Min(r, MathF.Min(g, b));
    float delta = max - min;
    v = max;
    s = (max == 0f) ? 0f : delta / max;
    if (delta == 0f) { h = 0f; return; }
    if (max == r)      h = 60f * ((g - b) / delta % 6);
    else if (max == g) h = 60f * ((b - r) / delta + 2);
    else               h = 60f * ((r - g) / delta + 4);
    if (h < 0) h += 360f;
}
```

These are pure math — no allocations, call freely.

## Preview Swatch

A `Rectangle` filled with a 1×1 white pixel texture tinted with `SelectedColor`:

```csharp
spriteBatch.Draw(_pixel, _swatchBounds, SelectedColor);
```

Update `SelectedColor` whenever any slider changes. Do not create `new Color(...)` inside `Draw()` — store it as a field.

## Events

- `event Action<Color>? ColorChanged` — fires when any slider changes; receives the new color
- `event Action<Color>? ColorCommitted` — fires on focus-lost or a dedicated "OK" button

## Anti-Patterns

- Never generate the hue/saturation gradient texture inside `Draw()` — generate it once in `LoadContent()` / constructor.
- Never store `Color` as four separate float fields and reconstruct with `new Color(...)` every `Draw()` — store the `Color` struct directly and update it only when the value changes.
- For the RGB picker, do not re-create `Slider` instances when the color changes externally — call `slider.Value = r/g/b` to push values in.

## Reference

Complete `ColorPickerRGB`, HSV picker components, and conversion utilities: `references/ui-colorpicker.md`.
