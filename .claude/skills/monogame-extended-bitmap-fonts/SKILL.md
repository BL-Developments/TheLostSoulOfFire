---
name: monogame-extended-bitmap-fonts
description: >
  MonoGame.Extended BitmapFont guide — rendering high-performance stylized text using
  pre-rasterized bitmap fonts (.fnt / AngelCode BMFont format) instead of SpriteFont.
  Use this skill whenever the user mentions bitmap font, BMFont, Hiero font, pixel art font,
  custom text effects in UI, BitmapFont extended, stylized text rendering, or wants to avoid
  SpriteFont limitations — even if they just say "I want a custom font in my UI" or
  "how do I render pixel art text".
---

# MonoGame.Extended BitmapFont Implementation Guide

This skill covers loading and rendering text using `BitmapFont` from MonoGame.Extended,
a drop-in replacement for `SpriteFont` that uses pre-rasterized bitmap fonts.
For complete API signatures and code samples read `references/extended-bitmap-fonts.md`.

## Why BitmapFont Over SpriteFont

| Concern | SpriteFont | BitmapFont |
|---------|-----------|-----------|
| Rasterization | At content build time (limited glyphs) | At artist tool time (full control) |
| Large character sets (CJK, etc.) | Slow build, large atlas | Efficient atlas packing |
| Visual effects (outline, shadow, glow) | Requires custom shader | Pre-baked into the texture |
| Runtime API | `SpriteFont` | Drop-in `BitmapFont` via extension method |
| Pixel art fonts | Blurring at non-native sizes | Native pixel-perfect support |

Use `BitmapFont` when you need visual effects baked into glyphs, large character sets,
or exact pixel art font rendering. Stick with `SpriteFont` for simple debug text or
when the content pipeline already provides the font.

## Workflow: Tool → Asset → Code

### Step 1: Generate the font files

Use **BMFont** (AngelCode, free) or **Hiero** (libGDX, free) to export the font.

Recommended BMFont export settings:
- **Bit Depth**: 32 bits
- **Textures**: PNG
- **File Format**: Binary (smaller files; XML or Text also work)
- **Channel preset**: "White text with alpha"

The export produces:
- `myfont.fnt` — glyph metadata (character positions, kerning)
- `myfont_0.png`, `myfont_1.png`, … — texture pages

For effects (outline, shadow, gradient): apply them in BMFont's rendering options or
in the source image before export. They are baked into the texture pages permanently.

### Step 2: Add to the content project

**Option A — Content Pipeline (recommended for shipping)**

In the MGCB Editor:
1. Add reference: `MonoGame.Extended.Content.Pipeline.dll`
2. Add the `.fnt` file as a content item (importer auto-detected)
3. Place the `.png` texture page(s) in the **same directory** as the `.fnt` file

Load at runtime:
```csharp
BitmapFont font = Content.Load<BitmapFont>("Fonts/myfont");
```

**Option B — Runtime loading (good for modding / hot-reload)**

Copy both `.fnt` and `.png` files to the output directory (set "Copy if newer" in project
properties). They **must** be in the same directory.

```csharp
BitmapFont font = BitmapFont.FromFile(GraphicsDevice, "Content/Fonts/myfont.fnt");
```

### Step 3: Render text

`MonoGame.Extended` adds a `DrawString` overload to `SpriteBatch` that accepts `BitmapFont`:

```csharp
_spriteBatch.Begin();
_spriteBatch.DrawString(font, "Hello World", new Vector2(50, 50), Color.White);
_spriteBatch.End();
```

The call signature mirrors the native `SpriteFont` overload, so switching is a
one-line change.

## Text Alignment and Positioning

`BitmapFont.MeasureString(string)` returns a `SizeF` (from `MonoGame.Extended`) with
`.Width` and `.Height` fields. Use it for centering:

```csharp
using MonoGame.Extended.BitmapFonts;  // BitmapFont

SizeF size = _font.MeasureString(text);
Vector2 centeredPos = new Vector2(
    screenWidth  / 2f - size.Width  / 2f,
    screenHeight / 2f - size.Height / 2f
);
_spriteBatch.DrawString(_font, text, centeredPos, Color.White);
```

Do not use `font.GetSize(string)` — that method does not exist in v6.

## Integration with monogame-ui-controls Label

To make a `Label` component accept either `SpriteFont` or `BitmapFont`:

```csharp
public class Label
{
    // One of these is set, the other null
    public SpriteFont  SpriteFont  { get; set; }
    public BitmapFont  BitmapFont  { get; set; }

    public string Text  { get; set; } = "";
    public Color  Color { get; set; } = Color.White;

    public void Draw(SpriteBatch sb, Vector2 position)
    {
        if (BitmapFont != null)
            sb.DrawString(BitmapFont, Text, position, Color);
        else if (SpriteFont != null)
            sb.DrawString(SpriteFont, Text, position, Color);
    }
}
```

Default to `SpriteFont` for debug labels; switch individual labels to `BitmapFont`
where visual quality or character-set support matters.

## Rules and Gotchas

- The `.fnt` file and all its texture pages **must be in the same directory** —
  this applies to both Content Pipeline and runtime loading.
- Set "Copy to Output Directory → Copy if Newer" on `.fnt` and `.png` files when
  using runtime loading; the Content Pipeline handles this automatically.
- `BitmapFont.FromFile` takes the path to the `.fnt` file and discovers the texture
  pages relative to it — do not pass the `.png` path.
- Character sets are fixed at export time. If you need to add characters later,
  re-export from the tool.
- For pixel-perfect rendering ensure `SamplerState.PointClamp` is active in the
  `SpriteBatch.Begin` call.

## Reference

For complete API signatures, MGCB configuration details, and code samples,
read `references/extended-bitmap-fonts.md`.
