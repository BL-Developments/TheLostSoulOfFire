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

    [TestMethod]
    public void NewAttributes_StartAtBaseline_AndKeepTunedValues()
    {
        PlayerAttributes attributes = PlayerAttributes.Default;

        Assert.AreEqual(PlayerAttributes.Baseline, attributes.AttackSpeed);
        Assert.AreEqual(PlayerAttributes.Baseline, attributes.Luck);
        Assert.AreEqual(PlayerAttributes.Baseline, attributes.CoreSharpness);
        Assert.AreEqual(PlayerAttributes.Baseline, attributes.Attunement);
        Assert.AreEqual(PlayerAttributes.Baseline, attributes.Agility);
        Assert.AreEqual(PlayerAttributes.Baseline, attributes.Focus);
        Assert.AreEqual(PlayerAttributes.Baseline, attributes.Steadiness);
        Assert.AreEqual(58, attributes.ScaleCoreDamage(40, 1.45f));
        Assert.AreEqual(1f, attributes.KnockbackTaken);
        Assert.AreEqual(1f, attributes.MoveSpeedMultiplier);
    }

    [TestMethod]
    public void PointMultipliers_ChangeFivePercentPerPoint()
    {
        PlayerAttributes attributes = PlayerAttributes.Default with
        {
            AttackSpeed = 20, Luck = 14, CoreSharpness = 0, Attunement = 30, Agility = 20, Focus = 6
        };

        Assert.AreEqual(1.5f, attributes.AttackSpeedMultiplier, 0.0001f);
        Assert.AreEqual(1.2f, attributes.LuckMultiplier, 0.0001f);
        Assert.AreEqual(0.5f, attributes.CoreSharpnessMultiplier, 0.0001f);
        Assert.AreEqual(2f, attributes.AttunementMultiplier, 0.0001f);
        Assert.AreEqual(1.5f, attributes.AgilityMultiplier, 0.0001f);
        Assert.AreEqual(0.8f, attributes.FocusMultiplier, 0.0001f);
    }

    [TestMethod]
    public void Agility_RaisesMovementSpeedOnePercentPerPoint()
    {
        PlayerAttributes attributes = PlayerAttributes.Default with { Agility = 20 };

        Assert.AreEqual(1f + 10 * PlayerAttributes.MoveSpeedPerPoint, attributes.MoveSpeedMultiplier, 0.0001f);
    }

    [TestMethod]
    [DataRow(10, 58)]
    [DataRow(20, 87)]
    [DataRow(0, 29)]
    public void CoreSharpness_ScalesCoreHits(int coreSharpness, int expected)
    {
        PlayerAttributes attributes = PlayerAttributes.Default with { CoreSharpness = coreSharpness };

        Assert.AreEqual(expected, attributes.ScaleCoreDamage(40, 1.45f));
    }

    [TestMethod]
    [DataRow(10, 1f)]
    [DataRow(20, 2f / 3f)]
    [DataRow(0, 2f)]
    public void Steadiness_ReducesKnockbackWithoutReachingZero(int steadiness, float expected)
    {
        PlayerAttributes attributes = PlayerAttributes.Default with { Steadiness = steadiness };

        Assert.AreEqual(expected, attributes.KnockbackTaken, 0.0001f);
    }

    [TestMethod]
    [DataRow(10, 3, 0.99, 3)]
    [DataRow(14, 3, 0.59, 4)]
    [DataRow(14, 3, 0.61, 3)]
    [DataRow(20, 25, 0.49, 38)]
    [DataRow(20, 25, 0.5, 37)]
    [DataRow(0, 3, 0.49, 2)]
    [DataRow(0, 3, 0.5, 1)]
    [DataRow(99, 0, 0.0, 0)]
    public void Luck_ScalesRewards_AndRollsTheFraction(int luck, int baseAmount, double roll, int expected)
    {
        PlayerAttributes attributes = PlayerAttributes.Default with { Luck = luck };

        Assert.AreEqual(expected, attributes.ScaleReward(baseAmount, roll));
    }

    [TestMethod]
    public void Luck_ExactAmounts_NeverRollAnExtraUnit()
    {
        // 1.2f times 5 is 6.0000002 in float; the extra unit must not appear for any roll.
        PlayerAttributes attributes = PlayerAttributes.Default with { Luck = 14 };

        Assert.AreEqual(6, attributes.ScaleReward(5, 0.0));
    }
}
