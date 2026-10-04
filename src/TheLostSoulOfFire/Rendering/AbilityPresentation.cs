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
    string Detail, string Status, float ReadyFraction, Color Accent)
{
    public AbilityDefinition Definition => RunAbilities.Definitions[(int)Ability];
    public static AbilityCard Create(RunAbility ability, RunAbilities abilities, Player player, int glut, bool combat)
    {
        var definition = RunAbilities.Definitions[(int)ability];
        var (category, summary, detail, accent) = ability switch
        {
            RunAbility.SecondWind => ("HEILUNG", "HEILT 25 LEBEN", "SOFORTIGE HEILUNG BIS ZUM MAXIMALEN LEBEN", new Color(110, 240, 175)),
            RunAbility.PiercingShot => ("KAMPF", $"{player.Attributes.ScaleAbilityDamage(40)} SCHADEN / DURCHDRINGT GEGNER", "GERADES GESCHOSS IN BLICKRICHTUNG", GameBalance.DeathFlameBright),
            RunAbility.Retreat => ("BEWEGUNG", "180 PX ZURUECK / STOSST GEGNER WEG", "KEINE UNVERWUNDBARKEIT / BOSSE UNBEWEGLICH", new Color(125, 195, 235)),
            RunAbility.Vortex => ("KONTROLLE", "ZIEHT GEGNER FUER 2 S ZUSAMMEN", "MAUSZIEL / 350 PX REICHWEITE / KEINE BOSSE", new Color(180, 140, 240)),
            RunAbility.Revenge => ("VERTEIDIGUNG", "2 S ABFANGEN / +24 FOLGESCHADEN", "NACH BLOCK: 5 S FUER DEN NAECHSTEN WAFFENTREFFER", new Color(240, 175, 100)),
            _ => ("KOMBO", "MARKIERT / +25 BEIM FOLGETREFFER", "5 S ZUM MARKIEREN / MARKE HAELT 5 S", GameBalance.GlutBright)
        };
        float cooldown = abilities.Cooldown(ability);
        string status = !combat ? "NUR IM KAMPF" : player.IsDead ? "NICHT HANDLUNGSFAEHIG" : cooldown > 0 ? $"ABKLINGZEIT {cooldown:0.0} S" :
            glut < definition.Cost ? $"{definition.Cost - glut} GLUT FEHLT" :
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
            MathHelper.Clamp(1 - cooldown / definition.Cooldown, 0, 1), accent);
    }
}

public static class AbilityPresentation
{
    public static Rectangle SlotBounds(Viewport viewport, int slot) =>
        new(viewport.Width / 2 - 482 + slot * 494, 150, 470, 30);

    public static Rectangle CatalogueBounds(Viewport viewport, int index) =>
        new(viewport.Width / 2 - 482 + index % 2 * 494, 195 + index / 2 * 142, 470, 128);

