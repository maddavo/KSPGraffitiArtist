// Minimal scene/quad stand-ins for the actual production terrain resolver.
// They deliberately provide no PQS transform ancestor; native Unity is not executed.
using System.Collections.Generic;
namespace UnityEngine
{
    public class Collider
    {
        public PQ ParentQuad;
        public T GetComponentInParent<T>() where T:class {return ParentQuad as T;}
    }
    public class MeshCollider : Collider {}
    public class MeshFilter {}
}
public class PQ
{
    public PQS sphereRoot;
    public UnityEngine.MeshCollider meshCollider;
    public UnityEngine.MeshFilter meshFilter;
}
public class PQS {public List<PQ> LocalSpacePQList=new List<PQ>();}
public class CelestialBody {public PQS pqsController;}
