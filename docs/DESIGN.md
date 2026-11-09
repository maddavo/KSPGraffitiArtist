# Technical design

Status: prototype architecture, updated 5 November 2026. maddavo confirmed 0.1.5 lighting is good. 0.1.6 corrects a reproduced stale-glyph capture path; native missing-letter validation remains pending.

## Current implementation

0.2.4 caches a Windows-coverage route for registered fonts whose native file load is non-dynamic. WindowsTextRasterizer verifies the chosen face and produces bottom-first grayscale alpha; PaintVisual adds transparent padding/pigment and projects it through the existing lit mesh pipeline. No extra managed font library or font installation is required.

0.2.3 shares TerrainSurfaces between target detection and visible-mesh collection. Ownership uses PQ.sphereRoot and exact meshCollider identity, with a read-only local-space quad-list fallback; meshFilter comes from the resolved quad. A PQS transform ancestor is not required.

0.2.2 adds UserFonts for read-only Windows per-user registrations and bounded internal TTF/OTF family-name parsing. PaintFonts merges those families into the catalogue and loads registered paths through Font(string). Font-family save data is unchanged. See [Countdown investigation](USER-FONTS.md).

0.2.0 introduces PaintTarget adapters for parts, KK roots and terrain patch frames, a save-local WorldPaintScenario/WorldPaintStore, and optional read-only KK UUID resolution. VisibleSurface projects onto target-owned meshes. See [world surfaces](WORLD-SURFACES.md).

0.1.8 adds installed-font selection, saved styles, legacy schema-1 loading and schema-2 records, plus bilinear blended edges. See [fonts and edges](FONTS-EDGES.md). Native appearance remains pending.

- `PaintTool` queries the active EVA Kerbal's actual KIS inventories and equipped `gaSprayCan` item. It distinguishes a short right-click from camera dragging, provides the flight editor, owns the EVA/control lock during editing, and rechecks a distance-only 3 m reach before changes.
- `PaintVisual` tessellates text glyphs or image rectangles at up to 20 mm sample spacing. In 0.1.1, `VisibleSurface` collects enabled readable renderer meshes owned by the target part, excludes paint meshes, and uses their model-to-part transforms. `SurfaceGeometry` intersects these triangles with a spatial hierarchy, rejects back faces, and applies a 2.5 mm lift above the visible triangle. It never uses collision geometry as a rendering fallback. Missing samples are clipped. Rendering now uses the stock Standard (Specular setup) lit shader in Transparent mode described below. No physical marking part is created.
- `ModuleGraffitiSurface` is added through ModuleManager to eligible part prefabs. It serializes committed markings inside that part's module, preserving unknown records for recovery. Existing launched vessels should receive the added prefab module through KSP's loader; this remains a required game test.
- `MarkingData` supplies a versioned invariant-culture codec with base64 UTF-8 text and validated relative PNG names. `MarkingCollection` copies drafts on commit and returns copied snapshots, separating previews from saved data.
- The held can uses original generated geometry and colour textures. No upstream mod asset or renderer source is bundled.
- The horizontal tangent is `outward normal × up`, giving camera-right when viewed from outside. Each text visual owns a cropped pigment-coloured region and remaps glyph UVs, avoiding reliance on a stock-shader tint property. In 0.1.6 glyphs are requested before layout and font atlas pixels are recaptured for every text build, reusing the scratch allocation but not stale contents. Texture identity/size and rebuild events are not treated as content versions. Pictures retain their RGB. Stored anchors, rotations and colour values are not rewritten.
- `PaintMaterial` selects Unity Standard (Specular setup) in Transparent mode: shader-premultiplied RGB with One/OneMinusSrcAlpha blending, no alpha test or depth writes, queue 3001, matte pigment and received shadows enabled. Paint does not cast a separate shadow; the hull already does. `SurfaceGeometry` retains triangle normals for offset/front-face filtering and returns separately interpolated vertex normals for shading. `PaintVisual` supplies those normals without flat-cell recalculation. No emission or unlit fallback is configured. Part normal maps/PBR properties are not inherited.

The following sections distinguish source research from implementation choices and native validation still needed.

## Proposed components

| Component | Responsibility |
| --- | --- |
| KIS spray-can item | Inventory representation, held model, and equipped-tool eligibility |
| EVA interaction controller | Surface targeting, reach checks, right-click versus camera-drag handling, and input ownership |
| Marking editor | Text/image selection, colour/font controls, preview rotation/size, Apply/Cancel/Erase |
| Surface renderer | Generate content and project it onto only the selected part |
| Persistent marking store | Save committed records, restore renderers, and associate each record with its original part |
| Image catalogue | Discover supported local images, provide thumbnails, and handle missing/invalid files |

The visible experience is paint. Persistence may use metadata associated with a part; it must not require a separate physical marking part or a player-managed plate.

## Tool integration

KIS is the initial integration target. Its `ModuleKISItem` exposes equipment configuration and `OnEquip`/`OnUnEquip` callbacks. The inspected source warns that equipment callbacks can run on a prefab module, and model-only equipment may have no live part. Do not assume that callbacks come from a normal spawned tool part.

