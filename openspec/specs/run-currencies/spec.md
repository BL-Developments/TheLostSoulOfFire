# run-currencies Specification

## Purpose
TBD - created by archiving change add-run-currencies. Update Purpose after archive.
## Requirements
### Requirement: Zwei Währungen mit Run- und gesichertem Bestand
Das System SHALL die Währungen Geld und Glut führen. Jede Währung SHALL einen Run-Bestand und einen gesicherten Bestand besitzen, die getrennt verändert werden. Kein Bestand MUST jemals negativ werden.

#### Scenario: Beide Konten sind getrennt
- **WHEN** im Run Glut gutgeschrieben wird
- **THEN** steigt nur der Run-Bestand Glut, und der gesicherte Bestand Glut bleibt unverändert

#### Scenario: Ausgeben ohne ausreichende Mittel
- **WHEN** ein Betrag aus einem Run-Bestand abgebucht werden soll, der größer als dieser Bestand ist
- **THEN** lehnt das System die Abbuchung ab und lässt alle Bestände unverändert

#### Scenario: Ausgeben mit ausreichenden Mitteln
- **WHEN** ein positiver Betrag abgebucht wird, der den Run-Bestand nicht übersteigt
- **THEN** sinkt nur dieser Run-Bestand genau um den Betrag

### Requirement: Ein Run beginnt mit einem Glut-Basisvorrat
Das System SHALL beim Betreten der Arena durch Tür I, beim Developer-Start in die Arena und bei einem Neuversuch nach dem Tod einen neuen Run beginnen. Zu Run-Beginn SHALL der Run-Bestand Geld null und der Run-Bestand Glut gleich dem Basisvorrat sein. Gesicherte Glut MUST NOT in den Run-Bestand übernommen werden.

#### Scenario: Spieler betritt die Arena
- **WHEN** die Eintrittssequenz durch Tür I endet
- **THEN** zeigt der Run-Bestand Geld null und der Run-Bestand Glut den Basisvorrat, unabhängig vom gesicherten Bestand

### Requirement: Besiegte Gegner schreiben genau einmal Glut gut
Das System SHALL im Run beim Übergang eines Gegners von lebendig zu besiegt dessen typabhängige Glutmenge genau einmal dem Run-Bestand gutschreiben. Weitere Treffer, Detonationen oder Zustandswechsel desselben Gegners MUST NOT eine zweite Gutschrift auslösen. Im Prolog SHALL keine Glut gutgeschrieben werden.

#### Scenario: Gegner wird besiegt
- **WHEN** ein Hollow, Burning oder Devourer in der Arena besiegt wird
- **THEN** steigt der Run-Bestand Glut im selben Frame um die Menge seines Typs

#### Scenario: Besiegter Gegner wird erneut getroffen
- **WHEN** ein bereits besiegter Gegner weiteren Schaden erhält
- **THEN** bleibt der Run-Bestand Glut unverändert

#### Scenario: Seele des Gegners wird später verschlungen
- **WHEN** ein Devourer die Seele eines bereits besiegten Gegners verschlingt
- **THEN** bleibt die beim Besiegen gutgeschriebene Glut erhalten

### Requirement: Ein Glutfunke zeigt die Aufnahme
Das System SHALL bei jeder Glut-Gutschrift einen Glutfunken vom besiegten Gegner zum Spieler fliegen lassen. Der Funke SHALL sich farblich vom Soul Release unterscheiden. Die Gutschrift MUST NOT vom Ende des Flugs abhängen.

#### Scenario: Funke wird unterbrochen
- **WHEN** der Spieler stirbt, bevor ein Glutfunke ihn erreicht
- **THEN** war die Glut bereits gutgeschrieben und wird mit dem Run-Bestand verloren

### Requirement: Kisten liefern Geld
Das System SHALL nach dem Leeren jeder Welle außer der letzten eine Kiste in der Arena erscheinen lassen. In ihrer Interaktionszone SHALL das System eine Aufforderung mit `E` zeigen. Das Öffnen mit `E` SHALL den festgelegten Geldbetrag genau einmal dem Run-Bestand Geld gutschreiben und die Kiste danach aus der Arena entfernen. Eine ungeöffnete Kiste SHALL mit dem Ende des Runs verfallen.

#### Scenario: Welle wird geleert
- **WHEN** die Wellen 1, 2 oder 3 geleert werden
- **THEN** erscheint eine geschlossene Kiste in der Arena

#### Scenario: Letzte Welle wird geleert
- **WHEN** die vierte Welle geleert wird
- **THEN** erscheint keine weitere Kiste, und der Abschluss beginnt

#### Scenario: Spieler öffnet eine Kiste
- **WHEN** der Spieler in der Zone einer geschlossenen Kiste `E` drückt
- **THEN** steigt der Run-Bestand Geld um den Kistenbetrag, und die Kiste verschwindet nach einer kurzen Öffnungsdarstellung

#### Scenario: Spieler drückt nach dem Öffnen erneut E
- **WHEN** der Spieler an der Stelle einer bereits geöffneten Kiste `E` drückt
- **THEN** bleibt der Run-Bestand Geld unverändert

### Requirement: Niederlage leert beide Run-Bestände
Das System SHALL beim Tod des Spielers im Run beide Run-Bestände auf null setzen. Gesicherte Bestände MUST unverändert bleiben.

#### Scenario: Spieler stirbt mit Run-Bestand
- **WHEN** der Spieler mit Geld und Glut im Run-Bestand stirbt
- **THEN** sind beide Run-Bestände null und die gesicherten Bestände unverändert

### Requirement: Der Arena-Abschluss sichert vorläufig den ganzen Run-Bestand
Das System SHALL beim Erreichen des Arena-Abschlusses beide Run-Bestände vollständig zu den gesicherten Beständen addieren, die Run-Bestände danach auf null setzen und das Profil speichern. Diese Regel gilt, bis Teilsicherung und Quote festgelegt sind.

#### Scenario: Arena wird abgeschlossen
- **WHEN** die vierte Welle geleert ist und der Abschlusszustand beginnt
- **THEN** steigen die gesicherten Bestände um die bisherigen Run-Bestände, die Run-Bestände sind null, und der Abschlusszustand nennt die gesicherten Beträge

### Requirement: Das HUD zeigt die Bestände
Das System SHALL im Kampf-HUD der Arena die Run-Bestände als `GELD <n>` und `GLUT <n>` anzeigen und bei einer Gutschrift die betroffene Zeile kurz hervorheben. Das System SHALL im Hub die gesicherten Bestände als `GESICHERT · GELD <n> · GLUT <n>` anzeigen.

#### Scenario: Spieler kämpft in der Arena
- **WHEN** das Kampf-HUD sichtbar ist
- **THEN** zeigt es die aktuellen Run-Bestände beider Währungen

#### Scenario: Spieler steht im Hub
- **WHEN** der Spieler steuerbar in der Aschenvorhalle ist
- **THEN** zeigt das System die gesicherten Bestände beider Währungen

