using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Effects;

public sealed class SpriteVfxSystem
{
    private sealed class Instance
    {
        public required SpriteClip Clip;
        public required Vector2 Position;
        public float Rotation;
        public float Scale;
        public Color Color = Color.White;
        public float Elapsed;
    }

    private readonly ArtAssets _art;
    private readonly List<Instance> _instances = [];

    public SpriteVfxSystem(ArtAssets art)
    {
        _art = art;
    }

    public void Spawn(
        string effectKey,
        Vector2 position,
        float rotation = 0f,
        float scale = 1f,
        Color? color = null)
    {
        _instances.Add(new Instance
        {
            Clip = _art.GetEffect(effectKey),
            Position = position,
            Rotation = rotation,
            Scale = scale,
            Color = color ?? Color.White
        });
    }

    public void Update(float deltaTime)
    {
        for (int index = _instances.Count - 1; index >= 0; index--)
        {
            Instance instance = _instances[index];
            instance.Elapsed += deltaTime;
            if (!instance.Clip.Loop && instance.Elapsed >= instance.Clip.Duration)
            {
                _instances.RemoveAt(index);
            }
        }
    }

    public void Draw(SpriteBatch batch)
    {
        foreach (Instance instance in _instances)
        {
            ArtAssets.DrawClip(
                batch,
                instance.Clip,
                instance.Elapsed,
                instance.Position,
                instance.Rotation,
                instance.Scale,
                instance.Color);
        }
    }

    public void Clear() => _instances.Clear();
}
