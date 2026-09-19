# Floater spawners

See [Spawn mechanics](Spawn-mechanics.md) for the complete light, habitat, distance, population and unloading rules.

Rare cobblestone rooms deep underground contain a cage with a miniature [Floater](Floater.md). Most rooms open into caves; a small minority are completely buried. Search newly explored terrain: established chunks keep their original generation.

![A Floater cage operating in a dark room](images/alpha-playtest/floater-spawner-active.png)

![A seeded cobblestone dungeon room with its generated cage](images/alpha-playtest/generated-floater-room.png)

This second view shows a generated room reached at known coordinates for verification. Dungeon discovery rates still need ordinary exploration playtests.

## Keeping a cage active

Stay within **36 blocks**. The cage needs darkness, a suitable floor and enough empty space nearby for a Floater to fit. It attempts a spawn every **10 seconds** while active. Failed attempts wait for another cycle.

Each cage supports up to **five living mobs that came from that cage**. A neighbouring cage has its own five-mob allowance. Ordinary natural hostile population limits do not consume this allowance. Killing or normally despawning one of its mobs frees a slot, subject to the next eligible attempt.

Floaters produced by cages still behave like ordinary Floaters and drop one [Floater Rock](Item-floater-rock.md) when defeated. Walking away does not turn them into permanent residents: normal unloading and despawning rules apply.

## Making the room safe

Place torches around the cage and its floor. Light level **8 or higher** prevents new Floaters; holding a torch only brightens your view. Light does not remove creatures already present.

![Placed light prevents new spawns from this cage](images/alpha-playtest/floater-spawner-torch-disabled.png)

**Breaking a cage destroys it without dropping a collectible spawner.** There is no Survival crafting or collection recipe. Keep a discovered cage intact if you want to use it later.

These screenshots come from the September 19, 2026 Alpha playtest verification room. The fixture demonstrates operation and lighting; it is not a claim that the room shown was discovered through ordinary play.

[Hostile mobs](Mobs.md) · [Lava](Lava.md) · [All items](Items.md) · [Home](Home.md)
