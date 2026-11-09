# Opaque glyph boxes and distance-only reach - 0.2.1

maddavo reported obstructed reach while standing on the target surface and solid red rectangles in place of test vessel's lettering on 6 November 2026.

## Reach

CanReach now checks valid tool/target state and distance to the draft anchor only: at most 3 m from the EVA Kerbal's part origin. There is no Kerbal-to-surface raycast, obstruction test or collision-hit proximity requirement. This applies to opening, moving, Apply and Erase on parts, KK instances and terrain. The mouse ray still chooses the first physical surface under the pointer; this is target selection, not a Kerbal line-of-sight restriction.

## Save evidence

A test vessel has two intact Arial Regular schema-2 labels: one digit on an adapter and a multiline label on a Mk3 crew cabin. Saved text, size and pigment values are valid. Private record identifiers are omitted.

The screenshot's large single rectangle and five-letter row plus one lower rectangle match these glyph-quad footprints. No save repair or repaint is indicated.

## Installed shader evidence

0.1.8-0.2.0 requested Standard Fade (`_ALPHABLEND_ON`) and highlight/reflection-off keywords. The installed compiled Standard shader contains **zero** Fade programs and **zero** programs with either off keyword on both D3D11 and OpenGLCore. Unity cannot enable shader programs stripped from the game build and may pick another available variant. This is a confirmed material-selection defect and a strong explanation for opaque glyph quads; no GPU frame or live font atlas was captured.

The original local exporter inspected only segment zero. Unity 2019 program-table entries also identify their compressed-data segment. The new read-only audit follows that segment field and parses all compiled program keyword sets. This resolves the earlier export limitation without changing stock files or bundling stock bytecode.

0.2.1 selects **Standard (Specular setup)**, Transparent mode (`_ALPHAPREMULTIPLY_ON`), One/OneMinusSrcAlpha blending, no depth writes and queue 3001. The installed shader has 318 matching material-profile programs on D3D11 and 276 on OpenGLCore, including 72 and 69 screen-shadow programs respectively. Specular colour is black and gloss is zero, eliminating the transparent-rectangle sheen without requesting stripped off keywords. No emission is enabled. The shader premultiplies RGB itself; cropped font/PNG textures continue to contain straight RGB and fractional alpha, preserving soft edges without multiplying coverage twice.

## Validation and limits

The corrected material host test fails against the old Fade configuration and passes after the change. Build plus 1,677 checks pass locally: 869 record/world/module/coverage, 726 geometry, 28 font/material host, 45 assembly/config/IL and 9 compiled-shader audit checks. The reach IL check confirms CanReach invokes no raycasts; target selection retains its mouse ray.

The optional compiled audit requires a separately obtained UnityPy installation and reads only the installed assets. It is run by test.ps1 when build/unity-reader/UnityPy exists. Without that helper, the normal 1,668 checks still run; the audit is not silently counted. No proprietary game bytes, complete saves or screenshots are committed to source.

In KSP, load test save before repainting and inspect both saved labels, including soft edges and sun/shadow/EVA light. Check the same labels after reload and with other fonts/styles or PNGs. Try opening, moving and applying while standing on the surface within 3 m and with an intervening obstruction, then beyond 3 m. Native pixels and actual shader selection still require gameplay confirmation.

References: [Unity Standard shader keywords and build-time stripping](https://docs.unity3d.com/ru/2021.1/Manual/materials-scripting-standard-shader.html), [shader variant stripping and fallback](https://docs.unity3d.com/cn/2022.3/Manual/shader-variant-stripping.html).
