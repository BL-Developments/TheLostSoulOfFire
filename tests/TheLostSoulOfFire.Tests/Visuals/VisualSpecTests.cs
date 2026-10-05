using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Tests.Visuals;

[TestClass]
public sealed class VisualSpecTests
{
    private static VisualRegistry CheckedInRegistry => VisualRegistry.Parse(File.ReadAllText(RepositoryPaths.Registry));

    private static string SpecPath(string id) => Path.Combine(RepositoryPaths.Specs, id + ".md");

    [TestMethod]
    public void EveryCodeId_AndEveryRegistryId_HasASpec()
    {
        IEnumerable<string> ids = VisualIds.All.Concat(CheckedInRegistry.Entries.Select(entry => entry.Id)).Distinct();

        string[] missing = ids.Where(id => !File.Exists(SpecPath(id))).OrderBy(id => id).ToArray();

        Assert.AreEqual(0, missing.Length, "Visual-IDs ohne Visual-Spec unter art/specs/: " + string.Join(", ", missing));
    }

    [TestMethod]
    public void EverySpec_IsComplete_AndMatchesTheRegistry()
    {
        VisualRegistry registry = CheckedInRegistry;
        List<string> problems = [];
        foreach (string path in Directory.GetFiles(RepositoryPaths.Specs, "*.md").Where(path => Path.GetFileName(path) != "README.md"))
        {
            string id = Path.GetFileNameWithoutExtension(path);
            VisualSpec spec = VisualSpec.Parse(id, File.ReadAllText(path));
            problems.AddRange(spec.Problems(registry, RepositoryPaths.Specs));
        }

        Assert.AreEqual(0, problems.Count, "Visual-Specs mit Fehlern:\n" + string.Join("\n", problems));
    }

    private const string CompleteSpec = """
        # Visual-Spec: enemy.test

        Art: character
        Status: im-spiel
        Stil: hausstil
        Weltgröße: 112 × 112
        Akzentfarbe: Violett
        Lore: [Figurenblatt](hollow.md)

        ## Merkmale
        - Maske.

        ## Silhouette
        Hoch und schmal.

        ## Animationen
        - `idle`: Ruhe.
        - `swipe`: Telegraph 0,42 s, Treffer 0,13 s.
        """;

    private static readonly VisualRegistry TestRegistry = VisualRegistry.Parse("""
        { "version": 1, "visuals": [
          { "id": "enemy.test", "kind": "character", "palette": "world", "worldSize": [112, 112],
            "clips": {
              "idle": { "path": "T/idle/{dir}", "frameSize": [128, 128], "frames": 9, "fps": 8, "loop": true },
              "swipe": { "path": "T/swipe/{dir}", "frameSize": [128, 128], "frames": 9, "fps": 18, "loop": false } } }
        ] }
        """);

    private static string[] ProblemsOf(string markdown, VisualRegistry? registry = null) =>
        VisualSpec.Parse("enemy.test", markdown).Problems(registry ?? TestRegistry, null).ToArray();

    [TestMethod]
    public void CompleteSpec_HasNoProblems()
    {
        VisualSpec spec = VisualSpec.Parse("enemy.test", CompleteSpec);

        CollectionAssert.AreEqual(new[] { "idle", "swipe" }, spec.Animations.ToArray());
        Assert.AreEqual(0, ProblemsOf(CompleteSpec).Length, string.Join("\n", ProblemsOf(CompleteSpec)));
    }

    [TestMethod]
    [DataRow("Art: character", "Art")]
    [DataRow("Weltgröße: 112 × 112", "Weltgröße")]
    [DataRow("Akzentfarbe: Violett", "Akzentfarbe")]
    [DataRow("Lore: [Figurenblatt](hollow.md)", "Lore")]
    public void DeletedField_FailsWithVisualId(string line, string field)
    {
        string[] problems = ProblemsOf(CompleteSpec.Replace(line, $"{field}:"));

        Assert.IsTrue(problems.Any(problem => problem.StartsWith("enemy.test:") && problem.Contains($"'{field}'")), string.Join("\n", problems));
    }

    [TestMethod]
    public void MissingSection_Fails()
    {
        string[] problems = ProblemsOf(CompleteSpec.Replace("## Silhouette\n        Hoch und schmal.", "").Replace("## Silhouette\nHoch und schmal.", ""));

        Assert.IsTrue(problems.Any(problem => problem.Contains("'## Silhouette'")), string.Join("\n", problems));
    }

    [TestMethod]
    public void InvalidStatus_Fails()
    {
        string[] problems = ProblemsOf(CompleteSpec.Replace("Status: im-spiel", "Status: fertig"));

        Assert.IsTrue(problems.Any(problem => problem.Contains("Status 'fertig' ist ungültig")), string.Join("\n", problems));
    }

    [TestMethod]
    public void InGame_WithAnimationMissingInRegistry_NamesTheAnimation()
    {
        string spec = CompleteSpec.Replace("- `swipe`: Telegraph 0,42 s, Treffer 0,13 s.", "- `swipe`: Telegraph.\n- `death`: Auflösung.");

        string[] problems = ProblemsOf(spec);

        Assert.IsTrue(problems.Any(problem => problem.StartsWith("enemy.test:") && problem.Contains("fehlen die Animationen death")), string.Join("\n", problems));
    }

    [TestMethod]
    public void InGame_WithoutRegistryEntry_Fails_ButDummyPasses()
    {
        string[] inGame = ProblemsOf(CompleteSpec, VisualRegistry.Empty);
        string[] dummy = ProblemsOf(CompleteSpec.Replace("Status: im-spiel", "Status: dummy"), VisualRegistry.Empty);

        Assert.IsTrue(inGame.Any(problem => problem.Contains("keinen Eintrag")), string.Join("\n", inGame));
        Assert.AreEqual(0, dummy.Length, string.Join("\n", dummy));
    }

    [TestMethod]
    public void KindMismatch_WithRegistry_Fails()
    {
        string[] problems = ProblemsOf(CompleteSpec.Replace("Art: character", "Art: effect"));

        Assert.IsTrue(problems.Any(problem => problem.Contains("passt nicht zur Registry")), string.Join("\n", problems));
    }
}
