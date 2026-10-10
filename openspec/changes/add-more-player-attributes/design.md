## Context

`PlayerAttributes` ist ein Werttyp ohne MonoGame mit Stärke, Fähigkeitsstärke und Rüstung. Sense und Kanone bekommen die Werte pro Update, `Player` hält sie, `CharacterSheet` rechnet die Prozentangaben der Charakterseite aus denselben Formeln. Belohnungen schreibt `GameWorld.Currency` gut, Kerntreffer berechnet `GameWorld` beim Treffer.

## Goals / Non-Goals

**Goals:** Sieben weitere Werte mit nachvollziehbarer, getesteter Wirkung; beim Startwert 10 ändert sich kein heutiges Verhalten.

**Non-Goals:** Werte im normalen Spiel steigern, Stufenaufstieg, Ausrüstung, Speichern der Werte, Balancing der Obergrenzen.

## Decisions

- **Eine Regel für alle Faktoren:** `1 + (Wert − 10) × 0,05`, wie bei Stärke. Jeder Punkt ist gleich viel wert, und bei 0 bleibt der Faktor 0,5, also nie null oder negativ.
- **Tempo beschleunigt die Zeit der Aktion, nicht nur den Treffer:** Der Schwung läuft mit `dt × Faktor`, damit Treffermoment, Kombo-Puffer und Darstellung zusammenbleiben. Die Kombo-Rücksetzzeit bleibt, sonst würde hohes Tempo die Kombo leichter abbrechen lassen. Bei der Kanone sinkt die Zeit bis zur vollen Ladung; Ziehen und Wegstecken bleiben gleich.
- **Glück rundet zufällig:** Belohnungen sind kleine Zahlen (Hollow 3 Glut). Feste Rundung würde viele Glückspunkte wirkungslos machen. Der ganzzahlige Teil ist sicher, der Bruchteil wird mit seiner Wahrscheinlichkeit zu einer weiteren Einheit; im Mittel stimmt der Faktor genau. Das skalierte Ergebnis wird vorher auf vier Nachkommastellen gerundet, damit Float-Rauschen keinen seltenen Extrapunkt erzeugt. Der Zufall kommt aus einem eigenen `Random` in `GameWorld`; die Formel nimmt den Wurf als Parameter und ist so ohne Zufall testbar.
- **Kernschärfe wirkt nach dem Kernbonus der Waffe:** erst Stärke oder Fähigkeitsstärke, dann Resonance, dann Kernbonus (1,45 bzw. 1,35), dann Kernschärfe. Gerundet wird einmal am Ende.
- **Einklang in `Player.AddResonance`:** Alle Quellen laufen dort durch, so bleibt keine Quelle unverstärkt.
- **Gewandtheit mit zwei Schritten:** Die Ausweich-Abklingzeit wird durch den Faktor geteilt. Die Laufgeschwindigkeit steigt nur um 1 % pro Punkt (`MoveSpeedPerPoint`), weil 5 % pro Punkt die Steuerung schnell unkontrollierbar machen würde.
- **Fokus beschleunigt das Herunterzählen** statt die Abklingzeit beim Wirken zu kürzen. So stimmt der Ring in der Fähigkeitsleiste, der Restzeit durch Grundabklingzeit teilt, weiterhin.
- **Standfestigkeit teilt den Rückstoß** durch den Faktor. Rückstoß erreicht so nie null, und beim Startwert bleibt er unverändert, anders als bei Rüstung, die schon bei 10 wirkt.
- **Zwei Spalten auf der Charakterseite:** Zehn Werte untereinander passen nicht in 720 Pixel Höhe. Links stehen Leben und die Angriffswerte, rechts Verteidigung, Bewegung, Resonance und Glück. Die Wirkungszeile steht unter dem Namen statt daneben, damit jede Spalte schmal bleibt.
- **Neue Werte als `init`-Properties mit Startwert 10:** Der Konstruktor mit drei Werten bleibt, bestehende Aufrufe funktionieren unverändert, und `with { Luck = 20 }` lässt alle anderen Werte beim Startwert.
- **Dev-Start-Parameter für alle neuen Werte:** Glück wirkt nur, wo es Währungen gibt, also nicht in der Sandbox. Ohne Parameter ließe es sich nicht anspielen. Die `DEV_START`-Zeile nennt neue Werte nur, wenn sie vom Startwert abweichen, damit die bekannte Zeile kurz bleibt.

## Risks / Trade-offs

- Hohe Werte sind nicht balanciert: Tempo 99 lässt die Sense etwa fünfmal so schnell schlagen. Bis es einen Weg gibt, Werte im Spiel zu steigern, betrifft das nur Sandbox und Tests.
- Glück macht Belohnungen bei Werten über oder unter 10 leicht zufällig. Beim Startwert bleiben sie exakt.
