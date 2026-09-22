using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Core;
using TheLostSoulOfFire.Levels;

namespace TheLostSoulOfFire.Gameplay;

public sealed class ProjectilePool
{
    private static readonly Point ProjectileSize = new(22, 14);
    private readonly FireProjectile[] _projectiles;
    private readonly Action<EnemyActor, Vector2> _onEnemyHit;
    private Rectangle _source;
    private Vector2 _origin;
    private Vector2 _scale;

    public ProjectilePool(int capacity, Action<EnemyActor, Vector2> onEnemyHit)
    {
        _projectiles = new FireProjectile[capacity];
        for (int i = 0; i < capacity; i++) _projectiles[i] = new FireProjectile();
        _onEnemyHit = onEnemyHit;
    }

    public int ActiveCount { get; private set; }

    public void InitializeRenderingData()
    {
        _source = GameCore.Assets.Region(AtlasRegion.Projectile);
        _origin.X = _source.Width * 0.5f;
        _origin.Y = _source.Height * 0.5f;
        _scale.X = ProjectileSize.X / (float)_source.Width;
        _scale.Y = ProjectileSize.Y / (float)_source.Height;
    }

    public bool TrySpawn(Vector2 position, Vector2 direction)
    {
        if (direction.LengthSquared() < 0.001f) return false;
        direction.Normalize();
        for (int i = 0; i < _projectiles.Length; i++)
        {
            FireProjectile projectile = _projectiles[i];
            if (projectile.Active) continue;
            projectile.Activate(position, direction * 520f, 1.8f);
            ActiveCount++;
            return true;
        }
        return false;
    }

    public void Update(GameTime gameTime, LevelRuntime level, IReadOnlyList<EnemyActor> enemies)
    {
        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        for (int i = 0; i < _projectiles.Length; i++)
        {
            FireProjectile projectile = _projectiles[i];
            if (!projectile.Active) continue;
            projectile.Update(delta);
            if (!level.Bounds.Contains(projectile.Bounds) || IntersectsWall(projectile.Bounds, level.Walls))
            {
                Deactivate(projectile);
                continue;
            }

            for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
            {
                EnemyActor enemy = enemies[enemyIndex];
                if (enemy.Defeated || !enemy.Entity.Active || !projectile.Bounds.Intersects(enemy.Hitbox.Bounds)) continue;
                Vector2 hitPosition = projectile.Position;
                Deactivate(projectile);
                _onEnemyHit(enemy, hitPosition);
                break;
            }

            if (projectile.Active && projectile.Lifetime <= 0f) Deactivate(projectile);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        for (int i = 0; i < _projectiles.Length; i++)
        {
            FireProjectile projectile = _projectiles[i];
            if (!projectile.Active) continue;
            float rotation = MathF.Atan2(projectile.Velocity.Y, projectile.Velocity.X);
            spriteBatch.Draw(GameCore.Assets.Atlas, projectile.Position, _source, Color.White, rotation, _origin, _scale, SpriteEffects.None, 0.35f);
        }
    }

    private void Deactivate(FireProjectile projectile)
    {
        if (!projectile.Active) return;
        projectile.Active = false;
        ActiveCount--;
    }

    private static bool IntersectsWall(Rectangle bounds, ReadOnlySpan<Rectangle> walls)
    {
        for (int i = 0; i < walls.Length; i++) if (bounds.Intersects(walls[i])) return true;
        return false;
    }

    private sealed class FireProjectile
    {
        public bool Active;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Lifetime;
        public Rectangle Bounds => CollisionMath.BoundsAt(Position, ProjectileSize);

        public void Activate(Vector2 position, Vector2 velocity, float lifetime)
        {
            Position = position;
            Velocity = velocity;
            Lifetime = lifetime;
            Active = true;
        }

        public void Update(float delta)
        {
            Position.X += Velocity.X * delta;
            Position.Y += Velocity.Y * delta;
            Lifetime -= delta;
        }
    }
}
