using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Core;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// All text of the game. With the typeset fonts loaded (<see cref="LoadFonts"/>) the sizes 1–2
/// are set in Alegreya Sans and the sizes from 3 up in Cinzel, sized by cap height so layouts
/// keep their proportions; without them the original 5×7 pixel font draws.
/// </summary>
public static class PixelText
{
    private sealed record Face(SpriteFont Font, float CapHeight, float CapTop);

    private static Face? _uiSmall;
    private static Face? _ui;
    private static Face? _display;
    private static Face? _displayLarge;

    /// <summary>Cap height in logical pixels per size step (the pixel font's was 7 × size).</summary>
    private static readonly float[] CapHeights = [0f, 8.5f, 12f, 17f, 22.5f, 28f, 34f, 44f];

    public static bool UsesFonts => _ui is not null;

    public static void LoadFonts(ContentManager content)
    {
        try
        {
            _uiSmall = FaceOf(content.Load<SpriteFont>("Fonts/ui_small"), 0);
            _ui = FaceOf(content.Load<SpriteFont>("Fonts/ui"), 0);
            _display = FaceOf(content.Load<SpriteFont>("Fonts/display"), 2);
            _displayLarge = FaceOf(content.Load<SpriteFont>("Fonts/display_large"), 3);
        }
        catch (ContentLoadException)
        {
            _uiSmall = _ui = _display = _displayLarge = null;
        }
    }

    private static Face FaceOf(SpriteFont font, float tracking)
    {
        font.Spacing = tracking;
        Rectangle cap = font.GetGlyphs().TryGetValue('H', out SpriteFont.Glyph glyph) ? glyph.Cropping : new Rectangle(0, 0, 1, (int)(font.LineSpacing * 0.7f));
        return new Face(font, cap.Height, cap.Y);
    }

    private static (Face Face, float Scale) Pick(int size)
    {
        int step = Math.Clamp(size, 1, CapHeights.Length - 1);
        Face face = step switch
        {
            1 => _uiSmall!,
            2 => _ui!,
            <= 4 => _display!,
            _ => _displayLarge!
        };
        return (face, CapHeights[step] / face.CapHeight);
    }

    private static readonly Dictionary<char, string> Glyphs = new()
    {
        ['A'] = "01110/10001/10001/11111/10001/10001/10001",
        ['B'] = "11110/10001/10001/11110/10001/10001/11110",
        ['C'] = "01111/10000/10000/10000/10000/10000/01111",
        ['D'] = "11110/10001/10001/10001/10001/10001/11110",
        ['E'] = "11111/10000/10000/11110/10000/10000/11111",
        ['F'] = "11111/10000/10000/11110/10000/10000/10000",
        ['G'] = "01111/10000/10000/10111/10001/10001/01111",
        ['H'] = "10001/10001/10001/11111/10001/10001/10001",
        ['I'] = "11111/00100/00100/00100/00100/00100/11111",
        ['J'] = "00111/00010/00010/00010/00010/10010/01100",
        ['K'] = "10001/10010/10100/11000/10100/10010/10001",
        ['L'] = "10000/10000/10000/10000/10000/10000/11111",
        ['M'] = "10001/11011/10101/10101/10001/10001/10001",
        ['N'] = "10001/11001/10101/10011/10001/10001/10001",
        ['O'] = "01110/10001/10001/10001/10001/10001/01110",
        ['P'] = "11110/10001/10001/11110/10000/10000/10000",
        ['Q'] = "01110/10001/10001/10001/10101/10010/01101",
        ['R'] = "11110/10001/10001/11110/10100/10010/10001",
        ['S'] = "01111/10000/10000/01110/00001/00001/11110",
        ['T'] = "11111/00100/00100/00100/00100/00100/00100",
        ['U'] = "10001/10001/10001/10001/10001/10001/01110",
        ['V'] = "10001/10001/10001/10001/10001/01010/00100",
        ['W'] = "10001/10001/10001/10101/10101/11011/10001",
        ['X'] = "10001/10001/01010/00100/01010/10001/10001",
        ['Y'] = "10001/10001/01010/00100/00100/00100/00100",
        ['Z'] = "11111/00001/00010/00100/01000/10000/11111",
        ['0'] = "01110/10001/10011/10101/11001/10001/01110",
        ['1'] = "00100/01100/00100/00100/00100/00100/01110",
        ['2'] = "01110/10001/00001/00010/00100/01000/11111",
        ['3'] = "11110/00001/00001/01110/00001/00001/11110",
        ['4'] = "00010/00110/01010/10010/11111/00010/00010",
        ['5'] = "11111/10000/10000/11110/00001/00001/11110",
        ['6'] = "01110/10000/10000/11110/10001/10001/01110",
        ['7'] = "11111/00001/00010/00100/01000/01000/01000",
        ['8'] = "01110/10001/10001/01110/10001/10001/01110",
        ['9'] = "01110/10001/10001/01111/00001/00001/01110",
        [':'] = "00000/00100/00100/00000/00100/00100/00000",
        ['-'] = "00000/00000/00000/11111/00000/00000/00000",
        ['+'] = "00000/00100/00100/11111/00100/00100/00000",
        ['/'] = "00001/00010/00010/00100/01000/01000/10000",
        ['.'] = "00000/00000/00000/00000/00000/00110/00110",
        ['·'] = "00000/00000/00100/00100/00000/00000/00000",
        ['%'] = "11001/11010/00100/01000/10110/00110/00000",
        ['?'] = "01110/10001/00001/00010/00100/00000/00100",
        ['Ä'] = "01010/01110/10001/10001/11111/10001/10001",
        ['Ö'] = "01010/01110/10001/10001/10001/10001/01110",
        ['Ü'] = "01010/10001/10001/10001/10001/10001/01110",
        ['ß'] = "01100/10010/10010/01100/10001/10001/10011"
    };

