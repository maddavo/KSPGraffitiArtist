# KSP Graffiti Artist

Paint text and pictures directly onto vessels already in service in Kerbal Space Program.

An EVA Kerbal carries a spray can in KIS inventory, equips it, and right-clicks a vessel surface to create a marking. A surface preview lets the player enter text or select an image, rotate it, and adjust its size before applying it. No visible plate or separate decal part needs to be attached.

**Status: 0.1.3 control update prepared; native validation pending.** Q/E rotation is reversed from 0.1.2 and size now uses A/D (smaller/larger). The existing orientation, colour and visible-hull rendering paths are retained; see [orientation and colour](docs/ORIENTATION-COLOUR.md).

## Why this exists

Modules and vessels can be difficult to identify after launch, particularly in a large station or surface base. Graffiti Artist is intended for labels such as `WORKSHOP`, `FUEL STORAGE`, `HAB 03`, arrows, and identification pictures placed during normal EVA gameplay.

The player should be able to decide what a label says at the destination. Preparing individual labelled parts before launch should not be necessary.

## Intended workflow

1. Carry a spray can in KIS inventory and equip it on EVA.
2. Right-click a vessel or module surface within reach to open the editor at that position.
3. Enter text or select an image and inspect its preview on the surface.
4. Use **Q/E** to rotate and **A/D** to decrease/increase size.
5. Choose **Apply** to retain the marking or **Cancel** to discard the preview.
6. With the tool equipped, right-click an existing marking to edit or erase it.

While editing, movement controls must not move the Kerbal. When a text field has focus, Q, E, W, and S must type letters normally.

Applied markings are intended to stay with the painted part through save/reload, scene changes, docking, and undocking. They must remain visible without the can equipped.

## Documentation

- [Interaction and requirements](docs/REQUIREMENTS.md): agreed behaviour, control rules, and scope.
- [Technical design](docs/DESIGN.md): proposed components, persistence, research findings, and unresolved decisions.
- [Development roadmap](docs/ROADMAP.md): staged implementation and evidence needed at each stage.
- [Validation plan](docs/VALIDATION.md): offline checks and separate in-game acceptance scenarios.

## Compatibility target

Initial target: **KSP 1.12.5 on Windows with Kerbal Inventory System (KIS)**. This is a target, not verified compatibility. The initial use case is an EVA engineer labelling existing vessels and modules.

The prototype uses its own surface-conforming meshes and part-module storage. It requires KIS (built against 1.29.8039.40483), KIS's bundled KSPDev utilities, and ModuleManager. Conformal Decals is not a dependency. Stock EVA inventory support is not implemented.

## Installation and development

See [building and using the prototype](docs/PROTOTYPE.md) for build/package commands, controls, limits, and future installation instructions. No live game files or saves were changed while preparing the candidate.

The first in-game check must investigate a marking on an already-launched curved tank, including surface alignment and save/reload persistence. See the [roadmap](docs/ROADMAP.md).
