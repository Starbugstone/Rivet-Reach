# Rivet Reach - Lore and World Rules

> **Status:** evolving design / internal lore bible
>
> This document defines the hidden rules that make Rivet Reach's worlds coherent. Most of this lore should **not be directly explained to the player**. Rivet Reach should use Minecraft-style environmental storytelling: the player sees villages, ruins, machines, creatures and impossible structures, then forms their own story from what they discover.
>
> These ideas will continue to evolve while we brainstorm, build the POC and test what produces the best world and gameplay.

---

## 1. Lore philosophy

Rivet Reach should not make the player the chosen hero of a predefined story.

The player appears in the world and is free to:

- explore;
- survive;
- build;
- industrialise;
- discover technology;
- establish settlements;
- leave everything behind and travel;
- create their own story.

There should be very little mandatory exposition, no long lore dumps and no NPC whose job is to explain the universe.

The world should instead create questions:

- Who built these ruins?
- Why are similar structures present on other planets?
- Where do the ancient gateways actually go?
- Why does Gatebuilder technology behave differently from our own?
- What happened to the civilisation that built all of this?

Some of those questions may never receive a definitive answer.

---

## 2. Hidden premise

The universe is old.

The current civilisation on the starting planet is relatively young and technologically simple. Small communities exist, but there is no planet-wide modern civilisation controlling the world.

Long before the current inhabitants, an unknown technological civilisation spread through the local planetary system.

Internally we currently refer to them as the **Gatebuilders**. This is a development term and does not have to become their final in-game name.

The Gatebuilders:

- colonised the local planetary system;
- constructed facilities and settlements on multiple physical worlds;
- created a distributed network of gateway structures;
- used technology far beyond the starting civilisation;
- accessed strange realms outside normal physical space;
- disappeared before the present era;
- left damaged, buried and partially functioning infrastructure behind.

We deliberately do **not** currently define why they disappeared.

---

## 3. Current civilisation

The starting world contains simple living settlements rather than a large scripted civilisation.

Possible settlements include:

- villages;
- farms;
- workshops;
- small mines;
- storage buildings;
- roads/paths;
- biome-specific variants later.

Village placement should make geographical sense where practical. Settlements can prefer useful locations such as:

- river valleys;
- fertile plains;
- coastlines;
- accessible terrain;
- useful nearby resources.

Villagers should not be walking encyclopaedias. They may use, trade or reuse things they do not fully understand.

A village wall might even contain a strange ancient block salvaged from a ruin simply because the inhabitants found it strong and useful.

This allows the player to recognise historical connections later without dialogue explaining them.

---

## 4. Layers of history in world generation

Generated structures should reflect different periods rather than all belonging to one generic structure pool.

A normal world can contain at least three broad historical layers:

```text
Current civilisation
|- villages
|- farms
|- workshops
|- active mines
|- roads/paths

Recent/ordinary ruins
|- abandoned houses
|- collapsed mines
|- old workshops
|- forgotten settlements

Gatebuilder remains
|- strange foundations
|- buried chambers
|- technological ruins
|- larger complexes
|- gateway sites
```

The exact history simulation does not need to be complicated, but generation should know **what category a structure belongs to and why it is there**.

Empty space is also important. Not every cave, biome or region needs a special ruin. Ancient structures stop feeling ancient if one appears every few minutes.

---

## 5. Gatebuilder presence across the system

The Gatebuilders did not build only one gateway on a planet. They created sparse planetary infrastructure.

Their gateway sites therefore form **large, widely separated networks** across procedurally generated worlds.

Important lore consequences:

- multiple Gatebuilder sites exist on the starting world;
- they continue to appear as the effectively infinite world expands;
- sites are intentionally very far apart;
- distant players can find local gateway infrastructure without returning to spawn;
- gateway distribution is not centred on the player spawn;
- additional physical planets also contain Gatebuilder remains because the Gatebuilders colonised the system;
- Gatebuilder structure density can vary by planet, revealing how important a world may once have been.

A heavily colonised world may contain considerably more remains than a remote outpost world.

The player is not told this. They infer it by eventually discovering similar architecture on other planets.

---

## 6. The gateway realms

Gatebuilder gateways do **not** function like player-built interplanetary teleporters.

They connect physical worlds to strange realms that do not appear to belong to normal physical space.

We intentionally do not need to decide whether these places are:

- alternate dimensions;
- pocket universes;
- artificial spaces;
- parallel realities;
- locations outside conventional spacetime;
- something else entirely.

The mystery is useful.

What matters is that the rules are consistent:

- rockets cannot reach portal realms;
- player-built normal teleporters cannot connect to portal realms;
- automated inter-world resource logistics cannot pass through them;
- the player must personally travel through Gatebuilder gateways;
- portal realms remain exploration spaces even in the late game.

This creates a permanent distinction:

```text
Physical worlds
explore -> colonise -> automate -> optimise

Portal realms
find -> enter -> explore personally -> return with discoveries
```

---

## 7. Gatebuilder technology is not ordinary electricity

Gatebuilder gateways should not simply be another electrical machine.

