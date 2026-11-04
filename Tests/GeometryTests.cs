using System;
using System.Collections.Generic;
using System.IO;
using KSPGraffitiArtist;
public static class GeometryTests
{
    static int count;
    static void Check(bool ok,string name){count++;if(!ok)throw new Exception("Geometry: "+name);}
    static SurfaceVector V(float x,float y,float z){return new SurfaceVector(x,y,z);}
    static void Plane(List<SurfaceTriangle> triangles,float z,bool front)
    {
        SurfaceVector n=V(0,0,front?1:-1);
        triangles.Add(new SurfaceTriangle(V(-2,-2,z),V(2,-2,z),V(2,2,z),n));
        triangles.Add(new SurfaceTriangle(V(-2,-2,z),V(2,2,z),V(-2,2,z),n));
    }
    public static int Run()
    {
        var triangles=new List<SurfaceTriangle>();Plane(triangles,.05f,true);
        var visible=new SurfaceGeometry(triangles);SurfaceVector hit;
        Check(visible.Project(V(0,0,0),V(0,0,1),.5f,out hit),"hit visible hull beyond original collider");
        Check(Math.Abs(hit.Z-.0525f)<1e-6f,"lift is relative to visible hull, not collider at z=0");
        Check(.0025f<.05f,"old 2.5 mm collider offset was buried in 50 mm mismatch");
        Check(!visible.Project(V(3,0,0),V(0,0,1),.5f,out hit),"do not bridge holes/outside hull");
        Check(!visible.Project(V(0,0,0),V(0,0,-1),.5f,out hit),"back face excluded");
        Check(!visible.Project(V(0,0,0),V(0,0,0),.5f,out hit),"zero direction rejected");
        Check(!visible.Project(V(0,0,0),V(0,0,1),float.NaN,out hit),"invalid depth rejected");
        Check(!visible.Project(V(0,0,0),V(0,0,1),.01f,out hit),"out of depth excluded");
        Plane(triangles,.15f,true);Plane(triangles,.3f,false);
        visible=new SurfaceGeometry(triangles);
        Check(visible.Project(V(0,0,0),V(0,0,1),.5f,out hit)&&Math.Abs(hit.Z-.1525f)<1e-6f,"nearest outward surface selected");
        // Enough triangles to exercise both sides of the spatial hierarchy.
        for(int i=0;i<100;i++){
            float x=i*10;
            triangles.Add(new SurfaceTriangle(V(x,-1,.2f),V(x+1,-1,.2f),V(x,1,.2f),V(0,0,1)));
        }
        visible=new SurfaceGeometry(triangles);
        for(int i=1;i<100;i++)Check(visible.Project(V(i*10+.1f,0,0),V(0,0,1),1,out hit)&&Math.Abs(hit.Z-.2025f)<1e-5f,"BVH branch "+i);
        // Convex cylinder: collision polygon lies inside the visible, finer polygon.
        var hull=new List<SurfaceTriangle>();float radius=1.89f;
        for(int i=0;i<96;i++){
            double a=i*Math.PI*2/96,b=(i+1)*Math.PI*2/96;
            var p=V((float)Math.Cos(a)*radius,-2,(float)Math.Sin(a)*radius);
            var q=V((float)Math.Cos(b)*radius,-2,(float)Math.Sin(b)*radius);
            var r=q+V(0,4,0);var s=p+V(0,4,0);var n=V(p.X+q.X,0,p.Z+q.Z);
            hull.Add(new SurfaceTriangle(p,q,r,n));hull.Add(new SurfaceTriangle(p,r,s,n));
        }
        visible=new SurfaceGeometry(hull);
        for(int i=-40;i<=40;i++){
            float x=i*.02f;
            Check(visible.Project(V(x,0,-1.84f),V(0,0,-1),1,out hit),"curved hull sample "+i);
            float squared=hit.X*hit.X+hit.Z*hit.Z;
            Check(squared>radius*radius,"paint outside visible curved hull "+i);
        }
        string fixture=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"mk3-surface-fixture.bin");
        if(File.Exists(fixture)){
            using(var reader=new BinaryReader(File.OpenRead(fixture))){
                int number=reader.ReadInt32();var realHull=new List<SurfaceTriangle>();
                for(int i=0;i<number;i++){
                    SurfaceVector a=Read(reader),b=Read(reader),c=Read(reader),n=Read(reader);
                    realHull.Add(new SurfaceTriangle(a,b,c,n));
                }
                visible=new SurfaceGeometry(realHull);int samples=reader.ReadInt32();
                for(int i=0;i<samples;i++){
                    SurfaceVector plane=Read(reader),normal=Read(reader);float expected=reader.ReadSingle();
                    Check(visible.Project(plane,normal,1.1f,out hit),"actual Mk3 saved-label sample "+i);
                    Check(Math.Abs(SurfaceVector.Dot(hit-plane,normal)-expected)<.0001f,"Mk3 hull clearance agrees with independent raycast "+i);
                }
                Console.WriteLine("Read-only actual Mk3/Eagle A fixture: "+number+" visible triangles, "+samples+" label samples.");
            }
        }
        Console.WriteLine("PASS: "+count+" production surface-projection assertions (offline).");return count;
    }
    static SurfaceVector Read(BinaryReader r){return V(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());}
}
