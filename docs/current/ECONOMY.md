# Run-Wirtschaft: Sichern, Extraktion und Niederlage

Stand: 06.10.2026. Beschlossen in #53 auf Grundlage von #52. Alle Zahlen sind
Arbeitswerte für das Balancing (#28), keine endgültigen Werte. Geld und Glut
sind Arbeitsnamen.

## Konten

Jede Währung hat einen **Run-Bestand** und einen **gesicherten Bestand** in der
Homebase. Geld hat im Run keine Verwendung; es bezahlt Waffenkauf und Schmied
in der Homebase. Glut bezahlt im Run aktive Fähigkeiten und aus dem gesicherten
Bestand die Skilltrees. Gesicherte Glut wird nicht in einen Run mitgenommen.

Jeder Run beginnt mit einem kleinen kostenlosen Glut-Vorrat. Er gehört zum
Run-Bestand und folgt denselben Regeln wie verdiente Glut.

## Reisepunkte

Am Ende von Level 1 und Level 2 eines Bioms steht ein Reisepunkt. Er bietet
genau drei Entscheidungen:

| Entscheidung | Wirkung |
|---|---|
| Teilsichern und weiter | Je 50 % des Run-Bestands von Geld und Glut wechseln in den gesicherten Bestand. Der Rest bleibt im Run, der Run geht im nächsten Level weiter. |
| Weiter ohne Sichern | Beide Run-Bestände bleiben vollständig im Run. |
| Extrahieren | Beide Run-Bestände werden vollständig gesichert. Der Run endet; der nächste Run dieses Bioms beginnt bei Level 1. |

- Die Quote ist fest; der Spieler wählt keinen Betrag.
- Je Reisepunkt ist höchstens eine Teilsicherung möglich, also höchstens zwei
  pro Biom.
- Der gesicherte Anteil wird je Währung abgerundet; der Rest bleibt im Run.
  Bei 81 Glut werden 40 gesichert und 41 bleiben. Ein Bestand von 1 sichert
  nichts.
- Es gibt keine Obergrenze pro Sicherung.
- Nach dem Bosssieg am Ende von Level 3 kehrt der Run zur Homebase zurück und
  beide Run-Bestände werden vollständig gesichert.

## Niederlage

Bei einer Niederlage gehen beide Run-Bestände vollständig verloren. Alles, was
vorher gesichert wurde, bleibt erhalten.

## Rechenbeispiele

Angenommene Arbeitswerte: 20 Glut Startvorrat; Level 1 bringt 300 Geld und
120 Glut, davon werden 60 Glut für Fähigkeiten ausgegeben; Level 2 bringt
250 Geld und 140 Glut, davon werden 90 Glut ausgegeben.

| Schritt | Geld Run | Glut Run | Geld gesichert | Glut gesichert |
|---|---|---|---|---|
| Run-Start | 0 | 20 | 0 | 0 |
| Ende Level 1 | 300 | 80 | 0 | 0 |
| Reisepunkt 1: Teilsichern | 150 | 40 | 150 | 40 |
| Ende Level 2 | 400 | 90 | 150 | 40 |

Am zweiten Reisepunkt:

| Variante | Ergebnis gesichert | Verloren |
|---|---|---|
| A: Extrahieren | 550 Geld, 130 Glut | nichts; der Run endet |
| B: Teilsichern (200 Geld, 45 Glut), dann Niederlage in Level 3 | 350 Geld, 85 Glut | 200 Geld, 45 Glut |
| C: Weiter ohne Sichern, dann Niederlage in Level 3 | 150 Geld, 40 Glut | 400 Geld, 90 Glut |
| D: Teilsichern, Bosssieg mit +200 Geld und +100 Glut, 60 Glut ausgegeben | 750 Geld, 170 Glut | nichts |

Rundung: Ein Run-Bestand von 81 Glut und 175 Geld sichert beim Teilsichern
40 Glut und 87 Geld; im Run bleiben 41 Glut und 88 Geld.

## Abgrenzung

- Koop-Besitz der Konten entscheidet #58, die Zustimmung beider Spieler an
  Reisepunkten #59.
- Die Arena sichert bis zur Umsetzung der Reisepunkte (#18) weiterhin beide
  Run-Bestände beim Abschluss vollständig (vorläufige Regel aus #52).
- Heilung oder ein Run-Händler, die Geld im Run nutzbar machen würden, bleiben
  Optionen und sind nicht Teil dieser Regel.
