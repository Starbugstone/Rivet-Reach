# Renewable power

[Solar Panels](Item-solar-panel.md) and [Wind Turbines](Item-wind-turbine.md) generate electricity without fuel. Connect them to the same [Power Cables, machines and batteries](Electricity-and-batteries.md) used by your boiler workshop.

## Craft both generators

Use a **Machinist’s Bench (4×4)**. Both recipes are shapeless:

| Generator | Ingredients |
| --- | --- |
| Solar Panel | 1 Machine Casing, 4 Glass, 4 Copper Wire, 2 Gold Ingots, 1 Diamond |
| Wind Turbine | 1 Alternator, 4 Iron Plates, 2 Cogs, 4 Copper Wire, 2 Gold Ingots, 1 Diamond |

Actual Windows-player captures, 19 September 2026:

![Solar Panel ingredients filled at the Machinist's Bench](images/renewables/solar-crafting.png)

![Wind Turbine ingredients filled at the Machinist's Bench](images/renewables/wind-crafting.png)

## Place outdoors and connect cables

Each generator occupies one block. Leave **air directly above** it and an unobstructed path to the sky above solid roofs or terrain. A roof or cave ceiling stops generation. The current sky check follows solid roof coverage; distant transparent leaves and glass do not shade it. There are no elevation bonuses or turbine spacing penalties.

Connect Power Cable on any face. Run one continuous cable grid past your machines and batteries. Optional Blue Signal at the front can switch a generator off. Neither generator accepts fuel or item pipes.

![Solar panel, turbine, lamp and three batteries connected to one cable grid](images/renewables/renewables-clear.png)

## Weather and daylight

| Weather | Solar at noon | Wind, day or night |
| --- | --- | --- |
| Clear | 400 W | 40–140 W |
| Rain | 200 W | 240–360 W |
| Storm | 60 W | 400 W |

Solar rises from zero at **06:00**, peaks at **12:00**, and falls to zero at **18:00**. It produces nothing at night. The table shows noon output; mornings and evenings produce less. Weather changes smoothly, so output can sit between the listed values during a transition.

Wind works through the night, with a small passive baseline and random gusts that rise and fall gradually. New gust targets blend over 12 seconds; storms hold maximum output. Its rotor turns faster in stronger wind and stops when it delivers no power. Charging a battery counts as demand. These are the initial tuning values; the 800 W Boiler Engine/Alternator remains a stronger, steady supply while it has fuel and water.

![Rain reduces solar output and strengthens wind generation](images/renewables/renewables-rain.png)

![Storm weather provides maximum turbine output](images/renewables/renewables-storm.png)

![Wind continues producing at night while solar stops](images/renewables/renewables-night.png)

## Machines first, batteries in parallel

All generator inputs on a **connected cable grid** are added together. Machines take their power first. Remaining generation is divided equally among eligible batteries and formed battery banks. A full battery gives up its share to batteries that still have room; tiny whole-watt leftovers rotate fairly.

For example, at clear noon a **400 W panel** and a turbine currently producing **120 W** provide **520 W**. A **20 W lamp** takes its share first. Three batteries divide the remaining **500 W**, receiving 166 or 167 W each.

**Battery output only serves machines. It never charges another battery**, even in a cable loop. When generation falls short, charged batteries share the machine deficit. Battery banks count as one endpoint; their physical cells retain their stored energy. Separate cable grids remain separate unless their cables join.

![Battery control showing its share of renewable surplus](images/renewables/renewable-battery.png)

## Inspect output and save your workshop

Use/right-click or Interact opens generator controls. **Available** shows the electricity it can provide under current conditions; it is not a count of power actually consumed. Spare output is discarded when neither machines nor storage can use it.

![Solar Panel controls and available output](images/renewables/solar-controls.png)

![Wind Turbine controls and available output](images/renewables/wind-controls.png)

The controls also show **Delivered** power. With full batteries and no machine demand, delivery drops to zero and the turbine rotor stops.

![Idle turbine with full batteries and no machine demand](images/renewables/wind-idle.png)

Save Game retains the placed generators, wiring, battery modes and exact stored energy. Loading recomputes available output from the saved time and weather. Unloaded generators and time spent outside the game produce no electricity. Sleeping changes the time and may change the weather, but does not award energy for the skipped night.
