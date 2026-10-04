using System;
using TheLostSoulOfFire.Combat;

namespace TheLostSoulOfFire.Menu;

/// <summary>Bounds and step sizes of a value the sandbox dev menu can change.</summary>
public readonly record struct DevValueRange(int Min, int Max, int Step, int LargeStep)
{
    public const int MaxHealthLimit = 999;

    /// <summary>Maximum health: 1 to 999 in steps of 10, or 100 with Shift.</summary>
    public static DevValueRange Health { get; } = new(1, MaxHealthLimit, 10, 100);

    /// <summary>Strength, ability power and armor: the attribute range in steps of 1, or 10 with Shift.</summary>
    public static DevValueRange Attribute { get; } = new(PlayerAttributes.MinValue, PlayerAttributes.MaxValue, 1, 10);

    /// <summary>Moves <paramref name="value"/> one step in <paramref name="direction"/> and clamps it to the range.</summary>
    public int Adjust(int value, int direction, bool largeStep) =>
        Math.Clamp(value + Math.Sign(direction) * (largeStep ? LargeStep : Step), Min, Max);
}
