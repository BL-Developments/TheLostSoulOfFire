using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Tuning surface for the generated casting floor. The painted arena asset only
/// contains the perimeter machinery; the whole combat basin was an unlit hole.
/// These values describe the material that fills it.
/// </summary>
public static class ArenaFloorTuning
{
    // Cast-iron pour plates. Larger than an actor so the floor reads as architecture.
    public const int PlateWidth = 236;
    public const int PlateHeight = 152;
    public const int JointWidth = 3;

    // Basin footprint inside the painted border, plus the feather that hides the seam.
    public const float BasinCenterX = 900f;
    public const float BasinCenterY = 520f;
    public const float BasinRadiusX = 716f;
    public const float BasinRadiusY = 372f;
    public const float BasinExponent = 2.55f;
    public const float BasinFeather = 0.235f;

    // The one warm-cold anchor: the collapsed ladle spills light onto the floor.
    public const float LadleLightX = 392f;
    public const float LadleLightY = 470f;
    public const float LadleLightRadius = 640f;
    public const float LadleLightStrength = 15f;
}

/// <summary>
/// Builds the Abandoned Soul Furnace casting floor as a deterministic CPU texture.
/// It is generated instead of authored so it stays out of the runtime Content
/// pipeline, remains fully reversible and can be re-tuned without new art files.
/// The overlay is masked to the dark basin of the painted arena asset, so the
/// existing perimeter machinery still owns the frame edges.
/// </summary>
public static class ArenaFloorSurface
{
    private static readonly Color JointColor = new(10, 9, 15);
    private static readonly Color PlateColor = new(50, 47, 60);
    private static readonly Color BevelColor = new(101, 96, 114);
    private static readonly Color SlagColor = new(21, 19, 30);
    private static readonly Color SlagLipColor = new(88, 82, 100);
    private static readonly Color ResidueColor = new(146, 84, 232);
    private static readonly Color AshColor = new(96, 92, 105);
    private static readonly Color WearColor = new(72, 68, 82);

