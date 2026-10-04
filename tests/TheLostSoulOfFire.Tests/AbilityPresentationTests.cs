using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Effects;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class AbilityPresentationTests
{
    [TestMethod]
    public void Catalogue_AllCardsAndTextFitWithoutOverlap()
    {
        var viewport = new Viewport(0, 0, 1280, 720);
        var abilities = new RunAbilities();
        var player = new Player(new Vector2(500, 350));
        player.Attributes = new PlayerAttributes(99, 99, 99);
        foreach (RunAbility ability in Enum.GetValues<RunAbility>())
        {
            var card = AbilityCard.Create(ability, abilities, player, 0, true);
            var bounds = AbilityPresentation.CatalogueBounds(viewport, (int)ability);
            Assert.IsTrue(viewport.Bounds.Contains(bounds));
            Assert.IsTrue(PixelText.Measure(card.Definition.Name, 2) <= bounds.Width - 32);
            Assert.IsTrue(PixelText.Measure(card.Summary, 1) <= bounds.Width - 32);
            Assert.IsTrue(PixelText.Measure(card.Detail, 1) <= bounds.Width - 32);
            string cost = $"{card.Definition.Cost} GLUT / {card.Definition.Cooldown:0.#} S ABKLINGZEIT";
            Assert.IsTrue(PixelText.Measure(cost, 1) + PixelText.Measure(card.Status, 1) + 16 <= bounds.Width - 32);
            foreach (char glyph in (card.Summary + card.Detail + card.Status).Where(c => c != ' '))
                Assert.IsTrue(PixelText.CanRender(glyph), $"Unsupported glyph: {glyph}");
            for (int other = 0; other < (int)ability; other++)
                Assert.IsFalse(bounds.Intersects(AbilityPresentation.CatalogueBounds(viewport, other)));
        }
    }

    [TestMethod]
    public void CardsFollowEquippedSlotsAndLiveCooldown()
    {
        var abilities = new RunAbilities();
        var player = new Player(new Vector2(500, 350));
        var wallet = new CurrencyWallet();
        wallet.BeginRun(30);
        Assert.IsTrue(abilities.Equip(1, RunAbility.Vortex));
        Assert.AreEqual(1, AbilityCard.Create(RunAbility.Vortex, abilities, player, 30, true).Slot);
        Assert.AreEqual(-1, AbilityCard.Create(RunAbility.PiercingShot, abilities, player, 30, true).Slot);
        Assert.IsTrue(abilities.TryCast(RunAbility.Vortex, player, wallet, new Vector2(600, 350),
            new Rectangle(0, 0, 1000, 700), [], new ParticleSystem()));
        var card = AbilityCard.Create(RunAbility.Vortex, abilities, player, 26, true);
        Assert.AreEqual(0f, card.ReadyFraction);
        Assert.IsTrue(card.Status.StartsWith("SOG AKTIV"));
        abilities.Update(2.5f, player, new Rectangle(0, 0, 1000, 700), [], new ParticleSystem(), (_, _) => { });
        card = AbilityCard.Create(RunAbility.Vortex, abilities, player, 26, true);
        Assert.AreEqual(0.625f, card.ReadyFraction);
        Assert.IsTrue(card.Status.StartsWith("ABKLINGZEIT"));
    }

    [TestMethod]
    public void StatusDistinguishesFullHealthResourceShortageAndNonCombat()
    {
        var abilities = new RunAbilities();
        var player = new Player(Vector2.Zero);
        Assert.AreEqual("LEBEN VOLL", AbilityCard.Create(RunAbility.SecondWind, abilities, player, 30, true).Status);
        Assert.AreEqual("3 GLUT FEHLT", AbilityCard.Create(RunAbility.SecondWind, abilities, player, 0, true).Status);
        Assert.AreEqual("NUR IM KAMPF", AbilityCard.Create(RunAbility.SecondWind, abilities, player, 30, false).Status);
    }
    [TestMethod]
    public void SlotClickTargetsAreSeparateFromCardsAndEachOther()
    {
        var viewport = new Viewport(0, 0, 1280, 720);
        var first = AbilityPresentation.SlotBounds(viewport, 0);
        var second = AbilityPresentation.SlotBounds(viewport, 1);
        Assert.IsTrue(viewport.Bounds.Contains(first));
        Assert.IsTrue(viewport.Bounds.Contains(second));
        Assert.IsFalse(first.Intersects(second));
        for (int i = 0; i < 6; i++)
        {
            Assert.IsFalse(first.Intersects(AbilityPresentation.CatalogueBounds(viewport, i)));
            Assert.IsFalse(second.Intersects(AbilityPresentation.CatalogueBounds(viewport, i)));
        }
    }

}
