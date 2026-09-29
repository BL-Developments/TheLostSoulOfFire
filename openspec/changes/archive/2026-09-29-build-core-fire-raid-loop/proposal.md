## Why

Das Projekt benötigt eine erste durchgängig spielbare Vertical Slice, die seine zentrale Identität beweist: aus einem sicheren, düsteren Zufluchtsort in eine gefährliche Fire-Raid aufzubrechen und nach Erfolg oder Tod zurückzukehren. Dieser Kern schafft eine belastbare Grundlage, bevor permanente Verbesserungen, Gegenstände, Händler und prozedurale Inhalte hinzukommen.

## What Changes

- Einen Start-Hub mit Spieler-Spawn und einem klar erkennbaren, interaktiven Raid-Eingang einführen.
- Eine kurze, handgebaute Echtzeit-Fire-Raid mit mindestens einem abschließbaren Kampfbereich bereitstellen.
- Freie Top-down-Bewegung, einen grundlegenden Feuerangriff sowie Schaden, Gegner und Spielertod einführen.
- Einen vollständigen Ablauf für Raid-Start, erfolgreichen Abschluss und Tod mit anschließender Rückkehr in den Hub bereitstellen.
- Eine düstere, feuerzentrierte visuelle Darstellung mit klarer Trennung zwischen Umgebung, Figuren, Gefahren, Feuer-Effekten und HUD etablieren.
- Eine MonoGame-DesktopGL-Projektgrundlage und eine Szenen-/Zustandstrennung schaffen, soweit sie im aktuellen Quellstand fehlt.
- Permanente Fähigkeitsverbesserungen, kaufbare Gegenstände, Inventar, dauerhafte Währungen, Speichern/Laden, Bosse und prozedurale Levelgenerierung ausdrücklich ausklammern.

## Capabilities

### New Capabilities

- `fire-raid-flow`: Start im Hub, Eintritt in eine Raid sowie Rückkehr nach Erfolg oder Tod.
- `top-down-combat`: Echtzeitbewegung, grundlegender Feuerangriff, Gegnerinteraktion, Schaden und Tod.
- `raid-level`: Laden und Abschließen eines handgebauten Raid-Levels mit begrenzter Arena und Kampfbegegnung.
- `dark-fire-presentation`: Lesbare düstere 2D-Darstellung mit Feuerlicht, Effekten und getrenntem HUD.

### Modified Capabilities

Keine. Es existieren noch keine Hauptspezifikationen, deren Anforderungen geändert werden.

## Impact

- Betrifft die MonoGame-DesktopGL-Projektstruktur, den zentralen Game Loop, Szenenverwaltung, Eingabe, Kollisionen, Rendering und Content-Verwaltung.
- Führt Laufzeitdaten für Hub, aktuelle Raid und aktuelles Level ein, jedoch noch kein persistentes Spielerprofil.
- Benötigt einfache visuelle Platzhalter beziehungsweise Projekt-Assets für Hub, Raid, Spieler, Gegner und Feuer-Effekte.
- Führt keine externen Online-Dienste, Netzwerk-APIs oder Datenbankabhängigkeiten ein.
