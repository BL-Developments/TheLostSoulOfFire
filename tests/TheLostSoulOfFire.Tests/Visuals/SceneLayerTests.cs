using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Tests.Visuals;

[TestClass]
public sealed class SceneLayerTests
{
    private static List<string> SortedNames(params (string Name, float FootY)[] items)
    {
        List<string> drawn = [];
        List<DepthItem> depth = items.Select((item, index) => new DepthItem(item.FootY, index, () => drawn.Add(item.Name))).ToList();
        DepthSort.Sort(depth);
        depth.ForEach(item => item.Draw());
        return drawn;
    }

    [TestMethod]
    public void PlayerInFrontOfProp_IsDrawnAfterIt()
    {
        // Spec: player's foot point is in front of (below) the prop's → player drawn over the prop.
        CollectionAssert.AreEqual(new[] { "pillar", "player" }, SortedNames(("player", 620f), ("pillar", 600f)));
        CollectionAssert.AreEqual(new[] { "player", "pillar" }, SortedNames(("player", 580f), ("pillar", 600f)));
    }

    [TestMethod]
    public void EqualFeet_KeepSubmissionOrder()
    {
        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, SortedNames(("a", 500f), ("b", 500f), ("c", 500f)));
    }

    [TestMethod]
    public void Actors_AreOrderedBackToFront()
    {
        CollectionAssert.AreEqual(
            new[] { "hollow-far", "player", "prop", "hollow-near" },
            SortedNames(("hollow-near", 800f), ("player", 500f), ("hollow-far", 300f), ("prop", 640f)));
    }

    private static readonly RectangleF Pillar = new(400f, 200f, 120f, 400f); // foot at y = 600

    [TestMethod]
    public void HighProp_HidesOnlyWhatStandsBehindIt()
    {
        RectangleF player = RectangleF.Around(new Vector2(460f, 520f), new Vector2(60f, 100f));

        Assert.IsTrue(OccluderFade.Hides(Pillar, 600f, SceneLayer.HighProp, player, 570f), "player behind the pillar");
        Assert.IsFalse(OccluderFade.Hides(Pillar, 600f, SceneLayer.HighProp, player, 640f), "player in front of the pillar");
    }

    [TestMethod]
    public void ForegroundOccluder_HidesAnythingItOverlaps()
    {
        RectangleF player = RectangleF.Around(new Vector2(460f, 520f), new Vector2(60f, 100f));

        Assert.IsTrue(OccluderFade.Hides(Pillar, 600f, SceneLayer.Foreground, player, 900f));
        Assert.IsTrue(OccluderFade.Hides(Pillar, 600f, SceneLayer.Occluder, player, 900f));
    }

    [TestMethod]
    public void NoOverlap_NeverHides()
    {
        RectangleF farAway = RectangleF.Around(new Vector2(1200f, 520f), new Vector2(60f, 100f));

        Assert.IsFalse(OccluderFade.Hides(Pillar, 600f, SceneLayer.Foreground, farAway, 500f));
    }

    [TestMethod]
    public void TargetAlpha_TurnsSeeThroughForAnyHiddenTarget_IncludingTelegraphs()
    {
        RectangleF enemy = RectangleF.Around(new Vector2(1200f, 520f), new Vector2(60f, 100f));
        RectangleF telegraph = RectangleF.Around(new Vector2(470f, 560f), new Vector2(180f, 60f));

        float none = OccluderFade.TargetAlpha(Pillar, 600f, SceneLayer.HighProp, [(enemy, 570f)]);
        float hidden = OccluderFade.TargetAlpha(Pillar, 600f, SceneLayer.HighProp, [(enemy, 570f), (telegraph, 590f)]);

        Assert.AreEqual(1f, none);
        Assert.AreEqual(OccluderFade.SeeThroughAlpha, hidden);
    }

    [TestMethod]
    public void Fade_ApproachesWithoutOvershoot()
    {
        float alpha = 1f;
        for (int frame = 0; frame < 30; frame++)
        {
            alpha = OccluderFade.Approach(alpha, OccluderFade.SeeThroughAlpha, 1f / 60f);
            Assert.IsTrue(alpha >= OccluderFade.SeeThroughAlpha);
        }

        Assert.AreEqual(OccluderFade.SeeThroughAlpha, alpha, 0.0001f);
    }
}
