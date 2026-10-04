using System.Collections.Generic;

namespace TheLostSoulOfFire.Menu;

/// <summary>The entries of the sandbox dev menu in display order, grouped by section.</summary>
public static class SandboxDevMenuEntries
{
    public const string Health = "health";
    public const string Strength = "strength";
    public const string AbilityPower = "ability-power";
    public const string Armor = "armor";
    public const string ResetCharacter = "reset-character";

    public static IReadOnlyList<DevMenuEntry> All { get; } =
    [
        new(Health, DevMenuSection.Character, DevMenuEntryKind.Value, "LEBEN"),
        new(Strength, DevMenuSection.Character, DevMenuEntryKind.Value, "STÄRKE"),
        new(AbilityPower, DevMenuSection.Character, DevMenuEntryKind.Value, "FÄHIGKEITSSTÄRKE"),
        new(Armor, DevMenuSection.Character, DevMenuEntryKind.Value, "RÜSTUNG"),
        new(ResetCharacter, DevMenuSection.Character, DevMenuEntryKind.Action, "ZURÜCKSETZEN")
    ];
}
