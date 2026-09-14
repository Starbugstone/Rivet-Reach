# Compost

Issue [#10](https://github.com/Starbugstone/Rivet-Reach/issues/10), revised under the user's [September 14 feedback](https://github.com/Starbugstone/Rivet-Reach/issues/10#issuecomment-5662161983). Mixed immediate deposits, random 1–4 output and a manual/basic versus automatic split are user requirements. The user also confirmed electricity for the autocomposter. Contribution quantities, electrical defaults and upgrade recipe below are working balance choices.

## Manual Compost Bin

Craft one bin at a workbench from seven planks in a U shape:

```text
P · P
P · P
P P P
```

Open with right-click or Interact. The interface has an **Add organics** deposit target and one shared percentage. Left-click deposits the held stack; right-click deposits one item; Shift-click an inventory stack deposits it. Accepted items disappear immediately and their contributions remain even when the next input is a different item. The deposit target is not a processing inventory or a requirement to assemble one matching stack.

A full level needs **24 organic points**, displayed as 100%. `Resources/Definitions/Compost.json` authors each item's contribution:

| Points / item | Progress / item | Inputs |
|---:|---:|---|
| 1 | 4.17% | Leaves, wheat/flax/carrot/berry seeds, flax fibre |
| 2 | 8.33% | Saplings, apples, potatoes, grain, carrots, berries, mushrooms |
| 4 | 16.67% | Baked potatoes, bread, roasted carrots, vegetable stew, cooked mushrooms, fruit porridge |

Every accepted item needs the shared `compostable` tag and an explicit positive contribution. Search `#compostable` in the item browser. Any mixture of configured inputs contributes to the same level; contributions need not divide 24 exactly. There is no processing timer. The old catalog `ticks` field remains solely for precise historical definition compatibility and legacy-state validation.

At each full level, subtract 24 points and produce **one randomly selected quantity from 1–4 Compost**. Excess points carry into the next level. A stack can complete several batches. Selection uses a deterministic pseudorandom sequence with persisted batch count and station position, so save/load and blocked-output retries cannot reroll a pending batch. The basic bin ejects finished Compost above its rim as ordinary collectible drops. Normal dropped-item pickup and hazards apply.

The basic bin has **no item-pipe, power or Blue Signal connections**. Its world model shows accumulated organic material rising with progress. Deposits are bounded by the ordinary stack limit; no per-item GameObjects or new background jobs are introduced.

## Electric Autocomposter

Craft one **Autocomposter** at the 4×4 Machinist's Bench from one Compost Bin, one Machine Casing, two Cogs, two Item Pipes and one Gold Ingot. This keeps the wooden bootstrap cheap and gives gold a construction role in the automation upgrade.

It shares exactly the same mixed-input contribution and random-output rules. Its output is retained in a stack for manual collection or pipe extraction. Wrench-configured item inputs/outputs can use any face; only the output can be extracted. The shared pipe allocator remains authoritative. Blue Signal optionally controls operation at the front.

Electrical working defaults: **160 W**, **8 J per accepted organic item**, and a **512 J internal reserve** (up to 64 items). Ordinary all-face electrical allocation charges this reserve in fixed steps; partial power charges proportionally. Charging stops when full, dormant, signal-disabled or unable to fit the next output. Stored charge can finish deposits after disconnection, then further deposits wait for power. The interface shows remaining charge. The reserve is not a battery/grid source.

No item is accepted unless its charge and any output space required by that item are available. Accepted inputs are immediately consumed, including pipe-delivered items; an unpaid/unfittable remainder stays in the hand or source inventory. Pausing preserves points, finished output and paid charge. A blocked deposit does not advance the random sequence.

## Recovery and compatibility

Both machines save their exact partial level, completed-batch sequence and output; the autocomposter also saves paid charge. **Schema 11** appends those fields only to composter machine records. Older content fingerprints project away only the new autocomposter item/recipe and the revised-operation marker; the existing compost catalog and all unrelated definitions remain checked.

Schema-10 bins remain manual bins. Previously queued inputs and finished output are retained; opening the bin ejects old output and feeds its queued input under the new rules. The old timer represented unpaid work, so it is discarded without removing any input. Old attached pipes become disconnected from the manual bin; use the new autocomposter to restore automation. No placed machine is silently upgraded or charged a crafting cost.

Mining returns the machine and any unconsumed legacy input or retained finished output. Already-consumed unfinished organic progress and internal electrical reserve are lost when dismantling; they are not refunded as new organic items. Save/load preserves them while the machine stays placed. The UI and player guide explain this distinction. Existing Creative mining/drop rules still apply. Terrain generation and generated-chunk history are unchanged.

## Crop use

Select Compost and right-click an immature potato, wheat, flax, carrot or berry plant within normal five-block reach. One successful click consumes one Compost and advances exactly one stage. Holding Use does not repeatedly spend Compost; another click is required. Wild plants on natural soil and cultivated plants share this action. Creative retains the selected stack.

The target and support must be resident, supported and meet the [sky or placed-light threshold](LIGHTING.md) of 9. Mature crops, mushrooms, saplings, noncrop targets, blocked/light-ineligible targets and failed transactions consume nothing. Stations retain normal use precedence.

Acceleration replaces the old scheduled deadline with a full interval for the next stage. Maturation removes the scheduled job. This prevents an almost-due old job from immediately advancing the plant again, including after save/load. Growth remains scheduled and bounded; no per-plant component or per-frame scan is added.

Compost is optional and never a crop requirement. Maximum configured harvest contributions do not fund the three Compost needed to regrow the same crop instantly. Actual surplus availability and player value still require sustained playtesting.

## Assets and evidence

The basic bin and Compost retain their original artwork. `Tools/create_compost_assets.py --auto-only` adds the original reinforced, metal-banded autocomposter with gold shaft fittings and a front output opening. Editable sources and renders live in `ArtSource/Compost`; explicit FBX, normalized prefabs and matching icons serve placement, inventory, held and dropped presentation.

[Current verification](verification/COMPOST_RESULTS.md) records measured checks and remaining limits. [Player guide](wiki/Compost.md) teaches mixed deposits, power and pipe setup. Beds, fishing, chickens, crates, weather and renewables remain separate increments.
