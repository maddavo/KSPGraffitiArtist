using System;
using System.Linq;
using KSPGraffitiArtist;
using UnityEngine;
namespace UnityEngine
{
    public struct Ray {}
    public struct RaycastHit {public int Layer; public float distance;}
    public enum QueryTriggerInteraction {Ignore}
    public static class Physics
    {
        public const int DefaultRaycastLayers=~(1<<2);
        public static RaycastHit[] Scene;
        public static float LastRange;
        public static RaycastHit[] RaycastAll(Ray ray,float range,int mask,QueryTriggerInteraction triggers)
        {LastRange=range;return Scene.Where(h=>(mask & (1<<h.Layer))!=0).ToArray();}
    }
}
class PickingTests
{
    static int count;
    static void Check(bool ok,string name){count++;if(!ok)throw new Exception(name);}
    static int Main()
    {
        try{
            // A scaled Mun collider sorts ahead of the real KK floor in the old query.
            Physics.Scene=new[]{new RaycastHit{Layer=15,distance=12},new RaycastHit{Layer=10,distance=1},
                new RaycastHit{Layer=28,distance=13},new RaycastHit{Layer=2,distance=2}};
            var hits=PaintPicking.Hits(new Ray()).ToArray();
            Check(hits.Length==2 && hits[0].Layer==15,"scaled Mun no longer masks a local KK floor");
            Check(hits[1].Layer==28,"actual planetary terrain remains pickable");
            Physics.Scene=Physics.Scene.Concat(new[]{new RaycastHit{Layer=0,distance=8},new RaycastHit{Layer=17,distance=6}}).ToArray();
            hits=PaintPicking.Hits(new Ray()).ToArray();
            Check(hits[0].Layer==17 && hits[1].Layer==0,"local EVA and part obstructions keep nearest-hit priority");
            Check(!hits.Any(h=>h.Layer==2),"default ignore-raycast exclusion retained");
            Check(Physics.LastRange==10000,"existing camera ray range retained independently of EVA reach");
            Console.WriteLine("PASS: "+count+" production local picking checks (host physics).");return 0;
        }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
