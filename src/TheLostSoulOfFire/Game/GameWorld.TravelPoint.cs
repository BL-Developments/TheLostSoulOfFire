using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Audio;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Game;

/// <summary>
/// Travel point (#53, #74, change <c>add-arena-travel-point</c>). Until real levels exist the arena
/// stands in for a biome: one travel point appears in the pause after wave 5. <c>E</c> opens a menu
/// with the three decisions; the world stays frozen while it is open.
/// </summary>
public sealed partial class GameWorld
{
    private const float TravelFlashDuration = 2.6f;
    private const float ExtractionSummaryDuration = 4.5f;

    private TravelPoint? _travelPoint;
    private bool _travelMenuOpen;
    private int _travelSelection;
    private (int Geld, int Glut) _travelSecured;
    private float _travelSecuredAt = float.NegativeInfinity;
    private float _extractedAt = float.NegativeInfinity;

    internal TravelPoint? ActiveTravelPoint => _travelPoint;
    internal bool TravelMenuOpen => _travelMenuOpen;

    internal void PlaceAutomatedPlayerAtTravelPoint()
    {
        if (_travelPoint is { IsDecided: false } point)
        {
            _player.Reset(point.Position + new Vector2(64f, 30f));
        }
    }

    private void SpawnTravelPointAfterWave(int clearedWave)
    {
        if (!_sandboxActive && clearedWave == GameBalance.TravelPointWave)
        {
            _travelPoint = new TravelPoint(TravelPoint.ArenaPosition(_arena.CombatBounds));
        }
    }

    private void ClearTravelPoint()
    {
        _travelPoint = null;
        _travelMenuOpen = false;
    }

    private TravelPoint? TravelPointInReach() =>
        _loopState == ArenaLoopState.Intermission && _travelPoint is { } point && point.IsInReach(_player.Position) ? point : null;

    private void OpenTravelMenu()
    {
        _travelMenuOpen = true;
        _travelSelection = 0;
        _audio.Play(AudioCue.UiOpen, 0.45f);
    }

    /// <summary>Runs instead of the world while the menu is open; true while it consumed the frame.</summary>
    private bool UpdateTravelMenu(InputState input, Viewport viewport)
    {
        if (!_travelMenuOpen)
        {
            return false;
        }

        if (_travelPoint is not { IsDecided: false } point || _loopState != ArenaLoopState.Intermission)
        {
            _travelMenuOpen = false;
            return false;
        }

        if (input.WasKeyPressed(Keys.Escape))
        {
            _travelMenuOpen = false;
            _audio.Play(AudioCue.UiBack, 0.5f);
            return true;
        }

        int previous = _travelSelection;
        if (input.WasKeyPressed(Keys.W) || input.WasKeyPressed(Keys.Up)) _travelSelection--;
        if (input.WasKeyPressed(Keys.S) || input.WasKeyPressed(Keys.Down)) _travelSelection++;
        _travelSelection = (_travelSelection + TravelPoint.ChoiceCount) % TravelPoint.ChoiceCount;
        if (_travelSelection != previous) _audio.Play(AudioCue.UiMove, 0.5f);

        TravelChoice? choice = null;
        Keys[] direct = [Keys.D1, Keys.D2, Keys.D3];
        for (int index = 0; index < direct.Length; index++)
        {
            if (input.WasKeyPressed(direct[index])) choice = (TravelChoice)index;
        }
        if (input.WasKeyPressed(Keys.E) || input.WasKeyPressed(Keys.Enter) || input.WasKeyPressed(Keys.Space))
        {
            choice ??= (TravelChoice)_travelSelection;
        }

        if (choice is { } decided)
        {
            ApplyTravelChoice(point, decided, viewport);
        }

        return true;
    }

    private void ApplyTravelChoice(TravelPoint point, TravelChoice choice, Viewport viewport)
    {
        if (!point.TryDecide(choice, _wallet, out (int Geld, int Glut) secured))
        {
            return;
        }

        _travelMenuOpen = false;
        if (choice != TravelChoice.Continue)
        {
            // Secured values are persisted at once, so a defeat later in the run cannot take them.
            _profileStore.Save(_wallet.ToProfile());
            _audio.Play(AudioCue.CurrencyGain, 0.55f);
        }

        switch (choice)
        {
            case TravelChoice.SecureAndContinue:
                _travelSecured = secured;
                _travelSecuredAt = _presentationTime;
                _geldPulse = CurrencyPulseDuration;
                _glutPulse = CurrencyPulseDuration;
                _particles.EmitBurst(point.Position, -Vector2.UnitY, 22, GameBalance.Glut, 200f, 5f);
                StartNextWaveFromIntermission();
                break;
            case TravelChoice.Continue:
                StartNextWaveFromIntermission();
                break;
            case TravelChoice.Extract:
                _lastSecured = secured;
                _phase = GameFlowRules.ExtractToHub(_phase);
                BeginAntechamber(viewport);
                _extractedAt = _presentationTime;
                break;
        }
    }

