using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Tests.Visuals;

/// <summary>A parsed <c>art/specs/&lt;id&gt;.md</c>; the format is described in <c>art/specs/README.md</c>.</summary>
internal sealed partial record VisualSpec(
    string Id,
    IReadOnlyDictionary<string, string> Fields,
    IReadOnlyDictionary<string, string> Sections,
    IReadOnlyList<string> Animations,
    IReadOnlyList<string> Effects)
{
    public static readonly string[] RequiredFields = ["Art", "Status", "Stil", "Weltgröße", "Akzentfarbe", "Lore"];
    public static readonly string[] RequiredSections = ["Merkmale", "Silhouette"];
    public static readonly string[] Statuses = ["dummy", "konzept", "freigegeben", "im-spiel"];
    public static readonly string[] Styles = ["ludo", "hausstil"];

    private static readonly Dictionary<string, VisualKind> Kinds = new()
    {
        ["character"] = VisualKind.Character,
        ["effect"] = VisualKind.Effect,
        ["environment"] = VisualKind.Environment,
        ["prop"] = VisualKind.Prop,
        ["sprite"] = VisualKind.Sprite
    };

    [GeneratedRegex(@"^(?<name>[A-Za-zÄÖÜäöüß]+):[ \t]*(?<value>.*)$")]
    private static partial Regex FieldLine();

    [GeneratedRegex(@"^-\s+`(?<name>[^`]+)`")]
    private static partial Regex ListedName();

    [GeneratedRegex(@"\]\((?<target>[^)#]+)(#[^)]*)?\)")]
    private static partial Regex LinkTarget();

    public static VisualSpec Parse(string id, string markdown)
    {
        Dictionary<string, string> fields = new(StringComparer.Ordinal);
        Dictionary<string, List<string>> sections = new(StringComparer.Ordinal);
        List<string>? current = null;
        foreach (string rawLine in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            string line = rawLine.TrimEnd();
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                current = [];
                sections[line[3..].Trim()] = current;
                continue;
            }
            if (current is not null)
            {
                current.Add(line);
                continue;
            }
            Match field = FieldLine().Match(line);
            if (field.Success)
            {
                fields[field.Groups["name"].Value] = field.Groups["value"].Value.Trim();
            }
        }

        return new VisualSpec(
            id,
            fields,
            sections.ToDictionary(pair => pair.Key, pair => string.Join("\n", pair.Value).Trim()),
            ListedNames(sections.GetValueOrDefault("Animationen")),
            ListedNames(sections.GetValueOrDefault("Effekte")));
    }

    private static IReadOnlyList<string> ListedNames(List<string>? lines) =>
        lines is null
            ? []
            : lines.Select(line => ListedName().Match(line)).Where(match => match.Success).Select(match => match.Groups["name"].Value).ToArray();

    public string Field(string name) => Fields.TryGetValue(name, out string? value) ? value : string.Empty;

    /// <summary>
    /// Every problem with this spec, each starting with the Visual-ID. <paramref name="specDirectory"/>
    /// resolves the Lore link; pass <c>null</c> to skip that check.
    /// </summary>
    public IEnumerable<string> Problems(VisualRegistry registry, string? specDirectory)
    {
        foreach (string name in RequiredFields.Where(name => string.IsNullOrWhiteSpace(Field(name))))
        {
            yield return $"{Id}: Feld '{name}' fehlt oder ist leer.";
        }
        foreach (string name in RequiredSections.Where(name => string.IsNullOrWhiteSpace(Sections.GetValueOrDefault(name))))
        {
            yield return $"{Id}: Abschnitt '## {name}' fehlt oder ist leer.";
        }
        if (Animations.Count == 0 && Effects.Count == 0)
        {
            yield return $"{Id}: weder '## Animationen' noch '## Effekte' nennt einen Eintrag (- `name`: …).";
        }

        string status = Field("Status");
        if (status.Length > 0 && !Statuses.Contains(status))
        {
            yield return $"{Id}: Status '{status}' ist ungültig; erlaubt: {string.Join(", ", Statuses)}.";
        }
        string style = Field("Stil");
        if (style.Length > 0 && !Styles.Contains(style))
        {
            yield return $"{Id}: Stil '{style}' ist ungültig; erlaubt: {string.Join(", ", Styles)}.";
        }

        string art = Field("Art");
        bool hasEntry = registry.TryGet(Id, out VisualEntry entry);
        if (art.Length > 0)
        {
            if (!Kinds.TryGetValue(art, out VisualKind kind))
            {
                yield return $"{Id}: Art '{art}' ist ungültig; erlaubt: {string.Join(", ", Kinds.Keys)}.";
            }
            else if (hasEntry && entry.Kind != kind)
            {
                yield return $"{Id}: Art '{art}' passt nicht zur Registry ({entry.Kind}).";
            }
        }

        if (status == "im-spiel")
        {
            if (!hasEntry)
            {
                yield return $"{Id}: Status 'im-spiel', aber die Registry hat keinen Eintrag.";
            }
            else
            {
                string[] missing = Animations.Where(name => !entry.Clips.ContainsKey(name)).ToArray();
                if (missing.Length > 0)
                {
                    yield return $"{Id}: Status 'im-spiel', aber der Registry fehlen die Animationen {string.Join(", ", missing)}.";
                }
            }
        }

        if (specDirectory is not null)
        {
            Match link = LinkTarget().Match(Field("Lore"));
            if (!link.Success)
            {
                yield return $"{Id}: Feld 'Lore' enthält keinen Link auf ein Lore-Blatt.";
            }
            else if (!File.Exists(Path.GetFullPath(Path.Combine(specDirectory, link.Groups["target"].Value))))
            {
                yield return $"{Id}: Lore-Link '{link.Groups["target"].Value}' zeigt auf keine Datei.";
            }
        }
    }
}
