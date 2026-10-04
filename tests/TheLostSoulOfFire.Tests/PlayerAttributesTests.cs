using TheLostSoulOfFire.Combat;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class PlayerAttributesTests
{
    [TestMethod]
    public void Baseline_KeepsTunedOutgoingDamage()
    {
        PlayerAttributes attributes = PlayerAttributes.Default;

        Assert.AreEqual(40, attributes.ScaleWeaponDamage(40));
        Assert.AreEqual(68, attributes.ScaleAbilityDamage(68));
    }

    [TestMethod]
    public void Strength_ScalesOnlyWeaponDamage()
    {
        PlayerAttributes attributes = PlayerAttributes.Default with { Strength = 20 };

        Assert.AreEqual(60, attributes.ScaleWeaponDamage(40));
        Assert.AreEqual(68, attributes.ScaleAbilityDamage(68));
    }

    [TestMethod]
    public void AbilityPower_ScalesOnlyAbilityDamage()
    {
        PlayerAttributes attributes = PlayerAttributes.Default with { AbilityPower = 14 };

        Assert.AreEqual(82, attributes.ScaleAbilityDamage(68));
        Assert.AreEqual(40, attributes.ScaleWeaponDamage(40));
    }

    [TestMethod]
    public void LowAttributes_NeverDropDamageBelowOne()
    {
        PlayerAttributes attributes = new(0, 0, 0);

        Assert.AreEqual(10, attributes.ScaleWeaponDamage(20));
        Assert.AreEqual(1, attributes.ScaleAbilityDamage(1));
    }

    [TestMethod]
    [DataRow(0, 16, 16)]
    [DataRow(10, 16, 13)]
    [DataRow(10, 64, 53)]
    [DataRow(50, 24, 12)]
    [DataRow(99, 1, 1)]
    public void Armor_ReducesIncomingDamage(int armor, int incoming, int expected)
    {
        PlayerAttributes attributes = PlayerAttributes.Default with { Armor = armor };

        Assert.AreEqual(expected, attributes.MitigateIncomingDamage(incoming));
    }

    [TestMethod]
    public void Armor_IgnoresHitsWithoutDamage()
    {
        Assert.AreEqual(0, PlayerAttributes.Default.MitigateIncomingDamage(0));
    }
}
