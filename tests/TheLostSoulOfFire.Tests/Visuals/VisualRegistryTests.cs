using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Tests.Visuals;

[TestClass]
public sealed class VisualRegistryTests
{
    private const string ValidEntry = """
        {
          "id": "enemy.test",
          "kind": "character",
          "palette": "world",
          "worldSize": [112, 112],
          "fallbackClip": "idle",
          "clips": {
            "idle": { "path": "Textures/Test/idle/{dir}", "frameSize": [128, 128], "frames": 9, "fps": 8, "loop": true },
            "move": { "path": "Textures/Test/move/{dir}", "frameSize": [128, 128], "frames": 9, "fps": 12, "loop": true,
                      "progress": "distance", "cycleDistance": 180, "normalMap": "Textures/Test/move_n/{dir}" }
          },
          "dissolve": { "duration": 0.8 }
        }
        """;

    private static string Registry(params string[] entries) =>
        $$"""{ "version": 1, "visuals": [ {{string.Join(",", entries)}} ] }""";

    [TestMethod]
    public void CheckedInRegistry_Loads_AndCoversEveryCodeId()
    {
        VisualRegistry registry = VisualRegistry.Parse(File.ReadAllText(RepositoryPaths.Registry));

        string[] missing = VisualIds.All.Where(id => !registry.TryGet(id, out _)).ToArray();
        Assert.AreEqual(0, missing.Length, "Code-IDs ohne Registry-Eintrag: " + string.Join(", ", missing));
    }

    [TestMethod]
    public void ValidRegistry_ParsesAllFields()
    {
        VisualRegistry registry = VisualRegistry.Parse(Registry(ValidEntry));

        Assert.IsTrue(registry.TryGet("enemy.test", out VisualEntry entry));
        Assert.AreEqual(VisualKind.Character, entry.Kind);
        Assert.AreEqual(VisualPalette.World, entry.Palette);
        Assert.AreEqual(new Vector2(112f), entry.WorldSize);
        Assert.AreEqual(new Vector2(0.5f), entry.Origin, "origin defaults to the frame centre");
        Assert.AreEqual("idle", entry.FallbackClip);
        Assert.AreEqual(0.8f, entry.Dissolve!.Duration, 0.0001f);

        VisualClipDefinition move = entry.Clips["move"];
        Assert.IsTrue(move.IsDirectional);
        Assert.AreEqual("Textures/Test/move/sw", move.PathFor("sw"));
        Assert.AreEqual("Textures/Test/move_n/sw", move.NormalMapFor("sw"));
        Assert.AreEqual(ClipProgress.Distance, move.Progress);
        Assert.AreEqual(180f, move.CycleDistance);
        Assert.AreEqual(ClipProgress.Time, entry.Clips["idle"].Progress);
        Assert.AreEqual(9f / 8f, entry.Clips["idle"].Duration, 0.0001f);
    }

    [TestMethod]
    [DataRow("\"frames\": 9", "\"frames\": 0", "clips.idle.frames")]
    [DataRow("\"kind\": \"character\"", "\"kind\": \"monster\"", "kind")]
    [DataRow("\"Textures/Test/idle/{dir}\"", "\"Textures/Test/idle\"", "clips.idle.path")]
    [DataRow("\"worldSize\": [112, 112]", "\"worldSize\": [112]", "worldSize")]
    [DataRow("\"fallbackClip\": \"idle\"", "\"fallbackClip\": \"dash\"", "fallbackClip")]
    [DataRow("\"cycleDistance\": 180", "\"cycleDistance\": 0", "clips.move.cycleDistance")]
    [DataRow("\"fps\": 8", "\"fps\": \"fast\"", "clips.idle.fps")]
    [DataRow("\"palette\": \"world\"", "\"palette\": \"orange\"", "palette")]
    public void InvalidEntry_NamesIdAndField(string valid, string broken, string field)
    {
        string json = Registry(ValidEntry.Replace(valid, broken));

        VisualRegistryException exception = Assert.ThrowsException<VisualRegistryException>(() => VisualRegistry.Parse(json));

        StringAssert.Contains(exception.Message, "'enemy.test'");
        StringAssert.Contains(exception.Message, $"Feld '{field}'");
    }

