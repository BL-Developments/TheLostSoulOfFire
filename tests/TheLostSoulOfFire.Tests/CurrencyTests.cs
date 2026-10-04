using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class CurrencyWalletTests
{
    [TestMethod]
    public void BeginRun_StartsWithStarterGlutAndNoGeld_WithoutTakingSecuredGlut()
    {
        CurrencyWallet wallet = new();
        wallet.LoadSecured(new PlayerProfile { SecuredGeld = 40, SecuredGlut = 90 });

        wallet.BeginRun(GameBalance.GlutStarterStock);

        Assert.AreEqual(0, wallet.Run(Currency.Geld));
        Assert.AreEqual(GameBalance.GlutStarterStock, wallet.Run(Currency.Glut));
        Assert.AreEqual(90, wallet.Secured(Currency.Glut));
    }

    [TestMethod]
    public void Credit_ChangesOnlyTheRunBalance_AndIgnoresNonPositiveAmounts()
    {
        CurrencyWallet wallet = new();
        wallet.BeginRun(0);

        wallet.Credit(Currency.Glut, 5);
        wallet.Credit(Currency.Glut, 0);
        wallet.Credit(Currency.Glut, -3);

        Assert.AreEqual(5, wallet.Run(Currency.Glut));
        Assert.AreEqual(0, wallet.Secured(Currency.Glut));
        Assert.AreEqual(0, wallet.Run(Currency.Geld));
    }

    [TestMethod]
    public void TrySpendRun_RefusesShortFundsAndInvalidAmounts_WithoutChange()
    {
        CurrencyWallet wallet = new();
        wallet.BeginRun(10);

        Assert.IsFalse(wallet.TrySpendRun(Currency.Glut, 11));
        Assert.IsFalse(wallet.TrySpendRun(Currency.Glut, 0));
        Assert.IsFalse(wallet.TrySpendRun(Currency.Glut, -1));
        Assert.AreEqual(10, wallet.Run(Currency.Glut));

        Assert.IsTrue(wallet.TrySpendRun(Currency.Glut, 4));
        Assert.AreEqual(6, wallet.Run(Currency.Glut));
        Assert.IsTrue(wallet.TrySpendRun(Currency.Glut, 6));
        Assert.IsFalse(wallet.TrySpendRun(Currency.Glut, 1));
        Assert.AreEqual(0, wallet.Run(Currency.Glut));
    }

    [TestMethod]
    public void LoseRun_ClearsBothRunBalances_AndKeepsSecured()
    {
        CurrencyWallet wallet = new();
        wallet.LoadSecured(new PlayerProfile { SecuredGeld = 7, SecuredGlut = 8 });
        wallet.BeginRun(10);
        wallet.Credit(Currency.Geld, 25);

        wallet.LoseRun();

        Assert.AreEqual(0, wallet.Run(Currency.Geld));
        Assert.AreEqual(0, wallet.Run(Currency.Glut));
        Assert.AreEqual(7, wallet.Secured(Currency.Geld));
        Assert.AreEqual(8, wallet.Secured(Currency.Glut));
    }

    [TestMethod]
    public void SecureAllRun_MovesEverythingIncludingStarterStock_AndReportsTheAmounts()
    {
        CurrencyWallet wallet = new();
        wallet.LoadSecured(new PlayerProfile { SecuredGeld = 5, SecuredGlut = 1 });
        wallet.BeginRun(10);
        wallet.Credit(Currency.Geld, 25);
        wallet.Credit(Currency.Glut, 3);

        (int geld, int glut) = wallet.SecureAllRun();

        Assert.AreEqual((25, 13), (geld, glut));
        Assert.AreEqual(30, wallet.Secured(Currency.Geld));
        Assert.AreEqual(14, wallet.Secured(Currency.Glut));
        Assert.AreEqual(0, wallet.Run(Currency.Geld));
        Assert.AreEqual(0, wallet.Run(Currency.Glut));
        Assert.AreEqual((0, 0), wallet.SecureAllRun());
    }
}

[TestClass]
public sealed class EnemyRewardTests
{
    private static IEnumerable<object[]> Enemies() =>
    [
        [new Hollow(Vector2.Zero, 1), GameBalance.HollowGlut],
        [new Burning(Vector2.Zero, 1), GameBalance.BurningGlut],
        [new Devourer(Vector2.Zero), GameBalance.DevourerGlut]
    ];

    [TestMethod]
    [DynamicData(nameof(Enemies), DynamicDataSourceType.Method)]
    public void TryClaimReward_PaysTheTypeAmountExactlyOnce_AfterDefeat(Enemy enemy, int expected)
    {
        Assert.IsFalse(enemy.TryClaimReward(out _), "a living enemy pays nothing");

        DamageInfo lethal = new(enemy.MaxHealth * 2, Vector2.Zero, enemy.Position);
        enemy.ApplyDamage(lethal);
        Assert.IsTrue(enemy.TryClaimReward(out int glut));
        Assert.AreEqual(expected, glut);

        enemy.ApplyDamage(lethal);
        Assert.IsFalse(enemy.TryClaimReward(out int second));
        Assert.AreEqual(0, second);
    }
}

