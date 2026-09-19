# Mobs

Rivet Reach currently has three native creatures. All use the hostile encounter system, although the Rustback beetle warns you before attacking. Passive livestock, including chickens, are planned and are not yet available.

| Creature | Where and when | Behaviour | Health | Hit damage | Defeat drop |
|---|---|---|---:|---:|---|
| [Rustback beetle](Rustback-Beetle.md) | Dark surface terrain | Warns when approached; attacks if you stay or strike it; climbs walls | 12 | 2 | None |
| [Dusk prowler](Dusk-Prowler.md) | Dark surface terrain at night | Pursues a visible player and bites | 18 | 3 | None |
| [Floater](Floater.md) | Dark, covered underground caves, day or night | Hovers above the ground and punches | 16 | 3 | One Floater Rock |

Damage values are before the player's armor. These creatures do not destroy blocks or raid machinery. A wall can obstruct an attack, but beetles can climb supported walls.

## Spawn habitats and ground

Creatures have a surface, underground or combined habitat and a list of blocks they may spawn on. A suitable floor must support the whole creature, with clear space above it. Habitat and ground requirements are shared rules for hostile and future passive creatures; passive animals are not implemented yet.

The current species can spawn on grass, dirt, stone, sand, sandstone, snow or red clay within their habitat. A species can be configured with a narrower rule, such as grass only.

## Lighting prevents new hostile spawns

All current hostile creatures require light level **7 or lower** immediately above their supporting ground. **Level 8 or higher prevents new spawns.** Daylight and nearby placed torches, powered Workshop Lamps and lava contribute to this level. Light must reach the ground; walls can leave dark pockets.

Light the floor of underground rooms to prevent new Floaters there. Existing creatures can still enter a lit area, and lighting does not remove them. Holding a torch only lights the view; place it to protect the surrounding ground. Habitat, support-block and light requirements can be configured independently for future passive creatures too.

![A torch lights a cave floor beside an existing Floater](images/lit-cave-spawn-floor.png)

Verification capture, September 13, 2026: this floor measures level 12 and rejects new natural spawns. An explicitly placed Floater remains alive in the lit room.

## Encounters and combat

New hostile encounters appear around you on supported, loaded terrain, outside the protected area around the initial world spawn. The total ambient population is capped at 14. Night runs from 18:00 until 06:00. Dawn stops new prowler spawns; it does not remove creatures that already exist. Floaters spawn only in sufficiently dark caves, independently of the surface clock.

Aim at a creature and hold the attack/mining button within melee reach. Its name and health bar appear when targeted. Your selected item's attack damage applies; swords deal more damage than bare hands. Attacking a creature takes precedence over mining the block behind it.

Watch for the preparation animation before a bite or punch. Back away or put solid terrain between you and the attack before it lands. Equipment armor and the normal health/death rules apply.

## Persistence

Save/Load retains existing creature identity, health and intent. Distant or unloaded creatures stop thinking and rendering; ambient creatures far enough away can be removed without loot. They are an ambient population, not permanent named pets. Pausing stops their simulation.

[All items](Items.md) · [Home](Home.md)

For persistent passive animals, see [Chickens](Chickens.md). Their growth and egg timers pause when distant; hostile despawning never removes them.
