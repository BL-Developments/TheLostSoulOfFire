## ADDED Requirements

### Requirement: Die Trainingspuppe lässt sich im Dev-Menü spawnen
Das System SHALL im Abschnitt `GEGNER` des Dev-Menüs die Aktion `TRAININGSPUPPE` nach den übrigen Gegnertypen und vor `ALLE GEGNER ENTFERNEN` anzeigen. Sie SHALL eine Trainingspuppe nach denselben Regeln wie andere Gegner in Sichtweite des Spielers setzen; `ALLE GEGNER ENTFERNEN` SHALL auch Trainingspuppen entfernen.

#### Scenario: Puppe spawnen und entfernen
- **WHEN** der Spieler im Dev-Menü `TRAININGSPUPPE` auslöst und später `ALLE GEGNER ENTFERNEN`
- **THEN** steht zuerst eine Puppe in Sichtweite und die Zeile zeigt 1, danach ist sie verschwunden
