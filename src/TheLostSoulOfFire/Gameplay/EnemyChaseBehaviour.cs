using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Ecs;
using TheLostSoulOfFire.Levels;

namespace TheLostSoulOfFire.Gameplay;

public sealed class EnemyChaseBehaviour : GameBehaviour
{
    private readonly TransformBehaviour _target;
    private readonly LevelRuntime _level;
    private readonly float _speed;
    private TransformBehaviour _transform = null!;
    private HitboxBehaviour _hitbox = null!;

    public EnemyChaseBehaviour(TransformBehaviour target, LevelRuntime level, float speed = 82f)
    {
        _target = target;
        _level = level;
        _speed = speed;
    }

    public override void Awake()
    {
        _transform = Entity.GetComponent<TransformBehaviour>() ?? throw new InvalidOperationException("Enemy needs a transform.");
        _hitbox = Entity.GetComponent<HitboxBehaviour>() ?? throw new InvalidOperationException("Enemy needs a hitbox added first.");
    }

    public override void Update(GameTime gameTime)
    {
        Vector2 direction = _target.Position - _transform.Position;
        if (direction.LengthSquared() < 1f) return;
        direction.Normalize();
        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector2 motion = direction * (_speed * delta);
        _transform.Position = CollisionMath.Move(_transform.Position, motion, _hitbox.Size, _level.Bounds, _level.Walls);
    }
}
