## Context

Der aktive Einstiegspunkt erzeugt `Game1` und genau eine `GameWorld`. Deren `ArenaLoopState` enthält aktuell Titel, Arena-Intro, Kampf, Wellenübergang und Abschluss. Nach beliebiger Eingabe im Titel wird direkt das Arena-Intro gestartet. Der bestehende Renderingpfad zeichnet die Welt in ein Scene-Target, ergänzt Soulfire-Licht und Vignette und legt anschließend Soul-Sense-Layer und HUD darüber.

Im Repository existiert außerdem eine ältere `HubScene` mit eigener Scene-/ECS-/Levelarchitektur. Sie wird vom aktiven `Game1` nicht verwendet, besitzt andere Assets und passt nicht zum modernen Arena-Showcase. Die Aschenvorhalle wird deshalb im aktiven `GameWorld`-Pfad ergänzt.

## Goals / Non-Goals

**Goals:**

- Einen selbst begehbaren, atmosphärischen Raum zwischen Titel und Arena bereitstellen.
- Die visuelle und akustische Soulfire-Identität der Arena vor dem ersten Kampf etablieren.
- Einen klaren, bewussten Eintritt durch ein großes Tor ermöglichen.
- Soul Sense optional für Environmental Storytelling nutzbar machen.
- Bestehendes Spieler-, Kamera-, Render-, Licht-, Partikel-, Input- und Audiosystem wiederverwenden.
- Tod weiterhin schnell in die Arena zurückführen und nur einen vollständigen Neustart wieder über das Pre-Level leiten.

**Non-Goals:**

- Ein persistenter Hub, Levelauswahl oder Meta-Progression.
- Gegner, Kampfziele, Loot, Dialoge oder verpflichtende Tutorials im Pre-Level.
- Mehrere Räume, komplexe Innenwände oder ein allgemeines Leveldateiformat.
- Wiederbelebung oder Migration der alten `HubScene`-Architektur.
- Neue Waffen-, Gegner- oder Soul-Mechaniken.

## Decisions

### Übergeordnete GamePhase trennt Ort vom ArenaLoopState

Eine neue `GamePhase` unterscheidet `Title`, `Antechamber`, `EnteringArena` und `Arena`. Der bestehende `ArenaLoopState` beschreibt innerhalb der Arena weiterhin `Intro`, `Combat`, `Transition` und `Complete`.

Damit wird nicht ein räumlicher Bereich als weitere Wellenphase modelliert. Die Alternative, `Antechamber` direkt in `ArenaLoopState` einzufügen, wäre kurzfristig kleiner, würde aber Reset-, HUD-, Audio- und Präsentationslogik schwerer verständlich machen.

Der Ablauf ist:

```text
Title
  -> Antechamber
       -> EnteringArena
            -> Arena.Intro
                 -> Arena.Combat/Transition/Complete

Arena death + R       -> Arena.Intro
Arena complete + R    -> Title -> Antechamber
```

### Eigener kleiner Bereich statt generischer Levelarchitektur

`SoulFurnaceAntechamber` kapselt Bounds, Spawnpunkt, Torposition, Interaktionszone und Weltdarstellung. Der Raum verwendet eine einfache begehbare Rechteckfläche. Dekoration erzeugt keine zusätzliche Kollisionsgeometrie.

So bleibt die erste Version mit der aktuellen `Player.Update(..., Rectangle movementBounds, ...)`-Schnittstelle kompatibel. Ein datengetriebenes Multi-Level-Format oder Polygon-/Tile-Kollision wäre für einen einzelnen Vorraum unnötiger Scope.

### Ruhige Erkundung verwendet nur sichere Spieleraktionen

Im Pre-Level sind normale Bewegung, Blickrichtung, Ignition Dash und Soul Sense aktiv. Sensen- und Cannon-Angriffe bleiben bis zur Arena deaktiviert. Das erhält Kontrolle und körperliches Spielgefühl, ohne den ruhigen Raum mit wirkungslosen Angriffen, Projektilen oder Kampf-Audio zu füllen.

Soul Sense ist optional. Es zeigt Spuren, eingeschlossene Energie und eine stärkere Reaktion am Tor, darf aber weder Schlüssel noch Pflichtschalter darstellen.

### Das Tor ist eine explizite Interaktion

Erst innerhalb der Interaktionszone erscheint eine zurückhaltende Aufforderung. `E` startet `EnteringArena`; weitere Gameplay-Eingaben werden gesperrt. Torbewegung, Kamera-Vorschub, Lichtimpuls und Audioüberblendung bilden einen kurzen, nicht abbrechbaren Übergang. Nach dessen Ende wird der Spieler am Arena-Spawn zurückgesetzt und die bestehende Arena-Introsequenz gestartet.

