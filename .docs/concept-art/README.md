# Rivet Reach — current art references

These generated 2D sheets are the retained character and equipment art direction, not runtime screenshots or usable model/skin assets. Superseded world/UI mockups are preserved in Git history; [current Unity screenshots](../verification/README.md) now show the implemented game, including functional crafting.

## Player reference

![Male and female player turnaround reference](player-male-female-turnaround-v3.png)

Both models share practical workwear and support changeable skins. The longer tied-back female hair and balanced front/back coverage reflect the selected concept revision. Appearance remains separate from gameplay dimensions and abilities. [Current model and hand evidence](../verification/AVATAR_REWORK_RESULTS.md) records actual Blender renders, Unity imports, measured geometry and remaining artistic review.

## Equipment direction

![Equipment art direction](player-tools-weapons-v1.png)

The pickaxe, axe and sword guide the original equipment's silhouette and grip direction. The crossbow remains a proposal. Both models can use all implemented equipment; this illustration does not define recipes, weapon balance or additional scope. [CONTENT_PIPELINE.md](../CONTENT_PIPELINE.md) owns asset/skin constraints and [GAMEPLAY.md](../GAMEPLAY.md) owns behavior.

## Provenance

Created on 2026-09-08 with the built-in image-generation tool using the conversation-generated character reference; no external artist image was supplied. [The retained generation prompts](PROMPTS.md) record instructions for these two sheets. They remain under the repository's [ownership notice](../../LICENSE.md).

PNG files use Git LFS; run `git lfs pull` after cloning. These references belong in documentation, outside Unity's runtime asset tree. Runtime art must be authored and checked separately with the project's Blender/Unity workflow.
