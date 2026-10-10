using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Menu;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class CharacterMenuTests
{
    [TestMethod]
    public void Open_SelectsCharacterTab()
    {
        CharacterMenu menu = new();
        menu.Open();

        Assert.IsTrue(menu.IsOpen);
        Assert.AreEqual(CharacterMenuTab.Character, menu.SelectedTab);
        CollectionAssert.AreEqual(
            new[] { "CHARAKTER", "MAP", "SKILLS", "FÄHIGKEITEN" },
            CharacterMenu.Tabs.Select(CharacterMenu.GetLabel).ToArray());
    }

    [TestMethod]
    public void TabSelection_DoesNotWrapAtEitherEdge()
    {
        CharacterMenu menu = new();
        menu.Open();

        menu.SelectPrevious();
        Assert.AreEqual(CharacterMenuTab.Character, menu.SelectedTab);

        menu.SelectNext();
        Assert.AreEqual(CharacterMenuTab.Map, menu.SelectedTab);
        menu.SelectNext();
        Assert.AreEqual(CharacterMenuTab.Skills, menu.SelectedTab);
        menu.SelectNext();
        Assert.AreEqual(CharacterMenuTab.Abilities, menu.SelectedTab);
        menu.SelectNext();
        Assert.AreEqual(CharacterMenuTab.Abilities, menu.SelectedTab);
    }

    [TestMethod]
    public void Reopen_ReturnsToCharacterTab()
    {
        CharacterMenu menu = new();
        menu.Open();
        menu.Select(CharacterMenuTab.Map);
        menu.Close();

        Assert.IsFalse(menu.IsOpen);
        menu.Open();
        Assert.AreEqual(CharacterMenuTab.Character, menu.SelectedTab);
    }

    [TestMethod]
    public void MapAndSkillsArePlaceholders()
    {
        Assert.IsFalse(CharacterMenu.IsPlaceholder(CharacterMenuTab.Character));
        Assert.IsTrue(CharacterMenu.IsPlaceholder(CharacterMenuTab.Map));
        Assert.IsTrue(CharacterMenu.IsPlaceholder(CharacterMenuTab.Skills));
        Assert.IsFalse(CharacterMenu.IsPlaceholder(CharacterMenuTab.Abilities));
    }

    [TestMethod]
    public void Sheet_StartValues()
    {
        CharacterSheet sheet = new(100, 100, PlayerAttributes.Default, false, 0, 50, 0, 30);

        Assert.AreEqual("100 / 100", sheet.HealthText);
        Assert.AreEqual("WAFFENSCHADEN +0 %", sheet.WeaponDamageText);
        Assert.AreEqual("FÄHIGKEITSSCHADEN +0 %", sheet.AbilityDamageText);
        Assert.AreEqual("SCHADENSVERRINGERUNG 17 %", sheet.ArmorReductionText);
        Assert.AreEqual("ANGRIFFSTEMPO +0 %", sheet.AttackSpeedText);
        Assert.AreEqual("KERNSCHADEN +0 %", sheet.CoreDamageText);
        Assert.AreEqual("ABKLINGTEMPO +0 %", sheet.FocusText);
        Assert.AreEqual("RÜCKSTOSS +0 %", sheet.KnockbackText);
        Assert.AreEqual("AUSWEICHEN +0 % / LAUFEN +0 %", sheet.AgilityText);
        Assert.AreEqual("RESONANZAUFBAU +0 %", sheet.AttunementText);
        Assert.AreEqual("BEUTE +0 %", sheet.LuckText);
    }

    [TestMethod]
    public void Sheet_NewAttributeEffects()
    {
        PlayerAttributes attributes = PlayerAttributes.Default with
        {
            AttackSpeed = 20, CoreSharpness = 14, Focus = 6, Steadiness = 20, Agility = 20, Attunement = 30, Luck = 0
        };

        CharacterSheet sheet = new(100, 100, attributes, false, 0, 0, 0, 0);

        Assert.AreEqual("ANGRIFFSTEMPO +50 %", sheet.AttackSpeedText);
        Assert.AreEqual("KERNSCHADEN +20 %", sheet.CoreDamageText);
        Assert.AreEqual("ABKLINGTEMPO -20 %", sheet.FocusText);
        Assert.AreEqual("RÜCKSTOSS -33 %", sheet.KnockbackText);
        Assert.AreEqual("AUSWEICHEN +50 % / LAUFEN +10 %", sheet.AgilityText);
        Assert.AreEqual("RESONANZAUFBAU +100 %", sheet.AttunementText);
        Assert.AreEqual("BEUTE -50 %", sheet.LuckText);
    }

    [TestMethod]
    public void Sheet_DeveloperValues()
    {
        CharacterSheet sheet = new(87, 100, new PlayerAttributes(20, 6, 0), true, 0, 0, 10, 0);

        Assert.AreEqual("87 / 100", sheet.HealthText);
        Assert.AreEqual("WAFFENSCHADEN +50 %", sheet.WeaponDamageText);
        Assert.AreEqual("FÄHIGKEITSSCHADEN -20 %", sheet.AbilityDamageText);
        Assert.AreEqual("SCHADENSVERRINGERUNG 0 %", sheet.ArmorReductionText);
    }

    [TestMethod]
    public void Sheet_ShowsRunBalancesOnlyInRun()
    {
        CharacterSheet inRun = new(100, 100, PlayerAttributes.Default, true, 25, 50, 19, 30);
        CharacterSheet outside = inRun with { InRun = false };

        Assert.AreEqual("IM LAUF 25", inRun.GeldRunText);
        Assert.AreEqual("GESICHERT 50", inRun.GeldSecuredText);
        Assert.AreEqual("IM LAUF 19", inRun.GlutRunText);
        Assert.AreEqual("GESICHERT 30", inRun.GlutSecuredText);
        Assert.IsNull(outside.GeldRunText);
        Assert.IsNull(outside.GlutRunText);
        Assert.AreEqual("GESICHERT 50", outside.GeldSecuredText);
    }

    [TestMethod]
    public void SheetText_IsRenderableByPixelText()
    {
        CharacterSheet sheet = new(87, 100, new PlayerAttributes(20, 6, 0), true, 25, 50, 19, 30);
        string[] texts =
        [
            sheet.HealthText, sheet.WeaponDamageText, sheet.AbilityDamageText, sheet.ArmorReductionText,
            sheet.AttackSpeedText, sheet.CoreDamageText, sheet.FocusText, sheet.KnockbackText, sheet.AgilityText,
            sheet.AttunementText, sheet.LuckText, "TEMPO", "KERNSCHÄRFE", "FOKUS", "STANDFESTIGKEIT", "GEWANDTHEIT", "EINKLANG", "GLÜCK",
            sheet.GeldRunText!, sheet.GeldSecuredText, sheet.GlutRunText!, sheet.GlutSecuredText, "LEBEN", "STÄRKE", "FÄHIGKEITSSTÄRKE", "RÜSTUNG", "WÄHRUNGEN",
            "NOCH NICHT VERFÜGBAR", "CHARAKTER", "MAP", "SKILLS", "FÄHIGKEITEN"
        ];

        foreach (char character in string.Concat(texts).Where(character => character != ' '))
        {
            Assert.IsTrue(PixelText.CanRender(character), $"PixelText cannot render '{character}'.");
        }
    }

    [TestMethod]
    public void TabHitTargets_FitTheVirtualScreenWithoutOverlap()
    {
        Rectangle screen = new(0, 0, 1280, 720);
        IReadOnlyList<Rectangle> bounds = new CinematicPresentation().GetCharacterTabBounds(new Viewport(screen));

        Assert.AreEqual(CharacterMenu.Tabs.Count, bounds.Count);
        for (int i = 0; i < bounds.Count; i++)
        {
            Assert.IsTrue(screen.Contains(bounds[i]), $"Tab {i} hit target extends past the virtual screen.");
            if (i > 0)
            {
                Assert.IsFalse(bounds[i].Intersects(bounds[i - 1]), $"Tabs {i - 1} and {i} overlap.");
                Assert.IsTrue(bounds[i].Left > bounds[i - 1].Left);
            }
        }
    }
    [TestMethod]
    public void SkillSelection_ChangesTargetSlotAndRejectsDuplicatesAndCombatChanges()
    {
        var menu = new CharacterMenu();
        var abilities = new RunAbilities();
        menu.Open();
        menu.Select(CharacterMenuTab.Abilities);
        menu.SelectSkillSlot(1);
        Assert.IsTrue(menu.EquipSkill(abilities, RunAbility.Vortex, true));
        Assert.AreEqual(RunAbility.Vortex, abilities.Slots[1]);
        Assert.AreEqual(RunAbility.SecondWind, abilities.Slots[0]);
        menu.SelectSkillSlot(0);
        Assert.IsFalse(menu.EquipSkill(abilities, RunAbility.Vortex, true));
        Assert.IsTrue(menu.SkillFeedback.Contains("ANDEREN SLOT"));
        Assert.AreEqual(RunAbility.SecondWind, abilities.Slots[0]);
        Assert.IsFalse(menu.EquipSkill(abilities, RunAbility.Revenge, false));
        Assert.AreEqual(RunAbility.SecondWind, abilities.Slots[0]);
        Assert.IsTrue(menu.SkillFeedback.Contains("WECHSEL NUR"));
        Assert.IsTrue(menu.EquipSkill(abilities, RunAbility.Revenge, true));
        Assert.AreEqual(RunAbility.Revenge, abilities.Slots[0]);
        menu.Select(CharacterMenuTab.Skills);
        Assert.IsFalse(menu.EquipSkill(abilities, RunAbility.Setup, true));
        menu.Select(CharacterMenuTab.Character);
        Assert.IsFalse(menu.EquipSkill(abilities, RunAbility.Setup, true));
        menu.Close();
        menu.Open();
        Assert.AreEqual(0, menu.SelectedSkillSlot);
        Assert.AreEqual("", menu.SkillFeedback);
    }

}
