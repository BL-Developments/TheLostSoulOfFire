## Purpose

Ein Biom-Level ist eine handgebaute, deterministische Folge verbundener Räume mit Wänden, Raumtypen und Raum-Begegnungen. Seine Grafik hängt an austauschbaren Visual-IDs und erscheint als Graubox, solange keine Grafik vorliegt.

## ADDED Requirements

### Requirement: Ein Level besteht aus verbundenen Räumen
Das System SHALL ein Level aus rechteckigen Räumen und Durchgängen zwischen ihnen aufbauen. Jeder Raum SHALL vom Eingangsraum aus über Durchgänge erreichbar sein. Aufbau und Raumfolge eines Levels SHALL bei jedem Betreten gleich sein.

#### Scenario: Level wird betreten
- **WHEN** der Spieler ein Level betritt
- **THEN** steht er im Eingangsraum, und Räume und Durchgänge liegen an denselben Stellen wie beim letzten Betreten

#### Scenario: Unerreichbarer Raum
- **WHEN** eine Leveldefinition einen Raum ohne Durchgang zum Eingangsraum enthält
- **THEN** schlägt die Prüfung der Leveldefinitionen im Testlauf fehl und nennt Level und Raum

### Requirement: Außerhalb von Räumen und Durchgängen liegt Wand
Das System SHALL Spieler und Gegner auf Räume und Durchgänge beschränken. Bewegung und Dash MUST NOT einen Körper in die Wand führen; der Körper SHALL an der Wand entlanggleiten.

#### Scenario: Spieler läuft gegen eine Wand
- **WHEN** der Spieler schräg gegen eine Raumwand läuft
- **THEN** bleibt er im Raum und gleitet entlang der Wand weiter

#### Scenario: Dash in die Wand
- **WHEN** der Spieler einen Dash in Richtung Wand ausführt
- **THEN** endet der Dash an der Wand innerhalb des Raums

### Requirement: Jeder Raum hat einen Raumtyp
Das System SHALL jedem Raum genau einen Raumtyp aus der Raum-Grammatik zuordnen: Eingang, Kampf, Großer Kampf, Durchgangsraum, Belohnung, Elite, Levelende oder Wächter. Jedes Level SHALL genau einen Eingang und genau einen Raum vom Typ Levelende oder Wächter haben.

#### Scenario: Level ohne Ende
- **WHEN** eine Leveldefinition keinen Raum vom Typ Levelende oder Wächter enthält
- **THEN** schlägt die Prüfung der Leveldefinitionen im Testlauf fehl

### Requirement: Kampfräume starten ihre Begegnung beim Betreten
Das System SHALL die Begegnung eines Kampf-, Großen Kampf-, Elite- oder Wächterraums starten, sobald der Spieler den Raum vollständig betreten hat. Die Begegnung SHALL aus Schüben bestehen, die wie in der Arena nachrücken. Gegner SHALL innerhalb des Raums und mit Mindestabstand zum Spieler erscheinen.

#### Scenario: Spieler betritt einen Kampfraum
- **WHEN** der Spieler den Durchgang verlässt und ganz im Kampfraum steht
- **THEN** erscheint der erste Schub des Raums angekündigt an Stellen im Raum mit Mindestabstand zum Spieler

#### Scenario: Spieler steht noch im Durchgang
- **WHEN** der Spieler nur im Durchgang vor einem Kampfraum steht
- **THEN** startet keine Begegnung

### Requirement: Durchgänge schließen sich bis der Raum geräumt ist
Das System SHALL beim Start einer Begegnung alle Durchgänge des Raums sichtbar schließen und SHALL sie erst öffnen, wenn alle Schübe erschienen und alle Gegner des Raums besiegt sind. Ein geräumter Raum SHALL geräumt bleiben, bis das Level verlassen wird.

#### Scenario: Raum wird geräumt
- **WHEN** der letzte Gegner des letzten Schubs besiegt ist
- **THEN** öffnen sich die Durchgänge des Raums, und beim erneuten Betreten startet keine Begegnung

### Requirement: Gegner bleiben in ihrem Raum
Das System SHALL jeden Gegner einer Raum-Begegnung auf seinen Raum beschränken. Kein Gegner MUST einen Durchgang durchqueren.

#### Scenario: Gegner verfolgt den Spieler zum Durchgang
- **WHEN** ein Gegner den Spieler bis an einen geöffneten Durchgang verfolgt
- **THEN** bleibt der Gegner am Raumrand stehen

### Requirement: Die Kamera folgt dem Spieler im Level
Das System SHALL die Kamera dem Spieler folgen lassen und sie an den Grenzen des Levels halten, sodass außerhalb des Levels nichts Leeres zu sehen ist, solange das Level größer als der Bildausschnitt ist.

#### Scenario: Spieler wechselt den Raum
- **WHEN** der Spieler durch einen Durchgang in den nächsten Raum läuft
- **THEN** folgt die Kamera ihm ohne Sprung

### Requirement: Levelgrafik hängt an Visual-IDs
Das System SHALL für jedes Level eine Visual-ID für die gemalte Bodenebene und für jedes Biom eine Visual-ID für das Grading führen. Jede dieser Visual-IDs SHALL eine Visual-Spec haben. Liefert die Registry Grafik für eine dieser IDs, SHALL das System sie anstelle der Graubox zeichnen, ohne dass Code geändert wird.

#### Scenario: Gemalte Bodenebene wird nachgeliefert
- **WHEN** Registry-Eintrag und Bild für die Bodenebene eines Levels hinzukommen
- **THEN** zeichnet das Spiel beim nächsten Start dieses Bild statt der Graubox

#### Scenario: Grading fehlt
- **WHEN** für das Grading eines Bioms keine Grafik vorliegt
- **THEN** zeichnet das System die Szene mit neutralem Grading

### Requirement: Fehlende Levelgrafik erscheint als Graubox
Das System SHALL ein Level ohne Grafik als Graubox in der Platzhalterpalette seines Bioms zeichnen. Boden, Wand, Durchgänge und geschlossene Durchgänge SHALL klar unterscheidbar sein. Jeder Raumtyp SHALL eine eigene Markierung haben. Die Palette SHALL sich von Hub und Prolog deutlich unterscheiden.

#### Scenario: Graubox-Level wird betreten
- **WHEN** der Spieler ein Level ohne Grafik betritt
- **THEN** sind Wände, Durchgänge und der Raumtyp jedes Raums ohne Text erkennbar, und das Spiel meldet keine fehlende Grafik als Fehler
