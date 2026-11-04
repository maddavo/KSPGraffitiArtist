# Changelog

## 0.1.2 — orientation and colour correction, 4 November 2026

- Fix horizontally mirrored text and pictures by deriving the right tangent from outward-normal cross up. Preserve saved position, rotation, scale and content.
- Extract font alpha into a cached white-RGB coverage atlas, so chosen text colour is not multiplied by font-atlas RGB. Explicitly set mesh vertex colours to white. Keep pictures' original RGB and depth-tested surface rendering.
- Reverse triangle winding for the corrected basis, retaining outward mesh normals. No save schema changes or save edits.
- Add front/back and rotated-frame orientation regressions plus coverage/colour tests. Native corrected lettering and colour still require game validation.

## 0.1.1 — clipping correction candidate, 4 November 2026

- Project text and pictures onto the selected part's actual visible mesh instead of its collision hull. Quicksave #336's Eagle A/Mk3 label reproduced an average 20 mm hull/collider mismatch against the old 2.5 mm lift.
- Add spatially accelerated visible-triangle projection and finer 20 mm tessellation. Keep a small lift above the visible surface; do not inflate every label away from the hull.
- Preserve the existing record schema and saved markings. No save edits or repainting required by the data format.
- Add managed projection regressions and an optional local stock-Mk3 fixture. Corrected native appearance remains pending installation/testing.

## 0.1.0 — prototype candidate, 4 November 2026

- Implemented the KIS-equipped spray-can workflow, flight text/PNG editor, surface-conforming preview, Q/E rotation, W/S width, and Apply/Cancel/Edit/Erase.
- Added per-part persistence with bounded versioned records, transaction isolation, and preservation of unrecognized data.
- Added an original held can model, colour textures, and transparent arrow example.
- Added build/package scripts and offline tests. Installation, native rendering, KIS interaction, and gameplay persistence remain pending.
