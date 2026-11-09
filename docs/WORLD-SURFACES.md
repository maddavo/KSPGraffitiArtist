# KK instances and planetary terrain - 0.2.0

The spray-can editor now accepts vessel parts, Kerbal Konstructs instances and solid PQS terrain. Use the same distance-only 3 m EVA reach, text/picture selection, font/style/colour controls, I/J/K/L movement, Q/E rotation and A/D size. Right-click existing paint to edit or erase it. Apply commits; Cancel retains the saved state. Alt + right-click keeps normal interaction.

0.2.3 replaces the earlier terrain hierarchy assumptions with explicit quad owner/collider/mesh references and local-space resolution. See [Mun targeting](MUN-TARGETING.md). Vessel and KK painting are user-confirmed; native terrain results remain pending.

## Persistence and ownership

Vessel markings remain in ModuleGraffitiSurface. World markings live in a new automatically registered flight scenario, WorldPaintScenario, inside each save. The addon does not write KK instance configs, stock assets, PQS definitions or other saves.

KK markings use the instance UUID plus celestial-body name and instance-local coordinates. They follow that instance when it moves, and resolve again after unloading/reloading. An unavailable or deleted instance leaves its records in the save; they are not transferred to a similarly named instance. The optional read-only reflection adapter checks the installed KK enumeration, UUID, body and root contracts. There is no hard KK assembly dependency. UUIDs must remain stable in the KK placement data; if KK regenerates an ID, paint stays attached to the old ID rather than guessing a replacement.

Terrain patches use double-precision latitude, longitude and altitude plus body name. Each patch has a small local frame, updated from the body's current world position/rotation, avoiding storage of floating-origin scene positions or large float planet-centred anchors. Nearby clicks reuse patches within 4 m. Movement is limited to 4 m from a terrain patch origin; start another patch to paint farther away. Up to 128 world surfaces and 32 markings per surface are allowed.

Terrain geometry comes from enabled, readable MeshFilters on nearby PQS quad colliders belonging to the selected body's main PQS. A BVH projects the tessellated decal onto those visible meshes, using the existing lift and interpolated normals. The addon does not cast a physics ray for every tessellation vertex. Nearby paint is reprojected when the loaded terrain mesh identities or vertex counts change. Same-mesh vertex rewrites, custom terrain systems and modifier-specific LOD behaviour remain native compatibility risks.

World paint renders within 250 m of its frame while in flight. Unloaded/unavailable targets retain saved data. Oceans, arbitrary scenery, stock buildings not registered with KK, and unreadable meshes are not supported targets. This is a small surface decal system, not planetary texture replacement or a global terrain-editing tool.

Unknown world schemas, malformed children and duplicate anchors are retained as complete ConfigNodes. Empty cancelled patches are not saved. Existing part marking schema 1/2 support remains unchanged; world nodes use their own schema 1. Downgrading to an addon without the world scenario has not been tested: preserve a save backup before loading and saving under an older version.

## Validation

Warnings-as-errors build and 1,666 offline checks pass on the development machine: 869 record/module/world-store/coverage, 726 geometry, 27 font/material host and 44 installed API/config checks. New checks exercise the actual scenario persistence helper, double-coordinate round trips across locales, invalid data rejection, repeated-load deduplication, Apply/Cancel, missing-instance retention, unknown/duplicate record retention and the installed KK adapter signatures. These do not execute native colliders, KK activation, PQS geometry, rendering or KSP's scenario lifecycle.

In KSP, paint one KK wall and one terrain patch, check transforms and edges, Apply then quicksave/reload, leave physics range and return, and switch scenes. Move a KK instance in the editor and confirm paint follows the same UUID; remove/reload the instance and check retention without reassignment. Test terrain near a PQS seam, body rotation, floating-origin shifts and LOD changes. Check Cancel, Erase, within-reach targets despite intervening obstructions, out-of-reach rejection, text focus, boarding cleanup and vessel-part regression. Test fonts, blended edges and lighting again. Native results remain pending.

KK source reference: [StaticDatabase](https://github.com/GER-Space/Kerbal-Konstructs/blob/master/src/Core/StaticObjects/StaticDatabase.cs), [StaticInstance](https://github.com/GER-Space/Kerbal-Konstructs/blob/master/src/Core/StaticObjects/StaticInstance.cs). Runtime contract checks use the installed DLL rather than assuming upstream matches it.
