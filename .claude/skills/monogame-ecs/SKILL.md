---
name: monogame-ecs
description: >
  MonoGame component system (ECS) guide using GameBehaviour / GameEntity / GameWorld.
  Use this skill whenever the user mentions entities, components, behaviours, game objects,
  spawning/destroying actors, querying entities by type or interface, or asks how to structure
  game logic in MonoGame without deep class inheritance.
---

# MonoGame Component System (ECS)

This skill covers the custom lightweight component system for MonoGame projects.
The naming and mental model deliberately mirror Unity:

| This system  | Unity equivalent  |
|--------------|-------------------|
| `GameEntity` | `GameObject`      |
| `GameBehaviour` | `MonoBehaviour` |
| `GameWorld`  | `Scene`           |
| `entity.GetComponent<T>()` | `gameObject.GetComponent<T>()` |

For the full API reference and complete implementation code see `references/ecs.md`.

---

## Core Concepts

**GameBehaviour** is the base class for every piece of logic. Override only the lifecycle
hooks you need — the engine only calls the ones a behaviour actually overrides (see
Performance below).

```csharp
class GravityBehaviour : GameBehaviour, IGravity
{
    public float Scale { get; set; } = 1f;
    private TransformBehaviour _transform;

    // Awake: resolve sibling dependencies — like Unity's Awake()
    public override void Awake()
        => _transform = Entity.GetComponent<TransformBehaviour>();

    public override void Update(GameTime gt)
    {
        float dt = (float)gt.ElapsedGameTime.TotalSeconds;
        _transform.Velocity += Vector2.UnitY * Scale * 980f * dt;
    }
}
```

**GameEntity** is a named container of GameBehaviours. Every entity is born with a `Guid Id`
and a `TransformBehaviour` pre-attached — accessible as `entity.Transform`, exactly like
Unity's `gameObject.transform`. You never add Transform manually:

```csharp
_world.CreateEntity("Player", new Vector2(100, 200))
      .Add(new SpriteBehaviour(texture))
      .Add(new GravityBehaviour { Scale = 1f })
      .Add(new PlayerInputBehaviour());

// Direct access — no GetComponent<> needed:
entity.Transform.Position += Vector2.UnitX * speed;
Console.WriteLine(entity.Id);   // Guid
Console.WriteLine(entity.Name); // "Player"
```

**GameWorld** owns all entities, drives the loop, and handles safe mid-frame
creation/destruction:

```csharp
// In Game1.Update():
_world.Update(gameTime);

// In Game1.Draw():
_world.Draw(gameTime, _spriteBatch);
```

---

## Lifecycle Order

```
Add()       → Awake()          (immediately, synchronous)
              ↓
First frame → Start()          (before first Update, after all Awakes in scene)
              ↓
Every frame → Update(gt)       (only if behaviour overrides it)
              ↓
Every frame → Draw(gameTime, sb)  (only if behaviour overrides it)
              ↓
Destroy()   → OnDestroy()      (deferred to end of frame)
```

Use **Awake** to cache sibling component references.
Use **Start** for logic that depends on all entities/behaviours being initialized.

---

## Domain Interfaces

Define capability contracts as interfaces for queries and loose coupling —
never for logic or inheritance chains. The interfaces below (`IGravity`, `ICollidable`, `IDamageable`) are **illustrative examples** — they are not part of the library. Define your own project interfaces.

```csharp
interface IGravity    { float Scale { get; set; } }
interface ICollidable { Rectangle Bounds { get; } }
interface IDamageable { int Health { get; } void TakeDamage(int amount); }
interface IPoolable   { void Reset(); }
```

`GetComponent<T>()` and `HasComponent<T>()` work with both concrete types and interfaces:

```csharp
// Concrete:
var transform = entity.GetComponent<TransformBehaviour>();

// Interface — works even when you don't know the concrete type:
var damageable = entity.GetComponent<IDamageable>();
damageable?.TakeDamage(10);

// Query all entities with a given capability:
foreach (var e in _world.FindEntities<ICollidable>())
    ResolveCollision(e);
```

