# sandbox-mode Specification

## Purpose
TBD - created by archiving change add-sandbox-start. Update Purpose after archive.
## Requirements
### Requirement: Die Sandbox ist nur über den Developer-Mode erreichbar
Das System SHALL die Sandbox ausschließlich beim Start mit `--dev --start sandbox` öffnen. Hauptmenü, Hub, Arena und alle übrigen Abläufe SHALL keinen Zugang zur Sandbox bieten.

#### Scenario: Start der Sandbox
- **WHEN** das Spiel mit `--dev --start sandbox` gestartet wird
- **THEN** erscheint der Spieler steuerbar in der Mitte der Sandbox, ohne Titelmenü, Prolog, Hub und Arena-Intro

#### Scenario: Normaler Ablauf
- **WHEN** der Spieler ohne `--dev` Prolog, Hub und Arena durchspielt
- **THEN** begegnet ihm an keiner Stelle ein Zugang zur Sandbox

### Requirement: Die Sandbox sieht aus wie die Arena, hat aber keinen Arena-Ablauf
Das System SHALL in der Sandbox die Arena-Kulisse mit Boden, Atmosphäre, Licht und Kampfkamera darstellen und Kampf wie in der Arena erlauben. Es SHALL keine Wellen, keinen Nachschub, keine Wellenpause, keinen Wellenstart per `E`, keine Truhen und keinen Abschluss geben. Oben im Bild SHALL `SANDBOX` stehen.

#### Scenario: Keine Gegner von selbst
- **WHEN** der Spieler die Sandbox betritt und wartet
- **THEN** erscheinen keine Gegner und keine Wellenankündigung

#### Scenario: Gegner per Debug-Taste
- **WHEN** der Spieler in der Sandbox `F2` drückt
- **THEN** erscheint ein Hollow neben ihm und lässt sich wie in der Arena bekämpfen

#### Scenario: Feld geleert
- **WHEN** der Spieler alle Gegner in der Sandbox besiegt
- **THEN** startet keine Wellenpause, keine Truhe erscheint und die Sandbox läuft weiter

### Requirement: In der Sandbox gibt es keine Währungen
Das System SHALL in der Sandbox keine Glut für besiegte Gegner gutschreiben, bei einer Niederlage keine Bestände verlieren und kein Währungs-HUD zeigen. Gesicherte Bestände SHALL unverändert bleiben.

#### Scenario: Gegner besiegt
- **WHEN** der Spieler in der Sandbox einen Gegner besiegt
- **THEN** erscheint kein Glutfunke und kein Bestand ändert sich

#### Scenario: Charaktermenü in der Sandbox
- **WHEN** der Spieler in der Sandbox `Tab` drückt
- **THEN** zeigt die Charakterseite bei Geld und Glut nur `GESICHERT`

### Requirement: Eine Niederlage setzt den Spieler in der Sandbox zurück
Das System SHALL nach einer Niederlage in der Sandbox mit `R` und jederzeit mit `F8` alle Gegner, Seelen und Geschosse entfernen und den Spieler mit vollem Leben in die Mitte der Sandbox setzen, ohne Intro und ohne die Sandbox zu verlassen. Gesetzte Charakterwerte SHALL erhalten bleiben.

#### Scenario: Neustart nach Niederlage
- **WHEN** der Spieler in der Sandbox besiegt wird und `R` drückt
- **THEN** steht er mit vollem Leben in der Mitte der Sandbox, das Feld ist leer und oben steht weiterhin `SANDBOX`

#### Scenario: Zum Hauptmenü
- **WHEN** der Spieler in der Sandbox im Pausenmenü `Zum Hauptmenü` wählt und danach ein neues Spiel beginnt
- **THEN** läuft das Spiel im normalen Ablauf ohne Sandbox-Verhalten

### Requirement: F öffnet und schließt das Dev-Menü der Sandbox
Das System SHALL in der Sandbox bei `F` das Dev-Menü öffnen, sofern weder Pausenmenü noch Charaktermenü noch Dev-Menü geöffnet sind und der Spieler nicht besiegt ist. Bei geöffnetem Dev-Menü SHALL `F` oder `Escape` es schließen und die Sandbox an derselben Stelle fortsetzen; dabei SHALL weder das Pausenmenü noch das Charaktermenü erscheinen, und `Tab` SHALL bei geöffnetem Dev-Menü wirkungslos bleiben. Außerhalb der Sandbox SHALL `F` nichts bewirken.

