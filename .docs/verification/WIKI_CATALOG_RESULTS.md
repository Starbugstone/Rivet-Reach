# Wiki catalog audit — 2026-09-19

The remote wiki at `33a9d8f` and maintained sources contained all **191 registered item pages**. The apparent missing recent items came from an unrestricted numeric category rule: Beds, cookers, compost machinery and textile/seed items were placed under multiblock tanks/batteries. Category selection now handles these explicitly, adds Home and storage, and provides recent-item links near the catalog opening. The catalog identifies itself as the current development build, distinct from the downloadable alpha.

All 220 maintained pages were checked for local targets. The corrected generated catalog and links pass the publisher: **220 pages, 9,450 links/images**. The read-only audit found no additional stale claims in maintained non-generated guides. Existing dated screenshots retain their original verification dates.

Potato stages 33–36 previously used generic cube icons. `CropIconBuild` now bakes the actual `ChunkMesher` plant geometry with its stage texture, transparent margins and one shared camera scale. Four current PNGs are consumed by both the in-game item browser and the wiki. Their silhouettes grow from a small sprout to the mature leaf/flower texture; no terrain geometry or growth rules changed. The Farming guide shows all four stages together. Generation was verified in the running Unity 6000.4.4f1 Editor; this visual/wiki change does not claim a new game playtest.

Live publication evidence follows deployment.
