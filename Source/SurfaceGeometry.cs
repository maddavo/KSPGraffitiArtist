using System;
using System.Collections.Generic;

namespace KSPGraffitiArtist
{
    // Managed geometry kernel, also used by the offline regression executable.
    public struct SurfaceVector
    {
        public float X,Y,Z;
        public SurfaceVector(float x,float y,float z){X=x;Y=y;Z=z;}
        public static SurfaceVector operator +(SurfaceVector a,SurfaceVector b){return new SurfaceVector(a.X+b.X,a.Y+b.Y,a.Z+b.Z);}
        public static SurfaceVector operator -(SurfaceVector a,SurfaceVector b){return new SurfaceVector(a.X-b.X,a.Y-b.Y,a.Z-b.Z);}
        public static SurfaceVector operator *(SurfaceVector a,float b){return new SurfaceVector(a.X*b,a.Y*b,a.Z*b);}
        public static float Dot(SurfaceVector a,SurfaceVector b){return a.X*b.X+a.Y*b.Y+a.Z*b.Z;}
        public static SurfaceVector Cross(SurfaceVector a,SurfaceVector b){return new SurfaceVector(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);}
        public float LengthSquared {get{return Dot(this,this);}}
        public SurfaceVector Unit {get{float n=(float)Math.Sqrt(LengthSquared);return n>1e-12f?this*(1/n):new SurfaceVector();}}
        public float Axis(int axis){return axis==0?X:axis==1?Y:Z;}
    }
    public struct SurfaceTriangle
    {
        public SurfaceVector A,B,C,Normal,NA,NB,NC;
        public SurfaceTriangle(SurfaceVector a,SurfaceVector b,SurfaceVector c,SurfaceVector outward)
        {
            A=a;B=b;C=c;Normal=SurfaceVector.Cross(b-a,c-a).Unit;
            if(SurfaceVector.Dot(Normal,outward)<0)Normal=Normal*-1;
            NA=NB=NC=Normal;
        }
        public SurfaceTriangle(SurfaceVector a,SurfaceVector b,SurfaceVector c,SurfaceVector na,SurfaceVector nb,SurfaceVector nc)
            :this(a,b,c,na+nb+nc)
        { NA=na.Unit;NB=nb.Unit;NC=nc.Unit; }
    }
    public sealed class SurfaceGeometry
    {
        // Offset from the visible triangle, not the collision hull. Small cells bound chord sag.
        public const float Lift=.0025f;
        public const float SampleSpacing=.02f;
        public static SurfaceVector RightTangent(SurfaceVector outward,SurfaceVector up)
        {
            // From outside the hull the viewing direction is -outward.
            // Camera-right = up x viewing-direction = outward x up.
            return SurfaceVector.Cross(outward,up).Unit;
        }
        sealed class Node
        {
            public SurfaceVector Min,Max;
            public Node Left,Right;
            public int Start,Count;
        }
        readonly SurfaceTriangle[] triangles;
        readonly Node root;
        public SurfaceGeometry(IEnumerable<SurfaceTriangle> input)
        {
            var list=new List<SurfaceTriangle>();
            foreach(SurfaceTriangle t in input)if(t.Normal.LengthSquared>.5f)list.Add(t);
            triangles=list.ToArray();if(triangles.Length>0)root=Build(0,triangles.Length);
        }
        public int Count {get{return triangles.Length;}}
        Node Build(int start,int count)
        {
            var min=new SurfaceVector(float.MaxValue,float.MaxValue,float.MaxValue);
            var max=new SurfaceVector(float.MinValue,float.MinValue,float.MinValue);
            for(int i=start;i<start+count;i++)foreach(SurfaceVector v in new[]{triangles[i].A,triangles[i].B,triangles[i].C}){
                min=new SurfaceVector(Math.Min(min.X,v.X),Math.Min(min.Y,v.Y),Math.Min(min.Z,v.Z));
                max=new SurfaceVector(Math.Max(max.X,v.X),Math.Max(max.Y,v.Y),Math.Max(max.Z,v.Z));
            }
            var node=new Node{Min=min,Max=max,Start=start,Count=count};
            if(count<=12)return node;
            SurfaceVector span=max-min;int axis=span.X>span.Y?(span.X>span.Z?0:2):(span.Y>span.Z?1:2);
            Array.Sort(triangles,start,count,Comparer<SurfaceTriangle>.Create((a,b)=>
                (a.A.Axis(axis)+a.B.Axis(axis)+a.C.Axis(axis)).CompareTo(b.A.Axis(axis)+b.B.Axis(axis)+b.C.Axis(axis))));
            int half=count/2;node.Left=Build(start,half);node.Right=Build(start+half,count-half);node.Count=0;return node;
        }
        public bool Project(SurfaceVector plane,SurfaceVector normal,float depth,out SurfaceVector point)
        { SurfaceVector shading;return Project(plane,normal,depth,out point,out shading); }
        public bool Project(SurfaceVector plane,SurfaceVector normal,float depth,out SurfaceVector point,out SurfaceVector shading)
        {
            point=new SurfaceVector();shading=new SurfaceVector();
            if(root==null || !MarkingData.Finite(depth) || depth<=0)return false;
            normal=normal.Unit;if(normal.LengthSquared<.5f)return false;
            SurfaceVector origin=plane+normal*depth,direction=normal*-1,hitNormal=new SurfaceVector();
            float nearest=depth*2;bool found=Search(root,origin,direction,normal,ref nearest,ref hitNormal,ref shading);
            if(found)point=origin+direction*nearest+hitNormal*Lift;
            return found;
        }
        bool Search(Node node,SurfaceVector origin,SurfaceVector direction,SurfaceVector normal,ref float nearest,ref SurfaceVector hitNormal,ref SurfaceVector shading)
        {
            if(!HitsBox(node,origin,direction,nearest))return false;
            bool found=false;
            if(node.Count==0){found=Search(node.Left,origin,direction,normal,ref nearest,ref hitNormal,ref shading);return Search(node.Right,origin,direction,normal,ref nearest,ref hitNormal,ref shading)||found;}
            for(int i=node.Start;i<node.Start+node.Count;i++){
                SurfaceTriangle t=triangles[i];
                if(SurfaceVector.Dot(t.Normal,normal)<=.35f)continue;
                float distance,u,v;
                if(RayTriangle(origin,direction,t,out distance,out u,out v)&&distance<=nearest){
                    nearest=distance;hitNormal=t.Normal;
                    shading=(t.NA*(1-u-v)+t.NB*u+t.NC*v).Unit;
                    if(shading.LengthSquared<.5f)shading=t.Normal;
                    if(SurfaceVector.Dot(shading,t.Normal)<0)shading=shading*-1;
                    found=true;
                }
            }
            return found;
        }
        static bool HitsBox(Node node,SurfaceVector origin,SurfaceVector direction,float limit)
        {
            float min=0,max=limit;
            for(int i=0;i<3;i++){
                float d=direction.Axis(i),o=origin.Axis(i),lo=node.Min.Axis(i),hi=node.Max.Axis(i);
                if(Math.Abs(d)<1e-10f){if(o<lo-1e-5f||o>hi+1e-5f)return false;continue;}
                float a=(lo-o)/d,b=(hi-o)/d;if(a>b){float tmp=a;a=b;b=tmp;}
                min=Math.Max(min,a);max=Math.Min(max,b);if(min>max+1e-5f)return false;
            }
            return true;
        }
        static bool RayTriangle(SurfaceVector origin,SurfaceVector direction,SurfaceTriangle t,out float distance,out float u,out float v)
        {
            distance=0;u=0;v=0;
            SurfaceVector edge1=t.B-t.A,edge2=t.C-t.A,p=SurfaceVector.Cross(direction,edge2);
            float det=SurfaceVector.Dot(edge1,p);if(Math.Abs(det)<1e-9f)return false;
            SurfaceVector offset=origin-t.A;
            u=SurfaceVector.Dot(offset,p)/det;if(u<-1e-5f||u>1.00001f)return false;
            SurfaceVector q=SurfaceVector.Cross(offset,edge1);
            v=SurfaceVector.Dot(direction,q)/det;if(v<-1e-5f||u+v>1.00001f)return false;
            distance=SurfaceVector.Dot(edge2,q)/det;return distance>=0;
        }
    }
}
