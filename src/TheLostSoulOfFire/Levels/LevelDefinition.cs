using System.Text.Json;
using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Levels;

public sealed class LevelDefinition
{
    public string Name { get; set; } = string.Empty;
    public RectangleData Bounds { get; set; } = new();
    public PointData PlayerSpawn { get; set; } = new();
    public List<RectangleData> Walls { get; set; } = new();
    public RectangleData Portal { get; set; } = new();
    public RectangleData Altar { get; set; } = new();
    public List<PointData> EnemySpawns { get; set; } = new();
}

public sealed class RectangleData
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public Rectangle ToRectangle() => new(X, Y, Width, Height);
}

public sealed class PointData
{
    public float X { get; set; }
    public float Y { get; set; }
    public Vector2 ToVector2() => new(X, Y);
}

public static class LevelDefinitionLoader
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static LevelDefinition Load(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Content", "Levels", fileName);
        using FileStream stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<LevelDefinition>(stream, Options)
            ?? throw new InvalidDataException($"Level definition '{fileName}' is empty.");
    }
}