#### Scenario: Öffnen in der Sandbox
- **WHEN** der Spieler in der Sandbox `F` drückt
- **THEN** erscheint das Dev-Menü

#### Scenario: Schließen mit Escape
- **WHEN** der Spieler bei geöffnetem Dev-Menü `Escape` drückt
- **THEN** schließt sich das Dev-Menü und das Pausenmenü erscheint nicht

#### Scenario: Andere Menüs offen
- **WHEN** der Spieler bei geöffnetem Pausen- oder Charaktermenü `F` drückt
- **THEN** öffnet sich kein Dev-Menü

#### Scenario: Nach einer Niederlage
- **WHEN** der Spieler in der Sandbox besiegt ist und `F` drückt
- **THEN** öffnet sich kein Dev-Menü, und `R` setzt ihn wie gewohnt zurück

#### Scenario: Außerhalb der Sandbox
- **WHEN** der Spieler in der regulären Arena, im Hub oder im Prolog `F` drückt
- **THEN** passiert nichts

### Requirement: Das Dev-Menü hält die Sandbox an
Das System SHALL die Sandbox anhalten, solange das Dev-Menü geöffnet ist, mit denselben Regeln wie beim Pausenmenü: Kein Spielzustand schreitet fort, Spiel- und Entwicklereingaben bleiben wirkungslos, Musik läuft leiser weiter und laufende Effekte werden angehalten. Beim Schließen SHALL alles wie vorher weiterlaufen.

#### Scenario: Gegner stehen still
- **WHEN** das Dev-Menü geöffnet ist, während Gegner in der Sandbox angreifen
- **THEN** bewegen sich Gegner, Geschosse und Spieler nicht weiter und der Spieler erleidet keinen Schaden

### Requirement: Das Dev-Menü zeigt Abschnitte für Charakter und Gegner
Das System SHALL das Dev-Menü als Tafel über der angehaltenen, sichtbar bleibenden Sandbox darstellen, mit der Überschrift `DEV-MENÜ`, den Abschnitten `CHARAKTER` und `GEGNER` in dieser Reihenfolge und einer Tastenhilfe. Ein Abschnitt ohne Einträge SHALL `NOCH KEINE EINTRÄGE` zeigen. Das Sandbox-HUD SHALL `SANDBOX · F DEV-MENÜ` lauten.

#### Scenario: Menü erscheint
- **WHEN** das Dev-Menü geöffnet wird
- **THEN** stehen links `DEV-MENÜ`, darunter `CHARAKTER` und `GEGNER`, und das Feld mit Spieler und Gegnern bleibt daneben sichtbar

### Requirement: Einträge werden per Tastatur und Maus bedient
Das System SHALL mit `W`/`S` oder Pfeil hoch/runter den vorherigen beziehungsweise nächsten Eintrag über beide Abschnitte hinweg wählen, ohne am Rand umzubrechen. Bei einem Werteintrag SHALL `A`/`D` oder Pfeil links/rechts den Wert verringern beziehungsweise erhöhen, bei gedrückter Umschalttaste in großen Schritten. Bei einem Aktionseintrag SHALL `Enter` die Aktion ausführen. Überfahren mit der Maus SHALL einen Eintrag wählen; ein Klick SHALL eine Aktion ausführen oder einen Wert ändern (linke Hälfte verringert, rechte erhöht). Beim erneuten Öffnen SHALL der zuletzt gewählte Eintrag gewählt sein.

#### Scenario: Auswahl am Ende
- **WHEN** der letzte Eintrag gewählt ist und der Spieler `S` drückt
- **THEN** bleibt der letzte Eintrag gewählt

#### Scenario: Erneutes Öffnen
- **WHEN** der Spieler einen Eintrag wählt, das Menü schließt und wieder öffnet
- **THEN** ist derselbe Eintrag gewählt

