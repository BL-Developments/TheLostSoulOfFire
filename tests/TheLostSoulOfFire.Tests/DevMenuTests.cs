using TheLostSoulOfFire.Core;
using TheLostSoulOfFire.Menu;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class DevMenuTests
{
    private static readonly DevMenuEntry[] SampleEntries =
    [
        new("a", DevMenuSection.Character, DevMenuEntryKind.Value, "A"),
        new("b", DevMenuSection.Character, DevMenuEntryKind.Action, "B"),
        new("c", DevMenuSection.Enemies, DevMenuEntryKind.Action, "C")
    ];

    [TestMethod]
    public void Sections_AreCharacterThenEnemies()
    {
        CollectionAssert.AreEqual(
            new[] { "CHARAKTER", "GEGNER" },
            DevMenu.Sections.Select(DevMenu.GetLabel).ToArray());
    }

    [TestMethod]
    public void Open_Close_AndTimer()
    {
        DevMenu menu = new(SampleEntries);
        Assert.IsFalse(menu.IsOpen);

        menu.Open();
        menu.Tick(0.2f);
        Assert.IsTrue(menu.IsOpen);
        Assert.AreEqual(0.2f, menu.OpenTimer, 0.0001f);

        menu.Close();
        menu.Tick(0.2f);
        Assert.IsFalse(menu.IsOpen);
    }

    [TestMethod]
    public void Selection_MovesAcrossSections_WithoutWrapping()
    {
        DevMenu menu = new(SampleEntries);
        menu.Open();
        Assert.AreEqual("a", menu.SelectedEntry!.Id);

        menu.MoveSelection(-1);
        Assert.AreEqual("a", menu.SelectedEntry!.Id);

        menu.MoveSelection(1);
        menu.MoveSelection(1);
        Assert.AreEqual("c", menu.SelectedEntry!.Id);

        menu.MoveSelection(1);
        Assert.AreEqual("c", menu.SelectedEntry!.Id);
    }

    [TestMethod]
    public void Reopen_KeepsSelection()
    {
        DevMenu menu = new(SampleEntries);
        menu.Open();
        menu.Select(2);
        menu.Close();
        menu.Open();

        Assert.AreEqual(2, menu.SelectedIndex);
        Assert.AreEqual(0f, menu.OpenTimer);
    }

    [TestMethod]
    public void EmptyMenu_HasNoSelection()
    {
        DevMenu menu = new([]);
        menu.Open();
        menu.MoveSelection(1);

        Assert.IsNull(menu.SelectedEntry);
        Assert.AreEqual(0, DevMenuRenderer.GetEntryBounds(menu).Count);
    }

    [TestMethod]
    public void EntriesOutOfSectionOrder_AreRejected()
    {
        Assert.ThrowsException<ArgumentException>(() => new DevMenu([SampleEntries[2], SampleEntries[0]]));
    }

    [TestMethod]
    public void EntryBounds_FollowEntryOrder_AndDoNotOverlap()
    {
        DevMenu menu = new(SampleEntries);
        var bounds = DevMenuRenderer.GetEntryBounds(menu);

        Assert.AreEqual(SampleEntries.Length, bounds.Count);
        for (int i = 1; i < bounds.Count; i++)
        {
            Assert.IsTrue(bounds[i].Top >= bounds[i - 1].Bottom, $"row {i} overlaps");
        }
    }

    [TestMethod]
    public void SandboxEntries_FitTheVirtualScreen()
    {
        DevMenu menu = new(SandboxDevMenuEntries.All);

        var bounds = DevMenuRenderer.GetEntryBounds(menu);

        // The key hints and the panel padding follow the last row.
        Assert.IsTrue(bounds[^1].Bottom + 40 <= RenderResolution.LogicalHeight, $"last row ends at {bounds[^1].Bottom}");
    }

    [TestMethod]
    public void SandboxEntries_AreRenderable()
    {
        DevMenu menu = new(SandboxDevMenuEntries.All);
        foreach (DevMenuEntry entry in menu.Entries)
        {
            Assert.IsTrue(entry.Label.All(character => character == ' ' || PixelText.CanRender(character)), entry.Label);
        }
        Assert.AreEqual(menu.Entries.Count, menu.Entries.Select(entry => entry.Id).Distinct().Count());
    }
}
