# Validation plan

Status: offline validation completed for the 0.1.6 candidate, 5 November 2026. maddavo confirmed lighting is good in 0.1.5. The missing-letter regression in the font snapshot path is reproduced before the correction and passing after it; native label completeness remains pending. See [glyph-refresh regression](GLYPH-REFRESH.md).

## 0.2.4 Countdown load/fallback evidence

Build and 1,715 local checks pass: 869 record/world/module/coverage, 726 geometry, 37 font/material host, 48 API/config, 9 compiled-shader audit, 10 terrain resolver and 16 real Windows font-raster checks. Countdown is rendered by the actual production GDI code in all four styles with verified face identity and transparent antialiased coverage. Unity texture upload, final projection/lighting and native Mun targeting remain pending. See [Countdown loading](COUNTDOWN-LOADING.md).

## 0.2.3 terrain resolver evidence

Build and 1,695 local checks pass: 869 record/world/module/coverage, 726 geometry, 34 font/material host, 47 installed API/config, 9 compiled-shader audit and 10 production terrain-resolver host checks. New tests cover explicit ownership, detached quads, geometry links, incorrect targets and replacement handling. Native Mun targeting and terrain rendering remain pending; see [Mun targeting](MUN-TARGETING.md).

## 0.2.2 user-font evidence

Build and 1,683 local checks pass, including the actual Countdown registration/name parser and a host test routing it to its registered file while native enumeration omits it. The 34 font/material tests use explicit engine stand-ins; native Countdown/CFF rendering is pending. See [user fonts](USER-FONTS.md).

## 0.2.1 reach and compiled shader evidence

Build and 1,677 local checks pass (1,668 normal checks plus 9 optional installed compiled-shader checks). The new material test fails under the old Fade settings. Installed shader program tables verify that the old requested variants are absent and the replacement material/shadow profiles exist on both platforms. No native render or font-atlas capture is implied; see [transparent-glyph diagnosis](TRANSPARENT-GLYPHS.md).

## 0.2.0 world-surface evidence

Build and 1,666 offline checks pass: 869 record/world-store/module/coverage, 726 geometry, 27 font/material host checks and 44 installed assembly/config checks. The actual world persistence helper and installed KK adapter contracts are checked. Native KK/PQS placement, reloads, floating-origin/LOD handling and rendering remain pending; see [acceptance checks](WORLD-SURFACES.md).

## 0.1.8 font/style and edge evidence

Build and 1,636 offline checks pass: 847 record/store/module/coverage, 726 geometry, 27 font-capture/font-catalogue/material host checks and 36 installed assembly/config checks. The tests preserve legacy defaults and font/style metadata and inspect the actual Fade material configuration through explicit host stand-ins. They do not render fonts, shaders or shadows. See [native acceptance](FONTS-EDGES.md).

## Completed offline evidence (0.1.6)

- Compiles against the installed KSP 1.12.5 and KIS 1.29.8039.40483 assemblies with compiler warnings treated as errors.
- 814 assertions execute the production record codec, marking collection, surface module, font/pigment conversion and PAW adapter. They include every alpha value's preservation under coloured pigment. PAW/menu operations use explicit host stand-ins; these do not execute KSP's actual UI lifecycle.
- 36 checks inspect installed assemblies/candidate IL and parse the can configuration with KSP's real ConfigNode parser. Alongside prior menu/material checks they verify explicit glyph requests. No Unity scene, shader execution or KIS interaction is executed.
- 390 baseline assertions execute the production projection/orientation kernel. Local Mk3 fixtures add 126 assertions at 63 test save positions and 210 assertions at 105 test save test vessel positions (726 total on maddavo's machine). Stock geometry is not bundled in the repository or package.
- 15 assertions execute the actual production FontCoverageAtlas capture/crop code against explicit CPU readback stand-ins. In-place glyph additions with unchanged texture reference/size and no rebuild event fail under the old code and pass under 0.1.6. Tests also cover pigment/alpha, independent owned crops, resizing, target restoration and teardown. GPU timing is not simulated. Total offline checks on maddavo's machine: 1,591.
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
| Reach | Within 3 m is accepted regardless of Kerbal line of sight; distant or invalid targets are rejected |
| Projection | Paint stays on the selected part, without bleeding onto a neighbouring part or through the far wall |
| Camera and UI | Right-drag still moves the camera; clicking editor/UI controls does not paint the hull behind them |
| Keyboard focus | I/J/K/L move, Q/E rotate and A/D resize outside text input; inside text input they type letters; the Kerbal does not move while editing |
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

## 0.1.7 placement acceptance

- With text and pictures, hold I/J/K/L individually and diagonally. Verify up/left/down/right relative to the original placement axes, unchanged by Q/E rotation. Check both sides of a vessel and a curved hull.
- Try moving off the selected part and beyond 3 m reach: the last valid draft must remain editable; no jump to another part.
- Verify text focus types IJKL normally, the Kerbal stays still while editing, and boarding releases controls.
- Move an existing marking, Cancel and confirm the original position. Repeat with Apply, save/reload and confirm the new position.
- Confirm the complete LABEL 1 lettering, colour and lit/shadow appearance are retained. Offline build/API checks do not establish these native results.