---

## Inter-Component Communication

Components talk to siblings through the entity, not through direct references or statics:

```csharp
// GOOD — resolved once in Awake, used every frame
class AttackBehaviour : GameBehaviour
{
    private IDamageable _target;

    public override void Awake()
        => _target = Entity.GetComponent<IDamageable>();

    public override void Update(GameTime gt)
    {
        if (ShouldAttack())
            _target?.TakeDamage(10);
    }
}
```

---

## Enable / Disable

Individual behaviours and entire entities can be toggled at runtime:

```csharp
entity.GetComponent<GravityBehaviour>().Enabled = false;  // pause gravity
entity.Active = false;                                     // freeze whole entity
```

Disabled behaviours are skipped in Update/Draw. Inactive entities skip everything.

---

## Creating and Destroying Entities

Creation and destruction are always **deferred to the start of the next Update** — safe
to call from inside Update or collision callbacks:

```csharp
// Spawn a bullet from inside another behaviour's Update():
var bullet = Entity.World.CreateEntity("Bullet")
                   .Add(new TransformBehaviour(pos))
                   .Add(new BulletBehaviour(direction, speed));

// Destroy when expired (deferred — entity finishes this frame normally):
Entity.World.Destroy(Entity);
```

---

## Performance

### Separate update/draw lists

`GameEntity` uses reflection **once at Add time** to detect whether a behaviour overrides
`Update` or `Draw`. Only those behaviours land in the hot-path lists. A component that
only has data (e.g. `TransformBehaviour`, `HealthBehaviour`) costs zero per frame.

```
1,000 entities × 3 behaviours = 3,000 Add() checks  (one-time, at spawn)
                                = 0–3,000 Update() calls/frame  (only overrides)
```

### Object Pooling for Frequent Spawns

For entities that spawn and die constantly (bullets, particles, enemies), allocating on
the heap each time will pressure the GC. Implement `IPoolable` on the behaviour and
maintain a pool in the spawning system:

```csharp
// Mark poolable behaviours with the interface
class BulletBehaviour : GameBehaviour, IPoolable
{
    public void Reset()  // called when retrieved from pool
    {
        Enabled = true;
        _lifetime = 0f;
    }
}

// Simple pool — reuse instead of new/Destroy
var bullet = _bulletPool.Get();    // Reset() called internally
// ...
_bulletPool.Return(bullet);        // instead of World.Destroy()
```

See `references/ecs.md` for a minimal generic pool implementation.

---

## Why GameEntity is sealed

`GameEntity` is a dumb container — it has no game logic and should never have any.
Customization always goes in behaviours, not in subclasses:

```csharp
// ❌ Never do this
class EnemyEntity : GameEntity { public int Health { get; set; } }

// ✅ Compose instead
world.CreateEntity("Enemy")
     .Add(new HealthBehaviour(100))
     .Add(new AIBehaviour())
     .Add(new SpriteBehaviour(tex));
```

The payoff: behaviours can be added/removed at runtime, shared across entity types, and
tested in isolation. A `HealthBehaviour` works on a player, an enemy, or a destructible
crate — same code, no inheritance.

---

## Rules

- Cache `GetComponent<T>()` calls in `Awake()` — never call them inside `Update()`.
- `Awake` resolves **own** siblings. `Start` resolves **cross-entity** dependencies.
- Never call `World.Destroy()` on an entity and then read its state the same frame —
  treat it as dead immediately after the call.
- Keep GameBehaviours focused: one responsibility per class. Prefer small behaviours
  composed together over large monolithic ones.
- Domain interfaces (`IGravity`, `ICollidable`…) define *what*, not *how*. Define your own — the library does not ship these.
- Use `entity.Active = false` to pause temporarily; use `World.Destroy()` to remove
  permanently.
