# Countdown and user-installed fonts - 0.2.2

0.2.3 made Countdown selectable, but its native file load failed in KSP. 0.2.4 retains this discovery and adds a real Windows glyph-coverage fallback; see [loading diagnosis](COUNTDOWN-LOADING.md). Native KSP upload/rendering remains pending.

maddavo reports Countdown missing from the font picker on 7 November 2026. The installed 0.2.1 source only uses Unity's GetOSInstalledFontNames and caches that list until leaving flight.

Read-only Windows inspection confirms a current-user Fonts registry entry named Countdown (TrueType), pointing to AppData/Local/Microsoft/Windows/Fonts/Countdown Regular.otf. The 19,496-byte file is an OpenType CFF font (OTTO signature) whose internal family and full name are both Countdown. Windows InstalledFontCollection also lists Countdown. It passes the mod's font-name validation. The font was present before this KSP process started; this is not merely an installation made after the game was launched. Its SHA-256 is 7771D76DA962170B772DE392E43755CC54A6F4E211D89087CA5CAEA8E965E97B.

We have not captured the running Unity enumeration or loaded Countdown in native Unity. Windows registration is confirmed; relying solely on Unity's list leaves a discovery gap consistent with the report. CFF rendering compatibility still needs a native check.

## Correction

0.2.2 supplements Unity's catalogue with read-only current-user Windows font registrations. It reads the internal family name from bounded TTF/OTF naming tables rather than assuming the registry label or filename is the family. It accepts Windows/Unicode naming records and prefers typographic family and English Windows entries. Missing, inaccessible, truncated or malformed files are skipped. TTC collections are not added through this supplement; Unity's existing native catalogue remains available.

For a supplemented family, the renderer opens its registered file with Unity's Font(string) constructor rather than retrying a name lookup that may not see it. A non-dynamic load reports an error. Font-family/style records remain unchanged and contain no absolute paths. Fonts are not copied, converted, redistributed or installed by the addon. Windows registrations are not modified. The existing font cache is released on leaving flight.

Clicking Font now refreshes the catalogue each time it opens; existing live fonts and their independent pigment textures are retained. Fonts missing on another computer still preserve their saved names and display the existing Arial fallback. Multiple files sharing a family resolve to the first discovered registration; native face/style selection for such families remains a compatibility limitation. Countdown has one registered face in this investigation.

## Validation

The warnings-as-errors build and 1,683 local checks pass: 869 record/world/module/coverage, 726 geometry, 34 font/material host, 45 assembly/config and 9 installed shader audit checks. New tests parse a synthetic OpenType name fixture, reject malformed/truncated metadata, refresh the cached list, discover maddavo's actual registered Countdown while the Unity host deliberately omits it, and route loading to its registered file path. The Font constructor is a host substitute in these tests; this does not prove native CFF rendering.

KSP is running, so 0.2.1 remains installed during preparation. After the update is installed with KSP closed, open the font picker and choose Countdown. Confirm its distinctive glyphs appear rather than an Arial fallback, then Apply, save/reload and check text/style, colour, smooth edges and shadows. No external save edits are required.

Unity 2019.4 references: [font file paths and Font constructor](https://docs.unity3d.com/2019.4/Documentation/ScriptReference/Font.GetPathsToOSFonts.html), [Font constructor](https://docs.unity3d.com/2019.4/Documentation/ScriptReference/Font-ctor.html).
