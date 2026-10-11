## MODIFIED Requirements

### Requirement: Die Charakterseite zeigt die Eigenschaften des Charakters
Das System SHALL auf der Seite `CHARAKTER` die aktuellen Werte des Spielers in zwei Spalten anzeigen. Links SHALL `LEBEN` als aktuelles und maximales Leben stehen, darunter `STÄRKE`, `FÄHIGKEITSSTÄRKE`, `TEMPO`, `KERNSCHÄRFE` und `FOKUS`; rechts `RÜSTUNG`, `STANDFESTIGKEIT`, `GEWANDTHEIT`, `EINKLANG` und `GLÜCK`, jeweils als ganze Zahl. Unter jedem Charakterwert SHALL eine Wirkungszeile stehen: `WAFFENSCHADEN`, `FÄHIGKEITSSCHADEN`, `ANGRIFFSTEMPO`, `KERNSCHADEN`, `ABKLINGTEMPO`, `RÜCKSTOSS`, `AUSWEICHEN … / LAUFEN …`, `RESONANZAUFBAU` und `BEUTE` als Abweichung vom Grundwert in ganzen Prozent mit Vorzeichen, `SCHADENSVERRINGERUNG` als Anteil, den Rüstung von erlittenem Schaden abzieht, in ganzen Prozent ohne Vorzeichen. Die Prozentwerte SHALL aus denselben Regeln berechnet werden, mit denen `player-attributes` die Wirkung bestimmt.

#### Scenario: Startwerte
- **WHEN** ein neuer Lauf beginnt und der Spieler das Charaktermenü öffnet
- **THEN** zeigt die Seite `LEBEN 100 / 100`, `STÄRKE 10` mit `WAFFENSCHADEN +0 %`, `FÄHIGKEITSSTÄRKE 10` mit `FÄHIGKEITSSCHADEN +0 %`, `RÜSTUNG 10` mit `SCHADENSVERRINGERUNG 17 %` und die übrigen Werte mit 10 und `+0 %`

#### Scenario: Gesetzte Werte im Developer-Mode
- **WHEN** das Spiel mit `--dev --start arena --strength 20 --ability-power 6 --armor 0` gestartet wird und der Spieler das Charaktermenü öffnet
- **THEN** zeigt die Seite `STÄRKE 20` mit `WAFFENSCHADEN +50 %`, `FÄHIGKEITSSTÄRKE 6` mit `FÄHIGKEITSSCHADEN -20 %` und `RÜSTUNG 0` mit `SCHADENSVERRINGERUNG 0 %`

#### Scenario: Neue Werte im Developer-Mode
- **WHEN** das Spiel mit `--dev --start arena --attack-speed 20 --steadiness 20 --agility 20 --luck 0` gestartet wird und der Spieler das Charaktermenü öffnet
- **THEN** zeigt die Seite `ANGRIFFSTEMPO +50 %`, `RÜCKSTOSS -33 %`, `AUSWEICHEN +50 % / LAUFEN +10 %` und `BEUTE -50 %`

#### Scenario: Leben nach einem Treffer
- **WHEN** der Spieler mit Rüstung 10 einen Hollow-Hieb erlitten hat und das Charaktermenü öffnet
- **THEN** zeigt die Seite `LEBEN 87 / 100`
