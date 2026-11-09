# Changelog

## 0.2.5 - first public packaged release, 10 November 2026

- Publish the current working 0.2.5 feature set with an MIT licence, included in the release archive.
- Update installation, dependencies, controls, fonts, custom PNG limits and compatibility documentation. No plugin behaviour changes in this release preparation.

## 0.2.5 - local surface targeting, 8 November 2026

- Exclude distant scaled-space planet colliders from the local paint ray. Actual rejected KK clicks logged Mun on layer 10 (Scaled Scenery); nearby local obstacles still own clicks and the 3 m EVA reach remains unchanged.
- Include the pending KIS spray-can stacking configuration. Build and 1,722 local checks pass, including scaled-planet/KK/terrain picking fixtures and installed editor integration. No save-format changes. Native KK/terrain targeting and inventory validation remain pending.

## 0.2.4 + stackable can configuration - 8 November 2026

- Enable KIS inventory stacking for the spray can, matching KIS hand tools. Plugin version and painting behaviour are unchanged. Inventory stacking/equipping validation remains pending in game.

## 0.2.4 - Countdown dynamic-load fallback, 8 November 2026

- Handle registered file fonts that do not become dynamic Unity fonts. Normalize the native path and use offscreen Windows glyph coverage when needed, verifying the actual selected family instead of silently using Arial.
- Preserve font/style/text data, centred multiline layout, pigment, fractional alpha edges and the existing lit surface projection. Bound bitmap sizes and release native resources; add no managed DLLs or font-registration changes.
- Build and 1,715 checks pass, including 16 real Windows raster checks against Countdown. KSP texture/rendering integration remains pending.

## 0.2.3 - explicit terrain quad ownership, 7 November 2026

- Replace transform-parent ownership checks with exact PQ sphereRoot/collider matching and read-only local-space quad resolution; obtain projection meshes from the resolved quad's meshFilter.
- Log rejected target details to aid native terrain diagnosis. Retain distance-only reach, save formats and the pending 0.2.2 Countdown update.
- Build and 1,695 local checks pass. maddavo confirms vessel/KK painting works; native Mun targeting and Countdown rendering remain pending.

## 0.2.2 - user-installed font discovery, 7 November 2026

- Supplement Unity font enumeration with read-only per-user Windows font registrations and internal TTF/OTF family names, including the registered Countdown OpenType file.
- Load supplemented families from their registered paths; refresh the catalogue whenever the picker opens. Preserve saved family/style data and avoid system font installation or redistribution.
- Build and 1,683 local checks pass; native Countdown rendering pending. maddavo confirmed the preceding 0.2.1 fix works.

## 0.2.1 - distance-only reach and transparent glyphs, 6 November 2026

- Remove Kerbal line-of-sight and collision-proximity checks; retain only valid state and the 3 m distance limit for all targets/actions.
- Diagnose intact test vessel text in test save and missing compiled Standard Fade/highlight-off variants. Use installed Standard (Specular setup) Transparent mode with black specular reflectivity and no unsupported keywords; preserve smooth alpha edges and requested shadows.
- Add an optional read-only multi-segment compiled shader audit. Build and 1,677 local checks pass; native glyph/lighting confirmation pending. No save edits.

## 0.2.0 - KK and terrain surfaces, 6 November 2026

- Add optional read-only KK UUID/body/root resolution and selected-instance mesh projection.
- Add solid PQS terrain patches using double body coordinates, small local frames, nearby readable quad meshes and mesh-change reprojection.
- Store world markings in a save-local scenario with tested persistence and retention of unavailable/unknown/duplicate records. Vessel part marking records are unchanged.
- Reuse the existing spray-can editor, reach checks and controls for all targets. Build and 1,666 offline checks pass; native world placement and persistence remain pending.

## 0.1.8 - fonts, styles and blended edges, 6 November 2026

- Add installed-font selection and Regular, Bold, Italic and Bold Italic styles, including existing-label editing and missing-font fallback without changing the saved selection.
- Read schema 1 as Regular; save font/style with schema 2 so older addon versions retain unsupported records safely.
- Replace the hard alpha cutoff with stock Standard lit Fade blending and bilinear coverage filtering. Keep matte/no-emission settings, received shadows and interpolated hull normals. Native edge quality, lighting and transparency ordering require gameplay validation.
- Build and 1,636 offline checks pass; no game saves edited.

## 0.1.7 - surface movement controls, 5 November 2026

