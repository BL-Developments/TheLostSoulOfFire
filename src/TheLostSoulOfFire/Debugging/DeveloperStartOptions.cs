using System;
using System.Collections.Generic;
using System.Linq;
using TheLostSoulOfFire.Combat;

namespace TheLostSoulOfFire.Debugging;

public enum DeveloperStartArea
{
    Title,
    Prologue,
    PrologueFindTrace,
    PrologueSearch,
    PrologueDevourer,
    PrologueTransit,
    Hub,
    Arena,
    Sandbox,
    Level,
    Biome
}

/// <summary>
/// Parses the developer start parameters (<c>--dev --start &lt;area&gt; [--wave n]</c>) that let a
/// feature be reached directly instead of playing through title, prologue and hub. The optional
/// <c>--strength</c>, <c>--ability-power</c>, <c>--armor</c> and the flags of the later attributes
/// (<c>--attack-speed</c>, <c>--luck</c> and so on) override the player's attributes so their
/// scaling can be tried out. The sandbox area is reachable only from here.
/// Kept free of MonoGame so the command-line rules
/// are testable on their own.
/// </summary>
public sealed record DeveloperStartOptions(DeveloperStartArea Area, int Wave, PlayerAttributes? AttributeOverride = null, int? Seed = null, int Level = 1)
{
    public const int MinWave = 1;
    public static int MaxWave => Game.GameBalance.ArenaWaveCount;
    public const int MinLevel = 1;
    public static int MaxLevel => Game.Levels.BiomeCatalog.One.LevelCount;

    private static readonly (string Name, DeveloperStartArea Area)[] Areas =
    [
        ("title", DeveloperStartArea.Title),
        ("prologue", DeveloperStartArea.Prologue),
        ("prologue:find-trace", DeveloperStartArea.PrologueFindTrace),
        ("prologue:search", DeveloperStartArea.PrologueSearch),
        ("prologue:devourer", DeveloperStartArea.PrologueDevourer),
        ("prologue:transit", DeveloperStartArea.PrologueTransit),
        ("hub", DeveloperStartArea.Hub),
        ("arena", DeveloperStartArea.Arena),
        ("sandbox", DeveloperStartArea.Sandbox),
        ("level", DeveloperStartArea.Level),
        ("biome:1", DeveloperStartArea.Biome)
    ];

    private static readonly string[] AutomatedTestFlags =
    [
        "--audio-runtime-test",
        "--audio-loop-runtime-test",
        "--audio-gameplay-test",
        "--audio-death-restart-test",
        "--antechamber-visual-test",
        "--currency-visual-test",
        "--ability-visual-test",
        "--slice-visual-test",
        "--tour-visual-test",
        "--travel-visual-test",
        "--level-visual-test",
        "--biome-visual-test",
        "--expect-audio-fallback"
    ];

    public static IReadOnlyList<string> AreaNames { get; } = Areas.Select(entry => entry.Name).ToArray();

    public string AreaName => Areas.First(entry => entry.Area == Area).Name;

    public static string Usage =>
        "Usage: dotnet run --project src/TheLostSoulOfFire -- --dev [--start <area>] [--wave <1-10>] [--strength <n>] [--ability-power <n>] [--armor <n>] [--seed <n>]" + Environment.NewLine +
        "Further attributes: --attack-speed, --luck, --core-sharpness, --attunement, --agility, --focus, --steadiness <n>" + Environment.NewLine +
        $"Areas: {string.Join(", ", AreaNames)}" + Environment.NewLine +
        "--wave is only valid with --start arena." + Environment.NewLine +
        "--level <1-3> is only valid with --start biome:1." + Environment.NewLine +
        "--seed <integer> is only valid with --start level or biome:1." + Environment.NewLine +
        $"Attributes range from {PlayerAttributes.MinValue} to {PlayerAttributes.MaxValue}; unset ones keep {PlayerAttributes.Baseline}.";

    private static readonly string[] AttributeFlags =
    [
        "--strength", "--ability-power", "--armor",
        "--attack-speed", "--luck", "--core-sharpness", "--attunement", "--agility", "--focus", "--steadiness"
    ];

    /// <summary>The console line written when a developer start takes effect.</summary>
    public string Describe()
    {
        string line = Area switch
        {
            DeveloperStartArea.Arena => $"DEV_START area={AreaName} wave={Wave}",
            DeveloperStartArea.Level when Seed is { } seed => $"DEV_START area={AreaName} seed={seed}",
            DeveloperStartArea.Biome when Seed is { } seed => $"DEV_START area={AreaName} level={Level} seed={seed}",
            DeveloperStartArea.Biome => $"DEV_START area={AreaName} level={Level}",
            _ => $"DEV_START area={AreaName}"
        };
        return AttributeOverride is { } attributes
            ? $"{line} strength={attributes.Strength} ability-power={attributes.AbilityPower} armor={attributes.Armor}{DescribeLaterAttributes(attributes)}"
            : line;
    }

