## ADDED Requirements

### Requirement: Die Trainingspuppe ist ein unbesiegbares Übungsziel
Das System SHALL eine Trainingspuppe als Gegnertyp bereitstellen, der sich nicht bewegt, nicht angreift und durch Rückstoß nicht verschoben wird. Treffer SHALL Trefferfeedback wie bei anderen Gegnern auslösen und das Leben der Puppe senken, jedoch nie unter 1; die Puppe SHALL nie besiegt werden, keine Glut geben und keine Seele freisetzen. Jeder Treffer SHALL seinen vollen Schaden als aufsteigende Zahl über der Puppe zeigen, Kerntreffer hervorgehoben, und über dem Lebensbalken SHALL die Summe seit dem letzten Auffüllen als `SUMME <n>` stehen. Nach 2,5 Sekunden ohne Treffer SHALL die Puppe ihr Leben auffüllen und die Summe zurücksetzen. Die Trainingspuppe SHALL nur in der Sandbox vorkommen, nicht in Arena-Wellen oder im Prolog.

#### Scenario: Sensenkombo auf die Puppe
- **WHEN** der Spieler mit Stärke 10 die Puppe mit allen drei Sensenschlägen trifft
- **THEN** steigen die Zahlen 20, 25 und 40 auf, `SUMME 85` erscheint und der Lebensbalken sinkt

#### Scenario: Puppe stirbt nicht
- **WHEN** die Puppe mehr Schaden erhält, als sie Leben hat
- **THEN** bleibt sie mit 1 Leben stehen, die Summe zählt den vollen Schaden und es erscheinen weder Glut noch Seele

#### Scenario: Auffüllen
- **WHEN** die Puppe 2,5 Sekunden lang nicht getroffen wird
- **THEN** ist ihr Lebensbalken wieder voll und `SUMME` verschwindet
