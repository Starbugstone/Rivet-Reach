---
name: blender-game-art
description: Author, refine and visually verify Blender game assets against approved concept art, then check their Unity imports. Use for actual model or asset work and visual-quality fixes.
---

# Blender game art

Use the approved art as the visual target. Inspect it before modeling and identify the shapes that carry its identity: proportions, silhouette, face, hair, clothing construction and material separation. A low triangle count does not establish visual quality. Do not treat an arrangement of primitive proxies as an accepted finished character.

Use the available `unity-blender-local` skill for application discovery, background execution and preservation of existing editor work. Work in a separate background Blender process when authoring/export scripts can complete the task; this does not require closing the user's interactive scene or a live MCP bridge.

## Author and compare

Prefer coherent shaped topology for visible anatomy and clothing. Use procedural geometry where useful, but inspect its actual output. Check garment intersections from the back as well as the front, hair silhouette, facial expression, hand proportions and the intended first-person pose. Detail that breaks the silhouette or clips through another surface needs correction before delivery.

Render the actual source meshes with readable neutral lighting. Compare those renders directly with the approved sheet, then inspect the imported model under the game's lighting and camera. Generated concept illustrations are references, never evidence that a runtime asset matches them. Report any remaining mismatch and leave artistic acceptance to the user; continue routine fixes without requesting intermediate approvals.

## Preserve the asset contract

Read the project's content pipeline before export. Preserve supported skeleton names, skin region semantics, units, origin and Unity `.meta` identities. Save editable sources and reproducible export scripts. Use relative texture references in `.blend` files and explicit exports for Unity so playing the game does not require Blender.

When joining meshes, normalize the active UV-layer name across every part first. Blender primitives and custom meshes can otherwise retain separate UV layers, silently making some parts sample the wrong palette region after joining or export. Inspect eyes, hands, boots and other small parts in the final render, not only the unjoined objects.

Check winding, normals, bounds, material slots and bone weights after export. Preserve the render texture’s aspect ratio in UI portraits: crop the studio background or fit the image rather than stretching the character into a differently shaped panel. Count triangles/materials/bones on the imported asset as well as the source; measure actual first-person and full-body costs where they differ. Claim performance only for the measured workload.

## Rivet Reach entry points

Within the Rivet Reach repository, read `.docs/CONTENT_PIPELINE.md` and the current files in `.docs/concept-art/`. `Tools/create_player_assets.py` authors the player sources and FBX/skin outputs; `Tools/render_player_review.py` renders their actual front/back appearance. The scripts run through Blender's `--background --python` CLI with an absolute script path. `Tools/Build-Windows.ps1` and `Tools/Verify-POC.ps1` build and exercise the Unity import. Review the scripts before rerunning them over existing assets.

Keep this skill's repository copy in `.docs/skills/blender-game-art/SKILL.md` synchronized with the installed copy in `~/.codex/skills/blender-game-art/SKILL.md`. Follow the project's existing commit/push authorization for completed modifications.