    private void DrawTravelPointWorld(SpriteBatch batch, Texture2D pixel)
    {
        if (_travelPoint is not { } point || _loopState != ArenaLoopState.Intermission)
        {
            return;
        }

        // A Warden waystone without its own art yet: a cold soul-light beacon over a ring, so it
        // reads apart from the warm money chest and the violet wave trigger.
        float pulse = 0.5f + 0.5f * MathF.Sin(_presentationTime * 2.4f);
        Color light = GameBalance.SoulWhite;
        Color edge = GameBalance.SoulSenseTrace;
        Vector2 foot = point.Position;
        _art.DrawSoftSpot(batch, foot + new Vector2(4f, 4f), new Vector2(48f, 16f), new Color(3, 3, 7) * 0.6f);
        WorldMarks.Ring(batch, pixel, foot, GameBalance.TravelPointInteractRadius * 0.62f, edge * (0.35f + pulse * 0.3f));
        Rectangle stone = new((int)foot.X - 11, (int)foot.Y - 58, 22, 58);
        batch.FillRectangle(pixel, stone, new Color(34, 32, 44));
        batch.DrawRectangle(pixel, stone, GameBalance.MetalColor, 2f);
        _art.DrawSoftSpot(batch, foot - new Vector2(0f, 70f), new Vector2(34f + pulse * 8f), edge * (0.28f + pulse * 0.12f));
        _art.DrawSoftSpot(batch, foot - new Vector2(0f, 70f), new Vector2(9f), light * (0.75f + pulse * 0.25f));
    }

    private void DrawTravelPointPrompt(SpriteBatch batch, Texture2D pixel, Viewport viewport) =>
        DrawCenteredPrompt(batch, pixel, viewport, "E  REISEPUNKT", GameBalance.SoulSenseTrace);

    private void DrawTravelPointOverlay(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        float sinceSecure = _presentationTime - _travelSecuredAt;
        if (_phase == GamePhase.Arena && sinceSecure is >= 0f and < TravelFlashDuration && !_travelMenuOpen)
        {
            float alpha = MathHelper.Clamp((TravelFlashDuration - sinceSecure) / 0.6f, 0f, 1f);
            // Above the combat HUD and below the wave banner; the bottom line is the resonance bar's.
            UiKit.Balances(batch, pixel, viewport.Width * 0.5f, viewport.Height - 250f, "TEILGESICHERT", _travelSecured.Geld, _travelSecured.Glut, alpha);
        }

        float sinceExtract = _presentationTime - _extractedAt;
        if (_phase == GamePhase.Antechamber && sinceExtract is >= 0f and < ExtractionSummaryDuration && !IsGamePaused)
        {
            float alpha = MathHelper.Clamp(MathF.Min(sinceExtract / 0.5f, (ExtractionSummaryDuration - sinceExtract) / 0.8f), 0f, 1f);
            // One line above the hub's own secured totals.
            UiKit.Balances(batch, pixel, viewport.Width * 0.5f, viewport.Height - 62f, "EXTRAHIERT", _lastSecured.Geld, _lastSecured.Glut, alpha);
        }

        if (_travelMenuOpen)
        {
            DrawTravelMenu(batch, pixel, viewport);
        }
    }

    private void DrawTravelMenu(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(4, 3, 8) * 0.55f);

        (int Geld, int Glut) half = _wallet.PreviewPartialSecure(GameBalance.TravelPointSecurePercent);
        int runGeld = _wallet.Run(Currency.Geld);
        int runGlut = _wallet.Run(Currency.Glut);
        (string Title, string Detail)[] rows =
        [
            ("TEILSICHERN UND WEITER", $"SICHERT {half.Geld} GELD UND {half.Glut} GLUT · IM RUN BLEIBEN {runGeld - half.Geld} UND {runGlut - half.Glut}"),
            ("WEITER OHNE SICHERN", $"{runGeld} GELD UND {runGlut} GLUT BLEIBEN IM RUN · BEI NIEDERLAGE VERLOREN"),
            ("EXTRAHIEREN", $"SICHERT {runGeld} GELD UND {runGlut} GLUT · DER RUN ENDET, ZURÜCK IN DEN HUB")
        ];

        const int rowHeight = 64;
        const int width = 760;
        int height = 120 + rows.Length * (rowHeight + 10) + 70;
        Rectangle panel = new(viewport.Width / 2 - width / 2, viewport.Height / 2 - height / 2, width, height);
        UiKit.Panel(batch, pixel, panel, GameBalance.SoulSenseTrace, 0.5f, 1f, 0.15f);
        PixelText.DrawCentered(batch, pixel, "REISEPUNKT", panel.Center.X, panel.Y + 30, 3, GameBalance.SoulWhite);
        PixelText.DrawCentered(batch, pixel, "EINE ENTSCHEIDUNG PRO REISEPUNKT", panel.Center.X, panel.Y + 74, 1, GameBalance.DeathFlameBright * 0.7f);

        for (int index = 0; index < rows.Length; index++)
        {
            bool selected = index == _travelSelection;
            Rectangle row = new(panel.X + 34, panel.Y + 110 + index * (rowHeight + 10), width - 68, rowHeight);
            UiKit.Panel(batch, pixel, row, selected ? GameBalance.SoulSenseTrace : UiKit.IronLine, selected ? 0.8f : 0.3f, 1f, selected ? 0.35f : 0f);
            UiKit.Key(batch, pixel, new Vector2(row.X + 16, row.Y + 20), (index + 1).ToString(), GameBalance.SoulWhite, 1f, 2, 24);
            PixelText.Draw(batch, pixel, rows[index].Title, new Vector2(row.X + 62, row.Y + 14), 2, GameBalance.SoulWhite * (selected ? 1f : 0.75f));
            PixelText.Draw(batch, pixel, rows[index].Detail, new Vector2(row.X + 62, row.Y + 40), 1, (index == 2 ? GameBalance.Geld : GameBalance.DeathFlameBright) * (selected ? 0.9f : 0.6f));
        }

        UiKit.Balances(batch, pixel, panel.Center.X, panel.Bottom - 56, "GESICHERT", _wallet.Secured(Currency.Geld), _wallet.Secured(Currency.Glut), 1f);
        PixelText.DrawCentered(batch, pixel, "W/S WÄHLEN · E BESTÄTIGEN · ESC ZURÜCK", panel.Center.X, panel.Bottom - 28, 1, GameBalance.SoulWhite * 0.6f);
    }
}
