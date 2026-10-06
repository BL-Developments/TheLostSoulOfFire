## Context

Gegner leiten von `Enemy` ab, das Leben, Rückstoß, Trefferblitz und `OnDeath` verwaltet. `GameWorld` trifft Gegner über `ApplyEnemyDamage` (Sense, Kanone, Burning-Explosion, `F6`) und schreibt Glut nur beim Übergang von lebend zu besiegt gut. Sprite-Art zeichnet `ArtAssets.DrawEnemy` nur für Hollow, Burning und Devourer; andere Typen zeichnen sich über ihr eigenes `Draw`. `SandboxEnemyKind` ist seit `add-sandbox-enemy-spawner` die zentrale Liste spawnbarer Typen.

## Goals / Non-Goals

**Goals:**

- Ein stillstehendes Ziel, das jeden Treffer und die Summe lesbar zeigt und nie verschwindet.
- Kein Sonderfall in der Kampflogik von `GameWorld`.

**Non-Goals:**

- Sprite-Art für die Puppe, DPS-Messung über Zeit, einstellbare Puppenwerte.

## Decisions

**Lebensuntergrenze statt Unverwundbarkeit.** `TrainingDummy.ApplyDamage` merkt sich den vollen Treffer (Zahl, Summe) und reicht an `Enemy.ApplyDamage` einen auf `Health − 1` gekürzten Schaden ohne Rückstoß weiter. So bleiben Trefferblitz und Lebensbalken echt, `IsAlive` wird nie falsch, `OnDeath` wird nie erreicht und `GameWorld` braucht keine Ausnahme: Es gibt keinen Übergang zu besiegt, also keine Glut, keine Seele, keinen Todeston. Auch `F6` lässt die Puppe stehen; entfernt wird sie mit `ALLE GEGNER ENTFERNEN`.

**Feste Position.** Die Puppe merkt sich ihren Ankerpunkt und setzt sich nach `UpdateCommon` jedes Bild darauf zurück. Rückstoß wird schon beim Treffer verworfen.

**Auffüllen nach Pause.** Nach `TrainingDummyRefillDelay` (2,5 s) ohne Treffer setzt die Puppe Leben und Summe zurück. Ein neuer Treffer startet die Pause neu. Das macht Kombos und Ladeschüsse vergleichbar, ohne die Puppe neu zu spawnen.

**Lesbare Zahlen.** Jeder Treffer erzeugt eine Zahl, die in 0,9 s aufsteigt und verblasst; drei versetzte Spuren verhindern, dass schnelle Treffer übereinanderliegen. Kerntreffer erscheinen in `DeathFlameBright`. `SUMME <n>` steht über dem Lebensbalken. Die Puppe ist mit einfachen Formen in gedeckten Holz- und Strohtönen gezeichnet, passend zum dunklen Arena-Stil.

**Nur Sandbox.** Die Puppe ist ein `SandboxEnemyKind`, aber kein `ArenaEnemyKind`; Wellen und Prolog erzeugen sie nicht.
