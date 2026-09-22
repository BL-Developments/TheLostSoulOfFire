---
name: monogame-content-pipeline
description: MonoGame content pipeline implementation guide covering MGCB, ContentManager, asset loading patterns, custom processors and parameters, SpriteFont, IntermediateSerializer XML, Android texture compression, and asset lifecycle. Use this skill whenever the user asks about loading textures, sounds, fonts, effects, or any assets; MGCB configuration; content build errors; processor parameters (GenerateMipmaps, ResizeToPowerOfTwo, Scale, etc.); custom importers/processors; ContentManager usage; Android Asset Packs; or anything related to the content pipeline in MonoGame — even if they just say "how do I load X", "my content isn't loading", or "my processor isn't showing up".
---

# MonoGame Content Pipeline Implementation Guide

This skill guides asset loading and content pipeline usage in MonoGame. For custom processor code patterns, see `references/content-pipeline.md`.

## How the Pipeline Works

```
Source asset → Importer → Processor → Writer → .xnb file (compiled)
```

- The **.mgcb file** defines which assets to build, which importer/processor to use, and build settings.
- The **MGCB Editor** is a GUI tool to manage the `.mgcb` file — use it to add, configure, and remove assets.
- At runtime, `ContentManager.Load<T>()` reads the compiled `.xnb` files.

## Loading Assets

All asset loading belongs in `LoadContent()`. Never load assets in `Update()` or `Draw()`.

```csharp
// Path is relative to the Content/ root, without extension:
Texture2D   playerSprite = Content.Load<Texture2D>("Sprites/Player");
SoundEffect jumpSound    = Content.Load<SoundEffect>("Audio/Jump");
Song        bgMusic      = Content.Load<Song>("Audio/Theme");
SpriteFont  uiFont       = Content.Load<SpriteFont>("Fonts/UI");
Effect      vignetteEffect = Content.Load<Effect>("Shaders/Vignette");
```

Path rules:
- **Relative to Content/ folder** — do not include the Content/ prefix.
- **No file extension** — just the name as defined in the `.mgcb` file.
- **Case-sensitive on Linux/macOS** — use consistent casing in the `.mgcb` file.

## ContentManager Scoping

Use **separate `ContentManager` instances** per scene to make unloading clean:

```csharp
// Create in scene constructor or LoadContent:
_sceneContent = new ContentManager(Game.Services, "Content");

// Load scene assets:
_texture = _sceneContent.Load<Texture2D>("Level1/Tileset");

// Unload all assets for this scene only:
protected override void UnloadContent()
{
    _sceneContent.Unload();
    _sceneContent.Dispose();
}
```

`ContentManager.Unload()` disposes all assets loaded through that manager. Calling the root `Content.Unload()` disposes **every** asset including shared ones — avoid unless exiting.

## Asset Types and Built-in Processors

| C# type | Content type | Processor |
|---------|-------------|-----------|
| `Texture2D` | PNG, JPG, BMP, TGA | Texture - MonoGame |
| `SoundEffect` | WAV | Sound Effect - MonoGame |
| `Song` | MP3, OGG, WMA | Song - MonoGame |
| `SpriteFont` | .spritefont XML | Sprite Font Description - MonoGame |
| `Effect` | .fx (HLSL) | Effect - MonoGame |
| `Model` | FBX, OBJ | Model - MonoGame |

## SpriteFont

Define fonts using a `.spritefont` XML file added to the MGCB. Minimum definition:

```xml
<?xml version="1.0" encoding="utf-8"?>
<XnaContent>
  <Asset Type="Graphics:FontDescription">
    <FontName>Arial</FontName>
    <Size>16</Size>
    <Spacing>0</Spacing>
    <UseKerning>true</UseKerning>
    <Style>Regular</Style>
    <DefaultCharacter>*</DefaultCharacter>
    <CharacterRegions>
      <CharacterRegion>
        <Start>&#32;</Start>   <!-- space -->
        <End>&#126;</End>      <!-- tilde -->
      </CharacterRegion>
    </CharacterRegions>
  </Asset>
</XnaContent>
```

For non-Latin characters, add additional `CharacterRegion` entries covering the required Unicode ranges. Each language variant of the font should be a separate `.spritefont` file.

## Custom XML Data

For game data (level definitions, configuration, item databases), use `IntermediateSerializer`:

```csharp
// Define the data class:
public class LevelData
{
    public string Name;
    public int Width;
    public int Height;
    public List<TileData> Tiles;
}

// Add the .xml file to the MGCB with processor: "Xml Importer - MonoGame"
// Load at runtime:
LevelData level = Content.Load<LevelData>("Levels/Level01");
```

## Standard Processor Parameters

Standard processors accept parameters you can set in the MGCB Editor properties panel or in the `.mgcb` file with `/processorParam:Name=Value`. Key parameters:

