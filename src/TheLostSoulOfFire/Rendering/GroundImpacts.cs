using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// The floor answering a blow (tools/visuals/ground_kit.py): while a Devourer winds up, the soul
/// light it drives into the ground creeps out as fissures and reaches the edge of its blow as the
/// fists come down, and loose stones start to shake there; on the slam the floor breaks into a
/// crater of tilted slabs and fissures, a wall of stone dust rolls out to the edge and the cracks
/// flare and cool. The edge of the blow is shown by matter, not by a ring: the fissure tips and
/// the dust end exactly at its radius. Presentation only; nothing here touches the hit.
/// </summary>
public sealed class GroundImpacts
{
    /// <summary>Pixels from a texture's centre to the edge of the blow (ground_kit.EDGE).</summary>
    private const float Edge = 232f;
    private const int GrowthFrames = 8;
    private const int GrowthColumns = 4;
    private const float ShatterHold = 2.4f;
    private const float ShatterFade = 1.3f;
    private const float FlareTime = 0.55f;
    /// <summary>The growth frames of the windup; the last frame is the broken floor's.</summary>
    private const int WindupFrames = GrowthFrames - 1;
    private const int DustVariants = 4;

    private static Texture2D? _shatter;
    private static Texture2D? _fissures;
    private static Texture2D? _dust;

    public static void Load(ContentManager content)
    {
        try
        {
            _shatter = content.Load<Texture2D>("Textures/Effects/ground_shatter");
            _fissures = content.Load<Texture2D>("Textures/Effects/ground_fissures");
            _dust = content.Load<Texture2D>("Textures/Effects/dust_puffs");
        }
        catch (ContentLoadException)
        {
            _shatter = null;
        }
    }

    /// <summary>Without the textures the old rings remain the telegraph.</summary>
    public static bool Loaded => _shatter is not null && _fissures is not null && _dust is not null;

    private sealed class Windup
    {
        public required object Key;
        public Vector2 Center;
        public float Radius;
        public float Progress;
        public float Rotation;
        public float Visible;
        public bool Seen;
        public float DustTimer;
    }

    private sealed class Impact
    {
        public Vector2 Center;
        public float Radius;
        public float Rotation;
        public float Age;
    }

    private sealed class Puff
    {
        public Vector2 Start;
        public Vector2 Direction;
        public float Travel;
        public float TravelTime;
        public Vector2 Drift;
        public float Age;
        public float Life;
        public float StartSize;
        public float EndSize;
        public float Rotation;
        public float Spin;
        public int Variant;
        public float Opacity;
        /// <summary>Thrown up into the air: drawn over the figures instead of under them.</summary>
        public bool Air;
    }

    private readonly List<Windup> _windups = [];
    private readonly List<Impact> _impacts = [];
    private readonly List<Puff> _puffs = [];
    private readonly Random _random = new(2207);
    private float _time;

    /// <summary>
    /// Called every frame a blow is being wound up, with its progress (0 to 1). A windup that is
    /// not renewed (the Devourer was staggered or died) fades out.
    /// </summary>
    public void WindUp(object key, Vector2 center, float radius, float progress, float deltaTime)
    {
        Windup? windup = _windups.Find(candidate => ReferenceEquals(candidate.Key, key));
        if (windup is null)
        {
            windup = new Windup { Key = key, Rotation = (float)(_random.NextDouble() * MathHelper.TwoPi) };
            _windups.Add(windup);
        }

        windup.Center = center;
        windup.Radius = radius;
        windup.Progress = MathHelper.Clamp(progress, 0f, 1f);
        windup.Seen = true;

        // Loose dust lifting along the edge as the blow gathers, more and more often.
        if (Loaded && windup.Progress > 0.35f)
        {
            windup.DustTimer -= deltaTime;
            if (windup.DustTimer <= 0f)
            {
                windup.DustTimer = MathHelper.Lerp(0.09f, 0.025f, (windup.Progress - 0.35f) / 0.65f);
                float angle = (float)(_random.NextDouble() * MathHelper.TwoPi);
                Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
                AddPuff(center + direction * radius * Range(0.9f, 1.0f), direction, Range(2f, 8f), 0.5f,
                    new Vector2(0f, -Range(8f, 18f)), Range(0.6f, 0.9f), Range(22f, 32f), Range(46f, 60f), 0.3f);
            }
        }
    }

