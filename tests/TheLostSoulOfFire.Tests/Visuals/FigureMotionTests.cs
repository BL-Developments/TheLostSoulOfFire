using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Tests.Visuals;

[TestClass]
public sealed class FigureMotionTests
{
    private const float Frame = 1f / 60f;

    private static Vector2 At(float degrees) => new(MathF.Cos(MathHelper.ToRadians(degrees)), MathF.Sin(MathHelper.ToRadians(degrees)));

    private static int SectorDistance(int a, int b)
    {
        int distance = Math.Abs(a - b) % 8;
        return Math.Min(distance, 8 - distance);
    }

    [TestMethod]
    public void FastCircling_TurnsThroughNeighbouringDirections_WithoutJumps()
    {
        FacingTracker tracker = new();
        tracker.Update(At(0f), Frame);
        int previous = tracker.Sector;
        HashSet<int> visited = [previous];

        // The mouse circles the figure twice per second: 12° per frame, far faster than the body turns.
        for (int frame = 1; frame <= 180; frame++)
        {
            tracker.Update(At(frame * 12f), Frame);
            Assert.IsTrue(SectorDistance(previous, tracker.Sector) <= 1, $"jumped from {previous} to {tracker.Sector} at frame {frame}");
            previous = tracker.Sector;
            visited.Add(previous);
        }

        Assert.AreEqual(8, visited.Count, "the body still turns all the way round");
    }

    [TestMethod]
    public void LongFrameHitch_StillCannotSkipADirection()
    {
        FacingTracker tracker = new();
        tracker.Update(At(0f), Frame);
        int before = tracker.Sector;

        tracker.Update(At(180f), 0.25f);

        Assert.AreNotEqual(before, tracker.Sector, "it turned");
        Assert.IsTrue(SectorDistance(before, tracker.Sector) <= 4);
    }

    [TestMethod]
    public void JitterOnASectorBorder_DoesNotFlicker()
    {
        FacingTracker tracker = new();
        tracker.Update(At(20f), Frame);
        for (int frame = 0; frame < 30; frame++)
        {
            tracker.Update(At(20f), Frame);
        }
        string settled = tracker.Direction;

        // The border between east and south-east is at 22.5°; wobble across it by ±4°.
        int changes = 0;
        string last = settled;
        for (int frame = 0; frame < 240; frame++)
        {
            tracker.Update(At(frame % 2 == 0 ? 18.5f : 26.5f), Frame);
            if (tracker.Direction != last)
            {
                changes++;
                last = tracker.Direction;
            }
        }

        Assert.AreEqual(0, changes, "the drawn direction must hold while the mouse wobbles on the border");
    }

    [TestMethod]
    public void ClearTurn_IsReachedAndHeld()
    {
        FacingTracker tracker = new();
        tracker.Update(At(0f), Frame);
        for (int frame = 0; frame < 60; frame++)
        {
            tracker.Update(At(90f), Frame);
        }

        Assert.AreEqual("s", tracker.Direction);
        Assert.AreEqual(MathHelper.PiOver2, tracker.Angle, 0.001f);
    }

    [TestMethod]
    public void Snap_TurnsAtOnce_ForASwing()
    {
        // A scythe swing must read in its own direction from its first frame.
        FacingTracker tracker = new();
        tracker.Update(At(0f), Frame);

        Assert.AreEqual("w", tracker.Snap(At(180f)));
        Assert.AreEqual(MathHelper.Pi, MathF.Abs(tracker.Angle), 0.001f);
        Assert.AreEqual("w", tracker.Update(At(180f), Frame), "after the snap, turning carries on from there");
    }

    [TestMethod]
    public void HalfSpeed_GivesHalfTheAnimationRate()
    {
        VisualClipDefinition run = new("move", "P/move/{dir}", 128, 128, 9, 12f, true, null, ClipProgress.Distance, 232f);
        float full = 0f;
        float half = 0f;
        for (int frame = 0; frame < 60; frame++)
        {
            full = ClipClock.Advance(full, run, Frame, 310f * Frame);
            half = ClipClock.Advance(half, run, Frame, 155f * Frame);
        }

        Assert.AreEqual(full * 0.5f, half, 0.0001f);
        Assert.AreEqual(0f, ClipClock.Advance(0f, run, Frame, 0f), "standing still does not animate a run");
    }

    [TestMethod]
    public void WalkingBackward_RunsTheCycleBackward()
    {
        VisualClipDefinition walk = new("aim_move", "P/aim_move/{dir}", 128, 128, 12, 12f, true, null, ClipProgress.Distance, 150f);
        float elapsed = ClipClock.Advance(0.5f, walk, Frame, -30f);
        Assert.AreEqual(0.5f - 30f / 150f * walk.Duration, elapsed, 0.0001f, "backing away steps the cycle back");
        float wrapped = ClipClock.Advance(0.1f, walk, Frame, -30f);
        Assert.IsTrue(wrapped >= 0f && wrapped < walk.Duration, "a backward step below the start wraps into the cycle");
    }

    [TestMethod]
    public void TimeClip_RunsWithTheClock()
    {
        VisualClipDefinition idle = new("idle", "P/idle/{dir}", 128, 128, 9, 9f, true, null, ClipProgress.Time, 0f);

        Assert.AreEqual(Frame, ClipClock.Advance(0f, idle, Frame, 500f), 0.00001f);
    }

    [TestMethod]
    public void AttackDirection_StaysTheImmediateAim()
    {
        // The drawn facing lags behind on purpose; gameplay facing must not.
        Player player = new(new Vector2(500f, 350f));
        FacingTracker tracker = new();
        tracker.Update(player.FacingDirection, Frame);

        Vector2 aim = At(137f);
        player.Update(Frame, new InputState(), player.Position + aim * 200f, new Rectangle(0, 0, 1000, 700), new ParticleSystem(), new ScreenEffects());
        tracker.Update(player.FacingDirection, Frame);

        Assert.AreEqual(aim.X, player.FacingDirection.X, 0.0001f);
        Assert.AreEqual(aim.Y, player.FacingDirection.Y, 0.0001f);
        Assert.AreNotEqual(VisualDirections.FromVector(aim), tracker.Direction, "the drawn body is still turning");
    }
}
