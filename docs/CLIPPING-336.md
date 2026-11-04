# Eagle A label clipping — quicksave 336

Investigated 4 November 2026. The save and live installation were read only during diagnosis and build preparation; KSP was running, so no replacement DLL was installed.

## Evidence

Quicksave `#336` contains one schema-1 text marking: **EAGLE 4** on vessel **Eagle A**, part `mk3CrewCabin`, part UID `63251757`. The 2.069 m-wide label is anchored at part-local `(0.0511500724, 0.947919667, -1.8430512)` and rotated 98.79898 degrees. This establishes that the label reached the saved part; it does not establish reload behaviour.

- Save SHA-256: `6FACE938897A6071CB058D6D93427F9AEBE12E8737724E6ECAC76DE9CE9491CF`.
- Stock `Squad/Parts/FuelTank/mk3Fuselage/CREW.mu` SHA-256: `7E9A3542CF88771C38DA2DE3D2932CF36420F572C27A1EE4AC0A2BC070391B57`.
- The stock model has a visible cabin mesh and a separate simplified `Mk3COL` collision mesh.
- At 63 positions across the saved label footprint, an independent offline raycast found the visible hull **1.236–42.074 mm outside the collider**, averaging **20.328 mm**. The old code added only 2.5 mm to collider hits, consistent with Dave seeing only slivers of the label.

The model comparison uses the stock MODEL placement with child transforms and the saved label's axes. It is an offline geometric reproduction, not a captured Unity rendering.

## Correction

0.1.1 reads enabled static renderer meshes belonging to the selected part, transforms them into part-local coordinates, and projects onto those visible triangles. It excludes other parts, collision-only meshes, inactive renderers, and the paint meshes themselves. A bounding hierarchy accelerates the ray tests.

The 2.5 mm lift is now measured above the visible surface. Tessellation is reduced from 50 mm to 20 mm spacing to limit inward chords over convex surfaces. Front-facing filtering and selected-part ownership remain in place. An unreadable or absent visible mesh produces an explicit preview error, rather than silently reverting to the faulty collider projection.

## Checks and remaining validation

- 289 persistence/transaction host assertions pass unchanged.
- 270 baseline projection assertions pass. A local fixture with the actual cabin's 1,184 visible triangles adds 126 assertions: production projection agrees with independent surface intersections at all 63 saved-label samples within 0.1 mm.
- 13 installed-assembly/configuration checks pass against the new version.
- Existing schema-1 label records are not modified. The renderer regenerates them on load using the new projection path.

`Tools/diagnose_mk3_surface.py` reproduces the read-only comparison using a separately obtained `io_object_mu` reader and local installed stock model/save. It writes its report and stock-geometry fixture only into a specified local output directory. Keep generated fixtures out of Git and redistributable packages.

After installation, reload #336 and inspect the existing label before repainting. Confirm complete glyph visibility, close/oblique views, preview versus Apply, and save/reload. Those corrected gameplay results remain pending.
