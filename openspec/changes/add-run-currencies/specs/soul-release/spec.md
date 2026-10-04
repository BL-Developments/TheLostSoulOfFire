## MODIFIED Requirements

### Requirement: Nur erfolgreiche Freisetzung erzeugt Resonance
Das System SHALL Resonance erst nach erfolgreichem Soul Release erhöhen und MUST NOT für eine verschlungene Seele Resonance verbuchen. Soul Release MUST NOT Geld oder Glut gutschreiben; Glut entsteht ausschließlich beim Besiegen des Gegners.

#### Scenario: Soul Release wird abgeschlossen
- **WHEN** eine Seele erfolgreich die Welt verlässt
- **THEN** bewegt sich Soul Residue zum Spieler-Core und erhöht dessen Resonance, ohne einen Währungsbestand zu verändern

#### Scenario: Seele wird verschlungen
- **WHEN** ein Devourer den Verschlingvorgang abschließt
- **THEN** erhält der Spieler für diese Seele keine Resonance, bis sie wieder freigesetzt und anschließend erfolgreich released wurde
