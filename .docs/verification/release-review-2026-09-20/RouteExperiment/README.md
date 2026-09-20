# Rejected explicit route renderer experiment

This isolated build tested explicit GPU instancing for existing cable/item-pipe/fluid-pipe bodies. It is **not part of the accepted source**. `build-identity.json` identifies the measured assembly. The ordinary-renderer comparison also used the experimental instancing-capable shader, so these are off/on helper comparisons, not an untouched-shader baseline.

The three alternating 600-frame pairs reduced submissions but showed no consistent p95 benefit. The later five-minute soak, storm, unload/return and save checks passed correctness while frame-time tails remained outside acceptance. GPU telemetry records thermal throttling. `release-review/performance.json` and compressed frame rows retain every sampled window; no warm/setup/capture frames are presented as gameplay samples.

The temporary renderer retained original meshes/materials, grouped by spatial region and rendering state, and restored ordinary renderers when disabled or unsupported. Frozen resource/transform and actual origin-shift checks passed. Decoded capture differences did not establish pixel equivalence. Lower submitted draw counts alone did not justify maintaining the additional renderer and lifecycle paths.

The accepted implementation instead shares the installed URP material declaration in the existing WorldLit shader. Its own compatibility checks and final native results live in `../AcceptedFinal/`; they do not inherit this experiment's results.
