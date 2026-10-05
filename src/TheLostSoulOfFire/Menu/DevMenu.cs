using System;
using System.Collections.Generic;
using System.Linq;

namespace TheLostSoulOfFire.Menu;

public enum DevMenuSection
{
    Character,
    Enemies
}

public enum DevMenuEntryKind
{
    /// <summary>Changed with Left/Right; Enter does nothing.</summary>
    Value,
    /// <summary>Runs once on Enter or click.</summary>
    Action
}

public sealed record DevMenuEntry(string Id, DevMenuSection Section, DevMenuEntryKind Kind, string Label);

/// <summary>
/// Open state and selection of the sandbox dev menu opened with F (#104). Like
/// <see cref="CharacterMenu"/> it is free of MonoGame input and rendering; <c>GameWorld</c>
/// maps keys and clicks onto it and decides what an entry does.
/// </summary>
public sealed class DevMenu
{
    public static IReadOnlyList<DevMenuSection> Sections { get; } = [DevMenuSection.Character, DevMenuSection.Enemies];

    private float _openTimer;

    /// <param name="entries">Entries in display order; they must be grouped by <see cref="Sections"/>.</param>
    public DevMenu(IReadOnlyList<DevMenuEntry> entries)
    {
        Entries = Sections.SelectMany(section => entries.Where(entry => entry.Section == section)).ToArray();
        if (Entries.Count != entries.Count || !Entries.SequenceEqual(entries))
        {
            throw new ArgumentException("Entries must be grouped in section order.", nameof(entries));
        }
    }

    public IReadOnlyList<DevMenuEntry> Entries { get; }
    public bool IsOpen { get; private set; }
    public int SelectedIndex { get; private set; }
    public float OpenTimer => _openTimer;
    public DevMenuEntry? SelectedEntry => Entries.Count > 0 ? Entries[SelectedIndex] : null;

    public static string GetLabel(DevMenuSection section) => section switch
    {
        DevMenuSection.Character => "CHARAKTER",
        _ => "GEGNER"
    };

    public IEnumerable<DevMenuEntry> EntriesIn(DevMenuSection section) => Entries.Where(entry => entry.Section == section);

    /// <summary>Keeps the previous selection, so repeated spawns need no navigation.</summary>
    public void Open()
    {
        IsOpen = true;
        _openTimer = 0f;
    }

    public void Close() => IsOpen = false;

    public void Tick(float deltaTime)
    {
        if (IsOpen) _openTimer += deltaTime;
    }

    /// <summary>Moves through all entries across sections; stays at the ends instead of wrapping.</summary>
    public void MoveSelection(int direction)
    {
        if (Entries.Count > 0)
        {
            SelectedIndex = Math.Clamp(SelectedIndex + Math.Sign(direction), 0, Entries.Count - 1);
        }
    }

    public void Select(int index)
    {
        if (index >= 0 && index < Entries.Count)
        {
            SelectedIndex = index;
        }
    }
}
