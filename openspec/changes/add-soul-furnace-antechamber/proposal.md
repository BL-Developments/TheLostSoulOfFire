## Why

Der aktuelle Showcase wechselt nach dem Titel unmittelbar in eine automatische Arena-Introsequenz. Eine kurze begehbare Aschenvorhalle soll Spieler räumlich und atmosphärisch in den Abandoned Soul Furnace hineinführen, bevor das eigentliche Kampfspiel beginnt.

## What Changes

- Zwischen Titelzustand und Arena-Intro ein kampffreies Pre-Level namens **Aschenvorhalle** einführen.
- Den Spieler im Pre-Level mit seinem normalen Erscheinungsbild und seiner normalen Bewegung selbst steuern lassen.
- Die gotisch-industrielle Soulfire-Identität durch schwarzen Stein, dunkles Metall, Rohre, Ketten, erloschene Öfen und zurückhaltende violette Seelenenergie fortführen.
- Ein massives Ofen-/Kathedralentor als klaren Übergang zur Arena darstellen und durch `E` in Interaktionsreichweite aktivierbar machen.
- Optionale Soul-Sense-Details wie Seelenspuren und eingeschlossene Energie sichtbar machen, ohne Soul Sense zum Weiterkommen zu verlangen.
- Beim Eintritt eine kurze diegetische Tor- und Kameraüberleitung in das bestehende Arena-Intro ausführen.
- Arena-Retries nach Spielertod direkt in der Arena beginnen lassen; ein vollständiger Neustart nach dem Finale führt erneut über Titel und Pre-Level.

## Capabilities

### New Capabilities

- `soul-furnace-antechamber`: Begehbares, atmosphärisches Pre-Level zwischen Titel und Arena einschließlich Erkundung, Soul-Sense-Details, Toreintritt und Übergangsregeln.

### Modified Capabilities

Keine. Die zugehörige Arena-Flow-Capability liegt derzeit noch in einem offenen Change und ist noch nicht in `openspec/specs/` archiviert.

## Impact

- Betrifft den übergeordneten Spielphasenfluss in `GameWorld`, Start/Reset-Verhalten und die automatisierten Laufzeittests.
- Ergänzt einen eigenen Pre-Level-Bereich mit Bounds, Darstellung, Atmosphäre, Lichtquellen, Tortrigger und Übergangspräsentation.
- Verwendet den bestehenden Spieler, Input-, Kamera-, Soul-Sense-, Rendering- und Audiopfad; die alte ungenutzte `HubScene` wird nicht reaktiviert.
- Benötigt in der ersten Ausbaustufe keine Gegner, neue Kampfmechanik, komplexen Innenwandkollisionen oder zusätzliche externe Abhängigkeiten.
