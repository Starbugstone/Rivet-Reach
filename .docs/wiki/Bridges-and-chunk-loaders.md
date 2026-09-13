# Bridges and chunk loaders

[Floater Rocks](Item-floater-rock.md) contain the magic that lets [Floaters](Floater.md) hover. Craft them into paired bridges to join distant workshops, and chunk loaders to keep those workshops active while you explore.

## Craft your bridges

Use the [Machinist’s Bench](Item-machinist-bench.md). Each bridge recipe makes **two blocks**:

| Pair | Ingredients |
|---|---|
| [Item Bridges](Item-item-bridge.md) | 2 Floater Rocks + 2 Machine Casings + 4 Item Pipes |
| [Liquid Bridges](Item-liquid-bridge.md) | 2 Floater Rocks + 2 Machine Casings + 4 Fluid Pipes |
| [Power Bridges](Item-power-bridge.md) | 2 Floater Rocks + 2 Machine Casings + 4 Power Cables |

![Three bridge types connected to a battery, chest and liquid tank](images/bridges-source-networks.png)

## Link two workshops

1. Place one bridge at each workshop and connect the matching pipes or cables on any face.
2. Right-click or press Interact on the first bridge. Enter a name, such as **Quarry**, then choose **Apply Name**.
3. Give the second bridge of the same type the same name. Names ignore upper/lower case.
4. Check the displayed partner coordinates and **Linked** status. Nearby labels identify each bridge and its network.

![Bridge controls show the private name, owner and partner coordinates](images/bridge-name-and-partner.png)

Each name accepts two bridges of each type. A third is refused; choose another name. Item, liquid and power pairs stay separate even when all three are named Quarry. Other players’ same-named networks will stay separate too.

**Unlink** clears one endpoint. Renaming or mining either block breaks its previous connection. A newly placed replacement needs to be named again.

Bridges join the existing networks: they do not create electricity or duplicate stored items/liquids. [Machine pipe ends](Pipes.md) still need the appropriate wrench directions. A pipe connected to a bridge behaves as a continuous route. Liquid bridges carry water or lava, but connected tanks must contain compatible liquids. Blue Signal does not pass through these bridges.

## Keep the remote workshop running

Craft a [Chunk Loader](Item-chunk-loader.md) from **4 Floater Rocks + 1 Machine Casing + 4 Azure Crystals** at the Machinist’s Bench. Place it in the chunk you want active; it starts enabled and needs no power or fuel.

![Chunk loader controls show enabled state and chunk coverage](images/chunk-loader-controls.png)

A loader covers only its own **32 × 32 × 32 block chunk**. Open its controls to see the chunk’s minimum coordinates. Machines, stations, pipe/cable runs and pump/drill targets across a chunk boundary need coverage in that chunk too. Cover both bridge endpoints and the factories they connect.

Enabled loaders and bridge names survive Save/Load. Their distant chunks load automatically when you resume that world. Pausing or closing the world stops production; nothing is produced offline. Ordinary creature spawning still depends on player range.

![The receiver workshop after restoring its distant saved installation](images/bridges-receiver-networks.png)

These are actual in-game captures from the **2026-09-13 Bridges review build**.

## Troubleshooting

| Status or symptom | What to check |
|---|---|
| Unlinked | Enter a name and choose Apply Name. |
| Waiting for second bridge | Use the same name on another bridge of the same type. |
| Partner asleep | Load both endpoints with the player nearby or chunk loaders. |
| Network already has two bridges | Unlink an old endpoint or choose another name. |
| Linked but no transfer | Check source contents, cable/pipe continuity, loader coverage and machine pipe-end directions. |
| Liquid conflict | Separate or drain tanks containing different liquids. |

Turning off or mining the last loader in a chunk allows it to unload when you leave. Its bridge waits safely, and stored resources remain at their source.
