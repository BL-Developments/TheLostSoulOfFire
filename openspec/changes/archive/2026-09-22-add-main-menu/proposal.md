## Why

Der Showcase startet heute mit einer Titelkarte, die auf jede beliebige Eingabe sofort den Arenadurchlauf beginnt. Es gibt keinen Ort, an dem sich Spielmodi, Einstellungen oder ein geordnetes Beenden auswählen lassen. Ein Hauptmenü schafft diesen Einstiegspunkt und legt die Struktur an, in die Mehrspieler, Einstellungen und Spielstände später ohne Umbau eingehängt werden können.

## What Changes

- Nach der bestehenden Titelkarte ein Hauptmenü mit den Einträgen `EINZELSPIELER`, `MEHRSPIELER`, `EINSTELLUNGEN` und `BEENDEN` einführen.
- Die Titelkarte in Aufbau, Schleier, Letterbox, Zierlinien und Kameraführung unverändert lassen; bestätigende Eingabe wechselt nicht mehr direkt in den Arenaablauf, sondern blendet an der Stelle der Startaufforderung die Menüliste ein.
- `EINZELSPIELER` öffnet ein Untermenü mit `NEUES SPIEL`, `SPIEL LADEN` und `ZURÜCK`.
- `NEUES SPIEL` startet den bestehenden Arenadurchlauf; `BEENDEN` schließt die Anwendung.
- `MEHRSPIELER`, `EINSTELLUNGEN` und `SPIEL LADEN` sind auswählbar, lösen aber bewusst keine Wirkung aus.
- Menüeinträge sind gleichwertig per Maus (Hover, Klick) und Tastatur (Auf/Ab, Bestätigen) bedienbar.
- Den Zeichensatz der Pixel-Schrift um `Ä`, `Ö`, `Ü` und `ß` erweitern, damit deutsche Menütexte vollständig dargestellt werden.
- ESC behält ausdrücklich seine heutige Wirkung und beendet die Anwendung in jedem Zustand; eine Pausefunktion und eine Rückkehr aus dem laufenden Kampf in das Menü sind ausgeklammert.
- Speichern und Laden von Spielständen, Mehrspielerbetrieb und ein Einstellungsumfang sind ausgeklammert.

## Capabilities

### New Capabilities

- `main-menu`: Einstieg über Titelkarte in ein navigierbares Hauptmenü, Untermenüstruktur, Auswahl per Maus und Tastatur, Start eines neuen Spiels, Beenden der Anwendung sowie wirkungslose Platzhaltereinträge.

### Modified Capabilities

Keine. `openspec/specs/` enthält bislang keine Hauptspezifikationen; die Anforderung zum Titelzustand liegt ausschließlich als Delta in der noch nicht archivierten Änderung `document-soulfire-mvp-expansion` (`arena-showcase-flow`). Beim Archivieren jener Änderung ist deren Anforderung „Der Showcase besitzt einen Titelzustand" mit der hier festgelegten Abfolge Titel → Hauptmenü → Arenaablauf abzugleichen.

## Impact

- Betrifft den Titelzweig des Arenaablaufs, die Overlay-Darstellung der Titelkarte, die Eingabeauswertung und den Anwendungsstart.
- Führt einen Menüzustand mit Seitenstapel als eigene Ebene vor dem Arenaablauf ein; der Arenaablauf selbst und seine Zustandsfolge bleiben unverändert.
- Erweitert die Eingabeauswertung um eine Erkennung tatsächlicher Mausbewegung, damit ein ruhender Zeiger eine per Tastatur getroffene Auswahl nicht überschreibt.
- Erweitert den Glyphensatz der Pixel-Schrift; bestehende Textausgaben bleiben unberührt.
- Die dokumentierten Läufe `--audio-gameplay-test` und `--audio-death-restart-test` steuern den Titelzustand automatisiert an und benötigen einen Weg am Menü vorbei, sonst laufen sie in ihre Zeitgrenzen.
- Führt keine neuen externen Abhängigkeiten, Netzwerkdienste oder persistenten Daten ein.
