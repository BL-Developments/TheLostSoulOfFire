using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game.Levels;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Game;

/// <summary>
/// Run currencies (#52, change <c>add-run-currencies</c>): Glut on every defeat in the arena,
/// money chests after waves 3, 6 and 9 (change <c>extend-arena-waves</c>), loss of both run balances on defeat and, as a placeholder
/// until #53, securing everything when the arena is completed.
/// </summary>
public sealed partial class GameWorld
{
    private const float CurrencyPulseDuration = 0.45f;

    private readonly CurrencyWallet _wallet = new();
    private readonly PlayerProfileStore _profileStore;
    private readonly List<ArenaChest> _chests = [];
    private readonly List<GlutSpark> _glutSparks = [];
    private float _geldPulse;
    private float _glutPulse;
    private (int Geld, int Glut) _lastSecured;

    public CurrencyWallet Wallet => _wallet;

    internal int ChestCount => _chests.Count;

    internal void PlaceAutomatedPlayerAtNewestChest()
    {
        if (_phase == GamePhase.Arena && _chests.Count > 0)
        {
            // Beside the chest, inside its reach, so captures show the chest and not the figure in front of it.
            _player.Reset(_chests[^1].Position + new Vector2(56f, 22f));
        }
    }

    private void BeginCurrencyRun()
    {
        _wallet.BeginRun(GameBalance.GlutStarterStock);
        _chests.Clear();
        _glutSparks.Clear();
        _openedChests.Clear();
        _geldPulse = 0f;
        _glutPulse = 0f;
        _lastSecured = (0, 0);
    }

    private void CreditDefeatedEnemy(Enemy enemy)
    {
        if (_phase != GamePhase.Arena || _sandboxActive || !enemy.TryClaimReward(out int glut))
        {
            return;
        }

        _wallet.Credit(Currency.Glut, glut);
        _glutPulse = CurrencyPulseDuration;
        _glutSparks.Add(new GlutSpark(enemy.Position));
    }

    private void LoseRunCurrencies()
    {
        if (_phase == GamePhase.Arena && !_sandboxActive)
        {
            _wallet.LoseRun();
        }
    }

    private void SecureRunCurrencies()
    {
        _lastSecured = _wallet.SecureAllRun();
        _chests.Clear();
        _glutSparks.Clear();
        _openedChests.Clear();
        _profileStore.Save(_wallet.ToProfile());
    }

    private void SpawnChestAfterWave(int clearedWave)
    {
        if (Array.IndexOf(GameBalance.ArenaChestWaves, clearedWave) >= 0)
        {
            _chests.Add(new ArenaChest(ArenaChest.PositionForWave(clearedWave, _arena.CombatBounds)));
        }
    }

    private ArenaChest? ChestInReach()
    {
        foreach (ArenaChest chest in _chests)
        {
            if (chest.IsInReach(_player.Position)) return chest;
        }

        return null;
    }

    private bool PlayerAtWaveTrigger =>
        !InLevel && _loopState == ArenaLoopState.Intermission &&
        Vector2.DistanceSquared(_player.Position, _arena.CombatBounds.Center.ToVector2()) <= GameBalance.WaveTriggerRadius * GameBalance.WaveTriggerRadius;

    internal void PlaceAutomatedPlayerAtWaveTrigger()
    {
        if (_phase == GamePhase.Arena && _loopState == ArenaLoopState.Intermission)
        {
            _player.Reset(_arena.CombatBounds.Center.ToVector2());
        }
    }

    internal void RequestAutomatedNextWave()
    {
        if (_phase == GamePhase.Arena && _loopState == ArenaLoopState.Intermission)
        {
            StartNextWaveFromIntermission();
        }
    }

    private void StartNextWaveFromIntermission()
    {
        _loopState = ArenaLoopState.Transition;
        ClearTravelPoint();
        _presentation.BeginWaveTransition();
        _particles.EmitDeathFlame(_arena.CombatBounds.Center.ToVector2(), 18, 1f);
    }

