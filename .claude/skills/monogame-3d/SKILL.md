---
name: monogame-3d
description: MonoGame 3D rendering guide covering BasicEffect, world/view/projection matrices, 3D camera (LookAt, rotate/move, fit-to-scene), vertex types (VertexPositionColor, VertexPositionColorTexture, custom IVertexType), DrawUserPrimitives, indexed primitives, state objects (RasterizerState, DepthStencilState), model loading and bone transforms, dynamic vertex buffers, mouse picking via ray casting, and 3D collision detection. Use this skill whenever the user asks about 3D rendering, BasicEffect, DrawPrimitives, model loading, 3D camera, vertex buffers, custom vertices, state objects, ray casting, mouse picking, or 3D collision — even if they just say "how do I render a 3D object", "my model doesn't appear", or "how do I click on a 3D object".
---

# MonoGame 3D Rendering Implementation Guide

This skill provides architecture rules and implementation patterns for MonoGame 3D graphics. For complete API signatures and code samples, read `references/3d.md`.

## Graphics Profile

Before writing any 3D code, verify `GraphicsProfile`:
- `GraphicsProfile.Reach` — shader model 2.0/3.0; safe for all platforms including mobile
- `GraphicsProfile.HiDef` — shader model 4.0+; desktop/console only

Set in the `Game` constructor:
```csharp
_graphics = new GraphicsDeviceManager(this)
{
    GraphicsProfile = GraphicsProfile.HiDef
};
```

## 3D Camera

A 3D camera is defined by three matrices: **World**, **View**, and **Projection**.

### Core setup

```csharp
// View matrix — where the camera is and what it looks at
_view = Matrix.CreateLookAt(
    cameraPosition,   // Vector3 — camera world position
    cameraTarget,     // Vector3 — point camera looks at
    Vector3.Up        // Vector3 — which direction is "up"
);

// Projection matrix — perspective frustum
_projection = Matrix.CreatePerspectiveFieldOfView(
    MathHelper.ToRadians(45f),                          // vertical FOV
    GraphicsDevice.Viewport.AspectRatio,                // width / height
    0.1f,                                               // near clip plane
    1000f                                               // far clip plane
);
```

Compute `_view` every `Update()` when the camera moves. Compute `_projection` once in `LoadContent()`, and again only when the viewport changes.

### Rotating and moving a camera

Store the camera's position and a forward direction; rotate the forward vector using `CreateRotationY`:

```csharp
// Fields:
private Vector3 _cameraPos     = new Vector3(0, 0, 10f);
private Vector3 _cameraForward = Vector3.Forward; // (0, 0, -1)
private float   _cameraYaw     = 0f;

// In Update:
_cameraYaw += yawDelta;                                    // from input
_cameraForward = Vector3.Transform(Vector3.Forward,
    Matrix.CreateRotationY(_cameraYaw));

// Move along current forward:
_cameraPos += _cameraForward * moveSpeed * elapsed;

// Rebuild view:
_view = Matrix.CreateLookAt(_cameraPos, _cameraPos + _cameraForward, Vector3.Up);
```

### Fit camera to scene

Position the camera far enough to include all objects in the frustum:

```csharp
// Merge all model bounding spheres:
BoundingSphere sceneBounds = models[0].Meshes[0].BoundingSphere;
foreach (var model in models)
    foreach (var mesh in model.Meshes)
        sceneBounds = BoundingSphere.CreateMerged(sceneBounds, mesh.BoundingSphere);

// Distance needed to contain the full sphere:
float dist = sceneBounds.Radius / MathF.Sin(fovY / 2f);

// Move camera backward along its current orientation:
_cameraPos = sceneBounds.Center - _view.Forward * dist;
_view = Matrix.CreateLookAt(_cameraPos, sceneBounds.Center, Vector3.Up);
```

## BasicEffect

`BasicEffect` is MonoGame's built-in 3D effect. It handles vertex colors, texturing, and per-vertex lighting without a custom shader.

### Initialization

```csharp
// In LoadContent:
_basicEffect = new BasicEffect(GraphicsDevice)
{
    VertexColorEnabled = true,       // enables per-vertex color
    World      = Matrix.Identity,
    View       = _view,
    Projection = _projection
};
```

### Enabling texture mapping

