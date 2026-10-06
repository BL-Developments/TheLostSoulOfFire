using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Combat;
using TheLostSoulOfFire.Entities;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

/// <summary>A shared live snapshot for the combat HUD and paused skill catalogue.</summary>
public readonly record struct AbilityCard(RunAbility Ability, int Slot, string Category, string Summary,
    string Detail, string Status, float ReadyFraction, Color Accent, bool FreeCast)
{
    public string CostText => FreeCast ? "FREI" : $"{Definition.Cost} GLUT";
    public AbilityDefinition Definition => RunAbilities.Definitions[(int)Ability];
    public static AbilityCard Create(RunAbility ability, RunAbilities abilities, Player player, int glut, bool combat, bool freeCast = false)
    {
        var definition = RunAbilities.Definitions[(int)ability];
        var (category, summary, detail, accent) = ability switch
        {
            RunAbility.SecondWind => ("HEILUNG", "HEILT 25 LEBEN", "SOFORTIGE HEILUNG BIS ZUM MAXIMALEN LEBEN", new Color(110, 240, 175)),
            RunAbility.PiercingShot => ("KAMPF", $"{player.Attributes.ScaleAbilityDamage(40)} SCHADEN / DURCHDRINGT GEGNER", "GERADES GESCHOSS IN BLICKRICHTUNG", GameBalance.DeathFlameBright),
            RunAbility.Retreat => ("BEWEGUNG", "180 PX ZURÜCK / STÖSST GEGNER WEG", "KEINE UNVERWUNDBARKEIT / BOSSE UNBEWEGLICH", new Color(125, 195, 235)),
            RunAbility.Vortex => ("KONTROLLE", "ZIEHT GEGNER FÜR 2 S ZUSAMMEN", "MAUSZIEL / 350 PX REICHWEITE / KEINE BOSSE", new Color(180, 140, 240)),
            RunAbility.Revenge => ("VERTEIDIGUNG", "2 S ABFANGEN / +24 FOLGESCHADEN", "NACH BLOCK: 5 S FÜR DEN NÄCHSTEN WAFFENTREFFER", new Color(240, 175, 100)),
            _ => ("KOMBO", "MARKIERT / +25 BEIM FOLGETREFFER", "5 S ZUM MARKIEREN / MARKE HÄLT 5 S", GameBalance.GlutBright)
        };
        float cooldown = abilities.Cooldown(ability);
        string status = !combat ? "NUR IM KAMPF" : player.IsDead ? "NICHT HANDLUNGSFÄHIG" : cooldown > 0 ? $"ABKLINGZEIT {cooldown:0.0} S" :
            !freeCast && glut < definition.Cost ? $"{definition.Cost - glut} GLUT FEHLT" :
            ability == RunAbility.SecondWind && player.Health >= player.MaxHealth ? "LEBEN VOLL" :
            ability == RunAbility.Retreat && player.IsDashing ? "AUSWEICHEN AKTIV" : "BEREIT";
        if (combat && !player.IsDead)
        {
            if (ability == RunAbility.Revenge && player.AbilityEffects.GuardRemaining > 0) status = $"SCHUTZ {player.AbilityEffects.GuardRemaining:0.0} S";
            else if (ability == RunAbility.Revenge && player.AbilityEffects.RevengeRemaining > 0) status = $"BONUS {player.AbilityEffects.RevengeRemaining:0.0} S";
            else if (ability == RunAbility.Setup && player.AbilityEffects.SetupRemaining > 0) status = $"VORBEREITET {player.AbilityEffects.SetupRemaining:0.0} S";
            else if (ability == RunAbility.Vortex && abilities.VortexRemaining > 0) status = $"SOG AKTIV {abilities.VortexRemaining:0.0} S";
        }
        return new(ability, Array.IndexOf(abilities.Slots, ability), category, summary, detail, status,
            MathHelper.Clamp(1 - cooldown / definition.Cooldown, 0, 1), accent, freeCast);
    }
}

public static class AbilityPresentation
{
    public static Rectangle SlotBounds(Viewport viewport, int slot) =>
        new(viewport.Width / 2 - 482 + slot * 494, 150, 470, 30);

    public static Rectangle CatalogueBounds(Viewport viewport, int index) =>
        new(viewport.Width / 2 - 482 + index % 2 * 494, 195 + index / 2 * 142, 470, 128);

    public static void DrawHud(SpriteBatch batch, Texture2D pixel, Viewport viewport, AbilityCard card, float time = 0f)
    {
        Rectangle bounds = new(24 + card.Slot * 356, viewport.Height - 145, 344, 74);
        bool ready = card.ReadyFraction >= 0.999f && card.Status == "BEREIT";
        float breathe = 0.5f + 0.5f * MathF.Sin(time * 2.6f + card.Slot * 1.7f);
        UiKit.Panel(batch, pixel, bounds, card.Accent, ready ? 0.85f : 0.4f, 1f, ready ? 0.12f + breathe * 0.1f : 0f);
        UiKit.Key(batch, pixel, new Vector2(bounds.X + 13, bounds.Y + 13), card.Slot == 0 ? "Z" : "X", card.Accent, 1f, 2, 26);

        int textX = bounds.X + 52;
        PixelText.DrawFace(batch, pixel, card.Definition.Name, new Vector2(textX, bounds.Y + 13), TextFace.Display, 13f, GameBalance.SoulWhite, 0.2f);
        DrawCost(batch, pixel, card.CostText, bounds.Right - 16, bounds.Y + 15, card.FreeCast);
        Body(batch, pixel, card.Summary, textX, bounds.Y + 33, GameBalance.SoulWhite * 0.74f);
        Body(batch, pixel, card.Status, textX, bounds.Y + 48, ready ? card.Accent : Color.Lerp(card.Accent, GameBalance.SoulWhite, 0.3f) * 0.9f, 0.6f);
        UiKit.Bar(batch, pixel, new Rectangle(textX + 6, bounds.Bottom - 11, bounds.Right - textX - 28, 4), card.ReadyFraction, card.Accent * 0.9f);
    }

