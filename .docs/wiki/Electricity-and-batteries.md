# Electricity, batteries and battery banks

Electricity travels through **Power Cable**, including power fittings added to item or fluid pipes. Blue Signal is a separate control channel and cannot power machinery.

## Start generating

Place a Boiler Engine and an Alternator facing the same direction, with the alternator immediately on the boiler’s **right**. Their shafts must meet. Add coal or charcoal and water to the boiler through its interface. The coupled alternator supplies **400 W** from its rear socket.

Run Power Cable from that rear socket to a machine’s power input. The Pump needs 80 W, Crusher 160 W, Drill 240 W and Workshop Lamp 20 W. When demand exceeds supply, priorities apply first, then equal-priority loads share available power and work more slowly.

## Standalone Battery Block

A Battery Block stores **100 kJ** and can charge or discharge at up to **400 W**. It starts **empty**, including when placed in Creative. Connect a cable to any face.

Surplus generation charges it after current machines receive power. When generation falls below demand, a charged battery supplies the shortfall. An idle circuit does not drain it, and batteries do not transfer charge to each other automatically.

Open the battery to see stored energy, actual input/output watts and its mode:

| Mode | Behaviour |
|---|---|
| Automatic | Charge from generation surplus; discharge for demand. |
| ChargeOnly | Accept surplus, never supply loads. |
| DischargeOnly | Supply loads, never accept charge. |
| Isolated | Keep its charge without transferring electricity. |

At 400 W of uninterrupted surplus, an empty cell takes 250 seconds to fill. A full cell can run a lone 20 W lamp for 5,000 seconds. Actual duration depends on connected demand.

## Build a battery bank

Use **Battery Blocks plus exactly one Battery Bank Controller** to make a **filled rectangular pack**. This is a solid construction, unlike a hollow tank. Each dimension may be 1 through 5 blocks; the pack must include at least one battery.

The smallest bank is a controller with one battery behind it:

```text
Top view

B  battery
C  controller
↓  visible front and power socket face outside
```

A useful 2×1×2 pack contains three cells and one controller:

```text
B B
C B
↓ front
```

This stores **300 kJ** and can transfer up to **1,200 W** in either direction. The controller provides no storage by itself.

1. Place the controller from outside, facing you.
2. Fill every remaining cell of the chosen rectangular pack with Battery Blocks. No gaps, hollow centres, diagonal-only attachments or other block types.
3. Open the controller and check FORMED dimensions in the details.
4. Connect Power Cable to the controller’s **front** socket.
5. Charge from a running generator. The controller reports combined energy and charge/output watts.

While formed, the bank’s controller and mode manage all member cells; their individual sockets are inactive. A cell’s interface shows that it belongs to a bank. Keep separate packs from touching: leave an air gap so each connected pack has only one controller.

## Repair, enlarge or dismantle

Charge stays in each battery cell. Removing an empty cell may invalidate the rectangle. Invalid banks stop using the controller; unclaimed cells return to standalone operation through their own sockets, so attached individual cables may still operate them.

Repair the pack or add cells to make a larger filled rectangle. Its combined capacity and transfer limit update after validation. Adding an empty cell never creates charge.

A charged battery cell cannot be mined. Discharge it into a real load first; use DischargeOnly to prevent replenishment. The controller can be removed because it owns no charge. The remaining cells keep their exact energy and return to standalone operation.

Unloaded cells contribute no power; the bank waits until the required terrain returns. There is no offline charging, and stored energy currently lasts only for the running world session.

## Crafting

Both assemblies are made at the Machinist’s Bench. Inspect them in the item browser for the recipe:

- Battery Block: 1 Machine Casing, 2 Copper Plate, 4 Copper Wire and 2 Coal.
- Battery Bank Controller: 1 Machine Casing, 4 Copper Wire, 1 Azure Crystal and 1 Glass.

The current capacity and rate are working gameplay values and may change during balancing.
