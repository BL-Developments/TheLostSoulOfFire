using System.Collections.Generic;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Menu;

/// <summary>
/// What confirming the currently selected entry results in, as seen by the
/// caller driving the arena loop. Navigation within the menu (opening a
/// sub-page, going back) is handled internally and reported as <see cref="None"/>.
/// </summary>
public enum MenuActionResult
{
    None,
    SettingsChanged,
    NewGame,
    Quit,
    Resume,
    QuitToMainMenu
}

/// <summary>
/// Owns a menu's page stack, selection and open/close lifecycle. The same type
/// drives the title menu (root <see cref="MenuPages.Main"/>) and the pause menu
/// (root <see cref="MenuPages.Pause"/>).
/// Deliberately free of any MonoGame input or rendering dependency so it can
/// be driven and tested with plain values.
/// </summary>
public sealed class MenuController
{
    /// <summary>
    /// Seconds a freshly opened menu ignores input for, matching the visual
    /// fade-in of the menu list. Kept here as the single source of truth;
    /// the presentation layer reads it to synchronize the fade with this gate.
    /// </summary>
    public const float RevealDuration = 0.35f;

    private readonly Stack<MenuPage> _pages = new();
    private int _selectedIndex;
    private float _openTimer;

    public GameSettings Settings { get; }

    public MenuController(GameSettings? settings = null) => Settings = settings ?? new GameSettings();

    public bool IsOpen => _pages.Count > 0;
    public MenuPage CurrentPage => _pages.Peek();
    public int SelectedIndex => _selectedIndex;
    public float OpenTimer => _openTimer;
    public bool AcceptsInput => _openTimer >= RevealDuration;

    public MenuPage? RootPage { get; private set; }

    public void Open(MenuPage? root = null)
    {
        RootPage = root ?? MenuPages.Main;
        _pages.Clear();
        _pages.Push(RootPage);
        _selectedIndex = 0;
        _openTimer = 0f;
    }

    /// <summary>Opens the main menu with the quit confirmation already on top.</summary>
    public void OpenQuitConfirmation()
    {
        Open(MenuPages.Main);
        Push(MenuPages.QuitConfirm);
    }

    /// <summary>
    /// Escape inside the menu: sub-pages go back one level, the pause root resumes and
    /// the main root asks before quitting.
    /// </summary>
    public MenuActionResult HandleEscape()
    {
        if (!IsOpen) return MenuActionResult.None;
        if (GoBack()) return MenuActionResult.None;
        if (RootPage == MenuPages.Pause) return MenuActionResult.Resume;
        Push(MenuPages.QuitConfirm);
        return MenuActionResult.None;
    }

    public static bool IsValueEntry(MenuEntryId id) => id is MenuEntryId.OptionalHints or MenuEntryId.Fullscreen
        or MenuEntryId.CameraMotion or MenuEntryId.MasterVolume or MenuEntryId.MusicVolume or MenuEntryId.EffectsVolume;

    public void Close()
    {
        _pages.Clear();
        _openTimer = 0f;
    }

    public void Tick(float deltaTime)
    {
        if (IsOpen)
        {
            _openTimer += deltaTime;
        }
    }

    public void MoveSelection(int delta)
    {
        int count = CurrentPage.Entries.Count;
        _selectedIndex = ((_selectedIndex + delta) % count + count) % count;
    }

    public void SetHoverIndex(int index)
    {
        if (index >= 0 && index < CurrentPage.Entries.Count)
        {
            _selectedIndex = index;
        }
    }

