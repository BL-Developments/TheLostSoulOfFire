# soulfire-enemy-roster Specification

## Purpose
Der Gegnerbestand umfasst Hollow, Burning und Devourer mit jeweils eigenem, lesbarem Angriffs- und Seelenverhalten.
## Requirements
### Requirement: Hollow ist ein lesbarer Nahkampfgegner
Das System SHALL Hollow langsam zum Spieler führen und SHALL seinen Swipe durch eine erkennbare Ausholphase vor Schaden ankündigen.

#### Scenario: Hollow erreicht Nahkampfreichweite
- **WHEN** ein Hollow den Spieler angreifen kann
- **THEN** zeigt er zuerst einen Telegraphen, führt danach genau einen Swipe aus und geht anschließend in Recovery

#### Scenario: Hollow-Core wird in Soul Sense getroffen
- **WHEN** ein Angriff bei aktivem Soul Sense den Brust-Core des Hollow trifft
- **THEN** erhält der Treffer einen Schwachpunktbonus und ein unterscheidbares Core-Feedback

### Requirement: Burning greift durch eine telegraphierte Charge an
Das System SHALL Burning als schnellen, instabilen Gegner mit Flare-Telegraph, gerichteter Charge und bestrafter Fehlattacke darstellen.

#### Scenario: Burning beginnt Charge
- **WHEN** ein Burning seine Angriffsdistanz erreicht
- **THEN** kündigt er die Richtung sichtbar und hörbar an, stürmt anschließend vor und geht nach Treffer oder Fehlschlag in Recovery

#### Scenario: Burning wird während Charge destabilisiert
- **WHEN** die Cannon einen aktiv chargenden Burning trifft
- **THEN** löst das System eine gegnerschädigende, für den Spieler ungefährliche Detonation aus und erhält die Seele für Soul Release

### Requirement: Devourer priorisiert exponierte Seelen
Das System SHALL Devourer zu einer erreichbaren exponierten Seele umleiten und SHALL andernfalls den Spieler mit einem telegraphierten Heavy Slam bedrohen.

#### Scenario: Exponierte Seele erscheint
- **WHEN** eine verschlingbare Seele in Reichweite eines lebenden Devourer erscheint
- **THEN** wechselt der Devourer sein sichtbares Ziel vom Spieler zur Seele und beginnt bei Annäherung den Verschlingvorgang

#### Scenario: Devour wird abgeschlossen
- **WHEN** der Spieler den Verschlingvorgang nicht rechtzeitig unterbricht
- **THEN** wird die Seele als konsumiert geführt, der Devourer heilt sich und sein Slam-Schaden steigt bis zur vorgesehenen Stack-Grenze

#### Scenario: Devourer stirbt mit gefangenen Seelen
- **WHEN** ein Devourer mit konsumierten Seelen besiegt wird
- **THEN** gibt seine zerbrechende Seelenmasse alle gefangenen Seelen wieder in einen freisetzbaren Zustand zurück

### Requirement: Die Trainingspuppe ist ein unbesiegbares Übungsziel
Das System SHALL eine Trainingspuppe als Gegnertyp bereitstellen, der sich nicht bewegt, nicht angreift und durch Rückstoß nicht verschoben wird. Treffer SHALL Trefferfeedback wie bei anderen Gegnern auslösen und das Leben der Puppe senken, jedoch nie unter 1; die Puppe SHALL nie besiegt werden, keine Glut geben und keine Seele freisetzen. Jeder Treffer SHALL seinen vollen Schaden als aufsteigende Zahl über der Puppe zeigen, Kerntreffer hervorgehoben, und über dem Lebensbalken SHALL die Summe seit dem letzten Auffüllen als `SUMME <n>` stehen. Nach 2,5 Sekunden ohne Treffer SHALL die Puppe ihr Leben auffüllen und die Summe zurücksetzen. Die Trainingspuppe SHALL nur in der Sandbox vorkommen, nicht in Arena-Wellen oder im Prolog.

#### Scenario: Sensenkombo auf die Puppe
- **WHEN** der Spieler mit Stärke 10 die Puppe mit allen drei Sensenschlägen trifft
- **THEN** steigen die Zahlen 20, 25 und 40 auf, `SUMME 85` erscheint und der Lebensbalken sinkt

#### Scenario: Puppe stirbt nicht
- **WHEN** die Puppe mehr Schaden erhält, als sie Leben hat
- **THEN** bleibt sie mit 1 Leben stehen, die Summe zählt den vollen Schaden und es erscheinen weder Glut noch Seele

#### Scenario: Auffüllen
- **WHEN** die Puppe 2,5 Sekunden lang nicht getroffen wird
- **THEN** ist ihr Lebensbalken wieder voll und `SUMME` verschwindet

