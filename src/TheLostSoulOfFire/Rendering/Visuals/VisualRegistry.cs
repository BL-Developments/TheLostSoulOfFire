using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Rendering.Visuals;

public enum VisualKind
{
    Character,
    Effect,
    Environment,
    Prop,
    Sprite
}

public enum VisualPalette
{
    DeathFlame,
    LifeFlame,
    World
}

public enum ClipProgress
{
    Time,
    Distance
}

/// <summary>
/// One animation of a Visual-ID. <see cref="Path"/> is a content path without extension;
/// for characters it contains <c>{dir}</c>, which stands for one of the eight directions.
/// </summary>
public sealed record VisualClipDefinition(
    string Name,
    string Path,
    int FrameWidth,
    int FrameHeight,
    int Frames,
    float FramesPerSecond,
    bool Loop,
    string? NormalMap,
    ClipProgress Progress,
    float CycleDistance)
{
    public bool IsDirectional => Path.Contains(VisualDirections.Placeholder, StringComparison.Ordinal);

    public float Duration => Frames / FramesPerSecond;

    public string PathFor(string direction) => Path.Replace(VisualDirections.Placeholder, direction, StringComparison.Ordinal);

    public string? NormalMapFor(string direction) => NormalMap?.Replace(VisualDirections.Placeholder, direction, StringComparison.Ordinal);
}

public sealed record VisualDissolve(float Duration);

public sealed record VisualEntry(
    string Id,
    VisualKind Kind,
    VisualPalette Palette,
    Vector2 WorldSize,
    Vector2 Origin,
    string? FallbackClip,
    IReadOnlyDictionary<string, VisualClipDefinition> Clips,
    VisualDissolve? Dissolve)
{
    public bool TryGetClip(string name, out VisualClipDefinition clip) => Clips.TryGetValue(name, out clip!);
}

public sealed class VisualRegistryException(string message) : Exception(message);

/// <summary>The parsed contents of <c>Content/Visuals/registry.json</c>.</summary>
public sealed partial class VisualRegistry
{
    public const string ContentPath = "Content/Visuals/registry.json";

    private readonly Dictionary<string, VisualEntry> _entries;

    public VisualRegistry(IEnumerable<VisualEntry> entries)
    {
        _entries = entries.ToDictionary(entry => entry.Id, StringComparer.Ordinal);
    }

    public static VisualRegistry Empty { get; } = new([]);

    public IReadOnlyCollection<VisualEntry> Entries => _entries.Values;

    public bool TryGet(string id, out VisualEntry entry) => _entries.TryGetValue(id, out entry!);

    public static VisualRegistry Load(Stream stream)
    {
        using StreamReader reader = new(stream);
        return Parse(reader.ReadToEnd());
    }

    /// <summary>
    /// Parses and validates a registry. Every problem is reported with its Visual-ID and
    /// field; all problems are collected before the exception is thrown.
    /// </summary>
    public static VisualRegistry Parse(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        }
        catch (JsonException exception)
        {
            throw new VisualRegistryException($"Visual-Registry ist kein gültiges JSON: {exception.Message}");
        }

