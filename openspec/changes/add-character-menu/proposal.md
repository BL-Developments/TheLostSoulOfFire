## Why

Seit `add-player-attributes` hat der Spieler Stärke, Fähigkeitsstärke und Rüstung, aber im Spiel sieht man sie nirgends. Björn möchte ein zweites Menü neben dem Pausenmenü: Mit `Tab` öffnet sich ein Fenster mit einer Reiterleiste oben (`CHARAKTER`, `MAP`, `SKILLS`). Die Charakterseite zeigt die Eigenschaften des Charakters; Map und Skills sind vorerst Platzhalter, die später mit Karte und Skilltrees gefüllt werden.

## What Changes

- Neues Charaktermenü, das in jeder Spielphase außer der Titelphase mit `Tab` geöffnet und mit `Tab` oder `Escape` wieder geschlossen wird.
- Solange es offen ist, ist das Spiel angehalten wie beim Pausenmenü: kein Spielzustand schreitet fort, Spiel- und Entwicklereingaben wirken nicht, Musik läuft leiser, Effekte pausieren.
- Reiterleiste oben mit `CHARAKTER`, `MAP` und `SKILLS` in dieser Reihenfolge. Beim Öffnen ist `CHARAKTER` gewählt. Reiterwechsel per Mausklick, `Links`/`Rechts` oder `A`/`D`.
- Die Charakterseite zeigt Leben (aktuell/maximal) sowie Stärke, Fähigkeitsstärke und Rüstung mit ihrer Wirkung: Waffenschaden und Fähigkeitsschaden in Prozent gegenüber dem Grundwert, Rüstung als Schadensverringerung in Prozent (`SCHADENSVERRINGERUNG 17 %` beim Startwert).
- `MAP` und `SKILLS` zeigen nur einen Platzhalterhinweis.
- Darstellung im Stil des Pausenmenüs: angehaltenes Spielbild unter dem dunklen Schleier, Letterbox, Zierlinie, Schrift, Farben und Auswahlmarkierung des Hauptmenüs.
- Pausenmenü und Charaktermenü schließen sich gegenseitig aus: `Escape` im Charaktermenü schließt es, statt das Pausenmenü zu öffnen; `Tab` im Pausenmenü bleibt wirkungslos.

## Capabilities

### New Capabilities

- `character-menu`: Charaktermenü per `Tab` mit Reiterleiste, Charakterseite und Platzhalterseiten für Map und Skills.

### Modified Capabilities

- `pause-menu`: `Escape` öffnet das Pausenmenü nur, wenn das Charaktermenü nicht geöffnet ist.

## Impact

Neu sind ein kleines Modell für das Charaktermenü (`Menu/CharacterMenu`, offen/zu, gewählter Reiter, Öffnungszeit) und die Darstellung in `CinematicPresentation`. Geändert werden `GameWorld` (Öffnen, Schließen, Anhalten, Zeichnen, Screenshot-Kontext), die Pausenlogik im `AudioDirector` wird mitbenutzt, und die README nennt die neue Taste. Keine neuen Assets, Abhängigkeiten oder Speicherdaten. Die Charakterwerte selbst bleiben unverändert (`player-attributes`).