    /// <summary>The later attributes, only those set away from the baseline, so the familiar line stays short.</summary>
    private static string DescribeLaterAttributes(PlayerAttributes attributes)
    {
        int[] values =
        [
            attributes.AttackSpeed, attributes.Luck, attributes.CoreSharpness, attributes.Attunement,
            attributes.Agility, attributes.Focus, attributes.Steadiness
        ];
        string description = "";
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] != PlayerAttributes.Baseline)
            {
                description += $" {AttributeFlags[i + 3][2..]}={values[i]}";
            }
        }
        return description;
    }

    /// <summary>
    /// Returns true when the arguments are valid. <paramref name="options"/> is null when
    /// <c>--dev</c> is absent, so a normal start stays untouched. Arguments unrelated to the
    /// developer mode are ignored.
    /// </summary>
    public static bool TryParse(string[] args, out DeveloperStartOptions? options, out string? error)
    {
        options = null;
        error = null;

        bool dev = false;
        string? startValue = null;
        string? waveValue = null;
        string? seedValue = null;
        string? levelValue = null;
        string?[] attributeValues = new string?[AttributeFlags.Length];

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--dev":
                    dev = true;
                    break;

                case "--start":
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        error = "--start needs an area.";
                        return false;
                    }
                    startValue = args[++i];
                    break;

                case "--seed":
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        error = "--seed needs a number.";
                        return false;
                    }
                    seedValue = args[++i];
                    break;

                case "--wave":
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        error = "--wave needs a number.";
                        return false;
                    }
                    waveValue = args[++i];
                    break;

                case "--level":
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        error = "--level needs a number.";
                        return false;
                    }
                    levelValue = args[++i];
                    break;

                default:
                    int attributeIndex = Array.IndexOf(AttributeFlags, args[i]);
                    if (attributeIndex < 0)
                    {
                        break;
                    }
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        error = $"{args[i]} needs a number.";
                        return false;
                    }
                    attributeValues[attributeIndex] = args[++i];
                    break;
            }
        }

        if (!dev)
        {
            if (startValue is not null || waveValue is not null || seedValue is not null || levelValue is not null || attributeValues.Any(value => value is not null))
            {
                error = "--start, --wave, --seed, --level and attribute flags require --dev.";
                return false;
            }
            return true;
        }

        string? testFlag = AutomatedTestFlags.FirstOrDefault(flag => Array.IndexOf(args, flag) >= 0);
        if (testFlag is not null)
        {
            error = $"--dev cannot be combined with {testFlag}.";
            return false;
        }

        DeveloperStartArea area = DeveloperStartArea.Title;
        if (startValue is not null)
        {
            int index = Array.FindIndex(Areas, entry => string.Equals(entry.Name, startValue, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                error = $"Unknown area '{startValue}'.";
                return false;
            }
            area = Areas[index].Area;
        }

        int wave = MinWave;
        if (waveValue is not null)
        {
            if (area != DeveloperStartArea.Arena)
            {
                error = "--wave is only valid with --start arena.";
                return false;
            }

            if (!int.TryParse(waveValue, out wave) || wave < MinWave || wave > MaxWave)
            {
                error = $"--wave must be between {MinWave} and {MaxWave}, got '{waveValue}'.";
                return false;
            }
        }

        int level = MinLevel;
        if (levelValue is not null)
        {
            if (area != DeveloperStartArea.Biome)
            {
                error = "--level is only valid with --start biome:1.";
                return false;
            }

            if (!int.TryParse(levelValue, out level) || level < MinLevel || level > MaxLevel)
            {
                error = $"--level must be between {MinLevel} and {MaxLevel}, got '{levelValue}'.";
                return false;
            }
        }

        int? seed = null;
        if (seedValue is not null)
        {
            if (area is not (DeveloperStartArea.Level or DeveloperStartArea.Biome))
            {
                error = "--seed is only valid with --start level or biome:1.";
                return false;
            }

            if (!int.TryParse(seedValue, out int parsedSeed))
            {
                error = $"--seed must be a whole number, got '{seedValue}'.";
                return false;
            }

            seed = parsedSeed;
        }

        int[] attributes = Enumerable.Repeat(PlayerAttributes.Baseline, AttributeFlags.Length).ToArray();
        for (int i = 0; i < AttributeFlags.Length; i++)
        {
            string? value = attributeValues[i];
            if (value is null)
            {
                continue;
            }

            if (!int.TryParse(value, out attributes[i]) || attributes[i] < PlayerAttributes.MinValue || attributes[i] > PlayerAttributes.MaxValue)
            {
                error = $"{AttributeFlags[i]} must be between {PlayerAttributes.MinValue} and {PlayerAttributes.MaxValue}, got '{value}'.";
                return false;
            }
        }

        PlayerAttributes? attributeOverride = attributeValues.Any(value => value is not null)
            ? new PlayerAttributes(attributes[0], attributes[1], attributes[2])
            {
                AttackSpeed = attributes[3],
                Luck = attributes[4],
                CoreSharpness = attributes[5],
                Attunement = attributes[6],
                Agility = attributes[7],
                Focus = attributes[8],
                Steadiness = attributes[9]
            }
            : null;

        options = new DeveloperStartOptions(area, wave, attributeOverride, seed, level);
        return true;
    }
}
