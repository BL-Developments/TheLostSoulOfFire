using System;
using System.Collections.Generic;
using System.Linq;

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
    Arena
}

/// <summary>
/// Parses the developer start parameters (<c>--dev --start &lt;area&gt; [--wave n]</c>) that let a
/// feature be reached directly instead of playing through title, prologue and hub. Kept free of
/// MonoGame so the command-line rules are testable on their own.
/// </summary>
public sealed record DeveloperStartOptions(DeveloperStartArea Area, int Wave)
{
    public const int MinWave = 1;
    public const int MaxWave = 4;

    private static readonly (string Name, DeveloperStartArea Area)[] Areas =
    [
        ("title", DeveloperStartArea.Title),
        ("prologue", DeveloperStartArea.Prologue),
        ("prologue:find-trace", DeveloperStartArea.PrologueFindTrace),
        ("prologue:search", DeveloperStartArea.PrologueSearch),
        ("prologue:devourer", DeveloperStartArea.PrologueDevourer),
        ("prologue:transit", DeveloperStartArea.PrologueTransit),
        ("hub", DeveloperStartArea.Hub),
        ("arena", DeveloperStartArea.Arena)
    ];

    private static readonly string[] AutomatedTestFlags =
    [
        "--audio-runtime-test",
        "--audio-loop-runtime-test",
        "--audio-gameplay-test",
        "--audio-death-restart-test",
        "--antechamber-visual-test",
        "--currency-visual-test",
        "--expect-audio-fallback"
    ];

    public static IReadOnlyList<string> AreaNames { get; } = Areas.Select(entry => entry.Name).ToArray();

    public string AreaName => Areas.First(entry => entry.Area == Area).Name;

    public static string Usage =>
        "Usage: dotnet run --project src/TheLostSoulOfFire -- --dev [--start <area>] [--wave <1-4>]" + Environment.NewLine +
        $"Areas: {string.Join(", ", AreaNames)}" + Environment.NewLine +
        "--wave is only valid with --start arena.";

    /// <summary>The console line written when a developer start takes effect.</summary>
    public string Describe() => Area == DeveloperStartArea.Arena
        ? $"DEV_START area={AreaName} wave={Wave}"
        : $"DEV_START area={AreaName}";

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

                case "--wave":
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        error = "--wave needs a number.";
                        return false;
                    }
                    waveValue = args[++i];
                    break;
            }
        }

        if (!dev)
        {
            if (startValue is not null || waveValue is not null)
            {
                error = "--start and --wave require --dev.";
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

        options = new DeveloperStartOptions(area, wave);
        return true;
    }
}
