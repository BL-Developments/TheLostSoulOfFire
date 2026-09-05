using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TheLostSoulOfFire.Game;

public enum EncounterRole
{
    Hollow,
    Burning,
    Devourer
}

/// <summary>
/// One staged arrival. Offsets are relative to the centre of the combat bounds
/// so the whole encounter can be read and re-tuned as a single table.
/// </summary>
public readonly record struct EncounterSpawn(
    float Delay,
    EncounterRole Role,
    Vector2 Offset,
    int Seed = 0,
    int HeldSouls = 0);

/// <summary>
/// One authored movement of the encounter. The title and line are the furnace's
/// human history surfacing, not a wave counter.
/// </summary>
public readonly record struct EncounterBeat(
    string Title,
    string Line,
    EncounterSpawn[] Spawns);

/// <summary>
/// The Golden Slice: a single authored encounter in the Abandoned Soul Furnace.
///
/// This is deliberately a fixed table rather than a level or wave framework. Each
/// beat introduces exactly one idea, gives it space, and then folds it into the
/// next. Reinforcements are staged in time rather than dumped at once so the
/// Player learns each Lost Soul family by fighting it, not by reading a tooltip.
/// </summary>
public sealed class EncounterDirector
{
    public const int BeatCount = 4;

    private static readonly EncounterBeat[] Beats =
    [
        // I. One Hollow, alone, walking out of the ladle light. Nothing else is
        //    happening, so the swipe telegraph and the Anchor mark are readable.
        new(
            "THE FIRST STILL ONE",
            "SHE NEVER LEFT HER STATION",
            [
                new(0f, EncounterRole.Hollow, new Vector2(-296f, -44f), 1),
                new(6.4f, EncounterRole.Hollow, new Vector2(344f, 158f), 2)
            ]),

        // II. The Burning charges down a long lane, which needs open floor to be
        //     legible. A single Hollow keeps the Player from standing still.
        new(
            "THE ONE THAT STILL BURNS",
            "HE COULD NOT PUT IT DOWN",
            [
                new(0f, EncounterRole.Burning, new Vector2(438f, -126f), 3),
                new(1.4f, EncounterRole.Hollow, new Vector2(-382f, 128f), 4),
                new(6.8f, EncounterRole.Burning, new Vector2(-436f, -186f), 5)
            ]),

        // III. The Devourer arrives already carrying Souls. The Hollows die fast
        //      and leave more, so the Player has to choose between damage and
        //      rescue. This is the beat the Severance Window exists for.
        new(
            "WHAT IT TOOK",
            "IT KEEPS WHAT IT SWALLOWS",
            [
                new(0f, EncounterRole.Hollow, new Vector2(-252f, -232f), 6),
                new(0.5f, EncounterRole.Hollow, new Vector2(272f, 214f), 7),
                new(2.6f, EncounterRole.Devourer, new Vector2(-468f, 22f), 0, 2)
            ]),

        // IV. Everything at once, still staged so the frame never becomes noise.
        new(
            "THE FURNACE ANSWERS",
            "ALL OF THEM AT ONCE",
            [
                new(0f, EncounterRole.Burning, new Vector2(472f, -224f), 8),
                new(0.4f, EncounterRole.Hollow, new Vector2(-448f, 236f), 9),
                new(2.2f, EncounterRole.Devourer, new Vector2(496f, 44f), 0, 1),
                new(4.8f, EncounterRole.Burning, new Vector2(-482f, -64f), 10),
                new(7.2f, EncounterRole.Hollow, new Vector2(404f, 262f), 11)
            ])
    ];

    private readonly List<EncounterSpawn> _pending = [];
    private int _beatIndex;
    private float _beatTime;

    public int BeatNumber => _beatIndex + 1;
    public bool IsFinalBeat => _beatIndex >= BeatCount - 1;
    public bool AllSpawnsReleased => _pending.Count == 0;
    public float BeatTime => _beatTime;
    public string Title => Beats[Math.Clamp(_beatIndex, 0, BeatCount - 1)].Title;
    public string Line => Beats[Math.Clamp(_beatIndex, 0, BeatCount - 1)].Line;

    public static string GetTitle(int beatNumber) =>
        Beats[Math.Clamp(beatNumber - 1, 0, BeatCount - 1)].Title;

    public static string GetLine(int beatNumber) =>
        Beats[Math.Clamp(beatNumber - 1, 0, BeatCount - 1)].Line;

    public void Reset()
    {
        _pending.Clear();
        _beatIndex = 0;
        _beatTime = 0f;
    }

    public void BeginBeat(int beatNumber)
    {
        _beatIndex = Math.Clamp(beatNumber - 1, 0, BeatCount - 1);
        _beatTime = 0f;
        _pending.Clear();
        _pending.AddRange(Beats[_beatIndex].Spawns);
    }

    /// <summary>
    /// Releases arrivals whose staged moment has come. When the floor is already
    /// clear the remaining arrivals are pulled forward: a Player who wins the beat
    /// quickly should not be made to wait, and the automated lifecycle check needs
    /// the beat to be able to finish deterministically.
    /// </summary>
    public void Update(float deltaTime, bool combatFloorEmpty, List<EncounterSpawn> released)
    {
        released.Clear();
        _beatTime += deltaTime;

        if (_pending.Count == 0)
        {
            return;
        }

        if (combatFloorEmpty)
        {
            released.AddRange(_pending);
            _pending.Clear();
            return;
        }

        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            if (_beatTime < _pending[i].Delay)
            {
                continue;
            }

            released.Add(_pending[i]);
            _pending.RemoveAt(i);
        }
    }
}