- Add I/J/K/L for up/left/down/right movement while creating or editing text and pictures. Keep Q/E rotation and A/D sizing; transform keys type normally in the text field.
- Move at 0.25 m/s along the original placement axes, independent of decal rotation; normalise diagonal movement and cap a frame's step to avoid jumps after a stall. Reproject onto the selected visible hull and reject steps beyond its edge or the Kerbal's reach without losing the current draft.
- Apply saves the moved anchor through the existing marking schema; Cancel leaves the original marking unchanged. Existing glyph and lit-paint paths retained. Native movement, edge/reach limits and save/reload remain pending gameplay.
## 0.1.6 — fresh glyph capture, 5 November 2026

- Explicitly request the label's characters before layout and capture current font atlas pixels for every text build. Do not treat texture identity, size or textureRebuilt as a pixel-content version: fonts can add letters in place.
- Reuse the scratch texture allocation while refreshing its pixels. Owned pigment crops remain independent of later font updates. Keep lighting, placement offset, records and schema unchanged.
- Reproduced the stale E-only capture with production atlas code and added a passing regression; added read-only test save test vessel hull samples. Native missing-letter correction pending installation/testing. maddavo confirmed 0.1.5 lighting is good.

## 0.1.5 — lit surface paint, 5 November 2026

- Replace unlit sprites with the stock KSP/Alpha/Cutoff diffuse part shader. Enable received shadows and retain hull-only shadow casting, so paint no longer intentionally maintains full brightness in shadow.
- Interpolate the visible hull's vertex normals onto paint samples instead of recalculating flat per-cell normals.
- Bake chosen text pigment into a cropped font-coverage texture and remap glyph UVs; do not rely on a material tint property the stock shader may lack. PNG RGB is retained.
- Transparent backgrounds use alpha cutout at 0.15; partially transparent pixels above the threshold are opaque lit paint, not soft blending. Existing markings/schema unchanged. Native light/shadow appearance pending validation.

## 0.1.4 — safe part-menu lifecycle, 4 November 2026

- Stop destroying stock PAW GameObjects. Use the stock controller's dismissal/recycling path so its window lists and mod cleanup events stay consistent. Pinned windows are retained; Alt still leaves normal part-menu interaction available.
- Explicitly close the paint editor and release its named input lock on boarding, vessel change, component disable and scene teardown. Recheck EVA/tool eligibility before late-frame menu handling.
- Includes the prepared 0.1.3 control changes (reversed Q/E, A/D width). No save schema or marking changes. Native paint/board/right-click validation pending.

## 0.1.3 — editor controls update, 4 November 2026

- Reverse Q/E rotation direction at maddavo's request. Q now increases the stored angle; E decreases it.
- Move size controls from W/S to A/D: A decreases width and D increases it. Update editor help, part description and usage documentation.
- Preserve text-entry focus behaviour, transform speeds, saved markings and schema. Native control validation pending installation.

## 0.1.2 — orientation and colour correction, 4 November 2026

- Fix horizontally mirrored text and pictures by deriving the right tangent from outward-normal cross up. Preserve saved position, rotation, scale and content.
- Extract font alpha into a cached white-RGB coverage atlas, so chosen text colour is not multiplied by font-atlas RGB. Explicitly set mesh vertex colours to white. Keep pictures' original RGB and depth-tested surface rendering.
- Reverse triangle winding for the corrected basis, retaining outward mesh normals. No save schema changes or save edits.
- Add front/back and rotated-frame orientation regressions plus coverage/colour tests. Native corrected lettering and colour still require game validation.

## 0.1.1 — clipping correction candidate, 4 November 2026

- Project text and pictures onto the selected part's actual visible mesh instead of its collision hull. test save's Test Vessel/Mk3 label reproduced an average 20 mm hull/collider mismatch against the old 2.5 mm lift.
- Add spatially accelerated visible-triangle projection and finer 20 mm tessellation. Keep a small lift above the visible surface; do not inflate every label away from the hull.
- Preserve the existing record schema and saved markings. No save edits or repainting required by the data format.
- Add managed projection regressions and an optional local stock-Mk3 fixture. Corrected native appearance remains pending installation/testing.

## 0.1.0 — prototype candidate, 4 November 2026

- Implemented the KIS-equipped spray-can workflow, flight text/PNG editor, surface-conforming preview, Q/E rotation, W/S width, and Apply/Cancel/Edit/Erase.
- Added per-part persistence with bounded versioned records, transaction isolation, and preservation of unrecognized data.
- Added an original held can model, colour textures, and transparent arrow example.
- Added build/package scripts and offline tests. Installation, native rendering, KIS interaction, and gameplay persistence remain pending.