```csharp
_basicEffect.TextureEnabled   = true;
_basicEffect.VertexColorEnabled = false;   // can't use both simultaneously
_basicEffect.Texture = _myTexture;
```

### Enabling lighting

```csharp
_basicEffect.LightingEnabled = true;
_basicEffect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);
_basicEffect.DirectionalLight0.Enabled = true;
_basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1, -1, -1));
_basicEffect.DirectionalLight0.DiffuseColor = Vector3.One;  // white
```

### Rendering loop

Always iterate through technique passes before calling any Draw method:

```csharp
foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes)
{
    pass.Apply();
    GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, _vertices, 0, primitiveCount);
}
```

## Vertex Types

### Built-in vertex formats

| Type | Fields | Use when |
|------|--------|----------|
| `VertexPositionColor` | Position, Color | Simple colored geometry |
| `VertexPositionTexture` | Position, TextureCoordinate | Textured without per-vertex color |
| `VertexPositionColorTexture` | Position, Color, TextureCoordinate | Tinted textured geometry |
| `VertexPositionNormalTexture` | Position, Normal, TextureCoordinate | Lit textured geometry |

### Custom vertex type

Implement `IVertexType` when you need extra data (e.g., a second UV set):

```csharp
public struct MyVertex : IVertexType
{
    public Vector3 Position;
    public Vector2 TexCoord;

    public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
        new VertexElement(0,  VertexElementFormat.Vector3, VertexElementUsage.Position,          0),
        new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
    );

    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
}
```

Offsets are byte offsets: `Vector3` = 12 bytes, `Vector2` = 8 bytes.

## Drawing Primitives

### Immediate (no buffer) — `DrawUserPrimitives`

```csharp
var verts = new VertexPositionColor[]
{
    new VertexPositionColor(new Vector3( 0,  1, 0), Color.Red),
    new VertexPositionColor(new Vector3(-1, -1, 0), Color.Green),
    new VertexPositionColor(new Vector3( 1, -1, 0), Color.Blue),
};

foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes)
{
    pass.Apply();
    GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, verts, 0, 1);
}
```

### Indexed — `DrawUserIndexedPrimitives`

```csharp
var verts   = new VertexPositionColor[4]; // quad corners
var indices = new short[] { 0, 1, 2, 0, 2, 3 }; // two triangles

foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes)
{
    pass.Apply();
    GraphicsDevice.DrawUserIndexedPrimitives(
        PrimitiveType.TriangleList,
        verts, 0, 4,       // vertex buffer, offset, count
        indices, 0, 2      // index buffer, offset, primitive count
    );
}
```

### Primitive types

| PrimitiveType | Description |
|---------------|-------------|
| `TriangleList` | Every 3 indices = 1 triangle (no sharing) |
| `TriangleStrip` | First triangle uses 3 verts; each additional uses 1 more |
| `LineList` | Every 2 indices = 1 line segment |
| `LineStrip` | Connected line segments |

## Static and Dynamic Vertex Buffers

### Static (geometry defined once)

```csharp
// In LoadContent:
_vertexBuffer = new VertexBuffer(
    GraphicsDevice,
    VertexPositionColor.VertexDeclaration,
    vertices.Length,
    BufferUsage.WriteOnly
);
_vertexBuffer.SetData(vertices);

// In Draw:
GraphicsDevice.SetVertexBuffer(_vertexBuffer);
foreach (EffectPass pass in _basicEffect.CurrentTechnique.Passes)
{
    pass.Apply();
    GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, primitiveCount);
}
```

### Dynamic (geometry changes every frame)

```csharp
// In LoadContent:
_dynamicBuffer = new DynamicVertexBuffer(
    GraphicsDevice,
    VertexPositionColor.VertexDeclaration,
    maxVertices,
    BufferUsage.WriteOnly
);

// In Update (after rebuilding vertex data):
_dynamicBuffer.SetData(_vertices, 0, _vertexCount, SetDataOptions.Discard);
```

Use `SetDataOptions.Discard` to avoid GPU stalls — it signals that the old data can be discarded.

## State Objects

Create state objects in `LoadContent()` — never per-frame.

### RasterizerState

