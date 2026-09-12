# Hand crank

Authorized by the user's 2026-09-12 request for manual early-game electricity. This document owns the crank; [batteries](BATTERIES.md) own storage and [industry](INDUSTRY.md) owns electrical allocation. Values below are working balance choices, subject to play review.

## Craft and use

Craft one **Hand Crank** at a 3×3 workbench from **2 copper ingots, 1 iron ingot, 1 stick and 2 planks**. This shapeless recipe uses four occupied ingredient slots. It requires no powered machine, Azure crystal or industrial component. The existing battery recipe is unchanged.

Select the crank and right-click a horizontal side of a Battery Block. Placement occupies the adjacent cell and automatically points its rear power socket into the battery. This placement gesture takes precedence over opening the battery. Top/bottom battery placement is rejected with a side-attachment hint. A formed bank accepts electricity through any exposed face of its controller; claimed cells retain their existing disconnected-socket rule. A crank can also be placed separately and connected on any face using a power cable. When placing against another machine, the usual crouch-to-place rule applies.

Aim at the placed crank within the normal five-metre interaction range. **Right-click once** (or the bound Interact key) pays for one turn. **Hold right-click** to repeat. Remapped mouse Use retains the same behavior. The HUD explains the gesture. No machine inventory opens when using the crank.

One turn supplies **100 W for ten 20 Hz steps**, totaling **50 J (0.05 kJ)**. A turn cannot be stacked or restarted until it finishes, so rapid clicking cannot exceed held-use output. Releasing Use, looking away or entering a menu stops further turns; a turn already started finishes on eligible simulation ticks. Pause/death stop simulation. Dormant machines and rebuilding topology generate no offline credit. The original mesh's handle rotates only during paid operation.

## Electricity and persistence

Under the [all-face electrical rules](INDUSTRY.md#wrench-and-configurable-pipe-ends--2026-09-12), all six face terminals share one ordinary generation budget without joining separate cable grids. The rear fitting still guides automatic side-attachment placement, but does not restrict conduction. Existing allocation serves machine demand first and charges batteries only from surplus. A directly attached idle battery receives 50 J per complete turn. A 20 W lamp on that network leaves 40 J per turn for storage. At continuous 100 W, a completely empty 100 kJ cell takes approximately 16 minutes 40 seconds to fill without loads; a short effort can instead accumulate a useful small starting reserve. Actual input cadence/frame timing can extend that time.

Battery modes, exact per-cell storage, capacity clamping, bank controller rules and exact millijoule conservation apply unchanged. Full/isolated batteries accept no energy; unused generated power is discarded. Mining the crank returns its ordinary item once and discards any unfinished turn, while stored battery charge remains in the battery. Charged battery removal remains protected.

Stable identity is `rivet:hand_crank`, runtime ID **170**, recipe `rivet:industry_170`. Paid-turn remainder uses the existing serialized machine `PulseTicks` field, bounded to 0–10 for this item; no save schema layout changes. Save/load preserves the physical crank, orientation, remaining paid turn and exact battery charge. The loader explicitly accepts the pre-crank content fingerprint only when every pre-existing definition still matches. New saves retain the full new fingerprint; older executables reject them. See [save compatibility](SAVES.md#storage-and-compatibility).

## Original assets and verification

[create_hand_crank.py](../Tools/create_hand_crank.py) authors [HandCrank.blend](../ArtSource/HandCrank/HandCrank.blend), the exported `Industry/hand_crank.fbx` and icon `Industry/Icons/170.png`. It follows the existing iron/copper/brass workshop direction and reuses the project's original atlas and geometry helpers. The housing and moving crank are two mesh parts sharing one material. No third-party art or dependency is introduced.

[Hand crank verification](verification/HAND_CRANK_RESULTS.md) records the actual checks, build and images. `HandCrankBuild.Run` prepares only the new definitions/import, validates the crank plus existing domain/industry/battery/multiblock suites, exports the item reference and builds `Builds/HandCrank/RivetReach.exe`. `-rr-verify -rr-hand-crank-review -rr-output <directory>` runs the focused Windows player scenario. Ordinary sessions receive no fixtures.
