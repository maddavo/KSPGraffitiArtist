using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KSPGraffitiArtist
{
    public static class PaintPicking
    {
        // KSP 1.12.5 layer 10 is Scaled Scenery: a separate coordinate space.
        // Its planet colliders cannot participate in a local flight-camera ray.
        public static IEnumerable<RaycastHit> Hits(Ray ray)
        {
            return Physics.RaycastAll(ray,10000f,Physics.DefaultRaycastLayers & ~(1<<10),
                QueryTriggerInteraction.Ignore).OrderBy(hit=>hit.distance);
        }
    }
}
