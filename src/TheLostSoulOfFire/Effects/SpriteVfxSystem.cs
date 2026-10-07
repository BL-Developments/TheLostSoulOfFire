using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Effects;

public sealed class SpriteVfxSystem
{
    private sealed class Instance
    {
        public required string VisualId;
        public required SpriteClip? Clip;
        public required Vector2 Position;
        public float Rotation;
        public float Scale;
        public Color Color = Color.White;
        public float Elapsed;

        /// <summary>Where the effect is held each frame (a muzzle that moves with its owner), or null to stay put.</summary>
        public Func<Vector2>? Anchor;

        public bool Loops => Clip?.Loop ?? false;
        public float Duration => Clip?.Duration ?? ArtAssets.EffectDummyLifetime;
    }

    private readonly ArtAssets _art;
    private readonly List<Instance> _instances = [];

    public SpriteVfxSystem(ArtAssets art)
    {
        _art = art;
    }

    public void Spawn(
        string visualId,
        Vector2 position,
        float rotation = 0f,
        float scale = 1f,
        Color? color = null,
        Func<Vector2>? anchor = null)
    {
        _instances.Add(new Instance
        {
            VisualId = visualId,
            Clip = _art.GetEffect(visualId),
            Position = position,
            Rotation = rotation,
            Scale = scale,
            Color = color ?? Color.White,
            Anchor = anchor
        });
    }

    public void Update(float deltaTime)
    {
        for (int index = _instances.Count - 1; index >= 0; index--)
        {
            Instance instance = _instances[index];
            instance.Elapsed += deltaTime;
            if (instance.Anchor is not null)
            {
                instance.Position = instance.Anchor();
            }
            if (!instance.Loops && instance.Elapsed >= instance.Duration)
            {
                _instances.RemoveAt(index);
            }
        }
    }

    public void Draw(SpriteBatch batch)
    {
        foreach (Instance instance in _instances)
        {
            if (instance.Clip is null)
            {
                _art.DrawEffectDummy(batch, instance.VisualId, instance.Position, instance.Rotation, instance.Scale, instance.Elapsed / instance.Duration);
                continue;
            }

            ArtAssets.DrawClip(
                batch,
                instance.Clip,
                instance.Elapsed,
                instance.Position,
                instance.Rotation,
                instance.Scale * _art.WorldSizeOf(instance.VisualId, new Vector2(instance.Clip.FrameWidth)).X / instance.Clip.FrameWidth,
                instance.Color);
        }
    }

    public void Clear() => _instances.Clear();
}
