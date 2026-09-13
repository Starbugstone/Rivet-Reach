# Light-dependent mob spawning — 2026-09-13

[Mob rules](../MOBS.md#shared-hostilepassive-spawning-rules) · [Player guide](../wiki/Mobs.md#lighting-prevents-new-hostile-spawns) · [Lighting contract](../LIGHTING.md)

The shared spawning profile now authors inclusive light limits independently of habitat and support blocks. Current hostile species allow 0–7; ground at 8 or higher prevents a new natural spawn. The query checks the air above every floor cell, including below hovering Floaters. It uses the lighting system's regional, revision-aware query and the shared day/night clock. Existing entities and explicit encounter placement retain their lifecycle.

## Evidence

The Windows review player is **`Builds/MobLight/RivetReach.exe`**, Unity **6000.4.4f1**, built with **0 errors / 0 warnings**. [Build summary](mob-light-2026-09-13/build-summary.txt) and [source/artifact hashes](mob-light-2026-09-13/artifacts.json) identify the exact isolated build. It includes the coordinated regional light query and frozen torch/held-light presentation; later Workshop Lamp presentation work is outside this artifact.

- **166 native assertions passed, zero runtime errors:** [full mob report](mob-light-2026-09-13/mob-runtime-report.json). Daylight prevents both surface hostile species, while night admits prowlers. Covered dark caves accept Floaters day or night. Real cross-chunk torch placement immediately defers stale spawning, then blocks it at light 12; removal restores darkness and spawning. Moving a torch one cell produces the exact allowed-7/rejected-8 boundary, including a footprint crossing both levels. Disabling the presentation light pool does not bypass prevention. Shared bright-ground rules, all habitats/support whitelists, caps, wall/hover navigation, melee, loot and saved entities also pass.
- **24 asset/configuration/save assertions passed:** [checks](mob-light-2026-09-13/asset-and-save-checks.txt), which also contains one checkpoint-path diagnostic. Real pre-light cave-profile, pre-habitat schema-10 and pre-Floater schema-7 checkpoints remain readable. Unknown light/support/health/item changes are rejected. Invalid light ranges are rejected for authors.
- The mob importer’s missing-definition fallback now recreates the same dark-ground defaults and underground/non-nocturnal Floater profile. Existing authored definitions are preserved. The subsequent [Editor compile/export](mob-light-2026-09-13/editor-authoring.json) passed; this Editor-only correction does not change the tested player DLL.

![An existing Floater beside a placed torch in an underground room](mob-light-2026-09-13/lit-cave-spawn-floor.png)

The floor measures level 12; natural spawning is rejected. The visible Floater was explicitly placed to verify that lighting does not remove existing creatures. [Generated cave capture](mob-light-2026-09-13/floater-cave.png) shows the cave-dwelling model with the same build.

The earlier [cave-habitat results](FLOATER_RESULTS.md) retain their own pre-light build identity.

## Verification limits

The numerical threshold is a working balance choice, not a user-specified value or a completed balance playtest. Shared bright-ground profiles are exercised, but passive animal populations are not implemented. The shared query intentionally excludes presentation-only held glow and cave ambient fill. Save schema and generated terrain are unchanged.
