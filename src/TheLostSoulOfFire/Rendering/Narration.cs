using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// A narrator in subtitles (Owner 07.10.2026: the story was not understood): at a few moments
/// of the fight one line says what is happening and why, each once per session, at the top of
/// the picture so it never covers the fight or the cards. Lines wait their turn; each stays
/// <see cref="Show"/> seconds and fades in and out. Presentation only.
/// </summary>
public sealed class Narration
{
    private const float Show = 4.8f;
    private const float Fade = 0.45f;

    private readonly Queue<string> _queue = new();
    private readonly HashSet<string> _said = [];
    private string? _line;
    private float _time;

    /// <summary>The line on screen, if any (for the tour's checks).</summary>
    public string? Current => _line;

    /// <summary>Every line shown so far, in order (for the tour's protocol).</summary>
    public List<string> History { get; } = [];

    public void SayOnce(string key, string line)
    {
        if (_said.Add(key))
        {
            _queue.Enqueue(line);
        }
    }

    public void Update(float deltaTime)
    {
        if (_line is null)
        {
            if (_queue.Count > 0)
            {
                _line = _queue.Dequeue();
                _time = 0f;
                History.Add(_line);
            }
            return;
        }

        _time += deltaTime;
        if (_time >= Show)
        {
            _line = null;
        }
    }

    /// <summary>Drops what is on screen or waiting (a restart); what was said stays said.</summary>
    public void Clear()
    {
        _queue.Clear();
        _line = null;
    }

    public void Draw(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        if (_line is not { } spoken)
        {
            return;
        }
        string line = PixelText.Prose(spoken);

        float alpha = MathHelper.Clamp(MathHelper.Min(_time / Fade, (Show - _time) / Fade), 0f, 1f);
        alpha = alpha * alpha * (3f - 2f * alpha);
        int width = PixelText.MeasureFace(line, TextFace.Body, 15f);
        float centerX = viewport.Width * 0.5f;
        int bandWidth = width + 120;
        // Below the ability feedback lines (85 and 102), above the fight.
        UiKit.CaptionBand(batch, pixel, new Rectangle((int)(centerX - bandWidth * 0.5f), 122, bandWidth, 58), 0.72f * alpha);
        PixelText.DrawFace(batch, pixel, line, new Vector2(centerX - width * 0.5f, 141f), TextFace.Body, 15f, GameBalance.SoulWhite * alpha);
    }
}
