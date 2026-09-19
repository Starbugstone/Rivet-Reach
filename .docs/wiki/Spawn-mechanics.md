# Spawn mechanics

Creature spawning depends on the actual ground, available space, light, your position and the existing population. A dark room is only one part of the check. Natural hostiles, [cage spawners](Spawners.md) and [chickens](Chickens.md) share basic site requirements but have different population and persistence rules.

These are the current Alpha settings. Distances are measured in blocks; spawn distances include vertical separation unless stated otherwise. Timings are active simulation time and pause with the game.

## Which creatures can appear?

| Creature | Natural habitat | Time | Required light above the floor | Natural species cap |
| --- | --- | --- | --- | ---: |
| [Rustback beetle](Rustback-Beetle.md) | Surface | Day or night, if dark enough | 0–7 | 8 |
| [Dusk prowler](Dusk-Prowler.md) | Surface | Night, 18:00–06:00 | 0–7 | 6 |
| [Floater](Floater.md) | Covered underground caves | Day or night | 0–7 | 4 |
| [Chicken](Chickens.md) | Surface grass | Day, 06:00–18:00 | 9–15 | 12 within 48 blocks of a proposed site |

The three hostile species also share a **14-living-natural-hostile cap** across the current population. It is not a separate allowance for every room or chunk. Four natural Floaters in nearby caves can prevent a fifth from appearing in your prepared room even when that room is suitable. Chickens and cage-produced mobs do not consume these natural hostile allowances.

## Ground, habitat and body clearance

Current hostiles accept **grass, dirt, stone, sand, sandstone, snow, red clay, cobblestone and planks**. The entire creature footprint must have eligible support. Natural chickens require **grass**; placing a wooden floor in a pen does not attract new wild chickens.

A surface site must be exposed to the sky. A Floater site must be covered and below that column's terrain surface. A roof over an above-ground building does not turn it into an underground Floater habitat, and an open shaft is not a covered cave. Deep constructed rooms can qualify: cobblestone and plank floors are accepted.

The complete body volume must be loaded, dry and free of solid blocks. Water and lava in that volume prevent a spawn. The site must not overlap the player, a hostile or a chicken. Natural hostiles additionally avoid positions within three blocks of another living hostile.

| Hostile | Body width | Body height | Height hovering above support |
| --- | ---: | ---: | ---: |
| Rustback beetle | 0.90 | 0.92 | 0 |
| Dusk prowler | 0.85 | 1.70 | 0 |
| Floater | 0.90 | 1.05 | 0.60 |

For a Floater room, leave at least two clear blocks above a flat floor and room across its whole footprint. Decorative blocks, low ceilings and existing creatures can invalidate an otherwise dark location.

## Light that prevents hostile spawning

Hostiles require **0–7 at every tested floor position under their footprint**. Level **8 or higher** at any of those positions rejects the site. The game checks immediately above the supporting floor, including beneath hovering Floaters.

Daylight, placed torches, powered Workshop Lamps and lava affect this light value. Walls and ceilings can leave dark pockets. A held torch brightens your view but does not protect the floor from spawning. Display brightness and ambient visual light are not substitutes for placed light.

![A placed torch illuminates a cage and prevents new Floater spawns](images/alpha-playtest/floater-spawner-torch-disabled.png)

Existing creatures remain alive and can walk into lit areas. After changing a roof or light source, spawning waits for a valid light result; it does not treat an uncalculated area as dark. See [Lighting and underground farms](Lighting-and-underground-farms.md) for light behavior.

## Natural hostile attempts

Natural hostiles must appear **24–48 blocks from you**, measured in three dimensions. Standing inside a prepared room prevents spawning close to you. Standing more than 48 blocks from it prevents natural attempts there too. A site must also lie outside the protected **16-block horizontal radius around the world's initial origin**.

Candidates in your current camera view are rejected when there is a clear line of sight. Look away from the area or observe from behind a wall. Underground candidates can be above or below you within the distance range; there is no separate 24-block horizontal exclusion for caves.

The game begins its natural attempt timer at four seconds, then tries every **two seconds** while the expedition is ready and you are alive. Each cycle examines up to **eight randomly chosen species/columns** and creates at most one hostile. Cave columns search a bounded vertical range around your height. The system only reads loaded terrain; it does not load distant caves to find a spawn site.

