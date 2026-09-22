using System.Collections.Generic;

namespace TheLostSoulOfFire.Menu;

/// <summary>
/// What confirming the currently selected entry results in, as seen by the
/// caller driving the arena loop. Navigation within the menu (opening a
/// sub-page, going back) is handled internally and reported as <see cref="None"/>.
/// </summary>
public enum MenuActionResult
{
    None,
    NewGame,
    Quit
}

/// <summary>
/// Owns the main menu's page stack, selection and open/close lifecycle.
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

    public bool IsOpen => _pages.Count > 0;
    public MenuPage CurrentPage => _pages.Peek();
    public int SelectedIndex => _selectedIndex;
    public float OpenTimer => _openTimer;
    public bool AcceptsInput => _openTimer >= RevealDuration;

    public void Open()
    {
        _pages.Clear();
        _pages.Push(MenuPages.Main);
        _selectedIndex = 0;
        _openTimer = 0f;
    }

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
            case MenuEntryId.Back:
                Pop();
                return MenuActionResult.None;
            case MenuEntryId.NewGame:
                return MenuActionResult.NewGame;
            case MenuEntryId.Quit:
                return MenuActionResult.Quit;
            default:
                return MenuActionResult.None;
        }
    }

    private void Push(MenuPage page)
    {
        _pages.Push(page);
        _selectedIndex = 0;
    }

    private void Pop()
    {
        if (_pages.Count > 1)
        {
            _pages.Pop();
            _selectedIndex = 0;
        }
    }
}
