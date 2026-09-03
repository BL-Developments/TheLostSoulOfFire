# Technical Architecture

**DESIGN STATUS: LOCKED**

## Stack

Current project stack:

- C#
- .NET 9
- MonoGame
- DesktopGL
- MonoGame Content Pipeline

Preserve the existing project unless a change is strictly required.

## Primary Rule

> Implement the simplest solution that completely satisfies the specification.

And:

> Simplify implementation, not design.

Do not change locked design decisions because an alternative is easier to code.

## Architecture Style

Prefer:

- concrete classes
- composition
- small inheritance where obviously useful
- enum state machines
- primitive collision
- centralized tunable balance values

Avoid speculative abstractions.

## Suggested Runtime Structure

```text
Game1
├── GameWorld
├── Camera2D
├── InputState
├── ScreenEffects / VFX
└── Audio
```

`Game1.Update()` and `Game1.Draw()` should delegate rather than contain the whole game.

## GameWorld

May own:

- Player
- Enemies
- Souls
- Projectiles
- Particles
- Arena
- WaveManager

Responsibilities may include:

- update
- collision coordination
- spawning
- cleanup
- restart/reset

## Player

A concrete `Player` is sufficient.

Expected state includes:

- Position
- Velocity
- Health
- FacingDirection
- Combo state
- Dash state
- Cannon state
- SoulSenseActive
- Resonance
- ResonanceActive

Separate `ScytheCombat` and `SoulCannon` classes are allowed if they make the code simpler.

Do not create a generic ability framework.

## Enemies

A small shared base is appropriate:

```text
Enemy
├── Hollow
├── Burning
└── Devourer
```

Common concerns:

- position
- velocity
- health
- hitbox
- death
- update
- damage

Concrete behavior belongs directly in concrete enemy classes.

No behavior trees.

## State Machines

Small enum-based state machines are encouraged.

Example Burning:

- Idle
- Approach
- Telegraph
- Charge
- Recovery
- Dead

Example Devourer:

- ApproachPlayer
- ApproachSoul
- Attack
- Devour
- Staggered
- Dead

Example Cannon:

- Stored
- Drawing
- Charging
- Firing
- Returning

## Combat Data

A small structure such as:

```csharp
DamageInfo
{
    Damage
    Knockback
    HitPosition
    IsSoulCoreHit
}
```

is appropriate.

Exact fields may vary.

Do not build a generic combat event pipeline.

## Collision

Use:

- circles
- rectangles
- distance checks
- angle checks

Scythe arcs may be approximated with range + angle.

Cannon may use simple projectile or beam collision.

Do not use:

- pixel-perfect collision
- complex polygon collision
- full physics engine

## Soul Runtime

Recommended data:

- Position
- State
- ReleaseTimer
- SourceEnemy if useful

Recommended states:

- Exposed
- BeingDevoured
- Releasing
- Released
- Consumed

Keep it simple.

## Resonance Runtime

Player may own:

- current Resonance
- max Resonance
- active flag
- timer

Do not build a generic buff system.

## VFX

A simple `ParticleSystem` is enough.

Particle data may include:

- Position
- Velocity
- Lifetime
- Scale
- Rotation
- optional texture/type

Use it for:

- Death Flame
- Soul Release
- Dash
- Burning
- Cannon
- Resonance

No VFX graph.

## Screen Effects

A small centralized `ScreenEffects` service/class is encouraged for:

- Hitstop
- CameraShake
- ScreenFlash
- Vignette
- SoulSenseOverlay
- ImpactFrame

## Balance

Create one central location such as `GameBalance.cs`.

Include tunable values such as:

- PlayerMoveSpeed
- DashDistance
- DashCooldown
- ScytheDamage1/2/3
- CannonChargeTime
- HollowHealth
- BurningHealth
- DevourerHealth
- SoulReleaseDelay
- ResonanceRequired
- ResonanceDuration

This is strongly encouraged.

## Input

A small `InputState` helper should distinguish:

- Down
- Pressed
- Released

Keyboard and mouse only are required.

## Assets

Suggested organization:

```text
Content/
├── Textures/
│   ├── Player/
│   ├── Enemies/
│   ├── Environment/
│   └── Effects/
├── Audio/
│   ├── Music/
│   └── Sfx/
└── Fonts/
```

Do not block on missing final assets.

## Debugging

Development keys:

- F1 — Toggle debug overlay
- F2 — Spawn Hollow
- F3 — Spawn Burning
- F4 — Spawn Devourer
- F5 — Fill Resonance
- F6 — Kill all enemies
- F7 — Toggle/force Soul Sense
- F8 — Reset arena

Overlay minimum:

- FPS
- Player HP
- Resonance
- current Wave
- enemy count
- Soul count
- Player state

If easy, also show:

- hitboxes
- Soul Cores
- enemy state
- enemy target

## Logging

Minimal logging only.

Useful events:

- Wave started
- Enemy spawned
- Enemy died
- Soul released
- Soul consumed
- Resonance activated
- Player died

`Debug.WriteLine` or a tiny helper is enough.

## Tests

Do not build a large test architecture for rendering.

Pure logic tests are welcome when cheap, especially:

- Resonance calculations
- wave progression
- Soul state transitions

Primary verification remains:

1. restore
2. build
3. launch
4. play the loop

## Performance

Target stable 60 FPS on a normal desktop.

Do not prematurely optimize.

Do not build object-pooling infrastructure unless actual profiling/runtime behavior demonstrates a need.

## Forbidden Overengineering

Do not introduce:

- ECS
- behavior trees
- dependency-injection infrastructure
- generic repositories
- generic ability framework
- strategy/factory hierarchies for three enemies
- event-bus architecture
- architecture for hypothetical future multiplayer/content/modding

## Suggested Layout

```text
TheLostSoulOfFire/
├── Game1.cs
├── Game/
│   ├── GameWorld.cs
│   ├── GameBalance.cs
│   ├── WaveManager.cs
│   └── Arena.cs
├── Entities/
│   ├── Player.cs
│   ├── Enemy.cs
│   ├── Hollow.cs
│   ├── Burning.cs
│   ├── Devourer.cs
│   └── Soul.cs
├── Combat/
│   ├── DamageInfo.cs
│   ├── ScytheCombat.cs
│   └── SoulCannon.cs
├── Effects/
│   ├── ParticleSystem.cs
│   └── ScreenEffects.cs
├── Input/
│   └── InputState.cs
├── Rendering/
│   └── Camera2D.cs
├── Audio/
│   └── AudioManager.cs
└── Content/
```

This layout is guidance, not folder religion. Deviate when a simpler implementation is clearer.
