using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Effects;

public sealed class ParticleSystem
{
    private enum ParticleShape
    {
        Orb,
        Shard,
        /// <summary>Solid debris (mask shards, stone chips): drawn as matter, not light.</summary>
        Chip
    }

    private enum ParticleMotion
    {
        Free,
        Converge,
        /// <summary>Thrown up, falls under gravity to its floor line (TargetPosition.Y), bounces once and rests.</summary>
        Fall
    }

    private sealed class Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Lifetime;
        public float Remaining;
        public float StartSize;
        public float EndSize;
        public Color Color;
        public ParticleShape Shape;
        public float Rotation;
        public float AngularVelocity;
        public ParticleMotion Motion;
        public Vector2 StartPosition;
        public Vector2 TargetPosition;
    }

    private readonly List<Particle> _particles = [];
    private readonly Random _random = new(1987);

    public void EmitDeathFlame(Vector2 position, int count, float intensity = 1f)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = RandomRange(-MathHelper.Pi, MathHelper.Pi);
            float speed = RandomRange(15f, 48f) * intensity;
            Vector2 velocity = new(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);

            // Death Flame deliberately drifts sideways or downward instead of behaving like normal fire.
            velocity.Y += RandomRange(-2f, 22f) * intensity;
            Color color = _random.NextDouble() > 0.25d ? GameBalance.DeathFlame : GameBalance.DeathFlameBright;
            Add(
                position + RandomVector(7f),
                velocity,
                RandomRange(0.22f, 0.55f),
                RandomRange(2f, 5f) * intensity,
                0.5f,
                color,
                i % 3 == 0 ? ParticleShape.Shard : ParticleShape.Orb);
        }
    }

    public void EmitBurst(Vector2 position, Vector2 direction, int count, Color color, float force, float size)
    {
        Vector2 baseDirection = direction.LengthSquared() > 0.001f ? Vector2.Normalize(direction) : Vector2.UnitX;
        float baseAngle = MathF.Atan2(baseDirection.Y, baseDirection.X);

        for (int i = 0; i < count; i++)
        {
            float angle = baseAngle + RandomRange(-0.9f, 0.9f);
            float speed = RandomRange(force * 0.35f, force);
            Add(
                position + RandomVector(5f),
                new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed,
                RandomRange(0.16f, 0.38f),
                RandomRange(size * 0.45f, size),
                0.5f,
                color,
                i % 4 == 0 ? ParticleShape.Shard : ParticleShape.Orb);
        }
    }

    public void EmitConvergence(
        Vector2 target,
        int count,
        float radius,
        Color color,
        float lifetime = 0.24f,
        float size = 4f)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = RandomRange(-MathHelper.Pi, MathHelper.Pi);
            float distance = RandomRange(radius * 0.62f, radius);
            Vector2 start = target + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
            Add(
                start,
                Vector2.Zero,
                lifetime * RandomRange(0.82f, 1.12f),
                size * RandomRange(0.65f, 1.08f),
                0.45f,
                color,
                i % 3 == 0 ? ParticleShape.Shard : ParticleShape.Orb,
                ParticleMotion.Converge,
                target + RandomVector(2.5f));
        }
    }

    /// <summary>
    /// Debris thrown from <paramref name="position"/> that falls to <paramref name="floorY"/>: mask
    /// shards of a defeated Hollow, stone chips under a Devourer's slam. Presentation only.
    /// </summary>
    public void EmitDebris(Vector2 position, float floorY, int count, Color color, float force, float size, float spread = MathHelper.Pi)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = -MathHelper.PiOver2 + RandomRange(-spread, spread);
            float speed = RandomRange(force * 0.4f, force);
            Vector2 velocity = new(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed * 0.8f);
            Add(position + RandomVector(6f), velocity, RandomRange(1.1f, 1.8f), RandomRange(size * 0.5f, size), size * 0.4f,
                Color.Lerp(color, Color.Black, RandomRange(0f, 0.35f)), ParticleShape.Chip, ParticleMotion.Fall,
                new Vector2(0f, floorY + RandomRange(-10f, 10f)));
        }
    }

    public void EmitSoulRelease(Vector2 position)
    {
        for (int i = 0; i < 18; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            Vector2 velocity = new(side * RandomRange(8f, 34f), RandomRange(-78f, -28f));
            Add(position + RandomVector(5f), velocity, RandomRange(0.45f, 0.85f), RandomRange(2f, 5f), 0.4f, GameBalance.SoulWhite, ParticleShape.Orb);
        }
    }

    public void Update(float deltaTime)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            Particle particle = _particles[i];
            particle.Remaining -= deltaTime;
            if (particle.Remaining <= 0f)
            {
                _particles.RemoveAt(i);
                continue;
            }

            if (particle.Motion == ParticleMotion.Converge)
            {
                float progress = 1f - particle.Remaining / particle.Lifetime;
                float eased = 1f - MathF.Pow(1f - MathHelper.Clamp(progress, 0f, 1f), 2.4f);
                particle.Position = Vector2.Lerp(particle.StartPosition, particle.TargetPosition, eased);
            }
            else if (particle.Motion == ParticleMotion.Fall)
            {
                float floor = particle.TargetPosition.Y;
                if (particle.Position.Y < floor || particle.Velocity.LengthSquared() > 1f)
                {
                    particle.Velocity.Y += 980f * deltaTime;
                    particle.Position += particle.Velocity * deltaTime;
                    if (particle.Position.Y >= floor && particle.Velocity.Y > 0f)
                    {
                        particle.Position.Y = floor;
                        particle.Velocity = particle.Velocity.Y > 120f ? new Vector2(particle.Velocity.X * 0.4f, -particle.Velocity.Y * 0.28f) : Vector2.Zero;
                        particle.AngularVelocity *= 0.3f;
                    }
                }
                else
                {
                    particle.AngularVelocity = 0f;
                }
            }
            else
            {
                particle.Position += particle.Velocity * deltaTime;
                particle.Velocity *= MathF.Pow(0.08f, deltaTime);
            }
            particle.Rotation += particle.AngularVelocity * deltaTime;
        }
    }

    /// <summary>
    /// Draws the particles. With <paramref name="softDot"/> (a soft round light) embers glow as
    /// points of light with a hot centre and shards streak along their direction; without it they
    /// fall back to flat shapes.
    /// </summary>
    public void Draw(SpriteBatch batch, Texture2D pixel, Texture2D? softDot = null)
    {
        if (softDot is not null)
        {
            DrawSoft(batch, softDot, pixel);
            return;
        }

        foreach (Particle particle in _particles)
        {
            if (particle.Shape == ParticleShape.Chip)
            {
                DrawChip(batch, pixel, particle);
                continue;
            }

            float normalized = particle.Remaining / particle.Lifetime;
            float size = MathHelper.Lerp(particle.EndSize, particle.StartSize, normalized);
            if (particle.Shape == ParticleShape.Shard)
            {
                Vector2 direction = new(MathF.Cos(particle.Rotation), MathF.Sin(particle.Rotation));
                batch.DrawLine(pixel, particle.Position - direction * size, particle.Position + direction * size * 1.6f, particle.Color * normalized, MathF.Max(1.5f, size * 0.45f));
            }
            else
            {
                batch.FillCircle(pixel, particle.Position, size, particle.Color * normalized);
            }
        }
    }

    private void DrawChip(SpriteBatch batch, Texture2D pixel, Particle particle)
    {
        float normalized = particle.Remaining / particle.Lifetime;
        float fade = MathHelper.Clamp(normalized / 0.3f, 0f, 1f);
        float size = MathHelper.Lerp(particle.EndSize, particle.StartSize, normalized);
        // A thin flake: wide one way, narrow the other, turning as it falls.
        batch.Draw(pixel, particle.Position, null, particle.Color * fade, particle.Rotation, new Vector2(0.5f, 0.5f),
            new Vector2(size * 1.6f, size * 0.7f), SpriteEffects.None, 0f);
    }

    private void DrawSoft(SpriteBatch batch, Texture2D dot, Texture2D pixel)
    {
        Vector2 origin = new(dot.Width * 0.5f, dot.Height * 0.5f);
        float unit = 1f / dot.Width;
        foreach (Particle particle in _particles)
        {
            if (particle.Shape == ParticleShape.Chip)
            {
                DrawChip(batch, pixel, particle);
                continue;
            }

            float normalized = particle.Remaining / particle.Lifetime;
            float size = MathHelper.Lerp(particle.EndSize, particle.StartSize, normalized);
            float fade = normalized * normalized * (3f - 2f * normalized);
            // Light, not paint: the halo adds light (no coverage), the centre burns whiter.
            Color halo = particle.Color * (0.75f * fade);
            halo.A = 0;
            Color centre = Color.Lerp(particle.Color, Color.White, 0.55f) * fade;
            if (particle.Shape == ParticleShape.Shard)
            {
                Vector2 velocity = particle.Velocity.LengthSquared() > 1f ? particle.Velocity : new Vector2(MathF.Cos(particle.Rotation), MathF.Sin(particle.Rotation));
                float angle = MathF.Atan2(velocity.Y, velocity.X);
                float stretch = 2.2f + MathHelper.Clamp(velocity.Length() / 120f, 0f, 3f);
                batch.Draw(dot, particle.Position, null, halo, angle, origin, new Vector2(size * stretch * 2.4f, size * 1.6f) * unit, SpriteEffects.None, 0f);
                batch.Draw(dot, particle.Position, null, centre, angle, origin, new Vector2(size * stretch * 1.3f, size * 0.7f) * unit, SpriteEffects.None, 0f);
            }
            else
            {
                batch.Draw(dot, particle.Position, null, halo, 0f, origin, size * 3.2f * unit, SpriteEffects.None, 0f);
                batch.Draw(dot, particle.Position, null, centre, 0f, origin, size * 1.3f * unit, SpriteEffects.None, 0f);
            }
        }
    }

    public void DrawLighting(SpriteBatch batch, SoulfireRenderer renderer)
    {
        foreach (Particle particle in _particles)
        {
            if (particle.Shape == ParticleShape.Chip)
            {
                continue;
            }

            float normalized = particle.Remaining / particle.Lifetime;
            float size = MathHelper.Lerp(particle.EndSize, particle.StartSize, normalized);
            float radius = MathF.Max(10f, size * SoulfireRenderSettings.ParticleGlowRadiusMultiplier);
            renderer.DrawGlow(
                batch,
                particle.Position,
                radius,
                particle.Color,
                normalized * SoulfireRenderSettings.ParticleGlowIntensity);
        }
    }

    public void Clear() => _particles.Clear();

    private void Add(
        Vector2 position,
        Vector2 velocity,
        float lifetime,
        float startSize,
        float endSize,
        Color color,
        ParticleShape shape,
        ParticleMotion motion = ParticleMotion.Free,
        Vector2 targetPosition = default)
    {
        _particles.Add(new Particle
        {
            Position = position,
            StartPosition = position,
            TargetPosition = targetPosition,
            Velocity = velocity,
            Lifetime = lifetime,
            Remaining = lifetime,
            StartSize = startSize,
            EndSize = endSize,
            Color = color,
            Shape = shape,
            Motion = motion,
            Rotation = RandomRange(-MathHelper.Pi, MathHelper.Pi),
            AngularVelocity = RandomRange(-8f, 8f)
        });
    }

    private Vector2 RandomVector(float radius) =>
        new(RandomRange(-radius, radius), RandomRange(-radius, radius));

    private float RandomRange(float minimum, float maximum) =>
        minimum + (float)_random.NextDouble() * (maximum - minimum);
}
