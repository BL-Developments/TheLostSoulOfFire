## ADDED Requirements

### Requirement: Freie Top-down-Bewegung
Das System SHALL die Spielfigur in acht Richtungen durch gehaltene Bewegungsaktionen bewegen, SHALL diagonale Eingaben normalisieren und SHALL das Durchqueren blockierender Levelgeometrie verhindern.

#### Scenario: Diagonale Bewegung
- **WHEN** zwei senkrecht zueinander stehende Bewegungsrichtungen gleichzeitig gehalten werden
- **THEN** bewegt sich die Spielfigur diagonal mit derselben Gesamtgeschwindigkeit wie bei einer geraden Bewegung

#### Scenario: Bewegung gegen eine Wand
- **WHEN** die Spielfigur sich in eine blockierende Wand bewegt
- **THEN** bleibt ihre Hitbox außerhalb der Wand und eine Bewegung entlang der freien Achse bleibt möglich

### Requirement: Grundlegender Feuerangriff
Das System SHALL bei gehaltener Angriffsaktion nach Ablauf einer Abklingzeit ein Feuerprojektil in Richtung der aktuellen Mausposition in der Spielwelt abgeben.

#### Scenario: Feuerprojektil abgeben
- **WHEN** die Angriffsaktion gehalten wird und die Angriffsabklingzeit abgelaufen ist
- **THEN** startet ein Feuerprojektil an der Spielfigur und bewegt sich in die anvisierte Weltrichtung

#### Scenario: Angriff während Abklingzeit
- **WHEN** die Angriffsaktion gehalten wird und die Angriffsabklingzeit noch aktiv ist
- **THEN** wird bis zum Ende der Abklingzeit kein weiteres Projektil abgegeben

### Requirement: Feuerprojektile beschädigen Gegner
Das System SHALL einem Gegner bei der ersten Kollision mit einem Feuerprojektil Schaden zufügen und SHALL das Projektil danach deaktivieren. Ein Gegner SHALL entfernt werden, sobald seine Gesundheit null erreicht.

#### Scenario: Projektil trifft Gegner
- **WHEN** die Hitbox eines aktiven Feuerprojektils die Hitbox eines lebenden Gegners schneidet
- **THEN** verliert der Gegner Gesundheit und das Projektil kann keinen weiteren Treffer verursachen

#### Scenario: Gegner wird besiegt
- **WHEN** der zugefügte Schaden die Gesundheit eines Gegners auf null reduziert
- **THEN** nimmt der Gegner nicht mehr an Bewegung, Kollision oder Angriff teil

### Requirement: Gegner bedrohen die Spielfigur
Das System SHALL den grundlegenden Gegnertyp innerhalb der Kampfbegegnung zur Spielfigur bewegen und SHALL bei Kontakt Schaden mit einer begrenzten Trefferfrequenz verursachen.

#### Scenario: Gegner erreicht die Spielfigur
- **WHEN** sich die Hitbox eines lebenden Gegners mit der Spielerhitbox überschneidet und die Unverwundbarkeitszeit abgelaufen ist
- **THEN** verliert die Spielfigur Gesundheit und eine neue kurze Unverwundbarkeitszeit beginnt

#### Scenario: Kontakt während Unverwundbarkeit
- **WHEN** ein Gegner die Spielfigur während ihrer aktiven Unverwundbarkeitszeit berührt
- **THEN** verursacht dieser Kontakt keinen weiteren Gesundheitsschaden

### Requirement: Gesundheit und Tod sind sichtbar
Das System SHALL die aktuelle Spielergesundheit im HUD darstellen und SHALL nach Erreichen von null Gesundheit keine weitere Kampfsteuerung verarbeiten.

#### Scenario: Spieler nimmt Schaden
- **WHEN** die Spielfigur Gesundheit verliert
- **THEN** zeigt das HUD den aktualisierten Wert und die Spielfigur erhält eine sichtbare Trefferreaktion

#### Scenario: Gesundheit erreicht null
- **WHEN** die Spielergesundheit auf null sinkt
- **THEN** kann die Spielfigur sich nicht mehr bewegen oder angreifen und der Niederlagenablauf beginnt
