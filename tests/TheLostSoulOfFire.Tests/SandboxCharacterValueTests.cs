using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Menu;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class SandboxCharacterValueTests
{
    [TestMethod]
    public void Attribute_StepsByOneOrTen_AndClampsToAttributeRange()
    {
        DevValueRange range = DevValueRange.Attribute;
        Assert.AreEqual(11, range.Adjust(10, 1, largeStep: false));
        Assert.AreEqual(20, range.Adjust(10, 1, largeStep: true));
        Assert.AreEqual(PlayerAttributes.MinValue, range.Adjust(3, -1, largeStep: true));
        Assert.AreEqual(PlayerAttributes.MaxValue, range.Adjust(95, 1, largeStep: true));
    }

    [TestMethod]
    public void Health_StepsByTenOrHundred_AndStaysBetweenOneAnd999()
    {
        DevValueRange range = DevValueRange.Health;
        Assert.AreEqual(110, range.Adjust(100, 1, largeStep: false));
        Assert.AreEqual(200, range.Adjust(100, 1, largeStep: true));
        Assert.AreEqual(1, range.Adjust(10, -1, largeStep: false));
        Assert.AreEqual(1, range.Adjust(1, -1, largeStep: true));
        Assert.AreEqual(999, range.Adjust(950, 1, largeStep: true));
    }

    [TestMethod]
    public void SetMaxHealth_FillsPlayer_AndSurvivesReset()
    {
        Player player = new(Vector2.Zero);
        Assert.AreEqual(GameBalance.PlayerMaxHealth, player.MaxHealth);

        player.SetMaxHealth(250);
        Assert.AreEqual(250, player.MaxHealth);
        Assert.AreEqual(250, player.Health);

        player.Reset(Vector2.One);
        Assert.AreEqual(250, player.Health);

        player.SetMaxHealth(0);
        Assert.AreEqual(1, player.MaxHealth);
    }

    [TestMethod]
    public void CharacterEntries_ComeFirst_InSheetOrder()
    {
        CollectionAssert.AreEqual(
            new[]
            {
                "LEBEN", "STÄRKE", "FÄHIGKEITSSTÄRKE", "RÜSTUNG", "TEMPO", "KERNSCHÄRFE", "FOKUS",
                "STANDFESTIGKEIT", "GEWANDTHEIT", "EINKLANG", "GLÜCK", "ZURÜCKSETZEN"
            },
            SandboxDevMenuEntries.All.Where(entry => entry.Section == DevMenuSection.Character).Select(entry => entry.Label).ToArray());
        Assert.AreEqual(DevMenuEntryKind.Action, SandboxDevMenuEntries.All.Single(entry => entry.Id == SandboxDevMenuEntries.ResetCharacter).Kind);
    }

    [TestMethod]
    public void LongestValueRow_FitsThePanel()
    {
        // FÄHIGKEITSSTÄRKE plus "- 99 +" is the widest character row.
        int width = PixelText.Measure("FÄHIGKEITSSTÄRKE", 2) + 16 + PixelText.Measure("- 999 +", 2);
        Assert.IsTrue(width < 500 - 48, $"row is {width} wide");
    }
}
