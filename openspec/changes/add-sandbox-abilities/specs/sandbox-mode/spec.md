## ADDED Requirements

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
