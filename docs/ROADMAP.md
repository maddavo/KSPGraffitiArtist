# Development roadmap

Status: 0.1.0 implementation built, updated 4 November 2026. Code for the five stages now exists, including a prototype can asset; all in-game exit evidence below remains pending. Offline evidence is recorded in [validation](VALIDATION.md). These are project milestones, not scheduled tasks or delivery dates.

## 1. Prove surface marking and persistence

Create a minimal development prototype targeting KSP 1.12.5. Demonstrate readable text and a transparent image on a curved tank belonging to a vessel launched before installation. Select a rendering and storage approach using observed results.

Exit evidence: correct surface alignment, selected-part clipping, rotation/size, save/reload, and repeated reload without duplicated markings. Use a disposable test save; distinguish a successful build from actual game validation.

## 2. Prove the KIS equipped-tool workflow

Add the inventory item and detect when the active EVA Kerbal equips it. Connect surface right-clicks to a draft preview. Resolve right-click camera-drag/menu conflicts and enforce reach and line of sight.

Exit evidence: stored/unequipped cans cannot paint; equipped cans can target a nearby part; unequip, boarding, vessel switches, and target loss cancel safely.

## 3. Implement the text editor

Provide text, colour/font selection, live preview, Q/E rotation, W/S size, and Apply/Cancel. Add existing-mark editing and erasing. Resolve overlapping-mark selection.

Exit evidence: typing does not transform paint or move the Kerbal; cancellation preserves the old marking; every exit restores normal controls.

## 4. Implement image selection

Add the local image catalogue and thumbnail gallery, transparent-image support, aspect-ratio preservation, and bounded file loading. Document supported formats, paths, and refresh/restart behaviour based on the implementation.

Exit evidence: image selection and reload work, missing/invalid files fail gracefully, and vessel/save loading remains intact.

## 5. Validate and prepare a first release

Run the [validation plan](VALIDATION.md), including docking/undocking, scene changes, part movement, and marked-part destruction. Record exact dependency/build versions and known limitations. Finish the held spray-can asset with appropriate rights and attribution.

Exit evidence: a reproducible build, installation/removal instructions, licence decisions for code and assets, and separately reported offline and in-game results. Deployment must be recorded in the local mod registry; installation requires the established KSP-closed confirmation before changing live DLLs.