        using (document)
        {
            List<string> errors = [];
            List<VisualEntry> entries = [];
            if (!document.RootElement.TryGetProperty("visuals", out JsonElement visuals) || visuals.ValueKind != JsonValueKind.Array)
            {
                throw new VisualRegistryException("Visual-Registry: Feld 'visuals' fehlt oder ist keine Liste.");
            }

            HashSet<string> seen = new(StringComparer.Ordinal);
            int index = 0;
            foreach (JsonElement element in visuals.EnumerateArray())
            {
                EntryReader entryReader = new(element, $"#{index}", errors);
                VisualEntry? entry = entryReader.Read();
                if (entry is not null)
                {
                    if (!seen.Add(entry.Id))
                    {
                        errors.Add($"'{entry.Id}' Feld 'id': kommt mehrfach vor.");
                    }
                    else
                    {
                        entries.Add(entry);
                    }
                }
                index++;
            }

            if (errors.Count > 0)
            {
                throw new VisualRegistryException("Visual-Registry ungültig:" + Environment.NewLine + string.Join(Environment.NewLine, errors.Select(error => "  " + error)));
            }

            return new VisualRegistry(entries);
        }
    }

    private sealed partial class EntryReader(JsonElement element, string fallbackLabel, List<string> errors)
    {
        private string _label = fallbackLabel;

        [GeneratedRegex("^[a-z0-9]+([.-][a-z0-9]+)*$")]
        private static partial Regex IdPattern();

        public VisualEntry? Read()
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                Error("eintrag", "muss ein Objekt sein.");
                return null;
            }

            int errorsBefore = errors.Count;
            string? id = RequiredString(element, "id");
            if (id is not null)
            {
                _label = id;
                if (!IdPattern().IsMatch(id))
                {
                    Error("id", "nur Kleinbuchstaben, Ziffern, '.' und '-' erlaubt.");
                }
            }

            VisualKind kind = RequiredEnum(element, "kind", KindNames);
            VisualPalette palette = RequiredEnum(element, "palette", PaletteNames);
            Vector2 worldSize = RequiredPair(element, "worldSize", positive: true);
            Vector2 origin = OptionalPair(element, "origin") ?? new Vector2(0.5f);
            if (origin.X is < 0f or > 1f || origin.Y is < 0f or > 1f)
            {
                Error("origin", "beide Werte müssen zwischen 0 und 1 liegen.");
            }

            Dictionary<string, VisualClipDefinition> clips = new(StringComparer.Ordinal);
            if (!element.TryGetProperty("clips", out JsonElement clipsElement) || clipsElement.ValueKind != JsonValueKind.Object)
            {
                Error("clips", "fehlt oder ist kein Objekt.");
            }
            else
            {
                foreach (JsonProperty property in clipsElement.EnumerateObject())
                {
                    VisualClipDefinition? clip = ReadClip(property.Name, property.Value, kind);
                    if (clip is not null)
                    {
                        clips[property.Name] = clip;
                    }
                }
                if (!clipsElement.EnumerateObject().Any())
                {
                    Error("clips", "braucht mindestens einen Clip.");
                }
            }

            string? fallback = OptionalString(element, "fallbackClip");
            if (fallback is not null && !clips.ContainsKey(fallback) && clipsElement.ValueKind == JsonValueKind.Object &&
                !clipsElement.TryGetProperty(fallback, out _))
            {
                Error("fallbackClip", $"Clip '{fallback}' existiert nicht.");
            }

            VisualDissolve? dissolve = null;
            if (element.TryGetProperty("dissolve", out JsonElement dissolveElement))
            {
                float duration = RequiredFloat(dissolveElement, "duration", "dissolve.duration");
                if (duration <= 0f)
                {
                    Error("dissolve.duration", "muss größer als 0 sein.");
                }
                dissolve = new VisualDissolve(duration);
            }

            if (errors.Count > errorsBefore || id is null)
            {
                return null;
            }

            return new VisualEntry(id, kind, palette, worldSize, origin, fallback, clips, dissolve);
        }

        private VisualClipDefinition? ReadClip(string name, JsonElement clip, VisualKind kind)
        {
            string prefix = $"clips.{name}";
            if (clip.ValueKind != JsonValueKind.Object)
            {
                Error(prefix, "muss ein Objekt sein.");
                return null;
            }

            int errorsBefore = errors.Count;
            string? path = RequiredString(clip, "path", $"{prefix}.path");
            Vector2 frameSize = RequiredPair(clip, "frameSize", positive: true, field: $"{prefix}.frameSize");
            int frames = RequiredInt(clip, "frames", $"{prefix}.frames");
            if (frames < 1)
            {
                Error($"{prefix}.frames", "muss eine ganze Zahl ≥ 1 sein.");
            }
            float fps = RequiredFloat(clip, "fps", $"{prefix}.fps");
            if (fps <= 0f)
            {
                Error($"{prefix}.fps", "muss größer als 0 sein.");
            }
            bool loop = RequiredBool(clip, "loop", $"{prefix}.loop");
            string? normalMap = OptionalString(clip, "normalMap");
            ClipProgress progress = clip.TryGetProperty("progress", out _)
                ? RequiredEnum(clip, "progress", ProgressNames, $"{prefix}.progress")
                : ClipProgress.Time;
            float cycleDistance = 0f;
            if (progress == ClipProgress.Distance)
            {
                cycleDistance = RequiredFloat(clip, "cycleDistance", $"{prefix}.cycleDistance");
                if (cycleDistance <= 0f)
                {
                    Error($"{prefix}.cycleDistance", "muss bei progress 'distance' größer als 0 sein.");
                }
            }

            if (path is not null)
            {
                bool directional = path.Contains(VisualDirections.Placeholder, StringComparison.Ordinal);
                if (kind == VisualKind.Character && !directional)
                {
                    Error($"{prefix}.path", $"Figuren brauchen '{VisualDirections.Placeholder}' im Pfad.");
                }
                if (kind != VisualKind.Character && directional)
                {
                    Error($"{prefix}.path", $"'{VisualDirections.Placeholder}' ist nur für Figuren erlaubt.");
                }
                if (normalMap is not null && normalMap.Contains(VisualDirections.Placeholder, StringComparison.Ordinal) != directional)
                {
                    Error($"{prefix}.normalMap", $"muss '{VisualDirections.Placeholder}' genau dann enthalten, wenn 'path' es enthält.");
                }
            }

            if (errors.Count > errorsBefore || path is null)
            {
                return null;
            }

            return new VisualClipDefinition(name, path, (int)frameSize.X, (int)frameSize.Y, frames, fps, loop, normalMap, progress, cycleDistance);
        }

        private static readonly Dictionary<string, VisualKind> KindNames = new()
        {
            ["character"] = VisualKind.Character,
            ["effect"] = VisualKind.Effect,
            ["environment"] = VisualKind.Environment,
            ["prop"] = VisualKind.Prop,
            ["sprite"] = VisualKind.Sprite
        };

        private static readonly Dictionary<string, VisualPalette> PaletteNames = new()
        {
            ["death-flame"] = VisualPalette.DeathFlame,
            ["life-flame"] = VisualPalette.LifeFlame,
            ["world"] = VisualPalette.World
        };

        private static readonly Dictionary<string, ClipProgress> ProgressNames = new()
        {
            ["time"] = ClipProgress.Time,
            ["distance"] = ClipProgress.Distance
        };

        private void Error(string field, string message) => errors.Add($"'{_label}' Feld '{field}': {message}");

        private string? RequiredString(JsonElement owner, string name, string? field = null)
        {
            if (owner.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(value.GetString()))
            {
                return value.GetString();
            }
            Error(field ?? name, "fehlt oder ist leer.");
            return null;
        }

        private string? OptionalString(JsonElement owner, string name)
        {
            if (!owner.TryGetProperty(name, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }
            if (value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
            {
                return value.GetString();
            }
            Error(name, "muss ein nicht leerer Text sein.");
            return null;
        }

        private T RequiredEnum<T>(JsonElement owner, string name, Dictionary<string, T> names, string? field = null) where T : struct
        {
            string? text = RequiredString(owner, name, field);
            if (text is null)
            {
                return default;
            }
            if (names.TryGetValue(text, out T value))
            {
                return value;
            }
            Error(field ?? name, $"'{text}' ist ungültig; erlaubt: {string.Join(", ", names.Keys)}.");
            return default;
        }

        private int RequiredInt(JsonElement owner, string name, string field)
        {
            if (owner.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int result))
            {
                return result;
            }
            Error(field, "fehlt oder ist keine ganze Zahl.");
            return 0;
        }

        private float RequiredFloat(JsonElement owner, string name, string field)
        {
            if (owner.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number)
            {
                return value.GetSingle();
            }
            Error(field, "fehlt oder ist keine Zahl.");
            return 0f;
        }

        private bool RequiredBool(JsonElement owner, string name, string field)
        {
            if (owner.TryGetProperty(name, out JsonElement value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return value.GetBoolean();
            }
            Error(field, "fehlt oder ist kein Wahrheitswert.");
            return false;
        }

        private Vector2 RequiredPair(JsonElement owner, string name, bool positive, string? field = null)
        {
            Vector2? pair = ReadPair(owner, name, field ?? name);
            if (pair is null)
            {
                Error(field ?? name, "fehlt; erwartet [Breite, Höhe].");
                return Vector2.Zero;
            }
            if (positive && (pair.Value.X <= 0f || pair.Value.Y <= 0f))
            {
                Error(field ?? name, "beide Werte müssen größer als 0 sein.");
            }
            return pair.Value;
        }

        private Vector2? OptionalPair(JsonElement owner, string name) =>
            owner.TryGetProperty(name, out _) ? ReadPair(owner, name, name) ?? Vector2.Zero : null;

        private Vector2? ReadPair(JsonElement owner, string name, string field)
        {
            if (!owner.TryGetProperty(name, out JsonElement value))
            {
                return null;
            }
            if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() != 2 ||
                value.EnumerateArray().Any(item => item.ValueKind != JsonValueKind.Number))
            {
                Error(field, "muss eine Liste aus zwei Zahlen sein.");
                return Vector2.Zero;
            }
            float[] numbers = value.EnumerateArray().Select(item => item.GetSingle()).ToArray();
            return new Vector2(numbers[0], numbers[1]);
        }
    }
}

