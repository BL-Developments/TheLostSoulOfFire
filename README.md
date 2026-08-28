# The Lost Soul of Fire

Ein 2D-Top-down-Roguelike auf Basis von C# und MonoGame.

## Voraussetzungen

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Entwicklung

```powershell
dotnet restore
dotnet build
dotnet run --project src/TheLostSoulOfFire
```

Mit `Escape` oder der Zurück-Taste eines Controllers wird das Spiel beendet.

## Projektstruktur

- `src/TheLostSoulOfFire` – DesktopGL-Spielprojekt
- `src/TheLostSoulOfFire/Content` – MonoGame-Content-Pipeline

DesktopGL erlaubt Builds für Windows, Linux und macOS. Spielinhalte werden in
`Content/Content.mgcb` eingetragen und beim Build durch die Content-Pipeline
verarbeitet.
