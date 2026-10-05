using System.Collections.Generic;
using TheLostSoulOfFire.Combat;

namespace TheLostSoulOfFire.Menu;

public enum CharacterMenuTab
{
    Character,
    Map,
    Skills,
    Abilities
}

/// <summary>
/// Open state and selected tab of the character menu opened with Tab. Like
/// <see cref="MenuController"/> it is free of MonoGame input and rendering so it can be
/// driven and tested with plain values; <c>GameWorld</c> maps keys and clicks onto it.
/// </summary>
public sealed class CharacterMenu
{
    public static IReadOnlyList<CharacterMenuTab> Tabs { get; } =
        [CharacterMenuTab.Character, CharacterMenuTab.Map, CharacterMenuTab.Skills, CharacterMenuTab.Abilities];

    private float _openTimer;

    public bool IsOpen { get; private set; }
    public CharacterMenuTab SelectedTab { get; private set; }
    public float OpenTimer => _openTimer;
    public int SelectedSkillSlot { get; private set; }
    public string SkillFeedback { get; private set; } = "";

    public void SelectSkillSlot(int slot)
    {
        if (slot is < 0 or > 1) return;
        SelectedSkillSlot = slot;
        SkillFeedback = "";
    }

    public bool EquipSkill(RunAbilities abilities, RunAbility ability, bool canChoose)
    {
        if (!IsOpen || SelectedTab != CharacterMenuTab.Abilities) return false;
        if (!canChoose)
        {
            SkillFeedback = "WECHSEL NUR IM HUB ODER VOR / ZWISCHEN WELLEN";
            return false;
        }
        if (!abilities.Equip(SelectedSkillSlot, ability))
        {
            SkillFeedback = "BEREITS IM ANDEREN SLOT AUSGERUESTET";
            return false;
        }
        SkillFeedback = $"{RunAbilities.Definitions[(int)ability].Name} AUF {(SelectedSkillSlot == 0 ? "Z" : "X")} AUSGERUESTET";
        return true;
    }

    public static string GetLabel(CharacterMenuTab tab) => tab switch
    {
        CharacterMenuTab.Character => "CHARAKTER",
        CharacterMenuTab.Map => "MAP",
        CharacterMenuTab.Skills => "SKILLS",
        _ => "FÄHIGKEITEN"
    };

    /// <summary>Map and Skills are placeholders.</summary>
    public static bool IsPlaceholder(CharacterMenuTab tab) => tab is CharacterMenuTab.Map or CharacterMenuTab.Skills;

    /// <summary>Opens on the character tab every time.</summary>
    public void Open()
    {
        IsOpen = true;
        SelectedTab = CharacterMenuTab.Character;
        _openTimer = 0f;
        SelectedSkillSlot = 0;
        SkillFeedback = "";
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
