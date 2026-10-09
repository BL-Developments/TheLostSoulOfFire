using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Game.Levels;

/// <summary>Where a room's exits sit on the north wall of its combat bounds.</summary>
public static class RoomExit
{
    private const float WallOffset = 40f;
    private const float SingleExitShare = 0.5f;
    private const float LeftExitShare = 0.3f;
    private const float RightExitShare = 0.7f;

    public static Vector2 Position(Rectangle combatBounds, int exitCount, int exitIndex)
    {
        float share = exitCount == 1
            ? SingleExitShare
            : exitIndex == 0 ? LeftExitShare : RightExitShare;
        return new Vector2(combatBounds.Left + combatBounds.Width * share, combatBounds.Top + WallOffset);
    }

    /// <summary>Index of the exit within interaction reach of <paramref name="player"/>, or null.</summary>
    public static int? InReach(Rectangle combatBounds, int exitCount, Vector2 player)
    {
        float radiusSquared = GameBalance.RoomExitInteractRadius * GameBalance.RoomExitInteractRadius;
        for (int index = 0; index < exitCount; index++)
        {
            if (Vector2.DistanceSquared(Position(combatBounds, exitCount, index), player) <= radiusSquared)
            {
                return index;
            }
        }

        return null;
    }
}