An attempt is not a guaranteed spawn. It may choose another species, a blocked column, an unsuitable habitat or an area already at its cap. A large eligible floor offers more possible sites than a small room, but nearby caves still compete for the same natural Floater cap. There is no fixed promise that a particular room fills within a few minutes.

## Cage spawners

A cage uses the same species habitat, floor, body-clearance and light checks. Its miniature identifies the species; that miniature is only a visual and does not consume a population slot.

| Setting | Current Floater cage |
| --- | --- |
| Player activation distance | Within 36 blocks of the cage centre |
| First attempt | After four active seconds |
| Later cycles | Every ten active seconds |
| Candidate area | Up to four blocks in each horizontal direction; nearby floor levels |
| Candidate tries per cycle | Up to 12 |
| Successful spawns per cycle | At most one |
| Active allowance | Five living mobs originating from that cage |
| Light | 0–7 above the cage itself and across the chosen supporting floor |

Cages do not use the natural 24–48 distance band, camera-view exclusion or natural population caps. They still cannot place a mob inside you, another creature, solid terrain or liquid. Placing sufficient light at the cage prevents it from redirecting attempts into a dark corner.

Each cage counts **its own originating mobs**, including ones that move away. Two neighbouring cages can each have five. A full natural hostile population does not disable them. Defeating or normally despawning one of a cage's mobs frees that cage's slot; another eligible cycle can replace it. Failed cycles wait for the next cooldown.

Cooldowns pause when you leave activation range or the cage's chunk unloads. Cages do not load chunks themselves. Breaking one drops no collectible cage, and a replacement at the same position does not inherit the old cage's population. See [Floater spawners](Spawners.md) for dungeon discovery and safe setup.

## Leaving the area and saving

Natural and cage-produced hostiles share the same lifecycle. They stop active movement beyond about **68 blocks** and despawn beyond **112 blocks**. They are also removed when their own chunk unloads, even if it unloads before the distance limit. Removing a cage mob releases its origin's slot. A cage does not keep a distant mob alive.

Save checkpoints preserve currently recorded hostile creatures, cage identities, origins and cooldowns. Loading does not make those creatures permanent: ordinary distance and residency rules still apply. A broken cage stays broken even when its former mobs were alive at save time.

Chickens have a separate persistent lifecycle. They remain in the world when distant or unloaded, and their growth, breeding and egg timers pause. Up to **128 nearby chickens** are active within 68 blocks, with a **4,096-record world limit**. A distant chunk loader alone does not run a chicken farm.

## Natural chickens and breeding

Wild adult chickens use **20–44 blocks from the player**, daylight, surface grass, light **9–15**, full body clearance and the camera-view exclusion. A natural attempt runs every **ten active seconds** after an initial five-second delay, checking up to eight columns. A successful site can add an adult and a nearby companion if both independently qualify.

There must be fewer than **12 chickens within 48 blocks of each proposed site**. Existing farm animals count too, so building a flock can suppress further wild arrivals nearby. This natural-spawn limit is separate from breeding: a fed pair can produce a chick when there is safe space and the world record limit permits it. Breeding does not require grass, daylight or a new wild-spawn distance. Follow the [chicken guide](Chickens.md) for feed, cooldowns, growth and eggs.

## Why is my room empty?

1. Check habitat: Floaters need a covered underground room, not a roofed surface building.
2. Check the entire floor and body volume: use accepted blocks, keep it dry, and provide two clear blocks for a Floater.
3. Check light on the floor. Placed light prevents hostiles; holding a torch does not.
4. For natural spawns, wait 24–48 blocks away, outside the room's clear camera view. Keep the terrain loaded.
5. Consider other caves. The four-natural-Floater allowance is shared, so nearby Floaters can fill it first.
6. For a cage, stay within 36 blocks, leave candidate space around it and allow a full cooldown. Five living mobs from that cage prevent further spawns until a slot is released.

The September 19, 2026 verification capture above uses a constructed review room. Current automated checks exercise shared eligibility, natural spawning, light rejection, separate cage ownership and unloading. These rules are Alpha defaults; they do not claim a measured farm throughput or guaranteed room-filling time.

[Hostile mobs](Mobs.md) · [Floater spawners](Spawners.md) · [Chickens](Chickens.md) · [Home](Home.md)
