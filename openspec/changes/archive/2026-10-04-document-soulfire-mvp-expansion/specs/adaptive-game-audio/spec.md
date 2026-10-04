## ADDED Requirements

### Requirement: Audio wird zentral und fehlertolerant gesteuert
Das System SHALL Musik, Ambience und wiederverwendbare SFX-Cues über eine zentrale Audiosteuerung laden und abspielen und SHALL bei nicht verfügbarem Audio ohne Abbruch weiterlaufen.

#### Scenario: Audio-Assets sind verfügbar
- **WHEN** der Content geladen wird
- **THEN** startet das System die vorgesehenen Musik-/Ambience-Schleifen und stellt alle registrierten Gameplay-Cues bereit

#### Scenario: Audiowiedergabe ist nicht verfügbar
- **WHEN** Gerät oder einzelnes Asset nicht initialisiert werden kann
- **THEN** bleibt das Spiel start- und spielbar und unterdrückt den betroffenen Cue kontrolliert

### Requirement: Gleichzeitige Cues werden priorisiert und begrenzt
Das System SHALL Cue-Gruppen, Instanzgrenzen und Mindestabstände verwenden, damit häufige Treffer- und Gegnergeräusche wichtige Zustands-Cues nicht verdecken.

#### Scenario: Viele Treffer treten gleichzeitig auf
- **WHEN** mehr gleichartige Cues angefordert werden als die definierte Gruppe zulässt
- **THEN** begrenzt oder ersetzt die Audiosteuerung Instanzen, statt unkontrolliert alle Sounds zu überlagern

### Requirement: Gameplayzustände verändern die Audiopräsentation
Das System SHALL für Waffenladung, gegnerische Telegraphen, Soul Release, Resonance, Soul Sense, Wellen und Endzustände unterscheidbare authored Cues verwenden.

#### Scenario: Cannon erreicht Vollaufladung
- **WHEN** die Cannon vollständig geladen ist
- **THEN** bestätigt ein eindeutiger Cue die Schussbereitschaft auch ohne Blick auf das HUD

#### Scenario: Soul Sense wechselt Zustand
- **WHEN** Soul Sense aktiviert oder deaktiviert wird
- **THEN** spielt das System einen passenden Übergang und passt die Mischung zugunsten seelenbezogener Klänge an beziehungsweise zurück

#### Scenario: Soul Release gelingt
- **WHEN** eine Seele erfolgreich freigesetzt wird
- **THEN** tritt aggressiver Kampfsound kurz zurück und ein ruhiger Release-/Residue-Cue begleitet den Abschluss
