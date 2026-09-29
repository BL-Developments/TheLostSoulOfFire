using System;
using System.IO;
using System.Text.Json;

namespace TheLostSoulOfFire.Game;

public enum CameraMotionLevel
{
    Normal,
    Reduced,
    Off
}

public sealed class GameSettings
{
    public bool OptionalHints { get; set; } = true;
    public bool Fullscreen { get; set; }
    public CameraMotionLevel CameraMotion { get; set; } = CameraMotionLevel.Normal;
    public int MasterVolume { get; set; } = 100;
    public int MusicVolume { get; set; } = 100;
    public int EffectsVolume { get; set; } = 100;

    public void Validate()
    {
        if (!Enum.IsDefined(CameraMotion)) CameraMotion = CameraMotionLevel.Normal;
        if (MasterVolume is < 0 or > 100) MasterVolume = 100;
        if (MusicVolume is < 0 or > 100) MusicVolume = 100;
        if (EffectsVolume is < 0 or > 100) EffectsVolume = 100;
    }
}

public sealed class GameSettingsStore
{
    private readonly string _path;

    public GameSettingsStore(string? path = null) => _path = path ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TheLostSoulOfFire", "settings.json");

    public GameSettings Load()
    {
        try
        {
            GameSettings settings = JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(_path)) ?? new();
            settings.Validate();
            return settings;
        }
        catch
        {
            return new GameSettings();
        }
    }

    public void Save(GameSettings settings)
    {
        string temporaryPath = _path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings));
            File.Move(temporaryPath, _path, true);
        }
        catch
        {
            try { File.Delete(temporaryPath); } catch { }
        }
    }
}
