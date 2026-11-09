# KK click rejection on 0.2.4

maddavo's KK floor click displays "Choose a vessel, a KK instance or solid planetary terrain."
The running 0.2.4 DLL hash is A08B8DB828D7878402880E946172BF7F4A2D3FAF5E87AFBB666E82C422CFC694.
KSP.log at 23:55-23:56 on 8 November 2026 repeatedly records:

    [GraffitiArtist] Rejected target: Mun, layer 10, parent quad=False, world store=True

Read-only inspection of the installed KSP_x64_Data/globalgamemanagers TagManager
confirms layer 10 is "Scaled Scenery", 15 "Local Scenery", and 28 "TerrainColliders".
The old local-camera query used Physics.DefaultRaycastLayers, which includes 10.
It accepted the nearest physical hit and returned immediately after rejecting it.
Thus the observed clicks were rejected on the distant-planet representation before
KK instance acquisition could examine the local floor.

0.2.5 removes only layer 10 from that query. It retains the default ignore-raycast
exclusion, trigger exclusion, nearest-hit ordering, local EVA/part obstacles,
KK UUID/body resolution, explicit PQ ownership, and separate 3 m EVA reach.
It does not treat a scaled planet as paintable terrain or skip local obstructions.

Host regression fixtures put a scaled Mun hit ahead of KK/terrain hits and retain
nearer local obstacles. Installed candidate IL confirms the editor calls this
tested picker. These are offline checks, not native Unity physics execution.
After restart, check the same KK floor, exposed Mun terrain, normal part targeting
and the reach limit. Capture rejected-target logs if acquisition still fails.
The full native ray hit list was not captured; this correction addresses the
actual rejected layer, and remaining KK/terrain acquisition is unconfirmed.

The pending stackable-can configuration is included in this package.
