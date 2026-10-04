using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Game;
using TheLostSoulOfFire.Menu;

namespace TheLostSoulOfFire.Rendering;

/// <summary>
/// Sandbox dev menu (F): a panel on the left over the frozen sandbox, so the field stays
/// visible. Layout and hit-testing share <see cref="Layout"/>.
/// </summary>
public static class DevMenuRenderer
{
    private const int PanelX = 32;
    private const int PanelY = 88;
    private const int PanelWidth = 500;
    private const int Padding = 24;
    private const int TitleScale = 3;
    private const int TextScale = 2;
    private const float SectionGap = 22f;
    private const float HeaderHeight = 30f;
    private const float RowHeight = 30f;
    private const float VeilAlpha = 0.3f;
    private const string EmptySection = "NOCH KEINE EINTRÄGE";
    private const string Hints = "W/S WÄHLEN · A/D ÄNDERN · ENTER · F SCHLIESSEN";

    private readonly record struct Row(DevMenuSection Section, int EntryIndex, float Y);

    /// <summary>Hit-testable row bounds in <see cref="DevMenu.Entries"/> order.</summary>
    public static IReadOnlyList<Rectangle> GetEntryBounds(DevMenu menu)
    {
        List<Rectangle> bounds = new(menu.Entries.Count);
        foreach (Row row in Layout(menu, out _, out _))
        {
            if (row.EntryIndex >= 0)
            {
                bounds.Add(new Rectangle(PanelX + Padding - 8, (int)(row.Y - 8f), PanelWidth - Padding * 2 + 16, (int)RowHeight));
            }
        }
        return bounds;
    }

    public static void Draw(SpriteBatch batch, Texture2D pixel, Viewport viewport, DevMenu menu, Func<DevMenuEntry, string?> valueOf, string? footer = null)
    {
        float reveal = Ease(menu.OpenTimer / MenuController.RevealDuration);
        batch.FillRectangle(pixel, viewport.Bounds, Color.Black * (VeilAlpha * reveal));

        IReadOnlyList<Row> rows = Layout(menu, out float panelBottom, out float hintsY);
        float bottom = hintsY + 7 * 1 + Padding;
        if (footer is not null) bottom += 22f;
        Rectangle panel = new(PanelX, PanelY, PanelWidth, (int)(bottom - PanelY));
        batch.FillRectangle(pixel, panel, new Color(6, 5, 10) * (0.88f * reveal));
        batch.DrawRectangle(pixel, panel, GameBalance.DeathFlame * (0.55f * reveal), 2f);

        float left = PanelX + Padding;
        float right = PanelX + PanelWidth - Padding;
        PixelText.Draw(batch, pixel, "DEV-MENÜ", new Vector2(left, PanelY + Padding), TitleScale, GameBalance.SoulWhite * reveal);

        Color label = GameBalance.SoulWhite * (0.72f * reveal);
        Color dim = GameBalance.SoulWhite * (0.42f * reveal);
        Color selectedColor = GameBalance.DeathFlameBright * reveal;
        foreach (Row row in rows)
        {
            if (row.EntryIndex == -1)
            {
                PixelText.Draw(batch, pixel, DevMenu.GetLabel(row.Section), new Vector2(left, row.Y), TextScale, dim);
                float ruleY = row.Y + 7 * TextScale + 6f;
                batch.DrawLine(pixel, new Vector2(left, ruleY), new Vector2(right, ruleY), GameBalance.DeathFlame * (0.32f * reveal), 1f);
                continue;
            }

            if (row.EntryIndex == -2)
            {
                PixelText.Draw(batch, pixel, EmptySection, new Vector2(left + 16f, row.Y), TextScale, dim * 0.8f);
                continue;
            }

            DevMenuEntry entry = menu.Entries[row.EntryIndex];
            bool selected = row.EntryIndex == menu.SelectedIndex;
            float breathe = selected ? 0.85f + MathF.Sin(menu.OpenTimer * 3f) * 0.15f : 1f;
            Color color = (selected ? selectedColor : label) * breathe;
            if (selected)
            {
                Vector2 marker = new(left + 4f, row.Y + 7f);
                batch.DrawLine(pixel, marker + new Vector2(-5f, -5f), marker + new Vector2(3f, 0f), selectedColor, 2f);
                batch.DrawLine(pixel, marker + new Vector2(3f, 0f), marker + new Vector2(-5f, 5f), selectedColor, 2f);
            }
            PixelText.Draw(batch, pixel, entry.Label, new Vector2(left + 16f, row.Y), TextScale, color);
            string? value = valueOf(entry);
            if (value is not null)
            {
                string text = entry.Kind == DevMenuEntryKind.Value ? $"- {value} +" : value;
                PixelText.Draw(batch, pixel, text, new Vector2(right - PixelText.Measure(text, TextScale), row.Y), TextScale, color);
            }
        }

        if (footer is not null)
        {
            PixelText.Draw(batch, pixel, footer, new Vector2(left, hintsY), 1, GameBalance.SoulWhite * (0.6f * reveal));
            hintsY += 22f;
        }
        PixelText.Draw(batch, pixel, Hints, new Vector2(left, hintsY), 1, dim);
    }

    /// <summary>Headers (index -1), empty-section notes (-2) and entries (their index) top to bottom.</summary>
    private static IReadOnlyList<Row> Layout(DevMenu menu, out float bottom, out float hintsY)
    {
        List<Row> rows = [];
        float y = PanelY + Padding + 7 * TitleScale + SectionGap;
        int index = 0;
        foreach (DevMenuSection section in DevMenu.Sections)
        {
            rows.Add(new Row(section, -1, y));
            y += HeaderHeight;
            bool any = false;
            foreach (DevMenuEntry _ in menu.EntriesIn(section))
            {
                rows.Add(new Row(section, index++, y));
                y += RowHeight;
                any = true;
            }
            if (!any)
            {
                rows.Add(new Row(section, -2, y));
                y += RowHeight;
            }
            y += SectionGap;
        }

        bottom = y;
        hintsY = y;
        return rows;
    }

    private static float Ease(float amount)
    {
        float value = MathHelper.Clamp(amount, 0f, 1f);
        return value * value * (3f - 2f * value);
    }
}
