# player-profile Specification

## Purpose
TBD - created by archiving change add-run-currencies. Update Purpose after archive.
## Requirements
### Requirement: Das Profil speichert die gesicherten Bestände
Das System SHALL die gesicherten Bestände von Geld und Glut in einem versionierten Profil speichern und beim Spielstart laden. Run-Bestände MUST NOT gespeichert werden.

#### Scenario: Neustart nach Sicherung
- **WHEN** nach einem gesicherten Arena-Abschluss das Spiel beendet und neu gestartet wird
- **THEN** zeigt der Hub dieselben gesicherten Bestände wie vor dem Beenden

### Requirement: Das Profil wird atomar geschrieben
Das System SHALL das Profil zuerst in eine temporäre Datei schreiben und diese anschließend an die Stelle des bisherigen Profils setzen. Ein abgebrochener Schreibvorgang MUST NOT das bisherige Profil beschädigen.

#### Scenario: Schreibvorgang scheitert
- **WHEN** das Schreiben der temporären Datei fehlschlägt
- **THEN** bleibt das bisherige Profil unverändert lesbar

### Requirement: Fehlende oder ungültige Profile werden definiert behandelt
Das System SHALL bei fehlendem Profil mit leeren gesicherten Beständen starten. Bei unlesbarem Inhalt, unbekannter Version oder negativen Werten SHALL das System mit leeren gesicherten Beständen starten und die ungültige Datei vor dem nächsten Speichern als Sicherungskopie beiseitelegen.

#### Scenario: Kein Profil vorhanden
- **WHEN** das Spiel ohne Profildatei startet
- **THEN** sind beide gesicherten Bestände null

#### Scenario: Profil ist beschädigt
- **WHEN** die Profildatei kein gültiges Profil enthält
- **THEN** startet das Spiel mit leeren gesicherten Beständen, und die beschädigte Datei bleibt als Sicherungskopie erhalten

