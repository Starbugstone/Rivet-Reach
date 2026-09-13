# Compost

Issue [#10](https://github.com/Starbugstone/Rivet-Reach/issues/10), next playable increment authorized September 13, 2026. The user selected implementation of the recommended compost slice. Numerical quantities below are working defaults, not individually user-selected or long-session balance results. [Farming](FARMING.md) owns ordinary crop growth and harvests.

## Player loop

Craft one **Compost Bin** at a workbench with seven planks in a U shape:

```text
P · P
P · P
P P P
```

The original wooden bin occupies one solid cell and opens through right-click or the bound Interact action. It has one organic input slot and one output slot. Each holds an ordinary stack; one input identity is processed at a time. No fuel, electricity, water or industrial component is needed.

A batch needs **24 organic points** and **200 fixed ticks / 10 seconds** to make one Compost. Contributions come from `Resources/Definitions/Compost.json`:

| Contribution per item | Inputs | Items for one Compost |
|---|---|---:|
| 1 | Leaves, wheat/flax/carrot/berry seeds, flax fibre | 24 |
| 2 | Saplings, apples, potatoes, grain, carrots, berries, mushrooms | 12 |
| 4 | Baked potatoes, bread, roasted carrots, vegetable stew, cooked mushrooms, fruit porridge | 6 |

Inputs must carry the shared `compostable` tag **and** have an explicit positive catalog contribution. Compost itself cannot be composted. Contributions must divide the batch threshold exactly, and a batch must fit in the input stack. Validation publishes the lookup only after all entries pass; tags grant eligibility rather than material value. Search `#compostable` in the item browser; each exact conversion appears in the recipe/uses browser and exported wiki reference.

## Processing, recovery and automation

Input items stay in the bin until a whole batch finishes and output space is available. The synchronous completion transaction removes exactly the configured batch and adds exactly one Compost. There is no hidden partially consumed organic balance or rounding loss. An incomplete quantity waits in the input slot; it does not begin the processing timer.

Full output, insufficient inputs, optional Blue Signal OFF and nonresident terrain pause processing without consumption. Changing input identity resets unpaid processing time. Removing and replacing a stack of the same identity may retain unpaid time; it cannot change the required batch or its yield. The bin has no power endpoint or fuel slot.

Item pipes use the existing shared grid allocator and wrench-configured ends. Input ends on any face accept configured organics when the single slot has space; output ends extract only finished Compost. Disconnected ends transfer nothing. The optional signal terminal uses the bin's front face, matching ordinary machine signal input. The controller UI shows status, batch quantities and actual time progress.

Mining recovers one bin, every queued input and finished output through the existing drop transaction. Unpaid elapsed processing time resets on replacement; no consumed material is lost because batch ingredients were retained. Existing Creative mining/drop rules still apply. Saving a placed bin retains its input, output, orientation and exact partial processing time.

## Crop use

Select Compost and right-click an immature potato, wheat, flax, carrot or berry plant within normal five-block reach. One successful click consumes one Compost and advances exactly one stage. Holding Use does not repeatedly spend Compost; another click is required. Wild plants on natural soil and cultivated plants share this action. Creative retains the selected stack.

The target and support must be resident, supported and meet the existing skylight threshold of 9. Mature crops, mushrooms, saplings, noncrop targets, blocked/light-ineligible targets and failed transactions consume nothing. Stations retain normal use precedence.

Acceleration replaces the old scheduled deadline with a full interval for the next stage. Maturation removes the scheduled job. This prevents an almost-due old job from immediately advancing the plant again, including after save/load. Growth remains scheduled and bounded; no per-plant component or per-frame scan is added.

Compost is optional and never a crop requirement. Maximum configured harvest contributions do not fund the three Compost needed to regrow the same crop instantly. Actual surplus availability and player value still require sustained playtesting.

## Persistence and compatibility

The bin reuses existing machine slots, `Work` and `WorkInput`; no binary fields or generator change are needed. Saves remain schema **10**, with a new content fingerprint including the compost catalog, new items, recipe and tags. Accepted older fingerprints project away only the two new identities, bin recipe and frozen `compostable` additions to known old inputs. Unrelated item/recipe/tag checks and existing previous-tier compatibility remain enforced.

Save capture synchronizes changed input identity before writing. Loading rejects fractional or completed-but-uncommitted work, wrong work identity, fuel, hidden-slot contents and non-Compost output. Failed-load rollback remains authoritative. Old terrain is never regenerated or amended by this addition.

## Assets and evidence

`Tools/create_compost_assets.py` authors original slatted wooden-bin and compost-clod meshes against the existing Rivet Reach atlas. Editable sources and neutral-lit source renders live in `ArtSource/Compost`; explicit FBX, normalized prefabs and matching PNG icons are used for world, held and dropped presentation. The bin's organic surface is visible when it contains input or output.

[Verification](verification/COMPOST_RESULTS.md) records actual checks, build/capture identities and remaining limits. [Player guide](wiki/Compost.md) teaches crafting, quantities, crop use and pipe setup. Beds, fishing, chickens, warehouse storage, weather and renewables remain separate increments.