[TestClass]
public sealed class ArenaChestTests
{
    [TestMethod]
    public void TryOpen_SucceedsOnce_ThenTheChestLeavesReachAndDisappears()
    {
        ArenaChest chest = new(new Vector2(100f, 100f));
        Assert.IsTrue(chest.IsInReach(new Vector2(130f, 100f)));
        Assert.IsFalse(chest.IsInReach(new Vector2(100f + GameBalance.ChestInteractRadius + 1f, 100f)));

        Assert.IsTrue(chest.TryOpen());
        Assert.IsFalse(chest.TryOpen());
        Assert.IsFalse(chest.IsInReach(chest.Position));
        Assert.IsFalse(chest.IsGone);

        chest.Update(GameBalance.ChestOpenDuration);
        Assert.IsTrue(chest.IsGone);
    }

    [TestMethod]
    public void PositionForWave_GivesDistinctSpotsInsideTheCombatArea()
    {
        Rectangle bounds = GameBalance.CombatBounds;
        Vector2[] spots = [ArenaChest.PositionForWave(1, bounds), ArenaChest.PositionForWave(2, bounds), ArenaChest.PositionForWave(3, bounds)];
        foreach (Vector2 spot in spots)
        {
            Assert.IsTrue(bounds.Contains(spot), $"{spot} lies outside the combat area");
        }

        for (int first = 0; first < spots.Length; first++)
        for (int second = first + 1; second < spots.Length; second++)
        {
            Assert.IsTrue(Vector2.Distance(spots[first], spots[second]) > GameBalance.ChestInteractRadius * 2f);
        }
    }
}

[TestClass]
public sealed class PlayerProfileStoreTests
{
    private string _directory = string.Empty;
    private string ProfilePath => Path.Combine(_directory, "profile.json");

    [TestInitialize]
    public void CreateDirectory()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TestCleanup]
    public void DeleteDirectory() => Directory.Delete(_directory, true);

    [TestMethod]
    public void Load_MissingFile_ReturnsEmptyProfile()
    {
        PlayerProfile profile = new PlayerProfileStore(ProfilePath).Load();
        Assert.AreEqual(0, profile.SecuredGeld);
        Assert.AreEqual(0, profile.SecuredGlut);
    }

    [TestMethod]
    public void SaveAndLoad_PreserveSecuredBalances()
    {
        PlayerProfileStore store = new(ProfilePath);
        Assert.IsTrue(store.Save(new PlayerProfile { SecuredGeld = 25, SecuredGlut = 31 }));

        PlayerProfile loaded = new PlayerProfileStore(ProfilePath).Load();

        Assert.AreEqual(PlayerProfile.CurrentVersion, loaded.Version);
        Assert.AreEqual(25, loaded.SecuredGeld);
        Assert.AreEqual(31, loaded.SecuredGlut);
        Assert.IsFalse(File.Exists(ProfilePath + ".tmp"));
    }

    [TestMethod]
    [DataRow("not json")]
    [DataRow("{\"Version\":2,\"SecuredGeld\":5,\"SecuredGlut\":5}")]
    [DataRow("{\"Version\":1,\"SecuredGeld\":-5,\"SecuredGlut\":5}")]
    [DataRow("null")]
    public void Load_InvalidProfile_StartsEmpty_AndKeepsACopyOnNextSave(string content)
    {
        File.WriteAllText(ProfilePath, content);
        PlayerProfileStore store = new(ProfilePath);

        PlayerProfile profile = store.Load();
        Assert.AreEqual(0, profile.SecuredGeld);
        Assert.AreEqual(0, profile.SecuredGlut);

        store.Save(new PlayerProfile { SecuredGeld = 1 });
        Assert.AreEqual(content, File.ReadAllText(store.InvalidCopyPath));
        Assert.AreEqual(1, new PlayerProfileStore(ProfilePath).Load().SecuredGeld);
    }

    [TestMethod]
    public void Save_UnwritablePath_KeepsThePreviousProfile()
    {
        PlayerProfileStore store = new(ProfilePath);
        store.Save(new PlayerProfile { SecuredGeld = 9 });
        Directory.CreateDirectory(ProfilePath + ".tmp");

        Assert.IsFalse(store.Save(new PlayerProfile { SecuredGeld = 99 }));
        Assert.AreEqual(9, new PlayerProfileStore(ProfilePath).Load().SecuredGeld);
    }
}
