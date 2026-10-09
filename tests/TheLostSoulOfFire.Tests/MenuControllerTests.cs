using System.Linq;
using TheLostSoulOfFire.Menu;
using TheLostSoulOfFire.Game;

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
    public void SettingsPages_OpenAndReturn_AndPlaceholdersStayInert()
    {
        MenuController menu = new();
        menu.Open();
        menu.MoveSelection(2);
        menu.Confirm();
        Assert.AreEqual(MenuPages.Settings.Id, menu.CurrentPage.Id);
        Assert.AreEqual(7, menu.CurrentPage.Entries.Count);
        menu.SetHoverIndex(3);
        menu.Confirm();
        Assert.AreEqual(MenuPages.Settings.Id, menu.CurrentPage.Id);
        menu.SetHoverIndex(1);
        menu.Confirm();
        Assert.AreEqual(MenuPages.Graphics.Id, menu.CurrentPage.Id);
        Assert.IsTrue(menu.GoBack());
        Assert.AreEqual(MenuPages.Settings.Id, menu.CurrentPage.Id);
    }

    [TestMethod]
    public void Confirm_EitherBestManAnswer_ShowsThanksAndBackReturnsToSettings()
    {
        foreach (int answer in new[] { 0, 1 })
        {
            MenuController menu = new();
            menu.Open();
            menu.SetHoverIndex(2);
            menu.Confirm();
            menu.SetHoverIndex(5);
            menu.Confirm();
            Assert.AreEqual(MenuPages.BestManQuestion.Id, menu.CurrentPage.Id);
            menu.SetHoverIndex(answer);

            menu.Confirm();

            Assert.AreEqual(MenuPages.BestManThanks.Id, menu.CurrentPage.Id);
            menu.Confirm();
            Assert.AreEqual(MenuPages.Settings.Id, menu.CurrentPage.Id);
        }
    }

    [TestMethod]
    public void SettingAdjustments_RespectTheirRangesAndDisplayCurrentValues()
    {
        GameSettings settings = new();
        MenuController menu = new(settings);
        menu.Open();
        menu.SetHoverIndex(2);
        menu.Confirm();
        menu.SetHoverIndex(2);
        menu.Confirm();
        menu.SetHoverIndex(2);
        for (int i = 0; i < 12; i++) menu.AdjustSelectedValue(1);
        Assert.AreEqual(100, settings.EffectsVolume);
        Assert.AreEqual("EFFEKTE: 100%", menu.GetLabel(menu.CurrentPage.Entries[2]));
        for (int i = 0; i < 15; i++) menu.AdjustSelectedValue(-1);
        Assert.AreEqual(0, settings.EffectsVolume);
    }

    [TestMethod]
    public void Close_ClearsStack()
    {
        MenuController menu = new();
        menu.Open();

        menu.Close();

        Assert.IsFalse(menu.IsOpen);
        Assert.IsFalse(menu.IsSettingsPage);
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
    public void Confirm_OnQuit_AsksBeforeQuitting()
    {
        MenuController menu = new();
        menu.Open();
        menu.MoveSelection(-1); // wrap from EINZELSPIELER to BEENDEN

        Assert.AreEqual(MenuActionResult.None, menu.Confirm());
        Assert.AreEqual(MenuPages.QuitConfirm.Id, menu.CurrentPage.Id);
        Assert.IsNotNull(menu.CurrentPage.Prompt);

        Assert.AreEqual(MenuActionResult.Quit, menu.Confirm()); // JA
    }

    [TestMethod]
    public void QuitConfirmation_NoAndEscapeReturnToMainMenu()
    {
        MenuController menu = new();
        menu.Open();
        menu.MoveSelection(-1);
        menu.Confirm();
        menu.SetHoverIndex(1); // NEIN
        Assert.AreEqual(MenuActionResult.None, menu.Confirm());
        Assert.AreEqual(MenuPages.Main.Id, menu.CurrentPage.Id);

        Assert.AreEqual(MenuActionResult.None, menu.HandleEscape());
        Assert.AreEqual(MenuPages.QuitConfirm.Id, menu.CurrentPage.Id);
        Assert.AreEqual(MenuActionResult.None, menu.HandleEscape());
        Assert.AreEqual(MenuPages.Main.Id, menu.CurrentPage.Id);
        Assert.IsTrue(menu.IsOpen);
    }

    [TestMethod]
    public void OpenQuitConfirmation_ShowsQuestionOverMainMenu()
    {
        MenuController menu = new();

        menu.OpenQuitConfirmation();

        Assert.AreEqual(MenuPages.QuitConfirm.Id, menu.CurrentPage.Id);
        Assert.AreEqual(MenuPages.Main, menu.RootPage);
        menu.HandleEscape();
        Assert.AreEqual(MenuPages.Main.Id, menu.CurrentPage.Id);
    }

    [TestMethod]
    public void Escape_InSubPages_GoesBackOneLevel()
    {
        MenuController menu = new();
        menu.Open();
        menu.Confirm(); // Einzelspieler
        Assert.AreEqual(MenuActionResult.None, menu.HandleEscape());
        Assert.AreEqual(MenuPages.Main.Id, menu.CurrentPage.Id);

        menu.SetHoverIndex(2);
        menu.Confirm(); // Einstellungen
        menu.SetHoverIndex(2);
        menu.Confirm(); // Audio
        menu.HandleEscape();
        Assert.AreEqual(MenuPages.Settings.Id, menu.CurrentPage.Id);
        menu.HandleEscape();
        Assert.AreEqual(MenuPages.Main.Id, menu.CurrentPage.Id);
    }

    [TestMethod]
    public void PauseMenu_HasFourEntriesWithResumeSelected()
    {
        MenuController menu = new();

        menu.Open(MenuPages.Pause);

        Assert.AreEqual(MenuPages.Pause.Id, menu.CurrentPage.Id);
        CollectionAssert.AreEqual(
            new[] { "FORTSETZEN", "EINSTELLUNGEN", "ERRUNGENSCHAFTEN UND STATISTIKEN", "BEENDEN" },
            menu.CurrentPage.Entries.Select(entry => entry.Label).ToArray());
        Assert.AreEqual(0, menu.SelectedIndex);
        Assert.AreEqual(MenuActionResult.Resume, menu.Confirm());
    }

    [TestMethod]
    public void PauseMenu_EscapeResumesAtRootAndGoesBackInSubPages()
    {
        MenuController menu = new();
        menu.Open(MenuPages.Pause);
        Assert.AreEqual(MenuActionResult.Resume, menu.HandleEscape());

        menu.SetHoverIndex(1);
        menu.Confirm();
        Assert.AreEqual(MenuPages.Settings.Id, menu.CurrentPage.Id);
        Assert.AreEqual(MenuActionResult.None, menu.HandleEscape());
        Assert.AreEqual(MenuPages.Pause.Id, menu.CurrentPage.Id);
    }

    [TestMethod]
    public void PauseMenu_AchievementsPlaceholderStaysInert()
    {
        MenuController menu = new();
        menu.Open(MenuPages.Pause);
        menu.SetHoverIndex(2);

        Assert.AreEqual(MenuActionResult.None, menu.Confirm());
        Assert.AreEqual(MenuPages.Pause.Id, menu.CurrentPage.Id);
    }

    [TestMethod]
    public void PauseMenu_QuitOffersMainMenuDesktopAndBack()
    {
        MenuController menu = new();
        menu.Open(MenuPages.Pause);
        menu.SetHoverIndex(3);
        Assert.AreEqual(MenuActionResult.None, menu.Confirm());
        Assert.AreEqual(MenuPages.PauseQuit.Id, menu.CurrentPage.Id);

        menu.SetHoverIndex(0);
        Assert.AreEqual(MenuActionResult.QuitToMainMenu, menu.Confirm());
        menu.SetHoverIndex(1);
        Assert.AreEqual(MenuActionResult.Quit, menu.Confirm());
        menu.SetHoverIndex(2);
        Assert.AreEqual(MenuActionResult.None, menu.Confirm());
        Assert.AreEqual(MenuPages.Pause.Id, menu.CurrentPage.Id);

        menu.SetHoverIndex(3);
        menu.Confirm();
        menu.HandleEscape();
        Assert.AreEqual(MenuPages.Pause.Id, menu.CurrentPage.Id);
    }

    [TestMethod]
    public void PauseAndTitleMenus_ShareSettings()
    {
        GameSettings settings = new();
        MenuController title = new(settings);
        MenuController pause = new(settings);
        pause.Open(MenuPages.Pause);
        pause.SetHoverIndex(1);
        pause.Confirm();
        pause.SetHoverIndex(2);
        pause.Confirm(); // Audio
        pause.AdjustSelectedValue(-1);

        Assert.AreEqual(90, title.Settings.MasterVolume);
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
