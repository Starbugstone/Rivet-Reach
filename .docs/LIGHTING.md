# Cave lighting and crop light

User-requested fixes, 2026-09-13: planted crops must use nearby placed light, and covered caves must not receive outdoor illumination. These are working implementation decisions under that request. [Farming](FARMING.md) owns planting, stage times and harvests; [verification](verification/LIGHTING_RESULTS.md) records measured results and limits.

## Light access

Loaded chunks cache two independent four-bit channels per voxel: sky access and placed light. Open vertical columns receive sky level 15. Skylight spreads sideways and around cave entrances through nonopaque cells, losing one level per step. Opaque terrain blocks stop both channels. A sealed room has zero skylight; a shaft or side entrance admits light, which fades into the cave. Removing or replacing roofs and walls invalidates affected columns and neighbouring light boundaries.

Placed torches emit level 14, powered Workshop Lamps emit level 14 and lava emits level 15. Sources spread through open cells with the same one-level-per-step attenuation. Light follows an unobstructed voxel path, including around corners; it cannot pass through opaque walls. Torch brightness in rendering still uses the existing warm shadowed point lights, including the selected held torch. Held/dropped items do not become persistent growth sources. Light simulation does not depend on the camera's limited pool of visible point lights.

Crops, saplings and compost use a minimum light level of 9, taking the brighter of sky access and placed light. A torch therefore supports crops within five open-cell steps of its cell. Existing stage times, residency pauses, crop mutation budget and surface geometric-skylight growth rules remain unchanged. No irrigation, offline growth or new clock is introduced.

## Rendering

World surfaces sample the cached skylight at their actual position. Outdoor ambient illumination, environment reflections, direct sun/moon light and distance fog are reduced by cave sky access; local torch lighting and authored emission remain separate. The existing URP directional shadows still determine directional occlusion near an opening. This is bounded voxel sky access, not a simulation of physical multiple-bounce global illumination.

Terrain retains greedy meshes. A shared packed GPU buffer and chunk hash table supply per-fragment light queries without additional terrain vertices, per-block objects or per-crop lights. Original world materials and first-person surfaces retain URP's BRDF and local-light handling. Offscreen item/portrait previews retain their own presentation lighting.

## Scheduling and lifetime

Terrain edits, changed lamp state and chunk residency enqueue work. A worker solves an immutable chunk snapshot using cached column heights and six neighbouring boundary snapshots. Changed boundaries enqueue their neighbours until the light field converges. Main-thread work publishes accepted results and uploads packed data; revisions reject superseded results. New edits can briefly precede their visual lighting update. Cached artificial/indirect light is withheld from growth while its page is invalidated.

Unchanged terrain performs no light solves. Moving the sun/moon changes presentation strength without rebuilding fields. Loaded chunks, including persistent chunk-loader tickets, own the cache; unloaded pages release their GPU slots. Uniform dark/sunlit fields share CPU arrays and are represented by a single value in the GPU chunk table. Nonuniform pages use one packed byte per voxel; expanding their buffer copies existing data on the GPU. Height lookups respect saved chunk generator versions and do not register or regenerate terrain. All light data is derived and rebuilt after loading; durable-save bytes, generator versions and world content definitions are unchanged.

[Player guide](wiki/Lighting-and-underground-farms.md) explains cave openings and underground farm setup.
