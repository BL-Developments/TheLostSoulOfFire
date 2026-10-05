using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using StbImageSharp;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Tests.Visuals;

/// <summary>Straight-alpha RGBA pixels, as stored in the source PNGs.</summary>
internal sealed record ImageData(int Width, int Height, byte[] Rgba)
{
    public static ImageData? Load(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        ImageResult image = ImageResult.FromMemory(File.ReadAllBytes(path), ColorComponents.RedGreenBlueAlpha);
        return new ImageData(image.Width, image.Height, image.Data);
    }

    public int Index(int x, int y) => (y * Width + x) * 4;
}

/// <summary>
/// Mechanical checks of the graphics behind a registry entry. Every problem names the
/// Visual-ID, the file and the reason (spec "visual-quality-checks").
/// </summary>
internal static class AssetChecks
{
    /// <summary>Highest alpha still counted as transparent on a frame's outer ring.</summary>
    public const int TransparentAlpha = 8;

    /// <summary>Share of visible pixels a death-flame graphic may spend on orange, green or blue.</summary>
    public const float DeathFlameBudget = 0.025f;

    public static IEnumerable<string> Check(VisualEntry entry, Func<string, ImageData?> load)
    {
        foreach (VisualClipDefinition clip in entry.Clips.Values)
        {
            IEnumerable<string?> directions = clip.IsDirectional ? VisualDirections.All : [null];
            foreach (string? direction in directions)
            {
                string path = direction is null ? clip.Path : clip.PathFor(direction);
                string label = direction is null ? $"{entry.Id}/{clip.Name}" : VisualDirections.Describe(entry.Id, clip.Name, direction);
                string file = path + ".png";
                ImageData? image = load(path);
                if (image is null)
                {
                    yield return $"{label}: {file}: Datei fehlt.";
                    continue;
                }

                foreach (string problem in CheckImage(entry, clip, image))
                {
                    yield return $"{entry.Id}: {file}: {problem}";
                }

                string? normalPath = direction is null ? clip.NormalMap : clip.NormalMapFor(direction);
                if (normalPath is not null)
                {
                    ImageData? normal = load(normalPath);
                    if (normal is null)
                    {
                        yield return $"{entry.Id}: {normalPath}.png: Normal-Map fehlt.";
                    }
                    else if (normal.Width != image.Width || normal.Height != image.Height)
                    {
                        yield return $"{entry.Id}: {normalPath}.png: Normal-Map-Maße {normal.Width}×{normal.Height}, Farbbild {image.Width}×{image.Height}.";
                    }
                }
            }
        }
    }

    public static IEnumerable<string> CheckImage(VisualEntry entry, VisualClipDefinition clip, ImageData image)
    {
        if (!GridFits(image.Width, image.Height, clip.FrameWidth, clip.FrameHeight, clip.Frames))
        {
            int columns = (int)MathF.Ceiling(MathF.Sqrt(clip.Frames));
            int rows = (int)MathF.Ceiling(clip.Frames / (float)columns);
            yield return $"Raster passt nicht: {clip.Frames} Frames zu {clip.FrameWidth}×{clip.FrameHeight} erwarten ein Vielfaches davon (z. B. {columns * clip.FrameWidth}×{rows * clip.FrameHeight}), das Bild ist {image.Width}×{image.Height}.";
            yield break;
        }

        bool needsTransparency = entry.Kind is not (VisualKind.Environment or VisualKind.Grade);
        if (needsTransparency)
        {
            int columns = image.Width / clip.FrameWidth;
            for (int frame = 0; frame < clip.Frames; frame++)
            {
                int left = frame % columns * clip.FrameWidth;
                int top = frame / columns * clip.FrameHeight;
                int opaque = OpaqueRingPixels(image, left, top, clip.FrameWidth, clip.FrameHeight);
                if (opaque > 0)
                {
                    yield return $"transparenter Rand fehlt in Frame {frame} ({opaque} deckende Randpixel).";
                    break;
                }
            }

            if (HasCheckerboard(image))
            {
                yield return "Hintergrundmuster (eingebackenes Schachbrett statt Transparenz).";
            }
        }

        if (entry.Palette == VisualPalette.DeathFlame)
        {
            float share = ForbiddenHueShare(image);
            if (share > DeathFlameBudget)
            {
                yield return string.Create(CultureInfo.InvariantCulture,
                    $"Farbbudget death-flame überschritten: {share * 100f:0.0} % Orange, Grün oder Blau (erlaubt {DeathFlameBudget * 100f:0.0} %).");
            }
        }
    }