    /// <summary>Glut cost with its ember, right-aligned at <paramref name="right"/>.</summary>
    private static void DrawCost(SpriteBatch batch, Texture2D pixel, string cost, int right, int y, bool free)
    {
        int width = BodyWidth(cost);
        Body(batch, pixel, cost, right - width, y, free ? GameBalance.SoulWhite * 0.7f : GameBalance.GlutBright);
        if (!free)
        {
            UiKit.Icon(batch, pixel, UiIcon.Glut, new Vector2(right - width - 11, y + 4.5f), 0.65f);
        }
    }

    public static void DrawCatalogue(SpriteBatch batch, Texture2D pixel, Viewport viewport,
        IReadOnlyList<AbilityCard> cards, bool canChoose, int selectedSlot, string feedback, float alpha)
    {
        for (int slot = 0; slot < 2; slot++)
        {
            Rectangle bounds = SlotBounds(viewport, slot);
            string name = "";
            foreach (var card in cards)
                if (card.Slot == slot) name = card.Definition.Name;
            bool active = slot == selectedSlot;
            Color accent = (active ? GameBalance.GlutBright : GameBalance.SoulWhite * 0.45f) * alpha;
            Panel(batch, pixel, bounds, accent, active, alpha);
            Body(batch, pixel, $"{(slot == 0 ? "Z" : "X")} / {name}", bounds.X + 16, bounds.Y + 10, accent, 0.6f);
            string label = active ? "ZIELSLOT" : "SLOT WÄHLEN";
            Body(batch, pixel, label, bounds.Right - BodyWidth(label, 0.6f) - 16, bounds.Y + 10, accent, 0.6f);
        }
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            Rectangle b = CatalogueBounds(viewport, i);
            bool equipped = card.Slot >= 0;
            Color accent = card.Accent * alpha;
            Panel(batch, pixel, b, accent, equipped, alpha);
            Body(batch, pixel, $"{i + 1} / {card.Category}", b.X + 18, b.Y + 13, accent, 0.8f);
            string badge = equipped ? $"AUSGERÜSTET / {(card.Slot == 0 ? "Z" : "X")}" : "NICHT AUSGERÜSTET";
            Body(batch, pixel, badge, b.Right - BodyWidth(badge, 0.6f) - 18, b.Y + 13,
                (equipped ? card.Accent : GameBalance.SoulWhite * 0.4f) * alpha, 0.6f);
            PixelText.DrawFace(batch, pixel, card.Definition.Name, new Vector2(b.X + 18, b.Y + 32), TextFace.Display, 15f, GameBalance.SoulWhite * alpha, 0.3f);
            Body(batch, pixel, card.Summary, b.X + 18, b.Y + 58, GameBalance.SoulWhite * (0.88f * alpha));
            Body(batch, pixel, card.Detail, b.X + 18, b.Y + 75, GameBalance.SoulWhite * (0.55f * alpha));
            UiKit.Divider(batch, pixel, b.Center.X, b.Y + 95, b.Width - 36, accent * 0.45f);
            if (!card.FreeCast)
            {
                UiKit.Icon(batch, pixel, UiIcon.Glut, new Vector2(b.X + 24, b.Y + 110.5f), 0.65f, alpha);
            }
            Body(batch, pixel, $"{card.CostText} / {card.Definition.Cooldown:0.#} S ABKLINGZEIT", b.X + (card.FreeCast ? 18 : 36), b.Y + 106, accent);
            Body(batch, pixel, card.Status, b.Right - BodyWidth(card.Status) - 18, b.Y + 106, GameBalance.SoulWhite * (0.75f * alpha));
        }
        PixelText.DrawCentered(batch, pixel, canChoose ? "Z / X SLOT WÄHLEN / KARTE ANKLICKEN ODER 1-6 / TAB SCHLIESSEN" : "AUSWAHL GESPERRT / WECHSEL IM HUB ODER VOR / ZWISCHEN WELLEN", viewport.Width * 0.5f, 638, 1, GameBalance.SoulWhite * (0.6f * alpha));
        if (feedback.Length > 0)
            PixelText.DrawCentered(batch, pixel, feedback, viewport.Width * 0.5f, 658, 1, GameBalance.GlutBright * alpha);
    }

    private const float BodyCap = 9.5f;

    private static void Body(SpriteBatch batch, Texture2D pixel, string text, float x, float y, Color color, float tracking = 0f) =>
        PixelText.DrawFace(batch, pixel, text, new Vector2(x, y), TextFace.Body, BodyCap, color, tracking);

    private static int BodyWidth(string text, float tracking = 0f) => PixelText.MeasureFace(text, TextFace.Body, BodyCap, tracking);
    private static void Panel(SpriteBatch batch, Texture2D pixel, Rectangle b, Color accent, bool selected, float alpha = 1) =>
        UiKit.Panel(batch, pixel, b, accent, selected ? 0.9f : 0.3f, alpha, selected ? 0.18f : 0f);
}
