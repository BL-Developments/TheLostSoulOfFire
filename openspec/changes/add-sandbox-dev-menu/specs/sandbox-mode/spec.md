## ADDED Requirements

### Requirement: F öffnet und schließt das Dev-Menü der Sandbox
Das System SHALL in der Sandbox bei `F` das Dev-Menü öffnen, sofern weder Pausenmenü noch Charaktermenü noch Dev-Menü geöffnet sind. Bei geöffnetem Dev-Menü SHALL `F` oder `Escape` es schließen und die Sandbox an derselben Stelle fortsetzen; dabei SHALL weder das Pausenmenü noch das Charaktermenü erscheinen, und `Tab` SHALL bei geöffnetem Dev-Menü wirkungslos bleiben. Außerhalb der Sandbox SHALL `F` nichts bewirken.

#### Scenario: Öffnen in der Sandbox
- **WHEN** der Spieler in der Sandbox `F` drückt
- **THEN** erscheint das Dev-Menü

#### Scenario: Schließen mit Escape
- **WHEN** der Spieler bei geöffnetem Dev-Menü `Escape` drückt
- **THEN** schließt sich das Dev-Menü und das Pausenmenü erscheint nicht

#### Scenario: Andere Menüs offen
- **WHEN** der Spieler bei geöffnetem Pausen- oder Charaktermenü `F` drückt
- **THEN** öffnet sich kein Dev-Menü

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