Their technology is technological rather than explicitly magical, but it uses a principle the current civilisation does not initially understand.

Development terms such as **resonance**, **phase coupling** or simply **Gate energy** may be used internally until the final lore is decided.

A useful hidden rule is:

> Gatebuilder machinery establishes and stabilises a relationship between matching structures across worlds. Once a gateway pair has been successfully linked, the Gate network itself sustains the connection rather than drawing conventional electrical power for every traversal.

Consequences:

- connecting a normal generator to a Gate should not simply power it;
- restoring a Gate is about repairing/replacing ancient systems and components;
- travel through an operational Gate has **no consumable or energy toll**;
- repeated back-and-forth exploration must remain convenient;
- one healthy endpoint can maintain a link when the opposite endpoint is damaged;
- player-built teleportation remains a separate technology with different rules.

The player does not need to receive this explanation directly.

---

## 8. Gate ruins and condition

Gate sites should be generated as ruins with variable condition.

Two instances of the same gateway network do not need identical damage.

Possible conditions include:

- mostly intact;
- missing modules;
- damaged stabilisation systems;
- buried or overgrown machinery;
- broken control pedestal;
- partially collapsed surrounding complex;
- heavily damaged receiving side.

A Gate's **destination/network** and its **physical condition** are separate concepts.

A gateway leading to the same realm may be easy to restore at one location and heavily damaged at another.

This allows procedural luck and exploration knowledge to create different player stories without arbitrary technology-level locks.

---

## 9. Activation philosophy

Gateway activation is still an open design area, but several rules are established.

Activation should:

- use physical clues rather than a level requirement;
- involve repairing or supplying missing Gatebuilder components/systems;
- allow different degrees of ruin/difficulty;
- be understandable through the common interaction language of the game;
- not require a consumable fee for every traversal;
- never permanently strand a player on the receiving side.

A knowledgeable/lucky player may be able to restore a Gate unusually early. The game should generally allow this rather than displaying `Requires Industrial Age`.

The difficulty should come from finding the structure, obtaining the right components and understanding what is missing.

---

## 10. Gate control pedestals

Each major gateway should have a readable Gatebuilder control pedestal or equivalent local control structure.

The pedestal is the main diegetic way to communicate Gate state without large text boxes.

It should communicate through consistent visual conventions such as:

- empty shape = missing component;
- filled/illuminated shape = component present;
- dark = inactive;
- lit = active;
- broken/flickering segment = damaged;
- filling ring/dial = recovery or stabilisation progress;
- paired symbols = local and remote endpoint status.

For example, a player should be able to infer:

```text
LOCAL         REMOTE

  active ----- damaged
```

without needing a paragraph explaining it.

Detailed inspection information can exist for players who want it, but the basic state must remain visually understandable.

The pedestal should look unmistakably Gatebuilder-made rather than like one of the player's industrial control panels.

---

## 11. Damaged receiving endpoints

An operational source Gate can connect to a damaged receiving Gate.

The player must always be able to return eventually, but a damaged receiving side can create a temporary inconvenience.

Current preferred concept:

```text
healthy endpoint
-> immediate/faster reuse

damaged endpoint
-> short stabilisation/recovery cooldown
-> visibly shown on the pedestal
-> Gate becomes usable again automatically
```

Repairing the damaged side removes or substantially reduces the cooldown.

This provides a meaningful benefit for repairing both endpoints without turning the first journey into a trap or resource grind.

Exact cooldown values will be balanced later and should remain short enough that exploration does not become tedious.

---

## 12. Gate networks and geography

The Gatebuilders created multiple gateway **networks**, not isolated one-off portals.

A network can have many Gate sites distributed across a physical world and the corresponding portal realm.

The save seed determines the shared Gate anchors so matching Gate sites exist at corresponding horizontal coordinates in linked worlds.

This means a player can:

```text
Starting world
Gate A at coordinate X/Z
        |
        v
Portal realm
Gate A at the same X/Z
        |
        | explore normally
        v
find Gate B at another X/Z
        |
        v
activate Gate B
        |
        v
Starting world
Gate B at that corresponding X/Z
```

This is intentional.

The Gate realm therefore has real geography rather than being a collection of disconnected dungeon entrances.

Distance is preserved. A player who settles extremely far from other players remains extremely far from them in the corresponding portal realm.

The Gate network must **not** become a compressed-distance fast-travel system like Minecraft's Nether coordinate scaling.

More implementation detail lives in [TRANSPORT.md](TRANSPORT.md).

---

## 13. Environmental clues around Gatebuilder sites

Gate sites can have subtle regional clues, but should not automatically reveal themselves on a map.

Possible clues include:

- smaller satellite ruins;
- unusual ancient blocks exposed in caves;
- broken pylons or foundations;
- unnatural geometric terrain details;
- reused ancient material in nearby settlements;
- uncommon vegetation or environmental patterns where appropriate.

These should reward observant exploration without turning into giant glowing quest markers.

The current satellite concept is **cartography only**, not ancient-structure detection.

---

## 14. Physical planets and ancient history

Physical planets are places the player can eventually colonise and automate.