Ein automatischer Grenztrigger wäre unsichtbarer, könnte aber versehentlich ausgelöst werden und gäbe dem Eintritt weniger Gewicht.

### Derselbe Render- und Assetpfad erhält den Stil

Die Aschenvorhalle verwendet `SoulfireRenderer`, PointClamp-Weltdarstellung, `ArtAssets`, den bestehenden Spieler-Sprite, Soulfire-Licht, Partikel und Vignette. Architektur wird zunächst wie die aktuelle `Arena` aus präzisen Primitives und wiederverwendbaren Texturen aufgebaut: schwarzer Stein, Metall, gotische Rippen, Ketten, Rohre, tote Öfen und ein massives Tor.

Die orange Flame of Life aus dem Finale wird nicht vorweggenommen. Aktive übernatürliche Beleuchtung bleibt violett und sparsam, sodass das Finale seine farbliche Bedeutung behält.

### Audio verwendet eine gedämpfte Variante des vorhandenen Raums

Für die erste Version sind keine neuen Audiodateien erforderlich. Arena-Ambience läuft in der Vorhalle leiser und stärker zurückgenommen; die Musik setzt erst beim Toreintritt beziehungsweise Arena-Intro ein. Der Übergang verwendet vorhandene Gate-/Wave- und Soulfire-Cues, sofern sie semantisch passen. Gameplay darf weiterhin nicht durch fehlendes Audio blockiert werden.

### Reset-Semantik unterscheidet Encounter und vollständigen Durchlauf

`ResetEncounter` bleibt der schnelle Pfad nach Tod und setzt direkt die Arena auf `Intro`. Ein separater vollständiger Reset leert Arena-, Spieler-, Soul-, Effekt- und Präsentationszustand und kehrt zu `Title` zurück. Dadurch muss ein gescheiterter Spieler die Vorhalle nicht wiederholt durchlaufen, während ein vollständig neuer Durchlauf den vorgesehenen Einstieg zeigt.

Automatisierte Gameplay-/Audio-Tests erhalten einen gezielten Pre-Level-Gate-Schritt, statt nach dem Titel sofort Arena-Combat zu erwarten.

## Risks / Trade-offs

- **[Der Vorraum fühlt sich wie unnötige Wartezeit an]** → Laufweg kurz halten, Kontrolle früh übergeben und das Tor aus dem Spawn lesbar machen.
- **[Pre-Level und Arena wirken wie unterschiedliche Spiele]** → Denselben Renderer, Spieler, Palette, Licht- und Audiopfad verwenden; nur Dichte und Intensität reduzieren.
- **[Soul-Sense-Details werden als Pflichtmechanik missverstanden]** → Tor und Interaktionshinweis sind auch ohne Soul Sense vollständig erkennbar und nutzbar.
- **[Zusätzliche Zustände destabilisieren Retry und Tests]** → GamePhase und ArenaLoopState getrennt halten und alle Start-, Todes-, Abschluss- und Debug-Resetpfade explizit testen.
- **[Kamera oder Dash verlässt den kleinen Raum]** → Bewegung und Kamera gegen eigene Antechamber-Bounds begrenzen und den Spawn mit ausreichendem Abstand zu Rändern wählen.
- **[Übergang erzeugt Lade- oder Audiohärte]** → Beide Bereiche verwenden bereits geladene Assets; Phase und Musik werden über kurze Fade-/Kamerazeiten gewechselt.

## Migration Plan

1. Übergeordnete `GamePhase` und getrennte Resetpfade einführen, ohne den bestehenden Arenaablauf zu verändern.
2. Aschenvorhalle mit Bounds, Spawn, Tor und minimaler Darstellung ergänzen.
3. Pre-Level-Update, Spieleraktionen, Soul Sense, Kamera und HUD/Prompt integrieren.
4. Toreintritt und Übergang in das bestehende Arena-Intro verbinden.
5. Licht, Atmosphäre und Audiomischung angleichen.
6. Laufzeit- und Resetpfade sowie DesktopGL-Build verifizieren.

Ein Rollback entfernt `SoulFurnaceAntechamber` und `GamePhase` und verbindet Titelbestätigung wieder direkt mit `ArenaLoopState.Intro`. Persistente Daten müssen nicht migriert werden.

## Open Questions

- Der endgültige sichtbare Name kann zwischen „Aschenvorhalle“, „Ashen Antechamber“ und einer stärker loregebundenen Bezeichnung abgestimmt werden.
- Eigene authored Environment-Assets können später ergänzt werden, sind aber keine Voraussetzung für die erste Version.
