using System;

namespace TheLostSoulOfFire.Game;

/// <summary>Working names from #52; final names and lore are still open.</summary>
public enum Currency
{
    Geld,
    Glut
}

/// <summary>
/// Holds the run balance and the secured balance of both currencies. The run balance is what
/// the current run can spend or lose; the secured balance survives defeat and is persisted in
/// the player profile. Kept free of MonoGame so the economy rules are testable on their own.
/// </summary>
public sealed class CurrencyWallet
{
    private readonly int[] _run = new int[2];
    private readonly int[] _secured = new int[2];

    public int Run(Currency currency) => _run[(int)currency];
    public int Secured(Currency currency) => _secured[(int)currency];

    /// <summary>Starts a run: no money, only the free Glut starter stock. Secured Glut stays home.</summary>
    public void BeginRun(int starterGlut)
    {
        _run[(int)Currency.Geld] = 0;
        _run[(int)Currency.Glut] = Math.Max(0, starterGlut);
    }

    public void Credit(Currency currency, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        _run[(int)currency] += amount;
    }

    /// <summary>Spends from the run balance; refuses without any change when the funds are short.</summary>
    public bool TrySpendRun(Currency currency, int amount)
    {
        if (amount <= 0 || _run[(int)currency] < amount)
        {
            return false;
        }

        _run[(int)currency] -= amount;
        return true;
    }

    /// <summary>Defeat: both run balances are lost, secured balances stay.</summary>
    public void LoseRun()
    {
        Array.Clear(_run);
    }

    /// <summary>
    /// Moves both run balances into the secured balances: extraction at a travel point and
    /// arena completion, which stands in for the boss victory (#53).
    /// </summary>
    public (int Geld, int Glut) SecureAllRun()
    {
        (int Geld, int Glut) secured = (Run(Currency.Geld), Run(Currency.Glut));
        _secured[(int)Currency.Geld] += secured.Geld;
        _secured[(int)Currency.Glut] += secured.Glut;
        Array.Clear(_run);
        return secured;
    }

    /// <summary>
    /// What a partial securing would move: <paramref name="percent"/> of each run balance,
    /// rounded down per currency (#53). The remainder stays in the run.
    /// </summary>
    public (int Geld, int Glut) PreviewPartialSecure(int percent)
    {
        int clamped = Math.Clamp(percent, 0, 100);
        return (Run(Currency.Geld) * clamped / 100, Run(Currency.Glut) * clamped / 100);
    }

    /// <summary>Moves the share from <see cref="PreviewPartialSecure"/> into the secured balances.</summary>
    public (int Geld, int Glut) SecurePartialRun(int percent)
    {
        (int Geld, int Glut) secured = PreviewPartialSecure(percent);
        _run[(int)Currency.Geld] -= secured.Geld;
        _run[(int)Currency.Glut] -= secured.Glut;
        _secured[(int)Currency.Geld] += secured.Geld;
        _secured[(int)Currency.Glut] += secured.Glut;
        return secured;
    }

    public void LoadSecured(PlayerProfile profile)
    {
        _secured[(int)Currency.Geld] = Math.Max(0, profile.SecuredGeld);
        _secured[(int)Currency.Glut] = Math.Max(0, profile.SecuredGlut);
    }

    public PlayerProfile ToProfile() => new()
    {
        SecuredGeld = Secured(Currency.Geld),
        SecuredGlut = Secured(Currency.Glut)
    };
}