The Gatebuilders reached them before the player.

Finding their ruins on another world should be an important environmental story beat:

```text
land on new planet
-> explore geology/resources
-> find something artificial
-> recognise Gatebuilder architecture
```

Different planets can tell different parts of their history purely through:

- ruin density;
- ruin scale;
- preservation;
- gateway density;
- abandoned infrastructure;
- absence of structures in some regions/worlds.

No narration is required.

---

## 15. Creatures and mobs

The mob system should initially remain readable and mechanically simple.

Most creatures are simply native life. Ancient technology should **not** become the explanation for every hostile creature in existence.

A small set of reusable behaviour families is preferable to dozens of expensive unique AI systems.

### Passive roles

Working concepts:

- **Grazer** - herd animal; food/hide/fibre-type resources.
- **Rooter** - pig/boar-like forager; interacts with ground/vegetation.
- **Fowl** - small farmable bird role; food/egg/feather-type resources.

These are placeholder gameplay names, not final creature designs.

### Neutral role

A territorial creature can remain peaceful until the player ignores its warning range, then charge/defend itself.

### Hostile roles

Working behavioural concepts:

- **Lurker** - straightforward night/darkness melee predator;
- **Spitter** - ranged hostile with readable projectiles;
- **Burrower** - cave/underground threat that makes mining less completely safe.

### Rare anomalous creatures

Very rare creatures near ancient sites or inside portal realms may feel deliberately "wrong" compared with the normal ecosystem.

This should be subtle. Not every hostile mob is a Gatebuilder experiment or dimensional invader.

---

## 16. Portal-realm ecology

Portal realms should allow considerably stranger world and creature designs than normal planets.

Possible directions include:

- unfamiliar vegetation;
- unusual skies and lighting;
- strange terrain formations;
- impossible-looking geology;
- realm-specific creatures;
- unique structures;
- resources acquired through exploration, formations, structures or creatures.

Portal resources should not all reduce to `place automated miner and leave forever`.

The player must retain a reason to physically return.

---

## 17. Mobs and gateways

Gateways are physical passages for living entities, not player-only menu transitions.

Mobs may pass through an active gateway when they physically enter it.

This allows situations such as:

```text
player flees through Gate
-> hostile mob follows
-> both arrive in destination realm
```

It also means players can potentially move livestock or other entities through Gate networks manually.

The same entity should be transferred, preserving its relevant state rather than destroying it and spawning a generic replacement.

Transport/chunk behaviour is defined in [TRANSPORT.md](TRANSPORT.md).

---

## 18. Things the lore should not become

Avoid drifting into:

- chosen-one narratives;
- mandatory quest chains explaining the setting;
- ancient-civilisation exposition dumps;
- every monster being caused by the Gatebuilders;
- every ruin containing major loot;
- every region containing a mystery;
- portals becoming ordinary player-crafted blocks;
- Gatebuilder technology becoming merely another voltage tier;
- portal realms becoming automated remote mining colonies.

The hidden lore exists to make procedural generation and gameplay coherent, not to take ownership of the player's story.

---

## 19. Current internal canon

Unless deliberately changed later, the current core lore rules are:

1. The current civilisation is relatively simple and fragmented.
2. The Gatebuilders colonised the local physical planetary system before the present civilisation.
3. Gatebuilder ruins therefore exist on multiple planets.
4. The Gatebuilders created sparse, widely separated gateway networks rather than one unique portal per world.
5. Gateway networks connect to strange non-physical portal realms.
6. Gate coordinates are shared across linked worlds so the network has consistent geography.
7. Gatebuilder gateway technology is not ordinary player electricity.
8. Once established, normal Gate travel does not consume resources or energy per traversal.
9. Gate endpoints can be damaged independently.
10. One sufficiently operational endpoint can sustain a linked pair.
11. A damaged receiving side may impose a short visible recovery cooldown but cannot permanently strand the player.
12. Repairing both sides provides convenience/performance benefits.
13. Gatebuilder control pedestals communicate state primarily through diegetic visual language.
14. Gatebuilder gateways cannot be constructed by the player.
15. Player-built teleporters are a different late-game technology and do not work in portal realms.
16. Portal realms cannot be converted into unattended inter-world resource farms.
17. Most normal creatures are native life and unrelated to the Gatebuilders.
18. The reason the Gatebuilders disappeared remains intentionally undefined.

---

## 20. Open lore/design questions

Still intentionally unresolved:

- final name/identity of the Gatebuilders;
- what actually happened to them;
- whether they originated in the local system or arrived from elsewhere;
- what the portal realms fundamentally are;
- final number and identity of gateway networks/realm types;
- exact Gate activation/repair puzzle;
- what Gatebuilder components are and how substitutes/replacements work;
- how much of Gate technology the player can eventually understand;
- exact structure hierarchy and ruin archetypes;
- detailed village cultures/architecture;
- final mob designs and names;
- portal-realm ecosystems;
- whether some portal realms link to more than one physical planet without undermining rocket progression.

These should evolve through brainstorming and POC/playtesting rather than being locked prematurely.
