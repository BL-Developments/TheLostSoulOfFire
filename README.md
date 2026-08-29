# The Lost Soul of Fire

Ein kleiner 2D-Top-Down-Roguelite-Prototyp mit MonoGame. Die spielbare Vertical Slice umfasst das **Ember Sanctum** als Hub und einen **Fire-Raid**: Portal betreten, Glutwesen besiegen, den Feueraltar aktivieren und in den Hub zurückkehren. Beim Tod wird der laufende Raid vollständig verworfen.

## Voraussetzungen

- Windows, Linux oder macOS mit OpenGL-Unterstützung
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

Die MonoGame- und Content-Pipeline-Pakete werden beim Restore automatisch bezogen. Die Solution verwendet MonoGame DesktopGL 3.8.5.1 und MonoGame.Extended 6.0.0.

## Starten und testen

```powershell
dotnet restore TheLostSoulOfFire.sln
dotnet run --project src/TheLostSoulOfFire/TheLostSoulOfFire.csproj
dotnet test TheLostSoulOfFire.sln
```

## Steuerung

- `WASD` oder Pfeiltasten: bewegen
- Maus: zielen
- Linke Maustaste halten: Feuerprojektile schießen
- `E`: Portal oder aktiven Abschlussaltar benutzen
- `Esc`: Spiel beenden

## Enthaltene Systeme

- Hub → Raid → Hub-Schleife mit frischem Run-Zustand
- Acht-Richtungs-Bewegung, Kamera und AABB-Wandkollision
- Feuerprojektile aus einem festen Pool, einfache Verfolger und Kontaktschaden
- Gesperrter Abschlussaltar bis zum letzten besiegten Gegner
- Tod mit kurzer Niederlagenphase und vollständigem Raid-Reset
- Virtuelle Auflösung (960×540), Letterboxing und getrennte Welt-/Effekt-/UI-Pässe
- Generiertes dunkles Pixel-Art-Atlas und gepoolte Feuer-/Glutpartikel

Bewusst noch nicht enthalten sind Meta-Upgrades, Händler und Inventar, Speichern/Laden, prozedurale Level, mehrere Raids, Bosse sowie finale Audio- und Grafikassets.
