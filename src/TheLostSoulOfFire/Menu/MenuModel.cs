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
    EffectsVolume,
    Resume,
    PauseQuit,
    QuitToMainMenu,
    QuitToDesktop,
    ConfirmQuit,
    CancelQuit,
    BestManQuestion,
    AcceptBestMan,
    RefuseBestMan
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
    public MenuPage(string id, IReadOnlyList<MenuEntry> entries, string? prompt = null)
    {
        Id = id;
        Entries = entries;
        Prompt = prompt;
    }

    /// <summary>Stable identifier used for screenshot/debug context, not for display.</summary>
    public string Id { get; }
    public IReadOnlyList<MenuEntry> Entries { get; }

    /// <summary>Optional question drawn above the entries, e.g. on the quit confirmation.</summary>
    public string? Prompt { get; }
}

/// <summary>
/// The fixed page content for the main and pause menus. Defined once and shared by
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
        new MenuEntry(MenuEntryId.BestManQuestion, "EINE FRAGE AN LEO"),
        new MenuEntry(MenuEntryId.Back, "ZURÜCK")
    });

    // NEIN only exists to be dodged: the selection slides back to JA (see MenuController).
    public static readonly MenuPage BestManQuestion = new("best_man_question", new[]
    {
        new MenuEntry(MenuEntryId.AcceptBestMan, "JA"),
        new MenuEntry(MenuEntryId.RefuseBestMan, "NEIN")
    }, prompt: "LEO, MÖCHTEST DU MEIN TRAUZEUGE SEIN?");

    /// <summary>Lines the game answers each refusal attempt with, in order; the last one stays.</summary>
    public static readonly string[] BestManRefusalRemarks =
    {
        "BIST DU SICHER?",
        "DIE SEELE DES FEUERS SAGT JA.",
        "NEIN IST GERADE AUSVERKAUFT.",
        "DIESE OPTION IST IN DEINER REGION NICHT VERFÜGBAR."
    };

    public static readonly MenuPage BestManThanks = new("best_man_thanks", new[]
    {
        new MenuEntry(MenuEntryId.Back, "ZURÜCK")
    }, prompt: "DANKE, LEO! ICH FREUE MICH RIESIG. - BJÖRN");

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

    public static readonly MenuPage QuitConfirm = new("quit_confirm", new[]
    {
        new MenuEntry(MenuEntryId.ConfirmQuit, "JA"),
        new MenuEntry(MenuEntryId.CancelQuit, "NEIN")
    }, prompt: "SOLL DAS SPIEL WIRKLICH BEENDET WERDEN?");

    public static readonly MenuPage Pause = new("pause", new[]
    {
        new MenuEntry(MenuEntryId.Resume, "FORTSETZEN"),
        new MenuEntry(MenuEntryId.Settings, "EINSTELLUNGEN"),
        new MenuEntry(MenuEntryId.Achievements, "ERRUNGENSCHAFTEN UND STATISTIKEN", isPlaceholder: true),
        new MenuEntry(MenuEntryId.PauseQuit, "BEENDEN")
    });

    public static readonly MenuPage PauseQuit = new("pause_quit", new[]
    {
        new MenuEntry(MenuEntryId.QuitToMainMenu, "ZURÜCK ZUM HAUPTMENÜ"),
        new MenuEntry(MenuEntryId.QuitToDesktop, "ZURÜCK ZUM DESKTOP"),
        new MenuEntry(MenuEntryId.Back, "ZURÜCK")
    });
}
