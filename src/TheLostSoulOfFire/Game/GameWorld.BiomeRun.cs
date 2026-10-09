using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Audio;
using TheLostSoulOfFire.Game.Levels;
using TheLostSoulOfFire.Rendering;

namespace TheLostSoulOfFire.Game;

/// <summary>
/// Biome run (change <c>add-biome-run-flow</c>): Tür I starts biome I, three levels in a row. Travel
/// points at the end of levels 1 and 2 lead on; the guardian room of level 3 completes the biome.
/// The levels run in <see cref="GamePhase.Arena"/> like a single level, see <c>GameWorld.Levels.cs</c>.
/// </summary>
public sealed partial class GameWorld
{
    private const float BiomeCompleteFadeIn = 0.5f;
    private const float BiomeCompleteFadeOut = 0.8f;

    private BiomeRun? _biomeRun;
    /// <summary>The room counter's label inside a biome run; null outside one, where the counter reads the level's own stage.</summary>
    private string? _biomeRoomLabel;
    private string _biomeCompleteTitle = string.Empty;
    private float _biomeCompletedAt = float.NegativeInfinity;

    internal bool InBiomeRun => _biomeRun is not null;

    internal int BiomeLevel => _biomeRun?.Level ?? 0;

    internal BiomeRunState? BiomeState => _biomeRun?.State;

    /// <summary>Starts a run of biome I in <paramref name="level"/>; Tür I always starts at level 1.</summary>
    internal void StartBiomeRun(int? seed, int level, Viewport viewport)
    {
        ClearRunState();
        int runSeed = seed ?? Environment.TickCount;
        Console.WriteLine($"BIOME_SEED {runSeed}");
        BiomeRun biome = new();
        biome.Start(BiomeCatalog.One, runSeed, level);
        _biomeRun = biome;
        BeginBiomeLevel(biome, viewport);
    }

    private void BeginBiomeLevel(BiomeRun biome, Viewport viewport)
    {
        _biomeRoomLabel = BiomeRoomLabel(biome);
        BeginLevel(CreateBiomeLevelRun(biome), viewport);
    }

    private static LevelRun CreateBiomeLevelRun(BiomeRun biome) =>
        new(
            LevelLayoutGenerator.Generate(biome.LevelSeed, biome.Biome.Layout),
            guardianAtEnd: biome.IsLastLevel,
            combatRoomsEntered: biome.CombatRoomsEntered);

    private static string BiomeRoomLabel(BiomeRun biome) => $"BIOM {biome.Biome.Numeral} · LEVEL {biome.Level} · RAUM";

    /// <summary>The travel point of a level end led on: the next level's start room follows the transition, and the player keeps their state.</summary>
    private void TravelToNextBiomeLevel()
    {
        if (_biomeRun is not { } biome || !biome.TravelOn())
        {
            return;
        }

        ClearTravelPoint();
        _biomeRoomLabel = BiomeRoomLabel(biome);
        _levelRun = CreateBiomeLevelRun(biome);
        _roomTransitionElapsed = 0f;
        _loopState = ArenaLoopState.Transition;
        _presentation.BeginWaveTransition();
        _audio.Play(AudioCue.UiOpen, 0.45f);
    }

    /// <summary>The guardian room is cleared: the biome is complete and the whole run is secured.</summary>
    private void CompleteBiomeRun()
    {
        if (_biomeRun is not { } biome || !biome.CompleteBiome())
        {
            return;
        }

        SecureRunCurrencies();
        _biomeCompleteTitle = $"BIOM {biome.Biome.Numeral} ABGESCHLOSSEN";
        _biomeCompletedAt = _presentationTime;
    }

    private bool BiomeCompletionShown =>
        _biomeRun is { State: BiomeRunState.BiomeComplete } && _presentationTime - _biomeCompletedAt >= GameBalance.BiomeCompleteDuration;

    private void DrawBiomeCompleteOverlay(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        if (_biomeRun is not { State: BiomeRunState.BiomeComplete } || _phase != GamePhase.Arena)
        {
            return;
        }

        float sinceComplete = _presentationTime - _biomeCompletedAt;
        float alpha = MathHelper.Clamp(
            MathF.Min(sinceComplete / BiomeCompleteFadeIn, (GameBalance.BiomeCompleteDuration - sinceComplete) / BiomeCompleteFadeOut),
            0f,
            1f);
        // Above the combat HUD and below the wave banner, as the travel point's own summary.
        UiKit.Balances(batch, pixel, viewport.Width * 0.5f, viewport.Height - 250f, _biomeCompleteTitle, _lastSecured.Geld, _lastSecured.Glut, alpha);
    }
}
