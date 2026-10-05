# pause-menu Specification

## Purpose
Das Pausenmenü hält das laufende Spiel per Escape an und bietet Fortsetzen, Einstellungen und das Beenden in Hauptmenü oder Desktop, im Stil des Hauptmenüs.
## Requirements
### Requirement: Escape pausiert das laufende Spiel
Das System SHALL in jeder Spielphase außer der Titelphase bei `Escape` das Pausenmenü öffnen und das Spiel anhalten, sofern das Charaktermenü nicht geöffnet ist; bei geöffnetem Charaktermenü schließt `Escape` stattdessen das Charaktermenü. Solange das Pausenmenü geöffnet ist, SHALL kein Spielzustand fortschreiten: Spieler, Gegner, Geschosse, Partikel, Kamera, Bildschirmeffekte, Inszenierungen, Übergänge und Phasentimer bleiben stehen, und Spiel- und Entwicklereingaben bleiben wirkungslos.

#### Scenario: Pause wird im Kampf geöffnet
- **WHEN** der Spieler während eines Kampfes `Escape` drückt
- **THEN** erscheint das Pausenmenü und Gegner, Geschosse und Spieler bewegen sich nicht weiter

#### Scenario: Pause wird während einer Inszenierung geöffnet
- **WHEN** der Spieler während Prolog, Hub, Türübergang, Todes- oder Abschlussbildschirm `Escape` drückt
- **THEN** erscheint das Pausenmenü und die Inszenierung setzt erst nach dem Fortsetzen an derselben Stelle fort

#### Scenario: Spieleingaben während der Pause
- **WHEN** der Spieler bei geöffnetem Pausenmenü Angriffs-, Bewegungs- oder Entwicklertasten betätigt
- **THEN** verändert sich der Spielzustand nicht

#### Scenario: Escape wird lange gehalten
- **WHEN** der Spieler `Escape` im Spiel drückt und gedrückt hält
- **THEN** öffnet sich das Pausenmenü genau einmal und die Anwendung wird nicht beendet

#### Scenario: Escape bei geöffnetem Charaktermenü
- **WHEN** der Spieler bei geöffnetem Charaktermenü `Escape` drückt
- **THEN** schließt sich das Charaktermenü und das Pausenmenü erscheint nicht

### Requirement: Pausenmenü bietet vier Einträge in fester Reihenfolge
Das System SHALL im Pausenmenü die Einträge `FORTSETZEN`, `EINSTELLUNGEN`, `ERRUNGENSCHAFTEN UND STATISTIKEN` und `BEENDEN` in dieser Reihenfolge darstellen und beim Öffnen `FORTSETZEN` auswählen. Die Bedienung per Maus und Tastatur SHALL der des Hauptmenüs entsprechen.

#### Scenario: Pausenmenü wird geöffnet
- **WHEN** das Pausenmenü erscheint
- **THEN** zeigt es die vier Einträge in fester Reihenfolge und hebt `FORTSETZEN` hervor

#### Scenario: Errungenschaften und Statistiken wird ausgelöst
- **WHEN** der Spieler `ERRUNGENSCHAFTEN UND STATISTIKEN` im Pausenmenü auslöst
- **THEN** bleibt die Pausenseite unverändert sichtbar und das Spiel bleibt pausiert

### Requirement: Fortsetzen beendet die Pause
Das System SHALL bei `FORTSETZEN` sowie bei `Escape` auf der Pausenseite das Pausenmenü schließen und das Spiel an derselben Stelle fortsetzen. Die Eingabe, die das Fortsetzen auslöst, SHALL keine Spielaktion auslösen.

#### Scenario: Fortsetzen per Klick
- **WHEN** der Spieler `FORTSETZEN` anklickt
- **THEN** läuft das Spiel an derselben Stelle weiter und der Klick löst keinen Angriff aus

#### Scenario: Fortsetzen per Escape
- **WHEN** der Spieler auf der Pausenseite `Escape` drückt
- **THEN** schließt sich das Pausenmenü und das Spiel läuft weiter

