using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Ecs;

namespace TheLostSoulOfFire.Gameplay;

public sealed class HitboxBehaviour : GameBehaviour, ICollidable
{
    private readonly Point _size;
    private TransformBehaviour _transform = null!;

    public HitboxBehaviour(int width, int height)
    {
        _size = new Point(width, height);
    }

    public Point Size => _size;

    public Rectangle Bounds => CollisionMath.BoundsAt(_transform.Position, _size);

    public override void Awake() =>
        _transform = Entity.GetComponent<TransformBehaviour>() ?? throw new InvalidOperationException("Hitbox needs a transform.");
}
