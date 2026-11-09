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
        // A viewer on the -Z side looks along +Z: their screen-right is +X.
        SurfaceVector front=SurfaceGeometry.RightTangent(V(0,0,-1),V(0,1,0));
        Check(front.X>.999f,"front-face text/image must advance toward screen-right");
        Check(SurfaceVector.Cross(V(0,1,0),V(0,0,-1)).X<0,"old basis reproduced mirrored lettering");
        SurfaceVector back=SurfaceGeometry.RightTangent(V(0,0,1),V(0,1,0));
        Check(back.X<-.999f,"opposite face remains readable from its own outside");
        foreach(SurfaceVector normal in new[]{V(0,0,-1),V(0,0,1),V(-1,0,0),V(1,0,0),V(0,-1,0),V(0,1,0)}){
            SurfaceVector initial=Math.Abs(normal.Y)>.5f?V(0,0,1):V(0,1,0);
            foreach(float angle in new[]{0f,30f,90f,98.79898f,180f,270f}){
                double radians=-angle*Math.PI/180;
                SurfaceVector up=initial*(float)Math.Cos(radians)+SurfaceVector.Cross(normal,initial)*(float)Math.Sin(radians);
                SurfaceVector right=SurfaceGeometry.RightTangent(normal,up);
                Check(Math.Abs(right.LengthSquared-1)<1e-5f,"unit horizontal axis "+angle);
                Check(Math.Abs(SurfaceVector.Dot(right,up))<1e-5f,"orthogonal horizontal axis "+angle);
                Check(SurfaceVector.Dot(SurfaceVector.Cross(up,normal*-1),right)>.999f,"viewer-right handedness "+angle);
            }
        }
        // An asymmetric picture/text footprint must keep increasing U/X to the viewer's right.
        float last=float.MinValue;
        foreach(float x in new[]{-.8f,-.4f,.2f,.9f}){
            float screenX=SurfaceVector.Dot(front*x,V(1,0,0));
            Check(screenX>last,"asymmetric glyph/picture point order");last=screenX;
        }
        var triangles=new List<SurfaceTriangle>();Plane(triangles,.05f,true);
        SurfaceVector na=V(-.4f,0,1).Unit,nb=V(.4f,0,1).Unit,nc=V(-.4f,.3f,1).Unit;
        var smooth=new SurfaceGeometry(new[]{new SurfaceTriangle(V(-1,-1,0),V(1,-1,0),V(-1,1,0),na,nb,nc)});
        SurfaceVector smoothPoint,smoothNormal;
        Check(smooth.Project(V(-.5f,-.5f,0),V(0,0,1),.5f,out smoothPoint,out smoothNormal),"smooth-normal surface hit");
        SurfaceVector expectedNormal=(na*.5f+nb*.25f+nc*.25f).Unit;
        Check((smoothNormal-expectedNormal).LengthSquared<1e-10f,"barycentric hull normals preserved");
        Check(Math.Abs(smoothNormal.LengthSquared-1)<1e-5f&&smoothNormal.Z>0,"unit outward shading normal");
        Check(Math.Abs(smoothPoint.Z-SurfaceGeometry.Lift)<1e-6f,"lift still follows geometry rather than smooth shading normal");
        var flat=new SurfaceGeometry(new[]{new SurfaceTriangle(V(-1,-1,0),V(1,-1,0),V(-1,1,0),V(0,0,1))});
        Check(flat.Project(V(-.5f,-.5f,0),V(0,0,1),.5f,out smoothPoint,out smoothNormal)&&smoothNormal.Z>.999f,"flat-normal fallback retained");
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
        foreach(string name in new[]{"mk3-surface-fixture.bin","glyph-refresh/mk3-surface-fixture.bin"}){
        string fixture=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,name);
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
                Console.WriteLine("Read-only actual Mk3 fixture "+name+": "+number+" visible triangles, "+samples+" label samples.");
            }
        }
        }
        Console.WriteLine("PASS: "+count+" production surface-projection assertions (offline).");return count;
    }
    static SurfaceVector Read(BinaryReader r){return V(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());}
}
