# MonoGame Camera Modes Reference

Quick-copy snippets for each camera mode. All modes use `Matrix.CreateLookAt` — only the position/target calculation differs.

## Table of Contents
1. [Shared view helper](#shared-view-helper)
2. [Fixed camera](#fixed-camera)
3. [Fixed tracking camera](#fixed-tracking-camera)
4. [First person camera](#first-person-camera)
5. [Third person camera (no spring)](#third-person-camera-no-spring)
6. [Third person camera (spring physics)](#third-person-camera-spring-physics)
7. [Top-down fixed camera](#top-down-fixed-camera)
8. [Top-down tracked camera](#top-down-tracked-camera)
9. [Orthographic projection (map view)](#orthographic-projection-map-view)
10. [Camera mode enum + dispatcher](#camera-mode-enum--dispatcher)

---

## Shared view helper

```csharp
private Matrix _view;
private Vector3 _currentCameraPos;

void UpdateCameraView(Vector3 position, Vector3 target)
{
    _currentCameraPos = position;
    _view = Matrix.CreateLookAt(position, target, Vector3.Up);
}
```

---

## Fixed camera

```csharp
private readonly Vector3 _fixedPos = new Vector3(0f, 1550f, 5000f);

void UpdateFixedCamera()
{
    UpdateCameraView(_fixedPos, Vector3.Zero);
}
```

---

## Fixed tracking camera

```csharp
private readonly Vector3 _trackingPos = new Vector3(0f, 1550f, 5000f);

void UpdateTrackingCamera(Vector3 targetPosition)
{
    UpdateCameraView(_trackingPos, targetPosition);
}
```

---

## First person camera

```csharp
private readonly Vector3 _fpEyeOffset = new Vector3(0f, 50f, 500f);

void UpdateFirstPersonCamera(Vector3 playerPos, float playerYaw)
{
    Vector3 eye = Vector3.Transform(_fpEyeOffset,
                      Matrix.CreateRotationY(playerYaw)) + playerPos;
    UpdateCameraView(eye, playerPos);
}
```

---

## Third person camera (no spring)

```csharp
private readonly Vector3 _tpOffset = new Vector3(0f, 1550f, 5000f);

void UpdateThirdPersonCamera(Vector3 playerPos, float playerYaw)
{
    Vector3 camPos = Vector3.Transform(_tpOffset,
                         Matrix.CreateRotationY(playerYaw)) + playerPos;
    UpdateCameraView(camPos, playerPos);
}
```

---

## Third person camera (spring physics)

```csharp
private Vector3 _springVelocity = Vector3.Zero;
private const float Stiffness = 1800f;
private const float Damping   = 600f;
private const float Mass      = 50f;

private readonly Vector3 _tpOffset = new Vector3(0f, 1550f, 5000f);

void UpdateThirdPersonCameraSpring(Vector3 playerPos, float playerYaw, float elapsed)
{
    Vector3 desired = Vector3.Transform(_tpOffset,
                          Matrix.CreateRotationY(playerYaw)) + playerPos;

    Vector3 stretch = _currentCameraPos - desired;
    Vector3 force   = -Stiffness * stretch - Damping * _springVelocity;

    _springVelocity  += (force / Mass) * elapsed;
    _currentCameraPos += _springVelocity * elapsed;

    UpdateCameraView(_currentCameraPos, playerPos);
}
```

Initialize `_currentCameraPos` before the first call or the spring will snap.

---

## Top-down fixed camera

```csharp
// Small Z offset avoids gimbal lock with Vector3.Up
private readonly Vector3 _topDownPos = new Vector3(0f, 25000f, 1f);

void UpdateTopDownFixedCamera()
{
    UpdateCameraView(_topDownPos, Vector3.Zero);
}
```

---

## Top-down tracked camera

```csharp
private readonly Vector3 _topDownOffset = new Vector3(0f, 25000f, 1f);

void UpdateTopDownTrackedCamera(Vector3 playerPos, float playerYaw)
{
    Vector3 camPos = Vector3.Transform(_topDownOffset,
                         Matrix.CreateRotationY(playerYaw)) + playerPos;
    UpdateCameraView(camPos, playerPos);
}
```

---

## Orthographic projection (map view)

Use instead of perspective for pure top-down map cameras — no depth distortion:

```csharp
// In LoadContent (or on viewport resize):
_projection = Matrix.CreateOrthographic(
    GraphicsDevice.Viewport.Width,
    GraphicsDevice.Viewport.Height,
    0.1f,
    100000f
);
```

Combine with `UpdateTopDownFixedCamera()` or `UpdateTopDownTrackedCamera()`.

---

## Camera mode enum + dispatcher

```csharp
public enum CameraMode
{
    Fixed,
    Tracking,
    FirstPerson,
    ThirdPerson,
    TopDownFixed,
    TopDownTracked
}

private CameraMode _mode = CameraMode.Fixed;

// In Update:
float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

switch (_mode)
{
    case CameraMode.Fixed:
        UpdateFixedCamera();
        break;
    case CameraMode.Tracking:
        UpdateTrackingCamera(_modelPos);
        break;
    case CameraMode.FirstPerson:
        UpdateFirstPersonCamera(_modelPos, _modelYaw);
        break;
    case CameraMode.ThirdPerson:
        UpdateThirdPersonCameraSpring(_modelPos, _modelYaw, elapsed);
        break;
    case CameraMode.TopDownFixed:
        UpdateTopDownFixedCamera();
        break;
    case CameraMode.TopDownTracked:
        UpdateTopDownTrackedCamera(_modelPos, _modelYaw);
        break;
}
```
