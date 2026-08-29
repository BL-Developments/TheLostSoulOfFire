using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Graphics;
using MonoGame.Extended.Particles;
using MonoGame.Extended.Particles.Data;
using MonoGame.Extended.Particles.Modifiers;
using MonoGame.Extended.Particles.Modifiers.Interpolators;
using MonoGame.Extended.Particles.Profiles;
using TheLostSoulOfFire.Core;

namespace TheLostSoulOfFire.Presentation;

public sealed class FireEffects : IDisposable
{
    private readonly ParticleEffect _continuous;
    private readonly ParticleEffect[] _bursts = new ParticleEffect[4];
    private int _nextBurst;

    public FireEffects(string name, Vector2 continuousPosition)
    {
        Texture2DRegion emberRegion = new(GameCore.Assets.Atlas, GameCore.Assets.Region(AtlasRegion.Ember));
        _continuous = CreateContinuous(name, continuousPosition, emberRegion);
        for (int i = 0; i < _bursts.Length; i++) _bursts[i] = CreateBurst($"{name}Burst{i}", emberRegion);
    }

    public bool ContinuousEnabled { get; set; } = true;

    public void TriggerBurst(Vector2 position)
    {
        ParticleEffect effect = _bursts[_nextBurst];
        _nextBurst = (_nextBurst + 1) % _bursts.Length;
        effect.Position = position;
        effect.Trigger();
    }

    public void Update(GameTime gameTime)
    {
        if (ContinuousEnabled) _continuous.Update(gameTime);
        for (int i = 0; i < _bursts.Length; i++) _bursts[i].Update(gameTime);
    }

    public void Draw(SpriteBatch spriteBatch, Matrix camera)
    {
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, null, null, null, camera);
        if (ContinuousEnabled) spriteBatch.Draw(_continuous);
        for (int i = 0; i < _bursts.Length; i++) spriteBatch.Draw(_bursts[i]);
        spriteBatch.End();
    }

    public void Dispose()
    {
        _continuous.Dispose();
        for (int i = 0; i < _bursts.Length; i++) _bursts[i].Dispose();
    }

    private static ParticleEffect CreateContinuous(string name, Vector2 position, Texture2DRegion region)
    {
        ParticleEffect effect = new(name)
        {
            Position = position,
            AutoTrigger = true,
            AutoTriggerFrequency = 0.08f
        };
        ParticleEmitter emitter = new(500)
        {
            LifeSpan = 0.85f,
            TextureRegion = region,
            Profile = Profile.Spray(-Vector2.UnitY, 1.25f),
            Parameters = new ParticleReleaseParameters
            {
                Quantity = new ParticleInt32Parameter(2, 4),
                Speed = new ParticleFloatParameter(18f, 48f),
                Color = new ParticleColorParameter(new Vector3(18f, 1f, 0.58f), new Vector3(46f, 1f, 0.74f)),
                Scale = new ParticleVector2Parameter(new Vector2(0.035f, 0.035f), new Vector2(0.075f, 0.075f))
            }
        };
        emitter.Modifiers.Add(new LinearGravityModifier { Direction = -Vector2.UnitY, Strength = 28f });
        emitter.Modifiers.Add(new AgeModifier
        {
            Interpolators = { new OpacityInterpolator { StartValue = 0.9f, EndValue = 0f } }
        });
        effect.Emitters.Add(emitter);
        return effect;
    }

    private static ParticleEffect CreateBurst(string name, Texture2DRegion region)
    {
        ParticleEffect effect = new(name) { AutoTrigger = false };
        ParticleEmitter emitter = new(180)
        {
            LifeSpan = 0.42f,
            TextureRegion = region,
            Profile = Profile.Circle(3f, CircleRadiation.Out),
            Parameters = new ParticleReleaseParameters
            {
                Quantity = new ParticleInt32Parameter(6, 10),
                Speed = new ParticleFloatParameter(35f, 95f),
                Color = new ParticleColorParameter(new Vector3(20f, 1f, 0.6f), new Vector3(50f, 1f, 0.8f)),
                Scale = new ParticleVector2Parameter(new Vector2(0.02f, 0.02f), new Vector2(0.05f, 0.05f))
            }
        };
        emitter.Modifiers.Add(new DragModifier { Density = 0.4f, DragCoefficient = 0.3f });
        emitter.Modifiers.Add(new AgeModifier
        {
            Interpolators = { new OpacityInterpolator { StartValue = 1f, EndValue = 0f } }
        });
        effect.Emitters.Add(emitter);
        return effect;
    }
}
