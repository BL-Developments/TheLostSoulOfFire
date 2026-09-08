# Zusammenfassung der Konzeptaufnahme

Quelle: vom Owner bereitgestelltes, undatiertes Transkript einer Konzeptbesprechung zu **The Lost Soul of Fire**. Importiert und ausgewertet am **2026-09-08**. Dieses Datum ist kein belegtes Aufnahmedatum. Diese Zusammenfassung enthält die produktrelevanten Inhalte; das Rohtranskript und private Gesprächsanteile werden nicht veröffentlicht.

Auftrag: die Aufnahme zusammenfassen, mit der bestehenden Lore abgleichen und daraus logisch getrennte GitHub-Meilensteine und Issues ableiten. **Bei Konflikten hat die Aufnahme nach ausdrücklicher Owner-Anweisung Vorrang.** Bestehende Lore, die der Aufnahme nicht widerspricht, bleibt erhalten. Die dazugehörige Prioritätsentscheidung wird im [Decision Log](DECISION-LOG.md) dokumentiert.

Dies ist eine Planungs- und Entscheidungsgrundlage, keine Behauptung, dass die beschriebenen Systeme bereits implementiert sind. Der aktuelle spielbare Stand steht in [Current Slice](CURRENT-SLICE.md).

## Navigation

- [Kern des Spiels](#kern-des-spiels)
- [Run-Struktur, Biome und Rückkehr](#run-struktur-biome-und-rückkehr)
- [Währungen und Risikoentscheidung](#währungen-und-risikoentscheidung)
- [Waffen, Fähigkeiten und Ultimate](#waffen-fähigkeiten-und-ultimate)
- [Dauerhafter Fortschritt und Charakter](#dauerhafter-fortschritt-und-charakter)
- [Tutorial, Homebase und NPCs](#tutorial-homebase-und-npcs)
- [Erkundung und Collectibles](#erkundung-und-collectibles)
- [Lore-Abgleich und bewusste Revisionen](#lore-abgleich-und-bewusste-revisionen)
- [Offene Entscheidungen und Beispielwerte](#offene-entscheidungen-und-beispielwerte)
- [Ableitungen für Issues und Abnahme](#ableitungen-für-issues-und-abnahme)

## Kern des Spiels

Das Spiel soll ein kooperativ gedachtes Action-Roguelike mit einer dauerhaften **Warden-Homebase** werden. Dort bereitet man einen Run vor, wählt seine Ausrüstung und entwickelt den Charakter weiter. Ein Run führt durch die Level eines Bioms bis zu dessen Boss. Gesammelte Ressourcen ermöglichen unmittelbare Hilfe im Kampf oder langfristigen Fortschritt nach der Rückkehr.

Die zentrale Spannung entsteht durch eine konkrete Entscheidung: **Gebe ich meine mystische Ressource jetzt aus, um weiterzukommen, oder riskiere ich sie, um später in der Homebase stärker zu werden?** Checkpoints erlauben, dieses Risiko teilweise oder vollständig zu beenden.

Nahkampf, Fernkampf, aktive Fähigkeiten, eine Ultimate und reaktionsbasierte Angriffe gehören zur gewünschten Richtung. Der Charakter erhält seine spielerische Ausprägung durch Waffen, Fähigkeiten und Fortschritt. Eine anfängliche Klassenwahl wie Krieger oder Magier und ein gewöhnliches Loot-Inventar werden abgelehnt.

Die Aufnahme bestätigt das gemeinsame Spiel, ändert aber die bereits etablierte Bruder-Prämisse nicht: Der zweite Spieler ist der Bruder des Protagonisten und bereits Warden. Lokaler Koop bleibt die erste technische Grundlage. Die konkrete Verteilung neuer Ressourcen und Fortschrittsrechte zwischen den Brüdern ist noch zu gestalten.

## Run-Struktur, Biome und Rückkehr

### Vereinbarte Richtung

- Die Homebase ist der dauerhafte Ausgangs- und Rückkehrpunkt nach dem Einstieg ins Spiel.
- Es gibt mehrere Biome, die jeweils aus mehreren Leveln bestehen.
- Am Ende jedes Bioms steht ein Boss. Seine Identität soll im Biom und in dessen Gegnern erkennbar sein.
- Der Sieg über den Boss des vorherigen Bioms schaltet das nächste Biom frei. Charakterlevel oder Währungsbesitz ersetzen diese Bedingung nicht.
- Ein einmal freigeschaltetes Biom kann direkt erneut gestartet werden. Man muss dafür nicht immer wieder bei Biom 1 beginnen.
- Scheitert ein Run beispielsweise in Biom 2, Level 3, beginnt der nächste Versuch dieses Bioms wieder bei Level 1.
- Warden-Reisepunkte beziehungsweise Checkpoints zwischen Leveln verbinden den Run mit der Homebase.
- Dort soll man Ressourcen sichern und weiterspielen oder den Run beenden und mit den verfügbaren Ressourcen zurückkehren können.
- Wer den Run durch Rückkehr beendet, setzt später keinen mittleren Levelstand fort, sondern startet das gewählte Biom erneut.

### Noch auszugestalten

Anzahl, Länge, Namen und Reihenfolge der Biome sind nicht festgelegt. Auch die Zahl ihrer Level, Minibosse und die konkrete Kartenvariation bleiben offen. Die Aufnahme legt keine prozedurale Generierung fest.

Das teilweise Sichern bei anschließendem Weiterspielen ist eine gewünschte Checkpoint-Funktion. Der gesicherte Anteil, mögliche Kosten und genaue Regeln sind noch zu bestimmen. Der genannte Wert von 50 Prozent ist ein Beispiel.

Die Regeln für den bestehenden Tutorial-Retry müssen von denen späterer Biome unterschieden werden. Eine spätere Fortsetzung nach Schließen des Spiels ist außerdem eine eigenständige Savegame-Frage; die Aufnahme entscheidet sie nicht.

## Währungen und Risikoentscheidung

### Vereinbarte Richtung

Es soll **mindestens zwei Währungen** geben: eine gewöhnliche Form von Geld und eine mystische Ressource. Letztere entsteht beim Erlösen einer Seele. Die Seele selbst geht ins wahre Jenseits weiter; gesammelt wird ein zurückbleibendes Artefakt, Residuum oder eine entsprechend benannte Ressource.

Die mystische Ressource soll sowohl für Fähigkeiten im Run als auch für langfristige Verbesserung relevant sein. Ihr Verbrauch im Kampf verbessert die Überlebenschance, verringert aber den Betrag, den man für dauerhaften Fortschritt sichern könnte. Scheitern bedroht den noch nicht gesicherten Bestand.

Waffenkauf, Waffenverbesserung und Charakterentwicklung sind vorgesehene Ausgabebereiche. In der Besprechung werden Geld für Waffen und die mystische Ressource für Charakter- beziehungsweise Skilltree-Fortschritt vorgeschlagen. Die genaue Zuordnung sämtlicher Preise und Dienste ist noch kein abgeschlossenes Ökonomiemodell.

### Offene Ausgestaltung

- Endgültige Namen, visuelle Form und Lore der beiden Währungen.
- Welche Ausgaben nur Geld, nur mystische Ressource oder eine Kombination benötigen.
- Optionale Heilung oder Waffenverbesserung bei einem Händler während des Runs.
- Was beim Scheitern mit gewöhnlichem Geld geschieht; ausdrücklich besprochen wird vor allem das Risiko der ungesicherten mystischen Ressource.
- Ob und wie viel jede Checkpoint-Nutzung sichern kann.
- Gemeinsame oder persönliche Bestände im Koop, Ausgaberechte und Zuordnung dauerhafter Freischaltungen.

Die Existenz zweier Wirtschaftswährungen verpflichtet nicht zu einer dritten Ultimate-Währung. Ebenso dürfen Geld, gesicherter Fortschritt und die bestehende Team-Resonance-Anzeige nicht allein wegen ähnlicher Begriffe als dasselbe Konto behandelt werden.

## Waffen, Fähigkeiten und Ultimate

### Waffenwahl und Kampf

Vor einem Run wird **eine ausgerüstete Waffe** gewählt: eine Nahkampf- oder eine Fernkampfwaffe. Die Sense ist als Einstiegswaffe vorgesehen. Weitere Waffen sollen über einen Händler zugänglich werden und eigene Verbesserungen, Eigenschaften oder Kombos ermöglichen.

Das unterscheidet sich vom derzeit gleichzeitig verfügbaren Sense-und-Soul-Cannon-Kit. Die vorhandenen Kampfimplementierungen bleiben wertvolle Grundlagen für auswählbare Waffen; der neue Plan muss ihre Verfügbarkeit und alle davon abhängigen Begegnungen an die Auswahl anpassen.

Timing soll mehr bieten als Schadensvermeidung. Als Beispiel wird ein heranstürmender Gegner genannt, der bei einem präzise gesetzten Treffer explodiert und weitere Gegner trifft. Das ist eine Referenz für eine Interaktion mit einer feindlichen Manifestation, keine Festlegung, intakte Seelen als Sprengstoff zu verbrauchen. Bestehende Severance-Fenster und Gegnerreaktionen bieten dafür eine Grundlage.

### Fähigkeiten vor jedem Run

Aus einem größeren Pool werden zu Run-Beginn **drei Fähigkeiten angeboten, von denen zwei gewählt werden**. Dies ist das konkrete Ausgangsmodell aus dem Gespräch; es soll als solches in der Planung erhalten bleiben. Die Anzahl kann für spätere Balanceanpassungen konfigurierbar sein.

Der Pool kann Angriffszauber, Bewegungsfähigkeiten und andere aktive Möglichkeiten enthalten. Die wechselnde Auswahl erhält eine Erklärung innerhalb der Welt: Auch andere Wardens sind unterwegs und nutzen die geteilten Kräfte beziehungsweise verfügbaren Möglichkeiten. Deshalb steht nicht vor jedem Run dieselbe Auswahl bereit.

Verbesserungen und später dauerhaft verfügbare Fähigkeiten sollen über Fortschritt erreichbar sein. Wie garantierte Fähigkeiten mit den zufälligen Angeboten kombiniert werden, muss konkret entworfen werden. Zufällige Verfügbarkeit soll keine Behauptung begründen, dass zukünftige Online-Spieler tatsächlich um einen gemeinsamen Serverbestand konkurrieren.

### Ultimate

Eine Ultimate ist ausdrücklich gewünscht. Ihre Finanzierung ist hingegen offen. Die Aufnahme erkennt das Problem, dass eine Ultimate, die den gesamten Vorrat verbraucht, die Sparentscheidung für dauerhaften Fortschritt verzerren könnte.

Diskutierte Alternativen sind eine getrennte Ressource, ein Zeitfaktor, Aufladung durch besiegte Gegner oder besondere Aufladeorte. Keine dieser Alternativen ist als endgültige Lösung beschlossen. Auch eine weiterhin release-basierte Resonance muss deshalb anhand der neuen Ökonomie überprüft werden. Der bestehende Resonance-Modus ist eine umsetzbare Ausgangsbasis, keine bereits bestätigte Endfassung.

## Dauerhafter Fortschritt und Charakter

### Vereinbarte Richtung

- Ein Fortschrittszweig verbessert die vor dem Run gewählte Waffe und kann weitere Eigenschaften oder Spezialangriffe zugänglich machen.
- Ein anderer Zweig verbessert die Fähigkeiten aus dem wechselnden Pool.
- Spätere Freischaltungen sollen den Zugriff auf einzelne Fähigkeiten verlässlich beziehungsweise dauerhaft machen können.
- Neue Waffen und Verbesserungen werden an die Angebote der Homebase und ihrer Bewohner angebunden.
- Der Protagonist bleibt eine bestehende Figur. Eine Klassenwahl beim Erstellen eines Charakters ist nicht vorgesehen.

Rüstung und äußere Anpassung werden als mögliche Formen der Individualisierung genannt. Daraus folgt noch kein Rüstungs-Lootsystem. Herkunft, Slots, Auswirkungen und Umfang dieser Anpassung bleiben offen.

### Vorgeschlagene Meta-Level

Im Gespräch entsteht die Idee eines Grundlevels, das durch erfolgreich nach Hause gebrachte mystische Ressourcen steigt. Dieses Level könnte spätere Waffen oder Bereiche der Skilltrees zugänglich machen und wiederholte Runs belohnen.

Das ist eine zu konkretisierende Richtung, keine fertige Erfahrungsformel. Insbesondere ist zu entscheiden, ob ein kumulativer Fortschrittszähler verwendet wird und wie Ausgaben davon getrennt sind. Die Aufnahme entscheidet weder Schwellen noch eine Rückstufung durch Einkäufe. **Biome werden weiterhin durch den vorausgehenden Boss freigeschaltet.**

Beispiele wie mehr Schaden, höhere Angriffsgeschwindigkeit oder doppelter Fähigkeitsschaden illustrieren Verbesserung. Sie ersetzen keinen Entwurf, der interessante Entscheidungen und unterschiedliche Spielweisen trägt.

## Tutorial, Homebase und NPCs

### Einstieg

Nach „New Game“ beginnt ein Tutorialpfad. In der aufgenommenen Fassung **wählt Vaelor den Protagonisten aus, dieser erhält die Sense, kämpft sich durch den Startbereich und wird von einem Warden geborgen**. Anschließend erreichen sie die Homebase. Die bereits etablierte Bruder-Begegnung kann diese Bergung im Koop tragen.

Der vorhandene Prolog mit Emergence, Suche, Bruder, Escape/Transit und Homebase-Schwelle ist deshalb eine Grundlage für Anpassungen. Ein zweites, paralleles Tutorial ist daraus nicht abzuleiten. Die Aufnahme erläutert nicht abschließend, wie Vaelors Auswahl metaphysisch funktioniert; der Konflikt zur bisherigen Fassung wird unten ausdrücklich festgehalten.

### Einführung der Homebase-Dienste

NPCs können während Runs gefunden, angesprochen oder gerettet werden. Danach werden ihre Dienste dauerhaft in der Homebase zugänglich. Genannt werden ein Händler, ein Schmied für Waffenverbesserungen und eine Figur beziehungsweise Funktion für Skilltrees.

Das erste Biom soll diese Systeme nach und nach vorstellen. Der Händler kann glaubwürdig unterwegs sein, weil er andere Wardens im Einsatz versorgt. Eine Rettung nach einem Miniboss oder Boss ist eine mögliche erzählerische Verbindung, aber noch keine festgelegte Questabfolge.

Die Abfolge „erster Level Händler, zweiter Level Schmied, nach dem Abschluss Skilltrees“ ist ein Beispiel für die Lernkurve. Frühere Passagen nennen andere Levelzahlen. Daraus darf keine unumstößliche Zuordnung abgeleitet werden.

## Erkundung und Collectibles

Ein gewöhnliches Inventar, in dem ständig neue Kampfgegenstände aus Zufallsloot gesammelt werden, wird in der Aufnahme ausdrücklich ausgeschlossen. Das Kern-Loadout besteht aus Waffe und Fähigkeiten.

Gewünscht sind dagegen **Collectibles und Easter Eggs**, die aufmerksames Erkunden und genaues Hinsehen belohnen. Sammelkategorien können bei Vervollständigung eine zusätzliche Belohnung auslösen, etwa Geld oder mystische Ressource. Art und Höhe dieser Belohnungen sind offen.

Ein Sammelregister ist damit möglich, ohne eine Tasche mit ausrüstbaren Lootgegenständen einzuführen. Persönliche Erinnerungsstücke, menschliche Geschichte und regionale Spuren passen zu dieser Richtung, sind aber konkrete Inhaltsvorschläge und keine in der Aufnahme festgelegten Collectible-Sets.

## Lore-Abgleich und bewusste Revisionen

| Thema | Bisherige Grundlage | Vorrang der Aufnahme und Konsequenz |
|---|---|---|
| Vaelors Auswahl | [Canon Status](CANON-STATUS.md) und [Story Opening](STORY-OPENING.md) lehnen eine persönliche Auswahl oder Ernennung durch Vaelor ab. | Die Aufnahme sieht seine Auswahl des Protagonisten und die anfängliche Sense vor. Diesen Storybeat als neue Richtung erhalten. Die genaue metaphysische Ursache ist offen; nicht stillschweigend wieder auf bloßes Erkennen von Potenzial reduzieren. Gottheit, Siegel, Allwissenheit und Bestimmung fremder Todeszeitpunkte werden damit nicht eingeführt. |
| Waffenverfügbarkeit | Der Prototyp stellt Scythe und Soul Cannon gemeinsam bereit. | Vor einem Run wird eine Nahkampf- oder Fernkampfwaffe gewählt. Bestehende Waffen, Gegnerinteraktionen und Tutorialschritte auf dieses Loadout umstellen, wenn die zugehörigen Issues implementiert werden. |
| Items und Inventar | [Product Truth](PRODUCT-TRUTH.md) nennt einen sechs Items umfassenden Soul-Echo-Proof. | Die neuere Absage an ein gewöhnliches Loot-Inventar hat Vorrang. Diesen Proof nicht unverändert umsetzen. Nützliche Effekte können nach passendem Entwurf in Fähigkeiten, Waffenfortschritt oder Freischaltungen überführt werden. |
| Niederlage und endgültiger Tod | Die endgültige Zerstörung eines Wardens vollendet seinen Übergang ins wahre Jenseits. | Der Run führt bei Scheitern zur Homebase zurück. Für wiederholbare Niederlage beziehungsweise Bergung eine konsistente Erklärung entwerfen; die Aufnahme erläutert sie noch nicht. Endgültigen Tod nicht versehentlich mit jedem verlorenen Run gleichsetzen. |
| Checkpoints | Der aktuelle Prolog besitzt sektorbezogene Retries. | Reguläre Biom-Runs starten nach Scheitern oder beendeter Rückkehr wieder bei Level 1 des gewählten Bioms. Tutorial-, Debug- und Run-Regeln klar auseinanderhalten. |
| Ressourcen und Team Resonance | Der lokale Koop-Prototyp nutzt einen gemeinsamen Resonance-Pool. | Die Aufnahme ergänzt Geld, eine sicherbare mystische Wirtschaftswährung und eine noch offene Ultimate-Aufladung. Gemeinsamkeit oder Trennung dieser Konten sowie persönliche Fortschrittsrechte ausdrücklich entwerfen. |
| Gesammelte Souls | Aktuelle Lore lässt einzelne taktische Soul-/Fragment-Fragen offen; der Release-Loop hinterlässt bereits Residuum. | Gesammelt wird etwas, das nach der Erlösung zurückbleibt. Die Seele geht weiter. Intakte, bewusste Seelen sind kein gewöhnlicher Treibstoff und keine Handelswährung. |

### Weiterhin gültige Lore

- Life Flame gibt einer Seele die Kraft zu bleiben; Death Flame gibt ihr die Kraft loszulassen.
- Lost Souls sind tragische Menschen, keine grundsätzlich böse Spezies.
- Eine Manifestation zu zerstören und ihre Seele zu erlösen sind unterschiedliche Ereignisse.
- Die Death Layer ist die Übergangsebene, nicht das wahre Jenseits.
- Vaelor bleibt First/Highest Warden und kein Gott. The Keeper bleibt unbekannt; The Stillness ist kein Reiseweg.
- Der zweite Spieler bleibt der bereits zum Warden gewordene Bruder.
- Regionen verbinden menschlichen Ort, menschliches Ereignis, emotionales Residuum und Seelenverformung. Die Handschrift eines Biom-Bosses ergänzt dieses Fundament.
- Soulfire Gothic, violett-weiße Death Flame und die seltene warme Life Flame bleiben die visuelle Identität. Die Aufnahme nennt keinen Ersatz für die bestehende Qualitätsrichtung.

## Offene Entscheidungen und Beispielwerte

### Nicht als feste Zahlen übernehmen

| Gesprächsbeispiel | Einordnung |
|---|---|
| 1.000 erlöste Seelen beziehungsweise Ressourceneinheiten laden eine besondere Fähigkeit auf | Veranschaulicht eine Schwelle; kein festgelegter Preis oder Zähler. |
| 50 Prozent am Checkpoint sichern und weiterspielen | Veranschaulicht Teilsicherung; kein beschlossenes Verhältnis. |
| Drei oder fünf Level pro Biom | Veranschaulicht die Struktur; Umfang offen. |
| Drei Anfangswaffen | Als Möglichkeit genannt; die Sense ist die vorgesehene Einstiegswaffe. |
| Händler in Level 1, Schmied in Level 2, Skilltrees nach dem Abschluss | Beispiel für gestufte Einführung; genaue Reihenfolge und Freischaltbedingungen offen. |
| Doppelter Schaden oder häufigere Angriffe | Beispiele für Verbesserungen; keine Balancewerte. |
| Drei zufällige Angebote, davon zwei wählen | Konkretes angenommenes Ausgangsmodell für den Fähigkeitenentwurf; nicht mit den beiläufigen Zahlenbeispielen gleichsetzen. |

### Entscheidungen vor abhängiger Umsetzung

Die größten offenen Fragen sind das genaue Bank-/Verlustmodell, die Ultimate-Aufladung, die Meta-Level-Berechnung, der Umfang dauerhafter Fähigkeiten und die Besitz-/Ausgaberegeln im Koop. Ebenso benötigen Vaelors Auswahl und die wiederholbare Run-Niederlage eine klar abgegrenzte Lore-Fassung.

Keines dieser offenen Details verhindert, die beschlossenen Zielsysteme bereits als Issues zu erfassen. Ein Issue muss eine noch notwendige Designentscheidung dann als solche benennen und darf keinen beiläufigen Vorschlag als fertiges Akzeptanzkriterium ausgeben.

## Ableitungen für Issues und Abnahme

Die Planung lässt sich in **Lore und Produktregeln**, **Homebase und Einführung**, **Run-Struktur und Speichern**, **Ökonomie**, **Kampf und Loadouts**, **dauerhaften Fortschritt**, **Biome und Bosse** sowie **Collectibles** gliedern. Diese Kategorien sind eine Ableitung aus dem Gespräch, keine zusätzliche Kampagnenentscheidung.

Für jedes spielrelevante Issue sollen Solo und lokaler Koop gemeinsam betrachtet werden: Wer wählt Loadout und Fähigkeiten, wer darf Ressourcen ausgeben oder sichern, was geschieht bei einem einzelnen gefallenen Bruder und wann scheitert der gesamte Run? Neue Regeln dürfen die bestehende Bruder- und Release-Logik nicht stillschweigend umgehen.

Vorhandene Systeme wie PrologueDirector, Scythe, Soul Cannon, Severance, Soul Release, Team Resonance und deterministische Szenarien sollen bei späterer Implementierung wiederverwendet und passend erweitert werden. Noch nicht vorhandene Hub-Innenräume, Wirtschaft, Savegame und Meta-Progression sind dabei als neue Arbeit auszuweisen.

Die Veröffentlichung dieser Zusammenfassung und der GitHub-Planung verändert keine Laufzeitmechanik. Eine spätere Abnahme muss die neuen Entscheidungen im tatsächlichen Spiel und in relevanten deterministischen Szenarien prüfen; ein erfolgreicher Build allein bestätigt weder Ökonomie noch Spielgefühl.
