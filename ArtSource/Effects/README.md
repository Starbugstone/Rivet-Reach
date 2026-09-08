# Original arcade debris kit

Copyright © 2026 Starbugstone. All rights reserved. See [LICENSE.md](../../LICENSE.md).

[create_arcade_effect_assets.py](../../Tools/create_arcade_effect_assets.py) authors beveled stone chips, wood splinters and a curled leaf in Blender 5.2. It saves `ArcadeChips.blend`, exports the explicit Unity FBX, records triangle/material counts and renders a neutral source review. No third-party meshes, textures or effect pack were used.

The [runtime FBX](../../Assets/RivetReach/Resources/Effects/ArcadeChips.fbx) preserves three individually named meshes. The particle consumer normalizes their local bounds, selects the appropriate material family and supplies size, colour, motion and lifetime. These visual chips have no colliders or item quantities. [The presentation contract](../../.docs/CONTENT_PIPELINE.md#arcade-presentation-and-dynamic-feedback) owns runtime limits and review requirements.
