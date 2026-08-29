# MonoGame.Extended Tilemaps Reference

API signatures and ready-to-paste C# code for the v6 unified tilemap system.

## Table of Contents
1. [Namespaces](#namespaces)
2. [Full game class: fields, Initialize, LoadContent, Update, Draw](#full-game-class)
3. [Drawing with entity interleaving](#drawing-with-entity-interleaving)
4. [TilemapRenderer (GPU-buffer alternative)](#tilemaprenderer-gpu-buffer-alternative)
5. [Object layer: spawning GameEntity instances](#object-layer-spawning-gameentity-instances)
6. [Tile layer: reading and modifying tiles](#tile-layer-reading-and-modifying-tiles)
7. [Custom properties](#custom-properties)
8. [Runtime parser (no content pipeline)](#runtime-parser-no-content-pipeline)

---

## Namespaces

```csharp
using MonoGame.Extended;                      // OrthographicCamera
using MonoGame.Extended.ViewportAdapters;     // BoxingViewportAdapter
using MonoGame.Extended.Tilemaps;             // Tilemap, TilemapTileLayer, TilemapObjectLayer…
using MonoGame.Extended.Tilemaps.Rendering;   // TilemapSpriteBatchRenderer
```

---

## Full game class

Minimal working example with `TilemapSpriteBatchRenderer` and `OrthographicCamera`.

```csharp
public class Game1 : Game
{
    private GraphicsDeviceManager    _graphics;
    private SpriteBatch              _spriteBatch;
    private Tilemap                  _tilemap;
    private TilemapSpriteBatchRenderer _renderer;
    private OrthographicCamera       _camera;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
        // BoxingViewportAdapter letterboxes/pillarboxes to your virtual resolution
        var viewport = new BoxingViewportAdapter(Window, GraphicsDevice, 320, 180);
        _camera = new OrthographicCamera(viewport);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _tilemap = Content.Load<Tilemap>("maps/level1");

        _renderer = new TilemapSpriteBatchRenderer();
        _renderer.BlendState = BlendState.AlphaBlend;  // content pipeline premultiplies alpha
        _renderer.LoadTilemap(_tilemap);
    }

    protected override void Update(GameTime gameTime)
    {
        _renderer.Update(gameTime);   // advance tile animations
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(_tilemap.BackgroundColor ?? Color.Black);
        _renderer.Draw(_spriteBatch, _camera);
        base.Draw(gameTime);
    }
}
```

---

## Drawing with entity interleaving

Insert entity rendering between named tile layers:

```csharp
protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(Color.Black);

    // Background tile layers
    _renderer.DrawLayers(_spriteBatch, _camera, "Background", "Ground");

    // Game entities rendered in world space
    _spriteBatch.Begin(transformMatrix: _camera.GetViewMatrix());
    foreach (var entity in _world.Entities)
        entity.Draw(_spriteBatch);
    _spriteBatch.End();

    // Foreground tile layer on top of entities
    _renderer.DrawLayer(_spriteBatch, _camera, "Foreground");
}
```

`DrawLayers` accepts any number of layer name strings.
`DrawLayer` draws a single named layer.
Both methods manage their own `SpriteBatch.Begin/End` internally.

---

## TilemapRenderer (GPU-buffer alternative)

Use when most tiles are always visible and draw call count is a bottleneck.
Must be disposed. Uses `GraphicsDevice` directly.

```csharp
private TilemapRenderer _tilemapRenderer;

protected override void LoadContent()
{
    _tilemap = Content.Load<Tilemap>("maps/level1");

    _tilemapRenderer = new TilemapRenderer(GraphicsDevice);
    _tilemapRenderer.BlendState = BlendState.AlphaBlend;
    _tilemapRenderer.LoadTilemap(_tilemap);

    // Optional: merge multiple layers into a single draw call
    _tilemapRenderer.DefineLayerGroup("Background", "Sky", "Clouds", "Mountains");
    _tilemapRenderer.DefineLayerGroup("Foreground", "Trees", "Overlay");
}

protected override void UnloadContent()
{
    _tilemapRenderer?.Dispose();
    base.UnloadContent();
}

protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(Color.Black);
    _tilemapRenderer.Draw(_camera);   // draws all layers
}

// Entity interleaving with TilemapRenderer
protected override void Draw(GameTime gameTime)
{
    GraphicsDevice.Clear(Color.Black);
    _tilemapRenderer.BeginDraw(_camera);

    _tilemapRenderer.DrawLayerGroup("Background");

    // SpriteBatch work between renderer calls — save/restore device state
    _tilemapRenderer.SaveGraphicsDeviceState();
    _spriteBatch.Begin(transformMatrix: _camera.GetViewMatrix());
    foreach (var entity in _world.Entities)
        entity.Draw(_spriteBatch);
    _spriteBatch.End();
    _tilemapRenderer.RestoreGraphicsDeviceState();

    _tilemapRenderer.DrawLayerGroup("Foreground");
    _tilemapRenderer.EndDraw();
}
```

After modifying tiles in a group, mark it dirty:
```csharp
_tilemapRenderer.MarkGroupDirty("Background");
```

---

## Object layer: spawning GameEntity instances

Iterate `TilemapObjectLayer` at load time to populate your `GameWorld`
(from the `monogame-ecs` skill) with `GameEntity` instances.

```csharp
protected override void LoadContent()
{
    _tilemap = Content.Load<Tilemap>("maps/level1");
    // ... renderer setup ...

    SpawnEntitiesFromMap();
}

private void SpawnEntitiesFromMap()
{
    var objectLayer = _tilemap.Layers["Entities"] as TilemapObjectLayer;
    if (objectLayer == null) return;

    foreach (TilemapObject obj in objectLayer.Objects)
    {
        if (!obj.IsVisible) continue;

        string type = obj.Properties.GetString("type", "");

        switch (obj)
        {
            case TilemapPointObject point:
                // Single spawn point: position only
                SpawnEntity(type, point.Position);
                break;

            case TilemapRectangleObject rect:
                // Area-based: use as collision zone or trigger
                var bounds = new RectangleF(
                    rect.Position.X, rect.Position.Y,
                    rect.Width,      rect.Height);
                RegisterTriggerZone(type, bounds);
                break;
        }
    }
}

// Example helper — adapt to your GameWorld API from monogame-ecs skill
private void SpawnEntity(string type, Vector2 worldPosition)
{
    switch (type)
    {
        case "Player":
            _player = _world.CreateEntity<PlayerEntity>();
            _player.Position = worldPosition;
            break;
        case "Enemy":
            var enemy = _world.CreateEntity<EnemyEntity>();
            enemy.Position = worldPosition;
            break;
        default:
            // unknown type — log and skip
            break;
    }
}
```

To get only rectangles:
```csharp
foreach (TilemapRectangleObject rect in objectLayer.GetObjects<TilemapRectangleObject>())
{
    // Use rect.Position, rect.Width, rect.Height
}
```

---

## Tile layer: reading and modifying tiles

```csharp
TilemapTileLayer ground = _tilemap.Layers["Ground"] as TilemapTileLayer;

// Read one tile (tile coordinates, not pixel coordinates)
TilemapTile? tile = ground.GetTile(col, row);
if (tile.HasValue)
{
    Console.WriteLine($"GID: {tile.Value.GlobalId}");
}

// Iterate all non-empty tiles
foreach (TilemapTileEntry entry in ground.GetTiles())
{
    Console.WriteLine($"Tile ({entry.X}, {entry.Y}): GID {entry.Tile.GlobalId}");
}

// Modify at runtime
ground.SetTile(col, row, new TilemapTile(newGlobalId));
ground.SetTile(col, row, null);  // clear the cell

// Convert pixel position to tile coordinates
int tileCol = (int)(worldPos.X / _tilemap.TileWidth);
int tileRow = (int)(worldPos.Y / _tilemap.TileHeight);
```

---

## Custom properties

```csharp
// Map-level properties
string zone      = _tilemap.Properties.GetString("zone", "default");
int    maxEnemies = _tilemap.Properties.GetInt("maxEnemies", 10);
float  gravity   = _tilemap.Properties.GetFloat("gravity", 9.8f);

// Layer properties
var layer = _tilemap.Layers["Ground"];
bool isDangerous = layer.Properties.GetBool("isDangerous", false);

// Object properties
string type     = obj.Properties.GetString("type", "");
int    hp       = obj.Properties.GetInt("hp", 1);
string dialogue = obj.Properties.GetString("dialogue", "");
```

---

## Runtime parser (no content pipeline)

For loading maps directly from disk at runtime (modding, hot-reload):

```csharp
using MonoGame.Extended.Tilemaps.Tiled;

ITilemapParser parser = new TiledTmxParser();
Tilemap tilemap = parser.ParseFromFile("Content/maps/level1.tmx", GraphicsDevice);
```

Use `BlendState.NonPremultiplied` (the renderer default) for runtime-loaded maps —
textures loaded from PNG are not premultiplied.

For LDtk maps:
```csharp
using MonoGame.Extended.Tilemaps.LDtk;

ITilemapParser parser = new LDtkJsonParser();
Tilemap tilemap = parser.ParseFromFile("Content/maps/world.ldtk", GraphicsDevice);
```
