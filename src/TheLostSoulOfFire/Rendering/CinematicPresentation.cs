using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Menu;
using TheLostSoulOfFire.Rendering.Visuals;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Owns the short, authored beats around the arena loop. This is deliberately
/// specific to Soulfire: it is presentation timing and composition, not a
/// general cutscene or timeline system.
/// </summary>
public sealed class CinematicPresentation
{
    public const float FullIntroDuration = 1.55f;
    public const float RetryIntroDuration = 0.52f;
    public const float WaveTransitionDuration = 1.05f;
    public const float LifeFlameRevealTime = 1.05f;

    private float _titleTime;
    private float _stateTime;
    private float _introDuration = FullIntroDuration;
    private bool _quickIntro;

    public float StateTime => _stateTime;
    public bool TransitionComplete => _stateTime >= _introDuration;
    public bool WaveTransitionComplete => _stateTime >= WaveTransitionDuration;

    public void Update(float deltaTime, GamePhase gamePhase)
    {
        if (gamePhase == GamePhase.Title)
        {
            _titleTime += deltaTime;
        }
        else
        {
            _stateTime += deltaTime;
        }
    }

    public void ResetTitle()
    {
        _titleTime = 0f;
        _stateTime = 0f;
        _quickIntro = false;
    }

    public void BeginIntro(bool quick)
    {
        _stateTime = 0f;
        _quickIntro = quick;
        _introDuration = quick ? RetryIntroDuration : FullIntroDuration;
    }

    public void BeginWaveTransition()
    {
        _stateTime = 0f;
        _quickIntro = false;
    }

    public void BeginDeath() => _stateTime = 0f;

    public void BeginCompletion() => _stateTime = 0f;

    public bool ShouldDrawPlayer(ArenaLoopState loopState, bool playerDead) =>
        playerDead ||
        loopState is ArenaLoopState.Combat or ArenaLoopState.Intermission or ArenaLoopState.Transition or ArenaLoopState.Complete ||
        loopState == ArenaLoopState.Intro && (_quickIntro || _stateTime >= 0.52f);

    public bool ShouldDrawCombatHud(ArenaLoopState loopState, bool playerDead) =>
        !playerDead && loopState is ArenaLoopState.Combat or ArenaLoopState.Intermission or ArenaLoopState.Transition;

    public bool ShouldDrawAim(ArenaLoopState loopState, bool playerDead) =>
        !playerDead && loopState is ArenaLoopState.Combat or ArenaLoopState.Intermission or ArenaLoopState.Transition;

    private Vector2 _lead;

    public void UpdateCamera(
        Camera2D camera,
        ArenaLoopState loopState,
        bool playerDead,
        Vector2 playerPosition,
        Rectangle worldBounds,
        Rectangle combatBounds,
        Viewport viewport,
        float deltaTime,
        Vector2 lead = default)
    {
        Vector2 arenaCenter = combatBounds.Center.ToVector2();
        // The view leads a little toward where the player aims and runs, eased slowly so a
        // flick of the mouse never jerks the picture.
        _lead = Vector2.Lerp(_lead, lead, 1f - MathF.Exp(-deltaTime * 2.6f));
        Vector2 target = playerPosition + _lead;
        float targetZoom = 1f;
        float followSpeed = 9f;

        if (playerDead)
        {
            target = playerPosition;
            targetZoom = 1.055f;
            followSpeed = 3.2f;
        }
        else if (loopState == ArenaLoopState.Intro)
        {
            float settle = Ease(_stateTime / MathF.Max(0.01f, _introDuration));
            target = Vector2.Lerp(arenaCenter + new Vector2(0f, -46f), playerPosition, settle);
            targetZoom = MathHelper.Lerp(_quickIntro ? 0.97f : 0.9f, 1f, settle);
            followSpeed = _quickIntro ? 10f : 4.5f;
        }
        else if (loopState == ArenaLoopState.Transition)
        {
            target = Vector2.Lerp(playerPosition, arenaCenter, 0.12f);
            targetZoom = 0.975f;
            followSpeed = 5f;
        }
        else if (loopState == ArenaLoopState.Complete)
        {
            // The view turns to the furnace: its hearth a fifth of the way down the picture, above
            // the title, and the view widens with the figure's distance so it stays in the frame.
            Vector2 hearth = Arena.FurnaceHearth;
            float zoom = MathHelper.Clamp(viewport.Height * 0.68f / MathF.Max(1f, playerPosition.Y - hearth.Y), 0.6f, 0.9f);
            Vector2 framed = new(MathHelper.Lerp(hearth.X, playerPosition.X, 0.55f), hearth.Y + viewport.Height * 0.28f / zoom);
            float settle = Ease((_stateTime - 0.6f) / 2.2f);
            target = Vector2.Lerp(playerPosition, framed, settle);
            targetZoom = MathHelper.Lerp(0.92f, zoom, settle);
            followSpeed = 2.2f;
        }

        float zoomSmoothing = 1f - MathF.Exp(-deltaTime * followSpeed);
        camera.Zoom = MathHelper.Lerp(camera.Zoom, targetZoom, zoomSmoothing);
        camera.Follow(target, worldBounds, viewport, zoomSmoothing);
    }

