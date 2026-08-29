using TheLostSoulOfFire.Ecs;

namespace TheLostSoulOfFire.Gameplay;

public sealed class EnemyActor
{
    public EnemyActor(GameEntity entity, HitboxBehaviour hitbox, HealthBehaviour health)
    {
        Entity = entity;
        Hitbox = hitbox;
        Health = health;
    }

    public GameEntity Entity { get; }
    public HitboxBehaviour Hitbox { get; }
    public HealthBehaviour Health { get; }
    public bool Defeated { get; set; }
}
