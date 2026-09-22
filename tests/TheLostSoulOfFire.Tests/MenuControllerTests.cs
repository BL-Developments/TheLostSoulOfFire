using TheLostSoulOfFire.Menu;

namespace TheLostSoulOfFire.Tests;

[TestClass]
public sealed class MenuControllerTests
{
    [TestMethod]
    public void Open_PushesMainPage_WithFirstEntrySelected()
    {
        MenuController menu = new();

        menu.Open();

        Assert.IsTrue(menu.IsOpen);
        Assert.AreEqual(MenuPages.Main.Id, menu.CurrentPage.Id);
        Assert.AreEqual(0, menu.SelectedIndex);
    }

    [TestMethod]
    public void Close_ClearsStack()
    {
        MenuController menu = new();
        menu.Open();

        menu.Close();

        Assert.IsFalse(menu.IsOpen);
    }

    [TestMethod]
    public void Confirm_OnSingleplayer_PushesSingleplayerPage()
    {
        MenuController menu = new();
        menu.Open();

        MenuActionResult result = menu.Confirm();

        Assert.AreEqual(MenuActionResult.None, result);
        Assert.AreEqual(MenuPages.Singleplayer.Id, menu.CurrentPage.Id);
        Assert.AreEqual(0, menu.SelectedIndex);
    }

    [TestMethod]
    public void Confirm_OnBack_ReturnsToMainPage()
    {
        MenuController menu = new();
        menu.Open();
        menu.Confirm(); // Singleplayer -> pushes Singleplayer page
        menu.MoveSelection(1); // NEUES SPIEL -> SPIEL LADEN
        menu.MoveSelection(1); // SPIEL LADEN -> ZURÜCK

        MenuActionResult result = menu.Confirm();

        Assert.AreEqual(MenuActionResult.None, result);
        Assert.AreEqual(MenuPages.Main.Id, menu.CurrentPage.Id);
        Assert.AreEqual(0, menu.SelectedIndex);
    }

    [TestMethod]
    public void Confirm_OnBack_ViaWraparoundSelection_ReturnsToMainPage()
    {
        MenuController menu = new();
        menu.Open();
        menu.Confirm(); // Singleplayer -> pushes Singleplayer page
        menu.MoveSelection(-1); // NEUES SPIEL -> wraps to ZURÜCK (last entry)
        Assert.AreEqual(MenuEntryId.Back, menu.CurrentPage.Entries[menu.SelectedIndex].Id);

        MenuActionResult result = menu.Confirm();

        Assert.AreEqual(MenuActionResult.None, result);
        Assert.AreEqual(MenuPages.Main.Id, menu.CurrentPage.Id);
        Assert.IsTrue(menu.IsOpen);
    }

    [TestMethod]
    public void Confirm_OnNewGame_ReturnsNewGameResult()
    {
        MenuController menu = new();
        menu.Open();
        menu.Confirm(); // enter Singleplayer page, NEUES SPIEL is first entry

        MenuActionResult result = menu.Confirm();

        Assert.AreEqual(MenuActionResult.NewGame, result);
    }

    [TestMethod]
    public void Confirm_OnQuit_ReturnsQuitResult()
    {
        MenuController menu = new();
        menu.Open();
        menu.MoveSelection(-1); // wrap from EINZELSPIELER to BEENDEN

        MenuActionResult result = menu.Confirm();

        Assert.AreEqual(MenuActionResult.Quit, result);
    }

    [TestMethod]
    public void Confirm_OnPlaceholder_ReturnsNoneAndKeepsSamePage()
    {
        MenuController menu = new();
        menu.Open();
        menu.MoveSelection(1); // MEHRSPIELER

        MenuActionResult result = menu.Confirm();

        Assert.AreEqual(MenuActionResult.None, result);
        Assert.AreEqual(MenuPages.Main.Id, menu.CurrentPage.Id);
        Assert.AreEqual(1, menu.SelectedIndex);
    }

    [TestMethod]
    public void MoveSelection_WrapsAtListEnds()
    {
        MenuController menu = new();
        menu.Open();
        int count = menu.CurrentPage.Entries.Count;

        menu.MoveSelection(-1);
        Assert.AreEqual(count - 1, menu.SelectedIndex);

        menu.MoveSelection(1);
        Assert.AreEqual(0, menu.SelectedIndex);
    }

    [TestMethod]
    public void SetHoverIndex_IgnoresOutOfRangeIndex()
    {
        MenuController menu = new();
        menu.Open();
        menu.SetHoverIndex(2);

        menu.SetHoverIndex(99);

        Assert.AreEqual(2, menu.SelectedIndex);
    }

    [TestMethod]
    public void AcceptsInput_IsFalseUntilRevealDurationElapsed()
    {
        MenuController menu = new();
        menu.Open();

        Assert.IsFalse(menu.AcceptsInput);

        menu.Tick(MenuController.RevealDuration - 0.01f);
        Assert.IsFalse(menu.AcceptsInput);

        menu.Tick(0.02f);
        Assert.IsTrue(menu.AcceptsInput);
    }

    [TestMethod]
    public void Tick_DoesNothingWhileClosed()
    {
        MenuController menu = new();

        menu.Tick(10f);

        Assert.AreEqual(0f, menu.OpenTimer);
    }

    [TestMethod]
    public void Open_ResetsSelectionAndTimerEvenIfAlreadyOpen()
    {
        MenuController menu = new();
        menu.Open();
        menu.MoveSelection(2);
        menu.Tick(1f);

        menu.Open();

        Assert.AreEqual(0, menu.SelectedIndex);
        Assert.AreEqual(0f, menu.OpenTimer);
        Assert.AreEqual(MenuPages.Main.Id, menu.CurrentPage.Id);
    }
}
