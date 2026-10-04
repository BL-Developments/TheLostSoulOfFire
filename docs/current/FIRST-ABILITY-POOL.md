# Erster Fähigkeitenpool — Spezifikationsentwurf für #79

Stand: 04.10.2026. **Auswahl und grundlegende Wirkungsregeln am 04.10.2026 vom Owner bestätigt.** Konkrete Balancewerte und ausdrücklich offene Grenzfälle bleiben zu spezifizieren. Keine Gameplay-Implementierung erfolgt.

[Issue #79](https://github.com/BL-Developments/TheLostSoulOfFire/issues/79) · [Ideenpool](../design/ABILITY-IDEAS.md) · Umsetzung #80 · Draft #16.

## Ziel und Grenzen

Ein bestätigter kleiner Pool mit sechs unterschiedlichen Werkzeugen für drei Angebote und zwei Auswahlen. Kein Ultimate, kein Skilltree, keine vollständige Umsetzung aller 50 Ideen. Lore wird später geprüft.

Das bestätigte Wirtschaftsmodell aus #52 gilt: mystischer Run-Bestand bezahlt Einsätze, kleiner kostenloser sicherbarer Basisvorrat, keine Mitnahme gesicherter Ressourcen. Besitz im Koop wird in #58 festgelegt. Die unten beschriebenen Wirkungen sind unabhängig von persönlichen oder gemeinsamen Konten zu spezifizieren.

## Gemeinsame Regeln — bestätigt

- Fähigkeiten sind mit Nah- und Fernkampf nutzbar; Hauptwaffe und normales Ausweichen bleiben ohne Ressource verfügbar.
- Pro erfolgreicher Aktivierung genau eine Abbuchung. Halten/Wiederholen einer Eingabe erzeugt keine zusätzlichen unbeabsichtigten Einsätze.
- Ein fehlgeschlagener Einsatz (zu wenig Ressource, ungültiges Ziel oder nicht erlaubter Zustand) hat keine Kosten.
- Für bestätigte Einsätze werden feste Kosten definiert; kurze Wiederverwendungszeiten dienen der Eingabekontrolle und ergänzen das Ressourcenmodell.
- Kosten, Dauer, Reichweite, Stärke und Wiederverwendungszeit bleiben bis zur Auswahl offen. Für den Prototyp werden ausdrücklich markierte Arbeitswerte festgelegt.
- Nur handlungsfähige Spieler können aktivieren. Keine Aktivierung während Pause, Down oder Run-Ende.
- Ausgeführte Effekte werden bei Levelwechsel/Run-Ende bereinigt. Umgang mit aktiven Effekten beim Down des Anwenders ist je Fähigkeit festzulegen.
- UI zeigt Kosten, Bereitschaft und fehlende Mittel. Effekte müssen gegnerische Angriffe lesbar lassen.
- Keine versteckte Pflicht zu einer zweiten Waffe oder zu einem Partner.

## Bestätigte Auswahl — Steckbriefe

### AB-006 — Zweiter Atem (Heilung)

**Rolle:** Sofortige, verlässliche eigene Heilung gegen Ressourcenverbrauch.
**Aktivierung:** Selbstziel, einmaliger Heilimpuls, kein Zielen.
**Bestätigte Wirkungsgrenzen:** Heilung bis zum Lebensmaximum; bei vollen Lebenspunkten keine Aktivierung/Abbuchung. Kein Wiederbeleben, kein Schild, keine Heilung des Partners. Solo und Koop identisch.
**Offene Werte:** Heilmenge (fest oder Anteil des Lebensmaximums), Kosten, Wiederverwendungszeit.
**Prüffälle:** Fehlende Lebenspunkte, Überheilung, volle Gesundheit, zu wenig Ressource, wiederholte Eingabe, Aktivierung im Down-Zustand.

### AB-012 — Durchschlag (Direkter Angriff)

**Rolle:** Gegner entlang einer Linie treffen.
**Aktivierung:** Projektil in aktueller Zielrichtung; gleiche Funktion mit beiden Hauptwaffen.
**Bestätigte Wirkungsgrenzen:** Jeder Gegner wird pro Projektil höchstens einmal getroffen; Projektil endet an blockierendem Gelände oder seiner Reichweite. Keine Pflicht zu verbündeten Treffern; kein Schaden an Spielern. Bosse erhalten normalen Fähigkeitsschaden, keine automatische Unterbrechung.
**Offene Werte:** Schaden, Geschwindigkeit, Breite, Reichweite, Kosten, Wiederverwendungszeit.
**Prüffälle:** Mehrere Gegner in Linie, mehrfacher Kollisionskontakt, Wand vor Gegner, kein Gegner, Boss, beide Waffen, Levelwechsel bei fliegendem Projektil.

### AB-017 — Rückstoßsprung (Movement)

**Rolle:** Distanz schaffen und nahe Gegner zurückdrängen.
**Aktivierung:** Bewegung entgegen aktueller Zielrichtung; kurzer Rückstoßbereich nach vorne.
**Bestätigte Wirkungsgrenzen:** Bewegung stoppt vor Hindernissen und verlässt keine Spielfläche. Keine zusätzliche Unverwundbarkeit im ersten Entwurf. Rückstoß gewöhnlicher Gegner; bei Bossen keine Positionsverschiebung. Kein eigener Schaden. Partner wird nicht verschoben.
**Offene Werte:** Sprungdistanz/-dauer, Rückstoßbereich/-stärke, Kosten, erlaubte Unterbrechung eines Waffenangriffs.
**Prüffälle:** Wand direkt hinter Spieler, Spielfeldrand, Gegnerkontakt, Boss, Partnerkontakt, gleichzeitiger normaler Dash. Vor Auswahl festlegen, ob bei vollständig blockiertem Sprung die Aktivierung abgelehnt wird.

### AB-021 — Sog (Kontrolle)

**Rolle:** Gegner für eigene oder gemeinsame Flächenangriffe gruppieren.
**Aktivierung:** Punkt innerhalb begrenzter Zielreichweite bestimmen; dort entsteht kurzzeitig ein Zugfeld.
**Bestätigte Wirkungsgrenzen:** Gewöhnliche Gegner werden zum Zentrum gezogen; Gelände wird nicht durchquert. Keine Spielerbewegung, kein eigener Schaden. Bosse bleiben unbewegt. Gegnerspezifische Resistenzen ausdrücklich dokumentieren.
**Offene Werte:** Zielreichweite, Radius, Dauer, Zugstärke, Kosten; Controller-Zielwahl und überlappende Sogfelder.
**Prüffälle:** Mehrere Gegner, Hindernisse, Ziel außerhalb Reichweite, Boss, leere Fläche, zwei gleichzeitige Felder im Koop.

### AB-027 — Vergeltung (Verteidigung)

**Rolle:** Einen gefährlichen Treffer abfangen und in einen stärkeren Waffenangriff umwandeln.
**Aktivierung:** Kurzzeitige eigene Schutzbereitschaft.
**Bestätigte Wirkungsgrenzen:** Erster zulässiger Schadenstreffer wird vollständig abgefangen; danach verstärkter nächster erfolgreicher Waffenangriff innerhalb eines begrenzten Fensters. Verfällt ohne abgefangenen Treffer oder ohne anschließenden Treffer. Nicht stapelbar. Kein Schutz des Partners, keine Heilung, kein Wiederbeleben.
**Offene Werte:** Schutzdauer, Bonusdauer/-schaden, Kosten; Umgang mit Umweltschaden, Mehrfachtreffern und Flächen-/durchdringenden Waffenangriffen.
**Prüffälle:** Kein Treffer während Schutz, mehrere gleichzeitige Treffer, Waffenangriff verfehlt, Bonusablauf, beide Waffentypen, Down/Levelwechsel mit gespeichertem Bonus.

### AB-048 — Vorlage (Vorbereitung/Koop)

**Rolle:** Eine Gelegenheit für einen Folgeangriff erzeugen; solo vollständig nutzbar.
**Aktivierung:** Rüstet den nächsten erfolgreichen eigenen Waffenangriff mit einer Markierung aus.
**Bestätigte Wirkungsgrenzen:** Im Koop verbraucht ein Folgetreffer des Partners die Markierung; solo der nächste eigene Treffer. Der auslösende Treffer verbraucht sie nicht selbst. Genau ein Bonus pro Markierung, keine Verkettung. Kein Effekt auf verbündete Ziele. Bosse dürfen markiert werden, ohne automatischen Stun.
**Offene Werte:** Bereitschafts- und Markierungsdauer, Bonuswirkung/-stärke, Kosten; Verhalten wenn Partner down ist, bei Flächenangriffen, mehreren Markierungen oder gleichzeitigem Folgeangriff.
**Prüffälle:** Solo-Folgetreffer, Partner-Folgetreffer, gleichzeitige Treffer, Markierungsablauf, Ziel stirbt vorher, beide Waffen, Partner down.

## Entscheidung und Nachweis in #79

- [x] Auswahl der sechs Fähigkeiten und grundlegende Wirkungsregeln bestätigt.
- [ ] Detailregeln aus den Steckbriefen entscheiden; offene Grenzfälle schließen.
- [ ] Je bestätigter Fähigkeit vollständige Arbeitswerte inklusive Kosten festlegen.
- [ ] Mindestens drei unterschiedliche Zweierkombinationen anhand konkreter Kampfsituationen beschreiben, einschließlich Nah- und Fernkampf.
- [ ] Pro Fähigkeit Solo-/Koop-Verhalten und Abnahmefälle vollständig festhalten.
- [ ] Vorschlag mit #16/#70 (Draft) und #58 (Koop-Besitz) abgleichen; verbleibende Voraussetzungen kennzeichnen.

Design kann vor vollständiger Umsetzung des Drafts erarbeitet werden. Eine Implementierung des Drafts ist kein Beleg für bestätigte Fähigkeiten. #79 schließt erst mit bestätigter Auswahl und vollständiger Spezifikation; #80 liefert die spätere spielbare Umsetzung und Nachweise.
