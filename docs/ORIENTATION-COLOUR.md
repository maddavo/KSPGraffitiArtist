# Mirrored lettering and missing colour

4 November 2026: Dave's screenshot of the 0.1.1 Eagle A marking shows that the lettering now sits visibly on the hull, but is mirrored. Dave also reported that the label should be red rather than black.

## Orientation

The old horizontal direction was `up × outward-normal`. An observer outside the surface looks in the direction `-outward-normal`; their screen-right is therefore `up × (-outward-normal)`, equivalently `outward-normal × up`. The old direction is the negative of that, mirroring both text and pictures.

0.1.2 uses the corrected tangent in rendering and hit-footprint calculations. Triangle winding is reversed to keep mesh normals outward. Stored anchors, rotation angles, size and texture UVs remain unchanged. No save mutation or repaint is required by the record format.

## Text colour

The old renderer fed font atlas RGB into the sprite material and relied on implicit mesh vertex colours. Multiplying dark font RGB by a red tint still produces dark RGB, whereas glyph coverage belongs in alpha. The screenshot and code identify this as the colour path to correct; the runtime atlas itself has not been sampled in Dave's running game.

The new path copies the font texture through a temporary render target (dynamic atlases need not be CPU-readable), preserves every coverage-alpha value, and sets RGB to white. The existing depth-tested sprite material then applies the selected ink colour. Mesh vertex colours are explicitly white. The shared coverage texture is cached, rebuilt when the font atlas changes and released on scene teardown. PNG pictures retain their source RGB and alpha.

This keeps hull depth testing; it does not switch text to an overlay shader that draws through the vessel. Unity's [font documentation](https://docs.unity3d.com/2018.3/Documentation/Manual/class-Font.html) distinguishes the font material and font texture and documents material text colour; the implementation here adapts coverage for the existing surface sprite renderer.

## Validation

The production helper is tested for both exterior sides, all six face normals, six rotations including Eagle A's stored 98.79898 degrees, and asymmetric content order. Coverage tests verify all 256 alpha values and independence from source RGB. Existing save/geometry/API regressions remain passing: 549 record/module/coverage assertions, 511 projection/orientation assertions (including 126 local Mk3 fixture checks), and 13 installed API/config checks, 1,073 total on the development machine.

These checks do not execute Unity shaders or GPU readback. In game, inspect the existing label for readable characters and the saved tint, then preview pure red text and the asymmetric arrow image. Confirm oblique/occluded views still respect the hull. Native results remain pending.
