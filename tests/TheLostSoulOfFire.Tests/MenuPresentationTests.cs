using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Menu;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class MenuPresentationTests
{
    [TestMethod]
    public void SettingsHitTargets_FitTheVirtualScreenAndCoverDisplayedValues()
    {
        MenuController menu = new();
        menu.Open();
        menu.SetHoverIndex(2);
        menu.Confirm();
        menu.SetHoverIndex(2);
        menu.Confirm();
        menu.SetHoverIndex(0);
        menu.AdjustSelectedValue(-1);

        Rectangle screen = new(0, 0, 1280, 720);
        IReadOnlyList<Rectangle> bounds = new CinematicPresentation().GetMenuEntryBounds(
            new Viewport(screen), menu.CurrentPage, menu);

        Assert.AreEqual(menu.CurrentPage.Entries.Count, bounds.Count);
        for (int i = 0; i < bounds.Count; i++)
        {
            Assert.IsTrue(screen.Contains(bounds[i]), $"Entry {i} hit target extends past the virtual screen.");
            Assert.IsTrue(bounds[i].Contains(new Point(640, bounds[i].Center.Y)));
        }
        Assert.AreEqual("GESAMTLAUTSTÄRKE: 90%", menu.GetLabel(menu.CurrentPage.Entries[0]));
    }

    [TestMethod]
    public void PauseAndQuitPages_HitTargetsFitTheVirtualScreenWithoutOverlap()
    {
        Rectangle screen = new(0, 0, 1280, 720);
        MenuController menu = new();
        foreach (MenuPage page in new[] { MenuPages.Pause, MenuPages.PauseQuit, MenuPages.QuitConfirm })
        {
            IReadOnlyList<Rectangle> bounds = new CinematicPresentation().GetMenuEntryBounds(new Viewport(screen), page, menu);

            Assert.AreEqual(page.Entries.Count, bounds.Count);
            for (int i = 0; i < bounds.Count; i++)
            {
                Assert.IsTrue(screen.Contains(bounds[i]), $"{page.Id} entry {i} extends past the virtual screen.");
                if (i > 0) Assert.IsFalse(bounds[i].Intersects(bounds[i - 1]), $"{page.Id} entries {i - 1} and {i} overlap.");
            }
        }
    }

    [TestMethod]
    public void QuitPrompt_IsFullyRenderable()
    {
        string prompt = MenuPages.QuitConfirm.Prompt!;

        Assert.IsTrue(prompt.All(c => c == ' ' || PixelText.CanRender(c)), $"Missing glyph in '{prompt}'.");
        Assert.IsTrue(PixelText.Measure(prompt, 2) < 1280);
    }
}
