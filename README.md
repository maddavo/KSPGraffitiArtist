# KSP Graffiti Artist

Paint text and pictures directly onto vessels, planet surfaces and KK objects already in Kerbal Space Program.

An EVA Kerbal carries a spray can in KIS inventory, equips it, and right-clicks a vessel surface to create a marking. A surface preview lets the player enter text or select an image, rotate it, and adjust its size before applying it. No visible plate or separate decal part needs to be attached.

**Status: 0.2.5 supports vessel, KK and terrain painting, installed-font selection, custom PNGs and stackable KIS spray cans.** The latest KK targeting correction is user-confirmed. See the [validation notes](docs/VALIDATION.md) for offline checks and remaining test coverage.

## Why this exists

Modules and vessels can be difficult to identify after launch, particularly in a large station or surface base. Graffiti Artist is intended for labels such as `WORKSHOP`, `FUEL STORAGE`, `HAB 03`, arrows, and identification pictures placed during normal EVA gameplay.  Planet surface locations sometimes need markings, biome borders, landing sites, navigation markings, notes, plans.

The player should be able to decide what a label says at the destination. Preparing individual labelled parts before launch should not be necessary.

## In-game examples

### Vessel branding

Apply a custom image to a vessel already in service. Here, a Qantas logo follows the curved hull beside the cockpit.

![Qantas logo painted onto a rover's curved hull, with a Kerbal holding the spray can nearby.](docs/images/vessel-qantas-logo.png)

### Surface wayfinding

Combine coloured text and markings to identify biomes at a surface base. These painted markings show a tri-biome location at a Kerbal Konstructs landing pad.  Here markings are painted on the Mun surface and on a KK object.

![Yellow East Crater, Midlands and Canyons labels with dashed directional markings on and beside a landing pad.](docs/images/surface-wayfinding.jpg)

## Intended workflow

1. Carry a spray can in KIS inventory and equip it on EVA.
2. Right-click a vessel part, KK instance or solid planetary terrain within reach to open the editor at that position.
3. Enter text and choose its font, style and colour, or select an image. Inspect the preview on the surface.
4. Use **I/J/K/L** to move (up/left/down/right), **Q/E** to rotate and **A/D** to decrease/increase size.
5. Choose **Apply** to retain the marking or **Cancel** to discard the preview.
6. With the tool equipped, right-click an existing marking to edit or erase it.

While editing, movement controls must not move the Kerbal. When a text field has focus, I/J/K/L, Q/E and A/D type letters normally. Click another editor control to use placement controls. Movement follows the original surface axes independently of decal rotation, at 0.25 m/s, and stops at the selected hull edge or the 3 m reach limit.

Applied markings are intended to stay with the painted part through save/reload, scene changes, docking, and undocking. They must remain visible without the can equipped.

Font choices use installed system fonts; styles are Regular, Bold, Italic and Bold Italic. Edges now use blended alpha coverage instead of a hard cutoff. See [font and edge details](docs/FONTS-EDGES.md), including save compatibility and pending lighting checks.

World markings stay in the current save: KK paint belongs to an instance UUID; terrain paint uses body coordinates. See [world surfaces](docs/WORLD-SURFACES.md) for supported targets, limits and acceptance checks.

Reach is distance-only (3 m). Opaque glyph boxes are addressed by selecting a compiled transparent shader profile available in the installed game; see [transparent-glyph diagnosis](docs/TRANSPARENT-GLYPHS.md). Existing labels regenerate without a save edit.

User-installed Windows TTF/OTF fonts are now read from their registrations as well as Unity's list, and opening the picker refreshes the catalogue. See [Countdown investigation](docs/USER-FONTS.md). maddavo confirmed the preceding 0.2.1 correction works; this does not validate native Countdown rendering.

Vessel and KK painting are user-confirmed. Mun click rejection is addressed by resolving a terrain quad through its explicit planet owner and mesh links; see [Mun targeting](docs/MUN-TARGETING.md). Native terrain testing remains pending.

Countdown discovery is working, but the native file-font load failed in 0.2.3. The new fallback renders the actual installed font through offscreen Windows GDI and feeds transparent glyph coverage to the existing paint renderer. See [Countdown loading](docs/COUNTDOWN-LOADING.md) for real Windows pixel evidence and remaining native checks.

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
