## MODIFIED Requirements

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
