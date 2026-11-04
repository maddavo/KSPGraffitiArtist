# Interaction and requirements

Status: acceptance requirements, updated 4 November 2026. A 0.1.0 prototype implements the core workflow; actual game behaviour remains unverified. See [prototype limits](PROTOTYPE.md).

## Agreed product direction

The tool puts text or pictures directly onto the surface of existing vessels and modules. It is intended for quick visual identification of purpose and identity after launch.

- The player carries a spray-paint can in KIS inventory and equips it as the active tool.
- Right-clicking a paintable surface opens an editor anchored to the clicked position.
- The editor accepts text or an image selection and shows a live surface preview.
- Q/E controls rotation; W/S controls size.
- Apply commits the preview; Cancel abandons it without changing existing paint.
- Equipped-tool interaction with an existing marking supports editing and erasing.
- Markings persist independently of the can, attached to their target part.
- There is no visible blank plate and no individual paint-marking part to transport or attach.

## Proposed detailed behaviour

These details make the agreed interaction implementable but can be refined during prototyping.

### Tool and targeting

Use one equipped right-hand KIS item to activate painting. A can merely stored in a nearby container must not activate the editor. The initial workflow uses an EVA engineer; exact skill restrictions remain to be decided.

Only vessel-part surfaces are in the initial scope. Terrain, buildings, Kerbals, and other scenery are outside this first implementation.

Targeting must respect a short EVA working distance and an unobstructed line of sight. The numerical reach will be selected and tested during the prototype. A hit identifies both a specific part and a surface position/normal, rather than the vessel's centre.

An ordinary right-click on a valid target with the tool equipped should open the paint editor. Right-drag camera movement must still work and must not accidentally create a marking. Right-clicks over UI must not paint through it. Without the tool equipped, normal part interaction remains available. The exact way to access a part's normal menu while the tool is equipped remains a UI decision.

### Editor and controls

| Input or action | Intended result |
| --- | --- |
| Right-click a paintable surface | Open a new marking preview at that point |
| Right-click an existing marking | Open that marking for editing |
| Text mode | Enter a label; select text colour and font |
| Image mode | Choose a picture from a thumbnail gallery |
| Q / E outside text entry | Rotate counterclockwise / clockwise as viewed from outside the surface |
| W / S outside text entry | Decrease / increase size |
| Apply | Commit the displayed marking |
| Cancel or Escape | Discard uncommitted changes and close |
| Erase while editing an existing marking | Explicitly remove that marking |

Use a visible orientation/size indication. Images should retain their aspect ratio. Text and images should support transparent backgrounds so the hull remains visible around the content. Exact fonts, supported image formats, size limits, rotation increments, and colour controls remain implementation decisions.

While the editor is open, conflicting EVA controls must be suppressed. A focused text field receives typing without rotating or scaling the marking. Input restrictions must be released on every exit path, including tool unequip, vessel switch, scene change, target loss, and errors.

Apply and Erase must recheck tool eligibility, target validity, and reach. Losing eligibility closes the preview without committing it. Editing must leave the previous applied marking intact until Apply or Erase succeeds. Cancelling a new preview creates nothing.

If multiple markings overlap, selection must offer a deterministic way to choose the intended marking; the exact UI is unresolved.

### Persistence and appearance

Applied paint should follow the original part's position and orientation, remain visible after the Kerbal leaves, and survive game reload and docking/undocking. Moving a painted part must not leave a floating marking behind. Destroying the part removes its visible markings.

Curved tanks are a first-prototype requirement. A flat floating rectangle that visibly intersects or hovers over the tank does not satisfy the intended appearance. Initial projection should be confined to the selected part; it must not paint through to neighbouring modules or the far side of a tank.

Image selection is from an addon's designated local image folder. The final location, file validation, and whether newly added files need a KSP restart are unresolved. Missing image files must not prevent loading a vessel or save, and their references should be retained for recovery.

## Scope boundaries

Initial work covers text/image markings, the KIS spray-can workflow, surface preview, editing/erasing, and persistence on vessel parts. Freehand brush strokes, terrain painting, paint-consumption resources, and stock-inventory integration are not agreed features.

Implementation and building were subsequently authorized. Installation is awaiting Dave's confirmation that KSP is closed. Development must validate the design before claiming gameplay success.
