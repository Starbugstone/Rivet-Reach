# Mobs

Rivet Reach currently has three native creatures. All use the hostile encounter system, although the Rustback beetle warns you before attacking. Passive livestock, including chickens, are planned and are not yet available.

| Creature | Where and when | Behaviour | Health | Hit damage | Defeat drop |
|---|---|---|---:|---:|---|
| [Rustback beetle](Rustback-Beetle.md) | Loaded surface terrain, day or night | Warns when approached; attacks if you stay or strike it; climbs walls | 12 | 2 | None |
| [Dusk prowler](Dusk-Prowler.md) | Loaded surface terrain, nighttime spawns | Pursues a visible player and bites | 18 | 3 | None |
| [Floater](Floater.md) | Loaded surface terrain, nighttime spawns | Hovers above the ground and punches | 16 | 3 | One Floater Rock |

Damage values are before the player's armor. These creatures do not destroy blocks or raid machinery. A wall can obstruct an attack, but beetles can climb supported walls.

## Encounters and combat

New hostile encounters appear around you on supported, loaded terrain, outside the protected area around the initial world spawn. The total ambient population is capped at 14. Night runs from 18:00 until 06:00. Dawn stops new prowler and Floater spawns; it does not remove creatures that already exist.

Aim at a creature and hold the attack/mining button within melee reach. Its name and health bar appear when targeted. Your selected item's attack damage applies; swords deal more damage than bare hands. Attacking a creature takes precedence over mining the block behind it.

Watch for the preparation animation before a bite or punch. Back away or put solid terrain between you and the attack before it lands. Equipment armor and the normal health/death rules apply.

## Persistence

Save/Load retains existing creature identity, health and intent. Distant or unloaded creatures stop thinking and rendering; ambient creatures far enough away can be removed without loot. They are an ambient population, not permanent named pets. Pausing stops their simulation.

[All items](Items.md) · [Home](Home.md)
