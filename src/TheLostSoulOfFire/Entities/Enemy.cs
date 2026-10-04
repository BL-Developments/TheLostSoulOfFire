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
    private bool _rewardClaimed;

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

    public float AbilityMarkRemaining { get; private set; }
    public void MarkForFollowup() => AbilityMarkRemaining = 5f;
    public int ConsumeAbilityMark()
    {
        if (AbilityMarkRemaining <= 0) return 0;
        AbilityMarkRemaining = 0;
        return 25;
    }
    public void Displace(Vector2 offset, Rectangle bounds)
    {
        if (IsAlive) Position = RunAbilities.Clamp(Position + offset, bounds, Radius);
    }
    public abstract string StateLabel { get; }

    /// <summary>Glut credited once when this enemy is defeated; amount depends on the type.</summary>
    public abstract int GlutReward { get; }

    /// <summary>Returns the Glut reward exactly once, and only after the enemy was defeated.</summary>
    public bool TryClaimReward(out int glut)
    {
        glut = 0;
        if (IsAlive || _rewardClaimed)
        {
            return false;
        }

        _rewardClaimed = true;
        glut = GlutReward;
        return true;
    }

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
        AbilityMarkRemaining = MathF.Max(0f, AbilityMarkRemaining - deltaTime);
        HitFlashRemaining = MathF.Max(0f, HitFlashRemaining - deltaTime);
        Position += _knockbackVelocity * deltaTime;
        _knockbackVelocity *= MathF.Pow(0.02f, deltaTime);
        Position = new Vector2(
            MathHelper.Clamp(Position.X, movementBounds.Left + Radius, movementBounds.Right - Radius),
            MathHelper.Clamp(Position.Y, movementBounds.Top + Radius, movementBounds.Bottom - Radius));
    }

    protected abstract void OnDeath();
}
