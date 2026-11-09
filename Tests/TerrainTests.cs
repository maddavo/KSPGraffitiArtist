using System;
using KSPGraffitiArtist;
using UnityEngine;
class TerrainTests
{
    static int count;
    static void Check(bool ok,string name){count++;if(!ok)throw new Exception(name);}
    static int Main()
    {
        try{
            var sphere=new PQS();var body=new CelestialBody{pqsController=sphere};
            var collider=new MeshCollider();var mesh=new MeshFilter();
            var quad=new PQ{sphereRoot=sphere,meshCollider=collider,meshFilter=mesh};collider.ParentQuad=quad;
            Check(TerrainSurfaces.Find(collider,body)==quad,"quad's actual owner suffices without a PQS transform ancestor");
            Check(TerrainSurfaces.MeshFor(collider,body)==mesh,"projection obtains quad's actual mesh filter");
            collider.ParentQuad=null;sphere.LocalSpacePQList.Add(quad);
            Check(TerrainSurfaces.Find(collider,body)==quad,"detached local-space collider resolves through owner's quad list");
            Check(TerrainSurfaces.MeshFor(collider,body)==mesh,"detached collider uses the same resolved visible mesh");
            var other=new CelestialBody{pqsController=new PQS()};
            Check(TerrainSurfaces.Find(collider,other)==null,"wrong body rejected");
            var ocean=new PQS();var waterCollider=new MeshCollider();var waterQuad=new PQ{sphereRoot=ocean,meshCollider=waterCollider};waterCollider.ParentQuad=waterQuad;
            Check(TerrainSurfaces.Find(waterCollider,body)==null,"ocean/other sphere rejected");
            var scenery=new MeshCollider{ParentQuad=quad};
            Check(TerrainSurfaces.Find(scenery,body)==null,"scenery merely parented below a quad is not terrain");
            Check(TerrainSurfaces.Find(null,body)==null&&TerrainSurfaces.Find(collider,null)==null,"missing target/body rejected safely");
            var replacement=new PQ{sphereRoot=sphere,meshCollider=new MeshCollider(),meshFilter=new MeshFilter()};
            sphere.LocalSpacePQList.Clear();sphere.LocalSpacePQList.Add(replacement);
            Check(TerrainSurfaces.Find(collider,body)==null,"unloaded old collider does not match replacement quad");
            Check(TerrainSurfaces.MeshFor(replacement.meshCollider,body)==replacement.meshFilter,"newly loaded quad resolves without stale cached references");
            Console.WriteLine("PASS: "+count+" production terrain ownership/resolver checks (host only).");return 0;
        }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