    [TestMethod]
    public void DuplicateIds_AreRejected()
    {
        VisualRegistryException exception = Assert.ThrowsException<VisualRegistryException>(
            () => VisualRegistry.Parse(Registry(ValidEntry, ValidEntry)));

        StringAssert.Contains(exception.Message, "'enemy.test' Feld 'id'");
    }

    [TestMethod]
    public void EveryProblem_IsReportedAtOnce()
    {
        string json = Registry(ValidEntry.Replace("\"frames\": 9", "\"frames\": 0").Replace("\"fps\": 12", "\"fps\": -1"));

        VisualRegistryException exception = Assert.ThrowsException<VisualRegistryException>(() => VisualRegistry.Parse(json));

        StringAssert.Contains(exception.Message, "clips.idle.frames");
        StringAssert.Contains(exception.Message, "clips.move.frames");
        StringAssert.Contains(exception.Message, "clips.move.fps");
    }

    [TestMethod]
    public void BrokenJson_IsReportedAsRegistryError()
    {
        Assert.ThrowsException<VisualRegistryException>(() => VisualRegistry.Parse("{ \"visuals\": [ "));
    }

    [TestMethod]
    public void Directions_MatchScreenSpaceSectors()
    {
        Assert.AreEqual("e", VisualDirections.FromVector(new Vector2(1f, 0f)));
        Assert.AreEqual("s", VisualDirections.FromVector(new Vector2(0f, 1f)), "y points down on screen");
        Assert.AreEqual("n", VisualDirections.FromVector(new Vector2(0f, -1f)));
        Assert.AreEqual("sw", VisualDirections.FromVector(new Vector2(-1f, 1f)));
        Assert.AreEqual("nw", VisualDirections.FromVector(new Vector2(-1f, -1f)));
        Assert.AreEqual("s", VisualDirections.FromVector(Vector2.Zero), "no aim faces the camera");
        CollectionAssert.AreEquivalent(new[] { "n", "ne", "e", "se", "s", "sw", "w", "nw" }, VisualDirections.All.ToArray());
    }
}

[TestClass]
public sealed class VisualResolverTests
{
    private static readonly VisualRegistry Registry = VisualRegistry.Parse("""
        { "version": 1, "visuals": [
          { "id": "player", "kind": "character", "palette": "world", "worldSize": [100, 100], "fallbackClip": "move",
            "clips": {
              "idle": { "path": "P/idle/{dir}", "frameSize": [128, 128], "frames": 9, "fps": 9, "loop": true },
              "move": { "path": "P/move/{dir}", "frameSize": [128, 128], "frames": 9, "fps": 12, "loop": true } } },
          { "id": "enemy.plain", "kind": "character", "palette": "world", "worldSize": [100, 100],
            "clips": { "idle": { "path": "E/idle/{dir}", "frameSize": [128, 128], "frames": 9, "fps": 9, "loop": true } } }
        ] }
        """);

    [TestMethod]
    public void PresentClip_IsUsed_WithoutReport()
    {
        ClipResolution resolution = VisualResolver.Resolve(Registry, "player", "idle");

        Assert.AreEqual("idle", resolution.Clip!.Name);
        Assert.IsNull(resolution.Missing);
    }

    [TestMethod]
    public void MissingClip_UsesFallbackClip_AndReportsIdSlashClip()
    {
        // Spec: no "dash" clip for player → the run clip is shown and player/dash is listed.
        ClipResolution resolution = VisualResolver.Resolve(Registry, "player", "dash");

        Assert.AreEqual("move", resolution.Clip!.Name);
        Assert.AreEqual("player/dash", resolution.Missing);
        Assert.IsFalse(resolution.IsDummy);
    }

    [TestMethod]
    public void MissingClip_WithoutFallback_IsDummy()
    {
        ClipResolution resolution = VisualResolver.Resolve(Registry, "enemy.plain", "swipe");

        Assert.IsTrue(resolution.IsDummy);
        Assert.AreEqual("enemy.plain/swipe", resolution.Missing);
    }

    [TestMethod]
    public void UnknownId_IsDummy_AndReportsTheId()
    {
        ClipResolution resolution = VisualResolver.Resolve(Registry, "enemy.ash-warden", "idle");

        Assert.IsTrue(resolution.IsDummy);
        Assert.AreEqual("enemy.ash-warden", resolution.Missing);
    }
}
