## Context

Der Branch `prototype/soulfire-mvp` ersetzt den einfachen, szenenbasierten Fire-Raid-Prototyp nicht vollständig, sondern ergänzt im selben MonoGame-Projekt eine umfangreichere Arena-Implementierung. Die Neuerungen verteilen sich über `GameWorld`, konkrete Spieler-, Waffen-, Gegner- und Seelenklassen sowie eigenständige Systeme für Rendering, Effekte, HUD und Audio. Die Spezifikation dokumentiert diesen bereits gemergten Stand anhand des Codes und der MVP-Designdokumente.

## Goals / Non-Goals

**Goals:**

- Das von `prototype/soulfire-mvp` eingeführte, von außen beobachtbare Spielverhalten testbar festhalten.
- Kampfaktionen, Seelenmechanik, Gegnerrollen und Arenaablauf als getrennte Capabilities beschreiben.
- Gameplay und Präsentation so koppeln, dass Telegraphen, Vollaufladung, Zustandswechsel und Treffer lesbar bleiben.
- Die zentralen Zustandsautomaten und ihre besonderen Interaktionen dokumentieren.

**Non-Goals:**

- Exakte Balancewerte als dauerhaft unveränderlichen Vertrag festschreiben.
- Meta-Progression, Inventar, Händler, Speichern/Laden oder prozedurale Level spezifizieren.
- Produktionsinterne Asset-Erzeugung oder verworfene Art-Kandidaten als Laufzeitverhalten behandeln.
- Die ältere `HubScene`-/`RaidScene`-Architektur mit der Arena-Implementierung vereinheitlichen.

## Decisions

### Fähigkeiten bleiben eigenständige Zustandsautomaten

Sense, Cannon, Dash, Soul Release, Resonance und Gegneraktionen besitzen klar begrenzte Zustände und Timer. Dadurch lassen sich Eingabefenster, Unterbrechungen und Übergänge unabhängig testen. Ein universelles Ability-Framework wurde vermieden, weil die Fähigkeiten bewusst unterschiedliche Regeln haben.

### Soul Sense markiert semantische Trefferzonen

Schwachpunkttreffer werden nur gewertet, wenn Soul Sense beim Angriff aktiv war und die jeweilige gegnerspezifische Zone getroffen wird. Das macht die Wahrnehmungsfähigkeit mechanisch relevant, ohne ein separates Targeting-System einzuführen.

### Soul Release und Resonance sind getrennte Ressourcenflüsse

Eine getötete Manifestation erzeugt eine exponierte Seele. Erst deren erfolgreiche Freisetzung erzeugt Soul Residue und Resonance; die Seele selbst wird nicht verbraucht. Devourer können diesen Übergang verzögern, aber durch Cannon-Stagger oder Tod müssen gefangene Seelen wieder freisetzbar werden.

### Gegner werden durch kleine spezialisierte Klassen modelliert

Hollow, Burning und Devourer teilen Basisschaden und Lebenszyklus über `Enemy`, implementieren ihre Telegraphen und Sonderregeln aber konkret. Eine datengetriebene Behavior-Tree-Lösung wäre für drei feste MVP-Rollen unnötig komplex.

### Präsentation reagiert auf Domänenereignisse

Combat-Presentation, Screen-Effects, Partikel, Licht, HUD und Audio leiten Feedback aus Attack-, Hit-, Release-, Wave- und Resonance-Ereignissen ab. Spielregeln bleiben dadurch unabhängig von konkreten Sound- oder Sprite-Ressourcen, während wichtige Zustände redundant sichtbar und hörbar werden.

### Balancewerte bleiben zentral und abstimmbar

Dauer, Schaden, Reichweite, Multiplikatoren und Schwellen liegen zentral in `GameBalance` beziehungsweise Feedback-Tuning. Die Specs verlangen qualitative Relationen und Zustandsfolgen; konkrete Zahlen dürfen angepasst werden, solange Szenarien und Lesbarkeit erhalten bleiben.

## Risks / Trade-offs

- **[Zwei Gameplay-Architekturen im selben Projekt]** → Einstiegspunkt und Solution müssen eindeutig festlegen, welcher Prototyp ausgeführt wird; eine spätere Bereinigung wird separat geplant.
- **[Viele gekoppelte Feedbacksysteme]** → Domänenzustände bleiben die Quelle der Wahrheit; Audio und VFX dürfen keinen Gameplayzustand steuern.
- **[Balancewerte werden versehentlich zu Produktverträgen]** → Anforderungen formulieren relative Wirkung und Ablauf, nicht unnötig starre Millisekunden- oder Schadenswerte.
- **[Dunkle Effekte verringern Lesbarkeit]** → Spieler, Telegraphen, Seelen und Trefferzonen müssen durch Helligkeit, Silhouette und zeitliche Cues priorisiert werden.
- **[Audio-Geräte oder Assets fehlen]** → Die zentrale Audiosteuerung muss degradieren können, ohne Start oder Gameplay zu blockieren.

## Migration Plan

Die Implementierung ist bereits über den Feature-Branch eingespielt. Dieser Change fügt ausschließlich nachträgliche OpenSpec-Artefakte hinzu. Bei Archivierung werden die neun Capabilities in den Haupt-Spec-Bestand übernommen; ein Rollback entfernt nur diese Dokumentationsartefakte und verändert keinen Laufzeitcode.

## Open Questions

- Ob die ältere Hub-/Raid-Szenenimplementierung entfernt oder mit dem Arena-Showcase verbunden wird, bleibt separat zu entscheiden.
- Finale Balancewerte und Produktionsnamen für Resonance sind weiterhin abstimmbar.
- Automatisierte Laufzeittests für Grafik und Audio benötigen gegebenenfalls eine eigene Headless- beziehungsweise Capture-Strategie.
