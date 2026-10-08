using System;
using System.Collections.Generic;
using System.Linq;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Tests.Visuals;

[TestClass]
public sealed class AssetCheckTests
{
    [TestMethod]
    public void EveryRegistryGraphic_PassesTheAssetChecks()
    {
        VisualRegistry registry = VisualRegistry.Parse(System.IO.File.ReadAllText(RepositoryPaths.Registry));
        Dictionary<string, ImageData?> cache = [];
        ImageData? Load(string path) => cache.TryGetValue(path, out ImageData? image)
            ? image
            : cache[path] = ImageData.Load(RepositoryPaths.ContentPng(path));

        string[] problems = registry.Entries.SelectMany(entry => AssetChecks.Check(entry, Load)).ToArray();

        Assert.AreEqual(0, problems.Length, "Asset-Prüfungen:\n" + string.Join("\n", problems));
    }

    // ---- Small test images, one per failure case ----

    private static VisualEntry Entry(string json) =>
        VisualRegistry.Parse($$"""{ "version": 1, "visuals": [ {{json}} ] }""").Entries.Single();

    private static ImageData Blank(int width, int height) => new(width, height, new byte[width * height * 4]);

    /// <summary>Fills a centred blob in every frame, leaving a transparent margin.</summary>
    private static ImageData Sheet(int frameSize, int columns, int rows, (byte R, byte G, byte B) color, int margin = 6)
    {
        ImageData image = Blank(frameSize * columns, frameSize * rows);
        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                int fx = x % frameSize, fy = y % frameSize;
                if (fx < margin || fy < margin || fx >= frameSize - margin || fy >= frameSize - margin)
                {
                    continue;
                }
                int index = image.Index(x, y);
                image.Rgba[index] = color.R;
                image.Rgba[index + 1] = color.G;
                image.Rgba[index + 2] = color.B;
                image.Rgba[index + 3] = 255;
            }
        }
        return image;
    }

    private static readonly (byte, byte, byte) Violet = (145, 71, 255);

    private const string Effect = """
        { "id": "fx.test-slash", "kind": "effect", "palette": "death-flame", "worldSize": [64, 64],
          "clips": { "default": { "path": "T/fx_test", "frameSize": [64, 64], "frames": 9, "fps": 24, "loop": false } } }
        """;

    private static string[] ProblemsOf(VisualEntry entry, Func<string, ImageData?> load) => AssetChecks.Check(entry, load).ToArray();

    [TestMethod]
    public void CleanEffectSheet_Passes()
    {
        Assert.AreEqual(0, ProblemsOf(Entry(Effect), _ => Sheet(64, 3, 3, Violet)).Length);
    }

    [TestMethod]
    public void BakedCheckerboard_FailsAsBackgroundPattern()
    {
        ImageData checker = Blank(192, 192);
        for (int y = 0; y < 192; y++)
        {
            for (int x = 0; x < 192; x++)
            {
                byte grey = (x / 8 + y / 8) % 2 == 0 ? (byte)204 : (byte)153;
                int index = checker.Index(x, y);
                checker.Rgba[index] = checker.Rgba[index + 1] = checker.Rgba[index + 2] = grey;
                checker.Rgba[index + 3] = 255;
            }
        }

        string[] problems = ProblemsOf(Entry(Effect), _ => checker);

        Assert.IsTrue(problems.Any(problem => problem.StartsWith("fx.test-slash: T/fx_test.png:") && problem.Contains("Hintergrundmuster")), string.Join("\n", problems));
    }

    [TestMethod]
    public void MissingDirection_NamesIdClipAndDirection()
    {
        VisualEntry player = Entry("""
            { "id": "player", "kind": "character", "palette": "world", "worldSize": [100, 100],
              "clips": { "run": { "path": "P/run/{dir}", "frameSize": [64, 64], "frames": 9, "fps": 12, "loop": true } } }
            """);

        string[] problems = ProblemsOf(player, path => path.EndsWith("/sw") ? null : Sheet(64, 3, 3, (60, 58, 70)));

        Assert.AreEqual(1, problems.Length, string.Join("\n", problems));
        StringAssert.StartsWith(problems[0], "player/run/sw:");
    }

    [TestMethod]
    public void WrongGrid_NamesExpectedAndActualSize()
    {
        // Spec: 9 frames of 256 pixels in a sheet 700 pixels wide.
        VisualEntry entry = Entry(Effect.Replace("[64, 64], \"frames\"", "[256, 256], \"frames\""));

        string[] problems = ProblemsOf(entry, _ => Blank(700, 768));

        Assert.IsTrue(problems.Any(problem => problem.Contains("768×768") && problem.Contains("700×768")), string.Join("\n", problems));
    }

    [TestMethod]
    public void OpaqueFrameEdge_FailsTransparentBorder()
    {
        string[] problems = ProblemsOf(Entry(Effect), _ => Sheet(64, 3, 3, Violet, margin: 0));

        Assert.IsTrue(problems.Any(problem => problem.StartsWith("fx.test-slash:") && problem.Contains("transparenter Rand fehlt")), string.Join("\n", problems));
    }

    [TestMethod]
    public void NormalMap_MustMatchColourSize()
    {
        VisualEntry entry = Entry("""
            { "id": "test.figure", "kind": "sprite", "palette": "world", "worldSize": [64, 64],
              "clips": { "default": { "path": "T/figure", "frameSize": [64, 64], "frames": 1, "fps": 1, "loop": true, "normalMap": "T/figure_n" } } }
            """);

        string[] problems = ProblemsOf(entry, path => path.EndsWith("_n") ? Sheet(32, 1, 1, (128, 128, 255)) : Sheet(64, 1, 1, (90, 90, 90)));

        Assert.IsTrue(problems.Any(problem => problem.Contains("Normal-Map-Maße 32×32") && problem.Contains("64×64")), string.Join("\n", problems));
    }

    [TestMethod]
    public void OrangeScytheSlash_ExceedsDeathFlameBudget()
    {
        string[] problems = ProblemsOf(Entry(Effect), _ => Sheet(64, 3, 3, (255, 140, 40)));

        Assert.IsTrue(problems.Any(problem => problem.StartsWith("fx.test-slash:") && problem.Contains("Farbbudget death-flame") && problem.Contains("100.0 %")), string.Join("\n", problems));
    }

    [TestMethod]
    public void OrangeLifeFlame_Passes()
    {
        VisualEntry lifeFlame = Entry(Effect.Replace("death-flame", "life-flame"));

        Assert.AreEqual(0, ProblemsOf(lifeFlame, _ => Sheet(64, 3, 3, (255, 140, 40))).Length);
    }

    [TestMethod]
    public void MissingFile_IsReported()
    {
        string[] problems = ProblemsOf(Entry(Effect), _ => null);

        CollectionAssert.AreEqual(new[] { "fx.test-slash/default: T/fx_test.png: Datei fehlt." }, problems);
    }
}
