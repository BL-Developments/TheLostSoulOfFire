using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Game;

/// <summary>
/// One local Warden: who he is, the body that fights, and where his intent comes
/// from. Nothing more — this is the smallest thing that supports one or two
/// players and is not a step toward a networked entity model.
/// </summary>
public sealed class PlayerSlot
{
    public PlayerSlot(int index, WardenIdentity identity, Player warden, IPlayerInputSource source)
    {
        Index = index;
        Identity = identity;
        Warden = warden;
        Source = source;
    }

    public int Index { get; }
    public WardenIdentity Identity { get; }
    public Player Warden { get; }
    public IPlayerInputSource Source { get; set; }
    public PlayerCommand Command { get; private set; }
    public Vector2 AimPoint { get; private set; }

    /// <summary>The downed brother this Warden is currently reaching for, if any.</summary>
    public Player StabilizeTarget { get; set; }

    public void Initialise(Vector2 aimPoint) => AimPoint = aimPoint;

    public void Read(InputState input, Camera2D camera, Viewport viewport)
    {
        Command = Source.Read(input, camera, viewport, Warden.Position, AimPoint);
        AimPoint = Command.AimPoint;
    }
}

/// <summary>
/// The one or two brothers currently in the encounter.
///
/// Player 1 is always the protagonist on keyboard and mouse. Player 2 is the
/// brother, on a gamepad when one is present and on the number pad when it is
/// not, so the design can be reviewed without controller hardware.
/// </summary>
public sealed class WardenRoster
{
    private readonly List<PlayerSlot> _slots = [];
    private readonly WardenField _field = new();

    public IReadOnlyList<PlayerSlot> Slots => _slots;
    public WardenField Field => _field;
    public int Count => _slots.Count;
    public bool IsCooperative => _slots.Count > 1;

    /// <summary>The protagonist. Always present; solo behaviour routes through him.</summary>
    public Player Lead => _slots[0].Warden;

    public PlayerSlot LeadSlot => _slots[0];

    public WardenRoster(Vector2 spawn)
    {
        _slots.Add(new PlayerSlot(0, WardenIdentity.Younger, new Player(spawn, WardenIdentity.Younger), new KeyboardMouseInput()));
        _slots[0].Initialise(spawn + Vector2.UnitX * 200f);
        _field.Configure(Wardens());
    }

    /// <summary>
    /// Brings the brother in. Returns false if he is already here or the slice's
    /// two-player limit is reached.
    /// </summary>
    public bool TryJoinSecond(Vector2 spawn, IPlayerInputSource source)
    {
        if (_slots.Count >= GameBalance.MaxLocalPlayers)
        {
            return false;
        }

        Player brother = new(spawn, WardenIdentity.Elder);
        PlayerSlot slot = new(1, WardenIdentity.Elder, brother, source);
        slot.Initialise(spawn + Vector2.UnitX * 200f);
        _slots.Add(slot);
        _field.Configure(Wardens());
        return true;
    }

    public void ReadCommands(InputState input, Camera2D camera, Viewport viewport)
    {
        foreach (PlayerSlot slot in _slots)
        {
            slot.Read(input, camera, viewport);
        }
    }

    public IEnumerable<Player> Wardens()
    {
        foreach (PlayerSlot slot in _slots)
        {
            yield return slot.Warden;
        }
    }

    public void Reset(Vector2 spawn)
    {
        for (int index = 0; index < _slots.Count; index++)
        {
            // Brothers arrive shoulder to shoulder rather than stacked.
            Vector2 offset = _slots.Count > 1 ? new Vector2((index - 0.5f) * 130f, 0f) : Vector2.Zero;
            _slots[index].Warden.Reset(spawn + offset);
            _slots[index].StabilizeTarget = null;
            _slots[index].Initialise(spawn + offset + Vector2.UnitX * 200f);
        }
    }

    /// <summary>Centroid of the Wardens the camera has to keep in frame.</summary>
    public Vector2 FrameCentre()
    {
        Vector2 sum = Vector2.Zero;
        int count = 0;
        foreach (PlayerSlot slot in _slots)
        {
            if (slot.Warden.IsDead && _slots.Count > 1)
            {
                continue;
            }

            sum += slot.Warden.Position;
            count++;
        }

        return count == 0 ? _slots[0].Warden.Position : sum / count;
    }

    /// <summary>Bounding span of the Wardens the camera has to keep in frame.</summary>
    public Vector2 FrameSpan()
    {
        Vector2 min = new(float.MaxValue);
        Vector2 max = new(float.MinValue);
        int count = 0;
        foreach (PlayerSlot slot in _slots)
        {
            if (slot.Warden.IsDead && _slots.Count > 1)
            {
                continue;
            }

            min = Vector2.Min(min, slot.Warden.Position);
            max = Vector2.Max(max, slot.Warden.Position);
            count++;
        }

        return count == 0 ? Vector2.Zero : max - min;
    }

    public bool AllDown()
    {
        foreach (PlayerSlot slot in _slots)
        {
            if (!slot.Warden.IsDead && !slot.Warden.IsDowned)
            {
                return false;
            }
        }

        return true;
    }

    public bool AnyDead()
    {
        foreach (PlayerSlot slot in _slots)
        {
            if (slot.Warden.IsDead)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Gamepad slot a joining brother should use, or null when none is present.</summary>
    public static PlayerIndex? FindFreeGamePad(InputState input)
    {
        for (int index = 0; index < 4; index++)
        {
            if (input.IsGamePadConnected((PlayerIndex)index))
            {
                return (PlayerIndex)index;
            }
        }

        return null;
    }
}
