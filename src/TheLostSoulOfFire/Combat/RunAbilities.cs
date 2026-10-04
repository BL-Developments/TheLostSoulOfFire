using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Combat;

public enum RunAbility { SecondWind, PiercingShot, Retreat, Vortex, Revenge, Setup }

public readonly record struct AbilityDefinition(string Name, string Description, int Cost, float Cooldown);

/// <summary>The first six abilities. All tuning values are provisional, not final balance.</summary>
public sealed class RunAbilities
{
    public static readonly AbilityDefinition[] Definitions =
    [
        new("ZWEITER ATEM", "HEILT 25 LEBEN", 3, 3f),
        new("DURCHSCHLAG", "GESCHOSS TRIFFT MEHRERE GEGNER", 3, 1f),
        new("RUECKSTOSSSPRUNG", "SPRUNG ZURUECK + GEGNER WEGSTOSSEN", 2, 2f),
        new("SOG", "ZIEHT GEGNER ZUM ZIELPUNKT", 4, 4f),
        new("VERGELTUNG", "TREFFER ABFANGEN + NAECHSTEN TREFFER STAERKEN", 3, 4f),
        new("VORLAGE", "TREFFER MARKIERT; FOLGETREFFER ERHAELT BONUS", 2, 2f)
    ];
    private readonly float[] _cooldowns = new float[6];
    private readonly List<AbilityProjectile> _projectiles = [];
    public IReadOnlyList<AbilityProjectile> Projectiles => _projectiles;
    public RunAbility[] Slots { get; } = [RunAbility.SecondWind, RunAbility.PiercingShot];
    public Vector2 VortexCenter { get; private set; }
    public float VortexRemaining { get; private set; }
    public float RetreatRemaining { get; private set; }
    private Vector2 _retreatDirection;
    public string Feedback { get; private set; } = "";
    public float FeedbackRemaining { get; private set; }
    public float Cooldown(RunAbility ability) => _cooldowns[(int)ability];

    public bool Equip(int slot, RunAbility ability)
    {
        if (slot is < 0 or > 1 || (int)ability is < 0 or > 5 || Slots[1 - slot] == ability) return false;
        Slots[slot] = ability;
        return true;
    }

    public bool TryCast(RunAbility ability, Player player, CurrencyWallet wallet, Vector2 target,
        Rectangle bounds, IReadOnlyList<Enemy> enemies, ParticleSystem particles)
    {
        AbilityDefinition definition = Definitions[(int)ability];
        if (player.IsDead) return Reject("NICHT HANDLUNGSFAEHIG");
        if (Cooldown(ability) > 0) return Reject("NOCH NICHT BEREIT");
        if (ability == RunAbility.SecondWind && player.Health >= GameBalance.PlayerMaxHealth)
            return Reject("LEBEN BEREITS VOLL");
        if (ability == RunAbility.Retreat && player.IsDashing) return Reject("AUSWEICHEN AKTIV");
        Vector2 destination = Clamp(player.Position - player.FacingDirection * 180f, bounds, player.Radius);
        if (ability == RunAbility.Retreat && Vector2.DistanceSquared(destination, player.Position) < 16f)
            return Reject("WEG BLOCKIERT");
        if (!wallet.TrySpendRun(Currency.Glut, definition.Cost)) return Reject("NICHT GENUG GLUT");

        _cooldowns[(int)ability] = definition.Cooldown;
        Feedback = definition.Name;
        FeedbackRemaining = 1.5f;
        switch (ability)
        {
            case RunAbility.SecondWind:
                player.Heal(25);
                particles.EmitBurst(player.Position, -Vector2.UnitY, 28, new Color(110, 240, 175), 120f, 5f);
                break;
            case RunAbility.PiercingShot:
                _projectiles.Add(new(player.Position + player.FacingDirection * 25f, player.FacingDirection,
                    player.Attributes.ScaleAbilityDamage(40)));
                break;
            case RunAbility.Retreat:
                _retreatDirection = -player.FacingDirection;
                RetreatRemaining = 0.22f;
                foreach (Enemy enemy in enemies)
                {
                    Vector2 offset = enemy.Position - player.Position;
                    if (enemy.IsAlive && enemy is not Devourer && offset.LengthSquared() < 150f * 150f &&
                        (offset.LengthSquared() < 1 || Vector2.Dot(Vector2.Normalize(offset), player.FacingDirection) > 0.25f))
                        enemy.Displace(player.FacingDirection * 65f, bounds);
                }
                break;
            case RunAbility.Vortex:
                Vector2 aim = target - player.Position;
                if (aim.LengthSquared() > 350f * 350f) aim = Vector2.Normalize(aim) * 350f;
                VortexCenter = Clamp(player.Position + aim, bounds, 10f);
                VortexRemaining = 2f;
                break;
            case RunAbility.Revenge:
                player.AbilityEffects.Guard();
                break;
            case RunAbility.Setup:
                player.AbilityEffects.PrepareSetup();
                break;
        }
        particles.EmitDeathFlame(player.Position, 16, 1f);
        return true;
    }

