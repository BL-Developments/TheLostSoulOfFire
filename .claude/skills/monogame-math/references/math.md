# MonoGame Math Reference

## Table of Contents
1. [Vector2 API](#vector2-api)
2. [Vector3 / Vector4](#vector3--vector4)
3. [Matrix operations](#matrix-operations)
4. [Rectangle — 2D AABB](#rectangle--2d-aabb)
5. [Bounding volumes — 3D](#bounding-volumes--3d)
6. [MathHelper utilities](#mathhelper-utilities)
7. [Curve — scalar interpolation over time](#curve--scalar-interpolation-over-time)
8. [Sprite rotation with origin](#sprite-rotation-with-origin)
9. [Sprite scaling](#sprite-scaling)

---

## Vector2 API

```csharp
// Construction (avoid inside Update/Draw — mutate fields instead):
Vector2 v = new Vector2(x, y);
Vector2 zero  = Vector2.Zero;      // (0, 0)
Vector2 one   = Vector2.One;       // (1, 1)
Vector2 unitX = Vector2.UnitX;     // (1, 0)
Vector2 unitY = Vector2.UnitY;     // (0, 1)

// Arithmetic (returns new Vector2):
Vector2 sum  = a + b;
Vector2 diff = a - b;
Vector2 scaled = a * 2f;
Vector2 neg  = -a;

// Mutation (no allocation):
v.X += dx;
v.Y += dy;

// Length / distance:
float len   = v.Length();                        // uses sqrt
float lenSq = v.LengthSquared();                // no sqrt — use for comparisons
float dist  = Vector2.Distance(a, b);           // uses sqrt
float distSq = Vector2.DistanceSquared(a, b);  // no sqrt — prefer for range checks

// Normalize (returns new unit vector):
Vector2 dir = Vector2.Normalize(v);
// In-place (ref overload, no alloc):
Vector2.Normalize(ref v, out v);

// Dot product (cos(angle) × |a| × |b|):
float dot = Vector2.Dot(a, b);

// Interpolation:
Vector2 lerped  = Vector2.Lerp(a, b, t);              // linear
Vector2 clamped = Vector2.Clamp(v, minVec, maxVec);   // per-component clamp

// Reflection and transform:
Vector2 reflected  = Vector2.Reflect(v, normal);
Vector2 transformed = Vector2.Transform(v, matrix);
Vector2 transformed = Vector2.TransformNormal(v, matrix); // direction only (ignores translation)

// Direction from angle (radians):
float angle = MathHelper.ToRadians(45f);
Vector2 dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

// Angle from direction:
float angle = MathF.Atan2(dir.Y, dir.X);
```

---

## Vector3 / Vector4

```csharp
Vector3 v3 = new Vector3(x, y, z);
Vector3.Cross(a, b)          // cross product → perpendicular vector
Vector3.Dot(a, b)
Vector3.Normalize(v)
Vector3.Distance(a, b)
Vector3.Transform(v, matrix)

Vector4 v4 = new Vector4(x, y, z, w);
// Commonly used for shader Color parameters (RGBA):
Color.White.ToVector4()      // → Vector4(1, 1, 1, 1)
```

---

## Matrix operations

```csharp
// Identity:
Matrix m = Matrix.Identity;

// Transforms (each returns a new Matrix):
Matrix t = Matrix.CreateTranslation(x, y, z);
Matrix s = Matrix.CreateScale(sx, sy, sz);
Matrix s = Matrix.CreateScale(uniformScale);
Matrix r = Matrix.CreateRotationZ(angleRadians);   // 2D rotation
Matrix r = Matrix.CreateRotationX(angle);
Matrix r = Matrix.CreateRotationY(angle);

// Combine (SRT order for world matrix):
Matrix world = Matrix.CreateScale(scale)
             * Matrix.CreateRotationZ(rotation)
             * Matrix.CreateTranslation(pos.X, pos.Y, 0f);

// Camera matrices:
Matrix view = Matrix.CreateLookAt(cameraPos, target, up);
Matrix proj = Matrix.CreateOrthographicOffCenter(0, w, h, 0, 0, 1); // 2D
Matrix proj = Matrix.CreatePerspectiveFieldOfView(fovRadians, aspectRatio, near, far);

// 2D camera with zoom and offset:
Matrix cam = Matrix.CreateTranslation(-camPos.X, -camPos.Y, 0f)
           * Matrix.CreateRotationZ(camRotation)
           * Matrix.CreateScale(camZoom, camZoom, 1f)
           * Matrix.CreateTranslation(screenW / 2f, screenH / 2f, 0f);

// Invert (expensive — cache as field, don't call per frame):
Matrix inv = Matrix.Invert(cam);
// Ref overload (avoids copy):
Matrix.Invert(ref cam, out Matrix inv);

// Transpose:
Matrix transposed = Matrix.Transpose(m);

// Multiply (ref overload avoids copy):
Matrix.Multiply(ref a, ref b, out Matrix result);

// Decompose:
m.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation);
```

---

## Rectangle — 2D AABB

```csharp
// Construction:
Rectangle r = new Rectangle(x, y, width, height);  // x,y = top-left

// Properties:
r.X; r.Y; r.Width; r.Height;
r.Left; r.Right; r.Top; r.Bottom;  // pixel coordinates of edges
r.Center;   // Point — center of rectangle
r.Location; // Point — top-left (x, y)
r.Size;     // Point — (width, height)

// Tests:
bool hit    = r1.Intersects(r2);
bool inside = r.Contains(new Point(px, py));
bool inside = r.Contains(new Rectangle(...));

// Overlap area (for push-out collision response):
Rectangle overlap;
Rectangle.Intersect(ref r1, ref r2, out overlap);
// overlap.Width / overlap.Height → penetration depth per axis

// Union (smallest rect containing both):
Rectangle union = Rectangle.Union(r1, r2);

// Empty rect:
bool empty = r.IsEmpty;  // width == 0 || height == 0
```

### Push-out collision response

```csharp
Rectangle.Intersect(ref playerRect, ref wallRect, out Rectangle overlap);
if (!overlap.IsEmpty)
{
    if (overlap.Width < overlap.Height)
        // shallower on X — push horizontally
        player.X += playerRect.Left < wallRect.Left ? -overlap.Width : overlap.Width;
    else
        // shallower on Y — push vertically
        player.Y += playerRect.Top < wallRect.Top ? -overlap.Height : overlap.Height;
}
```

---

## Bounding volumes — 3D

```csharp
// BoundingSphere:
var sphere = new BoundingSphere(center, radius);
bool hit = sphere1.Intersects(sphere2);
ContainmentType c = sphere.Contains(point);        // or Contains(sphere), Contains(box)
// ContainmentType: Contains, Intersects, Disjoint

// Translate a sphere with a world matrix:
BoundingSphere worldSphere = sphere.Transform(worldMatrix);

// BoundingBox (AABB):
var box = new BoundingBox(minCorner, maxCorner);
// Build from an arbitrary set of points:
BoundingBox box = BoundingBox.CreateFromPoints(IEnumerable<Vector3> points);
bool hit = box1.Intersects(box2);
ContainmentType c = box1.Contains(box2);

// BoundingFrustum (camera visibility culling):
var frustum = new BoundingFrustum(view * projection); // rebuild when camera changes
bool visible = frustum.Intersects(sphere);
bool visible = frustum.Intersects(box);
ContainmentType ct = frustum.Contains(sphere);        // Contains / Intersects / Disjoint

// Ray:
var ray = new Ray(origin, direction); // direction must be normalized
float? dist = ray.Intersects(sphere);
float? dist = ray.Intersects(box);
float? dist = ray.Intersects(plane);
if (dist.HasValue)
    Vector3 hitPoint = ray.Position + ray.Direction * dist.Value;

// Plane:
var plane = new Plane(normalVector, distanceFromOrigin);
var plane = new Plane(pointA, pointB, pointC);        // from three points
PlaneIntersectionType side = plane.Intersects(sphere); // Front, Back, or Intersecting
PlaneIntersectionType side = plane.Intersects(box);
```

### ContainmentType enum

```csharp
ContainmentType.Contains    // tested object is fully inside
ContainmentType.Intersects  // objects overlap partially
ContainmentType.Disjoint    // no overlap — safe to skip
```

### PlaneIntersectionType enum

```csharp
PlaneIntersectionType.Front        // object is on the positive (normal) side
PlaneIntersectionType.Back         // object is behind the plane
PlaneIntersectionType.Intersecting // object straddles the plane
```

### Model bounding spheres (Content Pipeline)

The Content Pipeline pre-calculates a `BoundingSphere` per `ModelMesh`. Transform it into world space for collision tests:

```csharp
// Single mesh — fast sphere-sphere check:
BoundingSphere worldSphere = model.Meshes[0].BoundingSphere.Transform(worldMatrix);
bool hit = worldSphere.Intersects(other);

// Full model — iterate all meshes for accuracy:
bool ModelsOverlap(Model a, Matrix aWorld, Model b, Matrix bWorld)
{
    foreach (ModelMesh meshA in a.Meshes)
    {
        BoundingSphere sa = meshA.BoundingSphere.Transform(aWorld);
        foreach (ModelMesh meshB in b.Meshes)
        {
            if (sa.Intersects(meshB.BoundingSphere.Transform(bWorld)))
                return true;
        }
    }
    return false;
}
```

### Frustum culling pattern

```csharp
// In Update() — rebuild once when camera changes:
_frustum = new BoundingFrustum(_camera.ViewMatrix * _camera.ProjectionMatrix);

// In Draw() — skip invisible entities:
foreach (var entity in _entities)
{
    if (_frustum.Contains(entity.WorldBounds) != ContainmentType.Disjoint)
        entity.Draw();
}
```

---

## MathHelper utilities

```csharp
MathHelper.Pi         // 3.14159...
MathHelper.TwoPi      // 6.28318...
MathHelper.PiOver2    // 1.5708...
MathHelper.PiOver4    // 0.7854...
MathHelper.E          // 2.71828...

MathHelper.ToRadians(float degrees) // → radians
MathHelper.ToDegrees(float radians) // → degrees

MathHelper.Clamp(value, min, max)
MathHelper.Lerp(a, b, t)              // linear interpolation
MathHelper.SmoothStep(a, b, t)        // smooth S-curve (3t² - 2t³)
MathHelper.Hermite(v1, t1, v2, t2, s) // cubic Hermite spline
MathHelper.Barycentric(v1, v2, v3, b2, b3)
MathHelper.CatmullRom(v1, v2, v3, v4, t)

MathHelper.WrapAngle(float angle)     // wrap to [-π, π]
MathHelper.Min(a, b)
MathHelper.Max(a, b)
MathHelper.Distance(a, b)            // |a - b|
```

---

## Curve — scalar interpolation over time

```csharp
// Build a curve with CurveKey(position/time, value):
var curve = new Curve();
curve.Keys.Add(new CurveKey(0f,   0f));
curve.Keys.Add(new CurveKey(0.5f, 1f));
curve.Keys.Add(new CurveKey(1f,   0f));

// Auto-compute tangents after all keys are added:
curve.ComputeTangents(CurveTangent.Smooth);  // options: Flat, Linear, Smooth

// Evaluate (returns interpolated value at any time):
float v = curve.Evaluate(t);

// Step (discontinuous jump — no interpolation between keys):
var key = new CurveKey(0.5f, 1f, 0f, 0f, CurveContinuity.Step);
```

### Loop types (PreLoop / PostLoop)

```csharp
curve.PreLoop  = CurveLoopType.Constant;    // hold value of first/last key
curve.PostLoop = CurveLoopType.Cycle;       // repeat from the start
curve.PostLoop = CurveLoopType.CycleOffset; // repeat, adding total delta each cycle
curve.PostLoop = CurveLoopType.Oscillate;   // ping-pong (reverse each cycle)
curve.PostLoop = CurveLoopType.Linear;      // extrapolate linearly past the endpoint
```

### CurveKey properties

```csharp
CurveKey k = curve.Keys[0];
k.Position   // float — the time/x value
k.Value      // float — the output/y value
k.TangentIn  // float — incoming slope (affects interpolation from previous key)
k.TangentOut // float — outgoing slope (affects interpolation to next key)
k.Continuity // CurveContinuity.Smooth (default) or CurveContinuity.Step
```

### Common patterns

```csharp
// Ease-in/out lerp over a fixed duration:
float t = MathHelper.Clamp(_elapsed / _duration, 0f, 1f);
float easedValue = _easeCurve.Evaluate(t);

// Decay curve (e.g. camera shake) — no loop needed:
// Keys: (0f, 1f), (0.3f, 0f) → ComputeTangents(CurveTangent.Smooth)
float shakeStrength = _shakeCurve.Evaluate(_shakeTimer);

// Bouncing ball height — oscillating, defined over [0, 1]:
curve.PostLoop = CurveLoopType.Cycle;
float height = _bounceCurve.Evaluate(Time.Total % 1f);
```

> `Curve` is scalar only. For Vector2/3 paths, use one `Curve` per axis (X and Y separately).

---

## Sprite rotation with origin

```csharp
// Center origin (rotate around sprite center):
Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);

// Draw with rotation (angle in radians):
spriteBatch.Draw(
    texture,
    position,    // position of the origin point in world space
    null,        // source rect
    Color.White,
    rotation,    // float, radians, clockwise
    origin,      // pivot in texture coordinates
    Vector2.One, // scale
    SpriteEffects.None,
    0f           // layerDepth
);

// Smooth rotation in Update:
_rotation += rotationSpeed * delta;
_rotation = MathHelper.WrapAngle(_rotation); // keep in [-π, π]
```

---

## Sprite scaling

```csharp
// Uniform scale:
spriteBatch.Draw(texture, position, null, Color.White, 0f, Vector2.Zero, scale: 2.0f, SpriteEffects.None, 0f);

// Non-uniform scale (Vector2):
spriteBatch.Draw(texture, position, null, Color.White, 0f, Vector2.Zero, new Vector2(2f, 1.5f), SpriteEffects.None, 0f);

// Destination rectangle (stretches to fit):
var destRect = new Rectangle(x, y, targetWidth, targetHeight);
spriteBatch.Draw(texture, destRect, Color.White);
// With source rect and rotation:
spriteBatch.Draw(texture, destRect, sourceRect, Color.White, rotation, origin, SpriteEffects.None, 0f);
```
