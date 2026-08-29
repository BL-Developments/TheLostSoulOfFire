using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Ecs;

public sealed class GameWorld : IDisposable
{
    private readonly List<GameEntity> _entities = new(64);
    private readonly List<GameEntity> _pendingAdd = new(16);
    private readonly List<GameEntity> _pendingDestroy = new(16);

    public IReadOnlyList<GameEntity> Entities => _entities;

    public GameEntity CreateEntity(string name, Vector2 position)
    {
        GameEntity entity = new(this, name, position);
        _pendingAdd.Add(entity);
        return entity;
    }

    public void Destroy(GameEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (!entity.Active || _pendingDestroy.Contains(entity)) return;
        entity.Active = false;
        _pendingDestroy.Add(entity);
    }

    public void Update(GameTime gameTime)
    {
        FlushChanges();
        for (int i = 0; i < _entities.Count; i++) _entities[i].Update(gameTime);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        for (int i = 0; i < _entities.Count; i++) _entities[i].Draw(gameTime, spriteBatch);
    }

    public void FlushChanges()
    {
        for (int i = 0; i < _pendingDestroy.Count; i++)
        {
            GameEntity entity = _pendingDestroy[i];
            entity.Destroy();
            _entities.Remove(entity);
            _pendingAdd.Remove(entity);
        }
        _pendingDestroy.Clear();

        for (int i = 0; i < _pendingAdd.Count; i++)
        {
            GameEntity entity = _pendingAdd[i];
            if (!entity.Active) continue;
            _entities.Add(entity);
            entity.Start();
        }
        _pendingAdd.Clear();
    }

    public void Dispose()
    {
        for (int i = 0; i < _entities.Count; i++) _entities[i].Destroy();
        for (int i = 0; i < _pendingAdd.Count; i++) _pendingAdd[i].Destroy();
        _entities.Clear();
        _pendingAdd.Clear();
        _pendingDestroy.Clear();
    }
}
