# Design

RunAbilities owns the two selections, cooldowns, retreat, vortex and piercing projectiles. AbilityEffects on Player owns revenge and setup readiness. Enemy tracks expiring setup marks. Weapon damage goes through a dedicated hook so ability damage does not trigger weapon bonuses. Piercing uses swept collision and a set of hit enemies.

Arena currency wallet is the sole payment source; failures do not spend. Healing is capped. Retreat uses the existing rectangular combat bounds, no invulnerability, and rejects a fully blocked path. Devourers cannot be displaced by retreat/vortex. One eligible damage hit is blocked by revenge; next successful weapon hit consumes its bonus. Solo setup marks the first hit target; a later weapon hit consumes one mark bonus. Death/reset/completion clear transient state; pause/selection freeze simulation. No new obstacle system is introduced.

Manual selection exposes all six without claiming the future random draft is implemented. Default slots: Second Wind and Piercing Shot. C opens selection before/between fights, left/right selects slot, 1–6 equips, Enter/C/Escape closes. Duplicate slots are rejected. Normal gameplay retains both existing weapons until the separate weapon-loadout work is implemented.

Provisional values: heal 25 / cost 3 / cooldown 3s; projectile damage 40 (AbilityPower scaling), speed 1000, life 0.75s / cost 3 / cooldown 1s; retreat 180 units over 0.22s, push 65 within 150 / cost 2 / cooldown 2s; vortex range 350, radius 155, duration 2s, pull 200/s / cost 4 / cooldown 4s; revenge guard 2s, bonus 24 for 5s / cost 3 / cooldown 4s; setup readiness and mark 5s, follow-up bonus 25 / cost 2 / cooldown 2s. All are tunable working values.

