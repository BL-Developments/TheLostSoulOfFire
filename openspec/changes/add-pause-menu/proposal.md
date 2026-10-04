## Why

Während des Spielens gibt es keine Pause: `Escape` beendet die Anwendung sofort, und Einstellungen sind nur im Titelmenü erreichbar. Spieler sollen das Spiel jederzeit anhalten, Einstellungen anpassen und den Lauf geordnet verlassen können.

## What Changes

- **BREAKING**: `Escape` beendet die Anwendung nicht mehr sofort. Im Spiel öffnet es das Pausenmenü; in Menü-Unterseiten führt es eine Ebene zurück; auf Titelkarte und Hauptmenüseite fragt es „SOLL DAS SPIEL WIRKLICH BEENDET WERDEN?“ mit `JA` und `NEIN`.
- `BEENDEN` im Hauptmenü fragt ebenfalls zuerst nach.
- Neues Pausenmenü im Stil des Hauptmenüs mit `FORTSETZEN`, `EINSTELLUNGEN`, `ERRUNGENSCHAFTEN UND STATISTIKEN` und `BEENDEN`. Das Spiel ist darunter angehalten und gedämpft sichtbar.
- `EINSTELLUNGEN` im Pausenmenü öffnet dieselben Seiten wie im Hauptmenü; Änderungen wirken sofort und werden gespeichert.
- `ERRUNGENSCHAFTEN UND STATISTIKEN` bleibt wie im Hauptmenü ein wirkungsloser Platzhalter.
- `BEENDEN` im Pausenmenü bietet `ZURÜCK ZUM HAUPTMENÜ`, `ZURÜCK ZUM DESKTOP` und `ZURÜCK`. „Zurück zum Hauptmenü“ verwirft den Lauf und zeigt direkt das geöffnete Hauptmenü; es gibt keinen zusätzlichen Hinweis auf Fortschrittsverlust.
- Während der Pause laufen Musik und Ambience leiser weiter; Soundeffekte werden angehalten und beim Fortsetzen weitergespielt.

## Capabilities

### New Capabilities

- `pause-menu`: Pausieren des laufenden Spiels per `Escape`, Pausenmenü mit Einstellungen und Beenden-Auswahl, Darstellung über dem angehaltenen Spiel und Tonverhalten während der Pause.

### Modified Capabilities

- `main-menu`: `Escape` wird zum Rückweg in Untermenüs; Beenden über `Escape` oder `BEENDEN` erfordert eine Bestätigung.

## Impact

Betroffen sind `MenuController`/`MenuModel` (Wurzelseite, neue Seiten und Ergebnisse, Escape-Auswertung), `GameWorld` (Pausenweiche, gemeinsame Menüeingabe, Rückkehr ins Hauptmenü), `Game1` (Escape nicht mehr global), `CinematicPresentation` (Pausen-Overlay, Abfragetext) und `AudioDirector` (Pausenmix). Keine neuen Assets, Abhängigkeiten oder Speicherdaten. Der noch nicht archivierte Change `add-initial-settings-menu` ändert dieselbe Escape-Anforderung; dieser Change formuliert sie vollständig neu und sollte nach ihm archiviert werden.
