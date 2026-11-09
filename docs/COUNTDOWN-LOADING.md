# Countdown native-load failure - 0.2.4

After installing 0.2.3, Countdown appears in the picker but reports **Cannot load font: Countdown**. KSP.log records repeated preview failures with that exact message. Discovery succeeded; the registered-file loader did not return a usable dynamic Unity font. This is a failure of the mod's loading path, not evidence that the Windows font is corrupt. Windows renders the installed Countdown face successfully.

The installed Unity bindings do contain Font(string)'s Internal_CreateFontFromPath path, so the API was not simply absent. The precise native reason for the unusable dynamic font was not captured. File paths are now normalized for Unity, and the reported non-dynamic case has an explicit rendering fallback rather than another assumption that the constructor must work.

## Rendering route

Registered user fonts still try the normalized native file path first. If that yields no dynamic font (or rejects the path), Windows-only fallback state is cached for that family until leaving flight. Existing Unity fonts retain their current rendering path. No substituted Arial label is silently committed.

The fallback creates an offscreen Windows GDI font, verifies the actual selected face name, and draws grayscale coverage into a bounded bitmap. It uses the real installed face and Regular/Bold/Italic/Bold Italic selection, supports centred multiline text, and rejects Windows font-family substitution. The renderer does not open/control any window, install a font, change a registration or require another managed DLL. Device contexts, font objects and bitmap handles are released after each render.

Coverage is cropped, converted to bottom-first alpha rows, padded with a transparent border and tinted with the chosen pigment in an owned Unity texture. The existing lit transparent material, surface tessellation, normals, rotation and size controls are reused. The glyph image has transparent spaces/holes and fractional edges; it is not an opaque plate. Actual ink width remains the saved marking width. Texture dimensions are bounded, with reduced raster size for long labels; impractically large text is rejected. Windows face names longer than 31 characters cannot use this fallback.

Saved family/style/text records remain unchanged; no repaint or save edit is required. Missing fonts still retain their saved names and show the existing explicit Arial fallback warning. A single diagnostic is logged when a family switches to Windows coverage.

## Evidence

Build and 1,715 checks pass locally: 869 record/world/module/coverage, 726 geometry, 37 font/material host, 48 API/config/IL, 9 installed shader audit, 10 terrain-resolver host and **16 real Windows font-raster checks**. The host now models a path constructor returning a non-dynamic font, matching the reported failure, and verifies fallback selection/cache/reset instead of throwing. Previously that host assumed successful dynamic loading.

The real Windows checks render the actual installed Countdown, verify selected face and pixels distinct from Arial, transparent holes/background, opaque ink and intermediate edge values, all four styles, centred multiline layout, equivalent newline forms, bounded output, rejected substitution, and repeated-render handle cleanup. A grayscale LABEL 5 proof is retained locally outside source/package assets. These tests invoke the same production GDI rasterizer the addon uses; they do not execute KSP/Unity texture upload, projection or lighting.

After installation, open a saved label, choose Countdown and check the distinctive glyphs, all styles, multiline alignment, pigment, edges, shadow response and Apply/Cancel/save-reload. The native KSP bridge and final appearance still require gameplay confirmation. Mun targeting from 0.2.3 also remains pending its separate check.

References: [Unity 2019 font bindings](https://github.com/Unity-Technologies/UnityCsReference/blob/2019.4/Modules/TextRendering/TextRendering.bindings.cs), [CreateFontW](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-createfontw), [DrawTextW](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-drawtextw), [CreateDIBSection](https://learn.microsoft.com/en-us/windows/win32/api/wingdi/nf-wingdi-createdibsection).