public static class VisualDirections
{
    public const string Placeholder = "{dir}";

    /// <summary>The eight directions every character clip must provide, clockwise from north.</summary>
    public static IReadOnlyList<string> All { get; } = ["n", "ne", "e", "se", "s", "sw", "w", "nw"];

    /// <summary>Screen-space direction (y down) to the nearest of the eight directions.</summary>
    public static string FromVector(Vector2 direction)
    {
        if (direction.LengthSquared() < 0.001f)
        {
            return "s";
        }

        return FromAngle(MathF.Atan2(direction.Y, direction.X));
    }

    public static string FromAngle(float radians) => FromSector(SectorOf(radians));

    /// <summary>Sector 0 is east, counting clockwise on screen (y down).</summary>
    public static int SectorOf(float radians)
    {
        float degrees = MathHelper.ToDegrees(radians) % 360f;
        if (degrees < 0f)
        {
            degrees += 360f;
        }
        return (int)MathF.Floor((degrees + 22.5f) / 45f) % 8;
    }

    public static string FromSector(int sector) => (((sector % 8) + 8) % 8) switch
    {
        0 => "e",
        1 => "se",
        2 => "s",
        3 => "sw",
        4 => "w",
        5 => "nw",
        6 => "n",
        _ => "ne"
    };

