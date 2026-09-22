## Why

Das Spiel läuft aktuell in einem starren 1280×720-Fenster. `Game1` setzt `PreferredBackBuffer` fest und lässt `Window.AllowUserResizing` auf dem Standardwert `false` — das Fenster lässt sich weder ziehen noch maximieren, und einen Vollbildmodus gibt es nicht. Auf großen Monitoren bleibt das Spiel dadurch ein Briefmarkenbild.

Die Infrastruktur für die Lösung liegt bereits im Projekt: `Core/ResolutionManager` beherrscht Aspect-Fit-Letterboxing samt Rückrechnung von Fenster- auf Spielkoordinaten und ist in `CollisionAndResolutionTests` abgedeckt. Sie wurde nur nie mit dem laufenden `Game1`-Stack verdrahtet, weil der ursprünglich dafür vorgesehene `GameCore`-Ast vom Soulfire-Prototyp überholt wurde und seitdem toter Code ist.

## What Changes

- Das Spielfenster frei skalierbar machen und den Maximize-Button aktivieren.
- Eine feste virtuelle Auflösung von 1280×720 einführen: Welt und HUD werden unverändert in diese Auflösung gezeichnet und anschließend seitenverhältnistreu in das Fenster skaliert, mit schwarzen Balken bei abweichendem Seitenverhältnis.
- Einen randlosen Vollbildmodus einführen, der per `F11` in beide Richtungen umgeschaltet wird und die vorherige Fenstergröße innerhalb der Sitzung wiederherstellt.
- Zeigereingaben von Fenster- in Spielkoordinaten umrechnen, damit Zielen und Menübedienung bei jeder Fenstergröße korrekt bleiben.
- Die Render-Target-Kette so umstellen, dass die Soulfire-Komposition in das virtuelle Ziel statt direkt in den Fensterpuffer schreibt.
- Screenshots unabhängig von der Fenstergröße in virtueller Auflösung und ohne Letterbox-Balken aufnehmen.

## Capabilities

### New Capabilities

- `adaptive-window-presentation`: Skalierbares Fenster, virtuelle Auflösung mit Letterboxing, randloser Vollbildmodus und die Abbildung von Zeigereingaben auf Spielkoordinaten.

### Modified Capabilities

Keine. Die betroffenen Bereiche liegen in noch offenen Changes; bestehendes Spielverhalten bleibt unverändert.

## Impact

- Betrifft `Game1`, `InputState`, `SoulfireRenderer`, die Zeigerauswertung in `GameWorld` und `ScreenshotCapture`.
- Nutzt den bestehenden, getesteten `Core/ResolutionManager` unverändert weiter.
- Kein Einfluss auf Balance, Kameraführung oder HUD-Layout: Sichtfeld und HUD-Maße bleiben exakt bei 1280×720 und damit identisch zum heutigen Zustand.
- Keine Persistenz und keine neue Konfigurationsdatei; das Fenster startet immer in 1280×720.
- Der tote `GameCore`-/`Gameplay`-Ast wird nicht angefasst; sein Rückbau bleibt einem eigenen Change vorbehalten.
