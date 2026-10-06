using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheLostSoulOfFire.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Input;
using TheLostSoulOfFire.Rendering;

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
            _player.Reset(_chests[^1].Position + new Vector2(0f, 40f));
        }
    }

    private void BeginCurrencyRun()
    {
        _wallet.BeginRun(GameBalance.GlutStarterStock);
        _chests.Clear();
        _glutSparks.Clear();
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
        _loopState == ArenaLoopState.Intermission &&
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
        _presentation.BeginWaveTransition();
        _particles.EmitDeathFlame(_arena.CombatBounds.Center.ToVector2(), 18, 1f);
    }

    private void UpdateCurrency(float deltaTime, InputState input)
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
            else if (PlayerAtWaveTrigger)
            {
                StartNextWaveFromIntermission();
            }
        }

        foreach (ArenaChest arenaChest in _chests) arenaChest.Update(deltaTime);
        _chests.RemoveAll(arenaChest => arenaChest.IsGone);

        for (int index = _glutSparks.Count - 1; index >= 0; index--)
        {
            GlutSpark spark = _glutSparks[index].Advance(deltaTime);
            if (spark.IsDone) _glutSparks.RemoveAt(index);
            else _glutSparks[index] = spark;
        }
    }

    private void DrawCurrencyWorld(SpriteBatch batch, Texture2D pixel)
    {
        foreach (ArenaChest chest in _chests)
        {
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
                batch.DrawCircle(pixel, position, 40f, GameBalance.Geld * glow, 2f, 28);
            }
        }

        foreach (GlutSpark spark in _glutSparks)
        {
            Vector2 position = spark.PositionToward(_player.Position);
            batch.FillCircle(pixel, position, 9f, GameBalance.Glut * 0.35f);
            batch.FillCircle(pixel, position, 4.5f, GameBalance.GlutBright);
        }
    }

    private void DrawCurrencyHud(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        HudRenderer.DrawCurrencies(batch, pixel, _wallet.Run(Currency.Geld), _wallet.Run(Currency.Glut), _geldPulse / CurrencyPulseDuration, _glutPulse / CurrencyPulseDuration);

        if (ChestInReach() is not null && CombatActionsEnabled)
        {
            DrawCenteredPrompt(batch, pixel, viewport, "E  KISTE ÖFFNEN", GameBalance.Geld);
        }
        else if (PlayerAtWaveTrigger && CombatActionsEnabled)
        {
            DrawCenteredPrompt(batch, pixel, viewport, _waveNumber + 1 >= GameBalance.ArenaWaveCount ? "E  LETZTE WELLE STARTEN" : $"E  WELLE {CinematicPresentation.ToRoman(_waveNumber + 1)} STARTEN", GameBalance.DeathFlame);
        }
        else if (_loopState == ArenaLoopState.Intermission && CombatActionsEnabled)
        {
            PixelText.DrawCentered(batch, pixel, "WELLE GELEERT · IN DER MITTE GEHT ES WEITER", viewport.Width * 0.5f, viewport.Height - 130f, 1, GameBalance.DeathFlameBright * 0.7f);
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
