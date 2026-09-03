## ADDED Requirements

### Requirement: Soul Cannon lädt über die sekundäre Angriffseingabe
Das System SHALL die Soul Cannon beim Halten der rechten Maustaste bis zur Vollaufladung laden und SHALL beim Loslassen ein Projektil in Zielrichtung abfeuern.

#### Scenario: Teilgeladener Schuss
- **WHEN** der Spieler die rechte Maustaste vor Vollaufladung loslässt
- **THEN** erzeugt das System einen Schuss, dessen Schaden, Größe und Wirkung dem erreichten Ladefortschritt entsprechen

#### Scenario: Vollgeladener Schuss
- **WHEN** der Spieler nach erreichter Vollaufladung abfeuert
- **THEN** erzeugt das System einen maximal verstärkten Schuss mit eindeutigem Vollaufladungs- und Abschussfeedback

### Requirement: Ladestufen sind ohne HUD lesbar
Das System SHALL den Ladefortschritt in drei unterscheidbaren audiovisuellen Stufen sowie durch einen eindeutigen Vollaufladungs-Cue darstellen.

#### Scenario: Ladung überschreitet eine Stufenschwelle
- **WHEN** die Cannon während des Haltens eine neue Ladestufe erreicht
- **THEN** nehmen Partikelkonvergenz, Leuchten, Vibration oder Audioton wahrnehmbar zu

### Requirement: Cannon besitzt gegnerspezifische Volltrefferfolgen
Das System SHALL vollgeladene Treffer zur Unterbrechung besonderer Gegneraktionen verwenden.

#### Scenario: Charging Burning wird getroffen
- **WHEN** ein Cannon-Schuss einen Burning während dessen Charge trifft
- **THEN** detoniert der Burning und fügt Gegnern im Wirkungsbereich Schaden zu, ohne den Spieler zu beschädigen

#### Scenario: Devourer wird vollgeladen getroffen
- **WHEN** ein vollgeladener Cannon-Schuss einen Devourer trifft
- **THEN** wird der Devourer stark gestaggert und eine gegebenenfalls verschlungene Seele wird wieder freigesetzt
