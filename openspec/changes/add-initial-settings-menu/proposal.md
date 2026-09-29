## Why

`EINSTELLUNGEN` im Titelmenü ist derzeit wirkungslos, obwohl Vollbild und mehrere Audio- und Kameraeffekte bereits im Spiel existieren. Spieler sollen diese Funktionen über ein verständliches Menü einstellen können und ihre Auswahl nach einem Neustart behalten.

## What Changes

- `EINSTELLUNGEN` öffnet im Titelmenü eine Seite mit `GAMEPLAY`, `GRAFIK`, `AUDIO` und `ZURÜCK`. `STEUERUNG` und `BARRIEREFREIHEIT` erscheinen dort zunächst als auswählbare, wirkungslose Platzhalter.
- Gameplay bietet `OPTIONALE HINWEISE` an/aus; erforderliche Interaktionshinweise bleiben sichtbar.
- Grafik bietet `VOLLBILD` an/aus und `BILDBEWEGUNG` normal/reduziert/aus. `F11` und das Einstellungsmenü steuern denselben Vollbildzustand.
- Audio bietet Regler für Gesamtlautstärke, Musik und Effekte einschließlich Ambience.
- Einstellungen werden lokal gespeichert und beim Start wiederhergestellt, einschließlich Vollbild. Fehlende oder ungültige Werte fallen auf sinnvolle Standardwerte zurück.
- Im Hauptmenü erscheint `ERRUNGENSCHAFTEN UND STATISTIKEN` als auswählbarer, wirkungsloser Platzhalter.
- `Escape` führt innerhalb der Einstellungsseiten eine Seite zurück; außerhalb dieser Seiten bleibt die bisherige Beenden-Wirkung bestehen.

## Capabilities

### New Capabilities

- `game-settings`: Einstellbare Gameplay-, Grafik- und Audiowerte, ihre Laufzeitwirkung und lokale Speicherung.

### Modified Capabilities

- `main-menu`: Einstellungen werden bedienbar; Platzhalter für Steuerung, Barrierefreiheit sowie Errungenschaften und Statistiken kommen hinzu; Rücknavigation in Einstellungsseiten.
- `adaptive-window-presentation`: Vollbild ist neben `F11` über das Menü einstellbar und wird über Sitzungen hinweg wiederhergestellt.

## Impact

Betroffen sind Menümodell/-steuerung und Darstellung, `Game1`/Fensterzustand, `GameWorld`/Bildbewegung, `AudioDirector` und eine kleine lokale Einstellungsdatei. Der erste Change bleibt auf das Titelmenü beschränkt; Pausenmenü, Tasten-Neubelegung sowie echte Errungenschaften und Statistiken folgen separat.
