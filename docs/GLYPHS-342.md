# Partial Eagle 1 lettering — quicksave 342

5 November 2026: Dave confirms the lighting is good, but his screenshot of Eagle 1 shows only some letters. He asks whether they are missing glyphs or below the surface.

## Read-only save and geometry checks

The latest numbered quicksave is **#342**. It contains the complete text **EAGLE 1** on vessel **Mun Eagle 1**, `mk3CrewCabin`, UID `793327480`, marking ID `f01f890bb03a44b1a20e69b7ffa96a5e`. The saved red colour, width 2.22213483 m, zero rotation and anchor are valid. No save repair is indicated.

- Save SHA-256: `0D6ACB77F281DF0CF97A89415D6AC2BF68253D54142E29FB0520E5BA164C0AA0`.
- Visible stock model: `Squad/Parts/FuelTank/mk3Fuselage/CREW.mu`, SHA-256 `7E9A3542CF88771C38DA2DE3D2932CF36420F572C27A1EE4AC0A2BC070391B57`.
- A five-row, 21-column read-only footprint comparison (105 positions, vertical offsets through ±0.24 m) finds valid visible surface. The production projector agrees with independent visible-triangle intersections within 0.1 mm at all those positions. This does not inspect actual glyph UVs or Unity depth pixels, so it does not prove that every rendered fragment is clear of the hull.

## Reproduced font-snapshot failure

The 0.1.5 coverage helper cached a white-RGB copy of the dynamic font atlas and refreshed it only when the font/texture reference changed or a textureRebuilt event invalidated it. A dynamic atlas is mutable: adding glyph pixels in place need not replace the texture or change its dimensions. Relying solely on those cache keys can retain an E-only snapshot after additional letters are requested.

A regression executable compiles the actual production FontCoverageAtlas source with CPU stand-ins for Unity readback. It first captures a resident E, adds A/G/L/1 coverage to the same source object without a rebuild event, then captures again. The old code fails at **new A must not be missing from a cached E-only snapshot**. The screenshot's repeated E shapes are consistent with this failure path. The running game's atlas itself was not sampled, so this is a reproduced defect and likely explanation, not a native pixel capture.

## Correction and validation

0.1.6 explicitly requests the label's characters before layout and reads the current atlas pixels for every text build, reusing the scratch allocation without reusing stale contents. Each owned pigment crop stays independent of later font changes. Hull placement, lift, lit material, saved labels and schema are unchanged.

15 capture/crop host assertions now pass, along with 814 prior record/coverage/adapter assertions, 726 projection/normal/orientation assertions including both local Mk3 fixtures, and 36 installed API/config checks: **1,591 total**. CPU stand-ins do not execute GPU upload/readback timing.

After installation reload #342 and inspect the existing EAGLE 1 label before repainting. Also enter a new label with previously unused letters and digits, and check lighting remains correct. Native correction remains pending.
