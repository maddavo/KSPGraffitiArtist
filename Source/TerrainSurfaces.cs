using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KSPGraffitiArtist
{
    public static class TerrainSurfaces
    {
        static readonly FieldInfo localQuads=typeof(PQS).GetField("LocalSpacePQList",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
        // PQs may live in local-space storage rather than below the owning PQS transform.
        // Match the actual quad collider and its explicit sphere owner, never scenery layers.
        public static PQ Find(Collider collider,CelestialBody body)
        {
            if(collider==null || body==null || body.pqsController==null)return null;
            PQ quad=collider.GetComponentInParent<PQ>();
            if(Owned(quad,collider,body.pqsController))return quad;
            var quads=localQuads==null?null:localQuads.GetValue(body.pqsController) as IEnumerable<PQ>;
            if(quads!=null)
                foreach(PQ candidate in quads)
                    if(Owned(candidate,collider,body.pqsController))return candidate;
            return null;
        }
        static bool Owned(PQ quad,Collider collider,PQS sphere)
        {return quad!=null && quad.sphereRoot==sphere && quad.meshCollider==collider;}
        public static MeshFilter MeshFor(Collider collider,CelestialBody body)
        {PQ quad=Find(collider,body);return quad==null?null:quad.meshFilter;}
    }
}
