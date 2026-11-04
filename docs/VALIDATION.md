# Validation plan

Status: planned only. None of these scenarios has been executed for Graffiti Artist. Documentation checks are not plugin tests.

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
