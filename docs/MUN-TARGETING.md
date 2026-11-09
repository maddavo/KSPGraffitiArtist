# Mun terrain click rejection - 0.2.3

maddavo confirms vessel and KK-object painting works, but right-clicking the Mun surface did not open the editor. The installed version is still 0.2.1; the prepared Countdown update is included in 0.2.3.

## Faulty assumptions

The previous terrain classifier required both a PQ quad and the body's PQS controller in the collider's transform ancestors. That mixes geometry hierarchy with logical planet ownership. The installed game exposes PQ.sphereRoot as the explicit owner, and PQS also maintains a non-public LocalSpacePQList. Requiring a PQS transform ancestor is unnecessary and can reject a valid quad. The exact hierarchy/collider under maddavo's click was not captured, so this is a corrected source defect consistent with the symptom, rather than a native hit trace proving every rejection cause.

The previous mesh collector also assumed that a MeshFilter lived on the collider GameObject. The installed PQ exposes its actual meshCollider and meshFilter references, so projection can use the same resolved quad instead of making a second hierarchy assumption.

## Correction

TerrainSurfaces.Find matches the exact collider to PQ.meshCollider and the current body's pqsController to PQ.sphereRoot. It first checks a parent quad, then searches the controller's actual local-space quad list when the collider is detached. The list is read through a narrow reflection adapter because it is not public. There are no writes to the controller or terrain definitions and no layer/name-only fallback.

TerrainSurfaces.MeshFor returns that same quad's meshFilter. Both initial editor targeting and nearby paint projection use the shared resolver. The result is not cached across unloaded/replaced quads. Existing distance-only 3 m reach, ownership, rendering, save coordinates and marking formats are unchanged. Unsupported target clicks now log collider name, layer, parent-quad presence and world-store availability, so a remaining rejection can be diagnosed from KSP.log.

Oceans with another PQS owner, other bodies, scenery merely parented under terrain, missing targets and stale colliders remain rejected. Native quad hierarchy, enabled/readable mesh behaviour and clicked hit identity still need gameplay validation.

## Evidence and acceptance

Warnings-as-errors compilation uses the installed game assemblies. New API checks verify sphereRoot, meshCollider, meshFilter and LocalSpacePQList against those assemblies. Ten tests execute the actual production resolver with explicit host stand-ins: a quad without a PQS transform ancestor, detached local-space lookup, correct mesh lookup, wrong-body/ocean rejection, scenery rejection, null inputs and unload/replacement handling. Total local checks: 1,695 (including the optional nine compiled-shader checks and the actual registered Countdown metadata tests on maddavo's machine). No native colliders or rendering are executed.

After installation, EVA within 3 m of exposed Mun terrain, equip the can and right-click clear ground. The editor should open with a conforming preview. Apply a short label, quicksave/reload, leave range and return; check the position, LOD changes, text, edges and lighting. Also check a vessel, a KK wall and Countdown. If terrain is still rejected, retain the corresponding GraffitiArtist rejection line from KSP.log; no save editing is needed.
