# MonoGame.Extended Particle System Reference

API signatures and ready-to-paste C# code for `ParticleEffect` and `ParticleEmitter`.

## Table of Contents
1. [Namespaces](#namespaces)
2. [Texture setup](#texture-setup)
3. [Frame loop](#frame-loop)
4. [Example: fire (continuous, spray, fade out)](#example-fire)
5. [Example: explosion (manual burst, outward circle, drag)](#example-explosion)
6. [Example: sparkle (every frame, hue cycle, no gravity)](#example-sparkle)
7. [Example: burst at collision point from a GameBehaviour](#example-burst-at-collision-point)
8. [Emission profiles API](#emission-profiles-api)
9. [Modifiers API](#modifiers-api)
10. [Interpolators API](#interpolators-api)
11. [ParticleReleaseParameters API](#particlereleaseparameters-api)

---

## Namespaces

```csharp
using MonoGame.Extended;                                          // HslColor
using MonoGame.Extended.Graphics;                                  // Texture2DRegion
using MonoGame.Extended.Particles;
using MonoGame.Extended.Particles.Data;                           // ParticleReleaseParameters, ParticleFloatParameter, ParticleColorParameter
using MonoGame.Extended.Particles.Profiles;
using MonoGame.Extended.Particles.Modifiers;
using MonoGame.Extended.Particles.Modifiers.Interpolators;
```

---

## Texture setup

A 1×1 white pixel texture works for all basic effects. For higher-quality effects,
load a soft circle or glow sprite from Content.

```csharp
// Minimal 1x1 white pixel (no content file needed)
_particleTexture = new Texture2D(GraphicsDevice, 1, 1);
_particleTexture.SetData(new[] { Color.White });

// Content pipeline sprite
_particleTexture = Content.Load<Texture2D>("Particles/spark");

// Both are wrapped in Texture2DRegion before passing to emitter
var region = new Texture2DRegion(_particleTexture);
```

---

## Frame loop

```csharp
// In Update
_particleEffect.Update(gameTime);

// In Draw — choose BlendState to match the visual type
_spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
_spriteBatch.Draw(_particleEffect);
_spriteBatch.End();
```

For smoke/dust use `BlendState.AlphaBlend` instead of Additive.

---

## Example: fire

Continuous upward spray with fade-out. Particles rise and disappear.

```csharp
private ParticleEffect _fireEffect;

protected override void LoadContent()
{
    var texture = new Texture2D(GraphicsDevice, 1, 1);
    texture.SetData(new[] { Color.White });

    _fireEffect = new ParticleEffect("Fire")
    {
        Position           = new Vector2(400, 500),
        AutoTrigger        = true,
        AutoTriggerFrequency = 0.05f  // emit every 50ms
    };

    var emitter = new ParticleEmitter(1500)
    {
        LifeSpan     = 2.0f,
        TextureRegion = new Texture2DRegion(texture),
        Profile      = Profile.Spray(-Vector2.UnitY, spread: 2.0f),
        Parameters   = new ParticleReleaseParameters
        {
            Quantity = new ParticleInt32Parameter(8, 16),
            Speed    = new ParticleFloatParameter(15f, 50f),
            Color    = new ParticleColorParameter(new Vector3(0f, 1f, 0.6f)),  // red HSL
            Scale    = new ParticleVector2Parameter(new Vector2(8f, 8f))
        }
    };

    emitter.Modifiers.Add(new LinearGravityModifier
    {
        Direction = -Vector2.UnitY,   // push upward
        Strength  = 80f
    });
    emitter.Modifiers.Add(new AgeModifier
    {
        Interpolators =
        {
            new OpacityInterpolator { StartValue = 1.0f, EndValue = 0.0f }
        }
    });

    _fireEffect.Emitters.Add(emitter);
}
```

---

## Example: explosion

Manual burst — fires once when `Trigger()` is called. Particles scatter outward,
fall under gravity, and slow due to drag.

```csharp
private ParticleEffect _explosionEffect;

protected override void LoadContent()
{
    var texture = new Texture2D(GraphicsDevice, 1, 1);
    texture.SetData(new[] { Color.White });

    _explosionEffect = new ParticleEffect("Explosion")
    {
        AutoTrigger = false   // manual trigger only
    };

    var emitter = new ParticleEmitter(2000)
    {
        LifeSpan     = 2.0f,
        TextureRegion = new Texture2DRegion(texture),
        Profile      = Profile.Circle(20, CircleRadiation.Out),   // outward burst
        Parameters   = new ParticleReleaseParameters
        {
            Quantity = new ParticleInt32Parameter(60, 100),
            Speed    = new ParticleFloatParameter(100f, 250f),
            Color    = new ParticleColorParameter(new Vector3(0f, 1f, 0.6f)),
            Scale    = new ParticleVector2Parameter(new Vector2(10f, 10f))
        }
    };

    emitter.Modifiers.Add(new LinearGravityModifier
    {
        Direction = Vector2.UnitY,    // pull down
        Strength  = 200f
    });
    emitter.Modifiers.Add(new DragModifier
    {
        Density          = 0.5f,
        DragCoefficient  = 0.3f
    });
    emitter.Modifiers.Add(new AgeModifier
    {
        Interpolators =
        {
            new OpacityInterpolator { StartValue = 1.0f, EndValue = 0.0f }
        }
    });

    _explosionEffect.Emitters.Add(emitter);
}

// Trigger at a specific world position:
private void PlayExplosionAt(Vector2 worldPosition)
{
    _explosionEffect.Position = worldPosition;
    _explosionEffect.Trigger();
}
```

---

## Example: sparkle

Every-frame emission. Particles shimmer through the hue spectrum and vanish.

```csharp
private ParticleEffect _sparkleEffect;

protected override void LoadContent()
{
    var texture = new Texture2D(GraphicsDevice, 1, 1);
    texture.SetData(new[] { Color.White });

    _sparkleEffect = new ParticleEffect("Sparkle")
    {
        Position             = new Vector2(400, 300),
        AutoTrigger          = true,
        AutoTriggerFrequency = 0.0f   // emit every frame
    };

    var emitter = new ParticleEmitter(2000)
    {
        LifeSpan     = 0.5f,
        TextureRegion = new Texture2DRegion(texture),
        Profile      = Profile.Circle(200, CircleRadiation.Out),
        Parameters   = new ParticleReleaseParameters
        {
            Quantity = new ParticleInt32Parameter(10, 20),
            Speed    = new ParticleFloatParameter(10f, 40f),
            Color    = new ParticleColorParameter(
                new Vector3(252f, 1f, 0.8f),   // light purple
                new Vector3(180f, 1f, 0.5f)),  // cyan
            Scale    = new ParticleVector2Parameter(new Vector2(5f, 5f))
        }
    };

    // Cycle hue over particle lifetime — no LinearGravityModifier for floating look
    emitter.Modifiers.Add(new AgeModifier
    {
        Interpolators =
        {
            new HueInterpolator { StartValue = 0f, EndValue = 360f }
        }
    });

    _sparkleEffect.Emitters.Add(emitter);
}
```

---

## Example: burst at collision point from a GameBehaviour

Integrates with the `monogame-ecs` skill's `GameBehaviour`/`GameEntity` system.

```csharp
// In a GameBehaviour that owns the visual hit effect
public class ImpactFxBehaviour : GameBehaviour
{
    private ParticleEffect _sparkEffect;
    private SpriteBatch    _spriteBatch;

    public void Initialize(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
    {
        _spriteBatch = spriteBatch;

        var texture = new Texture2D(graphicsDevice, 1, 1);
        texture.SetData(new[] { Color.White });

        _sparkEffect = new ParticleEffect("ImpactSpark") { AutoTrigger = false };

        var emitter = new ParticleEmitter(500)
        {
            LifeSpan      = 0.6f,
            TextureRegion = new Texture2DRegion(texture),
            Profile       = Profile.Circle(5, CircleRadiation.Out),
            Parameters    = new ParticleReleaseParameters
            {
                Quantity = new ParticleInt32Parameter(20, 40),
                Speed    = new ParticleFloatParameter(60f, 150f),
                Color    = new ParticleColorParameter(new Vector3(45f, 1f, 0.8f)), // yellow
                Scale    = new ParticleVector2Parameter(new Vector2(6f, 6f))
            }
        };
        emitter.Modifiers.Add(new LinearGravityModifier { Direction = Vector2.UnitY, Strength = 150f });
        emitter.Modifiers.Add(new DragModifier { Density = 0.5f, DragCoefficient = 0.4f });
        emitter.Modifiers.Add(new AgeModifier
        {
            Interpolators = { new OpacityInterpolator { StartValue = 1f, EndValue = 0f } }
        });

        _sparkEffect.Emitters.Add(emitter);
    }

    // Call this from your collision resolution code
    public void TriggerAt(Vector2 worldPosition)
    {
        _sparkEffect.Position = worldPosition;
        _sparkEffect.Trigger();
    }

    public override void Update(GameTime gameTime)
    {
        _sparkEffect.Update(gameTime);
    }

    public void Draw()
    {
        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
        _spriteBatch.Draw(_sparkEffect);
        _spriteBatch.End();
    }
}
```

---

## Emission profiles API

```csharp
Profile.Spray(direction: Vector2, spread: float)
Profile.Point()
Profile.Circle(radius: float, radiation: CircleRadiation)
    // CircleRadiation.Out, CircleRadiation.In, CircleRadiation.None
Profile.Ring(radius: float)
Profile.Line(axis: Vector2, length: float)
Profile.Box(width: float, height: float)
Profile.BoxUniform(width: float, height: float)
Profile.BoxFill(width: float, height: float)
```

---

## Modifiers API

```csharp
new LinearGravityModifier { Direction = Vector2, Strength = float }
new DragModifier          { Density = float, DragCoefficient = float }
new AgeModifier           { Interpolators = { ... } }
new RotationModifier      { RotationRate = float }   // radians per second
new VelocityModifier      { VelocityScale = float }
new VortexModifier        { Position = Vector2, Mass = float, MaxSpeed = float }
// Container modifiers (keep particles inside a shape):
new CircleContainerModifier    { Radius = float, Inside = bool }
new RectangleContainerModifier { Width = float, Height = float }
```

---

## Interpolators API

Used inside `AgeModifier.Interpolators`:

```csharp
new OpacityInterpolator  { StartValue = float, EndValue = float }
new ScaleInterpolator    { StartValue = Vector2, EndValue = Vector2 }
new RotationInterpolator { StartValue = float, EndValue = float }
new ColorInterpolator    { StartValue = HslColor, EndValue = HslColor }  // use HslColor.FromRgb(Color) to convert
new HueInterpolator      { StartValue = float, EndValue = float }      // 0–360
new VelocityInterpolator { StartValue = float, EndValue = float }
```

---

## ParticleReleaseParameters API

```csharp
new ParticleReleaseParameters
{
    Quantity = new ParticleInt32Parameter(min, max),        // particles per trigger
    Speed    = new ParticleFloatParameter(min, max),        // initial speed
    Color    = new ParticleColorParameter(hslVec3),         // single color
    // or
    Color    = new ParticleColorParameter(hslMin, hslMax),  // random range
    Scale    = new ParticleVector2Parameter(fixed),         // fixed size
    // or
    Scale    = new ParticleVector2Parameter(min, max),      // random size range
    Rotation = new ParticleFloatParameter(min, max),        // initial rotation radians
    Opacity  = new ParticleFloatParameter(min, max)         // initial opacity 0–1
}
```
