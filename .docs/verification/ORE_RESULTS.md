# Ore generation and bedrock verification — 2026-09-08

The user selected ore generation, then required multiple ores at different levels and an unbreakable bedrock base. This implementation adds five finite ore types, distinct raw-resource drops and a protected world floor. The material profile is in [ECONOMY.md](../ECONOMY.md#current-ore-generation-and-bedrock), interaction rules in [GAMEPLAY.md](../GAMEPLAY.md#17-ore-mining-and-bedrock), and generation/authority details in [SIMULATION.md](../SIMULATION.md#15-ore-generation-and-the-world-base).

## Build and replay

Unity **6000.4.4f1**, URP **17.4.0**, Windows x64 Development build; generator `terrain-3-ores-bedrock`. The build completed with **0 errors and 0 warnings** in 30.95 seconds. The tested executable is also preserved locally at `Builds/Ores/RivetReach.exe`; ordinary builds still use `Builds/PlayerRevision4/RivetReach.exe`.

```powershell
.\Tools\Build-Windows.ps1
.\Tools\Verify-POC.ps1 -Ores -OutputDirectory 'Logs\OreVerification'
.\Tools\Verify-POC.ps1 -OutputDirectory 'Logs\OreTerrainRegression'
```

The runtime test explicitly selects seed **246813**, finds natural ore through the generator, excavates approach corridors as test fixtures, then uses the real player targeting/mining/pickup path. It does not place fake ore blocks. Ordinary sessions receive neither corridors nor test teleports.

Preserved build SHA256: `RivetReach.exe` = `7d52ee70fb22771f4608d810d1015f539c37dd8b7b2fbb1a0ea9f6bc5c32d5c9`; `Assembly-CSharp.dll` = `da0bbc5759b4d73ef0f88dc6391382776dac7c2b42c97092e1f62c5a31e78db7`.

The shared checkout also contained a separate terrain/sky presentation revision during the final standalone checks. That revision is outside the ore change; screenshots describe the tested shared build. No character sources, exported models or animations were edited for ore generation.

## Automated evidence

- **54,921 domain assertions passed**, including existing terrain, tree, grass and inventory checks plus the ore checks. **1,157,353 crafting assertions passed** against the expanded item registry.
- **87 focused standalone assertions passed, with no logged errors or exceptions.** [Machine-readable runtime report](ore-runtime-report.json).
- **263 existing standalone terrain/inventory/visual regression assertions passed, with no logged errors or exceptions.** [Regression report](ore-terrain-regression-report.json).
- The standalone test targets and mines each of the five natural ores. Bare hands, axe and dagger are rejected; the starter pickaxe produces exactly one matching raw resource, collected once. Repeating the old command produces no second drop. Raw resources cannot be placed as ore.
- Bedrock rejects every current capability and their combined flags, direct removal and replacement. Sustained mining produces no progress, drops or edit records. A downward movement sweep stops at the floor's top face within the existing 1 mm collision skin.
- A 1,024-block trip unloads all five mined positions. Nonresident queries retain depletion, and returning regenerates the chunk with the removal still applied. The bedrock floor remains intact after unloading/reloading and origin shifts.
- Point queries agree with chunk generation at negative, positive and near-billion-block coordinates. Full neighbouring halo faces agree on all three axes at shallow/deep levels and the floor. Concurrent workers and reverse-order queries reproduce isolated generation. Different seeds change deep ore layouts. Ores stay inside their Y bands and never replace soil, cave air or bedrock.

## Measured distribution and cost

Editor sample: seeds **246813, 0 and −777**, each covering a 128 × 128 block square, Y **−256 through 95**: **528 chunks / 17,301,504 interior cells**. Every sampled seed contained all five ores.

| Ore | Total sampled ore cells | Mean ore Y |
|---|---:|---:|
| Coal | 53,852 | 33.90 |
| Copper | 37,916 | 4.67 |
| Iron | 85,607 | −60.13 |
| Gold | 7,316 | −148.48 |
| Diamond | 1,802 | −209.32 |

Editor generation alone: **1.943 ms median, 11.405 ms p95, 15.075 ms maximum per chunk**. These timings include terrain, caves, ores and trees, exclude meshing, and are not an incremental ore-cost comparison.

Focused standalone workload: Intel Core i7-10750H, RTX 2060, approximately 32 GB RAM, 1280 × 720, radius 10 chunks, 90 FPS cap. Sampled frames were **11.11 ms median, 15.50 ms p95, 288.70 ms maximum**; first-ready time was **11.42 s**, peak residency **1,950 chunks**, recorded allocation peak **244,070,721 bytes**. Frame sampling covers extraction, pickup and the final streaming trips; bulk corridor excavation is excluded. Teleports and session preparation are verification operations, not a continuous walking benchmark. The maximum frame remains a hitch, so these results do not establish consistently smooth traversal or final performance. Draw-call capture was unavailable (`−1` in the report).

## Visual evidence

Actual Unity screenshots were inspected for ore/stone distinction, tool feedback, resource icons and the protected floor. Approach corridors are test excavations; surrounding ore is naturally generated.

![Natural copper and required-tool feedback](ore-copper.png)

![Natural deep diamond ore](ore-diamond.png)

![Bedrock floor and unbreakable feedback](ore-bedrock.png)

![One collected resource from each ore in inventory](ore-inventory.png)

## Remaining review

Ore abundance, vein shapes, visual style and mining times are working defaults awaiting play feedback. The sampled seeds do not certify every seed's resource route or the future 256-block spawn-validation rule. Existing caves remain shallow; deep resources currently require excavation. Smelting, fuel use, new material recipes, tool grades, durability and durable world saves remain future work. Mining depletion persists only within the running session, as before.
