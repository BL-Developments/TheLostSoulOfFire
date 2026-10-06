using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Entities;

/// <summary>
/// Sandbox training dummy (#104): stands still, never attacks and never dies. Each hit shows its
/// damage as a rising number and adds to a running total; after a short pause without hits the
/// dummy refills its health and the total starts over.
/// </summary>
public sealed class TrainingDummy : Enemy
{
    private readonly Vector2 _anchor;
    private readonly List<DamageNumber> _numbers = [];
    private float _sinceLastHit;
    private int _hitCount;

    public TrainingDummy(Vector2 position)
        : base(position, GameBalance.TrainingDummyMaxHealth, GameBalance.TrainingDummyRadius)
    {
        _anchor = position;
    }

    public override string StateLabel => "DUMMY";
    public override int GlutReward => 0;

    /// <summary>Damage taken since the last refill, including what the health floor swallowed.</summary>
    public int DamageSinceRefill { get; private set; }

    public int LastHit { get; private set; }

    public IReadOnlyList<DamageNumber> Numbers => _numbers;

    public override void ApplyDamage(DamageInfo damage)
    {
        if (damage.Damage <= 0)
        {
            return;
        }

        LastHit = damage.Damage;
        DamageSinceRefill += damage.Damage;
        _sinceLastHit = 0f;
        _numbers.Add(new DamageNumber(damage.Damage, damage.IsSoulCoreHit, _hitCount++ % 3));
        // The bar drops but stops at 1, so the dummy stays on the field; knockback does not move it.
        base.ApplyDamage(damage with { Damage = Math.Min(damage.Damage, Health - 1), Knockback = Vector2.Zero });
    }

    public override void Update(
        float deltaTime,
        Player player,
        IReadOnlyList<Soul> souls,
        Rectangle movementBounds,
        ParticleSystem particles,
        ScreenEffects screenEffects)
    {
        UpdateCommon(deltaTime, movementBounds);
        Position = _anchor;

        _sinceLastHit += deltaTime;
        if (_sinceLastHit >= GameBalance.TrainingDummyRefillDelay && DamageSinceRefill > 0)
        {
            Health = MaxHealth;
            DamageSinceRefill = 0;
        }

        for (int i = _numbers.Count - 1; i >= 0; i--)
        {
            DamageNumber number = _numbers[i] with { Age = _numbers[i].Age + deltaTime };
            if (number.Age >= GameBalance.TrainingDummyNumberLifetime) _numbers.RemoveAt(i);
            else _numbers[i] = number;
        }
    }

    public override void Draw(
        SpriteBatch batch,
        Texture2D pixel,
        bool debugVisible,
        bool soulSenseActive,
        bool useSpriteArt)
    {
        bool flash = HitFlashRemaining > 0f;
        Color wood = flash ? GameBalance.SoulWhite : new Color(78, 60, 46);
        Color straw = flash ? GameBalance.SoulWhite : new Color(128, 106, 72);
        Color rope = new(48, 36, 28);

        if (!DrawnAsFigure)
        {
            batch.FillEllipse(pixel, Position + new Vector2(0f, 34f), 34f, 9f, new Color(3, 3, 7) * 0.6f);
            batch.DrawLine(pixel, Position + new Vector2(0f, 36f), Position + new Vector2(0f, -58f), wood, 8f);
            batch.DrawLine(pixel, Position + new Vector2(-34f, -30f), Position + new Vector2(34f, -30f), wood, 7f);
            batch.FillEllipse(pixel, Position + new Vector2(0f, -8f), 20f, 28f, straw);
            batch.DrawLine(pixel, Position + new Vector2(-18f, -14f), Position + new Vector2(18f, -14f), rope, 3f);
            batch.DrawLine(pixel, Position + new Vector2(-17f, 6f), Position + new Vector2(17f, 6f), rope, 3f);
            batch.FillCircle(pixel, Position + new Vector2(0f, -50f), 13f, straw);
            batch.DrawCircle(pixel, Position + new Vector2(0f, -8f), 10f, GameBalance.DeathFlame * 0.8f, 2f, 16);
        }

        // The rendered post stands taller; its bar and numbers sit above its hood.
        float lift = DrawnAsFigure ? 52f : 0f;
        Rectangle track = new((int)Position.X - 36, (int)(Position.Y - 82 - lift), 72, 6);
        UiKit.Bar(batch, pixel, track, Health / (float)MaxHealth, GameBalance.DeathFlameBright);

        if (DamageSinceRefill > 0)
        {
            PixelText.DrawFace(batch, pixel, $"SUMME {DamageSinceRefill}", new Vector2(Position.X - PixelText.MeasureFace($"SUMME {DamageSinceRefill}", TextFace.Body, 9.5f, 1f) * 0.5f, track.Y - 18f),
                TextFace.Body, 9.5f, GameBalance.SoulWhite * 0.9f, 1f);
        }

        foreach (DamageNumber number in _numbers)
        {
            float progress = number.Age / GameBalance.TrainingDummyNumberLifetime;
            float alpha = 1f - progress * progress;
            Vector2 at = Position + new Vector2((number.Lane - 1) * 22f, -112f - lift - progress * 42f);
            Color color = number.IsCoreHit ? GameBalance.DeathFlameBright : GameBalance.SoulWhite;
            string text = number.Damage.ToString();
            float pop = 1f + MathF.Max(0f, 0.25f - progress) * 1.6f;
            float cap = (number.IsCoreHit ? 17f : 14f) * pop;
            PixelText.DrawFace(batch, pixel, text, new Vector2(at.X - PixelText.MeasureFace(text, TextFace.Display, cap) * 0.5f, at.Y), TextFace.Display, cap, color * alpha);
        }
    }

    // Never reached: damage stops one point short of zero.
    protected override void OnDeath()
    {
    }

    public readonly record struct DamageNumber(int Damage, bool IsCoreHit, int Lane, float Age = 0f);
}
