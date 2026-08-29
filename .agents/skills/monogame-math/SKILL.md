---
name: monogame-math
description: MonoGame math implementation guide covering Vector2/3/4, Matrix, Rectangle, collision detection, camera transforms, and MathHelper utilities. Use this skill whenever the user asks about vectors, positions, distances, rotations, scaling, collision detection, AABB, bounding volumes, 2D camera math, coordinate transforms, or any math-related implementation in MonoGame.
---

# MonoGame Math Implementation Guide

This skill covers math types and patterns in MonoGame. For bounding volume details, see `references/math.md`.

## Vectors

`Vector2`, `Vector3`, and `Vector4` are **value types (structs)**. Do not allocate them with `new` inside `Update()` or `Draw()` — mutate fields instead.

```csharp
// BAD — allocates every frame:
_velocity = new Vector2(_velocity.X + acceleration, _velocity.Y);

// GOOD — mutate in place:
_velocity.X += acceleration;
```

### Common Vector2 operations

```csharp
Vector2 a, b;

float length    = a.Length();                          // expensive (sqrt)
float lengthSq  = a.LengthSquared();                  // cheap — use for distance comparisons
float distance  = Vector2.Distance(a, b);              // expensive
float distSq    = Vector2.DistanceSquared(a, b);       // cheap for "is within range X" checks
Vector2 norm    = Vector2.Normalize(a);               // unit vector (new instance)
Vector2 lerped  = Vector2.Lerp(a, b, t);              // linear interpolation, t in [0,1]
Vector2 clamped = Vector2.Clamp(a, min, max);         // per-component clamp
float   dot     = Vector2.Dot(a, b);                  // cos(angle) * |a| * |b|
```

### Direction from angle (radians)

```csharp
float angle = MathHelper.ToRadians(45f);
Vector2 dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
```

## Rectangle (2D AABB Collision)

`Rectangle` is a value type. Use it for 2D axis-aligned bounding box (AABB) collision — the most common pattern in 2D games.

```csharp
Rectangle playerBounds = new Rectangle((int)_pos.X, (int)_pos.Y, 32, 32);
Rectangle wallBounds   = new Rectangle(100, 100, 64, 64);

bool collides   = playerBounds.Intersects(wallBounds);
bool contains   = wallBounds.Contains(new Point(x, y));

// Overlap area:
Rectangle.Intersect(ref playerBounds, ref wallBounds, out Rectangle overlap);
```

When hitboxes don't match the sprite size, define the hitbox dimensions as separate constants rather than using texture width/height directly.

## Matrix (Transforms)

`Matrix` is a value type. Build it once per frame in `Update()` and cache it as a field — do not rebuild in `Draw()`.

```csharp
// Translation
Matrix t = Matrix.CreateTranslation(x, y, 0f);

// Scale
Matrix s = Matrix.CreateScale(2f, 2f, 1f);

// Rotation around Z (2D)
Matrix r = Matrix.CreateRotationZ(angleInRadians);

// Combine (order matters: scale × rotation × translation for SRT)
Matrix world = Matrix.CreateScale(scale) *
               Matrix.CreateRotationZ(rotation) *
               Matrix.CreateTranslation(position.X, position.Y, 0f);
```

### 2D Camera matrix

```csharp
Matrix camera =
    Matrix.CreateTranslation(-_camPos.X, -_camPos.Y, 0f) *
    Matrix.CreateRotationZ(_camRotation) *
    Matrix.CreateScale(_camZoom, _camZoom, 1f) *
    Matrix.CreateTranslation(screenCenterX, screenCenterY, 0f);
```

### Screen-to-world conversion

```csharp
// Cache the inverse — don't recompute per call:
_inverseCamera = Matrix.Invert(_cameraMatrix);

Vector2 worldPos = Vector2.Transform(screenPos, _inverseCamera);
```

## MathHelper Utilities

```csharp
MathHelper.Clamp(value, min, max)          // clamp float
MathHelper.Lerp(value1, value2, amount)    // linear interpolation
MathHelper.SmoothStep(value1, value2, t)   // smooth S-curve interpolation
MathHelper.ToDegrees(radians)
MathHelper.ToRadians(degrees)
MathHelper.WrapAngle(angle)                // wrap to [-π, π]
MathHelper.Pi                             // 3.14159...
MathHelper.TwoPi                          // 6.28318...
MathHelper.PiOver2                        // π/2
```

## Distance Checks — Fast vs Exact

For "is the enemy within X units?" comparisons, **always use `DistanceSquared`**:

```csharp
float rangeSquared = detectionRange * detectionRange;
if (Vector2.DistanceSquared(enemyPos, playerPos) <= rangeSquared)
    Chase();
```

Only call `Vector2.Distance()` (which uses `sqrt`) when you need the actual distance value (e.g., for UI display or audio attenuation).

## Bounding Volumes (3D)

### Choosing a volume