    private void UpdateCurrency(float deltaTime, InputState input, Viewport viewport)
    {
        _geldPulse = MathF.Max(0f, _geldPulse - deltaTime);
        _glutPulse = MathF.Max(0f, _glutPulse - deltaTime);

        if (_phase == GamePhase.Arena && !_sandboxActive && input.WasKeyPressed(Keys.E))
        {
            // A chest in reach wins over the wave trigger, so E never starts a wave by accident.
            if (ChestInReach() is { } chest)
            {
                if (chest.TryOpen())
                {
                    _audio.Play(AudioCue.ChestOpen, 0.7f);
                    _audio.Play(AudioCue.CurrencyGain, 0.5f);
                    _wallet.Credit(Currency.Geld, GameBalance.ChestGeld);
                    _geldPulse = CurrencyPulseDuration;
                    _particles.EmitBurst(chest.Position, -Vector2.UnitY, 18, GameBalance.Geld, 180f, 5f);
                }
            }
            else if (TravelPointInReach() is not null)
            {
                OpenTravelMenu();
            }
            else if (LevelExitInReach() is { } exitIndex)
            {
                TakeLevelExit(exitIndex, viewport);
            }
            else if (PlayerAtWaveTrigger)
            {
                // Starting the wave past an undecided travel point is the same as continuing without securing.
                StartNextWaveFromIntermission();
            }
        }

        foreach (ArenaChest arenaChest in _chests) arenaChest.Update(deltaTime);
        // An emptied chest leaves the game at once; its open lid lingers a moment as an image.
        foreach (ArenaChest gone in _chests)
            if (gone.IsGone) _openedChests.Add(new OpenedChest(gone.Position));
        _chests.RemoveAll(arenaChest => arenaChest.IsGone);
        for (int index = _openedChests.Count - 1; index >= 0; index--)
        {
            OpenedChest opened = _openedChests[index] with { Age = _openedChests[index].Age + deltaTime };
            if (opened.Age >= OpenedChestLinger) _openedChests.RemoveAt(index);
            else _openedChests[index] = opened;
        }

        for (int index = _glutSparks.Count - 1; index >= 0; index--)
        {
            GlutSpark spark = _glutSparks[index].Advance(deltaTime);
            if (spark.IsDone) _glutSparks.RemoveAt(index);
            else _glutSparks[index] = spark;
        }
    }

    private const float OpenedChestLinger = 1.6f;
    private readonly List<OpenedChest> _openedChests = [];

    /// <summary>Presentation only: where an emptied chest stood and for how long it has been open.</summary>
    private readonly record struct OpenedChest(Vector2 Position, float Age = 0f);

    /// <summary>The rendered chest stands this far below its gameplay position (floor centre of the box).</summary>
    private static readonly Vector2 ChestFoot = new(0f, 8f);

    private void DrawCurrencyWorld(SpriteBatch batch, Texture2D pixel)
    {
        bool rendered = _art.HasArt(VisualIds.ArenaChest);
        foreach (OpenedChest opened in _openedChests)
        {
            float fade = 1f - MathHelper.SmoothStep(0f, 1f, (opened.Age - 0.5f) / (OpenedChestLinger - 0.5f));
            if (rendered)
            {
                _art.DrawSoftSpot(batch, opened.Position + ChestFoot + new Vector2(4f, 2f), new Vector2(40f, 14f), new Color(3, 3, 7) * (0.6f * fade));
                _art.DrawPropFrame(batch, VisualIds.ArenaChest, "open", opened.Position + ChestFoot, 1f, Color.White * fade);
                _art.DrawSoftSpot(batch, opened.Position - new Vector2(0f, 12f), new Vector2(34f, 22f), GameBalance.Geld * (0.25f * fade * fade));
            }
        }

        foreach (ArenaChest chest in _chests)
        {
            if (rendered)
            {
                Vector2 foot = chest.Position + ChestFoot;
                float open = chest.OpenProgress;
                _art.DrawSoftSpot(batch, foot + new Vector2(4f, 2f), new Vector2(40f, 14f), new Color(3, 3, 7) * 0.6f);
                if (chest.IsOpened)
                {
                    _art.DrawPropFrame(batch, VisualIds.ArenaChest, "open", foot, open, Color.White);
                    _art.DrawSoftSpot(batch, chest.Position - new Vector2(0f, 12f + open * 6f), new Vector2(30f + open * 16f, 20f + open * 10f), GameBalance.Geld * (0.4f * open));
                }
                else
                {
                    // The unopened chest glows gold on the floor around it, breathing: no ring.
                    float glow = 0.35f + MathF.Sin(_presentationTime * 3f) * 0.12f;
                    Color gold = GameBalance.Geld * (glow * 1.5f);
                    gold.A = 0;
                    _art.DrawSoftSpot(batch, chest.Position + new Vector2(0f, 6f), new Vector2(62f, 36f), gold);
                    _art.DrawSoftSpot(batch, chest.Position, new Vector2(46f, 30f), GameBalance.Geld * (glow * 0.3f));
                    _art.DrawPropFrame(batch, VisualIds.ArenaChest, VisualClips.Default, foot, 0f, Color.White);
                }
                continue;
            }

            float fade = 1f - chest.OpenProgress;
            float lift = chest.OpenProgress * 14f;
            Vector2 position = chest.Position;
            batch.FillCircle(pixel, position + new Vector2(2f, 16f), 30f, new Color(3, 3, 7) * (0.5f * fade));
            Rectangle body = new((int)(position.X - 26f), (int)(position.Y - 12f), 52, 30);
            Rectangle lid = new((int)(position.X - 28f), (int)(position.Y - 24f - lift), 56, 13);
            batch.FillRectangle(pixel, body, new Color(58, 40, 30) * fade);
            batch.FillRectangle(pixel, lid, new Color(78, 54, 38) * fade);
            batch.DrawRectangle(pixel, body, GameBalance.Geld * (0.75f * fade), 2f);
            batch.DrawRectangle(pixel, lid, GameBalance.Geld * (0.75f * fade), 2f);
            batch.FillRectangle(pixel, new Rectangle((int)position.X - 4, (int)(position.Y - 8f), 8, 9), GameBalance.Geld * fade);
            if (!chest.IsOpened)
            {
                float glow = 0.35f + MathF.Sin(_presentationTime * 3f) * 0.12f;
                _art.DrawSoftSpot(batch, position, new Vector2(46f, 30f), GameBalance.Geld * (glow * 0.3f));
                WorldMarks.Ring(batch, pixel, position, 40f, GameBalance.Geld * glow);
            }
        }

        foreach (GlutSpark spark in _glutSparks)
        {
            // An ember flying home: a warm glow, a hot point and a short tail of fading glows.
            Vector2 position = spark.PositionToward(_player.Position);
            for (int echo = 3; echo >= 1; echo--)
            {
                GlutSpark earlier = spark with { Elapsed = MathF.Max(0f, spark.Elapsed - 0.018f * echo) };
                _art.DrawSoftSpot(batch, earlier.PositionToward(_player.Position), new Vector2(9f - echo * 1.5f), GameBalance.Glut * (0.45f - echo * 0.11f));
            }
            _art.DrawSoftSpot(batch, position, new Vector2(13f), GameBalance.Glut * 0.5f);
            _art.DrawSoftSpot(batch, position, new Vector2(4.5f), GameBalance.GlutBright);
        }
    }

