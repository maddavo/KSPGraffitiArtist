# Font, style and edge coverage - 0.1.8

0.2.1 supersedes the Fade material below: the installed game stripped that variant. See [transparent-glyph diagnosis](TRANSPARENT-GLYPHS.md) for the verified available transparent profile, retained soft coverage and pending native validation.

maddavo requested font/style editing and smoother decal edges on 6 November 2026.

## Font controls

In Text mode, click **Font: ...** to open the installed-font list. Choose a family, then **Regular**, **Bold**, **Italic** or **Bold Italic**. Changes appear in the preview and only Apply commits them. These controls also work for existing labels. I/J/K/L placement, Q/E rotation and A/D sizing remain available outside text focus.

The picker uses Unity's installed OS font names and dynamic-font API; fonts are not redistributed. Arial uses the existing built-in font. A family absent on another computer displays as Arial while retaining the saved family/style. A warning appears in the editor. Font shapes, available characters and native/synthesized styles depend on Unity and the installed family. Up to 64 selected OS families are cached per flight and released on leaving flight.

Schema 2 adds a validated style field and permits safe family names. Schema 1 loads with Regular style. No external save edits are needed. Old addon versions retain schema-2 records as unrecognised data but do not render them; restore the newer addon to display them again. Avoid erasing/recreating a missing label during a rollback.

## Smoother edges

The 0.1.5-0.1.7 material uses KSP/Alpha/Cutoff with threshold 0.15: coverage below threshold disappears and coverage above it is opaque. The font pipeline already preserves all 256 alpha values, but the material discards their smooth transition. PNGs encounter the same threshold.

0.1.8 uses the installed Unity **Standard** lit shader in Fade mode: source-alpha blending, no alpha clipping, no depth writes and queue 3001. Text pigment crops and PNGs use bilinear filtering. Fractional alpha blends the edge with the hull; fully opaque interiors stay opaque. This is texture coverage smoothing rather than a change to KSP's global antialiasing setting. Images with opaque rectangular backgrounds retain them; the mod does not infer a silhouette or remove backgrounds.

Metallic, gloss, emission, specular highlights and glossy reflections are disabled. Received shadows remain requested; paint casts no duplicate shadow and retains the interpolated hull normals. This changes the lighting shader, so matching the hull in sun, shadow and EVA lights must be checked again. Transparent rendering also needs an overlapping-label/depth-order check. No fallback to an unlit shader is allowed.

Read-only local asset inspection found Standard at sharedassets0.assets path ID 1906. KSP/Alpha/Translucent at resources.assets ID 307 has a depth prepass and its exported variants lack shadow keywords, so that simpler option was not used. Standard's parsed material properties are present; the local extractor failed to export all compiled Standard programs. Availability of every selected native variant and actual shadow/edge results are not proven by offline inspection. No stock shader assets are redistributed or modified.

## Validation

The warnings-as-errors build passes against the installed game assemblies. Offline tests cover schema-1 defaults, font/style persistence, unavailable-name retention, malformed metadata, safe font catalogue/cache/teardown, style mapping, preserved alpha, bilinear crops, exact Fade material settings, no rectangular depth writes, matte/no-emission settings and requested shadow flags. Engine APIs use explicit host substitutes in these tests; there is no GPU or native font rendering.

In KSP, check multiple families and all four styles, Apply/Cancel and save/reload; inspect a red label and a transparent PNG at close/far distances and oblique angles, in sunlight, vessel shadow and EVA lights. Check overlapping text/pictures and the existing LABEL 1 label. Native results remain pending.

Implementation references: [Unity 2019.4 font API](https://docs.unity3d.com/2019.4/Documentation/ScriptReference/Font.CreateDynamicFontFromOSFont.html), [Standard rendering modes](https://docs.unity3d.com/2019.4/Documentation/Manual/StandardShaderMaterialParameterRenderingMode.html).