    public static Texture2D Create(Texture2D arena)
    {
        int width = arena.Width;
        int height = arena.Height;
        Color[] source = new Color[width * height];
        arena.GetData(source);

        float[] openness = BuildOpennessField(source, width, height);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float mask = openness[index] * BasinMask(x, y);
                if (mask <= 0.004f)
                {
                    continue;
                }

                pixels[index] = ComposeFloor(x, y, mask);
            }
        }

        Texture2D surface = new(arena.GraphicsDevice, width, height, false, SurfaceFormat.Color);
        surface.SetData(pixels);
        return surface;
    }

    /// <summary>
    /// The painted asset has no alpha, so the writable basin is derived from where
    /// the art is genuinely dark. A blurred low-resolution field keeps the pipes,
    /// arches and furnace faces intact instead of painting a rectangle over them.
    /// </summary>
    private static float[] BuildOpennessField(Color[] source, int width, int height)
    {
        const int Step = 8;
        int coarseWidth = (width + Step - 1) / Step;
        int coarseHeight = (height + Step - 1) / Step;
        float[] coarse = new float[coarseWidth * coarseHeight];

        for (int cy = 0; cy < coarseHeight; cy++)
        {
            for (int cx = 0; cx < coarseWidth; cx++)
            {
                float peak = 0f;
                for (int y = cy * Step; y < Math.Min((cy + 1) * Step, height); y++)
                {
                    for (int x = cx * Step; x < Math.Min((cx + 1) * Step, width); x++)
                    {
                        Color pixel = source[y * width + x];
                        float luminance = pixel.R * 0.299f + pixel.G * 0.587f + pixel.B * 0.114f;
                        peak = MathF.Max(peak, luminance);
                    }
                }

                // 0 where the painted art is solid, 1 where it is empty void.
                coarse[cy * coarseWidth + cx] = 1f - MathHelper.Clamp((peak - 7f) / 13f, 0f, 1f);
            }
        }

        coarse = Blur(coarse, coarseWidth, coarseHeight, 4);
        coarse = Blur(coarse, coarseWidth, coarseHeight, 3);

        float[] field = new float[width * height];
        for (int y = 0; y < height; y++)
        {
            float sampleY = MathHelper.Clamp((y + 0.5f) / Step - 0.5f, 0f, coarseHeight - 1.001f);
            int y0 = (int)sampleY;
            float ty = sampleY - y0;
            for (int x = 0; x < width; x++)
            {
                float sampleX = MathHelper.Clamp((x + 0.5f) / Step - 0.5f, 0f, coarseWidth - 1.001f);
                int x0 = (int)sampleX;
                float tx = sampleX - x0;
                float top = MathHelper.Lerp(coarse[y0 * coarseWidth + x0], coarse[y0 * coarseWidth + x0 + 1], tx);
                float bottom = MathHelper.Lerp(coarse[(y0 + 1) * coarseWidth + x0], coarse[(y0 + 1) * coarseWidth + x0 + 1], tx);
                field[y * width + x] = MathHelper.Clamp(MathHelper.Lerp(top, bottom, ty), 0f, 1f);
            }
        }

        return field;
    }

    private static float[] Blur(float[] values, int width, int height, int radius)
    {
        float[] horizontal = new float[values.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float total = 0f;
                int count = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    int sample = Math.Clamp(x + offset, 0, width - 1);
                    total += values[y * width + sample];
                    count++;
                }
                horizontal[y * width + x] = total / count;
            }
        }

        float[] result = new float[values.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float total = 0f;
                int count = 0;
                for (int offset = -radius; offset <= radius; offset++)
                {
                    int sample = Math.Clamp(y + offset, 0, height - 1);
                    total += horizontal[sample * width + x];
                    count++;
                }
                result[y * width + x] = total / count;
            }
        }

        return result;
    }

    private static float BasinMask(int x, int y)
    {
        float normalizedX = (x - ArenaFloorTuning.BasinCenterX) / ArenaFloorTuning.BasinRadiusX;
        float normalizedY = (y - ArenaFloorTuning.BasinCenterY) / ArenaFloorTuning.BasinRadiusY;
        float distance = MathF.Pow(
            MathF.Pow(MathF.Abs(normalizedX), ArenaFloorTuning.BasinExponent) +
            MathF.Pow(MathF.Abs(normalizedY), ArenaFloorTuning.BasinExponent),
            1f / ArenaFloorTuning.BasinExponent);
        return 1f - SmoothStep(1f - ArenaFloorTuning.BasinFeather, 1f + 0.06f, distance);
    }

    private static Color ComposeFloor(int x, int y, float mask)
    {
        // 1. Irregular cast plates. Rows stagger and some plates merge so the
        //    grid never reads as a debug checkerboard.
        int row = FloorDiv(y, ArenaFloorTuning.PlateHeight);
        uint rowHash = Hash(row, 977);
        int rowShift = (int)(rowHash % 191u) - 95;
        int column = FloorDiv(x + rowShift, ArenaFloorTuning.PlateWidth);
        uint plateHash = Hash(column, row);
        bool merged = (plateHash & 0x1fu) < 5u;
        int mergeColumn = merged ? column - 1 : column;
        uint materialHash = Hash(mergeColumn, row);

        int localY = y - row * ArenaFloorTuning.PlateHeight;
        int localX = x + rowShift - column * ArenaFloorTuning.PlateWidth;

        float shade = ((materialHash >> 6) & 0xffu) / 255f;
        float grain = ((Hash(x >> 1, y >> 1) >> 9) & 0x3fu) / 63f;
        float tone = 0.7f + shade * 0.46f + grain * 0.13f;

        // 2. Slow ambient falloff away from the collapsed ladle keeps a value
        //    gradient across the basin instead of one flat brightness.
        float lightX = x - ArenaFloorTuning.LadleLightX;
        float lightY = (y - ArenaFloorTuning.LadleLightY) * 1.35f;
        float lightDistance = MathF.Sqrt(lightX * lightX + lightY * lightY) / ArenaFloorTuning.LadleLightRadius;
        float ambient = MathHelper.Lerp(1.32f, 0.5f, MathHelper.Clamp(lightDistance, 0f, 1.35f) / 1.35f);
        tone *= ambient;

        Color color = Scale(PlateColor, tone);

        // Wide, soft wear polish where the basin was walked and dragged. It breaks
        // per-plate flatness without adding pixel noise.
        float wear = MathF.Sin(x * 0.0043f + 1.3f) * MathF.Sin(y * 0.0067f - 0.4f);
        if (wear > 0.32f)
        {
            color = Blend(color, Scale(WearColor, ambient), (wear - 0.32f) * 0.5f);
        }

        // 3. Pour channel: a cooled slag trough with a raised, light-catching lip.
        float channel = ChannelAmount(x, y);
        float lip = ChannelLip(channel);
        if (channel > 0f)
        {
            color = Blend(color, Scale(SlagColor, 0.75f + grain * 0.55f), channel * 0.94f);
        }
        if (lip > 0f)
        {
            color = Blend(color, Scale(SlagLipColor, ambient), lip * 0.6f);
        }

        // 4. Plate joints and the light-catching bevel that gives the floor relief.
        bool nearTop = localY < ArenaFloorTuning.JointWidth;
        bool nearLeft = localX < ArenaFloorTuning.JointWidth && !merged;
        bool nearBottom = localY >= ArenaFloorTuning.PlateHeight - ArenaFloorTuning.JointWidth;
        bool nearRight = localX >= ArenaFloorTuning.PlateWidth - ArenaFloorTuning.JointWidth && !merged;
        if (channel < 0.55f)
        {
            float jointFade = 1f - channel * 1.4f;
            if (nearBottom || nearRight)
            {
                // The lower/right edge of a plate is the raised, lit lip.
                color = Blend(color, Scale(BevelColor, ambient), 0.52f * jointFade);
            }
            else if (nearTop || nearLeft)
            {
                color = Blend(color, JointColor, 0.86f * jointFade);
            }
            else if (localY < ArenaFloorTuning.JointWidth + 3 || (localX < ArenaFloorTuning.JointWidth + 3 && !merged))
            {
                color = Blend(color, JointColor, 0.4f * jointFade);
            }
        }

        // 5. Rivets, chips, scorch rings, seal and residue keep the surface used.
        color = ApplyRivets(color, x, y, localX, localY, merged, ambient, channel);
        color = ApplyChips(color, x, y, localX, localY, channel);
        color = ApplySeal(color, x, y, ambient, channel);
        color = ApplyScorch(color, x, y);
        color = ApplyResidue(color, x, y, channel, ambient);

        float alpha = MathHelper.Clamp(mask, 0f, 1f);
        return new Color(
            (byte)MathF.Round(color.R * alpha),
            (byte)MathF.Round(color.G * alpha),
            (byte)MathF.Round(color.B * alpha),
            (byte)MathF.Round(255f * alpha));
    }

    private static float ChannelDistance(int x, int y)
    {
        // The pour line leaves the collapsed ladle, drops to the lower rim of the
        // basin and runs to the tap hole. It is deliberately routed around the
        // fighting centre so combat keeps a quiet, readable plane.
        float spur = DistanceToSegment(x, y, 372f, 512f, 470f, 742f);
        float run = DistanceToSegment(x, y, 470f, 742f, 1132f, 828f);
        float tail = DistanceToSegment(x, y, 1132f, 828f, 1489f, 690f);
        float wobble = MathF.Sin(x * 0.019f) * 7f + MathF.Sin(y * 0.041f + 1.7f) * 5f;
        return MathF.Min(spur, MathF.Min(run, tail)) - wobble;
    }

    private static float ChannelAmount(int x, int y) =>
        1f - SmoothStep(29f, 48f, ChannelDistance(x, y));

    private static float ChannelLip(float channel) =>
        channel <= 0.02f || channel >= 0.5f ? 0f : 1f - MathF.Abs(channel - 0.22f) / 0.22f;

    private static Color ApplyRivets(Color color, int x, int y, int localX, int localY, bool merged, float ambient, float channel)
    {
        if (merged || channel > 0.35f)
        {
            return color;
        }

        int rivetX = localX % 78;
        int rivetY = localY % 66;
        if (rivetX is < 18 and > 6 || rivetY is < 18 and > 6)
        {
            return color;
        }

        int dx = rivetX - 12;
        int dy = rivetY - 12;
        int distanceSquared = dx * dx + dy * dy;
        if (distanceSquared > 14)
        {
            return color;
        }

        if ((Hash(x / 78, y / 66) & 3u) == 0u)
        {
            return color;
        }

        return distanceSquared <= 5
            ? Blend(color, Scale(BevelColor, ambient * 1.16f), 0.68f)
            : Blend(color, JointColor, 0.46f);
    }

    /// <summary>
    /// Broken plate corners. They are the only high-frequency contrast in the
    /// floor and give combat a sense of a surface that has taken damage.
    /// </summary>
    private static Color ApplyChips(Color color, int x, int y, int localX, int localY, float channel)
    {
        if (channel > 0.4f)
        {
            return color;
        }

        int cornerX = MathF.Min(localX, ArenaFloorTuning.PlateWidth - localX) < 26 ? 1 : 0;
        int cornerY = MathF.Min(localY, ArenaFloorTuning.PlateHeight - localY) < 22 ? 1 : 0;
        if (cornerX + cornerY < 2)
        {
            return color;
        }

        uint hash = Hash(x / 9 + 313, y / 9 - 77);
        if ((hash & 0x1fu) > 3u)
        {
            return color;
        }

        return Blend(color, JointColor, 0.62f);
    }

    /// <summary>
    /// The cast Warden mark around the tap hole. It is engraved, off-centre and
    /// low contrast: an orientation landmark, not decoration competing with VFX.
    /// </summary>
    private static Color ApplySeal(Color color, int x, int y, float ambient, float channel)
    {
        const float CenterX = 1178f;
        const float CenterY = 412f;
        float dx = x - CenterX;
        float dy = (y - CenterY) * 1.42f;
        float distance = MathF.Sqrt(dx * dx + dy * dy);
        if (distance > 156f || channel > 0.3f)
        {
            return color;
        }

        float angle = MathF.Atan2(dy, dx);
        bool ring = IsRing(distance, 146f, 3f) || IsRing(distance, 131f, 2f) || IsRing(distance, 79f, 4f);
        if (ring)
        {
            return Blend(color, JointColor, 0.36f);
        }

        // Eleven cast spokes, one of them broken away.
        int spoke = (int)MathF.Floor((angle + MathHelper.Pi) / MathHelper.TwoPi * 11f);
        float spokePhase = MathF.Abs(MathF.Sin((angle + MathHelper.Pi) * 5.5f));
        if (distance is > 82f and < 129f && spokePhase > 0.972f && spoke != 4)
        {
            return Blend(color, Scale(BevelColor, ambient), 0.24f);
        }

        if (distance < 74f)
        {
            float basin = 1f - distance / 74f;
            return Blend(color, JointColor, 0.16f + basin * 0.34f);
        }

        return color;
    }

    private static bool IsRing(float distance, float radius, float thickness) =>
        MathF.Abs(distance - radius) < thickness;

    private static Color ApplyScorch(Color color, int x, int y)
    {
        color = Scorch(color, x, y, 902f, 512f, 214f, 0.3f);
        color = Scorch(color, x, y, 1352f, 356f, 148f, 0.22f);
        color = Scorch(color, x, y, 612f, 742f, 176f, 0.26f);
        return color;
    }

    private static Color Scorch(Color color, int x, int y, float centerX, float centerY, float radius, float strength)
    {
        float dx = x - centerX;
        float dy = (y - centerY) * 1.18f;
        float distance = MathF.Sqrt(dx * dx + dy * dy) / radius;
        if (distance > 1.15f)
        {
            return color;
        }

        float amount = (1f - SmoothStep(0.55f, 1.15f, distance)) * strength;
        return Blend(color, JointColor, amount);
    }

    private static Color ApplyResidue(Color color, int x, int y, float channel, float ambient)
    {
        // Cold Death-Flame residue crystallised in the cooled pour. Thin, sparse
        // and confined to the trough so it never becomes a decorative wash.
        if (channel > 0.62f)
        {
            float vein = MathF.Sin(x * 0.081f + MathF.Sin(y * 0.043f) * 3.4f) *
                MathF.Sin(y * 0.062f - x * 0.017f);
            if (vein > 0.972f)
            {
                return Blend(color, ResidueColor, (vein - 0.972f) * 22f * MathHelper.Clamp(ambient, 0.35f, 1.1f));
            }
        }

        // Sparse ash-crust clusters, biased to the outer basin.
        uint hash = Hash(x / 4, y / 4);
        if ((hash & 0x3ffu) < 3u)
        {
            return Blend(color, Scale(AshColor, ambient), 0.3f);
        }

        return color;
    }

    private static float DistanceToSegment(float px, float py, float ax, float ay, float bx, float by)
    {
        float dx = bx - ax;
        float dy = by - ay;
        float lengthSquared = dx * dx + dy * dy;
        float amount = lengthSquared <= 0.001f
            ? 0f
            : MathHelper.Clamp(((px - ax) * dx + (py - ay) * dy) / lengthSquared, 0f, 1f);
        float cx = ax + dx * amount - px;
        float cy = ay + dy * amount - py;
        return MathF.Sqrt(cx * cx + cy * cy);
    }

    private static Color Scale(Color color, float amount) => new(
        (byte)MathHelper.Clamp(color.R * amount, 0f, 255f),
        (byte)MathHelper.Clamp(color.G * amount, 0f, 255f),
        (byte)MathHelper.Clamp(color.B * amount, 0f, 255f));

    private static Color Blend(Color from, Color to, float amount)
    {
        float clamped = MathHelper.Clamp(amount, 0f, 1f);
        return new Color(
            (byte)MathHelper.Lerp(from.R, to.R, clamped),
            (byte)MathHelper.Lerp(from.G, to.G, clamped),
            (byte)MathHelper.Lerp(from.B, to.B, clamped));
    }

    private static int FloorDiv(int value, int divisor)
    {
        int quotient = value / divisor;
        return value < 0 && value % divisor != 0 ? quotient - 1 : quotient;
    }

    private static float SmoothStep(float minimum, float maximum, float value)
    {
        float amount = MathHelper.Clamp((value - minimum) / (maximum - minimum), 0f, 1f);
        return amount * amount * (3f - 2f * amount);
    }

    private static uint Hash(int x, int y)
    {
        uint value = (uint)x * 0x8da6b343u ^ (uint)y * 0xd8163841u;
        value ^= value >> 13;
        value *= 0xcb1ab31fu;
        return value ^ value >> 16;
    }
}
