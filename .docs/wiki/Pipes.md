# Connect machines with pipes

Pipes move stored items or water between machines. **Power Cable** supplies electricity. These channels are separate: a plain Item Pipe does not carry electricity or water, and a plain Fluid Pipe does not carry items or power.

Machines accept their supported power, item and fluid connections on **all six faces**, including top and bottom. Pipe runs connect through neighbouring cells, turn corners, branch and climb vertically. They do not connect diagonally.

## Choose a connection

| Connection | Carries | How you control it |
|---|---|---|
| [Item Pipe](Item-item-pipe.md) | Items between supported machines and chests | Wrench sets each machine-facing end to Input or Output |
| [Fluid Pipe](Item-fluid-pipe.md) | Stored water between machines and tanks | Wrench sets each machine-facing end to Input or Output |
| [Power Cable](Item-power-cable.md) | Electricity | Automatic; no direction setting |
| [Signal Wire](Item-signal-wire.md) / [Signal Conduit](Item-signal-conduit.md) | Blue Signal ON/OFF | Separate controls; see [Blue Signal](Blue-Signal.md) |

Signal sockets and boiler/alternator shafts still have their own orientations. The all-face rule applies to electricity and item/fluid connections.

## Craft and equip a wrench

Craft one [Wrench](Item-wrench.md) at a **3×3 Workbench** from **three iron ingots**:

```text
Iron  Iron  ·
  ·   Iron  ·
  ·     ·   ·
```

The mirrored arrangement also works. The personal 2×2 grid cannot craft it. The wrench is reusable, does not stack, and is not consumed when configuring a connection.

Put it in your hotbar and **select it so you are holding it**. A wrench elsewhere in your backpack or hotbar does not enable configuration.

## Set the direction at a machine

1. Place the pipe against any accessible face of a compatible machine or chest.
2. Hold the **Wrench**.
3. Aim at the **pipe end touching that machine**, close to the join.
4. **Right-click** once to reverse that end's direction.
5. Check the arrow and the crosshair's Input/Output label.

| Arrow | Meaning |
|---|---|
| **Blue arrow entering the machine** | **Input**: the machine receives items or water |
| **Red arrow leaving the machine** | **Output**: the machine supplies items or water |

Directions are always named from the **machine's** point of view. A route normally needs a **red output at its source** and a **blue input at its destination**. Both arrows point along the intended movement.

**Without a selected wrench, you cannot change pipe direction.** Empty hands, other items and the Interact key do not change it. Holding right-click does not repeatedly flip it; each change needs another press. If you remap mouse Use, use that binding while holding the wrench.

Each end is independent. Changing one does not reverse the whole network or its other branches. Configured directions survive machine rotation, chunk unloading and Save Game / Load Game.

Pipe-to-pipe joins and loose ends have no machine direction to configure. Power cables have no direction setting. Right-click the **centre** of a pipe, or use Interact, to open its normal interface.

Arrows show the configured direction even when a machine is idle or a tank is empty. A blue input arrow is not an ON Blue Signal and does not power or enable the machine.

## Example: chest → crusher → chest

Bring a [Crusher](Item-crusher.md), two [Chests](Item-chest.md), Item Pipes, a Wrench and an electrical supply.

1. Put raw copper, raw iron or raw gold in the supply chest.
2. Connect that chest to the crusher with Item Pipe.
3. Set the **supply-chest end to red Output** and the **crusher end to blue Input**.
4. Run another pipe from the crusher to the receiving chest.
5. Set the **crusher end to red Output** and the **receiving-chest end to blue Input**.
6. Supply the crusher with **160 W** through a power cable on any free face.

```text
Supply chest       Crusher          Receiving chest
 red OUTPUT ───→ blue INPUT
                   red OUTPUT ───→ blue INPUT
```

One raw ore becomes two matching crushed pieces in five seconds at full power. The output pipe takes finished products, leaving the raw-input buffer alone. Smelt the crushed pieces in a Furnace.

