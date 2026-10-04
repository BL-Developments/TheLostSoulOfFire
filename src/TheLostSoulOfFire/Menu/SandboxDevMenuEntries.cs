using System;
using System.Collections.Generic;
using System.Linq;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Menu;

/// <summary>The entries of the sandbox dev menu in display order, grouped by section.</summary>
public static class SandboxDevMenuEntries
{
    public const string Health = "health";
    public const string Strength = "strength";
    public const string AbilityPower = "ability-power";
    public const string Armor = "armor";
    public const string ResetCharacter = "reset-character";
    public const string RemoveEnemies = "remove-enemies";
    private const string SpawnPrefix = "spawn:";

    public static IReadOnlyList<DevMenuEntry> All { get; } =
    [
        new(Health, DevMenuSection.Character, DevMenuEntryKind.Value, "LEBEN"),
        new(Strength, DevMenuSection.Character, DevMenuEntryKind.Value, "STÄRKE"),
        new(AbilityPower, DevMenuSection.Character, DevMenuEntryKind.Value, "FÄHIGKEITSSTÄRKE"),
        new(Armor, DevMenuSection.Character, DevMenuEntryKind.Value, "RÜSTUNG"),
        new(ResetCharacter, DevMenuSection.Character, DevMenuEntryKind.Action, "ZURÜCKSETZEN"),
        .. Enum.GetValues<SandboxEnemyKind>().Select(kind =>
            new DevMenuEntry(SpawnPrefix + kind, DevMenuSection.Enemies, DevMenuEntryKind.Action, SandboxSpawner.Label(kind))),
        new(RemoveEnemies, DevMenuSection.Enemies, DevMenuEntryKind.Action, "ALLE GEGNER ENTFERNEN")
    ];

    /// <summary>The enemy type a spawn entry creates, or null for any other entry.</summary>
    public static SandboxEnemyKind? SpawnKind(DevMenuEntry entry) =>
        entry.Id.StartsWith(SpawnPrefix, StringComparison.Ordinal) &&
        Enum.TryParse(entry.Id[SpawnPrefix.Length..], out SandboxEnemyKind kind)
            ? kind
            : null;
}
