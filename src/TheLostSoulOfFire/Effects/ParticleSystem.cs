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
        Shard
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

            particle.Position += particle.Velocity * deltaTime;
            particle.Velocity *= MathF.Pow(0.08f, deltaTime);
            particle.Rotation += particle.AngularVelocity * deltaTime;
        }
    }

    public void Draw(SpriteBatch batch, Texture2D pixel)
    {
        foreach (Particle particle in _particles)
        {
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

    private void Add(Vector2 position, Vector2 velocity, float lifetime, float startSize, float endSize, Color color, ParticleShape shape)
    {
        _particles.Add(new Particle
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Remaining = lifetime,
            StartSize = startSize,
            EndSize = endSize,
            Color = color,
            Shape = shape,
            Rotation = RandomRange(-MathHelper.Pi, MathHelper.Pi),
            AngularVelocity = RandomRange(-8f, 8f)
        });
    }

    private Vector2 RandomVector(float radius) =>
        new(RandomRange(-radius, radius), RandomRange(-radius, radius));

    private float RandomRange(float minimum, float maximum) =>
        minimum + (float)_random.NextDouble() * (maximum - minimum);
}