    public void DrawWorldAccents(
        SpriteBatch batch,
        Texture2D pixel,
        ArtAssets art,
        GamePhase gamePhase,
        ArenaLoopState loopState,
        bool playerDead,
        Player player,
        Rectangle combatBounds)
    {
        Vector2 center = combatBounds.Center.ToVector2();

        if (gamePhase == GamePhase.Title && art.HasArt(VisualIds.TitleBackdrop))
        {
            return;
        }

        if (gamePhase == GamePhase.Title)
        {
            float reveal = Ease((_titleTime - 0.15f) / 1.1f);
            float breathe = 0.94f + MathF.Sin(_titleTime * 2.1f) * 0.04f;
            art.DrawLoopingEffect(
                batch,
                this,
                VisualIds.DeathFlameLoop,
                center + new Vector2(0f, -92f),
                0f,
                0.54f * breathe,
                Color.White * (0.72f * reveal));
            batch.DrawCircle(pixel, center + new Vector2(0f, -92f), 47f + breathe * 4f, GameBalance.DeathFlame * (0.12f * reveal), 2f, 28);
            return;
        }

        if (gamePhase != GamePhase.Arena)
        {
            return;
        }

        if (playerDead)
        {
            // A rendered figure falls first (death clip, 1.3 s); the Death Flame then takes the
            // body where it lies, about 0.8 m ahead of the feet along the facing.
            bool fallen = art.HasClip(VisualIds.Player, VisualClips.Death);
            float delay = fallen ? 0.85f : 0f;
            if (_stateTime < delay)
            {
                return;
            }
            Vector2 body = fallen ? FigureHeights.FallenChestOf(player.Position, player.FacingDirection) : player.Position;
            float collapse = Ease((_stateTime - delay) / 1.2f);
            float scale = MathHelper.Lerp(0.68f, 0.34f, collapse);
            float alpha = MathHelper.Lerp(1f, 0.34f, collapse) * Ease((_stateTime - delay) / 0.25f);
            art.DrawLoopingEffect(batch, player, VisualIds.DeathFlameLoop, body, 0f, scale, Color.White * alpha);
            // The flame gathering the body in: soft light closing in, no ring.
            Color gather = GameBalance.DeathFlameBright * (0.35f * (1f - collapse));
            gather.A = 0;
            art.DrawSoftSpot(batch, body, new Vector2(MathHelper.Lerp(52f, 17f, collapse), MathHelper.Lerp(30f, 10f, collapse)), gather);
            return;
        }

        if (loopState == ArenaLoopState.Intro && !_quickIntro && _stateTime is >= 0.48f and <= 1.2f)
        {
            float reveal = 1f - MathF.Abs(_stateTime - 0.82f) / 0.38f;
            reveal = MathHelper.Clamp(reveal, 0f, 1f);
            art.DrawLoopingEffect(batch, this, VisualIds.DeathFlameLoop, player.Position, 0f, 0.46f, Color.White * (0.58f * reveal));
            Color bloom = GameBalance.DeathFlameBright * (0.3f * reveal);
            bloom.A = 0;
            art.DrawSoftSpot(batch, player.Position, new Vector2(30f + reveal * 34f, 18f + reveal * 20f), bloom);
        }

        if (loopState == ArenaLoopState.Complete)
        {
            DrawLifeFlame(batch, art);
        }
    }

    public void DrawOverlay(
        SpriteBatch batch,
        Texture2D pixel,
        Viewport viewport,
        GamePhase gamePhase,
        ArenaLoopState loopState,
        bool playerDead,
        int waveNumber,
        MenuController menu)
    {
        if (gamePhase == GamePhase.Title)
        {
            DrawTitle(batch, pixel, viewport, menu);
        }
        else if (gamePhase != GamePhase.Arena)
        {
            return;
        }
        else if (playerDead)
        {
            DrawDeath(batch, pixel, viewport);
        }
        else if (loopState == ArenaLoopState.Intro)
        {
            DrawIntro(batch, pixel, viewport);
        }
        else if (loopState == ArenaLoopState.Transition)
        {
            DrawWaveTransition(batch, pixel, viewport, waveNumber + 1);
        }
        else if (loopState == ArenaLoopState.Complete)
        {
            DrawCompletion(batch, pixel, viewport);
        }
    }

    /// <summary>Middle of the Life Flame's body, where its light comes from; it burns in the cold furnace.</summary>
    public Vector2 GetLifeFlamePosition() => Arena.FurnaceHearth - new Vector2(0f, 30f * GetLifeFlameKindle());

    public float GetLifeFlameAlpha() => Ease((_stateTime - 1.05f) / 1.15f);

    /// <summary>How far the flame has grown: from a first low tongue to its full height.</summary>
    public float GetLifeFlameKindle() => MathHelper.Lerp(0.35f, 1f, Ease((_stateTime - LifeFlameRevealTime) / 1.7f));

    /// <summary>The flame's slow breath with a small, quicker flicker on top.</summary>
    public float GetLifeFlameBreath() => 0.96f + MathF.Sin(_stateTime * 1.7f) * 0.035f + MathF.Sin(_stateTime * 7.3f + 0.6f) * 0.015f;

    /// <summary>Set by the game: the painted title key art and soft light spots come from here.</summary>
    public ArtAssets? Art { get; set; }

