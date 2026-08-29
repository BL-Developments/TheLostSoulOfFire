using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;

namespace TheLostSoulOfFire.Entities;

public abstract class Enemy
{
    private Vector2 _knockbackVelocity;

    protected Enemy(Vector2 position, int maxHealth, float radius)
    {
        Position = position;
        MaxHealth = maxHealth;
        Health = maxHealth;
        Radius = radius;
    }

    public Vector2 Position { get; protected set; }
    public int Health { get; protected set; }
    public int MaxHealth { get; }
    public float Radius { get; }
    public bool IsAlive => Health > 0;
    public bool IsFinished { get; protected set; }
    public float HitFlashRemaining { get; private set; }

    public abstract string StateLabel { get; }

    public abstract void Update(
        float deltaTime,
        Player player,
        IReadOnlyList<Soul> souls,
        Rectangle movementBounds,
        ParticleSystem particles,
        ScreenEffects screenEffects);

    public abstract void Draw(
        SpriteBatch batch,
        Texture2D pixel,
        bool debugVisible,
        bool soulSenseActive,
        bool useSpriteArt);

    public virtual bool TryConsumeSoulSpawn(out Vector2 position)
    {
        position = default;
        return false;
    }

    public virtual void ApplyDamage(DamageInfo damage)
    {
        if (!IsAlive)
        {
            return;
        }

        Health = Math.Max(0, Health - damage.Damage);
        _knockbackVelocity += damage.Knockback;
        HitFlashRemaining = damage.IsSoulCoreHit ? 0.16f : 0.1f;

        if (Health == 0)
        {
            OnDeath();
        }
    }

    protected void UpdateCommon(float deltaTime, Rectangle movementBounds)
    {
        HitFlashRemaining = MathF.Max(0f, HitFlashRemaining - deltaTime);
        Position += _knockbackVelocity * deltaTime;
        _knockbackVelocity *= MathF.Pow(0.02f, deltaTime);
        Position = new Vector2(
            MathHelper.Clamp(Position.X, movementBounds.Left + Radius, movementBounds.Right - Radius),
            MathHelper.Clamp(Position.Y, movementBounds.Top + Radius, movementBounds.Bottom - Radius));
    }

    protected abstract void OnDeath();
}
