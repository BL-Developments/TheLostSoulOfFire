## MODIFIED Requirements

### Requirement: Kisten liefern Geld
Das System SHALL nach dem Leeren der Wellen 3, 6 und 9 je eine Kiste an einer für diese Welle eigenen Stelle in der Arena erscheinen lassen; nach allen anderen Wellen SHALL keine Kiste erscheinen. In ihrer Interaktionszone SHALL das System eine Aufforderung mit `E` zeigen. Das Öffnen mit `E` SHALL den festgelegten Geldbetrag genau einmal dem Run-Bestand Geld gutschreiben und die Kiste danach aus der Arena entfernen. Eine ungeöffnete Kiste SHALL mit dem Ende des Runs verfallen.

#### Scenario: Kistenwelle wird geleert
- **WHEN** die Wellen 3, 6 oder 9 geleert werden
- **THEN** erscheint eine geschlossene Kiste in der Arena

#### Scenario: Andere Welle wird geleert
- **WHEN** eine der Wellen 1, 2, 4, 5, 7 oder 8 geleert wird
- **THEN** erscheint keine Kiste, und die Pause bis zum nächsten Wellenstart beginnt

#### Scenario: Ältere Kiste ist noch zu
- **WHEN** die sechste Welle geleert wird und die Kiste nach der dritten Welle noch ungeöffnet ist
- **THEN** stehen beide Kisten an getrennten Stellen in der Arena und lassen sich einzeln öffnen

#### Scenario: Letzte Welle wird geleert
- **WHEN** die zehnte Welle geleert wird
- **THEN** erscheint keine weitere Kiste, und der Abschluss beginnt

#### Scenario: Spieler öffnet eine Kiste
- **WHEN** der Spieler in der Zone einer geschlossenen Kiste `E` drückt
- **THEN** steigt der Run-Bestand Geld um den Kistenbetrag, und die Kiste verschwindet nach einer kurzen Öffnungsdarstellung

#### Scenario: Spieler drückt nach dem Öffnen erneut E
- **WHEN** der Spieler an der Stelle einer bereits geöffneten Kiste `E` drückt
- **THEN** bleibt der Run-Bestand Geld unverändert

### Requirement: Der Arena-Abschluss sichert vorläufig den ganzen Run-Bestand
Das System SHALL beim Erreichen des Arena-Abschlusses beide Run-Bestände vollständig zu den gesicherten Beständen addieren, die Run-Bestände danach auf null setzen und das Profil speichern. Diese Regel gilt, bis Teilsicherung und Quote festgelegt sind.

#### Scenario: Arena wird abgeschlossen
- **WHEN** die zehnte Welle geleert ist und der Abschlusszustand beginnt
- **THEN** steigen die gesicherten Bestände um die bisherigen Run-Bestände, die Run-Bestände sind null, und der Abschlusszustand nennt die gesicherten Beträge