```csharp
// Disable backface culling (show both sides of triangles):
var rasterNoCull = new RasterizerState { CullMode = CullMode.None };
GraphicsDevice.RasterizerState = rasterNoCull;

// Wireframe:
var rasterWire = new RasterizerState
{
    CullMode = CullMode.None,
    FillMode = FillMode.WireFrame
};

// Predefined presets:
GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise; // default
GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
GraphicsDevice.RasterizerState = RasterizerState.CullNone;
```

### DepthStencilState

```csharp
// Enable depth testing (default for 3D):
GraphicsDevice.DepthStencilState = DepthStencilState.Default;

// Disable depth write (particles, transparent objects):
GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

// Disable depth entirely (2D overlay on top of 3D scene):
GraphicsDevice.DepthStencilState = DepthStencilState.None;
```

Always restore default state after drawing transparent/special objects.

### Anti-aliasing

```csharp
// In Game constructor:
_graphics.PreferMultiSampling = true;
```

This requests multi-sample anti-aliasing from the device. Effect depends on platform and hardware support.

## Model Rendering

Load FBX/OBJ models through the Content Pipeline:

```csharp
// In LoadContent:
_model = Content.Load<Model>("Models/MyModel");
_boneTransforms = new Matrix[_model.Bones.Count];

// In Draw — full pattern:
_model.CopyAbsoluteBoneTransformsTo(_boneTransforms);

foreach (ModelMesh mesh in _model.Meshes)
{
    foreach (BasicEffect effect in mesh.Effects)
    {
        effect.World      = _boneTransforms[mesh.ParentBone.Index] * _worldMatrix;
        effect.View       = _view;
        effect.Projection = _projection;
        effect.EnableDefaultLighting();
    }
    mesh.Draw();
}
```

`CopyAbsoluteBoneTransformsTo` is required for models with more than one bone — skip it only for static single-mesh models.

## Mouse Picking (Ray Casting)

Create a ray from the camera through the mouse cursor and test it against scene objects:

```csharp
// In Update:
MouseState mouse = Mouse.GetState();
Viewport vp = GraphicsDevice.Viewport;

Vector3 nearPoint = vp.Unproject(
    new Vector3(mouse.X, mouse.Y, 0f),
    _projection, _view, Matrix.Identity);

Vector3 farPoint = vp.Unproject(
    new Vector3(mouse.X, mouse.Y, 1f),
    _projection, _view, Matrix.Identity);

Vector3 direction = Vector3.Normalize(farPoint - nearPoint);
var pickRay = new Ray(nearPoint, direction);

// Test against bounding sphere:
float? dist = pickRay.Intersects(targetSphere);
if (dist.HasValue)
{
    Vector3 hitPoint = nearPoint + direction * dist.Value;
    // ... handle hit
}
```

When multiple objects overlap, collect all `(object, dist)` pairs and sort by `dist` — the smallest distance is the frontmost hit.

## 3D Collision Detection

For model-to-model collision, iterate mesh bounding spheres transformed by each model's world matrix:

```csharp
bool ModelsCollide(Model a, Matrix aWorld, Model b, Matrix bWorld)
{
    foreach (ModelMesh meshA in a.Meshes)
    {
        BoundingSphere sA = meshA.BoundingSphere.Transform(aWorld);
        foreach (ModelMesh meshB in b.Meshes)
        {
            BoundingSphere sB = meshB.BoundingSphere.Transform(bWorld);
            if (sA.Intersects(sB)) return true;
        }
    }
    return false;
}
```

On collision response, reverse the velocity and move the object back to its previous valid position:
```csharp
if (ModelsCollide(a, aWorld, b, bWorld))
{
    _velocity = -_velocity;
    _position = _previousPosition;
}
```

For bounding sphere and BoundingBox API details, see `monogame-math` skill.

## Rules

- **Never allocate vertex arrays in `Draw()`** — allocate in `LoadContent()` or `Initialize()`, update in `Update()`.
- **Create state objects once** — `new RasterizerState(...)` in `LoadContent()`, not inside the draw loop.
- **Restore default states** after rendering special objects (transparent, wireframe) to avoid corrupting subsequent draw calls.
- **Call `pass.Apply()` before every `DrawPrimitives` call** — skipping it leaves the previous pass's state active.
- **`_basicEffect.World` must be updated per object** — set it before each mesh's `pass.Apply()`.

## Reference

For complete vertex type constructors, effect parameter lists, and additional code patterns, read `references/3d.md`.
