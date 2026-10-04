using System;
using System.IO;
using System.Text.Json;

namespace TheLostSoulOfFire.Game;

/// <summary>Persistent progress. Run balances are never stored, only what was secured.</summary>
public sealed class PlayerProfile
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public int SecuredGeld { get; set; }
    public int SecuredGlut { get; set; }

    public bool IsValid => Version == CurrentVersion && SecuredGeld >= 0 && SecuredGlut >= 0;
}

public sealed class PlayerProfileStore
{
    private readonly string _path;
    private bool _invalidFilePending;

    public PlayerProfileStore(string? path = null) => _path = path ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TheLostSoulOfFire", "profile.json");

    public string InvalidCopyPath => _path + ".invalid";

    /// <summary>
    /// Loads the profile. A missing file yields an empty profile; an unreadable, unknown-version
    /// or negative one also yields an empty profile and is moved aside before the next save.
    /// </summary>
    public PlayerProfile Load()
    {
        _invalidFilePending = false;
        if (!File.Exists(_path))
        {
            return new PlayerProfile();
        }

        try
        {
            PlayerProfile? profile = JsonSerializer.Deserialize<PlayerProfile>(File.ReadAllText(_path));
            if (profile is { IsValid: true })
            {
                return profile;
            }
        }
        catch
        {
        }

        _invalidFilePending = true;
        return new PlayerProfile();
    }

    public bool Save(PlayerProfile profile)
    {
        string temporaryPath = _path + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            if (_invalidFilePending && File.Exists(_path))
            {
                File.Copy(_path, InvalidCopyPath, true);
            }

            _invalidFilePending = false;
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(profile));
            File.Move(temporaryPath, _path, true);
            return true;
        }
        catch
        {
            try { File.Delete(temporaryPath); } catch { }
            return false;
        }
    }
}
