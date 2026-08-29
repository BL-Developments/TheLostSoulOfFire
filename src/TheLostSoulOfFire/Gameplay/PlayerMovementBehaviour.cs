using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Core;
using TheLostSoulOfFire.Ecs;
using TheLostSoulOfFire.Levels;

namespace TheLostSoulOfFire.Gameplay;

public sealed class PlayerMovementBehaviour : GameBehaviour
{
    private readonly LevelRuntime _level;
    private readonly float _speed;
    private TransformBehaviour _transform = null!;
    private HitboxBehaviour _hitbox = null!;

    public PlayerMovementBehaviour(LevelRuntime level, float speed = 210f)
    {
        _level = level;
        _speed = speed;
    }

    public bool AcceptInput { get; set; } = true;

    public override void Awake()
    {
        _transform = Entity.GetComponent<TransformBehaviour>() ?? throw new InvalidOperationException("Movement needs a transform.");
        _hitbox = Entity.GetComponent<HitboxBehaviour>() ?? throw new InvalidOperationException("Movement needs a hitbox added first.");
    }

    public override void Update(GameTime gameTime)
    {
        if (!AcceptInput) return;
        Vector2 input = GameCore.Input.GetMovement();
        float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector2 motion = input * (_speed * delta);
        _transform.Position = CollisionMath.Move(_transform.Position, motion, _hitbox.Size, _level.Bounds, _level.Walls);
    }
}