    public static float AngleOfSector(int sector) => MathHelper.ToRadians((((sector % 8) + 8) % 8) * 45f);

    public static string Describe(string id, string clip, string direction) =>
        string.Create(CultureInfo.InvariantCulture, $"{id}/{clip}/{direction}");
}

/// <summary>The outcome of asking an entry for a clip.</summary>
public readonly record struct ClipResolution(VisualClipDefinition? Clip, string? Missing)
{
    public bool IsDummy => Clip is null;
}

/// <summary>
/// The fallback rule: a missing Visual-ID draws as a dummy; a missing clip uses the
/// entry's <c>fallbackClip</c> and is reported as <c>id/clip</c>; a missing clip without
/// a usable fallback also draws as a dummy.
/// </summary>
public static class VisualResolver
{
    public static ClipResolution Resolve(VisualRegistry registry, string id, string clip)
    {
        if (!registry.TryGet(id, out VisualEntry entry))
        {
            return new ClipResolution(null, id);
        }

        if (entry.TryGetClip(clip, out VisualClipDefinition found))
        {
            return new ClipResolution(found, null);
        }

        string missing = $"{id}/{clip}";
        if (entry.FallbackClip is not null && entry.TryGetClip(entry.FallbackClip, out VisualClipDefinition fallback))
        {
            return new ClipResolution(fallback, missing);
        }

        return new ClipResolution(null, missing);
    }
}
