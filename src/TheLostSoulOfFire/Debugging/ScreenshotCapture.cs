using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheLostSoulOfFire.Debugging;

public static class ScreenshotCapture
{
    public static bool TrySaveBackBuffer(GraphicsDevice graphicsDevice, string context, out string result)
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

            int width = graphicsDevice.PresentationParameters.BackBufferWidth;
            int height = graphicsDevice.PresentationParameters.BackBufferHeight;
            Color[] pixels = new Color[width * height];
            graphicsDevice.GetBackBufferData(pixels);

            using Texture2D screenshot = new(graphicsDevice, width, height);
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
