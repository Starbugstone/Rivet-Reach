# Pumps and renewable water

A Pump draws from the **water source cell directly below it**. It requires **80 W** and takes two seconds at full power to move one source block into its 10 L output buffer. It stops when that buffer is full, power is absent, or an attached Blue Signal is OFF.

The pump does not create water. Ordinary water simulation can renew a source without any pump or electricity.

## Build a small renewable pool

1. Dig a 2×2 basin, one block deep, with a solid floor beneath all four cells.
2. Empty Water Buckets into two opposite corners. Allow water to update: all four cells become sources.
3. Place the Pump **one cell above** one corner of the pool, leaving the water cell underneath.
4. Connect Power Cable to the pump’s rear and Fluid Pipe to its right-hand outlet. Connect that fluid route to a tank INPUT or another accepting machine.

```text
Side view                     Top view: water layer

       P  ← pump cell          S S
       S  ← source below       S S
████████  ← solid basin floor

P is above S; they are separate cells.
```

To place the pump over the water, use the side of a temporary block at the pump’s intended height, then remove that support. **Clicking the bottom of the basin places the pump inside the water layer.** That replaces a water cell and leaves solid ground directly below the intake.

Open the pump to check **Below: water source**. “Blocked / not a source” means the intake height or block is wrong. Flowing water is not a source.

## Why does water return with no power?

A missing water cell renews when two horizontal neighbours are sources and it has a solid floor or another source beneath it. This is natural fluid behaviour. A correctly positioned, unpowered pump leaves the source intact; if a bucket removes that source, the pool may refill it independently.

Water cannot renew through a machine or solid ground. If the pump occupies the fourth square of your pool and the block under it is sandstone, waiting or adding power cannot turn that sandstone into water. Move the pump up one cell.

## Power and connection checks

- **No electrical power:** attach a running generator or a charged [battery](Electricity-and-batteries.md) to the rear power socket. Blue Signal does not supply energy.
- **Disabled by signal:** turn the connected signal ON, or remove that optional control connection.
- **Output full:** connect an accepting tank or take out a 10 L bucket.
- **Waiting for source water:** check the cell directly below, not merely adjacent water.
- **Underpowered:** the pump works proportionally more slowly. With zero watts, it makes no progress and extracts no water.

The boiler needs water before it can drive an alternator. Fill it manually with buckets for startup, then use the powered pump to supply more water.
