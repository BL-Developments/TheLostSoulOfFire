# MonoGame Content Pipeline Reference

## Table of Contents
1. [ContentManager API](#contentmanager-api)
2. [Built-in asset types and load paths](#built-in-asset-types-and-load-paths)
3. [Standard processor parameters](#standard-processor-parameters)
4. [Multiple ContentManagers (per-scene scoping)](#multiple-contentmanagers-per-scene-scoping)
5. [SpriteFont .spritefont schema](#spritefont-spritefont-schema)
6. [Custom XML data (IntermediateSerializer)](#custom-xml-data-intermediateserializer)
7. [XnaContent XML elements](#xnacontent-xml-elements)
8. [Custom Processor scaffold](#custom-processor-scaffold)
9. [Custom processor parameters](#custom-processor-parameters)
10. [context.AddDependency pattern](#contextadddependency-pattern)
11. [Extending a built-in processor](#extending-a-built-in-processor)
12. [Extending the font processor](#extending-the-font-processor)
13. [Loading content in a Game Library](#loading-content-in-a-game-library)
14. [Android texture compression](#android-texture-compression)
15. [Android Asset Packs](#android-asset-packs)

---

## ContentManager API

```csharp
// Root manager — available via Game.Content:
Content.RootDirectory = "Content"; // default

// Load an asset (path relative to RootDirectory, no extension):
Texture2D tex   = Content.Load<Texture2D>("Sprites/Player");
SoundEffect sfx = Content.Load<SoundEffect>("Audio/Jump");
Song music      = Content.Load<Song>("Audio/Theme");
SpriteFont font = Content.Load<SpriteFont>("Fonts/UI");
Effect shader   = Content.Load<Effect>("Shaders/Vignette");
Model model     = Content.Load<Model>("Models/Character");

// Load typed custom XML data:
MyData data = Content.Load<MyData>("Data/LevelConfig");

// Unload ALL assets loaded through this manager:
Content.Unload();

// Create a secondary manager (for scene-scoped loading):
var sceneContent = new ContentManager(Services, "Content");
// ... load scene assets ...
sceneContent.Unload();
sceneContent.Dispose();
```

Path rules:
- Relative to the `Content/` folder (the MGCB output root)
- Case-sensitive on Linux and macOS
- No file extension
- Subdirectory separator: `/` on all platforms

---

## Built-in asset types and load paths

| C# type | Typical MGCB processor | Source formats |
|---------|------------------------|----------------|
| `Texture2D` | Texture - MonoGame | PNG, JPG, BMP, TGA, GIF |
| `SoundEffect` | Sound Effect - MonoGame | WAV |
| `Song` | Song - MonoGame | MP3, OGG, WMA, M4A |
| `SpriteFont` | Sprite Font Description - MonoGame | `.spritefont` XML |
| `Effect` | Effect - MonoGame | `.fx` (HLSL) |
| `Model` | Model - MonoGame | FBX, OBJ |
| Custom type | Xml Importer - MonoGame | `.xml` (IntermediateSerializer) |

---

## Standard processor parameters

Set in MGCB Editor Properties panel, or in `.mgcb` with `/processorParam:Name=Value`.

### TextureProcessor

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `TextureFormat` | enum | `Color` | `Color` (uncompressed RGBA), `Compressed` (platform best), `DxtCompressed`, `PvrCompressed`, `AstcCompressed`, `Etc1Compressed`, `EtcCompressed` |
| `GenerateMipmaps` | bool | `false` | Set `true` for 3D/world textures |
| `ResizeToPowerOfTwo` | bool | `false` | Required for PVRTC; recommended for all mobile |
| `MakeSquare` | bool | `false` | Force square — required for some PVRTC implementations |
| `ColorKeyEnabled` | bool | `true` | Replace `ColorKeyColor` pixels with transparent |
| `ColorKeyColor` | Color | `255,0,255,255` | Magenta by default |
| `PremultiplyAlpha` | bool | `true` | Leave `true` unless using custom alpha blending |

### ModelProcessor

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| `Scale` | float | `1.0` | Build-time scale multiplier |
| `SwapWindingOrder` | bool | `false` | Fix inside-out models |
| `GenerateTangentFrames` | bool | `false` | Required for normal-map shaders |
| `GenerateMipmaps` | bool | `false` | Apply mipmaps to embedded textures |
| `TextureFormat` | enum | `Compressed` | Same options as TextureProcessor |
| `XAxisRotation` / `YAxisRotation` / `ZAxisRotation` | float | `0` | Bake rotation into asset |

### FontDescriptionProcessor

| Parameter | Type | Default |
|-----------|------|---------|
| `PremultiplyAlpha` | bool | `true` |
| `TextureFormat` | enum | `Compressed` |

> Changing a processor in MGCB resets **all** parameter values to defaults. Note your custom values first.

---

## Multiple ContentManagers (per-scene scoping)

```csharp
// Global/shared assets — load in Game.LoadContent(), never unload mid-game:
Texture2D _cursor = Content.Load<Texture2D>("UI/Cursor");
SpriteFont _uiFont = Content.Load<SpriteFont>("Fonts/UI");

// Per-scene assets — each scene creates and owns a ContentManager:
public class GameScene
{
    private ContentManager _content;

    public void LoadContent(IServiceProvider services)
    {
        _content = new ContentManager(services, "Content");
        _playerTexture = _content.Load<Texture2D>("Sprites/Player");
        _tilesheet     = _content.Load<Texture2D>("Tilesets/Dungeon");
        _music         = _content.Load<Song>("Audio/DungeonTheme");
    }

    public void UnloadContent()
    {
        MediaPlayer.Stop();          // stop streaming before unload
        _content.Unload();
        _content.Dispose();
        _content = null;
    }
}
```

**Never load the same asset through two different `ContentManager` instances** — it creates a duplicate in memory.

---

## SpriteFont .spritefont schema

```xml
<?xml version="1.0" encoding="utf-8"?>
<XnaContent xmlns:Graphics="Microsoft.Xna.Framework.Content.Pipeline.Graphics">
  <Asset Type="Graphics:FontDescription">

    <!-- System font name (must be installed on the build machine) -->
    <FontName>Arial</FontName>

    <!-- Point size -->
    <Size>16</Size>

    <!-- Extra pixels between characters -->
    <Spacing>0</Spacing>

    <!-- Kerning pairs from the font -->
    <UseKerning>true</UseKerning>

    <!-- Regular, Bold, Italic, Bold Italic -->
    <Style>Regular</Style>

    <!-- Fallback character for unmapped glyphs -->
    <DefaultCharacter>*</DefaultCharacter>

    <!-- Unicode ranges to include -->
    <CharacterRegions>
      <!-- Basic Latin (space through tilde) -->
      <CharacterRegion>
        <Start>&#32;</Start>
        <End>&#126;</End>
      </CharacterRegion>
      <!-- Latin-1 Supplement (accented characters: é, ñ, ü, etc.) -->
      <CharacterRegion>
        <Start>&#160;</Start>
        <End>&#255;</End>
      </CharacterRegion>
    </CharacterRegions>

  </Asset>
</XnaContent>
```

Add this file to the MGCB project and set the processor to **Sprite Font Description - MonoGame**.

---

## Custom XML data (IntermediateSerializer)

### Step 1 — Define the data class (in a shared library or the game project)

```csharp
// Must be public, serializable fields/properties:
public class EnemyData
{
    public string Name;
    public int    Health;
    public float  Speed;
    public List<string> Drops = new();
}
```

### Step 2 — Generate the XML file (run once with IntermediateSerializer)

```csharp
// Console app using MonoGame.Framework.Content.Pipeline:
using var writer = XmlWriter.Create("EnemyConfig.xml", new XmlWriterSettings { Indent = true });
IntermediateSerializer.Serialize(writer, new EnemyData
{
    Name = "Goblin", Health = 30, Speed = 1.5f, Drops = { "Gold", "Key" }
}, null);
```

The resulting XML format is recognized by the Content Pipeline **Xml Importer - MonoGame** processor.

### Step 3 — Add to MGCB and load

```csharp
// In MGCB Editor: set Importer = "Xml Importer - MonoGame", Processor = "No Processing Required"
EnemyData enemy = Content.Load<EnemyData>("Data/EnemyConfig");
```

---

## XnaContent XML elements

Elements recognized by `XmlImporter` (used for any `.xml` content file in MGCB):

| Element | Parent | Description |
|---------|--------|-------------|
| `<XnaContent>` | — | Root tag |
| `<Asset Type="Namespace.ClassName">` | `<XnaContent>` | Declares the target C# type. Use `ClassName[]` for arrays. |
| `<Item>` | `<Asset>` | One object in an array. Child elements map to public fields/properties. |
| Individual field elements | `<Asset>` or `<Item>` | Each public field/property becomes a child element. |

`IntermediateSerializer` serialization rules:
- Public fields and properties only; protected/private/internal are skipped
- Get-only or set-only properties are skipped
- Properties before fields; both in declaration order
- Nested types → nested elements; base class members before derived

Single-object XML example:
```xml
<?xml version="1.0" encoding="utf-8"?>
<XnaContent>
  <Asset Type="MyGame.LevelData">
    <Name>Level 1</Name>
    <Width>20</Width>
    <Height>15</Height>
  </Asset>
</XnaContent>
```

Array XML example:
```xml
<?xml version="1.0" encoding="utf-8"?>
<XnaContent>
  <Asset Type="MyGame.EnemyData[]">
    <Item>
      <Name>Goblin</Name>
      <Health>30</Health>
    </Item>
    <Item>
      <Name>Troll</Name>
      <Health>120</Health>
    </Item>
  </Asset>
</XnaContent>
```

---

## Custom Processor scaffold

Create a **separate Class Library** project (e.g., `MyGame.Pipeline`):

```xml
<!-- MyGame.Pipeline.csproj -->
<ItemGroup>
  <PackageReference Include="MonoGame.Framework.Content.Pipeline" Version="3.8.*" />
</ItemGroup>
```

```csharp
using Microsoft.Xna.Framework.Content.Pipeline;

[ContentImporter(".tmx", DisplayName = "Tiled Map Importer", DefaultProcessor = "TiledMapProcessor")]
public class TiledMapImporter : ContentImporter<string>
{
    public override string Import(string filename, ContentImporterContext context)
    {
        return File.ReadAllText(filename);
    }
}

[ContentProcessor(DisplayName = "Tiled Map Processor")]
public class TiledMapProcessor : ContentProcessor<string, TilemapData>
{
    public override TilemapData Process(string input, ContentProcessorContext context)
    {
        // Parse input XML/JSON, return strongly-typed TilemapData
        return new TilemapData { /* ... */ };
    }
}

[ContentTypeWriter]
public class TilemapWriter : ContentTypeWriter<TilemapData>
{
    protected override void Write(ContentWriter output, TilemapData value)
    {
        output.Write(value.Width);
        output.Write(value.Height);
        // write all data
    }
    public override string GetRuntimeReader(TargetPlatform targetPlatform)
        => "MyGame.TilemapReader, MyGame"; // namespace.ClassName, AssemblyName
}
```

Reference the compiled `.dll` in the MGCB file under **References**.

> **MGCB .NET constraint:** Extension libraries must target `.NET 8` or lower. `.NET 9+` assemblies are not loadable by the MGCB tool — the processor will silently not appear.

---

## Custom processor parameters

Expose configurable parameters that appear in the MGCB Editor Properties panel:

```csharp
using System.ComponentModel;

[ContentProcessor(DisplayName = "My Custom Processor")]
public class MyProcessor : ContentProcessor<string, MyOutputData>
{
    // Supported parameter types: bool, byte, char, decimal, double, float,
    // int, string, enum, Vector2/3/4, Color — others are ignored.

    [DefaultValue(1.0f)]
    [DisplayName("Scale Factor")]
    [Description("Multiplied against all sizes at build time.")]
    public float ScaleFactor { get; set; } = 1.0f;

    [DefaultValue(false)]
    [DisplayName("Flip Horizontal")]
    public bool FlipHorizontal { get; set; } = false;

    public override MyOutputData Process(string input, ContentProcessorContext context)
    {
        // use ScaleFactor and FlipHorizontal here
        return new MyOutputData();
    }
}
```

### Passing parameters when chaining processors

Use `OpaqueDataDictionary` to pass params to another processor:

```csharp
var parameters = new OpaqueDataDictionary
{
    { "ColorKeyColor",     Color.Magenta },
    { "ColorKeyEnabled",   true },
    { "ResizeToPowerOfTwo", true }
};

context.BuildAsset<TextureContent, TextureContent>(
    texture,
    typeof(TextureProcessor).Name,
    parameters,
    null,   // processorName override
    null);  // assetName
```

For in-memory objects use `context.Convert<TInput, TOutput>()` instead of `BuildAsset`.

---

## context.AddDependency pattern

Register external files your processor reads so the pipeline rebuilds when they change:

```csharp
public override MyData Process(string input, ContentProcessorContext context)
{
    // Always use GetFullPath so the path is absolute for the dependency tracker:
    string configPath = Path.GetFullPath(Path.Combine(
        Path.GetDirectoryName(context.OutputFilename), "..", "config.json"));

    context.AddDependency(configPath);    // pipeline rebuild triggered if this file changes

    string json = File.ReadAllText(configPath, Encoding.UTF8);
    // ... parse and return
}
```

---

## Extending a built-in processor

```csharp
[ContentProcessor(DisplayName = "Model Processor - With Tangents")]
public class ModelWithTangentsProcessor : ModelProcessor
{
    // Override to add tangent/binormal data:
    protected override void ProcessGeometry(MeshContent mesh, ContentProcessorContext context)
    {
        MeshHelper.CalculateTangentFrames(mesh,
            VertexChannelNames.TextureCoordinate(0),
            VertexChannelNames.Tangent(0),
            VertexChannelNames.Binormal(0));
        base.ProcessGeometry(mesh, context);
    }
}
```

---

## Extending the font processor

For large character sets (CJK, Arabic, etc.) avoid adding huge `CharacterRegion` ranges in `.spritefont`. Instead, extend `FontDescriptionProcessor` to add only the characters actually used:

```csharp
using System.ComponentModel;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

[ContentProcessor(DisplayName = "Font Processor - From Text File")]
internal class TextFileFontProcessor : FontDescriptionProcessor
{
    [DefaultValue(@"../messages.txt")]
    [DisplayName("Message File")]
    [Description("Characters in this file are added to the font. Path is relative to the Content folder.")]
    public string MessageFile { get; set; } = @"../messages.txt";

    public override SpriteFontContent Process(FontDescription input, ContentProcessorContext context)
    {
        string fullPath = Path.GetFullPath(MessageFile);

        context.AddDependency(fullPath);   // rebuild font when messages.txt changes

        // FontDescription.Characters is a HashSet — duplicates are ignored automatically
        foreach (char c in File.ReadAllText(fullPath, Encoding.UTF8))
            input.Characters.Add(c);

        return base.Process(input, context);
    }
}
```

Steps to activate:
1. Build the pipeline extension project.
2. In MGCB Editor → Content node → References → add the extension `.dll`.
3. Select the `.spritefont` file → change its Processor to `Font Processor - From Text File`.
4. Set `MessageFile` to the path of your text file (relative to the `.mgcb` file, so `../` = game project root).

---

## Loading content in a Game Library

```csharp
// Option A — Load .xnb files from disk (same Content pipeline):
public class MyComponent : DrawableGameComponent
{
    private ContentManager _localContent;

    protected override void LoadContent()
    {
        _localContent = new ContentManager(Game.Services, "Content");
        _texture = _localContent.Load<Texture2D>("MyLib/Texture");
    }
}

// Option B — Embed assets as resources and use ResourceContentManager:
// Add files to the class library with Build Action = Embedded Resource
// Then:
var resourceContent = new ResourceContentManager(services, MyLibResources.ResourceManager);
_texture = resourceContent.Load<Texture2D>("EmbeddedTextureName");
```

---

## Android texture compression

Set `TextureFormat` per-texture in MGCB Editor, or in the `.mgcb` file:

```sh
/importer:TextureImporter
/processor:TextureProcessor
/processorParam:TextureFormat=EtcCompressed
/build:Textures/Logo.png
```

For `.aab` distribution, Android selects textures by directory suffix. Use the `#tcf_` prefix format in the **output path** (after the `;`):

| `TextureProcessorOutputFormat` | Directory suffix | Supported hardware |
|-------------------------------|-----------------|-------------------|
| `EtcCompressed` | `#tcf_etc2` | All Android 4.3+ (safe default) |
| `DxtCompressed` | `#tcf_s3tc` | Nvidia Tegra |
| `AtcCompressed` | `#tcf_atc` | Qualcomm Adreno |
| `PvrCompressed` | `#tcf_pvrtc` | PowerVR (older) — must be power-of-2 AND square |
| `AstcCompressed` | `#tcf_astc` | Modern GPU, best quality |
| `Compressed` or `Color` | *(no suffix)* | Fallback / uncompressed |

Multi-format `.mgcb` example (build the same source into multiple output directories):

```sh
# ETC2 (default Android fallback)
/importer:TextureImporter
/processor:TextureProcessor
/processorParam:TextureFormat=EtcCompressed
/build:Textures/Logo.png;Textures#tcf_etc2/Logo

# ASTC (modern devices)
/importer:TextureImporter
/processor:TextureProcessor
/processorParam:TextureFormat=AstcCompressed
/build:Textures/Logo.png;Textures#tcf_astc/Logo

# S3TC/DXT (Nvidia Tegra)
/importer:TextureImporter
/processor:TextureProcessor
/processorParam:TextureFormat=DxtCompressed
/build:Textures/Logo.png;Textures#tcf_s3tc/Logo
```

> PVRTC requires textures to be power-of-2 **and** square (e.g. 512×512). Set `ResizeToPowerOfTwo=True` and `MakeSquare=True` in the processor params.

---

## Android Asset Packs

For large games exceeding Play Store size limits, use Android Asset Packs (`.aab`). Add an MSBuild target to the Android `.csproj`:

```xml
<!-- In the Android .csproj — moves selected content into an Asset Pack -->
<Target Name="_MoveContentIntoPacks" AfterTargets="IncludeContent">
  <ItemGroup>
    <!-- Move all music into a named pack (InstallTime by default) -->
    <AndroidAsset Update="Content/Music/**/*.*" AssetPack="MyGameAssets" />
    <!-- Move large textures into a separate pack -->
    <AndroidAsset Update="Content/Textures/HighRes/**/*.*" AssetPack="MyGameHD" />
  </ItemGroup>
</Target>
```

- `InstallTime` packs (default) are installed with the game — no extra code needed.
- Assets remain accessible via `Content.Load<T>()` as usual.
- For `FastFollow` or `OnDemand` packs (downloaded after install), use the Play Asset Delivery API — out of scope for standard MonoGame usage.
