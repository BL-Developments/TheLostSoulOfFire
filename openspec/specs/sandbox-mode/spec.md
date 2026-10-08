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

### Requirement: Charakterwerte lassen sich im Dev-Menü setzen
Das System SHALL im Abschnitt `CHARAKTER` des Dev-Menüs die Werteinträge `LEBEN`, `STÄRKE`, `FÄHIGKEITSSTÄRKE` und `RÜSTUNG` in dieser Reihenfolge mit ihrem aktuellen Wert anzeigen. Stärke, Fähigkeitsstärke und Rüstung SHALL sich in Schritten von 1, mit Umschalttaste von 10, im Bereich 0 bis 99 ändern lassen. `LEBEN` SHALL das maximale Leben in Schritten von 10, mit Umschalttaste von 100, im Bereich 1 bis 999 ändern und das aktuelle Leben auf das neue Maximum setzen. Jede Änderung SHALL sofort wirken; HUD und Charaktermenü SHALL die neuen Werte zeigen.

#### Scenario: Stärke erhöhen
- **WHEN** der Spieler im Dev-Menü `STÄRKE` wählt, mit gedrückter Umschalttaste `D` drückt und das Menü schließt
- **THEN** ist die Stärke 20, das Charaktermenü zeigt `WAFFENSCHADEN +50 %` und die Sense trifft entsprechend härter

#### Scenario: Leben erhöhen
- **WHEN** der Spieler im Dev-Menü bei `LEBEN` zweimal `D` drückt
- **THEN** zeigen Dev-Menü und HUD 120, und der Spieler hat 120 von 120 Leben

#### Scenario: Grenze
- **WHEN** die Rüstung 0 ist und der Spieler bei `RÜSTUNG` `A` drückt
- **THEN** bleibt die Rüstung 0

### Requirement: Zurücksetzen stellt die Startwerte wieder her
Das System SHALL mit der Aktion `ZURÜCKSETZEN` im Abschnitt `CHARAKTER` das maximale Leben auf 100 und den Spieler auf volles Leben setzen sowie Stärke, Fähigkeitsstärke und Rüstung auf die Werte beim Sandbox-Start zurücksetzen (die Werte aus `--strength`, `--ability-power` und `--armor` oder sonst 10). Ein Neustart in der Sandbox SHALL gesetzte Werte behalten. Beim Verlassen der Sandbox SHALL das System die Startwerte wiederherstellen.

#### Scenario: Zurücksetzen nach Änderungen
- **WHEN** die Sandbox mit `--armor 0` gestartet wurde, der Spieler Leben und Rüstung ändert und `ZURÜCKSETZEN` ausführt
- **THEN** hat der Spieler 100 von 100 Leben und Rüstung 0

#### Scenario: Neustart behält Werte
- **WHEN** der Spieler Stärke 30 setzt, besiegt wird und `R` drückt
- **THEN** ist die Stärke weiterhin 30

#### Scenario: Sandbox verlassen
- **WHEN** der Spieler Leben 500 setzt und über das Pausenmenü zum Hauptmenü zurückkehrt und ein neues Spiel beginnt
- **THEN** hat der Spieler wieder 100 Leben

### Requirement: Alle Gegnertypen lassen sich im Dev-Menü spawnen
Das System SHALL im Abschnitt `GEGNER` des Dev-Menüs für jeden spawnbaren Gegnertyp eine Aktion anzeigen, mindestens `HOLLOW`, `BURNING` und `DEVOURER`, jeweils mit der Zahl der lebenden Gegner dieses Typs. Die Aktion SHALL einen Gegner des Typs 260 Pixel vom Spieler entfernt, nie näher als 160 Pixel und vollständig innerhalb der Kampfgrenzen erscheinen lassen; aufeinanderfolgende Spawns SHALL sich um den Spieler verteilen. Das Dev-Menü SHALL dabei geöffnet bleiben. Die Einträge SHALL aus einer zentralen Liste der spawnbaren Typen entstehen.

#### Scenario: Mehrere Gegner spawnen
- **WHEN** der Spieler im Dev-Menü zweimal `HOLLOW` und einmal `DEVOURER` auslöst und das Menü schließt
- **THEN** stehen zwei Hollows und ein Devourer verteilt um den Spieler, die Zeilen zeigen 2 und 1, und die Gegner greifen nach dem Schließen an