    public static void DrawHud(SpriteBatch batch, Texture2D pixel, Viewport viewport, AbilityCard card)
    {
        Rectangle bounds = new(24 + card.Slot * 356, viewport.Height - 145, 344, 74);
        Panel(batch, pixel, bounds, card.Accent, true);
        Badge(batch, pixel, new(bounds.X + 10, bounds.Y + 10), card.Slot == 0 ? "Z" : "X", card.Accent);
        Text(batch, pixel, card.Definition.Name, bounds.X + 46, bounds.Y + 11, 2, GameBalance.SoulWhite);
        Text(batch, pixel, card.Summary, bounds.X + 12, bounds.Y + 34, 1, GameBalance.SoulWhite * 0.65f);
        Text(batch, pixel, card.Status, bounds.X + 12, bounds.Y + 51, 1, card.Accent);
        string cost = $"{card.Definition.Cost} GLUT";
        Text(batch, pixel, cost, bounds.Right - PixelText.Measure(cost, 1) - 12, bounds.Y + 51, 1, GameBalance.GlutBright);
        batch.FillRectangle(pixel, new Rectangle(bounds.X + 12, bounds.Bottom - 8, bounds.Width - 24, 3), new Color(40, 35, 48));
        batch.FillRectangle(pixel, new Rectangle(bounds.X + 12, bounds.Bottom - 8, (int)((bounds.Width - 24) * card.ReadyFraction), 3), card.Accent * 0.8f);
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
            Text(batch, pixel, $"{(slot == 0 ? "Z" : "X")} / {name}", bounds.X + 14, bounds.Y + 11, 1, accent);
            string label = active ? "ZIELSLOT" : "SLOT WAEHLEN";
            Text(batch, pixel, label, bounds.Right - PixelText.Measure(label, 1) - 14, bounds.Y + 11, 1, accent);
        }
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            Rectangle b = CatalogueBounds(viewport, i);
            bool equipped = card.Slot >= 0;
            Color accent = card.Accent * alpha;
            Panel(batch, pixel, b, accent, equipped, alpha);
            Text(batch, pixel, $"{i + 1} / {card.Category}", b.X + 16, b.Y + 12, 1, accent);
            string badge = equipped ? $"AUSGERUESTET / {(card.Slot == 0 ? "Z" : "X")}" : "NICHT AUSGERUESTET";
            Text(batch, pixel, badge, b.Right - PixelText.Measure(badge, 1) - 16, b.Y + 12, 1,
                (equipped ? card.Accent : GameBalance.SoulWhite * 0.35f) * alpha);
            Text(batch, pixel, card.Definition.Name, b.X + 16, b.Y + 31, 2, GameBalance.SoulWhite * alpha);
            Text(batch, pixel, card.Summary, b.X + 16, b.Y + 57, 1, GameBalance.SoulWhite * (0.85f * alpha));
            Text(batch, pixel, card.Detail, b.X + 16, b.Y + 73, 1, GameBalance.SoulWhite * (0.5f * alpha));
            batch.FillRectangle(pixel, new Rectangle(b.X + 16, b.Y + 92, b.Width - 32, 1), accent * 0.2f);
            Text(batch, pixel, $"{card.Definition.Cost} GLUT / {card.Definition.Cooldown:0.#} S ABKLINGZEIT", b.X + 16, b.Y + 105, 1, accent);
            Text(batch, pixel, card.Status, b.Right - PixelText.Measure(card.Status, 1) - 16, b.Y + 105, 1, GameBalance.SoulWhite * (0.7f * alpha));
        }
        PixelText.DrawCentered(batch, pixel, canChoose ? "Z / X SLOT WAEHLEN / KARTE ANKLICKEN ODER 1-6 / TAB SCHLIESSEN" : "AUSWAHL GESPERRT / WECHSEL IM HUB ODER VOR / ZWISCHEN WELLEN", viewport.Width * 0.5f, 638, 1, GameBalance.SoulWhite * (0.6f * alpha));
        if (feedback.Length > 0)
            PixelText.DrawCentered(batch, pixel, feedback, viewport.Width * 0.5f, 658, 1, GameBalance.GlutBright * alpha);
    }

    private static void Text(SpriteBatch batch, Texture2D pixel, string text, float x, float y, int scale, Color color) =>
        PixelText.Draw(batch, pixel, text, new Vector2(x, y), scale, color);
    private static void Badge(SpriteBatch batch, Texture2D pixel, Vector2 position, string key, Color accent)
    {
        batch.FillRectangle(pixel, new Rectangle((int)position.X, (int)position.Y, 26, 22), accent * 0.16f);
        Text(batch, pixel, key, position.X + 7, position.Y + 4, 2, accent);
    }
    private static void Panel(SpriteBatch batch, Texture2D pixel, Rectangle b, Color accent, bool selected, float alpha = 1)
    {
        batch.FillRectangle(pixel, b, new Color(10, 8, 17) * (0.95f * alpha));
        batch.FillRectangle(pixel, new Rectangle(b.X, b.Y, 3, b.Height), accent * (selected ? 1 : 0.25f));
        Color frame = accent * (selected ? 0.5f : 0.15f);
        batch.FillRectangle(pixel, new Rectangle(b.X + 3, b.Y, b.Width - 3, 1), frame);
        batch.FillRectangle(pixel, new Rectangle(b.X + 3, b.Bottom - 1, b.Width - 3, 1), frame);
        batch.FillRectangle(pixel, new Rectangle(b.Right - 1, b.Y, 1, b.Height), frame);
    }
}
