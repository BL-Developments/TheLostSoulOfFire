# MonoGame ECS Reference

## Table of Contents
1. [GameBehaviour — base class](#gamebehaviour--base-class)
2. [GameEntity](#gameentity)
3. [GameWorld](#gameworld)
4. [Domain interfaces — examples](#domain-interfaces--examples)
5. [Complete example — Player](#complete-example--player)
6. [Object pool — minimal implementation](#object-pool--minimal-implementation)

---

## GameBehaviour — base class

```csharp
/// <summary>Base class for all component logic. Override only what you need.</summary>
public abstract class GameBehaviour
{
    public GameEntity Entity  { get; internal set; } = null!;
    public bool       Enabled { get; set; } = true;

    /// <summary>Called immediately when added to an entity. Cache sibling components here.</summary>
    public virtual void Awake() { }

    /// <summary>Called before the first Update. Use for cross-entity setup.</summary>
    public virtual void Start() { }

    /// <summary>Called every frame. Only invoked if this class overrides it.</summary>
    public virtual void Update(GameTime gt) { }

    /// <summary>Called every draw frame. Only invoked if this class overrides it.</summary>
    public virtual void Draw(SpriteBatch sb) { }

    /// <summary>Called when the entity is destroyed.</summary>
    public virtual void OnDestroy() { }
}
```

---

## GameEntity

```csharp
public sealed class GameEntity
{
    public Guid      Id        { get; } = Guid.NewGuid();
    public string    Name      { get; }
    public bool      Active    { get; set; } = true;
    public GameWorld World     { get; internal set; } = null!;

    /// <summary>Always present. Equivalent to Unity's gameObject.transform.</summary>
    public TransformBehaviour Transform { get; private set; } = null!;

    private readonly Dictionary<Type, GameBehaviour> _behaviours = new();
    private readonly List<GameBehaviour> _updatables = new();
    private readonly List<GameBehaviour> _drawables  = new();
    private bool _started;

    internal GameEntity(string name) => Name = name;

    // ── Fluent builder ────────────────────────────────────────────────────────

    public GameEntity Add<T>(T behaviour) where T : GameBehaviour
    {
        _behaviours[typeof(T)] = behaviour;
        behaviour.Entity = this;

        // Cache Transform reference when that specific behaviour is added
        if (behaviour is TransformBehaviour t) Transform = t;

        // Reflection check — one-time cost at Add(), never repeated per frame.
        var type = behaviour.GetType();
        if (Overrides(type, nameof(GameBehaviour.Update),  typeof(GameTime)))
            _updatables.Add(behaviour);
        if (Overrides(type, nameof(GameBehaviour.Draw),    typeof(SpriteBatch)))
            _drawables.Add(behaviour);

        behaviour.Awake();
        return this;
    }

    // ── Component access ──────────────────────────────────────────────────────

    /// <summary>Returns the component by concrete type or interface. Returns null if not found.</summary>
    public T? GetComponent<T>() where T : class
    {
        // Fast path: exact concrete type
        if (_behaviours.TryGetValue(typeof(T), out var exact))
            return (T)(object)exact;

        // Slow path: search by interface or base type (only when needed)
        return _behaviours.Values.OfType<T>().FirstOrDefault();
    }

    public bool TryGetComponent<T>(out T component) where T : class
    {
        component = GetComponent<T>()!;
        return component is not null;
    }

    public bool HasComponent<T>() where T : class => GetComponent<T>() is not null;

    public IEnumerable<GameBehaviour> GetAllComponents() => _behaviours.Values;

    // ── Internal loop ─────────────────────────────────────────────────────────

    internal void Update(GameTime gt)
    {
        if (!Active) return;
        if (!_started) { StartAll(); _started = true; }

        foreach (var u in _updatables)
            if (u.Enabled) u.Update(gt);
    }

    internal void Draw(SpriteBatch sb)
    {
        if (!Active) return;
        foreach (var d in _drawables)
            if (d.Enabled) d.Draw(sb);
    }

    internal void Destroy()
    {
        foreach (var b in _behaviours.Values) b.OnDestroy();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void StartAll()
    {
        foreach (var b in _behaviours.Values) b.Start();
    }

    private static bool Overrides(Type type, string methodName, params Type[] paramTypes)
    {
        var method = type.GetMethod(
            methodName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public,
            null, paramTypes, null);
        return method?.DeclaringType != typeof(GameBehaviour);
    }
}
```

---

## GameWorld

```csharp
public sealed class GameWorld
{
    private readonly List<GameEntity> _entities  = new();
    private readonly List<GameEntity> _toAdd     = new();
    private readonly List<GameEntity> _toDestroy = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public void Update(GameTime gt)
    {
        FlushPending();
        foreach (var e in _entities) e.Update(gt);
    }

    public void Draw(SpriteBatch sb)
    {
        foreach (var e in _entities) e.Draw(sb);
    }

    // ── Entity management ─────────────────────────────────────────────────────

    /// <summary>
    /// Creates an entity with a pre-attached TransformBehaviour at the given position.
    /// Added to the world at the start of the next Update (deferred).
    /// </summary>
    public GameEntity CreateEntity(string name = "", Vector2 position = default)
    {
        var entity = new GameEntity(name) { World = this };
        entity.Add(new TransformBehaviour(position));
        _toAdd.Add(entity);
        return entity;
    }

    /// <summary>Destroys an entity. Removed at the start of the next Update (deferred).</summary>
    public void Destroy(GameEntity entity)
    {
        if (!_toDestroy.Contains(entity))
            _toDestroy.Add(entity);
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>All entities that have a component of type T (concrete or interface).</summary>
    public IEnumerable<GameEntity> FindEntities<T>() where T : class
        => _entities.Where(e => e.HasComponent<T>());

    /// <summary>All components of type T across all entities.</summary>
    public IEnumerable<T> FindComponents<T>() where T : class
        => _entities.Select(e => e.GetComponent<T>()).OfType<T>();

    /// <summary>First entity with the given name, or null.</summary>
    public GameEntity? FindByName(string name)
        => _entities.FirstOrDefault(e => e.Name == name);

    // ── Internal ──────────────────────────────────────────────────────────────

    private void FlushPending()
    {
        _entities.AddRange(_toAdd);
        _toAdd.Clear();

        foreach (var e in _toDestroy)
        {
            e.Destroy();
            _entities.Remove(e);
        }
        _toDestroy.Clear();
    }
}
```

---

## Domain interfaces — examples

C# interfaces can declare **properties** (get/set) and **methods**, but NOT fields (instance
variables). Fields always live in the concrete class.

```csharp
// Spatial
interface IGravity    { float Scale { get; set; } }
interface ICollidable { Rectangle Bounds { get; } }

// Combat — properties + methods
interface IDamageable
{
    int  MaxHealth     { get; }     // read-only from outside
    int  CurrentHealth { get; }     // read-only from outside
    bool IsDead        { get; }
    void TakeDamage(int amount);
    void Heal(int amount);
}

// Object pooling
interface IPoolable { void Reset(); }
```

### HealthBehaviour — complete documented example

```csharp
/// <summary>
/// Tracks current and max health for any entity (player, enemy, destructible object).
/// Implements IDamageable so other behaviours can interact without knowing the concrete type.
/// </summary>
class HealthBehaviour : GameBehaviour, IDamageable
{
    // ── IDamageable — public contract ────────────────────────────────────────
    public int  MaxHealth     { get; private set; }
    public int  CurrentHealth { get; private set; }
    public bool IsDead        => CurrentHealth <= 0;

    // ── Private state ────────────────────────────────────────────────────────
    // Fields are allowed in classes, not in interfaces
    private bool _invincible;

    // ── Constructor ──────────────────────────────────────────────────────────
    public HealthBehaviour(int maxHealth)
    {
        MaxHealth     = maxHealth;
        CurrentHealth = maxHealth;   // start at full health
    }

    // ── IDamageable methods ──────────────────────────────────────────────────
    public void TakeDamage(int amount)
    {
        if (_invincible || IsDead) return;
        CurrentHealth = Math.Max(CurrentHealth - amount, 0);
    }

    public void Heal(int amount)
        => CurrentHealth = Math.Min(CurrentHealth + amount, MaxHealth);

    // ── Additional helpers (not on the interface) ────────────────────────────
    public void SetInvincible(bool value) => _invincible = value;
    public float Percent => (float)CurrentHealth / MaxHealth;  // 0.0 – 1.0, useful for health bars
}
```

### Wiring "Gato" with health

```csharp
// Create the entity — Transform is auto-attached at given position
var gato = _world.CreateEntity("Gato", new Vector2(200, 300))
                 .Add(new HealthBehaviour(100))
                 .Add(new SpriteBehaviour(gatoTexture));

// Access via interface (loose coupling — doesn't need to know it's HealthBehaviour)
var health = gato.GetComponent<IDamageable>();
health.TakeDamage(25);    // CurrentHealth → 75
health.Heal(10);          // CurrentHealth → 85
Console.WriteLine(health.IsDead);     // false
Console.WriteLine(health.Percent);    // 0.85 — only if you use concrete type

// Access via concrete type when you need HealthBehaviour-specific methods
var hb = gato.GetComponent<HealthBehaviour>();
hb.SetInvincible(true);
hb.Percent;   // 0.85

// Another behaviour reacting to death (in e.g. AIBehaviour.Update):
if (Entity.GetComponent<IDamageable>()?.IsDead == true)
    Entity.World.Destroy(Entity);
```

### Query — find all damageable entities

```csharp
// Deal AOE damage to every entity within range
foreach (var e in _world.FindEntities<IDamageable>())
{
    float dist = Vector2.Distance(e.Transform.Position, explosionPos);
    if (dist < blastRadius)
        e.GetComponent<IDamageable>()!.TakeDamage((int)(100 * (1f - dist / blastRadius)));
}
```

---

## Complete example — Player

```csharp
// ── TransformBehaviour.cs ────────────────────────────────────────────────────
// Pure data — no Update/Draw override, zero per-frame cost.
class TransformBehaviour : GameBehaviour
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public float   Rotation { get; set; }
    public Vector2 Scale    { get; set; } = Vector2.One;

    public TransformBehaviour(Vector2 position = default) => Position = position;
}

// ── SpriteBehaviour.cs ───────────────────────────────────────────────────────
class SpriteBehaviour : GameBehaviour
{
    private readonly Texture2D _texture;

    public SpriteBehaviour(Texture2D texture) => _texture = texture;

    // Uses entity.Transform directly — no Awake needed
    public override void Draw(SpriteBatch sb)
        => sb.Draw(_texture, Entity.Transform.Position, Color.White);
}

// ── GravityBehaviour.cs ──────────────────────────────────────────────────────
class GravityBehaviour : GameBehaviour, IGravity
{
    public float Scale { get; set; } = 1f;

    public override void Update(GameTime gt)
    {
        float dt = (float)gt.ElapsedGameTime.TotalSeconds;
        Entity.Transform.Velocity += Vector2.UnitY * Scale * 980f * dt;
        Entity.Transform.Position += Entity.Transform.Velocity * dt;
    }
}

// ── PlayerInputBehaviour.cs ──────────────────────────────────────────────────
class PlayerInputBehaviour : GameBehaviour
{
    private KeyboardState _prev, _curr;
    private const float Speed = 200f;

    public override void Update(GameTime gt)
    {
        _prev = _curr;
        _curr = Keyboard.GetState();

        var move = Vector2.Zero;
        if (_curr.IsKeyDown(Keys.Left))  move.X -= 1f;
        if (_curr.IsKeyDown(Keys.Right)) move.X += 1f;
        if (_curr.IsKeyDown(Keys.Up))    move.Y -= 1f;

        Entity.Transform.Velocity = move * Speed;
    }
}

// ── Game1.cs (wiring) ────────────────────────────────────────────────────────
class Game1 : Game
{
    private GameWorld   _world = new();
    private SpriteBatch _spriteBatch;

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        var tex = Content.Load<Texture2D>("player");

        // Transform is auto-created at position (100, 200) — no manual Add needed
        _world.CreateEntity("Player", new Vector2(100, 200))
              .Add(new SpriteBehaviour(tex))
              .Add(new GravityBehaviour { Scale = 1f })
              .Add(new PlayerInputBehaviour());
    }

    protected override void Update(GameTime gt) { _world.Update(gt); base.Update(gt); }
    protected override void Draw(GameTime gt)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _spriteBatch.Begin();
        _world.Draw(_spriteBatch);
        _spriteBatch.End();
        base.Draw(gt);
    }
}
```

---

## Object pool — minimal implementation

```csharp
public class GameEntityPool<T> where T : GameBehaviour, IPoolable, new()
{
    private readonly Stack<GameEntity> _pool = new();
    private readonly GameWorld _world;
    private readonly string _name;

    public GameEntityPool(GameWorld world, string name, int prewarm = 0)
    {
        _world = world;
        _name  = name;
        for (int i = 0; i < prewarm; i++)
            _pool.Push(CreateNew());
    }

    public GameEntity Get(Action<GameEntity>? configure = null)
    {
        var entity = _pool.Count > 0 ? _pool.Pop() : CreateNew();
        entity.GetComponent<T>()!.Reset();
        entity.Active = true;
        configure?.Invoke(entity);
        return entity;
    }

    public void Return(GameEntity entity)
    {
        entity.Active = false;
        _pool.Push(entity);
    }

    private GameEntity CreateNew()
    {
        var e = _world.CreateEntity(_name);
        e.Add(new T());
        return e;
    }
}

// Usage:
var bulletPool = new GameEntityPool<BulletBehaviour>(_world, "Bullet", prewarm: 50);

// Spawn:
var bullet = bulletPool.Get(e =>
{
    e.GetComponent<BulletBehaviour>()!.Direction = shootDir;
    e.GetComponent<TransformBehaviour>()!.Position = muzzlePos;
});

// When bullet expires (in BulletBehaviour.Update):
if (_lifetime > MaxLifetime)
    _pool.Return(Entity);   // store pool reference in behaviour's Awake
```
