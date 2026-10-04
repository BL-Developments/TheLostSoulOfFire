# Combat Vision

## 1. Kampffantasie

Der Spieler führt Death Flame nicht als gewöhnliches Element, sondern als Kraft des Übergangs. Gute Kämpfe fühlen sich schnell, präzise, lesbar und expressiv an. Sie belohnen aktives Beobachten und Reagieren, nicht nur Schadensrotation oder passives Ausweichen.

Das System verbindet:

- klare 2D-Action und Silhouettenlesbarkeit im gemalten Bastion-Stil;
- zeitkritische Reaktionen und befriedigende Folgeangriffe, inspiriert von der Reaktionsqualität moderner God-of-War-Kämpfe;
- Soulfire-spezifische Entscheidungen über Resonance, Anchor, Release und Risiko;
- vollständige Koopfähigkeit.

Keine Referenz wird mechanisch 1:1 kopiert.

## 2. Reaktion erzeugt Angriffsmöglichkeiten

Der zentrale Rhythmus:

```text
Feind kündigt Absicht lesbar an
        ↓
Spieler reagiert im passenden Moment
        ↓
kurzes taktisches Vorteilsfenster
        ↓
besondere Soulfire-Attacke oder Teamaktion
```

Mögliche Reaktionen:

- perfektes Ausweichen;
- Parry bzw. Death-Flame-Deflection;
- gezielter Interrupt;
- Resonance-Match gegen einen gegnerischen Zustand;
- Rettung oder Synchronisierung mit dem Koop-Partner;
- Lösen eines Anchors im richtigen Moment.

Mögliche Belohnungen:

- sofortiger Counter statt bloß höherer Statistik;
- Scythe-Severance, die Rüstung oder Anchor-Verbindung trennt;
- aufgeladener Soul-Cannon-Schuss;
- Positionswechsel oder kurzer Raumkontrollimpuls;
- kooperative Finisher- oder Release-Sequenz;
- Erzeugung von Soul Resonance.

## 3. Keine QTE-Abhängigkeit

„Im richtigen Moment reagieren“ bedeutet Systemtiefe im laufenden Kampf, nicht eine Häufung filmischer Tastenanzeigen.

- Telegraphen werden visuell und akustisch gelernt.
- Erfolgreiche Reaktion bleibt unter Kontrolle des Spielers.
- Besondere Attacken sind kurz, lesbar und abbrechbar oder sicher eingebettet.
- Kamera bleibt für beide Koop-Spieler funktional.
- Fehler sind verständlich und nicht durch Effektchaos verdeckt.

## 4. Ressourcen und taktische Detonation

