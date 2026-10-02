# Guidance and building verification — 2026-10-02

This increment implements the user's selected alpha follow-up: depth/tool cues, live machine setup advice, Home/Last death navigation, Wooden/Stone Slabs and connected ordinary Glass. No mine marker or component assembler is implemented. The assembler has its own [issue #22](https://github.com/Starbugstone/Rivet-Reach/issues/22).

The local Windows player is **`Builds/AlphaGuidance/RivetReach.exe`**, built with Unity **6000.4.4f1 / URP**, non-development, and checked at **1600×1000 / Direct3D11**. [Build identity](alpha-guidance-2026-10-02/build-identity.json) pins the managed assembly and historical checkpoint hashes. The candidate includes the committed second-generation explorers. An unrelated uncommitted `BlockTiles.asset` in the main workspace was preserved; the isolated build, exported icons and publication validation use the committed terrain artwork.

## Measured checks

| Check | Result |
| --- | --- |
| [Geometry, material and guidance](alpha-guidance-2026-10-02/domain-checks.txt) | **56 passed**: exact slab crafting, half-height mesh/selection/collision, full-top support, voxel roof opacity, canonical item recovery, transparent face removal, cross-chunk halo joins/removal, diagonal masks, authoritative ore/tool hints and precise navigation/save data. |
| [Existing starter recipes](alpha-guidance-2026-10-02/starter-recipes.txt) | **1,888 passed** across all **56 original layouts/quantities**, including translated/mirrored crafting and exact consumption. The two added slab recipes have their own checks. |
| [Components and compatibility](alpha-guidance-2026-10-02/component-recipes.txt) | **138 passed**: approved one-ingot Cog/four plate Rivets, existing Sand → Glass processing, exact approved/previous schema-18 content envelopes, and rejection of unrelated definition changes. |
| [Historical envelopes](alpha-guidance-2026-10-02/save-envelopes.txt) | Pinned schemas **1–17** and independent corruption/rejection checks passed. These envelope checks are distinct from a full native world migration. |
| [Focused native acceptance](alpha-guidance-2026-10-02/native-result.json) | **38 behavioral/residency checks passed**, plus 311 supplied fixture-cell setup checks; no recorded errors; [process exit 0](alpha-guidance-2026-10-02/native-exit-code.txt). |
| [Ordinary Survival route](alpha-guidance-2026-10-02/survival-result.json) | **7 passed, exit 0** on the final delivery build; 312 seconds, gathered four logs, placed a workbench and crafted a wooden pickaxe; full health and 14/20 food at completion. An earlier normal route also passed before focused acceptance. |

The supplied native fixture exercises ordinary slab placement and one-item consumption, upper halves, merging/recovering two halves, exact collision, walking onto a lower slab, aiming through the empty half and the stone pickaxe gate. Drill extraction refuses a combined block with only one output slot unit free, then recovers exactly two slabs after capacity is released.

The real Crusher first has a connected cable without supply; its interface gives the matching instruction. Supplying the Boiler with fuel/water and aligning the Alternator then produces actual crushed output. Glass is mined and its gap remeshed across the X=31/32 chunk boundary. A normal Bed interaction binds Home; an actual Survival death records Last death, and respawning retains it.

A schema-19 checkpoint restores the exact slab variants, Glass, machine state, death address and bound-bed identity. A deliberately truncated final navigation field fails loading and restores the original world/navigation. Clearing the death marker and removing the bed invalidate their respective destinations. The retained [actual schema-18 checkpoint](alpha-guidance-2026-10-02/schema-18.rrsave), captured in the September 20 connection review, loads without a death marker and migrates through a full byte-for-byte schema-19 round trip. Synthetic catalog checks additionally cover the approved pre-slab Cog/Rivet definition set.

## Actual player captures

The supplied workshop demonstrates the new building materials; it is not claimed as a naturally gathered first factory.

![Joined clear Glass, stone slab porch and wooden slab roof](../wiki/images/alpha-guidance/connected-glass-workshop.png)

![A mined block exposes the Glass rim around its opening](../wiki/images/alpha-guidance/glass-removed-edge-update.png)

![Connected cable without supply gives an actionable Crusher instruction](../wiki/images/alpha-guidance/crusher-connected-no-supply.png)

![Running Boiler with rear fuel, water and shaft instructions](../wiki/images/alpha-guidance/boiler-shaft-fuel-water-guide.png)

![Home and Last death directions after a real death and respawn](../wiki/images/alpha-guidance/home-and-death-navigation.png)

![Diamond acquisition explains actual generator heights and tool requirements](../wiki/images/alpha-guidance/diamond-depth-and-tool-cue.png)

The [building guide](../wiki/Slabs-and-connected-glass.md) also shows the window corner; [navigation](../wiki/Exploration-and-navigation.md) and [machine feedback](../wiki/Machine-setup-feedback.md) explain the controls. The new slab icons are rendered from their actual textured meshes. Glass retains its existing held/component artwork and crafting identity.

## Reproduce

Build with the pinned Editor's `-executeMethod RivetReach.Editor.AlphaGuidanceBuild.Build`. It prepares only the additive slab assets/Glass material, runs the focused Editor gates, exports the catalog/icons and creates `Builds/AlphaGuidance`.

Run the player with `-rr-verify -rr-alpha-survival-review -rr-output <survival-directory>` before the focused acceptance. For the supplied fixture, copy `alpha-guidance-2026-10-02/schema-18.rrsave` to both `<focused-directory>/schema-18.rrsave` and `<focused-directory>/Saves/98eb00220b7d45c68b27e895230461e4.rrsave`, then run with `-rr-guidance-review -rr-output <focused-directory>`. Use fresh isolated output directories. These flags are opt-in; ordinary new expeditions remain empty-handed.

## Limits and publication

**Frame-rate rechecking is deferred at the user's request until the development machine is less stressed.** The functional checks/captures make no FPS, frame-pacing or long-session performance claim. The full natural-resource 1–2 hour first-line trial and wider alpha readiness items remain in the [gameplay review](ALPHA_0_1_0_GAMEPLAY_REVIEW.md).

Slab light/fluid occupancy uses the existing voxel simulation; there is no waterlogging or sub-voxel light transport. Creature placement remains conservative by occupied cell. Transparent glass uses ordinary alpha rendering; this check is not an exhaustive review of overlapping transparent structures across every camera angle/GPU.

The matching export contains **197 entries and 201 crafting/processing recipes**, including **114 grid recipes**. The isolated `python3 Tools/publish_wiki.py --check` passed **239 pages / 10,096 local links/images**; **582 design/report links** also passed. Main-workspace export validation still detects the preserved, unrelated terrain texture edit. The matching committed artwork was checked in the isolated candidate. Publication and live-render verification are recorded after deployment below.