    public static bool GridFits(int width, int height, int frameWidth, int frameHeight, int frames)
    {
        if (width % frameWidth != 0 || height % frameHeight != 0)
        {
            return false;
        }

        int columns = width / frameWidth;
        int rows = height / frameHeight;
        return columns * rows >= frames && rows == (frames + columns - 1) / columns;
    }

    public static int OpaqueRingPixels(ImageData image, int left, int top, int width, int height)
    {
        int count = 0;
        for (int x = left; x < left + width; x++)
        {
            count += image.Rgba[image.Index(x, top) + 3] > TransparentAlpha ? 1 : 0;
            count += image.Rgba[image.Index(x, top + height - 1) + 3] > TransparentAlpha ? 1 : 0;
        }
        for (int y = top + 1; y < top + height - 1; y++)
        {
            count += image.Rgba[image.Index(left, y) + 3] > TransparentAlpha ? 1 : 0;
            count += image.Rgba[image.Index(left + width - 1, y) + 3] > TransparentAlpha ? 1 : 0;
        }

        return count;
    }

    /// <summary>
    /// Finds the grey checkerboard image tools bake in when transparency is lost: square tiles
    /// of two alternating, nearly uniform grey levels over a large part of the image.
    /// </summary>
    public static bool HasCheckerboard(ImageData image)
    {
        foreach (int tile in (int[])[4, 8, 10, 12, 16, 20, 24, 32])
        {
            int tilesX = image.Width / tile;
            int tilesY = image.Height / tile;
            if (tilesX < 4 || tilesY < 4)
            {
                continue;
            }

            double[] sum = new double[2];
            double[] sumSquares = new double[2];
            int[] count = new int[2];
            int total = tilesX * tilesY;
            for (int ty = 0; ty < tilesY; ty++)
            {
                for (int tx = 0; tx < tilesX; tx++)
                {
                    if (!UniformGreyTile(image, tx * tile, ty * tile, tile, out double mean))
                    {
                        continue;
                    }
                    int parity = (tx + ty) % 2;
                    sum[parity] += mean;
                    sumSquares[parity] += mean * mean;
                    count[parity]++;
                }
            }

            if (count[0] + count[1] < total * 0.3 || count[0] < 4 || count[1] < 4)
            {
                continue;
            }

            double mean0 = sum[0] / count[0];
            double mean1 = sum[1] / count[1];
            double spread0 = Math.Sqrt(Math.Max(0, sumSquares[0] / count[0] - mean0 * mean0));
            double spread1 = Math.Sqrt(Math.Max(0, sumSquares[1] / count[1] - mean1 * mean1));
            if (Math.Abs(mean0 - mean1) >= 12 && spread0 <= 6 && spread1 <= 6)
            {
                return true;
            }
        }

        return false;
    }

    private static bool UniformGreyTile(ImageData image, int left, int top, int size, out double mean)
    {
        double sum = 0;
        double sumSquares = 0;
        for (int y = top; y < top + size; y++)
        {
            for (int x = left; x < left + size; x++)
            {
                int index = image.Index(x, y);
                byte r = image.Rgba[index], g = image.Rgba[index + 1], b = image.Rgba[index + 2], a = image.Rgba[index + 3];
                if (a < 200 || Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b)) > 18)
                {
                    mean = 0;
                    return false;
                }
                double luminance = 0.299 * r + 0.587 * g + 0.114 * b;
                sum += luminance;
                sumSquares += luminance * luminance;
            }
        }

        int pixels = size * size;
        mean = sum / pixels;
        return sumSquares / pixels - mean * mean <= 16;
    }

    /// <summary>Share of visible, saturated pixels whose hue is orange, green or blue.</summary>
    public static float ForbiddenHueShare(ImageData image)
    {
        int visible = 0;
        int forbidden = 0;
        for (int index = 0; index < image.Rgba.Length; index += 4)
        {
            if (image.Rgba[index + 3] <= 51)
            {
                continue;
            }

            visible++;
            float r = image.Rgba[index] / 255f, g = image.Rgba[index + 1] / 255f, b = image.Rgba[index + 2] / 255f;
            float max = MathF.Max(r, MathF.Max(g, b));
            float min = MathF.Min(r, MathF.Min(g, b));
            float delta = max - min;
            if (max < 0.2f || delta / max < 0.3f)
            {
                continue;
            }

            float hue = max == r ? 60f * ((g - b) / delta % 6f) : max == g ? 60f * ((b - r) / delta + 2f) : 60f * ((r - g) / delta + 4f);
            if (hue < 0f)
            {
                hue += 360f;
            }

            bool orange = hue is >= 15f and < 50f;
            bool green = hue is >= 75f and < 165f;
            bool blue = hue is >= 185f and < 235f;
            forbidden += orange || green || blue ? 1 : 0;
        }

        return visible == 0 ? 0f : forbidden / (float)visible;
    }
}
