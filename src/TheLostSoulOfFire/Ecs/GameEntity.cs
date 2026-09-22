using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Ecs;

public sealed class GameEntity
{
    private readonly List<GameBehaviour> _behaviours = new(8);
    private readonly List<GameBehaviour> _updateBehaviours = new(6);
    private readonly List<GameBehaviour> _drawBehaviours = new(4);
    private bool _started;

    internal GameEntity(GameWorld world, string name, Vector2 position)
    {
        World = world;
        Name = name;
        Id = Guid.NewGuid();
        Transform = new TransformBehaviour(position);
        Add(Transform);
    }

    public Guid Id { get; }
    public string Name { get; }
    public GameWorld World { get; }
    public TransformBehaviour Transform { get; }
    public bool Active { get; set; } = true;

    public GameEntity Add(GameBehaviour behaviour)
    {
        ArgumentNullException.ThrowIfNull(behaviour);
        behaviour.Entity = this;
        _behaviours.Add(behaviour);
        Type type = behaviour.GetType();
        if (Overrides(type, nameof(GameBehaviour.Update), typeof(GameTime))) _updateBehaviours.Add(behaviour);
        if (Overrides(type, nameof(GameBehaviour.Draw), typeof(GameTime), typeof(SpriteBatch))) _drawBehaviours.Add(behaviour);
        behaviour.Awake();
        return this;
    }

    public T? GetComponent<T>() where T : class
    {
        for (int i = 0; i < _behaviours.Count; i++)
        {
            if (_behaviours[i] is T component) return component;
        }
        return null;
    }

    public bool HasComponent<T>() where T : class => GetComponent<T>() is not null;

    internal void Start()
    {
        if (_started) return;
        for (int i = 0; i < _behaviours.Count; i++) _behaviours[i].Start();
        _started = true;
    }

    internal void Update(GameTime gameTime)
    {
        if (!Active) return;
        for (int i = 0; i < _updateBehaviours.Count; i++)
        {
            GameBehaviour behaviour = _updateBehaviours[i];
            if (behaviour.Enabled) behaviour.Update(gameTime);
        }
    }

    internal void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (!Active) return;
        for (int i = 0; i < _drawBehaviours.Count; i++)
        {
            GameBehaviour behaviour = _drawBehaviours[i];
            if (behaviour.Enabled) behaviour.Draw(gameTime, spriteBatch);
        }
    }

    internal void Destroy()
    {
        for (int i = 0; i < _behaviours.Count; i++) _behaviours[i].OnDestroy();
        Active = false;
    }

    private static bool Overrides(Type type, string methodName, params Type[] parameters)
    {
        MethodInfo? method = type.GetMethod(methodName, parameters);
        return method is not null && method.DeclaringType != typeof(GameBehaviour);
    }
}
