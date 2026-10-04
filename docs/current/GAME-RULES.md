# Spielregeln und Produktumfang

Konsolidiert am 04.10.2026 aus der Konzeptaufnahme und dem Entscheidungslog. Dies beschreibt Produktziele; implementiertes Verhalten steht in `openspec/specs/` und im Code auf `main`. Neuere datierte Beschlüsse im [Entscheidungslog](DECISION-LOG.md) haben Vorrang.

## Verbindlich festgelegt

| Bereich | Regel | Quelle / Arbeit |
|---|---|---|
| Run | Homebase → gewähltes Biom ab Level 1 → mehrere Levels → Boss, Extraktion oder Niederlage → Homebase. Freigeschaltete spätere Biome sind direkt anwählbar. | Aufnahme; #11, #14 |
| Niederlage | Kampfunfähigkeit; andere Wardens bergen vor endgültiger Zerstörung. Ungesicherte mystische Ressource geht verloren. Endgültiger Warden-Tod führt weiterhin ins wahre Jenseits. | Beschluss 29.09.; #7, #14 |
| Biome | Der vorherige Biom-Boss schaltet das nächste Biom frei; Meta-Level ersetzt dies nicht. | Aufnahme; #27 |
| Ressourcen | Mindestens Geld und mystische Ressource aus erfolgreicher Erlösung. Die Seele geht weiter und ist keine Verbrauchswährung. | Aufnahme; #7 |
| Risiko | Reisepunkte ermöglichen Teilsicherung mit Weiterreise oder vollständige Extraktion. Mystische Ressource steht zwischen Einsatzkosten im Run und dauerhaftem Fortschritt. | Aufnahme; #18, #21 |
| Waffen | Eine Hauptwaffe vor regulärem Run; Nah- oder Fernkampf. Sense als Startwaffe. | Aufnahme; #8, #15 |
| Fähigkeiten | Drei Angebote, daraus zwei auswählen. Aktive Fähigkeiten und Ultimate sind gewünscht; Ultimate-Modell ist offen. | Aufnahme; #16, #13, #24 |
| Fortschritt | NPC-Begegnungen öffnen Homebase-Dienste; Händler, Schmied und getrennte Waffen-/Fähigkeitsbäume. | Aufnahme; #20, #22, #25 |
| Charakter | Feste Figur ohne anfängliche Klassenwahl; Anpassungsumfang offen. | Aufnahme; #15 |
| Erkundung | Collectibles und Easter Eggs; kein gewöhnliches Loot-Inventar. Sechs-Item-Soul-Echo-Proof gestrichen. | Aufnahme; Beschluss 29.09.; #17 |
| Story | Vaelor wählt den Protagonisten aus, Sense, erste Kämpfe, Bergung und Homebase. Lost Souls bleiben tragische Menschen; Besiegen und Release bleiben getrennt. | Aufnahme; #6, #8 |
| Koop | Spieler 2 ist der bereits zum Warden gewordene Bruder. Kernsysteme werden koopfähig entworfen; lokale Umsetzung ist ein eigenes Ausbauziel. | Canon; #9 |
| Grafik | Gemalte, hochaufgelöste 2D-Grafik wie Bastion; Soulfire Gothic bleibt Identität. | Beschluss 02.10.; [Art Direction](VISUAL-ART-DIRECTION.md) |
| Code | `main` ist Basis; `prototype/design-polish` wird nicht insgesamt gemergt. Benötigte Systeme werden einzeln portiert. | Beschluss 02.10. |

## Offen und optional

[OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) verlinkt jede noch offene Regel mit dem zuständigen Issue. Meta-Level, Heilung/Run-Händler und Sammlungsabschluss-Belohnungen sind Optionen; keine davon darf durch eine Abhängigkeit oder einen Prototyp als beschlossen erscheinen.

## Beispielwerte

50 % Sicherung, 1.000 Einheiten, drei Startwaffen und drei/fünf Levels sind Gesprächsbeispiele. Drei Fähigkeitsangebote mit zwei Auswahlen ist dagegen das konkret besprochene Startmodell. Keine endgültigen Namen, Quoten, Kurven oder Limits aus Beispielen ableiten.

## Bestand und Referenzbranch

Auf `main` sind Hauptmenü, Solo-Prolog, Aschenvorhalle/Biom-Türen und Developer-Starteinstiege dokumentierte Grundlagen. Die Aschenvorhalle ist noch keine vollständige Homebase mit Diensten. Lokaler Koop, TeamResonance, Severance Window und Down/Stabilisieren aus `prototype/design-polish` sind Referenzen, die am jeweiligen Issue gezielt abgeglichen und portiert werden müssen. Ein Dokument, ein geschlossener Planungs-Change oder ein Issue beweist für sich allein keine verfügbare Funktion.
