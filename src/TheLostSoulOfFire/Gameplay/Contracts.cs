using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Gameplay;

public interface ICollidable
{
    Rectangle Bounds { get; }
}

public interface IDamageable
{
    int Health { get; }
    bool IsDead { get; }
    bool TakeDamage(int amount);
}
