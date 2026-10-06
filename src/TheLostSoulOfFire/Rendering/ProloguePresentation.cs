using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLostSoulOfFire.Game;

namespace TheLostSoulOfFire.Rendering;

public static class ProloguePresentation
{
    public static void DrawOverlay(
        SpriteBatch batch,
        Texture2D pixel,
        Viewport viewport,
        PrologueDirector prologue,
        bool playerDead,
        bool optionalHints = true)
    {
        float centerX = viewport.Width * 0.5f;
        if (playerDead)
        {
            batch.FillRectangle(pixel, viewport.Bounds, Color.Black * 0.5f);
            UiKit.Divider(batch, pixel, centerX, viewport.Height * 0.42f - 18f, 460f, GameBalance.DeathFlameBright * 0.55f);
            PixelText.DrawCentered(batch, pixel, "YOUR FLAME GUTTERS", centerX, viewport.Height * 0.42f, 4, GameBalance.DeathFlameBright);
            UiKit.KeyLine(batch, pixel, centerX, viewport.Height * 0.54f, "R", "TO RESTART THIS SECTOR", GameBalance.SoulWhite);
            return;
        }

        if (prologue.Stage == PrologueStage.Waking && prologue.StateTime < 1.6f)
            batch.FillRectangle(pixel, viewport.Bounds, Color.Black * (0.84f - prologue.StateTime * 0.38f));

        if (prologue.SectorTime < 3.1f || prologue.Stage is PrologueStage.Arrival or PrologueStage.Complete)
        {
            // The sector name settles from slightly below while it fades in, under a divider.
            float alpha = prologue.SectorTime < 2.2f ? 1f : MathHelper.Clamp((3.1f - prologue.SectorTime) / 0.9f, 0f, 1f);
            float settle = MathHelper.Clamp(prologue.SectorTime / 0.6f, 0f, 1f);
            alpha *= settle;
            float y = 70f + (1f - settle * settle * (3f - 2f * settle)) * 8f;
            UiKit.Divider(batch, pixel, centerX, y - 17f, 380f, GameBalance.DeathFlameBright * (0.5f * alpha));
            PixelText.DrawCentered(batch, pixel, prologue.SectorTitle, centerX, y, 4, GameBalance.SoulWhite * (0.9f * alpha));
        }

        if (optionalHints && prologue.Stage is not (PrologueStage.Arrival or PrologueStage.Complete))
        {
            Color hint = new Color(173, 161, 194) * 0.8f;
            float moveWidth = UiKit.KeyLineWidth("WASD", "MOVE", 1);
            float attackWidth = UiKit.KeyLineWidth("MOUSE", "ATTACK", 1);
            float left = centerX - (moveWidth + 28f + attackWidth) * 0.5f;
            UiKit.KeyLine(batch, pixel, left + moveWidth * 0.5f, 30f, "WASD", "MOVE", hint, 1);
            UiKit.KeyLine(batch, pixel, left + moveWidth + 28f + attackWidth * 0.5f, 30f, "MOUSE", "ATTACK", hint, 1);
        }

        string story = prologue.StoryLine;
        if (!string.IsNullOrEmpty(story))
        {
            UiKit.CaptionBand(batch, pixel, new Rectangle(80, viewport.Height - 146, viewport.Width - 160, 64), 0.78f);
            PixelText.DrawFace(batch, pixel, story, new Vector2(centerX - PixelText.MeasureFace(story, TextFace.Body, 13f) * 0.5f, viewport.Height - 121), TextFace.Body, 13f, GameBalance.SoulWhite);
        }

        if (!string.IsNullOrEmpty(prologue.Objective) && prologue.Stage != PrologueStage.Complete)
        {
            DrawObjective(batch, pixel, centerX, viewport.Height - 75, prologue.Objective, new Color(173, 161, 194));
        }

        if (prologue.Stage == PrologueStage.Complete)
        {
            batch.FillRectangle(pixel, viewport.Bounds, Color.Black * 0.32f);
            UiKit.Divider(batch, pixel, centerX, viewport.Height * 0.40f - 22f, 520f, GameBalance.DeathFlameBright * 0.6f);
            PixelText.DrawCentered(batch, pixel, "YOU ARE NOT ALONE", centerX, viewport.Height * 0.40f, 5, GameBalance.SoulWhite);
            PixelText.DrawCentered(batch, pixel, "THE WARDEN THRESHOLD", centerX, viewport.Height * 0.50f, 3, GameBalance.DeathFlameBright);
            UiKit.Divider(batch, pixel, centerX, viewport.Height * 0.50f + 34f, 360f, GameBalance.DeathFlameBright * 0.35f);
            DrawObjective(batch, pixel, centerX, viewport.Height * 0.67f, prologue.Objective, new Color(170, 164, 184));
        }
    }

    /// <summary>The current goal, led by a small flame diamond.</summary>
    private static void DrawObjective(SpriteBatch batch, Texture2D pixel, float centerX, float y, string objective, Color color)
    {
        int width = PixelText.MeasureFace(objective, TextFace.Body, 10f, 0.8f);
        float left = centerX - (width + 16f) * 0.5f;
        UiKit.FillDiamond(batch, pixel, new Vector2(left + 3f, y + 5f), 4, GameBalance.DeathFlame * 0.7f);
        UiKit.FillDiamond(batch, pixel, new Vector2(left + 3f, y + 5f), 2, GameBalance.DeathFlameBright);
        PixelText.DrawFace(batch, pixel, objective, new Vector2(left + 16f, y), TextFace.Body, 10f, color, 0.8f);
    }
}