    /// <summary>
    /// Draws in logical coordinates, but every glyph cell is a whole number of output pixels
    /// (see <see cref="RenderResolution"/>) so all cells stay equally sized at a fractional scale.
    /// </summary>
    public static void Draw(SpriteBatch batch, Texture2D pixel, string text, Vector2 position, int scale, Color color)
    {
        if (UsesFonts)
        {
            DrawTypeset(batch, text, position, scale, color);
            return;
        }

        float cell = CellSize(scale);
        Vector2 cellSize = new(cell);
        float x = RenderResolution.SnapToOutputPixel(position.X);
        float y = RenderResolution.SnapToOutputPixel(position.Y);
        foreach (char rawCharacter in text)
        {
            char character = char.ToUpperInvariant(rawCharacter);
            if (character == ' ')
            {
                x += cell * 4;
                continue;
            }

            if (!Glyphs.TryGetValue(character, out string pattern))
            {
                x += cell * 6;
                continue;
            }

            string[] rows = pattern.Split('/');
            for (int row = 0; row < rows.Length; row++)
            {
                for (int column = 0; column < rows[row].Length; column++)
                {
                    if (rows[row][column] == '1')
                    {
                        batch.Draw(pixel, new Vector2(x + column * cell, y + row * cell), null, color, 0f, Vector2.Zero, cellSize, SpriteEffects.None, 0f);
                    }
                }
            }
            x += cell * 6;
        }
    }

    /// <summary>Logical size of one glyph cell: <paramref name="scale"/> rounded to whole output pixels.</summary>
    public static float CellSize(int scale) => RenderResolution.ToOutputPixels(scale) / RenderResolution.Scale;

    public static void DrawCentered(SpriteBatch batch, Texture2D pixel, string text, float centerX, float y, int scale, Color color) =>
        Draw(batch, pixel, text, new Vector2(centerX - Measure(text, scale) * 0.5f, y), scale, color);

    public static bool CanRender(char character) =>
        UsesFonts ? _ui!.Font.Characters.Contains(character) || character == ' ' : Glyphs.ContainsKey(char.ToUpperInvariant(character));

    /// <summary>
    /// Typeset text: the top of the capitals lands where the pixel font's top row did, so
    /// existing layouts line up; a soft shadow keeps it readable over painted ground.
    /// </summary>
    private static void DrawTypeset(SpriteBatch batch, string text, Vector2 position, int scale, Color color)
    {
        (Face face, float k) = Pick(scale);
        string safe = Sanitise(text, face.Font);
        Vector2 at = new(RenderResolution.SnapToOutputPixel(position.X), RenderResolution.SnapToOutputPixel(position.Y - face.CapTop * k));
        float shadowOffset = 1f / RenderResolution.Scale * (scale >= 3 ? 2f : 1f);
        Color shadow = Color.Black * (color.A / 255f * 0.55f);
        batch.DrawString(face.Font, safe, at + new Vector2(shadowOffset), shadow, 0f, Vector2.Zero, k, SpriteEffects.None, 0f);
        batch.DrawString(face.Font, safe, at, color, 0f, Vector2.Zero, k, SpriteEffects.None, 0f);
    }

    private static string Sanitise(string text, SpriteFont font)
    {
        foreach (char character in text)
        {
            if (character != ' ' && !font.Characters.Contains(character))
            {
                char[] cleaned = text.ToCharArray();
                for (int i = 0; i < cleaned.Length; i++)
                {
                    if (cleaned[i] != ' ' && !font.Characters.Contains(cleaned[i]))
                    {
                        cleaned[i] = '?';
                    }
                }
                return new string(cleaned);
            }
        }
        return text;
    }

    public static int Measure(string text, int scale)
    {
        if (UsesFonts)
        {
            (Face face, float k) = Pick(scale);
            return (int)MathF.Ceiling(face.Font.MeasureString(Sanitise(text, face.Font)).X * k);
        }

        int units = 0;
        foreach (char character in text)
        {
            units += character == ' ' ? 4 : 6;
        }
        return (int)MathF.Ceiling(units * CellSize(scale));
    }
}
