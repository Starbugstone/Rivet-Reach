# Electricity, batteries and battery banks

Electricity is what turns a collection of machines into a working workshop. Start with a simple **Boiler Engine + Alternator**, connect one machine with **Power Cable**, and only add batteries once that basic circuit works.

> **Power, Blue Signal, items and water are separate networks.** Power Cable carries electricity. It does not carry water, items or Blue Signal.

## What a working power system looks like

This is an actual in-game Rivet Reach workshop. The Boiler Engine drives the Alternator mechanically, and the Alternator feeds the electrical network through Power Cable.

![Boiler Engine and Alternator in-game](https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/.docs/verification/machinery-05-steam-and-steel-2026-09-10.png)

## Start here: make your first power network

For the first test, keep it small. You need:

| What to bring | What it does |
|---|---|
| **1 Boiler Engine** | Burns Coal or Charcoal and turns fuel + water into mechanical power |
| **1 Alternator** | Converts the Boiler Engine's shaft power into up to **400 W** of electricity |
| **Power Cable** | Carries electricity between the Alternator, batteries and machines |
| **Coal or Charcoal** | Fuel for the Boiler Engine |
| **Water** | The Boiler Engine consumes up to **100 mL/s** while running |
| **1 powered machine** | Something simple to prove the circuit works; a Workshop Lamp is ideal |

### 1. Place the Boiler Engine

Place the Boiler Engine where you have room to reach its interfaces and route water to it.

A single Coal or Charcoal item keeps the boiler running for **80 eligible seconds**, provided it also has water.

### 2. Place the Alternator on the boiler's right

The Boiler Engine and Alternator must face the **same direction**. Place the Alternator immediately on the Boiler Engine's **right-hand side** so their mechanical shafts meet.

If the shaft connection is wrong, rotating the machines changes all their ports together.

![Boiler, Alternator and Crusher in-game](https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/.docs/verification/industry-workshop-close.png)

### 3. Add fuel and water

Open the Boiler Engine and add Coal or Charcoal. Supply water through its interface or fluid connection.

A boiler with fuel but no water cannot drive the Alternator.

### 4. Connect the electrical output

The Alternator's electrical socket is on its **rear**. Run Power Cable from that socket to the power input of your machine.

For a very easy first test, connect a **Workshop Lamp**. It only needs **20 W**, so a single 400 W Alternator has plenty of headroom.

### 5. Check the machine

Open the machine interface. It reports the power it is requesting and the power it is actually receiving.

If the machine runs, you now have a working electrical network.

## How much power do machines need?

A correctly coupled Boiler Engine + Alternator can supply up to **400 W**.

| Machine | Full-power demand |
|---|---:|
| Workshop Lamp | **20 W** |
| Pump | **80 W** |
| Crusher | **160 W** |
| Drill | **240 W** |

You can run several machines on the same network as long as generation can cover their combined demand.

For example, a Drill + Crusher requests exactly **400 W**. Adding a Lamp at the same time would raise demand to **420 W**, which is more than one Alternator can provide.

### What happens when demand is too high?

Machines have **High / Normal / Low** power priority. Higher-priority loads are served first. Machines at the same priority share the available whole watts fairly.

Underpowered processing machines do not instantly fail or lose their inputs: they work more slowly in proportion to the power they receive.

| Electrical situation | What happens |
|---|---|
| Generation is greater than demand | Machines receive power first; remaining surplus can charge batteries |
| Generation matches demand | Machines run normally; there is no surplus to store |
| Demand is greater than generation | Charged batteries can supply the shortfall |
| Demand still exceeds generation + battery output | Priority applies, then equal-priority loads share the available power |

## Add a standalone Battery Block

A single Battery Block is the easiest way to add energy storage before building a larger bank.

<img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/168.png" width="96">

A Battery Block:

- stores **100 kJ**;
- charges or discharges at up to **400 W**;
- starts **empty**, including when placed in Creative;
- can connect to Power Cable on **any face**;
- does not lose charge while simply sitting idle.

Connect it to the same electrical network as your generator and machines.

> **Machines are supplied before batteries charge.** A Battery Block only takes generator power that is left over after the current loads have been served.

When generation later falls below demand, a charged battery automatically supplies the missing power.

At a continuous **400 W** of surplus, an empty 100 kJ Battery Block takes **250 seconds** to fill. A completely full block could power a lone 20 W Workshop Lamp for **5,000 seconds**.

## Battery modes

Open a Battery Block to change how it behaves.

| Mode | Behaviour |
|---|---|
| **Automatic** | Charges from surplus generation and discharges when the network needs power |
| **ChargeOnly** | Can charge, but never supplies connected loads |
| **DischargeOnly** | Can supply loads, but will not recharge |
| **Isolated** | Keeps its stored energy and does not exchange power |

For normal workshop use, leave the battery on **Automatic**.

**DischargeOnly** is especially useful when you want to empty a battery before dismantling it.

## Build a battery bank

When 100 kJ is no longer enough, Battery Blocks can be combined with a **Battery Bank Controller** into one larger storage system.

This actual in-game bank is supplying a Workshop Lamp after generation has stopped:

![Battery bank powering a lamp after generation stops](https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/.docs/verification/workshop-battery-bank-powered-lamp-2026-09-10.png)

### Blocks to bring

| In-game block | What it does |
|---|---|
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/168.png" width="72"><br>**Battery Block** | Stores **100 kJ** and contributes up to **400 W** of bank transfer capacity |
| <img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/169.png" width="72"><br>**Battery Bank Controller** | Forms and controls the bank; the controller itself stores **no energy** |

A valid battery bank is very different from a multiblock tank:

> **A battery bank is a completely filled solid rectangular pack. Do not leave an empty interior.**

Each outer dimension may be from **1 to 5 blocks**, and the finished rectangle must contain **exactly one controller** and at least one Battery Block.

### Start with the smallest bank

The easiest bank is just one controller and one battery:

```text
Top view

B C

B = Battery Block
C = Battery Bank Controller
```

Place the controller from outside so its **front power socket faces outward**. Fill the rest of the chosen rectangle with Battery Blocks.

### A useful small bank: 2×1×2

A compact 2×1×2 bank can contain one controller and three Battery Blocks:

```text
Top view

B B
C B

C front faces outward
```

With three battery cells, this bank stores:

- **300 kJ** total energy;
- up to **1,200 W** charge/discharge rate.

The controller contributes control and the external socket, but **zero storage capacity**.

### Form the bank

1. Place the **Battery Bank Controller** from outside, with its front face accessible.
2. Fill every other position in the rectangular pack with **Battery Blocks**.
3. Leave **no gaps**, hollow spaces or unrelated blocks inside the rectangle.
4. Open the controller and check that it reports **FORMED** and the expected dimensions.
5. Connect Power Cable to the controller's **front socket**.
6. Run your generator and let surplus power charge the bank.

The controller shows the combined stored energy and the current charge/output rate:

![Battery bank controller interface](https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/.docs/verification/workshop-battery-bank-control-2026-09-10.png)

## How battery banks behave

Once the bank forms, the controller owns the electrical connection and operating mode for the whole pack.

The individual Battery Block sockets become inactive while those cells belong to the bank. Opening a member cell shows that it has been claimed by a bank.

Each Battery Block keeps its own exact stored energy internally, so forming or dismantling a bank does **not** create, destroy or redistribute charge.

### Keep separate banks apart

Do not build two separate battery banks directly touching each other. Leave an **air gap** between packs so each connected rectangular group contains only one controller.

## Crafting batteries

Both battery assemblies are made at the **Machinist's Bench**.

### Battery Block ×1

<img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/168.png" width="96">

Craft with:

