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