    /// <summary>
    /// The title over its key art (tools/visuals/blender/build_title.py): the painting drifts
    /// very slowly, the far Warden flame breathes, mist moves over the water; the title sits in
    /// the dark sky, the menu to the right of the figure.
    /// </summary>
    private void DrawTitleArt(SpriteBatch batch, Texture2D pixel, Viewport viewport, MenuController menu, ArtAssets art)
    {
        float reveal = Ease((_titleTime - 0.1f) / 1.4f);
        float drift = 0.5f - 0.5f * MathF.Cos(_titleTime * MathHelper.TwoPi / 48f);
        float zoom = 1.02f + 0.03f * drift;
        Vector2 focus = new(viewport.Width * 0.52f, viewport.Height * 0.46f);
        Vector2 size = new Vector2(viewport.Width, viewport.Height) * zoom;
        Vector2 topLeft = focus - new Vector2(viewport.Width * 0.52f, viewport.Height * 0.46f) * zoom + new Vector2(-6f + 12f * drift, 0f);
        art.DrawEnvironmentStretched(batch, VisualIds.TitleBackdrop, new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)size.X, (int)size.Y));
        Vector2 Map(float u, float v) => topLeft + new Vector2(u * size.X, v * size.Y);

        // The Warden flame in the far gate and its light on the water.
        float breathe = 0.85f + 0.1f * MathF.Sin(_titleTime * 2.3f) + 0.05f * MathF.Sin(_titleTime * 5.1f);
        art.DrawSoftSpot(batch, Map(0.571f, 0.36f), new Vector2(26f, 54f) * breathe, GameBalance.DeathFlame * 0.35f);
        art.DrawSoftSpot(batch, Map(0.571f, 0.36f), new Vector2(7f, 22f) * breathe, GameBalance.SoulWhite * 0.35f);
        art.DrawSoftSpot(batch, Map(0.571f, 0.79f), new Vector2(18f, 46f) * breathe, GameBalance.DeathFlame * 0.22f);
        // Mist drifting over the sea.
        for (int i = 0; i < 7; i++)
        {
            float speed = 9f + i * 3.1f;
            float x = ((i * 233f + _titleTime * speed) % (viewport.Width + 600f)) - 300f;
            float y = viewport.Height * (0.62f + 0.035f * (i % 4)) + MathF.Sin(_titleTime * 0.2f + i) * 6f;
            art.DrawSoftSpot(batch, new Vector2(x, y), new Vector2(240f + i * 25f, 26f + (i % 3) * 8f), new Color(150, 160, 190) * 0.05f);
        }

        // Darkness: fades in from black, keeps a dark band in the sky for the title, vignette.
        batch.FillRectangle(pixel, viewport.Bounds, Color.Black * MathHelper.Lerp(1f, 0.08f, Ease(_titleTime / 1.8f)));
        art.DrawShade(batch, new Rectangle(0, 0, viewport.Width, (int)(viewport.Height * 0.46f)), Color.Black * 0.62f);
        art.DrawSoftSpot(batch, new Vector2(0f, viewport.Height), new Vector2(420f, 300f), Color.Black * 0.6f);
        art.DrawSoftSpot(batch, new Vector2(viewport.Width, viewport.Height), new Vector2(420f, 300f), Color.Black * 0.5f);
        DrawLetterbox(batch, pixel, viewport, 44, 0.94f);

        float centerX = viewport.Width * 0.5f;
        float titleY = viewport.Height * 0.15f;
        DrawTitleRules(batch, pixel, viewport, titleY - 30f, reveal);
        PixelText.DrawCentered(batch, pixel, "THE LOST", centerX, titleY - 8f, 3, new Color(190, 182, 204) * (0.85f * reveal));
        PixelText.DrawCentered(batch, pixel, "SOUL OF FIRE", centerX, titleY + 22f, 7, GameBalance.SoulWhite * reveal);
        PixelText.DrawCentered(batch, pixel, "DEATH IS NOT THE END", centerX, titleY + 84f, 3, GameBalance.DeathFlameBright * (0.62f * reveal));

        if (menu.IsOpen)
        {
            float menuReveal = Ease(menu.OpenTimer / MenuController.RevealDuration);
            DrawMenuList(batch, pixel, viewport, menu, menuReveal, _titleTime);
            return;
        }

        float promptReveal = Ease((_titleTime - 1.6f) / 0.9f);
        float promptBreathe = 0.62f + MathF.Sin(_titleTime * 2.4f) * 0.14f;
        float promptX = viewport.Width * menu.AnchorX;
        PixelText.DrawCentered(batch, pixel, "PRESS ANY KEY OR CLICK", promptX, viewport.Height * 0.66f, 2, GameBalance.SoulWhite * (promptReveal * promptBreathe));
        DrawPromptMark(batch, pixel, new Vector2(promptX, viewport.Height * 0.66f - 20f), promptReveal * promptBreathe);
    }

    private void DrawTitle(SpriteBatch batch, Texture2D pixel, Viewport viewport, MenuController menu)
    {
        if (Art is { } art && art.HasArt(VisualIds.TitleBackdrop))
        {
            DrawTitleArt(batch, pixel, viewport, menu, art);
            return;
        }

        float reveal = Ease((_titleTime - 0.1f) / 1.05f);
        float darkness = MathHelper.Lerp(0.96f, 0.68f, Ease(_titleTime / 1.25f));
        batch.FillRectangle(pixel, viewport.Bounds, Color.Black * darkness);
        DrawLetterbox(batch, pixel, viewport, 44, 0.94f);

        float centerX = viewport.Width * 0.5f;
        float titleY = viewport.Height * 0.37f;
        DrawTitleRules(batch, pixel, viewport, titleY - 42f, reveal);
        PixelText.DrawCentered(batch, pixel, "THE LOST", centerX, titleY - 24f, 2, new Color(178, 168, 190) * (0.82f * reveal));
        PixelText.DrawCentered(batch, pixel, "SOUL OF FIRE", centerX, titleY + 9f, 6, GameBalance.SoulWhite * reveal);
        PixelText.DrawCentered(batch, pixel, "DEATH IS NOT THE END", centerX, titleY + 82f, 2, GameBalance.DeathFlameBright * (0.56f * reveal));

        if (menu.IsOpen)
        {
            float menuReveal = Ease(menu.OpenTimer / MenuController.RevealDuration);
            DrawMenuList(batch, pixel, viewport, menu, menuReveal, _titleTime);
            return;
        }

        float promptReveal = Ease((_titleTime - 1.15f) / 0.75f);
        float promptBreathe = 0.58f + MathF.Sin(_titleTime * 2.4f) * 0.12f;
        PixelText.DrawCentered(
            batch,
            pixel,
            "PRESS ANY KEY OR CLICK",
            centerX,
            viewport.Height * 0.72f,
            2,
            GameBalance.SoulWhite * (promptReveal * promptBreathe));
        DrawPromptMark(batch, pixel, new Vector2(centerX, viewport.Height * 0.72f - 20f), promptReveal * promptBreathe);
    }

    private const int MenuEntryScale = 3;
    private const float MenuEntrySpacing = 36f;
    private const float MenuStartYFraction = 0.60f;
    private const float MenuHitPaddingX = 24f;
    private const float MenuHitPaddingY = 6f;
    private const float MenuPromptOffset = 44f;

    /// <summary>
    /// Hit-testable bounds for each entry of <paramref name="page"/>, laid out
    /// with the exact same constants <see cref="DrawMenuList"/> draws with so
    /// hover/click detection matches what is on screen.
    /// </summary>
    public IReadOnlyList<Rectangle> GetMenuEntryBounds(Viewport viewport, MenuPage page, MenuController? menu = null)
    {
        List<Rectangle> bounds = new(page.Entries.Count);
        float centerX = viewport.Width * (menu?.AnchorX ?? 0.5f);
        for (int i = 0; i < page.Entries.Count; i++)
        {
            int width = PixelText.Measure(menu?.GetLabel(page.Entries[i]) ?? page.Entries[i].Label, MenuEntryScale);
            int height = 7 * MenuEntryScale;
            float y = viewport.Height * MenuStartYFraction + i * MenuEntrySpacing;
            float x = centerX - width * 0.5f;
            bounds.Add(new Rectangle(
                (int)(x - MenuHitPaddingX),
                (int)(y - MenuHitPaddingY),
                width + (int)(MenuHitPaddingX * 2f),
                height + (int)(MenuHitPaddingY * 2f)));
        }
        return bounds;
    }

    /// <summary>
    /// Pause overlay drawn over the frozen world and HUD: a translucent veil keeps the
    /// game visible, the rest mirrors the title menu's letterbox, rule, type and markers.
    /// </summary>
    public void DrawPauseMenu(SpriteBatch batch, Texture2D pixel, Viewport viewport, MenuController menu)
    {
        float reveal = Ease(menu.OpenTimer / MenuController.RevealDuration);
        DrawVeil(batch, pixel, viewport, reveal);

        float centerX = viewport.Width * 0.5f;
        float headingY = viewport.Height * 0.36f;
        DrawTitleRules(batch, pixel, viewport, headingY - 26f, reveal);
        PixelText.DrawCentered(batch, pixel, PauseHeading(menu.CurrentPage.Id), centerX, headingY, 5, GameBalance.SoulWhite * reveal);
        DrawMenuList(batch, pixel, viewport, menu, reveal, menu.OpenTimer);
    }

    private const float PauseVeilAlpha = 0.66f;

    /// <summary>The heading names the page the pause menu shows.</summary>
    private static string PauseHeading(string pageId) => pageId switch
    {
        "settings" => "EINSTELLUNGEN",
        "settings_gameplay" => "GAMEPLAY",
        "settings_graphics" => "GRAFIK",
        "settings_audio" => "AUDIO",
        "pause_quit" => "BEENDEN",
        _ => "PAUSIERT"
    };

    /// <summary>The veil over the frozen world: darkened, deeper at the edges, letterboxed.</summary>
    private void DrawVeil(SpriteBatch batch, Texture2D pixel, Viewport viewport, float reveal)
    {
        batch.FillRectangle(pixel, viewport.Bounds, new Color(4, 3, 8) * (PauseVeilAlpha * reveal));
        if (Art is { } art)
        {
            art.DrawShade(batch, new Rectangle(0, 0, viewport.Width, viewport.Height / 3), Color.Black * (0.4f * reveal));
            art.DrawSoftSpot(batch, new Vector2(0f, viewport.Height), new Vector2(460f, 320f), Color.Black * (0.45f * reveal));
            art.DrawSoftSpot(batch, new Vector2(viewport.Width, viewport.Height), new Vector2(460f, 320f), Color.Black * (0.45f * reveal));
        }
        DrawLetterbox(batch, pixel, viewport, 44, 0.94f * reveal);
    }

    private const int CharacterTabScale = 3;
    private const float CharacterTabY = 92f;
    private const float CharacterTabSpacing = 220f;
    private const float CharacterRuleY = 146f;
    private const float CharacterRowsY = 190f;
    private const float CharacterRowSpacing = 46f;
    private const float CharacterLabelOffsetX = -380f;
    private const float CharacterValueRightOffsetX = 20f;
    private const float CharacterEffectOffsetX = 64f;
    private const float CharacterSecuredColumnOffsetX = 210f;

    /// <summary>
    /// Hit-testable bounds of the character menu's tabs, laid out with the same constants
    /// <see cref="DrawCharacterMenu"/> draws with, in <see cref="CharacterMenu.Tabs"/> order.
    /// </summary>
    public IReadOnlyList<Rectangle> GetCharacterTabBounds(Viewport viewport)
    {
        List<Rectangle> bounds = new(CharacterMenu.Tabs.Count);
        for (int i = 0; i < CharacterMenu.Tabs.Count; i++)
        {
            float centerX = CharacterTabCenterX(viewport, i);
            int width = PixelText.Measure(CharacterMenu.GetLabel(CharacterMenu.Tabs[i]), CharacterTabScale);
            int height = 7 * CharacterTabScale;
            bounds.Add(new Rectangle(
                (int)(centerX - width * 0.5f - MenuHitPaddingX),
                (int)(CharacterTabY - MenuHitPaddingY),
                width + (int)(MenuHitPaddingX * 2f),
                height + (int)(MenuHitPaddingY * 2f)));
        }
        return bounds;
    }

    /// <summary>
    /// Character menu over the frozen world: the pause menu's veil, letterbox and rule,
    /// with the tab bar in place of the heading and the selected tab's page below it.
    /// </summary>
    public void DrawCharacterMenu(SpriteBatch batch, Texture2D pixel, Viewport viewport, CharacterMenu menu, CharacterSheet sheet, IReadOnlyList<AbilityCard> abilities, bool canChooseAbilities)
    {
        float reveal = Ease(menu.OpenTimer / MenuController.RevealDuration);
        DrawVeil(batch, pixel, viewport, reveal);
        batch.FillRectangle(pixel, viewport.Bounds, new Color(4, 3, 8) * (0.12f * reveal));
        DrawCharacterTabs(batch, pixel, viewport, menu, reveal);
        DrawTitleRules(batch, pixel, viewport, CharacterRuleY, reveal);

        if (menu.SelectedTab == CharacterMenuTab.Character)
        {
            float centerX = viewport.Width * 0.5f;
            Rectangle page = new((int)(centerX + CharacterLabelOffsetX - 36f), (int)CharacterRowsY - 34,
                (int)(-CharacterLabelOffsetX * 2f + 72f), (int)(CharacterRowSpacing * 6f + 90f));
            UiKit.Panel(batch, pixel, page, GameBalance.DeathFlame, 0.35f * reveal, 0.94f * reveal);
            DrawCharacterPage(batch, pixel, viewport, sheet, reveal);
        }
        else if (menu.SelectedTab == CharacterMenuTab.Abilities)
        {
            AbilityPresentation.DrawCatalogue(batch, pixel, viewport, abilities, canChooseAbilities, menu.SelectedSkillSlot, menu.SkillFeedback, reveal);
        }
        else
        {
            PixelText.DrawCentered(batch, pixel, "NOCH NICHT VERFÜGBAR", viewport.Width * 0.5f, viewport.Height * 0.5f - 10f, MenuEntryScale, GameBalance.SoulWhite * (0.42f * reveal));
        }
    }

    private static float CharacterTabCenterX(Viewport viewport, int index) =>
        viewport.Width * 0.5f + (index - (CharacterMenu.Tabs.Count - 1) * 0.5f) * CharacterTabSpacing;

    private void DrawCharacterTabs(SpriteBatch batch, Texture2D pixel, Viewport viewport, CharacterMenu menu, float reveal)
    {
        for (int i = 0; i < CharacterMenu.Tabs.Count; i++)
        {
            CharacterMenuTab tab = CharacterMenu.Tabs[i];
            string label = CharacterMenu.GetLabel(tab);
            float centerX = CharacterTabCenterX(viewport, i);
            bool selected = tab == menu.SelectedTab;
            Color baseColor = CharacterMenu.IsPlaceholder(tab) ? GameBalance.SoulWhite * 0.42f : GameBalance.SoulWhite * 0.72f;
            Color color = selected ? GameBalance.DeathFlameBright : baseColor;
            float breathe = selected ? 0.85f + MathF.Sin(menu.OpenTimer * 3f) * 0.15f : 1f;
            PixelText.DrawCentered(batch, pixel, label, centerX, CharacterTabY, CharacterTabScale, color * (reveal * breathe));

            if (selected)
            {
                int width = PixelText.Measure(label, CharacterTabScale);
                DrawSelectionMarker(batch, pixel, new Vector2(centerX - width * 0.5f - 18f, CharacterTabY + 8.5f), GameBalance.DeathFlameBright * reveal);
                float underlineY = CharacterTabY + 7 * CharacterTabScale + 10f;
                UiKit.Divider(batch, pixel, centerX, underlineY, width + 60f, GameBalance.DeathFlameBright * (0.8f * reveal));
            }
        }
    }

    private static void DrawCharacterPage(SpriteBatch batch, Texture2D pixel, Viewport viewport, CharacterSheet sheet, float reveal)
    {
        float centerX = viewport.Width * 0.5f;
        Color label = GameBalance.SoulWhite * (0.72f * reveal);
        Color value = GameBalance.SoulWhite * reveal;
        Color effect = GameBalance.SoulWhite * (0.42f * reveal);

        DrawCharacterRow(batch, pixel, centerX, CharacterRowsY, "LEBEN", sheet.HealthText, null, label, value, effect);
        DrawCharacterRow(batch, pixel, centerX, CharacterRowsY + CharacterRowSpacing, "STÄRKE", sheet.Attributes.Strength.ToString(), sheet.WeaponDamageText, label, value, effect);
        DrawCharacterRow(batch, pixel, centerX, CharacterRowsY + CharacterRowSpacing * 2f, "FÄHIGKEITSSTÄRKE", sheet.Attributes.AbilityPower.ToString(), sheet.AbilityDamageText, label, value, effect);
        DrawCharacterRow(batch, pixel, centerX, CharacterRowsY + CharacterRowSpacing * 3f, "RÜSTUNG", sheet.Attributes.Armor.ToString(), sheet.ArmorReductionText, label, value, effect);

        float currencyY = CharacterRowsY + CharacterRowSpacing * 4f + 26f;
        PixelText.Draw(batch, pixel, "WÄHRUNGEN", new Vector2(centerX + CharacterLabelOffsetX, currencyY), 2, effect);
        float ruleY = currencyY + 7 * 2 + 8f;
        UiKit.Divider(batch, pixel, centerX, ruleY, -CharacterLabelOffsetX * 2f, GameBalance.DeathFlameBright * (0.45f * reveal));
        DrawCurrencyRow(batch, pixel, centerX, ruleY + 20f, "GELD", sheet.GeldRunText, sheet.GeldSecuredText, GameBalance.Geld * reveal, value);
        DrawCurrencyRow(batch, pixel, centerX, ruleY + 20f + CharacterRowSpacing, "GLUT", sheet.GlutRunText, sheet.GlutSecuredText, GameBalance.Glut * reveal, value);
    }

    private static void DrawCharacterRow(SpriteBatch batch, Texture2D pixel, float centerX, float y, string label, string value, string? effect, Color labelColor, Color valueColor, Color effectColor)
    {
        PixelText.Draw(batch, pixel, label, new Vector2(centerX + CharacterLabelOffsetX, y), MenuEntryScale, labelColor);
        batch.FillRectangle(pixel, new Rectangle((int)(centerX + CharacterLabelOffsetX), (int)y + 32, (int)(-CharacterLabelOffsetX * 2f), 1), effectColor * 0.2f);
        int valueWidth = PixelText.Measure(value, MenuEntryScale);
        PixelText.Draw(batch, pixel, value, new Vector2(centerX + CharacterValueRightOffsetX - valueWidth, y), MenuEntryScale, valueColor);
        if (effect is not null)
        {
            // Scale 2 text sits on the scale 3 baseline.
            PixelText.Draw(batch, pixel, effect, new Vector2(centerX + CharacterEffectOffsetX, y + 7f), 2, effectColor);
        }
    }

    private static void DrawCurrencyRow(SpriteBatch batch, Texture2D pixel, float centerX, float y, string label, string? runText, string securedText, Color labelColor, Color valueColor)
    {
        UiKit.Icon(batch, pixel, label == "GELD" ? UiIcon.Geld : UiIcon.Glut, new Vector2(centerX + CharacterLabelOffsetX - 17f, y + 8.5f), 0.8f, valueColor.A / 255f);
        PixelText.Draw(batch, pixel, label, new Vector2(centerX + CharacterLabelOffsetX, y), MenuEntryScale, labelColor);
        float x = centerX + CharacterEffectOffsetX;
        if (runText is not null)
        {
            PixelText.Draw(batch, pixel, runText, new Vector2(x, y + 7f), 2, valueColor);
            x += CharacterSecuredColumnOffsetX;
        }
        PixelText.Draw(batch, pixel, securedText, new Vector2(x, y + 7f), 2, valueColor);
    }

    private void DrawMenuList(SpriteBatch batch, Texture2D pixel, Viewport viewport, MenuController menu, float reveal, float time)
    {
        float centerX = viewport.Width * menu.AnchorX;
        IReadOnlyList<MenuEntry> entries = menu.CurrentPage.Entries;
        if (menu.CurrentPage.Prompt is { } prompt)
        {
            PixelText.DrawCentered(
                batch,
                pixel,
                prompt,
                centerX,
                viewport.Height * MenuStartYFraction - MenuPromptOffset - 8f,
                3,
                GameBalance.DeathFlameBright * (0.85f * reveal));
        }
        for (int i = 0; i < entries.Count; i++)
        {
            float y = viewport.Height * MenuStartYFraction + i * MenuEntrySpacing;
            bool selected = i == menu.SelectedIndex;
            Color baseColor = entries[i].IsPlaceholder ? GameBalance.SoulWhite * 0.42f : GameBalance.SoulWhite * 0.72f;
            Color color = selected ? GameBalance.DeathFlameBright : baseColor;
            float breathe = selected ? 0.85f + MathF.Sin(time * 3f) * 0.15f : 1f;
            if (selected)
            {
                int width = PixelText.Measure(menu.GetLabel(entries[i]), MenuEntryScale);
                DrawSelection(batch, pixel, centerX, y, width, 17, reveal);
            }
            DrawEntryLabel(batch, pixel, menu.GetLabel(entries[i]), centerX, y, color * (reveal * breathe), reveal);
            if (menu.VolumeOf(entries[i]) is { } volume)
            {
                // A volume shows as a short iron bar under its value.
                Rectangle track = new((int)(centerX - 70f), (int)y + 21, 140, 3);
                UiKit.Bar(batch, pixel, track, volume / 100f, (selected ? GameBalance.DeathFlameBright : GameBalance.SoulWhite * 0.6f) * reveal, reveal);
            }
        }
    }

    /// <summary>An entry; a setting written "NAME: VALUE" shows its value in the flame colour.</summary>
    private static void DrawEntryLabel(SpriteBatch batch, Texture2D pixel, string label, float centerX, float y, Color color, float reveal)
    {
        int split = label.IndexOf(": ", StringComparison.Ordinal);
        if (split < 0)
        {
            PixelText.DrawCentered(batch, pixel, label, centerX, y, MenuEntryScale, color);
            return;
        }

        string name = label[..(split + 2)];
        string value = label[(split + 2)..];
        float left = centerX - PixelText.Measure(label, MenuEntryScale) * 0.5f;
        PixelText.Draw(batch, pixel, name, new Vector2(left, y), MenuEntryScale, color);
        float alpha = color.A / 255f;
        PixelText.Draw(batch, pixel, value, new Vector2(left + PixelText.Measure(name, MenuEntryScale), y), MenuEntryScale,
            Color.Lerp(GameBalance.DeathFlameBright, GameBalance.SoulWhite, 0.25f) * alpha);
    }

    private static void DrawSelectionMarker(SpriteBatch batch, Texture2D pixel, Vector2 center, Color color)
    {
        UiKit.FillDiamond(batch, pixel, center, 5, color * 0.45f);
        UiKit.FillDiamond(batch, pixel, center, 3, color);
    }

    /// <summary>A soft glow behind the selected entry, flanked by two flame diamonds.</summary>
    private void DrawSelection(SpriteBatch batch, Texture2D pixel, float centerX, float y, int width, int height, float alpha)
    {
        Art?.DrawSoftSpot(batch, new Vector2(centerX, y + height * 0.5f), new Vector2(width * 0.5f + 70f, height + 6f), GameBalance.DeathFlame * (0.16f * alpha));
        DrawSelectionMarker(batch, pixel, new Vector2(centerX - width * 0.5f - 20f, y + height * 0.5f), GameBalance.DeathFlameBright * alpha);
        DrawSelectionMarker(batch, pixel, new Vector2(centerX + width * 0.5f + 20f, y + height * 0.5f), GameBalance.DeathFlameBright * alpha);
    }

    private void DrawIntro(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        if (_quickIntro)
        {
            float darkness = 1f - Ease(_stateTime / RetryIntroDuration);
            batch.FillRectangle(pixel, viewport.Bounds, Color.Black * darkness);
            return;
        }

        float worldReveal = Ease((_stateTime - 0.12f) / 0.86f);
        batch.FillRectangle(pixel, viewport.Bounds, Color.Black * (1f - worldReveal));
        DrawLetterbox(batch, pixel, viewport, (int)MathHelper.Lerp(52f, 18f, worldReveal), 0.88f * (1f - worldReveal * 0.55f));

        float placeIn = Ease((_stateTime - 0.3f) / 0.34f);
        float placeOut = 1f - Ease((_stateTime - 1.12f) / 0.32f);
        float placeAlpha = placeIn * placeOut;
        DrawTitleRules(batch, pixel, viewport, viewport.Height * 0.18f - 18f, placeAlpha * 0.8f);
        PixelText.DrawCentered(batch, pixel, "ABANDONED SOUL FURNACE", viewport.Width * 0.5f, viewport.Height * 0.18f, 4, GameBalance.SoulWhite * (0.82f * placeAlpha));

        float warning = Ease((_stateTime - 0.88f) / 0.26f) * (1f - Ease((_stateTime - 1.38f) / 0.14f));
        DrawTitleRules(batch, pixel, viewport, viewport.Height * 0.78f - 16f, warning * 0.58f);
        PixelText.DrawCentered(batch, pixel, "THE FURNACE WAKES", viewport.Width * 0.5f, viewport.Height * 0.78f, 3, GameBalance.DeathFlameBright * (0.82f * warning));
    }

    private void DrawWaveTransition(SpriteBatch batch, Texture2D pixel, Viewport viewport, int nextWave)
    {
        float open = Ease(_stateTime / 0.2f);
        float close = 1f - Ease((_stateTime - 0.82f) / 0.23f);
        float alpha = open * close;
        batch.FillRectangle(pixel, viewport.Bounds, Color.Black * (0.18f * alpha));
        DrawLetterbox(batch, pixel, viewport, 13, 0.54f * alpha);

        string label = nextWave >= GameBalance.ArenaWaveCount ? "FINAL WAVE" : $"WAVE {ToRoman(nextWave)}";
        // Above the figure (the camera centres it), settling down a little as it fades in.
        bool final = nextWave >= GameBalance.ArenaWaveCount;
        float y = viewport.Height * 0.18f + (1f - open) * 10f;
        DrawTitleRules(batch, pixel, viewport, y - 24f, alpha * 0.7f);
        PixelText.DrawCentered(batch, pixel, label, viewport.Width * 0.5f, y, 5,
            (final ? GameBalance.DeathFlameBright : GameBalance.SoulWhite) * (0.92f * alpha));
        DrawTitleRules(batch, pixel, viewport, y + 50f, alpha * 0.4f);
    }

    private void DrawDeath(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        // The world stays visible while the figure falls (about 1.3 s); then the dark closes in.
        float collapse = Ease((_stateTime - 0.55f) / 1.25f);
        float darkness = MathHelper.Lerp(0.08f, 0.8f, collapse);
        batch.FillRectangle(pixel, viewport.Bounds, Color.Black * darkness);
        DrawLetterbox(batch, pixel, viewport, (int)MathHelper.Lerp(18f, 64f, collapse), 0.9f * collapse);

        float titleReveal = Ease((_stateTime - 1.05f) / 0.6f);
        float centerX = viewport.Width * 0.5f;
        float titleY = viewport.Height * 0.42f;
        DrawTitleRules(batch, pixel, viewport, titleY - 38f, titleReveal * 0.72f);
        PixelText.DrawCentered(batch, pixel, "THE FLAME", centerX, titleY - 16f, 3, new Color(172, 158, 186) * (0.72f * titleReveal));
        PixelText.DrawCentered(batch, pixel, "IS EXTINGUISHED", centerX, titleY + 18f, 5, GameBalance.DeathFlameBright * titleReveal);
        DrawTitleRules(batch, pixel, viewport, titleY + 66f, titleReveal * 0.4f);

        float promptReveal = Ease((_stateTime - 1.0f) / 0.45f);
        float promptBreathe = 0.58f + MathF.Sin(_stateTime * 2.2f) * 0.1f;
        DrawKeyPrompt(batch, pixel, centerX, viewport.Height * 0.66f, "R", "TO RETRY", GameBalance.SoulWhite * (promptReveal * promptBreathe));
    }

    private void DrawCompletion(SpriteBatch batch, Texture2D pixel, Viewport viewport)
    {
        float calm = Ease(_stateTime / 1.4f);
        batch.FillRectangle(pixel, viewport.Bounds, Color.Black * (0.26f * calm));
        DrawLetterbox(batch, pixel, viewport, (int)MathHelper.Lerp(10f, 34f, calm), 0.72f * calm);

        float stillIn = Ease((_stateTime - 0.48f) / 0.7f);
        float stillOut = 1f - Ease((_stateTime - 2.15f) / 0.7f);
        float stillAlpha = stillIn * stillOut;
        PixelText.DrawCentered(batch, pixel, "THE ARENA IS STILL", viewport.Width * 0.5f, viewport.Height * 0.48f, 3, GameBalance.SoulWhite * (0.72f * stillAlpha));

        float endingReveal = Ease((_stateTime - 2.8f) / 1.05f);
        DrawTitleRules(batch, pixel, viewport, viewport.Height * 0.39f - 34f, endingReveal * 0.66f);
        PixelText.DrawCentered(batch, pixel, "THE LOST SOUL OF FIRE", viewport.Width * 0.5f, viewport.Height * 0.39f, 5, GameBalance.SoulWhite * endingReveal);
        Color lifeFlame = new(255, 178, 82);
        PixelText.DrawCentered(batch, pixel, "PROTOTYPE COMPLETE", viewport.Width * 0.5f, viewport.Height * 0.46f, 2, lifeFlame * (0.76f * Ease((_stateTime - 3.8f) / 0.7f)));

        float promptReveal = Ease((_stateTime - 5.2f) / 0.65f);
        float promptBreathe = 0.56f + MathF.Sin(_stateTime * 2f) * 0.1f;
        DrawKeyPrompt(batch, pixel, viewport.Width * 0.5f, viewport.Height * 0.82f, "R", "TO RESTART", GameBalance.SoulWhite * (promptReveal * promptBreathe));
    }

    /// <summary>
    /// The Life Flame in the mouth of the cold furnace (art/specs/ending.life-flame.md): an ember
    /// bed first darkens the mouth's old violet, the flame grows from a low tongue and licks out
    /// over the arch, and a few sparks rise from it.
    /// </summary>
    private void DrawLifeFlame(SpriteBatch batch, ArtAssets art)
    {
        float alpha = GetLifeFlameAlpha();
        if (alpha <= 0f)
        {
            return;
        }

        Vector2 hearth = Arena.FurnaceHearth;
        float kindle = GetLifeFlameKindle();
        float breath = GetLifeFlameBreath();
        art.DrawSoftSpot(batch, hearth + new Vector2(0f, -15f), new Vector2(31f, 20f), new Color(20, 9, 6) * (0.82f * alpha));
        // Additive (alpha 0 in the premultiplied blend): the glowing bed under the flame.
        art.DrawSoftSpot(batch, hearth + new Vector2(0f, -3f), new Vector2(30f * kindle, 7f), new Color(255, 150, 70, 0) * (0.55f * alpha));

        // The sprite's flame stands on 37 units below its centre at scale 1.
        float scale = kindle * breath;
        art.DrawLifeFlame(batch, hearth - new Vector2(0f, 37f * scale), alpha, scale);

        for (int index = 0; index < 7; index++)
        {
            float life = (_stateTime * 0.42f + index * 0.381f) % 1f;
            float sway = MathF.Sin(index * 2.3f + _stateTime * (1.1f + index * 0.13f)) * (5f + life * 12f);
            Vector2 spark = hearth + new Vector2((index - 3) * 4.5f + sway, -18f - life * 120f * (0.7f + index % 3 * 0.15f));
            float glow = alpha * kindle * MathF.Sin(life * MathF.PI) * (0.55f + 0.45f * MathF.Sin(_stateTime * 9f + index));
            art.DrawSoftSpot(batch, spark, new Vector2(2.6f), new Color(255, 196, 120, 0) * glow);
        }
    }

    private static void DrawLetterbox(SpriteBatch batch, Texture2D pixel, Viewport viewport, int height, float alpha)
    {
        if (height <= 0 || alpha <= 0f)
        {
            return;
        }

        batch.FillRectangle(pixel, new Rectangle(0, 0, viewport.Width, height), Color.Black * alpha);
        batch.FillRectangle(pixel, new Rectangle(0, viewport.Height - height, viewport.Width, height), Color.Black * alpha);
    }

    private static void DrawTitleRules(SpriteBatch batch, Texture2D pixel, Viewport viewport, float y, float alpha)
    {
        if (alpha <= 0f)
        {
            return;
        }

        UiKit.Divider(batch, pixel, viewport.Width * 0.5f, y, 500f, GameBalance.DeathFlameBright * (0.62f * alpha));
    }

    private static void DrawKeyPrompt(SpriteBatch batch, Texture2D pixel, float centerX, float y, string key, string action, Color color) =>
        UiKit.KeyLine(batch, pixel, centerX, y, key, action, color);

    private static void DrawPromptMark(SpriteBatch batch, Texture2D pixel, Vector2 center, float alpha)
    {
        Color color = GameBalance.DeathFlameBright * (0.48f * alpha);
        batch.DrawLine(pixel, center + new Vector2(-5f, 0f), center + new Vector2(0f, -5f), color, 2f);
        batch.DrawLine(pixel, center + new Vector2(0f, -5f), center + new Vector2(5f, 0f), color, 2f);
        batch.DrawLine(pixel, center + new Vector2(5f, 0f), center + new Vector2(0f, 5f), color, 2f);
        batch.DrawLine(pixel, center + new Vector2(0f, 5f), center + new Vector2(-5f, 0f), color, 2f);
    }

    private static float Ease(float amount)
    {
        float value = MathHelper.Clamp(amount, 0f, 1f);
        return value * value * (3f - 2f * value);
    }

    internal static string ToRoman(int number) => number switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        4 => "IV",
        5 => "V",
        6 => "VI",
        7 => "VII",
        8 => "VIII",
        9 => "IX",
        10 => "X",
        _ => number.ToString()
    };
}
