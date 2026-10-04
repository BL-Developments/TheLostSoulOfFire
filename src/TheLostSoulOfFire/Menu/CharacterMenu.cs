using System.Collections.Generic;

namespace TheLostSoulOfFire.Menu;

public enum CharacterMenuTab
{
    Character,
    Map,
    Skills
}

/// <summary>
/// Open state and selected tab of the character menu opened with Tab. Like
/// <see cref="MenuController"/> it is free of MonoGame input and rendering so it can be
/// driven and tested with plain values; <c>GameWorld</c> maps keys and clicks onto it.
/// </summary>
public sealed class CharacterMenu
{
    public static IReadOnlyList<CharacterMenuTab> Tabs { get; } =
        [CharacterMenuTab.Character, CharacterMenuTab.Map, CharacterMenuTab.Skills];

    private float _openTimer;

    public bool IsOpen { get; private set; }
    public CharacterMenuTab SelectedTab { get; private set; }
    public float OpenTimer => _openTimer;

    public static string GetLabel(CharacterMenuTab tab) => tab switch
    {
        CharacterMenuTab.Character => "CHARAKTER",
        CharacterMenuTab.Map => "MAP",
        _ => "SKILLS"
    };

    /// <summary>Map and Skills are selectable but have no content yet.</summary>
    public static bool IsPlaceholder(CharacterMenuTab tab) => tab != CharacterMenuTab.Character;

    /// <summary>Opens on the character tab every time.</summary>
    public void Open()
    {
        IsOpen = true;
        SelectedTab = CharacterMenuTab.Character;
        _openTimer = 0f;
    }

    public void Close() => IsOpen = false;

    public void Tick(float deltaTime)
    {
        if (IsOpen) _openTimer += deltaTime;
    }

    /// <summary>Selects the next tab; stays on the last one instead of wrapping.</summary>
    public void SelectNext() => Move(1);

    /// <summary>Selects the previous tab; stays on the first one instead of wrapping.</summary>
    public void SelectPrevious() => Move(-1);

    public void Select(CharacterMenuTab tab) => SelectedTab = tab;

    private void Move(int direction)
    {
        int index = (int)SelectedTab + direction;
        if (index >= 0 && index < Tabs.Count)
        {
            SelectedTab = Tabs[index];
        }
    }
}
