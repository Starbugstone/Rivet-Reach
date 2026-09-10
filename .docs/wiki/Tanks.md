# Build a multiblock tank

A multiblock tank is a **sealed hollow box**, with reinforced frames along every edge and corner. Its controller and functional side panels face **outward**. Water is stored inside the machine; do not fill its interior with world water blocks.

This is different from the single-block **Water Tank**, which stores 100 L on its own.

## First build: 3×3×3, 250 litres

Bring these parts:

| Part | Count | Purpose |
|---|---:|---|
| Reinforced Tank Frame | 20 | All edges and corners |
| Tank Wall | 2 | Floor centre and roof centre |
| Tank Controller | 1 | Validation, status and recovery |
| Tank Access Hatch | 1 | Convenient bucket access |
| Tank Fluid Port | 2 | One input and one output |

The controller also supports buckets, so a hatch is optional. You may replace unused functional side panels with Tank Wall or Reinforced Tank Glass. Keep exactly one controller.

These are top views, with the front nearest the bottom of each diagram. `F` = frame, `W` = wall, `C` = controller, `H` = hatch, `I/O` = fluid ports, `.` = empty air.

**1. Build the bottom layer.**

```text
F F F
F W F
F F F
```

**2. Build the middle layer. Leave the centre empty.**

```text
F O F
I . H
F C F
  ↓ visible controller face points outside
```

Stand outside the box when placing each functional part. The visible panel should face you. Its pipe nozzle belongs on the outside, with empty interior behind it. If a part faces inward, open it and rotate it in 90° steps.

**3. Close the roof.**

```text
F F F
F W F
F F F
```

**4. Open the controller.** It should say **FORMED · 3 × 3 × 3**, with capacity **250 L**. Validation also runs automatically after nearby edits. RE-SCAN requests another check.

**5. Fill it.** Bring a Water Bucket and select ADD 10 L at the controller or hatch. For pipes, set the inlet to INPUT, attach Fluid Pipe to its front nozzle, and connect a water source machine. Set the other port to OUTPUT and connect a destination that can accept water.

## Larger tanks

Each outer dimension can be **3 through 9 blocks**. The dimensions may differ: a 6×4×5 tank is valid and stores 6,000 L.

Capacity is:

```text
(width − 2) × (height − 2) × (depth − 2) × 250 L
```

Every edge and corner uses frame blocks. Floor and roof face centres use solid Tank Wall. Side face centres may use walls, glass or functional tank parts. The entire interior must be empty air. A larger solid pile is not a tank.

## Valves and level sensors

A Signal Valve Port replaces an ordinary side port. Configure its fluid direction and connect Blue Signal to the keyed signal fitting. **An attached ON signal opens it; missing or OFF signal keeps it shut.** Fluid and signal can share a fitted Fluid Pipe while remaining separate channels.

A Tank Level Sensor outputs Blue Signal when the stored percentage reaches its threshold. Adjust that threshold in its interface. See [Blue Signal](Blue-Signal.md).

## When the controller says invalid

| Message or symptom | What to check |
|---|---|
| Interior must be empty behind controller | Remove an interior block or rotate the controller outward. |
| Missing Reinforced Tank Frame | Every edge and corner needs the frame item. |
| Floor and roof require solid Tank Wall | Replace glass or functional panels in those face centres. |
| Rotate component to face outside | Rotate the named controller, hatch, port, valve or sensor. |
| Interior open / shell incomplete | Close missing shell cells; keep the inside hollow. |
| Waiting for neighbouring chunk | Approach the entire structure so its terrain is resident. |
| Formed, but no fluid arrives | Check INPUT mode, an actual source of stored fluid, and valve signal. Power Cable does not carry water. |

The interface identifies the failed coordinate and highlights it in the world.

## Repairs and dismantling

Breaking the shell suspends normal transfers. Stored liquid stays with the controller. Repair the shell to restore it; never mine a full controller.

To dismantle, first take liquid out with buckets at the controller, or enable its **RECOVERY OUT** and connect the front outlet to a receiving tank. Recovery is available for a formed or invalid structure. The controller can be mined only when empty. Shrinking a tank below its stored amount requires draining first.

A bank of batteries uses a different, **solid** construction: [battery-bank guide](Electricity-and-batteries.md).
