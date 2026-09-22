# MonoGame.Extended BitmapFont Reference

API signatures and ready-to-paste C# code for loading and rendering BitmapFont.

## Table of Contents
1. [Namespaces](#namespaces)
2. [MGCB pipeline configuration](#mgcb-pipeline-configuration)
3. [Loading via Content Pipeline](#loading-via-content-pipeline)
4. [Loading at runtime (FromFile / FromStream)](#loading-at-runtime)
5. [Drawing text with SpriteBatch](#drawing-text-with-spritebatch)
6. [Text alignment and sizing](#text-alignment-and-sizing)
7. [Label component with optional BitmapFont](#label-component-with-optional-bitmapfont)
8. [Pixel-art font setup](#pixel-art-font-setup)

---

## Namespaces

```csharp
using MonoGame.Extended.BitmapFonts;   // BitmapFont class
// DrawString extension is automatically available after adding MonoGame.Extended NuGet
```

---

## MGCB pipeline configuration

In the **MGCB Editor** (MonoGame Content Builder):

1. **References tab** → Add reference → browse to
   `MonoGame.Extended.Content.Pipeline.dll`
   (found in the NuGet cache or your project's packages folder).

2. **Add content item** → select your `.fnt` file.
   The importer (`BitmapFont Importer - MonoGame.Extended`) and processor
   (`BitmapFont Processor - MonoGame.Extended`) are selected automatically.

3. Place the `.png` texture page(s) **in the same content directory** as the `.fnt` file.
   They do not need to be added as separate content items — the importer discovers them.

4. Build content project. Runtime load:
   ```csharp
   BitmapFont font = Content.Load<BitmapFont>("Fonts/myfont");
   // "Fonts/myfont" = path relative to Content root, without extension
   ```

---

## Loading via Content Pipeline

```csharp
private BitmapFont _font;

protected override void LoadContent()
{
    _font = Content.Load<BitmapFont>("Fonts/myfont");
}
```

Use `BlendState.AlphaBlend` when drawing — content pipeline premultiplies alpha.

---

## Loading at runtime

Both the `.fnt` and all `.png` texture pages must be in the **same directory** on disk
and set to "Copy to Output Directory → Copy if Newer" in the project file.

### FromFile

```csharp
BitmapFont font = BitmapFont.FromFile(GraphicsDevice, "Content/Fonts/myfont.fnt");
```

### FromStream

```csharp
using (Stream stream = TitleContainer.OpenStream("Content/Fonts/myfont.fnt"))
{
    BitmapFont font = BitmapFont.FromStream(
        GraphicsDevice,
        stream,
        "Content/Fonts/myfont.fnt"   // base path used to resolve texture pages
    );
}
```

Use `BlendState.NonPremultiplied` when drawing runtime-loaded fonts — PNG alpha is
not premultiplied.

---

## Drawing text with SpriteBatch

MonoGame.Extended adds an overload of `DrawString` that accepts `BitmapFont`:

```csharp
_spriteBatch.Begin();
_spriteBatch.DrawString(_font, "Score: 9999", new Vector2(20, 20), Color.White);
_spriteBatch.End();
```

Full overload with rotation, origin, scale:
```csharp
_spriteBatch.DrawString(
    _font,
    "Level Complete!",
    position: new Vector2(400, 300),
    color:    Color.Yellow,
    rotation: 0f,
    origin:   Vector2.Zero,
    scale:    1.5f,
    effects:  SpriteEffects.None,
    layerDepth: 0f
);
```

---

## Text alignment and sizing

`BitmapFont.MeasureString(string)` returns a `SizeF` (from `MonoGame.Extended`) with
`.Width` and `.Height` fields. This is the correct v6 API:

```csharp
SizeF size = _font.MeasureString("Hello");
float textW = size.Width;
float textH = size.Height;
```

Centered draw using measured size:
```csharp
SizeF size = _font.MeasureString(text);
Vector2 centeredPos = new Vector2(
    screenWidth  / 2f - size.Width  / 2f,
    screenHeight / 2f - size.Height / 2f
);
_spriteBatch.DrawString(_font, text, centeredPos, Color.White);
```

**Do not use** `font.GetSize(string)` — that method does not exist in v6.
For line height only, `_font.LineHeight` (int) is also available.

---

## Label component with optional BitmapFont

A flexible Label that accepts either `SpriteFont` (default) or `BitmapFont`:

```csharp
using MonoGame.Extended.Graphics;

public class Label
{
    public SpriteFont SpriteFont { get; set; }
    public BitmapFont BitmapFont { get; set; }
    public string     Text       { get; set; } = string.Empty;
    public Color      Color      { get; set; } = Color.White;
    public Vector2    Position   { get; set; }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (BitmapFont != null)
            spriteBatch.DrawString(BitmapFont, Text, Position, Color);
        else if (SpriteFont != null)
            spriteBatch.DrawString(SpriteFont, Text, Position, Color);
    }
}

// Usage — Content Pipeline font:
var titleLabel = new Label
{
    BitmapFont = Content.Load<BitmapFont>("Fonts/TitleFont"),
    Text       = "GAME OVER",
    Position   = new Vector2(200, 100),
    Color      = Color.Red
};

// Usage — debug label with SpriteFont:
var debugLabel = new Label
{
    SpriteFont = Content.Load<SpriteFont>("Fonts/Debug"),
    Text       = "FPS: 60",
    Position   = new Vector2(10, 10)
};
```

---

## Pixel-art font setup

For pixel art fonts rendered without filtering artifacts:

```csharp
_spriteBatch.Begin(
    SpriteSortMode.Deferred,
    BlendState.AlphaBlend,         // or NonPremultiplied for runtime-loaded
    SamplerState.PointClamp,       // nearest-neighbor — no bilinear blur
    null, null
);
_spriteBatch.DrawString(_font, "SCORE 100", position, Color.White);
_spriteBatch.End();
```

`SamplerState.PointClamp` is required any time a pixel font is drawn at its native
size. Omitting it causes sub-pixel blurring that destroys the crisp pixel look.
