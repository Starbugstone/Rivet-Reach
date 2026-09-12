# Wooden doors

Authorized by the user's request for craftable doors with right-click and Blue Signal interaction. The dimensions, material and edge-triggered control below are working implementation choices for this request.

## Craft, place and use

Craft **3 Wooden Doors** at a **3×3 workbench** from **6 planks**, arranged as two adjacent columns of three:

```text
P P .
P P .
P P .
```

The pattern can shift horizontally; the shared recipe also works in a larger compatible grid. No metal, Azure or electricity is needed to craft or manually use a door.

A door reserves **one cell wide and two cells high** above a solid terrain floor. Both cells must be dry, empty, loaded and clear of players and creatures. Placement consumes one door item only after validation. It faces the player's nearest horizontal orientation. Crouch while using an existing door to place an adjacent selected block, following the existing station-placement gesture.

Aim at either half and **right-click / bound Use** or press **Interact** to toggle it. Holding Use does not repeatedly toggle. The door remains targetable while open. Closed doors block both reserved cells; open doors release both for passage. Collision uses the existing voxel occupancy rule, rather than a thin rotating collision mesh. The hinged artwork swings within the reserved footprint. Doors retain their occupied cells for placement and water routing while open; this feature does not add floodgates or waterlogging.

A requested close waits while a player or native creature occupies either cell. It completes on a subsequent simulation step after the doorway clears. Mining either half removes both and recovers exactly one door item. Removing the floor also recovers one door. The upper half is internal terrain state, not a separate item, recipe or Creative-catalog entry.

## Blue Signal

Connect **Signal Conduit** (the blue cable) or supported **Signal Wire** adjacent to any face of the **lower cell**, then connect a lever, button or other Blue Signal source. No power cable or electrical supply is required. Ordinary terrain does not conduct the signal. [Industry](INDUSTRY.md) owns network routing and source behavior.

An **OFF → ON** signal transition requests opening; **ON → OFF** requests closing. Removing a live cable also produces OFF. Right-click remains available between signal changes, including while the input stays ON. A cable first attached while OFF does not override the current manual position. This gives manual access without fighting a continuously reapplied signal. Multiple sources retain the existing network's OR behavior. A button can open the door for its existing pulse duration, with safe deferred closing if someone remains inside.

The existing **Workshop Hatch** keeps its separate item, recipe and signal behavior.

## State and assets

`rivet:wooden_door` is item/anchor **171**; internal upper cell **172** has no item registration. One machine state at the lower cell owns the pair. The signal input uses the shared network topology. `Source` stores the requested opening, `NextSource` the last observed signal and `WorkInput` the physical open flag; these existing serialized fields require no binary layout change. Rendering follows the physical flag, independently of dormant machine status.

[Saves](SAVES.md) preserve both cells, rotation, requested/physical position and last signal. Restore validates paired cells, support and the door state. The content fingerprint permits the additive door definition/recipe to be absent in older checkpoints while retaining checks on every prior definition. This also retains the existing pre-hand-crank compatibility boundary. Chunk dormancy preserves the physical position; either half must be resident for interaction and simulation.

[create_door_assets.py](../Tools/create_door_assets.py) authors the original [WoodenDoor.blend](../ArtSource/Doors/WoodenDoor.blend), FBX and icon using the existing workshop atlas. The two named parts are a static frame and `MotionDoor` leaf. Scale is one metre per cell, with a 1×2×1 footprint and a vertical hinge. Source renders and Unity import/runtime checks belong in [door verification](verification/DOOR_RESULTS.md). Artistic acceptance and wider performance remain subject to play review.
