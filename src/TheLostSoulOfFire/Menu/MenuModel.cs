using System.Collections.Generic;

namespace TheLostSoulOfFire.Menu;

/// <summary>
/// Identifies the effect a menu entry has when confirmed. Entries not
/// handled explicitly by <see cref="MenuController"/> are placeholders.
/// </summary>
public enum MenuEntryId
{
    Singleplayer,
    Multiplayer,
    Settings,
    Quit,
    NewGame,
    LoadGame,
    Back
}

/// <summary>
/// A single labelled, selectable line on a menu page. Placeholder entries
/// are selectable and confirmable but intentionally produce no effect.
/// </summary>
public sealed class MenuEntry
{
    public MenuEntry(MenuEntryId id, string label, bool isPlaceholder = false)
    {
        Id = id;
        Label = label;
        IsPlaceholder = isPlaceholder;
    }

    public MenuEntryId Id { get; }
    public string Label { get; }
    public bool IsPlaceholder { get; }
}

/// <summary>
/// A page of menu entries. Pages are the units pushed onto and popped from
/// the menu's page stack.
/// </summary>
public sealed class MenuPage
{
    public MenuPage(string id, IReadOnlyList<MenuEntry> entries)
    {
        Id = id;
        Entries = entries;
    }

    /// <summary>Stable identifier used for screenshot/debug context, not for display.</summary>
    public string Id { get; }
    public IReadOnlyList<MenuEntry> Entries { get; }
}

/// <summary>
/// The fixed page content for the main menu. Defined once and shared by
/// every <see cref="MenuController"/> instance.
/// </summary>
public static class MenuPages
{
    public static readonly MenuPage Main = new("main", new[]
    {
        new MenuEntry(MenuEntryId.Singleplayer, "EINZELSPIELER"),
        new MenuEntry(MenuEntryId.Multiplayer, "MEHRSPIELER", isPlaceholder: true),
        new MenuEntry(MenuEntryId.Settings, "EINSTELLUNGEN", isPlaceholder: true),
        new MenuEntry(MenuEntryId.Quit, "BEENDEN")
    });

    public static readonly MenuPage Singleplayer = new("singleplayer", new[]
    {
        new MenuEntry(MenuEntryId.NewGame, "NEUES SPIEL"),
        new MenuEntry(MenuEntryId.LoadGame, "SPIEL LADEN", isPlaceholder: true),
        new MenuEntry(MenuEntryId.Back, "ZURÜCK")
    });
}
