# Wissen, Issues und Project pflegen

Stand: 04.10.2026. GitHub ist die gemeinsame Quelle: versioniertes Wissen im Repository, offene Arbeit in Issues, Reihenfolge und Status im Project.

## Ein Ergebnis pro Aufgabe

Ein Arbeits-Issue hat Ziel, überprüfbare Akzeptanzkriterien, Grenzen, konkrete Abhängigkeiten und einen Spezifikationslink. Ein vertikales Feature einschließlich sinnvoller Prüfung darf eine Aufgabe bleiben. Mehrere unabhängige Ergebnisse werden als echte GitHub-Unter-Issues getrennt; Sammel-Issues bleiben als Umfangsrahmen und Integrationsabnahme erhalten.

Unteraufgaben übernehmen Themenlabel und Milestone. Abhängigkeiten stehen vorläufig als Issue-Links im Text; Parent/Sub-Issue ist die echte GitHub-Hierarchie. Ein Parent ist keine Vorbedingung für seine Kinder. Status wird am jeweils bearbeiteten Issue gepflegt; Eltern schließen erst nach erfülltem Gesamtumfang. Kein Issue schließen, nur weil sein Text bereinigt wurde.

## Entscheidungen und Umsetzung

Designfragen erhalten ein Entscheidungs-Issue mit Optionen, Empfehlung und Auswirkungen. Die Empfehlung bleibt offen, bis der Owner sie ausdrücklich festlegt. Danach zuerst datiert in `docs/current/DECISION-LOG.md` dokumentieren und die thematische Spezifikation angleichen. Umsetzung verlinkt den Beschluss. Ein optionaler Prototyp wird bei Ablehnung gestrichen; eine Option wird nicht durch die Planung zur Pflicht.

## Project-Status

| Status | Bedeutung |
|---|---|
| Backlog | Noch nicht ausgewählte oder blockierte Arbeit |
| Ready | Umfang klar, notwendige Entscheidungen und Voraussetzungen erfüllt |
| In progress | Wird aktiv bearbeitet |
| In review | Ergebnis liegt zur Prüfung vor |
| Done | Akzeptanzkriterien nachgewiesen und Issue abgeschlossen |

Bestehende Statuswerte und Themenlabels weiterverwenden. Parent issue und Sub-issues progress nutzen, um Ziele und ausführbare Aufgaben gemeinsam sichtbar zu machen. Keine Kalendertermine oder Prioritätswerte aus ungeklärten Produktentscheidungen erfinden.

## Bestand und Historie

`main` ist Code-Basis; `prototype/design-polish` bleibt Referenz. `openspec/specs/` und Code beschreiben implementiertes Verhalten. Ein geplanter Change oder historischer Zustandsbericht ist kein Umsetzungsnachweis.

Der Import unter `recording-2026-09-08/` ist historisch und darf die bereinigten Issue-Texte und Hierarchien nicht erneut überschreiben. Die aktuelle Zuordnung steht in [issue-map-2026-10-04.json](issue-map-2026-10-04.json). Der vorherige Issue-Text wurde vor der Bereinigung als Snapshot gesichert. Aktuelle Texte werden direkt in GitHub gepflegt; die Zuordnung ist ein datierter Audit, kein automatischer Synchronisationsauftrag.
