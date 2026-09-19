# Renewable electricity verification — 19 September 2026

Scope: [renewable generation](../RENEWABLES.md), the next issue #10 increment, including the user's parallel battery charging, gradual solar curve, smooth wind gusts and idle-rotor requirements. [Player guide](../wiki/Renewable-power.md).

## Focused checks

The open Unity **6000.4.4f1** Editor ran the renewable domain, power allocation and content-compatibility checks, plus existing GridAllocation, Battery and Industry regression suites. Detailed artifacts are retained with the final player results below.

- Solar samples every minute from 06:00 through 18:00 rise monotonically to noon and fall monotonically afterward; night and both endpoints produce zero.
- Clear/rain/storm profiles apply the configured solar and wind values. Weather transitions stay gradual; a four-minute seeded gust sample remains bounded without abrupt jumps.
- Roof, residency and Blue Signal gates suppress generation. Each 400 W fixed step stores exactly 20 J when capacity is available.
- Mixed generators combine their supply; machines receive demand first; three batteries share the remaining output. Full cells relinquish their shares.
- Cable loops and a formed battery bank cannot turn battery discharge into battery charging. Actual delivered generation equals machine consumption plus charging. Fully charged idle grids draw zero.
- The exact complete pre-renewable schema-16 catalog is accepted; an unrelated older item mutation is rejected. No broad compatibility bypass or save-format bump was introduced.

## Art and imports

Original editable Blender 5.2 sources and reproducible exporter: `ArtSource/Renewables/` and `Tools/create_renewables.py`. The source renders were inspected against the established iron/copper Workshop kit. Solar uses a tilted framed cell array; wind uses three swept blades and a separate vertical rotor pivot.

Both imports fit one metre cells and retain UVs/normals. Each has three mesh renderers sharing the Workshop/status presentation. Source and imported triangle counts are recorded in the retained geometry/import reports. These counts are not a whole-game performance claim.

## Remaining review

The native Windows player passed **147 assertions**, including ordinary Survival startup, both real crafting/placement transactions, weather-dependent output, roof recovery, exact shared charging, rotor stop/restart on demand, and a full save/load preserving both generators and stored energy. Captures are in the [illustrated player guide](../wiki/Renewable-power.md).

Renewable domain checks passed **1,056 assertions**, allocation/delivery checks **8**, and compatibility checks **2**. [Build summary](renewables-2026-09-19/build-summary.txt), [artifact hashes](renewables-2026-09-19/build-hashes.json), [native report](renewables-2026-09-19/native-report.json), [domain checks](renewables-2026-09-19/domain-checks.txt), [power checks](renewables-2026-09-19/power-checks.txt) and [compatibility checks](renewables-2026-09-19/compatibility-checks.txt) retain the evidence.

The broader save suite passed **183 assertions** plus **5 fresh-process continuation checks** on the preceding renewable build, before the transient topology-rebuild reset was added. [Save report](renewables-2026-09-19/save-regression-report.json), [continue report](renewables-2026-09-19/save-resume-regression-report.json) and [that build’s hashes](renewables-2026-09-19/save-regression-build-hashes.json) preserve this distinction. The final build reran renewable, allocation, battery and industry checks and the 147-assertion native route. Its four warnings are existing deprecated object-query calls in AlphaPlaytestVerification; none are renewable compile errors.

The final build also passed **7 fresh-process renewable checks**: exact machine identities, stored battery energy, active tick and wind-gust value survived restart. An actual pre-renewable schema-16 Weather checkpoint passed **4 fresh-process load checks**, including its exact partly transitioned storm. [Renewable continuation](renewables-2026-09-19/renewable-resume-report.json) and [historical checkpoint](renewables-2026-09-19/legacy-weather-report.json) retain those results.

The refreshed wiki export contains **195 items / 199 recipes**. Local validation passed **228 pages / 9,829 links and images**. All **674 exported source fingerprints** match the staged Git content, including the committed terrain atlas; the unrelated working atlas is preserved separately. [Index check](renewables-2026-09-19/staged-export-check.txt). [Wiki deployment 35456121439](https://github.com/Starbugstone/Rivet-Reach/actions/runs/35456121439) succeeded from implementation commit `6b07ccb`, publishing wiki commit `e7d366d`. All 228 maintained pages and the 12 new renewable images/icons match the deployed files byte-for-byte. Live Chromium checks loaded the guide's 10 captures, both item pages (13/15 images) and all 195 index icons. All four pages fit a 390-pixel mobile viewport without document overflow; Home → Renewable power navigation succeeded.

[Live checks](renewables-2026-09-19/live-wiki-checks.json), [published asset hashes](renewables-2026-09-19/published-assets.json), [desktop guide](renewables-2026-09-19/wiki-Renewable-power-desktop.png), [mobile guide](renewables-2026-09-19/wiki-renewables-mobile.png) and [turbine item page](renewables-2026-09-19/wiki-Item-wind-turbine-desktop.png) retain publication evidence. Issue #10's renewable implementation and acceptance boxes are checked, with the user’s curve, gust, demand and battery rules recorded. [Remaining checklist](renewables-2026-09-19/issue-checklist.json): sustained hunger/food-cadence tuning and validation, plus conditional multiplayer synchronization. Long-session factory balance and subjective art acceptance remain separate from focused conservation checks. The 400 W peaks and gust ranges are working defaults, not a proven optimum.
