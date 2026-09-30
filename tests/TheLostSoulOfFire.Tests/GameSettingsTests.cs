using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class GameSettingsTests
{
    [TestMethod]
    public void Load_MissingOrInvalidFile_ReturnsDefaults()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        GameSettingsStore store = new(Path.Combine(directory, "settings.json"));
        Assert.AreEqual(100, store.Load().MasterVolume);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "settings.json"), "not json");
        Assert.AreEqual(100, store.Load().MasterVolume);
        Directory.Delete(directory, true);
    }

    [TestMethod]
    public void SaveAndLoad_PreserveSettings_AndClampOutOfRangeValues()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        GameSettingsStore store = new(Path.Combine(directory, "settings.json"));
        GameSettings settings = new() { Fullscreen = true, MusicVolume = 140, EffectsVolume = -5 };
        store.Save(settings);
        GameSettings loaded = store.Load();
        Assert.IsTrue(loaded.Fullscreen);
        Assert.AreEqual(100, loaded.MusicVolume);
        Assert.AreEqual(100, loaded.EffectsVolume);
        Directory.Delete(directory, true);
    }

    [TestMethod]
    public void Save_UnwritablePath_DoesNotThrow()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "blocker"), "file");
        GameSettingsStore store = new(Path.Combine(directory, "blocker", "settings.json"));
        store.Save(new GameSettings());
        Directory.Delete(directory, true);
    }
}
