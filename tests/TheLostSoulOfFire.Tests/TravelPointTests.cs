using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class TravelPointTests
{
    private static CurrencyWallet WalletWith(int geld, int glut, int securedGeld = 0, int securedGlut = 0)
    {
        CurrencyWallet wallet = new();
        wallet.LoadSecured(new PlayerProfile { SecuredGeld = securedGeld, SecuredGlut = securedGlut });
        wallet.BeginRun(0);
        wallet.Credit(Currency.Geld, geld);
        wallet.Credit(Currency.Glut, glut);
        return wallet;
    }

    [TestMethod]
    public void SecurePartialRun_MovesHalf_RoundedDownPerCurrency()
    {
        // Worked example from docs/current/ECONOMY.md: 175 Geld and 81 Glut.
        CurrencyWallet wallet = WalletWith(175, 81);

        (int geld, int glut) = wallet.SecurePartialRun(GameBalance.TravelPointSecurePercent);

        Assert.AreEqual((87, 40), (geld, glut));
        Assert.AreEqual(88, wallet.Run(Currency.Geld));
        Assert.AreEqual(41, wallet.Run(Currency.Glut));
        Assert.AreEqual(87, wallet.Secured(Currency.Geld));
        Assert.AreEqual(40, wallet.Secured(Currency.Glut));
    }

    [TestMethod]
    public void SecurePartialRun_AddsToExistingSecuredBalances()
    {
        CurrencyWallet wallet = WalletWith(300, 80, securedGeld: 150, securedGlut: 40);

        wallet.SecurePartialRun(50);

        Assert.AreEqual(300, wallet.Secured(Currency.Geld));
        Assert.AreEqual(80, wallet.Secured(Currency.Glut));
    }

    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(1, 1)]
    public void SecurePartialRun_WithZeroOrOne_SecuresNothing(int geld, int glut)
    {
        CurrencyWallet wallet = WalletWith(geld, glut);

        Assert.AreEqual((0, 0), wallet.SecurePartialRun(50));
        Assert.AreEqual(geld, wallet.Run(Currency.Geld));
        Assert.AreEqual(glut, wallet.Run(Currency.Glut));
        Assert.AreEqual(0, wallet.Secured(Currency.Geld));
    }

    [TestMethod]
    public void PreviewPartialSecure_MatchesTheTransfer_WithoutChangingBalances()
    {
        CurrencyWallet wallet = WalletWith(175, 81);

        (int Geld, int Glut) preview = wallet.PreviewPartialSecure(50);

        Assert.AreEqual(175, wallet.Run(Currency.Geld));
        Assert.AreEqual(preview, wallet.SecurePartialRun(50));
    }

    [TestMethod]
    public void SecureAndContinue_SecuresHalfOnce_AndRefusesASecondDecision()
    {
        CurrencyWallet wallet = WalletWith(400, 90);
        TravelPoint point = new(Vector2.Zero);

        Assert.IsTrue(point.TryDecide(TravelChoice.SecureAndContinue, wallet, out var first));
        Assert.IsFalse(point.TryDecide(TravelChoice.SecureAndContinue, wallet, out var second));
        Assert.IsFalse(point.TryDecide(TravelChoice.Extract, wallet, out _));

        Assert.AreEqual((200, 45), first);
        Assert.AreEqual((0, 0), second);
        Assert.AreEqual(200, wallet.Run(Currency.Geld));
        Assert.AreEqual(45, wallet.Secured(Currency.Glut));
        Assert.AreEqual(TravelChoice.SecureAndContinue, point.Decision);
    }

    [TestMethod]
    public void Continue_LeavesAllBalancesUnchanged()
    {
        CurrencyWallet wallet = WalletWith(400, 90, securedGeld: 10);
        TravelPoint point = new(Vector2.Zero);

        Assert.IsTrue(point.TryDecide(TravelChoice.Continue, wallet, out var secured));

        Assert.AreEqual((0, 0), secured);
        Assert.AreEqual(400, wallet.Run(Currency.Geld));
        Assert.AreEqual(90, wallet.Run(Currency.Glut));
        Assert.AreEqual(10, wallet.Secured(Currency.Geld));
    }

    [TestMethod]
    public void Extract_SecuresBothRunBalancesCompletely()
    {
        CurrencyWallet wallet = WalletWith(400, 90, securedGeld: 150, securedGlut: 40);
        TravelPoint point = new(Vector2.Zero);

        Assert.IsTrue(point.TryDecide(TravelChoice.Extract, wallet, out var secured));

        Assert.AreEqual((400, 90), secured);
        Assert.AreEqual(550, wallet.Secured(Currency.Geld));
        Assert.AreEqual(130, wallet.Secured(Currency.Glut));
        Assert.AreEqual(0, wallet.Run(Currency.Geld));
        Assert.AreEqual(0, wallet.Run(Currency.Glut));
    }

    [TestMethod]
    public void DefeatAfterPartialSecure_LosesOnlyTheRemainder()
    {
        // Variant B from docs/current/ECONOMY.md.
        CurrencyWallet wallet = WalletWith(400, 90, securedGeld: 150, securedGlut: 40);
        new TravelPoint(Vector2.Zero).TryDecide(TravelChoice.SecureAndContinue, wallet, out _);

        wallet.LoseRun();

        Assert.AreEqual(350, wallet.Secured(Currency.Geld));
        Assert.AreEqual(85, wallet.Secured(Currency.Glut));
        Assert.AreEqual(0, wallet.Run(Currency.Geld));
    }

    [TestMethod]
    public void IsInReach_OnlyNearAndOnlyBeforeTheDecision()
    {
        TravelPoint point = new(new Vector2(100f, 100f));

        Assert.IsTrue(point.IsInReach(new Vector2(100f + GameBalance.TravelPointInteractRadius - 1f, 100f)));
        Assert.IsFalse(point.IsInReach(new Vector2(100f + GameBalance.TravelPointInteractRadius + 1f, 100f)));

        point.TryDecide(TravelChoice.Continue, new CurrencyWallet(), out _);
        Assert.IsFalse(point.IsInReach(new Vector2(100f, 100f)));
    }

    [TestMethod]
    public void ArenaPosition_IsClearOfTheWaveTriggerAndTheChests()
    {
        Rectangle bounds = GameBalance.CombatBounds;
        Vector2 point = TravelPoint.ArenaPosition(bounds);
        float reach = GameBalance.TravelPointInteractRadius;

        Assert.IsTrue(Vector2.Distance(point, bounds.Center.ToVector2()) > reach + GameBalance.WaveTriggerRadius);
        foreach (int wave in GameBalance.ArenaChestWaves)
        {
            Assert.IsTrue(Vector2.Distance(point, ArenaChest.PositionForWave(wave, bounds)) > reach + GameBalance.ChestInteractRadius);
        }
    }

    [TestMethod]
    public void ExtractToHub_LeavesTheArenaForTheHub_AndIgnoresOtherPhases()
    {
        Assert.AreEqual(GamePhase.Antechamber, GameFlowRules.ExtractToHub(GamePhase.Arena));
        Assert.AreEqual(GamePhase.Prologue, GameFlowRules.ExtractToHub(GamePhase.Prologue));
    }
}
