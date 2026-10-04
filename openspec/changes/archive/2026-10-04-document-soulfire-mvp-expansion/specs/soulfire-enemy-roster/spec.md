## ADDED Requirements

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
