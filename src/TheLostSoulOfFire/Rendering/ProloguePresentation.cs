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
        bool playerDead)
    {
        if (playerDead)
        {
            batch.FillRectangle(pixel, viewport.Bounds, Color.Black * 0.46f);
            string failure = "YOUR FLAME GUTTERS";
            PixelText.DrawCentered(batch, pixel, failure, viewport.Width * 0.5f, viewport.Height * 0.42f, 3, GameBalance.DeathFlameBright);
            PixelText.DrawCentered(batch, pixel, "PRESS R TO RESTART THIS SECTOR", viewport.Width * 0.5f, viewport.Height * 0.54f, 2, GameBalance.SoulWhite);
            return;
        }

        if (prologue.Stage == PrologueStage.Waking && prologue.StateTime < 1.6f)
            batch.FillRectangle(pixel, viewport.Bounds, Color.Black * (0.84f - prologue.StateTime * 0.38f));

        if (prologue.SectorTime < 3.1f || prologue.Stage is PrologueStage.Arrival or PrologueStage.Complete)
        {
            float alpha = prologue.SectorTime < 2.2f ? 1f : MathHelper.Clamp((3.1f - prologue.SectorTime) / 0.9f, 0f, 1f);
            PixelText.DrawCentered(batch, pixel, prologue.SectorTitle, viewport.Width * 0.5f, 64f, 2, GameBalance.SoulWhite * alpha);
        }

        string story = prologue.StoryLine;
        if (!string.IsNullOrEmpty(story))
        {
            batch.FillRectangle(pixel, new Rectangle(120, viewport.Height - 142, viewport.Width - 240, 62), new Color(6, 5, 10) * 0.72f);
            PixelText.DrawCentered(batch, pixel, story, viewport.Width * 0.5f, viewport.Height - 120, 2, GameBalance.SoulWhite);
        }

        if (!string.IsNullOrEmpty(prologue.Objective) && prologue.Stage != PrologueStage.Complete)
        {
            PixelText.DrawCentered(batch, pixel, prologue.Objective, viewport.Width * 0.5f, viewport.Height - 65, 1, new Color(173, 161, 194));
        }

        if (prologue.Stage == PrologueStage.Complete)
        {
            batch.FillRectangle(pixel, viewport.Bounds, Color.Black * 0.28f);
            PixelText.DrawCentered(batch, pixel, "YOU ARE NOT ALONE", viewport.Width * 0.5f, viewport.Height * 0.40f, 4, GameBalance.SoulWhite);
            PixelText.DrawCentered(batch, pixel, "THE WARDEN THRESHOLD", viewport.Width * 0.5f, viewport.Height * 0.50f, 2, GameBalance.DeathFlameBright);
            PixelText.DrawCentered(batch, pixel, prologue.Objective, viewport.Width * 0.5f, viewport.Height * 0.67f, 1, new Color(170, 164, 184));
        }
    }
}