| Type | Pros | Cons | Best for |
|------|------|------|----------|
| `BoundingSphere` | Fastest test (distance vs radii sum). Rotation-independent — no rebuild needed. | False positives on long/narrow objects. | Rough first-pass culling, bullets, explosions |
| `BoundingBox` | Tight fit on axis-aligned rectangles. Fast axis-aligned test. | Must rebuild on rotation. Worst fit at 45°. | Level tiles, non-rotating objects |
| `BoundingFrustum` | Matches exactly what the camera sees. | Expensive to construct — rebuild only on camera move. | Visibility culling, skipping off-screen draw calls |
| `Ray` | No volume — a directed line from a point. | Not for volume-vs-volume tests. | Mouse picking, raycasting, line-of-sight |
| `Plane` | Splits space into front/back. | Returns relative position, not contact point. | Portal/sector visibility, reflection planes |

### BoundingSphere and BoundingBox

```csharp
var sphere1 = new BoundingSphere(center1, radius1);
var sphere2 = new BoundingSphere(center2, radius2);
bool hit = sphere1.Intersects(sphere2);  // true / false

// ContainmentType: Contains, Intersects, or Disjoint
ContainmentType ct = sphere1.Contains(sphere2);

// Ray against sphere — returns distance or null:
float? dist = ray.Intersects(sphere1);
if (dist.HasValue)
    Vector3 hitPoint = ray.Position + ray.Direction * dist.Value;

// BoundingBox from a set of points (e.g., model vertices):
var box = BoundingBox.CreateFromPoints(vertices);
bool hit = box1.Intersects(box2);
```

### BoundingFrustum — visibility culling

Rebuild whenever the camera view or projection changes. Skip drawing anything that is `Disjoint`:

```csharp
// Rebuild (expensive — once per frame when camera moves):
_frustum = new BoundingFrustum(_camera.View * _camera.Projection);

// Cull in Draw():
foreach (var entity in _entities)
{
    if (_frustum.Contains(entity.BoundingSphere) != ContainmentType.Disjoint)
        entity.Draw(_spriteBatch);
}
```

### Model bounding volumes

The Content Pipeline automatically calculates a `BoundingSphere` for each `ModelMesh`. Use it for model-model collision without manual vertex iteration:

```csharp
// Check all mesh spheres of two models:
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

### Plane

```csharp
// Plane defined by normal + distance from origin:
var plane = new Plane(normal, d);
// Or from three points:
var plane = new Plane(pointA, pointB, pointC);

// Returns PlaneIntersectionType: Front, Back, or Intersecting
PlaneIntersectionType side = plane.Intersects(sphere);
PlaneIntersectionType side = plane.Intersects(box);
```

### Contains vs Intersects

- `Intersects()` stops as soon as any overlap is found — faster, returns `bool`.
- `Contains()` checks whether one object fully encloses another — returns `ContainmentType` (`Contains`, `Intersects`, `Disjoint`). Slightly slower.

Use `Intersects` in hot paths (per-frame collision). Use `Contains` when you need to know if an entity is fully inside a zone (e.g., trigger volumes, frustum culling with LOD).

## Curve — Value Interpolation Over Time

`Curve` maps a float input (time) to a float output (value) via Hermite spline. Use it for easing, non-linear property animation, or any value that needs to change over time without hardcoding every frame.

```csharp
var curve = new Curve();
curve.Keys.Add(new CurveKey(0f,   0f));   // at t=0, value=0
curve.Keys.Add(new CurveKey(0.5f, 1f));   // at t=0.5, value=1
curve.Keys.Add(new CurveKey(1f,   0f));   // at t=1, value=0

// Smooth tangents automatically (call after adding all keys):
curve.ComputeTangents(CurveTangent.Smooth);

// Evaluate at any time:
float value = curve.Evaluate(t);  // t can be outside [0,1] — controlled by loop type
```

Control what happens outside the defined range with `PreLoop` / `PostLoop`:

```csharp
curve.PreLoop  = CurveLoopType.Constant;    // hold first/last value
curve.PostLoop = CurveLoopType.Cycle;       // repeat
curve.PostLoop = CurveLoopType.Oscillate;   // ping-pong
curve.PostLoop = CurveLoopType.Linear;      // extrapolate linearly
curve.PostLoop = CurveLoopType.CycleOffset; // repeat, accumulating the total delta
```

Step (discontinuous) transitions:

```csharp
// Use CurveContinuity.Step to snap instead of interpolate between two keys:
curve.Keys.Add(new CurveKey(0.5f, 1f, 0f, 0f, CurveContinuity.Step));
```

Typical patterns:

```csharp
// Ease-in / ease-out for a move animation (0→1 over duration):
float _elapsed;
float _duration = 0.4f;

float Progress => MathHelper.Clamp(_elapsed / _duration, 0f, 1f);
float EasedPos  => _easeCurve.Evaluate(Progress);

// Camera shake intensity that decays over 0.3 s:
// Keys: (0, 1), (0.3, 0)  →  ComputeTangents(CurveTangent.Smooth)
float shakeAmount = _shakeCurve.Evaluate(_shakeTimer);
```

`Curve` only interpolates **scalars**. For Vector2/Vector3 paths, use one `Curve` per component (X and Y separately).

## Rules

- Use `DistanceSquared` for range/proximity checks — avoid `sqrt` in hot paths.
- Never write `new Vector2(...)` inside `Update()` or `Draw()` — mutate fields or use static helpers.
- Cache `Matrix.Invert(camera)` as a field, not recomputed per frame.
- `Rectangle` intersection gives you an overlap region — use it to resolve push-out collision response.
- 2D rotations use `float` in radians; `Quaternion` is only for 3D.