Chest outputs can supply a pipe directly. An [Extractor](Item-extractor.md) remains useful when you want Blue Signal control of extraction: keep its source chest on its left and connect an output pipe on any free face.

Item transport moves up to **four items per second per source inventory**. Adding more output ends to the same inventory does not multiply that limit. Unsupported items remain at the source; a full destination stops delivery without deleting anything.

Current item-pipe endpoints include chests, crusher raw inputs and product outputs, boiler fuel inputs, drill outputs and extractor outputs. An input arrow does not give an output-only device a new processing input. Workbenches and the ordinary Furnace do not currently provide item-pipe endpoints.

## Example: pump → tank → boiler

1. Put a [Pump](Item-pump.md) **above a source-water cell** and supply **80 W** on a free face.
2. Run Fluid Pipe from a free pump face to a [Water Tank](Item-water-tank.md).
3. Set the pump end to **red Output** and the tank end to **blue Input**.
4. Run another Fluid Pipe from the tank to a [Boiler Engine](Item-boiler-engine.md).
5. Set that tank end to **red Output** and the boiler end to **blue Input**.

The pump's world-water intake is still directly below it. A pipe occupying that cell leaves no source to extract. Use another face for the outlet. See [Pumps and renewable water](Pumps-and-water.md) for pool construction and startup.

Fluid pipes move stored quantities, not flowing world blocks. Water is the currently available fluid. Each output end can transfer up to **100 mL per simulation step (2 L/s)**, subject to source contents and destination capacity. Water received by a tank becomes available for forwarding on a later step.

## Connect a multiblock tank

Build a valid [multiblock tank](Tanks.md) and use its functional Tank Fluid Ports or Signal Valve Ports. They share the tank's contents; wall, glass and frame blocks are not fluid endpoints.

Keep the port **Enabled** in its interface, then use a held wrench at the adjoining pipe end to choose Input or Output. A Signal Valve also needs an attached **ON** Blue Signal. The wrench does not bypass a closed valve, an invalid structure or a full tank.

**Recovery Out** at a controller is drain-only, including during a breach. Set its pipe end to red Output. Selecting blue Input does not allow that recovery connection to refill the tank. Drain contents before removing the controller.

## Fit power or Blue Signal to a pipe

Open the pipe interface at its centre or with Interact:

- **FIT POWER** adds a separate electrical channel using one Power Cable.
- **FIT SIGNAL** adds a separate control channel using one Signal Conduit.

Both fittings may share an Item Pipe or Fluid Pipe. Each channel follows its own compatible neighbours. Wrench direction changes affect only item/fluid transport; they do not reverse electricity or change Blue Signal.

## Troubleshooting

| Problem | Check |
|---|---|
| Right-click opens a menu instead of reversing an arrow | Select the Wrench and aim near the machine-facing pipe end, not the pipe centre. |
| There is no arrow | This may be a pipe-to-pipe join, a loose end or an unsupported machine/channel. Move close enough to inspect it. |
| Nothing moves | Use red Output at the source and blue Input at the destination; check contents, accepted items and free destination space. |
| The crusher receives ore but does not process it | Supply electricity; 160 W gives full speed. No attached Blue Signal is needed. |
| A tank valve stays closed | Enable the port, form the tank and supply ON Blue Signal. |
| An input set on a drill receives nothing | The drill produces items; it has no recipe-input buffer. Use its red Output end. |
| Rotating the machine did not reverse the arrows | Expected: configured pipe ends stay fixed. Use the wrench to change them. |
| Two pipe runs touch the same machine but are not connected | Each face is a terminal. Join the pipe runs directly if you want one transport network. |
| The battery has charge but the crusher shows 0 W | Check cable continuity and battery mode. Any crusher face accepts power; leave the battery on Automatic for normal use. |

Related guides: [Wrench recipe](Item-wrench.md), [electricity and batteries](Electricity-and-batteries.md), [pumps](Pumps-and-water.md), [tanks](Tanks.md), [Blue Signal](Blue-Signal.md).
