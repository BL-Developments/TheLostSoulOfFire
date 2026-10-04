## Why

In der Sandbox (`add-sandbox-start`) sollen sich laut #104 Charakterwerte setzen und Gegner gezielt spawnen lassen. Dafür braucht es zuerst ein eigenes Dev-Menü, das Björn mit der Taste `F` öffnet. Dieser Change liefert das Gerüst; die Einträge kommen in den Folge-Changes `add-sandbox-attribute-editor`, `add-sandbox-enemy-spawner` und `add-training-dummy`.

## What Changes

- `F` öffnet in der Sandbox das Dev-Menü, solange der Spieler nicht besiegt ist; `F` oder `Escape` schließen es wieder. Außerhalb der Sandbox bewirkt `F` nichts.
- Solange das Dev-Menü offen ist, ist das Spiel angehalten wie beim Pausenmenü (Musik leiser, Effekte pausiert, keine Spiel- oder Entwicklereingaben).
- Das Dev-Menü schließt Pausen- und Charaktermenü aus: `Escape` schließt das Dev-Menü, ohne das Pausenmenü zu öffnen, `Tab` öffnet dort nicht das Charaktermenü, und bei offenem Pausen- oder Charaktermenü öffnet `F` nichts.
- Darstellung als Tafel links über der angehaltenen Sandbox, damit das Feld sichtbar bleibt: Überschrift `DEV-MENÜ`, Abschnitte `CHARAKTER` und `GEGNER`, unten eine Tastenhilfe. Abschnitte ohne Einträge zeigen `NOCH KEINE EINTRÄGE`.
- Bedienung: `W`/`S` oder Pfeil hoch/runter wählen einen Eintrag über beide Abschnitte, `A`/`D` oder Pfeil links/rechts ändern einen Wert (mit gedrückter Umschalttaste in großen Schritten), `Enter` führt eine Aktion aus. Die Maus wählt beim Überfahren und löst per Klick aus. Die Auswahl bleibt beim erneuten Öffnen erhalten.
- Das Sandbox-HUD lautet `SANDBOX · F DEV-MENÜ`.

## Capabilities

### New Capabilities

Keine.

### Modified Capabilities

- `sandbox-mode`: Dev-Menü auf `F` mit Pausenverhalten, Abschnitten und Bedienung.

## Impact

Neu sind das Modell `Menu/DevMenu` (offen/zu, Einträge mit Abschnitt und Art, Auswahl), die Eintragsliste `Menu/SandboxDevMenuEntries` (vorerst leer) und `Rendering/DevMenuRenderer`. `GameWorld` öffnet, schließt, hält an und zeichnet das Menü; `IsGamePaused` umfasst es. Keine neuen Assets, Abhängigkeiten oder Speicherdaten.