    /// <summary>The blow lands: the floor breaks under <paramref name="center"/> out to <paramref name="radius"/>.</summary>
    public void Slam(object key, Vector2 center, float radius)
    {
        Windup? windup = _windups.Find(candidate => ReferenceEquals(candidate.Key, key));
        float rotation = windup?.Rotation ?? (float)(_random.NextDouble() * MathHelper.TwoPi);
        _windups.Remove(windup!);
        _impacts.Add(new Impact { Center = center, Radius = radius, Rotation = rotation });
        if (!Loaded)
        {
            return;
        }

        // The dust wall: rolls out from the crater and stops at the edge of the blow.
        const int wall = 34;
        for (int index = 0; index < wall; index++)
        {
            float angle = index * MathHelper.TwoPi / wall + Range(-0.08f, 0.08f);
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));
            AddPuff(center + direction * radius * 0.22f, direction, radius * Range(0.6f, 0.7f), Range(0.34f, 0.46f),
                direction * Range(4f, 12f) + new Vector2(0f, -Range(6f, 16f)), Range(1.1f, 1.6f), Range(54f, 70f), Range(120f, 150f), 0.95f);
        }

        // A billow thrown up from the crater: it rises around the body and spreads slowly.
        for (int index = 0; index < 11; index++)
        {
            float angle = (float)(_random.NextDouble() * MathHelper.TwoPi);
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle) * 0.6f);
            AddPuff(center + direction * radius * Range(0.05f, 0.3f), direction, radius * Range(0.15f, 0.35f), Range(0.3f, 0.5f),
                new Vector2(Range(-8f, 8f), -Range(26f, 52f)), Range(1.2f, 1.8f), Range(70f, 96f), Range(170f, 220f), 0.6f, air: true);
        }
    }

    public void Update(float deltaTime)
    {
        _time += deltaTime;
        for (int index = _windups.Count - 1; index >= 0; index--)
        {
            Windup windup = _windups[index];
            windup.Visible = windup.Seen
                ? MathF.Min(1f, windup.Visible + deltaTime * 8f)
                : windup.Visible - deltaTime * 3.5f;
            if (!windup.Seen && windup.Visible <= 0f)
            {
                _windups.RemoveAt(index);
                continue;
            }
            windup.Seen = false;
        }

        for (int index = _impacts.Count - 1; index >= 0; index--)
        {
            _impacts[index].Age += deltaTime;
            if (_impacts[index].Age >= ShatterHold + ShatterFade)
            {
                _impacts.RemoveAt(index);
            }
        }

        for (int index = _puffs.Count - 1; index >= 0; index--)
        {
            Puff puff = _puffs[index];
            puff.Age += deltaTime;
            puff.Rotation += puff.Spin * deltaTime;
            if (puff.Age >= puff.Life)
            {
                _puffs.RemoveAt(index);
            }
        }
    }

    public void Clear()
    {
        _windups.Clear();
        _impacts.Clear();
        _puffs.Clear();
    }

    /// <summary>On the floor, under the figures: broken ground, glowing fissures, dust.</summary>
    public void DrawFloor(SpriteBatch batch)
    {
        if (!Loaded)
        {
            return;
        }

        foreach (Impact impact in _impacts)
        {
            float alpha = impact.Age < ShatterHold ? 1f : 1f - (impact.Age - ShatterHold) / ShatterFade;
            // The slab of floor punches down: a quick settle from slightly large.
            float settle = 1f + 0.05f * MathF.Max(0f, 1f - impact.Age / 0.12f);
            float scale = impact.Radius / Edge * settle;
            batch.Draw(_shatter!, impact.Center, null, Color.White * alpha, impact.Rotation,
                new Vector2(_shatter!.Width, _shatter.Height) * 0.5f, scale, SpriteEffects.None, 0f);

            // The cracks flare with the soul light the blow drove in and cool to embers.
            float flare = MathF.Max(0f, 1f - impact.Age / FlareTime);
            float ember = 0.16f * MathF.Max(0f, 1f - impact.Age / (ShatterHold + ShatterFade * 0.5f));
            float glow = flare * flare * 1.1f + ember * (0.8f + 0.2f * MathF.Sin(_time * 7f + impact.Center.X));
            if (glow > 0.01f)
            {
                DrawFissures(batch, impact.Center, impact.Radius / Edge, impact.Rotation, GrowthFrames - 1,
                    Color.Lerp(GameBalance.DeathFlame, GameBalance.DeathFlameBright, flare) * MathF.Min(1f, glow));
            }
        }

        foreach (Windup windup in _windups)
        {
            DrawWindup(batch, windup);
        }

        foreach (Puff puff in _puffs)
        {
            if (!puff.Air)
            {
                DrawPuff(batch, puff);
            }
        }
    }

    /// <summary>Dust thrown into the air, over the figures.</summary>
    public void DrawAir(SpriteBatch batch)
    {
        if (!Loaded)
        {
            return;
        }

        foreach (Puff puff in _puffs)
        {
            if (puff.Air)
            {
                DrawPuff(batch, puff);
            }
        }
    }

    /// <summary>Light the blow throws on the floor and on figures nearby.</summary>
    public void DrawLighting(SpriteBatch batch, SoulfireRenderer renderer)
    {
        foreach (Impact impact in _impacts)
        {
            float flare = MathF.Max(0f, 1f - impact.Age / 0.35f);
            if (flare > 0f)
            {
                renderer.DrawGlow(batch, impact.Center, impact.Radius, GameBalance.DeathFlame, flare * flare * 0.55f);
            }
        }

        foreach (Windup windup in _windups)
        {
            float gather = windup.Progress * windup.Progress * windup.Visible;
            renderer.DrawGlow(batch, windup.Center, windup.Radius * (0.5f + 0.5f * windup.Progress), GameBalance.DeathFlame, gather * 0.32f);
        }
    }

    private void DrawWindup(SpriteBatch batch, Windup windup)
    {
        float progress = windup.Progress;
        float scale = windup.Radius / Edge;

        // The fissures creep outward with the windup and reach the edge as the fists come down;
        // the two nearest growth frames blend so the front moves smoothly.
        float frame = MathHelper.Clamp(progress * 1.08f, 0f, 1f) * (WindupFrames - 1);
        int lower = (int)MathF.Floor(frame);
        int upper = Math.Min(WindupFrames - 1, lower + 1);
        float blend = frame - lower;
        float flicker = 0.85f + 0.15f * MathF.Sin(_time * 23f + windup.Center.Y * 0.1f);
        float strength = (0.35f + 0.65f * progress) * flicker * windup.Visible;
        Color light = Color.Lerp(GameBalance.DeathFlame, GameBalance.DeathFlameBright, progress * progress);
        DrawFissures(batch, windup.Center, scale, windup.Rotation, lower, light * (strength * (1f - blend)));
        if (upper != lower)
        {
            DrawFissures(batch, windup.Center, scale, windup.Rotation, upper, light * (strength * blend));
        }

        // Loose stones along the edge start to hop as the blow gathers.
        float shake = MathHelper.Clamp((progress - 0.3f) / 0.7f, 0f, 1f) * windup.Visible;
        if (shake <= 0f)
        {
            return;
        }

        const int stones = 22;
        for (int index = 0; index < stones; index++)
        {
            float seed = index * 7.31f + windup.Rotation * 3f;
            float angle = index * MathHelper.TwoPi / stones + Hash(seed) * 0.25f;
            float reach = windup.Radius * (0.9f + 0.12f * Hash(seed + 1.3f));
            Vector2 at = windup.Center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * reach;
            float hop = MathF.Abs(MathF.Sin(_time * (26f + 9f * Hash(seed + 2.1f)) + seed)) * 3.2f * shake;
            float size = 2.2f + 2.2f * Hash(seed + 4.7f);
            Texture2D dust = _dust!;
            batch.Draw(dust, at + new Vector2(0f, 1f), SourceOf(0), new Color(4, 3, 8) * (0.35f * shake), 0f,
                new Vector2(dust.Width / 4f), new Vector2(size * 1.3f, size * 0.5f) / (dust.Width / 2f), SpriteEffects.None, 0f);
            batch.Draw(dust, at - new Vector2(0f, hop), SourceOf(index % DustVariants), new Color(62, 56, 70) * shake, seed,
                new Vector2(dust.Width / 4f), size / (dust.Width / 2f) * 1.4f, SpriteEffects.None, 0f);
        }
    }

    private void DrawPuff(SpriteBatch batch, Puff puff)
    {
        float t = puff.Age / puff.Life;
        float travel = 1f - MathF.Pow(1f - MathHelper.Clamp(puff.Age / puff.TravelTime, 0f, 1f), 3f);
        Vector2 at = puff.Start + puff.Direction * (puff.Travel * travel) + puff.Drift * puff.Age;
        float size = MathHelper.Lerp(puff.StartSize, puff.EndSize, 1f - MathF.Pow(1f - t, 2f));
        // Quick to appear, slow to settle.
        float alpha = puff.Opacity * MathHelper.Clamp(puff.Age / 0.06f, 0f, 1f) * (1f - t) * (1f - t);
        Texture2D dust = _dust!;
        // Pale stone dust with a darker underside, so it reads on the light floor.
        batch.Draw(dust, at + new Vector2(0f, size * 0.12f), SourceOf(puff.Variant), new Color(30, 26, 38) * (alpha * 0.35f), puff.Rotation,
            new Vector2(dust.Width / 4f), size / (dust.Width / 2f) * 0.95f, SpriteEffects.None, 0f);
        batch.Draw(dust, at, SourceOf(puff.Variant), new Color(214, 204, 220) * alpha, puff.Rotation,
            new Vector2(dust.Width / 4f), size / (dust.Width / 2f), SpriteEffects.None, 0f);
    }

    private static void DrawFissures(SpriteBatch batch, Vector2 center, float scale, float rotation, int frame, Color color)
    {
        Texture2D sheet = _fissures!;
        int size = sheet.Width / GrowthColumns;
        Rectangle source = new(frame % GrowthColumns * size, frame / GrowthColumns * size, size, size);
        batch.Draw(sheet, center, source, color, rotation, new Vector2(size * 0.5f), scale, SpriteEffects.None, 0f);
    }

    private static Rectangle SourceOf(int variant)
    {
        int size = _dust!.Width / 2;
        return new Rectangle(variant % 2 * size, variant / 2 % 2 * size, size, size);
    }

    private void AddPuff(Vector2 start, Vector2 direction, float travel, float travelTime, Vector2 drift, float life,
        float startSize, float endSize, float opacity, bool air = false) =>
        _puffs.Add(new Puff
        {
            Start = start,
            Direction = direction,
            Travel = travel,
            TravelTime = travelTime,
            Drift = drift,
            Life = life,
            StartSize = startSize,
            EndSize = endSize,
            Rotation = (float)(_random.NextDouble() * MathHelper.TwoPi),
            Spin = Range(-0.6f, 0.6f),
            Variant = _random.Next(DustVariants),
            Opacity = opacity,
            Air = air
        });

    private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);

    private static float Hash(float seed)
    {
        float value = MathF.Sin(seed * 12.9898f) * 43758.5453f;
        return value - MathF.Floor(value);
    }
}