#### Scenario: Spieler an der Wand
- **WHEN** der Spieler am Rand der Arena steht und `BURNING` auslöst
- **THEN** erscheint der Burning innerhalb der Arena und mindestens 160 Pixel vom Spieler entfernt

### Requirement: Alle Gegner lassen sich entfernen
Das System SHALL im Abschnitt `GEGNER` unter den Spawn-Aktionen die Aktion `ALLE GEGNER ENTFERNEN` mit der Zahl aller lebenden Gegner anzeigen. Sie SHALL alle Gegner und verlorenen Seelen vom Feld nehmen, ohne Seelen freizusetzen oder Todesanimationen abzuspielen.

#### Scenario: Feld räumen
- **WHEN** vier Gegner auf dem Feld stehen und der Spieler `ALLE GEGNER ENTFERNEN` auslöst
- **THEN** zeigt die Zeile 0 und nach dem Schließen ist das Feld leer

### Requirement: Die Trainingspuppe lässt sich im Dev-Menü spawnen
Das System SHALL im Abschnitt `GEGNER` des Dev-Menüs die Aktion `TRAININGSPUPPE` nach den übrigen Gegnertypen und vor `ALLE GEGNER ENTFERNEN` anzeigen. Sie SHALL eine Trainingspuppe nach denselben Regeln wie andere Gegner in Sichtweite des Spielers setzen; `ALLE GEGNER ENTFERNEN` SHALL auch Trainingspuppen entfernen.

#### Scenario: Puppe spawnen und entfernen
- **WHEN** der Spieler im Dev-Menü `TRAININGSPUPPE` auslöst und später `ALLE GEGNER ENTFERNEN`
- **THEN** steht zuerst eine Puppe in Sichtweite und die Zeile zeigt 1, danach ist sie verschwunden

### Requirement: Fähigkeiten lassen sich in der Sandbox jederzeit auswählen
Das System SHALL in der Sandbox die Fähigkeitsauswahl mit `C` jederzeit öffnen, solange der Spieler lebt und weder Pause-, Charakter- noch Dev-Menü offen ist. Während der Auswahl SHALL die Welt eingefroren sein. Solange die Auswahl offen ist, SHALL `F` das Dev-Menü nicht öffnen. Ein Neustart in der Sandbox SHALL laufende Fähigkeitseffekte und Abklingzeiten abräumen und die ausgerüsteten Fähigkeiten behalten.

#### Scenario: Auswahl mitten im Kampf
- **WHEN** der Spieler in der Sandbox neben einem Gegner `C` drückt
- **THEN** öffnet sich die Fähigkeitsauswahl, der Gegner bewegt sich nicht, und nach der Wahl mit `2` zeigt Slot 1 `DURCHSCHLAG`

#### Scenario: Neustart behält Fähigkeiten
- **WHEN** der Spieler `SOG` ausgerüstet und gewirkt hat und `R` drückt
- **THEN** ist `SOG` weiterhin ausgerüstet, das Sogfeld ist verschwunden und die Fähigkeit ist sofort wieder bereit

### Requirement: Fähigkeiten kosten in der Sandbox keine Glut
Das System SHALL in der Sandbox Fähigkeiten mit `Z`/`X` ohne Glutkosten wirken. Abklingzeiten und alle übrigen Ablehnungsgründe SHALL wie in der Arena gelten. Die Fähigkeitsleisten SHALL in der Sandbox `FREI` statt der Kosten zeigen und nie `GLUT FEHLT`. `ZWEITER ATEM` SHALL bis zum aktuellen Maximalleben des Spielers heilen.

#### Scenario: Wirken ohne Glut
- **WHEN** der Spieler in der Sandbox `DURCHSCHLAG` mit `X` auf eine Trainingspuppe wirkt
- **THEN** trifft das Geschoss die Puppe, die Leiste zeigt die Abklingzeit, und es wird keine Glut abgebucht oder angezeigt

#### Scenario: Abklingzeit gilt weiter
- **WHEN** der Spieler `SOG` wirkt und sofort erneut die Taste drückt
- **THEN** wird das Wirken mit `NOCH NICHT BEREIT` abgelehnt

#### Scenario: Heilen über 100
- **WHEN** der Spieler im Dev-Menü Leben 200 setzt, auf 120 Leben fällt und `ZWEITER ATEM` wirkt
- **THEN** hat er 145 von 200 Leben

