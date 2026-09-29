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
    Back,
    Achievements,
    Gameplay,
    Graphics,
    Audio,
    Controls,
    Accessibility,
    OptionalHints,
    Fullscreen,
    CameraMotion,
    MasterVolume,
    MusicVolume,
    EffectsVolume
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
        new MenuEntry(MenuEntryId.Settings, "EINSTELLUNGEN"),
        new MenuEntry(MenuEntryId.Achievements, "ERRUNGENSCHAFTEN UND STATISTIKEN", isPlaceholder: true),
        new MenuEntry(MenuEntryId.Quit, "BEENDEN")
    });

    public static readonly MenuPage Singleplayer = new("singleplayer", new[]
    {
        new MenuEntry(MenuEntryId.NewGame, "NEUES SPIEL"),
        new MenuEntry(MenuEntryId.LoadGame, "SPIEL LADEN", isPlaceholder: true),
        new MenuEntry(MenuEntryId.Back, "ZURÜCK")
    });

    public static readonly MenuPage Settings = new("settings", new[]
    {
        new MenuEntry(MenuEntryId.Gameplay, "GAMEPLAY"),
        new MenuEntry(MenuEntryId.Graphics, "GRAFIK"),
        new MenuEntry(MenuEntryId.Audio, "AUDIO"),
        new MenuEntry(MenuEntryId.Controls, "STEUERUNG", isPlaceholder: true),
        new MenuEntry(MenuEntryId.Accessibility, "BARRIEREFREIHEIT", isPlaceholder: true),
        new MenuEntry(MenuEntryId.Back, "ZURÜCK")
    });

    public static readonly MenuPage Gameplay = new("settings_gameplay", new[]
    {
        new MenuEntry(MenuEntryId.OptionalHints, "OPTIONALE HINWEISE"),
        new MenuEntry(MenuEntryId.Back, "ZURÜCK")
    });

    public static readonly MenuPage Graphics = new("settings_graphics", new[]
    {
        new MenuEntry(MenuEntryId.Fullscreen, "VOLLBILD"),
        new MenuEntry(MenuEntryId.CameraMotion, "BILDBEWEGUNG"),
        new MenuEntry(MenuEntryId.Back, "ZURÜCK")
    });

    public static readonly MenuPage Audio = new("settings_audio", new[]
    {
        new MenuEntry(MenuEntryId.MasterVolume, "GESAMTLAUTSTÄRKE"),
        new MenuEntry(MenuEntryId.MusicVolume, "MUSIK"),
        new MenuEntry(MenuEntryId.EffectsVolume, "EFFEKTE"),
        new MenuEntry(MenuEntryId.Back, "ZURÜCK")
    });
}
