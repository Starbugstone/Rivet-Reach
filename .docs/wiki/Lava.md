# Lava

Lava forms lakes deep underground in new worlds, just above bedrock. Explore carefully: touching it burns you, and dropped items that touch it are destroyed.

## Finding lava

Naturally generated deep cave pools reach **Y = −240**, sixteen blocks above the bedrock base at Y = −256. Their floors follow the cave shape. Bring blocks to build a safe ledge and an empty [Bucket](Item-bucket.md) before approaching.

![A naturally generated near-bedrock lava lake](images/lava-natural-lake.png)

Existing saves keep their original terrain and do not gain generated lava lakes. Start a new world for the new lakes. You can still place lava from a bucket in an older world, including a bucket obtained through Creative’s item catalog.

## Collecting and placing

Craft an iron [Bucket](Item-bucket.md) at a workbench. Hold it and right-click a lava **source** within reach to obtain a [Lava bucket](Item-lava-bucket.md). Flowing lava cannot be scooped up.

Right-click a block face with the filled bucket to place a lava source. The empty bucket returns to the same slot, so these actions also work with a full inventory. Lava held in a bucket does not burn you or other inventory items.

![The orange-filled lava bucket held in the game](images/lava-bucket-held.png)

## Slow flow, finite sources

Lava advances once per second and spreads up to **three blocks** horizontally on level ground. It prefers downward routes and recedes when its supplying source is removed. Water advances four times as often and reaches seven blocks.

**Lava cannot renew sources.** Putting two sources beside an empty space never creates another source. The renewable 2×2 water-pool arrangement does not work with lava.

![Placed lava spreading across a stone deck](images/lava-flow.png)

Water and lava remain separate when they meet. They do not currently produce stone or other reaction blocks.

## Burning and lost items

- Touching lava immediately costs **two hearts**, then another two hearts every half second. Ordinary armor does not reduce this damage.
- After leaving lava, you remain on fire for **four seconds**, losing half a heart each second. Step into water to extinguish the fire.
- Any dropped stack touching lava is destroyed completely, including items dropped on death. Collect valuables from a safe ledge.
- Creative mode prevents damage and extinguishes fire. Respawn also clears burning.

![Flames mark a burning player while hearts fall](images/lava-burning.png)

Saving preserves remaining fire time, health, placed lava and filled buckets. Pausing or closing the game grants no extra burn time.

A compatible [multiblock tank](Tanks.md) can store lava through its bucket controls, ten litres at a time. Different liquids cannot share its contents. Water pumps, boilers and small water buffers retain their existing water requirements.

These captures are from the September 13, 2026 lava review player. The stone deck is a verification setup; the underground lake is generated terrain.

[All items](Items.md) · [Pumps and water](Pumps-and-water.md) · [Home](Home.md)
