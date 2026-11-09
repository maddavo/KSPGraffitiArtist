# Paint remains bright in shadow

0.1.8 supersedes the cutoff material below with a lit blended material to preserve edge coverage. See [fonts and edges](FONTS-EDGES.md). The former 0.1.5 lighting result does not prove the newer shader's native appearance; shadow reception needs rechecking.

maddavo subsequently confirmed the lighting is good in 0.1.5 (5 November 2026). The following missing-letter investigation retains this lit material and shading path.

5 November 2026: maddavo reports the design looks as though it glows when the vessel surface is in shadow and requests paint that responds to the part's shading.

The code confirms the cause: `Sprites/Default` is an unlit presentation path, and the paint renderer explicitly disabled received shadows. Surface geometry alone cannot make an unlit material follow hull lighting. This was a prototype limitation, not intended final paint behaviour.

## 0.1.5 correction

- Use stock `KSP/Alpha/Cutoff` diffuse part lighting, with shadow reception enabled, rather than an overlay/unlit material. If the shader is unavailable, report an error instead of silently using full-bright paint.
- Keep paint shadow casting off: the hull underneath is the physical caster. Keep the existing small geometric lift and depth testing.
- Interpolate the selected visible mesh's vertex normals at each paint sample. Store those shading normals separately from the triangle face normal used for safe offset and front-face filtering. This avoids flat-lit grid cells on a smoothly shaded hull.
- Bake selected text pigment into a cropped font-coverage texture; preserve alpha and remap glyph UVs. Stock shader tint support is not assumed. Dispose the owned texture with its visual; retain PNG RGB unchanged.
- Use alpha cutout at 0.15 in the normal cutout queue. Pixels below the threshold are holes; higher-alpha pixels are opaque diffuse paint. Soft translucent gradients are not supported by this path. Hull normal maps and bespoke/PBR surface properties are not reproduced.

Unity's [lighting troubleshooting guidance](https://docs.unity3d.com/2018.3/Documentation/Manual/LightPerformance.html) describes shadow-reception configuration and alpha-cutout surfaces as the appropriate route for opaque surfaces with gaps. The actual KSP installation's lighting and occluder interactions still require native validation.

## Checks

814 production-source host assertions cover the existing records/UI adapter and all alpha values under pigment conversion. 516 projection/orientation assertions include barycentric smooth normals, outward normalization, geometric lift independence and the local Mk3/test save positions. 35 installed API/config/IL checks verify the new material and normal paths without running Unity rendering: **1,365 offline checks** total.

After installation inspect existing text and the arrow image in direct sunlight, with the sun behind the hull, under another vessel part's shadow and on the night side. Compare the light boundary with nearby bare hull, rotate the camera, and confirm colour, transparent background, orientation and save/reload remain intact. These native visual results remain pending. Existing labels use the new renderer on load; no save edits or repaint are required by the format.
