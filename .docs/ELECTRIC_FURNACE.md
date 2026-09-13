# Electric furnace

The user requested an electrical alternative to the normal furnace on 2026-09-13. It shares the compiled furnace processing registry: every ingredient, quantity, output and full-power duration stays identical. The ordinary furnace remains available with its existing item fuel rules.

## Working behavior

`rivet:electric_furnace` (runtime ID 174) occupies one cell. Open it with right-click or the bound Interact action. Its machine panel provides an ingredient slot, output slot, progress, allocated/requested watts, connection status and electrical priority; there is no fuel slot. It is available through the registry-backed Creative catalog.

The working consumption is **200 W** while processing. Current recipes take **200 fixed 50 ms ticks (10 seconds)** at full power, consuming **2 kJ per operation**. These are implementation defaults chosen for this request, not a balance playtest result. Limited supply advances work proportionally through the ordinary power allocator. No power, signal OFF, full/incompatible output, missing ingredients or dormant terrain stops work and new electricity consumption. Valid partial work survives these pauses; changing the input recipe resets it. No offline production is credited.

Connect a cable or power fitting on any face. Each face terminates its existing electrical grid; priorities, equal grid sharing and battery budgets remain shared with other machines. Optional Blue Signal connects at the front and enables processing only when ON if attached.

Item pipes use their existing independently configured ends. Input accepts valid furnace ingredients on **all six faces, including the rear**; logs are ingredients for charcoal, and coal/other fuel-only cargo is rejected. Output extracts only completed products. Receiver preferences match the current ingredient or retained product, including raw/crushed versions of the same metal. Wrench-only direction arrows and Input → Output → No connection behavior are unchanged.

## Crafting and assets

One furnace + one machine casing + four copper wire makes **one Electric Furnace**, shapeless at the **4×4 Machinist's Bench**. It upgrades a survival furnace using existing industrial components, without changing beginning recipe layouts. The compiled recipe/browser and wiki derive from the same authoring data.

[The authoring script](../Tools/create_electric_furnace.py) creates the original insulated cabinet, copper edging, heating rails, handle, cooling fins and status lamp against the existing workshop art. Editable source is `ArtSource/ElectricFurnace/ElectricFurnace.blend`; Unity uses the explicit `Industry/electric_furnace.fbx` export and icon 174. The shared workshop material dims the heating emission when stopped; the separate status lamp follows ordinary machine status colors. Placement, held and dropped visuals use the existing industry presentation paths. No third-party artwork or dependency is added.

## Persistence

The electric furnace adds no fields or schema revision. Its item stacks, orientation, priority, signal and partial work use the existing machine record. Loader work bounds use the maximum compiled furnace duration for this machine, retaining earlier bounds for older machines. Exact battery storage remains separately persisted.

An explicit additive content fingerprint omits only the new electric-furnace item and crafting recipe when reading earlier checkpoints. Existing item, crafting, processing, fuel and mob definitions remain checked. Earlier orchard/wrench/crank/door compatibility branches remain intact. New checkpoints require an executable recognizing the new content. Full-state validation and failed-load rollback remain the shared save authority.

[Verification](verification/ELECTRIC_FURNACE_RESULTS.md) records the actual checks and playable artifact. [The player guide](wiki/Electric-Furnace.md) explains setup.