    private void DrawCurrencyHud(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        HudRenderer.DrawCurrencies(batch, pixel, _wallet.Run(Currency.Geld), _wallet.Run(Currency.Glut), _geldPulse / CurrencyPulseDuration, _glutPulse / CurrencyPulseDuration);

        if (ChestInReach() is not null && CombatActionsEnabled)
        {
            DrawCenteredPrompt(batch, pixel, viewport, "E  KISTE ÖFFNEN", GameBalance.Geld);
        }
        else if (TravelPointInReach() is not null && CombatActionsEnabled)
        {
            DrawTravelPointPrompt(batch, pixel, viewport);
        }
        else if (LevelExitInReach() is not null && CombatActionsEnabled)
        {
            DrawCenteredPrompt(batch, pixel, viewport, "E  WEITER", GameBalance.DeathFlame);
        }
        else if (PlayerAtWaveTrigger && CombatActionsEnabled)
        {
            DrawCenteredPrompt(batch, pixel, viewport, _waveNumber + 1 >= GameBalance.ArenaWaveCount ? "E  LETZTE WELLE STARTEN" : $"E  WELLE {CinematicPresentation.ToRoman(_waveNumber + 1)} STARTEN", GameBalance.DeathFlame);
        }
        else if (_loopState == ArenaLoopState.Intermission && CombatActionsEnabled && !InLevel)
        {
            PixelText.DrawCentered(batch, pixel, "WELLE GELEERT · IN DER MITTE GEHT ES WEITER", viewport.Width * 0.5f, viewport.Height - 130f, 1, GameBalance.DeathFlameBright * 0.7f);
        }
        else if (_loopState == ArenaLoopState.Intermission && CombatActionsEnabled && _levelRun is { Current.Kind: not LevelRoomKind.LevelEnd })
        {
            // The exits sit on the north wall, out of view from the south gate, so the hint says where to go.
            PixelText.DrawCentered(batch, pixel, "DIE AUSGÄNGE IM NORDEN SIND OFFEN", viewport.Width * 0.5f, viewport.Height - 130f, 1, GameBalance.SoulSenseTrace * 0.8f);
        }
    }

    private void DrawSecuredSummary(SpriteBatch batch, Texture2D pixel, Viewport viewport, string prefix, int geld, int glut, float alpha) =>
        UiKit.Balances(batch, pixel, viewport.Width * 0.5f, viewport.Height - 30f, prefix, geld, glut, alpha);

    private void DrawCenteredPrompt(SpriteBatch batch, Texture2D pixel, Viewport viewport, string prompt, Color accent)
    {
        float pulse = 0.5f + MathF.Sin(_presentationTime * 4f) * 0.5f;
        UiKit.Prompt(batch, pixel, viewport.Width * 0.5f, viewport.Height - 206, prompt, accent, pulse);
    }

    /// <summary>Purely visual: the Glut is already credited when the spark starts.</summary>
    private readonly record struct GlutSpark(Vector2 Start, float Elapsed = 0f)
    {
        public bool IsDone => Elapsed >= GameBalance.GlutSparkTravelTime;

        public GlutSpark Advance(float deltaTime) => this with { Elapsed = Elapsed + deltaTime };

        public Vector2 PositionToward(Vector2 target)
        {
            float t = MathHelper.Clamp(Elapsed / GameBalance.GlutSparkTravelTime, 0f, 1f);
            float eased = t * t * (3f - 2f * t);
            Vector2 position = Vector2.Lerp(Start, target, eased);
            return position - Vector2.UnitY * (MathF.Sin(t * MathF.PI) * 60f);
        }
    }
}