Bestätigt ist eine mystische Ressource (Arbeitsname Glut), die beim Besiegen eines Monsters genau einmal gutgeschrieben wird; die Erlösung schreibt nichts zusätzlich gut, und die Seele selbst geht weiter. Glut finanziert Run-Fähigkeiten aus dem Run-Bestand und Skilltrees aus dem gesicherten Bestand (Beschluss #52). Sicherungsquote und Teilsicherung werden in #53 entschieden; Koop-Besitz in #9, Ultimate-Ladung in #13.

Die TeamResonance auf `prototype/design-polish` ist eine Referenz und weder automatisch auf `main` vorhanden noch identisch mit Währung, Ultimate-Ladung oder Meta-Fortschritt.

Eine Counter-Detonation kann eine feindliche Manifestation oder geeignete Restenergie betreffen. Ihre genaue Quelle, Kosten und Wirkung sind offen. Erlöste intakte Seelen werden nicht gesammelt oder als Sprengstoff verwendet. Früher diskutierte Soul-Detonationsoptionen sind keine gültige Verbrauchsregel.

## 5. Waffenidentitäten

### Scythe

- mittlere Reichweite, Bögen und Positionskontrolle;
- trennt Verbindungen statt nur Körper;
- starke Reaktion auf sichtbare Anchors;
- kann perfekte Reaktionen in Severance-Fenster verwandeln.

### Soul Cannon

- gerichtete, konzentrierte Death Flame;
- hoher Commitment- und Positionierungswert;
- kann durch Counters oder gesammelte Resonance verändert werden;
- für Fahrzeugsequenzen als größere Warden-Technologie skalierbar.

### Weitere Waffen

Jede neue Waffe muss eine philosophische Funktion des Todes verkörpern, eine eigene Reaktionsform besitzen und im Koop einen unterscheidbaren Nutzen bieten. Namen und Arsenal sind offen.

## 6. Gegnerdesign

Gegner sind hollowed menschliche Geschichten. Ihre Kampfmechanik soll aus dem emotionalen Residuum entstehen.

Beispiele:

- Festhalten: zieht Spieler, sperrt Raum oder bindet den Bruder.
- Schuld: reflektiert Schaden oder bestraft wiederholte Muster.
- Arbeitszwang: wiederholt starre Abläufe und beschleunigt bei Unterbrechung.
- Panik: unberechenbare Flucht- und Ansturmphasen.
- Verdrängung: verbirgt Telegraphen, bis Soul Sense die Wahrheit zeigt.

Ein Major Encounter braucht neben Siegbedingungen auch einen **Release State**. Der beste Ausgang kann andere Anforderungen besitzen als reines Töten.

## 7. Koop-Vertrag

Jede Kernmechanik wird von Beginn an für zwei Spieler gedacht.

- Beide Spieler bleiben handlungsfähig; keine langen Solo-Finisher.
- Telegraphen und Trefferquellen sind eindeutig zuzuordnen.
- Counter-Fenster dürfen vom Partner genutzt oder erweitert werden.
- Builds sollen sich ergänzen, ohne feste Pflichtrollen zu erzwingen.
- Downed-/Rettungsmechaniken müssen das Thema Festhalten und Loslassen stützen, dürfen aber nicht endloses Wiederbeleben trivialisieren.
- Ressourcenentscheidungen wie Sammeln vs. Detonieren brauchen klare Besitz- und Zustimmungsregeln.
- Encounter skalieren nicht nur über Lebenspunkte, sondern über Raumkontrolle, zusätzliche Muster und Teamfenster.
- Kamera, Bewegung und Interaktionen dürfen Spieler nicht regelmäßig gegeneinander blockieren.

## 8. Brüder-spezifische Möglichkeiten — UNRESOLVED

- Resonance-Link für synchronisierte Counters;
- ein Bruder markiert Anchors, der andere trennt sie;
- gemeinsame Soul-Cannon-Überladung;
- gegenseitige Stabilisierung bei drohendem Hollowing;
- unterschiedliche Death-Flame-Ausdrucksformen als Charakterisierung.

Diese Ideen sind Richtungen, keine bestätigten Fähigkeiten.

## 9. Fahrzeugkampf — PROPOSED

Das Death-Flame-Gefährt kann einen frühen Systemtest für Koop liefern:

- Verfolger aus mehreren Richtungen;
- Soul Cannons mit Hitze-/Resonance-Management;
- Wechsel zwischen Geschütz, Reparatur/Stabilisierung und Nahkampf;
- gemeinsame Abwehr eines großen Angriffs durch präzises Timing;
- reduzierte, klare Kamera ohne Effektüberlastung.

Es darf sich nicht wie ein isoliertes Minispiel anfühlen. Soul Cannons und Timingregeln sollen dieselbe Sprache wie der normale Kampf verwenden.

## 10. Combat Pillars

1. **Readable:** Absichten, Treffer und Chancen sind sichtbar.
2. **Reactive:** Timing verändert die verfügbaren Handlungen.
3. **Expressive:** Waffen sind Formen der Death-Flame-Beherrschung.
4. **Narrative:** Gegnermechanik erzählt das Festhalten einer Seele.
5. **Merciful but costly:** Release ist möglich, aber nicht trivial.
6. **Cooperative by construction:** Zwei Spieler sind Kernfall, kein später Modus.
7. **Soulfire-specific:** Entfernt man Flame-, Anchor- und Release-Logik, darf nicht nur ein generischer Actionkampf übrig bleiben.

## 11. Noch zu prototypisieren

- Welche Reaktion bildet den Hauptkern: Dodge, Parry, Interrupt oder mehrere gleichwertige Wege?
- Wie lang und deutlich sind Reaktionsfenster?
- Welche Belohnung fühlt sich besser an: neue Attacke, Stagger, Resource oder Release-Fortschritt?
- Was genau wird bei der vorgeschlagenen Detonation verbraucht?
- Wie interagieren zwei Spieler mit derselben Resonance-Gelegenheit?
- Wie verhindert man Effektchaos bei synchronen Countern?
- Wie bleibt Fahrzeugkampf im Solo- und Koopmodus gleichwertig?
- Welche Mechanik trennt „besiegt“ von „erlöst“?
