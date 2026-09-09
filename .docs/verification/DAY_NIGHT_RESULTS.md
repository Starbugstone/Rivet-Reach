# Day/night and lunar-phase verification — 2026-09-09

The user requested a moving sun/moon and a different moon phase each night. [Gameplay](../GAMEPLAY.md#day-night-and-lunar-phases) owns the working defaults; [simulation](../SIMULATION.md#session-world-clock-and-celestial-presentation) owns time and presentation boundaries. The cycle is implemented and verified in an isolated Windows player. Player feedback on duration and night visibility remains pending.

## Tested build and scope

Unity **6000.4.4f1**, URP **17.4.0**, Windows x64 Development build, committed baseline **2355996** plus only this day/night change. Concurrent terrain/biome, survival/progression and mob work was still under development in the shared checkout, so it was preserved and excluded from this review build. Those owners acknowledged file ownership and `game.Sky.Clock.IsNight` through shared coordination mailboxes. Combined-feature validation belongs to their later integration builds.

The final build succeeded with **0 errors and 0 warnings** in **19.90 seconds** after shader polish; the first successful build from the fresh import took 405.93 seconds. The verified player is copied to `Builds/DayNight/RivetReach.exe` in the main checkout. [Replay instructions](../FIRST_POC.md#verification) describe the Editor commands and `-rr-day-night-review` player flag.

SHA256: `RivetReach.exe` = `7d52ee70fb22771f4608d810d1015f539c37dd8b7b2fbb1a0ea9f6bc5c32d5c9`; `Assembly-CSharp.dll` = `27e13b873e16c544db2be2844d5b5239e31e0b5a5a9999112150f35de1101a78`.

Initial fresh imports hit native Bee backend failures, including access violation `0xC0000005`, before completing the build. The final build used a separate project and a project-local `BEE_CACHE_DIRECTORY`; shared caches, user Editor and running user player were preserved. A concurrent resource check found about 0.78 GiB free physical memory and 1.34 GiB virtual-memory headroom. Resource pressure is a plausible contributor, not a proven root cause. The cache override follows Unity's [cache-location reference](https://docs.unity3d.com/6000.4/Documentation/Manual/build-cache-location-reference.html).

## Automated and rendered evidence

- **144 domain assertions passed:** morning initialization, configurable duration, clock rollover, three complete lunar cycles, unchanged phase across midnight, exact dawn progression, batched versus 60 Hz advancement, long-running precision and invalid-input rejection. [Domain report](day-night-domain-report.txt).
- **39 standalone assertions passed, with no logged errors/exceptions and exit code 0:** normal title/start/new-seed flow; play/inventory advancement; five menu modes freezing time; sunrise/sunset/zenith directions; main-light alignment with sun/moon; phase-dependent moonlight; ambient/fog dimming; horizon continuity; unchanged fog range; all eight shader phase inputs; multi-day advance and new-world clock reset. [Runtime report](day-night-runtime-report.json).
- Actual game screenshots were inspected for terrain/hand visibility, sun position at sunset, stars after dusk and all eight moon silhouettes. The final polish delays stars/moon visibility until twilight, hiding the dawn phase change, and lets the moon remain readable through clouds.
- A pixel check independently confirms the rendered silhouettes: lit fractions were **0%, 15.5%, 51.8%, 88.1%, 100%, 86.3%, 50.6%, 13.9%** for phases 0–7. Waxing illumination is on the right and waning illumination on the left. The test excludes the central crosshair and compares the same moon position/FOV against new moon. The matched ground patch's mean displayed RGB values were **78.57 by day, 23.61 at full moon and 18.07 at new moon** (0–255 scale). These are screenshot brightness comparisons, not physical illumination measurements. [Pixel report and sampling method](day-night-image-check-report.json).

Screenshots below are actual player output. Phase portraits use a 35° camera FOV and hide scene geometry to review the sky at the same 22:00 position; ordinary play retains the player's FOV, terrain, hands and normal cloud motion.

![Sun setting in the west](day-night-02-sunset.png)

![Night terrain with full moon illumination](day-night-03-full-moon-landscape.png)

| Phase | Actual sky capture |
| --- | --- |
| New moon | [View](day-night-phase-0.png) |
| Waxing crescent | [View](day-night-phase-1.png) |
| First quarter | [View](day-night-phase-2.png) |
| Waxing gibbous | [View](day-night-phase-3.png) |
| Full moon | [View](day-night-phase-4.png) |
| Waning gibbous | [View](day-night-phase-5.png) |
| Last quarter | [View](day-night-phase-6.png) |
| Waning crescent | [View](day-night-phase-7.png) |

Additional captures: [noon](day-night-01-noon.png), [sunrise](day-night-05-sunrise.png), [moonless terrain](day-night-04-new-moon-landscape.png).

## Practical limits

The focused sample used seed **246813**, radius **10**, 1280×720, a **90 FPS** cap, Intel Core i7-10750H / RTX 2060 / about 32 GiB RAM. Final capture-workload frame times were **11.18ms median, 36.42ms p95 and 213.43ms maximum**, first-ready time **14.99 s** and recorded allocation peak **233,181,258 bytes**. This is a short UI/time-jump/sky-capture run on a memory-constrained machine, not an isolated lighting-cost benchmark or proof of hitch-free traversal. Draw-call capture was unavailable. Zero-valued unrelated fields in the shared report schema were not measured by this focused mode.

The 20-minute cycle, eight-night month, first full moon, opposed sun/moon orbit, apparent disk size and ambient floor are working tuning decisions. The orbit is stylized, with no seasons, eclipses or astronomical phase-dependent moonrise times. Clock state remains session-only, like world progress; durable saves and multiplayer authority are not introduced. Existing grass sky-access checks do not gain a photoperiod rule. Concurrent-feature gameplay and final art/feel acceptance still require their own review.