| Processor | Parameter | Default | Notes |
|-----------|-----------|---------|-------|
| `TextureProcessor` | `TextureFormat` | `Color` | `Color`, `Compressed`, `DxtCompressed`, `PvrCompressed`, `AstcCompressed`, `Etc1Compressed`, `EtcCompressed` |
| `TextureProcessor` | `GenerateMipmaps` | `false` | Set `true` for 3D textures |
| `TextureProcessor` | `ResizeToPowerOfTwo` | `false` | Required for PVRTC |
| `TextureProcessor` | `ColorKeyEnabled` | `true` | Removes magenta pixels |
| `ModelProcessor` | `Scale` | `1.0` | Scale multiplier at build time |
| `ModelProcessor` | `GenerateTangentFrames` | `false` | Required for normal-map shaders |
| `ModelProcessor` | `SwapWindingOrder` | `false` | Fix inside-out models |
| `FontDescriptionProcessor` | `PremultiplyAlpha` | `true` | Leave `true` unless using custom blending |

Changing processor mid-project resets all params to defaults — note your custom values before switching.

## Custom Processor

When the built-in processors don't cover your needs, create a **separate class library project** (e.g., `MyGame.Content.Pipeline`):

1. Reference `MonoGame.Framework.Content.Pipeline` NuGet.
2. Inherit from `ContentProcessor<TInput, TOutput>` or a built-in processor.
3. Reference the compiled `.dll` from the MGCB Editor under **Content node → References**.

> **Important .NET version constraint:** MGCB extension libraries must target **.NET 8 or lower**. The MGCB tool cannot load `.NET 9+` assemblies. This is a common build-time error — check first if a processor fails to appear.

```csharp
[ContentProcessor(DisplayName = "My Tilemap Processor")]
public class TilemapProcessor : ContentProcessor<string, TilemapData>
{
    public override TilemapData Process(string input, ContentProcessorContext context)
    {
        // parse input, return TilemapData
    }
}
```

### Processor Parameters

Add configurable parameters via properties with `[DefaultValue]` / `[DisplayName]` / `[Description]` attributes — they appear in the MGCB Properties panel:

```csharp
[DefaultValue(1.0f)]
[DisplayName("Tile Scale")]
[Description("Scale applied to all tiles during processing.")]
public float TileScale { get; set; } = 1.0f;
```

### Tracking External File Dependencies

If your processor reads a file outside the content project (e.g., a config or text file), register it with `context.AddDependency()` so the pipeline rebuilds when it changes:

```csharp
string fullPath = Path.GetFullPath(externalFilePath);
context.AddDependency(fullPath);
string content = File.ReadAllText(fullPath, Encoding.UTF8);
```

### Extending the Font Processor (add characters from a text file)

For CJK or other large character sets, extend `FontDescriptionProcessor` instead of adding huge `CharacterRegion` ranges in the `.spritefont`:

```csharp
[ContentProcessor(DisplayName = "Font Processor - From Text File")]
internal class TextFileFontProcessor : FontDescriptionProcessor
{
    [DefaultValue("../messages.txt")]
    [DisplayName("Message File")]
    [Description("All characters in this file will be added to the font.")]
    public string MessageFile { get; set; } = @"../messages.txt";

    public override SpriteFontContent Process(FontDescription input, ContentProcessorContext context)
    {
        string fullPath = Path.GetFullPath(MessageFile);
        context.AddDependency(fullPath);                     // rebuild if file changes
        foreach (char c in File.ReadAllText(fullPath, Encoding.UTF8))
            input.Characters.Add(c);                         // duplicates ignored automatically
        return base.Process(input, context);
    }
}
```

In the MGCB Editor, select the `.spritefont` file and change its processor to `Font Processor - From Text File`. The path in `MessageFile` is relative to the `.mgcb` file, so use `../` to step up to the game project root.

## Rules

- All `Content.Load<T>()` calls go in `LoadContent()` — no exceptions.
- Use per-scene `ContentManager` instances so `Unload()` doesn't affect other scenes.
- Never load the same asset twice with different `ContentManager` instances — the second load creates a duplicate in memory.
- Add `.fx` shader files to the MGCB with the **Effect - MonoGame** processor to get the compiled `.mgfxo`.
- On Linux/macOS, asset paths are case-sensitive — keep naming consistent with the `.mgcb` file.
- MGCB extension libraries must target **.NET 8 or lower** — never `.NET 9+`.
- Always call `context.AddDependency()` for any external file your processor reads — otherwise the pipeline won't rebuild on changes.
- After changing a processor in MGCB, all parameter values reset to defaults.

## Reference

For custom importer/processor scaffolding, `IntermediateSerializer` patterns, and Android texture compression, see `references/content-pipeline.md`.
