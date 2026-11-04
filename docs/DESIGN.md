# Technical design

Status: research and proposed architecture, 4 November 2026. No implementation exists. Names below describe responsibilities, not existing classes or APIs.

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

### Persistence choice remains open

Evaluate part-module storage against a scenario-level store indexed by stable part identity. Neither is selected yet. The proof must cover parts in vessels launched before the addon existed, loaded/unloaded transitions, repeated reload without duplication, docking/undocking, and removal of the target part. A vessel identifier alone is insufficient because docking changes vessel membership.

Document removal behaviour before release: what happens to stored markings if the addon is absent, and whether reinstalling can recover them. Do not promise either outcome before testing it. Installation must not require an external save-file rewrite.

## Editing transaction

1. Verify active EVA tool and a reachable, unobstructed surface hit.
2. Open a draft from a new anchor or a copy of an existing marking.
3. Update only the preview while changing text/image, orientation, and size.
4. Revalidate eligibility and target on Apply or Erase.
5. Commit the complete valid record, or delete the selected record, then update rendering.
6. On Cancel or invalidation, discard the draft and retain previous committed data.

Preview and committed rendering must use consistent geometry so Apply does not move or resize the marking unexpectedly. All exit paths must release input ownership and dispose of temporary rendering resources.

## Open implementation decisions

- Rendering strategy, dependency versions, and supported graphics/shader combinations.
- Storage mechanism and stable part identity for existing launched vessels.
- Eligibility rules, working distance, and equipped-model integration.
- Access to normal part menus while painting and overlapping-mark selection.
- Font packaging, text layout, image formats/limits, and image refresh behaviour.
- Limits on markings, texture memory, and cleanup when parts/vessels unload.
- Behaviour on animated, deployable, rescaled, or variant-switched parts; these require explicit validation rather than assumed support.

These are bounded prototype questions, not additional promised features.