    public MenuActionResult Confirm()
    {
        MenuEntry entry = CurrentPage.Entries[_selectedIndex];
        switch (entry.Id)
        {
            case MenuEntryId.Singleplayer:
                Push(MenuPages.Singleplayer);
                return MenuActionResult.None;
            case MenuEntryId.Settings:
                Push(MenuPages.Settings);
                return MenuActionResult.None;
            case MenuEntryId.Gameplay:
                Push(MenuPages.Gameplay);
                return MenuActionResult.None;
            case MenuEntryId.Graphics:
                Push(MenuPages.Graphics);
                return MenuActionResult.None;
            case MenuEntryId.Audio:
                Push(MenuPages.Audio);
                return MenuActionResult.None;
            case MenuEntryId.Back:
            case MenuEntryId.CancelQuit:
                GoBack();
                return MenuActionResult.None;
            case MenuEntryId.OptionalHints:
            case MenuEntryId.Fullscreen:
            case MenuEntryId.CameraMotion:
            case MenuEntryId.MasterVolume:
            case MenuEntryId.MusicVolume:
            case MenuEntryId.EffectsVolume:
                AdjustSelectedValue(1);
                return MenuActionResult.SettingsChanged;
            case MenuEntryId.NewGame:
                return MenuActionResult.NewGame;
            case MenuEntryId.Quit:
                Push(MenuPages.QuitConfirm);
                return MenuActionResult.None;
            case MenuEntryId.ConfirmQuit:
            case MenuEntryId.QuitToDesktop:
                return MenuActionResult.Quit;
            case MenuEntryId.Resume:
                return MenuActionResult.Resume;
            case MenuEntryId.PauseQuit:
                Push(MenuPages.PauseQuit);
                return MenuActionResult.None;
            case MenuEntryId.QuitToMainMenu:
                return MenuActionResult.QuitToMainMenu;
            default:
                return MenuActionResult.None;
        }
    }

    public bool IsSettingsPage => IsOpen && CurrentPage.Id.StartsWith("settings", System.StringComparison.Ordinal);

    public bool GoBack()
    {
        if (_pages.Count <= 1) return false;
        _pages.Pop();
        _selectedIndex = 0;
        return true;
    }

    public bool AdjustSelectedValue(int direction)
    {
        MenuEntryId id = CurrentPage.Entries[_selectedIndex].Id;
        switch (id)
        {
            case MenuEntryId.OptionalHints: Settings.OptionalHints = !Settings.OptionalHints; break;
            case MenuEntryId.Fullscreen: Settings.Fullscreen = !Settings.Fullscreen; break;
            case MenuEntryId.CameraMotion:
                Settings.CameraMotion = (CameraMotionLevel)(((int)Settings.CameraMotion + (direction > 0 ? 1 : 2)) % 3);
                break;
            case MenuEntryId.MasterVolume: Settings.MasterVolume = System.Math.Clamp(Settings.MasterVolume + direction * 10, 0, 100); break;
            case MenuEntryId.MusicVolume: Settings.MusicVolume = System.Math.Clamp(Settings.MusicVolume + direction * 10, 0, 100); break;
            case MenuEntryId.EffectsVolume: Settings.EffectsVolume = System.Math.Clamp(Settings.EffectsVolume + direction * 10, 0, 100); break;
            default: return false;
        }
        return true;
    }

    public string GetLabel(MenuEntry entry) => entry.Id switch
    {
        MenuEntryId.OptionalHints => $"OPTIONALE HINWEISE: {(Settings.OptionalHints ? "AN" : "AUS")}",
        MenuEntryId.Fullscreen => $"VOLLBILD: {(Settings.Fullscreen ? "AN" : "AUS")}",
        MenuEntryId.CameraMotion => $"BILDBEWEGUNG: {Settings.CameraMotion.ToString().ToUpperInvariant()}",
        MenuEntryId.MasterVolume => $"GESAMTLAUTSTÄRKE: {Settings.MasterVolume}%",
        MenuEntryId.MusicVolume => $"MUSIK: {Settings.MusicVolume}%",
        MenuEntryId.EffectsVolume => $"EFFEKTE: {Settings.EffectsVolume}%",
        _ => entry.Label
    };

    private void Push(MenuPage page)
    {
        _pages.Push(page);
        _selectedIndex = 0;
    }
}
