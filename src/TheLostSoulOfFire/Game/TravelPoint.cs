using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Game;

/// <summary>The three decisions a travel point offers (#53), in menu order.</summary>
public enum TravelChoice
{
    SecureAndContinue,
    Continue,
    Extract
}

/// <summary>
/// A travel point between two levels. It accepts exactly one decision; the first one wins, so a
/// repeated key press or a second input in the same frame cannot secure twice.
/// </summary>
public sealed class TravelPoint
{
    public const int ChoiceCount = 3;

    public TravelPoint(Vector2 position) => Position = position;

    public Vector2 Position { get; }
    public TravelChoice? Decision { get; private set; }
    public bool IsDecided => Decision is not null;

    public bool IsInReach(Vector2 position) =>
        !IsDecided && Vector2.DistanceSquared(position, Position) <= GameBalance.TravelPointInteractRadius * GameBalance.TravelPointInteractRadius;

    /// <summary>
    /// Applies <paramref name="choice"/> to <paramref name="wallet"/> and returns what was secured.
    /// False without any change once a decision has been taken.
    /// </summary>
    public bool TryDecide(TravelChoice choice, CurrencyWallet wallet, out (int Geld, int Glut) secured)
    {
        secured = (0, 0);
        if (IsDecided)
        {
            return false;
        }

        Decision = choice;
        secured = choice switch
        {
            TravelChoice.SecureAndContinue => wallet.SecurePartialRun(GameBalance.TravelPointSecurePercent),
            TravelChoice.Extract => wallet.SecureAllRun(),
            _ => (0, 0)
        };
        return true;
    }

    /// <summary>Where the arena's travel point stands: top centre, clear of the chests and the wave trigger.</summary>
    public static Vector2 ArenaPosition(Rectangle combatBounds) =>
        new(combatBounds.Left + combatBounds.Width * 0.5f, combatBounds.Top + combatBounds.Height * 0.2f);
}