    private bool Reject(string feedback)
    {
        Feedback = feedback;
        FeedbackRemaining = 1.5f;
        return false;
    }

    public void Update(float dt, Player player, Rectangle bounds, IReadOnlyList<Enemy> enemies,
        ParticleSystem particles, Action<Enemy, DamageInfo> damage)
    {
        for (int i = 0; i < _cooldowns.Length; i++) _cooldowns[i] = MathF.Max(0, _cooldowns[i] - dt);
        FeedbackRemaining = MathF.Max(0, FeedbackRemaining - dt);
        if (player.IsDead) { Clear(player); return; }
        if (RetreatRemaining > 0)
        {
            float step = MathF.Min(dt, RetreatRemaining);
            player.MoveByAbility(_retreatDirection * (180f / 0.22f) * step, bounds);
            RetreatRemaining = MathF.Max(0, RetreatRemaining - dt);
            particles.EmitDeathFlame(player.Position, 2, 0.8f);
        }
        if (VortexRemaining > 0)
        {
            VortexRemaining = MathF.Max(0, VortexRemaining - dt);
            foreach (Enemy enemy in enemies)
            {
                if (!enemy.IsAlive || enemy is Devourer) continue;
                Vector2 pull = VortexCenter - enemy.Position;
                float distance = pull.Length();
                if (distance > 5f && distance <= 155f)
                    enemy.Displace(pull / distance * MathF.Min(distance - 5, 200f * dt), bounds);
            }
            particles.EmitDeathFlame(VortexCenter, 2, 0.6f);
        }
        foreach (AbilityProjectile projectile in _projectiles)
        {
            projectile.Update(dt, bounds);
            foreach (Enemy enemy in enemies)
            {
                if (projectile.TryHit(enemy))
                    damage(enemy, new DamageInfo(projectile.Damage, Vector2.Zero, enemy.Position));
            }
            particles.EmitDeathFlame(projectile.Position, 2, 0.55f);
        }
        _projectiles.RemoveAll(projectile => projectile.IsFinished);
        if (player.AbilityEffects.GuardRemaining > 0)
            particles.EmitDeathFlame(player.Position, 1, 0.8f);
    }

    public static DamageInfo ResolveWeaponHit(Player player, Enemy enemy, DamageInfo damage)
    {
        int bonus = enemy.ConsumeAbilityMark() + player.AbilityEffects.ConsumeRevenge();
        bool mark = player.AbilityEffects.ConsumeSetup();
        if (mark) enemy.MarkForFollowup();
        return damage with { Damage = damage.Damage + bonus };
    }

    public void Clear(Player player)
    {
        Array.Clear(_cooldowns);
        _projectiles.Clear();
        VortexRemaining = RetreatRemaining = FeedbackRemaining = 0;
        player.AbilityEffects.Clear();
    }

    public static Vector2 Clamp(Vector2 position, Rectangle bounds, float radius) => new(
        MathHelper.Clamp(position.X, bounds.Left + radius, bounds.Right - radius),
        MathHelper.Clamp(position.Y, bounds.Top + radius, bounds.Bottom - radius));
}

public sealed class AbilityProjectile(Vector2 origin, Vector2 direction, int damage)
{
    private readonly HashSet<Enemy> _hit = [];
    private readonly Vector2 _direction = direction;
    private float _remaining = 0.75f;
    public Vector2 Position { get; private set; } = origin;
    public Vector2 PreviousPosition { get; private set; } = origin;
    public int Damage { get; } = damage;
    public bool IsFinished { get; private set; }
    public void Update(float dt, Rectangle bounds)
    {
        PreviousPosition = Position;
        Vector2 desired = Position + _direction * 1000f * dt;
        Position = RunAbilities.Clamp(desired, bounds, 1);
        _remaining -= dt;
        IsFinished = _remaining <= 0 || Position != desired;
    }
    public bool TryHit(Enemy enemy)
    {
        if (!enemy.IsAlive || _hit.Contains(enemy)) return false;
        Vector2 segment = Position - PreviousPosition;
        float t = segment.LengthSquared() > 0 ? MathHelper.Clamp(Vector2.Dot(enemy.Position - PreviousPosition, segment) / segment.LengthSquared(), 0, 1) : 0;
        if (Vector2.DistanceSquared(enemy.Position, PreviousPosition + segment * t) > (enemy.Radius + 12) * (enemy.Radius + 12)) return false;
        _hit.Add(enemy);
        return true;
    }
}