### Requirement: Einstellungen sind im Pausenmenü erreichbar
Das System SHALL über `EINSTELLUNGEN` im Pausenmenü dieselben Einstellungsseiten und Werte wie im Hauptmenü anbieten. Änderungen SHALL sofort wirken und gespeichert werden. `ZURÜCK` und `Escape` SHALL eine Ebene zurückführen, von der Einstellungsübersicht zur Pausenseite.

#### Scenario: Lautstärke wird in der Pause geändert
- **WHEN** der Spieler im Pausenmenü unter `AUDIO` einen Wert ändert
- **THEN** wirkt die neue Lautstärke sofort und ist nach einem Neustart erhalten

#### Scenario: Rückweg aus den Einstellungen
- **WHEN** der Spieler in der Einstellungsübersicht des Pausenmenüs `ZURÜCK` auslöst oder `Escape` drückt
- **THEN** zeigt das System wieder die Pausenseite und das Spiel bleibt pausiert

### Requirement: Beenden bietet Hauptmenü oder Desktop
Das System SHALL bei `BEENDEN` im Pausenmenü eine Seite mit `ZURÜCK ZUM HAUPTMENÜ`, `ZURÜCK ZUM DESKTOP` und `ZURÜCK` darstellen. Eine weitere Bestätigung oder ein Hinweis auf Fortschrittsverlust SHALL NOT erscheinen.

#### Scenario: Zurück zum Hauptmenü
- **WHEN** der Spieler `ZURÜCK ZUM HAUPTMENÜ` auslöst
- **THEN** verwirft das System den laufenden Durchlauf und zeigt die Titeldarstellung mit bereits geöffnetem Hauptmenü

#### Scenario: Neues Spiel nach Rückkehr ins Hauptmenü
- **WHEN** der Spieler nach `ZURÜCK ZUM HAUPTMENÜ` ein neues Spiel startet
- **THEN** beginnt ein frischer Durchlauf mit dem Prolog

#### Scenario: Zurück zum Desktop
- **WHEN** der Spieler `ZURÜCK ZUM DESKTOP` auslöst
- **THEN** beendet das System die Anwendung ohne weitere Abfrage

#### Scenario: Beenden wird verworfen
- **WHEN** der Spieler auf der Beenden-Seite `ZURÜCK` auslöst oder `Escape` drückt
- **THEN** zeigt das System wieder die Pausenseite

### Requirement: Pausenmenü liegt im Stil des Hauptmenüs über dem angehaltenen Spiel
Das System SHALL das angehaltene Spielbild unter einem halbtransparenten dunklen Schleier sichtbar lassen und darüber Letterbox, Zierlinie, die Überschrift `PAUSIERT` und die Menüeinträge in Schrift, Farben und Auswahlmarkierung des Hauptmenüs darstellen.

#### Scenario: Pausenbild wird dargestellt
- **WHEN** das Pausenmenü geöffnet ist
- **THEN** ist das angehaltene Spielbild gedämpft erkennbar und darüber stehen `PAUSIERT` und die Einträge im Stil des Hauptmenüs

### Requirement: Ton während der Pause
Das System SHALL während der Pause Musik und Ambience mit verringerter Lautstärke weiterspielen und laufende Soundeffekte anhalten. Beim Fortsetzen SHALL die vorherige Lautstärke wiederhergestellt und angehaltene Effekte fortgesetzt werden; bei Rückkehr ins Hauptmenü SHALL angehaltene Effekte verworfen werden.

#### Scenario: Pause wird geöffnet
- **WHEN** das Pausenmenü erscheint, während Musik und Effekte spielen
- **THEN** läuft die Musik leiser weiter und die Effekte verstummen

#### Scenario: Spiel wird fortgesetzt
- **WHEN** der Spieler das Spiel fortsetzt
- **THEN** spielt die Musik wieder in normaler Lautstärke und angehaltene Effekte laufen weiter

