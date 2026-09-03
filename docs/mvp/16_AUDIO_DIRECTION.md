# Audio Direction

**DESIGN STATUS: LOCKED**

## Direction

Audio combines:

- Gothic atmosphere
- industrial weight
- Soul energy
- anime-style combat impact
- deliberate silence

Prefer a small number of strong readable sounds over a large mediocre library.

## Arena Ambience

Use or approximate:

- deep furnace rumble
- distant metal
- chains
- occasional large bell
- wind/reverb
- extremely faint Soul whispering

Leave space between sounds.

## Scythe

- Hit 1: sharp `SWISH`
- Hit 2: stronger `SWOOSH`
- Soul Cleave: deep `WHOOOM + CRACK`
- Core Hit: distinct bright Soul `CRACK`

## Ignition Dash

- short deep flame `WHUMP`
- fast whoosh

Avoid long jet-engine audio.

## Soul Cannon

Charge should audibly escalate:

`low hum → rising Soul whine → high-energy vibration → FULL CHARGE cue → BOOM`

Full charge must be recognizable without looking at the UI.

Full shot:

- strong transient
- bass
- short reverb/space
- heavy mechanical/supernatural character

## Soul Sense

Activation:

- reverse whoosh
- high Soul tone

While active:

- ordinary world audio may be slightly suppressed
- Soul-related sounds may become more prominent

Deactivation:

- short reverse transition

## Hollow

- faint breathing
- distorted whispering
- creaking movement

## Burning

- crackling
- unstable flame rumble
- escalating Charge cue

## Devourer

- heavy movement
- low impacts
- distorted Soul sounds from torso
- unpleasant Devour sound

Enemies should be distinguishable acoustically.

## Soul Release

Combat noise should fall away.

Use:

- calm Soul tone
- soft chime/bell-like release
- quiet Residue whoosh

Release should feel relieving.

## Resonance

Ready:

- clear heartbeat/Core pulse

Activation:

`heartbeat → ~100 ms silence → deep impact → Death Flame eruption`

During Resonance:

- subtle Flame rumble
- music/ambience may intensify if easy

Do not create an annoying constant siren.

## Music

One dark atmospheric industrial/Gothic loop is enough for MVP.

Adaptive music is out of scope.

A simple additional intensity layer or volume change during finale/Resonance is allowed only if trivial.

## PROTOTYPE FALLBACK

Placeholder SFX are acceptable.

Keep audio loading/playback centralized enough that files can be replaced later.

Do not spend MVP time building procedural sound synthesis.
