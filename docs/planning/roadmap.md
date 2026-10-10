# Priorisierung der offenen Issues und offene Fragen

Stand: 04.10.2026 · Repository: [BL-Developments/TheLostSoulOfFire](https://github.com/BL-Developments/TheLostSoulOfFire/issues)

## Ausgangslage und Maßstab

Die übergeordneten Issues #6 bis #29 beschreiben Produktziele und Integrationsumfang; #5 ist der zentrale Wegweiser. Große Aufgaben sind in echte Unter-Issues zerlegt. Bereits klar umrissene vertikale Aufgaben bleiben eigenständig. Die aktuelle Zuordnung steht in [issue-map-2026-10-04.json](issue-map-2026-10-04.json); der tatsächliche Status wird in GitHub gepflegt.

**Branchgrundlage (Beschluss vom 02.10.2026):** `main` ist die Basis. Der Solo-Prolog mit `PrologueDirector` ist seit PR #37 auf `main`. Lokaler Koop, TeamResonance, Severance Window und Down/Stabilisieren existieren nur auf `prototype/design-polish` und werden einzeln als OpenSpec-Change portiert, sobald ein Issue sie braucht. Entscheidungslog, Aufnahme-Zusammenfassung, Canon und Lore liegen seit dem 02.10.2026 unter `docs/current/`.

Die Aufnahme hat laut #5 bei Konflikten Vorrang vor älteren Repo-Dokumenten. Die dort genannten 50 % Teilsicherung, 1.000 Ressourceneinheiten und drei beziehungsweise fünf Levels sind Beispiele, keine beschlossenen Werte. Drei Fähigkeitsangebote mit zwei Auswahlen sind dagegen das konkret besprochene Startmodell.

## P0 – Produktgrundlage und Entscheidungen

| Reihenfolge | Issue | Grund und Ergebnis |
| --- | --- | --- |
| 1 | [#6 – Aufnahme und Lore-Revision](https://github.com/BL-Developments/TheLostSoulOfFire/issues/6) | Klärt, welche älteren Aussagen überholt sind: Vaelors Auswahl, Sense als Startwaffe, Run-Niederlage gegenüber endgültigem Tod, eine Hauptwaffe und kein Loot-Inventar. Ergebnis: konsistenter Entscheidungslog und Canon. |
| 2 | [#7 – Zwei Währungen und Sicherungsregeln](https://github.com/BL-Developments/TheLostSoulOfFire/issues/7) | Grundlage für Speichern, Reisepunkte, Fähigkeiten, Händler und Fortschritt. Ergebnis: Herkunft, Konten, Ausgaben, Verlust und konkrete Rechenbeispiele. |
| 3 | [#9 – Koop-Besitz und gemeinsame Entscheidungen](https://github.com/BL-Developments/TheLostSoulOfFire/issues/9) | Legt fest, wem Ressourcen und Freischaltungen gehören und wie Start, Sicherung und Extraktion bei zwei Spielern entschieden werden. Muss vor Datenmodell und UI feststehen. |
| 4 | [#8 – Prolog überarbeiten](https://github.com/BL-Developments/TheLostSoulOfFire/issues/8) | Vermittelt die neuen Regeln im Einstieg. Umsetzung nach Branchentscheidung und #6; vorhandenen Prolog verwenden, falls er die gewählte Grundlage ist. |
| 5 | [#10 – Erstes Biom entwerfen](https://github.com/BL-Developments/TheLostSoulOfFire/issues/10) | Ein begrenzter Entwurf von menschlichem Ort, Boss-Handschrift und Levelablauf kann früh beginnen und verhindert ungeplante Content-Produktion für #23. |
| 6 | [#13 – Ultimate-Modell entscheiden](https://github.com/BL-Developments/TheLostSoulOfFire/issues/13) | Die bisherige Team-Resonance-Regel ist nicht automatisch die Regel für die neue Fortschrittswährung. Vor #24 entscheiden; blockiert den ersten Run-Kreislauf nicht. |

## P1 – Den Run-Kreislauf spielbar machen

| Reihenfolge | Issue | Grund und Ergebnis |
| --- | --- | --- |
| 7 | [#11 – Spielbare Homebase](https://github.com/BL-Developments/TheLostSoulOfFire/issues/11) | Ort für Vorbereitung, Biomwahl und Rückkehr. Hängt laut Issue von #8 und #9 ab. |
| 8 | [#12 – Dauerhafte Profile und Spielstände](https://github.com/BL-Developments/TheLostSoulOfFire/issues/12) | Gesicherte Ressourcen und Freischaltungen müssen Runs und Neustarts überstehen. Nach #7 und #9 weitgehend parallel zur Homebase möglich. |
| 9 | [#14 – Biom- und Level-Runs](https://github.com/BL-Developments/TheLostSoulOfFire/issues/14) | Verbindet Homebase, mehrere Levels, Niederlage und Neustart bei Level 1 des gewählten Bioms. Benötigt #11 und #12. |
| 10 | [#18 – Reisepunkte, Teilsicherung und Extraktion](https://github.com/BL-Developments/TheLostSoulOfFire/issues/18) | Macht die zentrale Risikoentscheidung spielbar. Benötigt #7, #9 und #14. |

**Erster Spielnachweis:** Homebase → Biom starten → Level abschließen → Teil sichern oder extrahieren → Rückkehr → Spiel neu laden → korrekte Bestände und Freischaltungen vorfinden. Diese Kette vor weiteren Fortschrittssystemen abnehmen.

## P2 – Kampf und Builds integrieren

| Reihenfolge | Issue | Grund und Ergebnis |
| --- | --- | --- |
| 11 | [#15 – Hauptwaffe vor dem Run wählen](https://github.com/BL-Developments/TheLostSoulOfFire/issues/15) | Sense und Cannon zu getrennten Loadouts machen. Beide müssen notwendige Kämpfe und Soul Releases allein bewältigen können. |
| 12 | [#16 – Drei Fähigkeiten anbieten, zwei wählen](https://github.com/BL-Developments/TheLostSoulOfFire/issues/16) | Reproduzierbare Run-Builds einschließlich kleinem Freischaltpool und Koop. |
| 13 | [#19 – Timing-Counter und Teamaktionen](https://github.com/BL-Developments/TheLostSoulOfFire/issues/19) | Vorhandene Reaktionsmechaniken für beide getrennten Waffen prüfen und für den Biomkampf absichern. |
| 14 | [#21 – Run-Fähigkeiten mit Ressourcenkosten](https://github.com/BL-Developments/TheLostSoulOfFire/issues/21) | Setzt „jetzt ausgeben oder später sichern“ im Kampf um. Benötigt #15, #16 und #18. |
| 15 | [#24 – Ultimate integrieren](https://github.com/BL-Developments/TheLostSoulOfFire/issues/24) | Setzt die Entscheidung aus #13 im fertigen Run-Kit um; benötigt auch #21. |
| 16 | [#20 – NPCs schalten Dienste frei](https://github.com/BL-Developments/TheLostSoulOfFire/issues/20) | Verbindet Run-Ereignisse mit der Homebase und bereitet Händler, Schmied und Skilltrees vor. Nach #12 und #14 parallel zu Kampfarbeit möglich. |

## P3 – Erstes Biom und dauerhafter Ausbau

| Reihenfolge | Issue | Grund und Ergebnis |
| --- | --- | --- |
| 17 | [#23 – Erstes Biom mit mehreren Levels](https://github.com/BL-Developments/TheLostSoulOfFire/issues/23) | Integriert Entwurf, Run-Struktur, Reisepunkte, NPCs und Counter zu einem wiederholbaren Abschnitt. |
| 18 | [#22 – Händler und Schmied](https://github.com/BL-Developments/TheLostSoulOfFire/issues/22) | Benötigt NPC-Freischaltung, Wirtschaft und Waffenwahl. |
| 19 | [#25 – Waffen- und Fähigkeits-Skilltrees](https://github.com/BL-Developments/TheLostSoulOfFire/issues/25) | Baut auf Diensten und Fähigkeiten auf; Zuständigkeit gegenüber Schmied-Upgrades klären. |
| 20 | [#26 – Meta-Level prüfen](https://github.com/BL-Developments/TheLostSoulOfFire/issues/26) | Eine besprochene Arbeitsrichtung, noch keine beschlossene Pflichtmechanik. Erst mit konkretem Ressourcen- und Skilltree-Modell beurteilen. |
| 21 | [#27 – Biom-Endboss und nächstes Biom](https://github.com/BL-Developments/TheLostSoulOfFire/issues/27) | Schließt den ersten Biom-Run ab und prüft die dauerhafte Freischaltung des nächsten Bioms. |
| 22 | [#17 – Collectibles und Easter Eggs](https://github.com/BL-Developments/TheLostSoulOfFire/issues/17) | Für Erkundung nützlich, für den Kernkreislauf weniger kritisch. Umfang klein halten, bis Boss, Rückkehr und Fortschritt funktionieren. |

## P4 – Balance und Gesamtabnahme

| Reihenfolge | Issue | Grund und Ergebnis |
| --- | --- | --- |
| 23 | [#28 – Wirtschaft und Builds balancieren](https://github.com/BL-Developments/TheLostSoulOfFire/issues/28) | Horten, Ausgeben, Niederlage und Fortschritt lassen sich erst an einem zusammenhängenden Abschnitt sinnvoll vergleichen. |
| 24 | [#29 – Gesamten Run solo und im Koop abnehmen](https://github.com/BL-Developments/TheLostSoulOfFire/issues/29) | Abschließender Nachweis für Laden, Extraktion, Niederlage, erneuten Start und beide Spielmodi. |
| Übersicht | [#5 – Planungsübersicht](https://github.com/BL-Developments/TheLostSoulOfFire/issues/5) | Als Wegweiser aktuell halten; keine zusätzliche Implementierungsaufgabe. |

**Optionale Inhalte:** #17 und #26 sind für die Kern-Balance keine Pflichtvoraussetzung. Sammlungsbelohnungen und Meta-Level nur berücksichtigen, wenn ausdrücklich beschlossen und implementiert; das Meta-Level ersetzt keine Boss-Freischaltung.

## Offene Fragen

### Sofort klären – sie blockieren Architektur oder Umsetzung

1. **Entschieden:** `main` ist Code-Basis, `prototype/design-polish` Referenz für gezielte Portierungen (02.10.2026).
2. **Entschieden:** Neuere datierte Beschlüsse und `docs/current/` haben Vorrang vor älteren widersprechenden Quellen (02.10.2026).
3. **Was genau sind die beiden Währungen?** Wie heißen sie, wodurch entstehen sie, wofür werden sie ausgegeben und welche Bestände sind im Run beziehungsweise dauerhaft verfügbar? Wann wird eine Soul Release genau einmal gutgeschrieben? (#7)
4. **Wie funktioniert Teilsicherung exakt?** Welche Quote gilt, wie wird gerundet, darf mehrfach pro Run gesichert werden und was geschieht bei erneutem Besuch desselben Reisepunkts? Was wird bei vollständiger Extraktion übertragen? (#7, #18)
5. **Was geschieht mit normalem Geld bei Niederlage?** Der Verlust ungesicherter mystischer Ressource ist vorgesehen; für Geld ist die Regel offen. (#7)
6. **Wem gehören Ressourcen und Fortschritt im lokalen Koop?** Gemeinsame oder persönliche Konten, Waffen, Freischaltungen und Meta-Fortschritt? Wie werden widersprüchliche Entscheidungen am Reisepunkt aufgelöst? (#9)
7. **Entschieden:** Run-Niederlage bedeutet Kampfunfähigkeit mit Bergung und Rückkehr. Endgültiger Warden-Tod führt weiterhin ins Jenseits (29.09.2026). Offen bleibt die Inszenierung.
8. **Wie lädt sich die Ultimate auf und wem gehört die Ladung?** Teamweit oder pro Spieler; durch Kampf, Zeit oder Orte? Was bleibt bei Levelwechsel und Niederlage erhalten? Die bisherige Resonance-Aktivierung darf nicht ungeprüft das Wirtschaftskonto leeren. (#13)

### Vor Content- und Fortschrittsproduktion klären

9. **Welche Waffe hat der Spieler im Tutorial und beim ersten regulären Run?** Die Sense ist Startwaffe, regulär wird eine Hauptwaffe gewählt. Wann wird die Cannon verfügbar, und gibt es im Tutorial vorübergehend Zugriff auf mehr als eine Waffe? (#8, #15)
10. **Welches erste Biom wird gebaut?** Menschlicher Ort, tragische Bossfigur, visuelle Handschrift, grobe Levelzahl und NPC-Reihenfolge brauchen einen begrenzten Beschluss. Bisher genannte Levelzahlen sind Beispiele. (#10, #23)
11. **Wann werden NPC-Freischaltungen dauerhaft?** Sofort bei Begegnung, erst nach Teilsicherung oder nur nach Extraktion? Was bewirkt eine Niederlage dazwischen? (#20)
12. **Wie verhalten sich Fähigkeitsangebote an den Rändern?** Was geschieht bei weniger als drei freigeschalteten Fähigkeiten, beim Abbruch der Vorbereitung und mit dauerhaft freigeschalteten Fähigkeiten? Belegen diese einen der zwei Slots? (#16, #25)
13. **Was unterscheidet Schmied-Upgrades von Waffen-Skilltree-Knoten?** Zuständigkeit, Kosten und Kombination der Effekte müssen eindeutig sein. (#22, #25)
14. **Braucht das Spiel ein Meta-Level?** Falls ja: Zählt kumulativ gesicherter Fortschritt oder der aktuelle Kontostand, und wie werden Teilsicherungen ohne Doppelzählung erfasst? Biomfreischaltungen bleiben an Bosssiege gebunden. (#26)
15. **Welche Speichertiefe braucht die erste Version?** #12 fordert dauerhafte Profile und Freischaltungen, aber keinen fortsetzbaren Spielstand mitten im Run. Offen sind vor allem Koop-Profilzuordnung und Umgang mit beschädigten Speicherdaten. (#12)
16. **Geben Collectibles eine Ressource?** Eine Sammlungsabschluss-Belohnung ist nur eine Option. Falls ja, müssen Zeitpunkt, Währung und Koop-Gutschrift vorher feststehen. (#17)

## Nächster Schritt

Zielbranch ist festgelegt. Als Nächstes #6, #7 und #9 als kurze, widerspruchsfreie Entscheidungsgrundlage abschließen. Damit werden die meisten nachfolgenden Issues ausführbar, ohne ihre Grundregeln mehrfach neu zu entscheiden.

## Pflege seit der Bereinigung

Ergänzung vom 10.10.2026: [Zehn neue Gegner-Archetypen](ENEMY-ARCHETYPES.md)
liegen als biomunabhängiger Entwurf vor. Die Auswahl erfolgt in
[#126](https://github.com/BL-Developments/TheLostSoulOfFire/issues/126),
die spätere Biom-Ausgestaltung in #10 und die Integration in #84. Die zehn
Ideen sind weder bestätigte Spielregeln noch ein verpflichtender Produktionsumfang.

[Projektwissen](../current/README.md), [offene Entscheidungen](../current/OPEN-QUESTIONS.md) und [Pflegeregeln](WORKFLOW.md) sind die Einstiege. Keine Statuszahlen aus diesem Dokument als Live-Stand behandeln. Die Reihenfolge bezeichnet Ziele; ausführbare Einheiten sind die verknüpften Unter-Issues. Vor Umsetzung deren konkrete Geschwister-Abhängigkeiten prüfen.
