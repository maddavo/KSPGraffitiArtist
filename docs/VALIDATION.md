# Validation plan

Status: offline validation completed for the 0.1.1 candidate, 4 November 2026. Dave used 0.1.0 and saved an Eagle A label in #336; its poor visibility is reproduced offline. Corrected in-game appearance and the remaining acceptance scenarios are pending.

## Completed offline evidence

- Compiles against the installed KSP 1.12.5 and KIS 1.29.8039.40483 assemblies with compiler warnings treated as errors.
- 289 assertions execute the production record codec, marking collection, and surface module source. They cover Unicode/newlines, invariant-culture/randomized round-trips, bad-data bounds, missing-image references, draft isolation, failed applies, capacity, erase, repeated load, and retention of unknown records. The module is hosted with explicit ConfigNode/Unity stand-ins; this does not test the stock loader.
- 13 checks inspect the actual installed assemblies and candidate DLL, and parse the can configuration using KSP's real ConfigNode parser. No Unity scene or KIS interaction is executed.
- 270 baseline assertions execute the production triangle-projection kernel, including collider-versus-visible-hull separation, nearest/front-facing hits, holes, bounds, hierarchy traversal, and convex curved hull clearance. A locally generated read-only Mk3/#336 fixture adds 126 assertions at 63 label-area positions (396 total projection assertions on Dave's machine). Stock geometry is not bundled in the repository or package. See [diagnosis](CLIPPING-336.md) for dimensions and hashes.
- Original can model independently parsed with the `io_object_mu` reader: six meshes, 804 vertices, four materials, four textures; 768 outward-facing triangles checked. The final model also contains a box collider. This is format/geometry evidence, not proof of loading or held orientation in KSP.

`build.ps1`, `test.ps1`, and `package.ps1` reproduce the build, persistence/API checks, and candidate package. Package receipts retain artifact/dependency hashes and the source revision.

Record the tested plugin revision, KSP/KIS versions, graphics configuration, result, and relevant logs or screenshots. Use a disposable career/sandbox and existing launched test vessels; do not modify a live career to manufacture a passing result.

## Offline checks once implementation exists

- Build against the targeted game and KIS assemblies and check required API signatures.
- Round-trip marking records, including multiline text, Unicode, rotations, image references, and schema versions.
- Reject malformed records and non-finite/out-of-range sizes without losing other valid markings.
- Verify draft cancellation and failed commits leave existing records intact.
- Validate image limits, relative path containment, missing files, and resource cleanup where testable outside Unity.

These checks cannot prove shader appearance, equipped-tool behaviour, native input handling, or game persistence.

## In-game acceptance scenarios

| Area | Scenario and expected result |
| --- | --- |
| Existing vessels | Visit a vessel launched before the addon was installed; paint a module without editing its save externally |
| Inventory gating | A stored can does nothing; equipping on the active EVA Kerbal enables painting; unequipping disables it |
| Surface placement | Right-click a reachable flat panel and curved tank; previews align with each selected surface |
| Occlusion/reach | A blocked, distant, or invalid target cannot receive paint |
| Projection | Paint stays on the selected part, without bleeding onto a neighbouring part or through the far wall |
| Camera and UI | Right-drag still moves the camera; clicking editor/UI controls does not paint the hull behind them |
| Keyboard focus | Q/E rotate and W/S resize outside text input; inside text input they type letters; the Kerbal does not move while editing |
| New marking | Apply retains exactly the preview; Cancel/Escape creates no record |
| Existing marking | Editing then Cancel retains the old marking; Apply replaces it; Erase removes only the selected marking |
| Overlap | Closely overlapping markings can be selected individually using the documented selection UI |
| Text | Labels remain readable at useful sizes; colour/font choices and entered characters survive reload |
| Images | Transparent PNG candidate and other selected supported formats preserve aspect ratio and render without an unwanted plate |
| Missing assets | Removing or renaming a selected image does not prevent vessel/save loading; reference remains recoverable |
| Reload | Save, reload, switch scenes, and leave/return to physics range; markings persist exactly once |
| Dock/undock | Painted modules retain their own markings as vessel membership changes |
| Part movement | Relocate a marked part through a supported construction workflow; paint follows it |
| Part destruction | Destroy the marked part; no floating paint or stale rendering remains |
| Exit paths | Unequip, board, change vessel, leave flight, or lose the target during editing; no partial commit or stuck controls |
| Repeated use | Repeated preview/apply/edit/erase and vessel unload cycles do not accumulate renderers, textures, or input locks |
| Addon absence | In a disposable save, test removal/reinstallation and document actual marking-data retention and loading behaviour |

Animated, rescaled, deployable, and variant-switched parts need additional explicit checks before being listed as supported. Compatibility with stock EVA inventory or other rendering mods must not be inferred from KIS success.