The prototype must inspect the supported installed KIS version and prove eligibility follows the currently active EVA Kerbal's actual equipped can. Polling or callbacks may be used after evaluation. Unequipping, boarding, switching vessels, and scene changes must end editing and release controls.

Do not replace KIS or customize its DLL. Any use of its models or other assets needs a separate licence review; the existence of an installed screwdriver model does not grant redistribution rights for a spray-can model.

Reference: [KIS ModuleKISItem source](https://github.com/ihsoft/KIS/blob/master/Source/ModuleKISItem.cs).

## Rendering investigation

Conformal Decals demonstrates text/image projection onto KSP parts, but its normal editor workflow is not this tool's interaction model.

Source inspected during initial research:

- [ModuleConformalDecal](https://git.offworld.city/drewcassidy/KSP-Conformal-Decals/src/branch/main/Source/ConformalDecals/ModuleConformalDecal.cs): flight selection is disabled by default; a decal that starts in flight without a parent destroys itself.
- [ModuleConformalText](https://git.offworld.city/drewcassidy/KSP-Conformal-Decals/src/branch/main/Source/ConformalDecals/ModuleConformalText.cs): text-editing events are configured for the editor rather than flight.
- [Text part configuration](https://git.offworld.city/drewcassidy/KSP-Conformal-Decals/src/branch/main/GameData/ConformalDecals/Parts/decal-text.cfg): the inspected configuration does not include the stock cargo module.

These observations explain why a cargo configuration change alone is not proof of EVA compatibility. They do not establish a tested KIS incompatibility, nor do they select Conformal Decals as a dependency. References track upstream and may change; pin the evaluated revision during implementation.

Evaluate a projector/shader approach or a surface-conforming mesh against the curved-tank requirement. Compare clipping, front/back rejection, lighting, readable text, transparent images, and performance. Choose the approach only after a running proof of concept. Shader code/assets reused from another project require licence and attribution compliance; no such material is included here.

## Proposed data model

Each committed marking needs a stable marking identifier, schema version, target-part association, surface anchor, orientation, size, and content definition. The anchor should be stored relative to the appropriate part/model transform, not in world coordinates.

Text records need content, font identity, and colour. Image records need a portable relative asset identifier and any chosen appearance settings. Store rotation and size explicitly, preserve image aspect ratio, and validate lengths and numeric ranges when loading.

Do not persist transient previews, UI focus, input locks, or temporary render objects. Image paths must resolve inside the designated image folder; arbitrary paths and executable content are not image inputs.

### Persistence choice and validation

Part-module storage was selected to avoid vessel-identifier remapping during docking. There is no scenario-wide vessel lookup. Offline host tests cover repeated record loading and transaction semantics. Actual KSP tests must still cover vessels launched before the addon existed, loaded/unloaded transitions, docking/undocking, and removal/movement of the target part.

Document removal behaviour before release: what happens to stored markings if the addon is absent, and whether reinstalling can recover them. Do not promise either outcome before testing it. Installation must not require an external save-file rewrite.

## Editing transaction

1. Verify active EVA tool and a reachable, unobstructed surface hit.
2. Open a draft from a new anchor or a copy of an existing marking.
3. Update only the preview while changing text/image, orientation, and size.
4. Revalidate eligibility and target on Apply or Erase.
5. Commit the complete valid record, or delete the selected record, then update rendering.
6. On Cancel or invalidation, discard the draft and retain previous committed data.

Preview and committed rendering must use consistent geometry so Apply does not move or resize the marking unexpectedly. All exit paths must release input ownership and dispose of temporary rendering resources.

## Decisions and remaining investigation

- Surface-conforming mesh rendering is selected. 0.1.0's collider projection was reproduced below the Mk3 hull; 0.1.1 follows visible triangles. Actual corrected graphics/shader appearance still needs game validation.
- Part-module storage is selected; existing-vessel loading and KIS construction/storage transfers need game validation.
- The initial tool is usable by any EVA Kerbal who can equip it, with 3 m reach; engineer-only restrictions are not imposed in this prototype.
- Alt + right-click keeps normal part-menu access. The editor lists markings on the target part for deterministic overlap selection.
- The initial font is built-in Arial. PNG selection supports refresh without restarting. See [prototype limits](PROTOTYPE.md) for bounds and memory budget.
- Renderer/texture disposal is implemented; actual repeated-use memory behaviour needs game validation.
- Behaviour on animated, deployable, rescaled, or variant-switched parts; these require explicit validation rather than assumed support.

These are bounded prototype questions, not additional promised features.

### Placement movement (0.1.7)

I/J/K/L translates the draft at 0.25 m/s along its original unrotated placement frame. Each proposed step is projected onto only the selected part's visible mesh within 0.1 m depth and checked with the existing distance-only reach rule. Invalid steps restore the previous anchor. The movement mesh is cached for the editor session and released on Close. Apply persists the updated local anchor; Cancel retains the original saved record. Movement shares the text-focus and EVA input-lock rules with rotation and sizing.
