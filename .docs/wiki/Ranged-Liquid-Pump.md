# Ranged Liquid Pump

Use the [Ranged Liquid Pump](Item-ranged-liquid-pump.md) to drain finite liquid deposits such as [lava](Lava.md). It collects sources up to **eight blocks away along each axis**: a **17×17×17 cube** centered on the pump, including corners, above and below. Solid blocks between the pump and the source do not block collection.

## Crafting

Combine **one [Pump](Item-pump.md) and one [Floater Rock](Item-floater-rock.md)** at the **[Machinist's Bench](Item-machinist-bench.md)**. This shapeless recipe makes one Ranged Liquid Pump. Defeat a [Floater](Floater.md) to obtain its rock.

## Setup

1. Place the pump within eight blocks of your liquid sources. It needs no electricity or fuel.
2. Connect a [Fluid Pipe](Item-fluid-pipe.md) to any pump face and lead it to a [Water Tank](Item-water-tank.md) or [multiblock tank](Tanks.md). Despite its name, the small Water Tank can hold lava.
3. Hold a [Wrench](Item-wrench.md). Set the pump-facing pipe end to **Output** (red arrow out of the pump), and the tank-facing end to **Input** (blue arrow into the tank). See [Pipes](Pipes.md).
4. Right-click the pump or use Interact to inspect its progress and stored liquid. Each collected source gives **10 L** after two seconds of collection; source searches can add a short delay.

![Ranged pump beside a finite lava source pool](images/ranged-pump-lava-pool.png)

## Operation

The internal buffer holds **10 L of one liquid**. A full buffer pauses collection until pipes or an empty bucket drain it. Flowing liquid is ignored. Lava sources disappear as they are collected; the pump does not make them renewable. Water follows its normal source-renewal rules.

![Pump controls showing the collected lava buffer](images/ranged-pump-lava-buffer.png)

An optional front Blue Signal connection enables the pump only while ON. With no signal attached, it is enabled. The pump operates in loaded terrain and does not keep distant chunks active by itself. Turning it changes the front signal connection, while the collection area stays fixed around its position.

![Drained pool with fluid pipe and receiving tank](images/ranged-pump-drained-pool.png)

![Receiving tank showing the collected lava](images/ranged-pump-destination.png)

If collection stops, check the buffer, signal, loaded terrain and remaining **source** blocks inside the area. A retained liquid prevents mixing: empty the buffer completely before collecting a different type. Conflicting liquids in a connected pipe network also stop transfers. Drain the pump before mining if you want to retain its liquid.

Save Game keeps the buffer, partial collection work and drained sources. No production occurs while the world is closed.

*Screenshots: native Windows review captured 2026-09-13; staged lava pool used to demonstrate source removal and exact storage.*
