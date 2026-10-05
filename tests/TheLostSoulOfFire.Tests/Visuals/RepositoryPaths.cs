using System;
using System.IO;

namespace TheLostSoulOfFire.Tests.Visuals;

/// <summary>Locates checked-in files (registry, textures, Visual-Specs) from the test output directory.</summary>
internal static class RepositoryPaths
{
    public static string Root { get; } = FindRoot();

    public static string GameProject => Path.Combine(Root, "src", "TheLostSoulOfFire");

    public static string Registry => Path.Combine(GameProject, "Content", "Visuals", "registry.json");

    public static string Specs => Path.Combine(Root, "art", "specs");

    /// <summary>The source PNG of a registry content path such as <c>Textures/Player/Animations/idle/n</c>.</summary>
    public static string ContentPng(string contentPath) => Path.Combine(GameProject, "Content", contentPath + ".png");

    private static string FindRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            string git = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(git) || File.Exists(git))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root (.git) not found above " + AppContext.BaseDirectory);
    }
}
