# Building and using prototype 0.1.4

0.1.4 replaces unsafe PAW object destruction with normal stock dismissal/recycling and adds boarding/vessel-change/disable cleanup. See [the PAW diagnosis](PAW-BOARDING.md). Native paint/board/right-click validation is pending; restart KSP for installation to discard existing damaged menu state.

Includes 0.1.3's reversed Q/E and A/D sizing: A smaller, D larger. No stored rotation values are rewritten.

0.1.1 made Eagle A's label visible, confirming progress on the hull-clipping correction, but the user screenshot showed mirrored lettering and black text where red was intended. 0.1.2 corrects the horizontal frame and font-colour path; see [the diagnosis](ORIENTATION-COLOUR.md). Existing schema-1 labels are retained and regenerated on load; no save migration or repaint is required by the data format. Native corrected appearance and colour still need a game check.

## Build and package

Run from the repository in Windows PowerShell:

```powershell
.\build.ps1
.\test.ps1
.\package.ps1
```

Scripts default to the Steam KSP installation and accept `-KspRoot` for another installation. Building requires the .NET Framework C# compiler, KSP's managed assemblies, and the installed KIS/KSPDev DLLs. These dependencies are referenced rather than bundled. ModuleManager must be installed for the surface patch.

The package script requires a clean committed source tree. It creates a dated candidate directory under `packages/`, a GameData ZIP, a source archive of the exact commit, a file/dependency hash receipt, and an offline-check report. It does not install anything. Binary build timestamps can change the DLL hash between builds; preserve the exact receipt/package being installed.

Original can geometry, colour textures, and the demonstration arrow can be regenerated with Python 3, without external packages:

```powershell
py -3 Tools\generate_assets.py
```

No third-party art or renderer code is bundled. The independent `.mu` reader used during initial verification is not included in the package. Licensing for a public release of this project remains undecided; this candidate is for development and testing.

## Installation when authorized

Wait for Dave to confirm KSP is closed, then verify no KSP process remains. Back up any existing `GameData/KSPGraffitiArtist` folder and receipt before replacing it. Install only the candidate ZIP's `GameData/KSPGraffitiArtist` directory into KSP's `GameData`. Keep the ZIP, source archive, and SHA-256 receipt for recovery, and update the mod registry for the actual installation.

No upstream KIS/stock files need replacement and no external save edit is required. No changes have been made to the live installation during preparation.

## Using the tool

1. In the editor's Utility category or KIS inventory part picker, find **GA-1 Graffiti Artist Spray Can**. It costs 25 funds, has 0.8 L KIS volume and 0.5 kg mass, and unlocks at Engineering 101.
2. Deliver a can in KIS storage to the existing vessel. Transfer it to the EVA Kerbal's KIS inventory and equip it in the right hand. The prototype permits any EVA Kerbal who can equip a KIS item.
3. Right-click a nearby hull without dragging the camera. Reach is measured within 3 m of the Kerbal's part origin, with a clear line of sight. Hold **Alt** when right-clicking to retain normal part menus.
4. Enter text or choose a PNG picture. Use **Q/E** to rotate and **A/D** to decrease/increase width. These keys type normally when the text field has focus; click another editor control to return to transform controls.
5. Click **Apply**, or **Cancel/Escape**. The old applied marking remains saved until a valid Apply/Erase succeeds, even though it is temporarily hidden while previewing an edit.
6. To edit/erase, right-click an existing marking. The editor also lists all markings on the target part, allowing overlapping marks to be selected. **New marking at clicked point** starts another draft at the original click position. Switching selections discards the current uncommitted draft.

The preview is fixed at the clicked surface point. To choose a different position, cancel and right-click the desired point.

## Text and images

Text uses built-in Arial, supports multiline input, and has RGB colour controls. Font selection is not implemented in this first candidate. Maximum input is 512 characters; unsupported glyphs depend on the game's font.

Copy pictures to `GameData/KSPGraffitiArtist/Images`, then use **Refresh pictures** in the editor. Only PNG basenames are accepted, with a maximum of 4 MiB per file and 2048 pixels per dimension. Up to 64 filenames are listed and decoded textures share a 64 MiB budget. Files that exceed limits or cannot load are unavailable rather than blocking vessel loading. A transparent `direction-arrow.png` is included for testing. Image colours are retained and aspect ratio is preserved.

Missing-image references remain in saved records. Restore the file and refresh pictures to recover the display. Markings can also be edited to select a replacement or erased.

## Prototype limits and recovery

- Up to 32 valid markings per part, widths from 0.1–4 m and rendered height up to 4 m. Paint is not consumed.
- Static vessel-part surfaces are the first target. Paint follows enabled, CPU-readable visible renderer meshes on the target part; colliders are used only for clicking/reach, not paint placement. Unreadable or skinned-only hulls cannot be painted by this implementation. Animated/deployable, rescaled, variant-switched, and unusual compound-mesh parts are not validated.
- Rendering is unlit for readable labels, rather than physically shaded spray paint. The small surface offset and transparent ordering need visual validation.
- Save records are inside `ModuleGraffitiSurface` on each part. Docking does not use a vessel-ID map, but actual stock and KIS save/transfer behaviour still needs testing.
- Erase is immediate when explicitly clicked; there is no undo history. Cancel preserves the previous committed record.
- Removing the addon removes its rendering. Saving without its module/ModuleManager patch may discard paint data, and missing-tool-part behaviour is governed by KSP/KIS. Back up the test save before removal; reinstallation recovery has not been established.

Initial game checks: paint a curved tank on a previously launched test vessel, verify preview/input and held orientation, save/reload, then dock/undock. Consult [the full validation plan](VALIDATION.md).
