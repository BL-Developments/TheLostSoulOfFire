using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Debugging;

public static class ScreenshotCapture
{
    /// <summary>
    /// Captures the game's virtual render target rather than the window's back buffer,
    /// so the resulting image is always the virtual resolution and never includes the
    /// letterbox bars added when the window doesn't match its aspect ratio.
    /// </summary>
    public static bool TrySaveVirtualTarget(RenderTarget2D virtualTarget, string context, out string result)
    {
        try
        {
            string root = FindRepositoryRoot() ?? Directory.GetCurrentDirectory();
            string directory = Path.Combine(root, "artifacts", "screenshots");
            Directory.CreateDirectory(directory);

            string safeContext = string.Concat(context
                .ToLowerInvariant()
                .Select(character => char.IsLetterOrDigit(character) ? character : '_'))
                .Trim('_');
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
            string path = Path.Combine(directory, $"{timestamp}_{safeContext}.png");

            int width = virtualTarget.Width;
            int height = virtualTarget.Height;
            Color[] pixels = new Color[width * height];
            virtualTarget.GetData(pixels);

            using Texture2D screenshot = new(virtualTarget.GraphicsDevice, width, height);
            screenshot.SetData(pixels);
            using FileStream stream = File.Create(path);
            screenshot.SaveAsPng(stream, width, height);

            result = Path.GetRelativePath(root, path);
            return true;
        }
        catch (Exception exception)
        {
            result = exception.Message;
            return false;
        }
    }

    private static string FindRepositoryRoot()
    {
        return FindRepositoryRoot(Directory.GetCurrentDirectory()) ??
               FindRepositoryRoot(AppContext.BaseDirectory);
    }

    private static string FindRepositoryRoot(string startPath)
    {
        DirectoryInfo directory = new(startPath);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
