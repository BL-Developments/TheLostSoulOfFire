## ADDED Requirements

### Requirement: Gegner hinterlassen eine exponierte Seele
Das System SHALL nach Zerstörung einer feindlichen Manifestation eine getrennte Seele erzeugen und SHALL Kampftod und Freisetzung als unterschiedliche Ereignisse behandeln.

#### Scenario: Manifestation wird besiegt
- **WHEN** die Gesundheit eines Gegners null erreicht und seine Todesdarstellung endet
- **THEN** verschwindet die Manifestation und eine exponierte Seele beginnt ihren Freisetzungsablauf

### Requirement: Soul Release erfolgt automatisch und friedlich
Das System SHALL eine ungestörte exponierte Seele nach einem zeitlich lesbaren Freisetzungsablauf aus der Welt entfernen.

#### Scenario: Seele wird nicht gestört
- **WHEN** eine exponierte Seele für die erforderliche Dauer nicht verschlungen wird
- **THEN** verbindet sich Death Flame mit ihr, sie hellt bis nahe Weiß auf, verlässt die Welt und hinterlässt Soul Residue

### Requirement: Nur erfolgreiche Freisetzung erzeugt Resonance
Das System SHALL Resonance erst nach erfolgreichem Soul Release erhöhen und MUST NOT eine verschlungene Seele als Spielerressource verbuchen.

#### Scenario: Soul Release wird abgeschlossen
- **WHEN** eine Seele erfolgreich die Welt verlässt
- **THEN** bewegt sich Soul Residue zum Spieler-Core und erhöht dessen Resonance

#### Scenario: Seele wird verschlungen
- **WHEN** ein Devourer den Verschlingvorgang abschließt
- **THEN** erhält der Spieler für diese Seele keine Resonance, bis sie wieder freigesetzt und anschließend erfolgreich released wurde
