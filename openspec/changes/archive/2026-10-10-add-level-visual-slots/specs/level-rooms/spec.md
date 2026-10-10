## MODIFIED Requirements

### Requirement: Räume sind wie die Arena aufgebaut
Das System SHALL jeden Raum mit den Grenzen und der Kampffläche der Arena aufbauen und ihn über die Grafik-Slots seines Bioms zeichnen, nicht mit den gemalten Ebenen der Arena. Der Spieler SHALL einen Raum am Südtor betreten.

#### Scenario: Spieler betritt einen Raum
- **WHEN** der Spieler einen Raum betritt
- **THEN** steht er am Südtor innerhalb der Kampffläche, und die Kamera zeigt den Raum wie die Arena
