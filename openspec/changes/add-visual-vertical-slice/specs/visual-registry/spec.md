## Purpose

Die Visual-Registry trennt Spiellogik von Grafik: Spielcode nennt nur Visual-IDs, eine Datei ordnet ihnen Grafik zu, und jede ID hat eine Visual-Spec mit Lore, Merkmalen und Status. So kann Gameplay neue Figuren und Effekte mit Dummys bauen, während Grafik unabhängig nachgeliefert wird.

## ADDED Requirements

### Requirement: Grafik wird über Visual-IDs zugeordnet
Das System SHALL jede Figur, jeden Effekt, jede Umgebungsebene und jedes Requisit im Spiel über eine Visual-ID darstellen. Welche Texturen, Frames, Bildraten, Schleifen, Ursprünge, Normal-Maps und Weltgrößen zu einer ID gehören, SHALL ausschließlich in der Registry-Datei stehen, nicht im Spielcode. Eine Änderung der Registry SHALL ohne Codeänderung wirken.

#### Scenario: Neue Grafik für eine bestehende ID
- **WHEN** in der Registry die Clips von `enemy.hollow` auf neue Texturen umgestellt werden und das Spiel neu gebaut wird
- **THEN** zeigt das Spiel den Hollow mit der neuen Grafik, ohne dass Spielcode geändert wurde

#### Scenario: Vorhandene Grafik bleibt erhalten
- **WHEN** das Spiel nach Einführung der Registry ohne neue Grafik gestartet wird
- **THEN** sehen Spieler, Gegner, Waffen, Seelen, Arena und Effekte genauso aus wie vorher

### Requirement: Fehlende Grafik erscheint als Dummy
Das System SHALL für eine Visual-ID ohne Registry-Eintrag, ohne passenden Clip oder ohne ladbare Textur eine Dummy-Darstellung zeichnen, die Größe, Position und Blickrichtung der Figur oder des Effekts erkennen lässt, und SHALL dabei weiterlaufen. Das Debug-Overlay SHALL die fehlenden Visual-IDs auflisten.

#### Scenario: Neuer Gegner ohne Grafik
- **WHEN** ein Gegner mit der Visual-ID `enemy.ash-warden` erscheint, für die es noch keinen Registry-Eintrag gibt
- **THEN** wird er als Dummy-Silhouette in seiner Weltgröße mit Blickrichtungsmarke gezeichnet und das Spiel läuft weiter

#### Scenario: Fehlender Clip einer Figur
- **WHEN** die Registry für `player` keinen Clip `dash` enthält und der Spieler dasht
- **THEN** zeigt das System während des Dashs die Laufanimation der Figur und listet `player/dash` im Debug-Overlay als fehlend

#### Scenario: Debug-Overlay zeigt Lücken
- **WHEN** der Entwickler mit `F1` das Debug-Overlay öffnet und im laufenden Spiel Grafik fehlt
- **THEN** nennt das Overlay jede fehlende Visual-ID beziehungsweise jeden fehlenden Clip genau einmal

### Requirement: Jede Visual-ID hat eine Visual-Spec mit Status
Das System SHALL zu jeder Visual-ID in der Registry und zu jeder im Spielcode verwendeten Visual-ID eine Visual-Spec unter `art/specs/<visual-id>.md` führen. Eine Visual-Spec SHALL Art, Lore, Merkmale, Silhouette, Weltgröße, Akzentfarbe, benötigte Animationen oder Effekte mit ihren Spielzeiten und einen Status aus `dummy`, `konzept`, `freigegeben` oder `im-spiel` enthalten. Der Testlauf SHALL fehlschlagen, wenn eine Visual-Spec fehlt, ein Pflichtfeld leer ist, ein Status ungültig ist oder eine Spec mit Status `im-spiel` keinen vollständigen Registry-Eintrag hat.

#### Scenario: Gameplay legt eine neue Figur an
- **WHEN** eine neue Visual-ID im Spielcode verwendet wird und unter `art/specs/` eine Visual-Spec mit Status `dummy` liegt
- **THEN** besteht der Testlauf und das Spiel zeigt die Figur als Dummy

#### Scenario: Visual-Spec fehlt
- **WHEN** eine Visual-ID im Spielcode verwendet wird, für die keine Visual-Spec existiert
- **THEN** schlägt der Testlauf fehl und nennt die Visual-ID

#### Scenario: Status passt nicht zur Registry
- **WHEN** eine Visual-Spec den Status `im-spiel` trägt, die Registry für ihre ID aber nicht alle in der Spec genannten Animationen enthält
- **THEN** schlägt der Testlauf fehl und nennt die fehlenden Animationen