- **1 Machine Casing**
- **2 Copper Plate**
- **4 Copper Wire**
- **2 Coal**

### Battery Bank Controller ×1

<img src="https://raw.githubusercontent.com/Starbugstone/Rivet-Reach/main/Assets/RivetReach/Resources/Industry/Icons/169.png" width="96">

Craft with:

- **1 Machine Casing**
- **4 Copper Wire**
- **1 Azure Crystal**
- **1 Glass**

The item sidebar in the game also shows these recipes and their upstream materials.

## Scaling up your electrical system

Once the basic generator → cable → machine → battery setup works, scale it in stages rather than building a large network all at once.

### Add more storage when

- your machines regularly outlive the current fuel cycle;
- demand briefly exceeds generation;
- you want lights or important machines to keep working after a boiler stops;
- you have useful generation surplus that is currently being wasted.

### Add more generation when

- batteries rarely or never reach full charge;
- the bank continuously discharges while the workshop is busy;
- high-priority machines are starving lower-priority machines;
- your normal continuous load is close to or above **400 W**.

Batteries smooth out shortages, but they are **storage, not generation**. If the workshop consumes more energy over time than the generators produce, even a huge bank will eventually empty.

## Repair, enlarge or dismantle a bank

Battery charge belongs to the individual Battery Blocks.

If a block is removed and the remaining shape is no longer a filled rectangle, the bank becomes invalid. Unclaimed cells return to standalone operation and their own sockets work again.

To enlarge a bank, add Battery Blocks until the whole structure forms a new filled rectangular pack. Its total capacity and transfer rate update when the controller validates the new shape.

Adding an empty Battery Block adds **capacity**, not free energy.

> **A charged Battery Block cannot be mined.** Discharge it into a real electrical load first. Setting it to **DischargeOnly** prevents it from immediately charging again while you empty it.

The Battery Bank Controller can be removed without draining because the controller itself stores no energy.

## Troubleshooting

| Symptom | What to check |
|---|---|
| **Alternator produces no power** | Make sure the Boiler Engine has both fuel and water, the machines face the same direction, and the Alternator is immediately on the boiler's right with the shafts meeting. |
| **Machine receives 0 W** | Check that Power Cable reaches the correct electrical sockets and that a Blue Signal connection is not holding the machine OFF. |
| **Machine runs slowly** | Open it and compare requested vs received watts. Total demand may exceed generation, or a higher-priority load may be taking power first. |
| **Battery never charges** | Batteries only receive true surplus. Disconnect or stop some loads and check whether generation now exceeds demand. Also check that the battery is not DischargeOnly or Isolated. |
| **Battery never discharges** | Check that it contains energy and is not ChargeOnly or Isolated. The network must also have real unmet electrical demand. |
| **Battery bank will not form** | The pack must be a completely filled rectangular solid, 1–5 blocks along each dimension, with exactly one controller and at least one Battery Block. Remove gaps and unrelated blocks. |
| **Controller formed but cable does nothing** | Connect Power Cable to the controller's **front** socket. Member-cell sockets are inactive while the bank is formed. |
| **Two nearby banks interfere with each other** | Leave an air gap between separate packs. Touching battery groups can be interpreted as one connected structure containing multiple controllers. |
| **Cannot mine a Battery Block** | It still contains charge. Put it in DischargeOnly and drain it into a real load first. |
| **Battery seems to lose progress after forming a bank** | Formation does not redistribute energy between cells. The controller reports their combined total; each cell retains its exact stored energy. |

## Related guides

- [Pipes and pumps](Pipes-and-pumps.md) — bring water to Boiler Engines and other machines.
- [Tanks](Tanks.md) — build larger water storage for an industrial workshop.
- [Blue Signal](Blue-Signal.md) — control machines without confusing signal wiring with electrical power.
- [Crafting](Crafting.md) — find recipes and use the Machinist's Bench.
