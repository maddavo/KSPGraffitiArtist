using System;
using System.Collections.Generic;
using UnityEngine;

namespace KSPGraffitiArtist
{
    // Only draw-ready meshes owned by the selected part participate, never collision geometry.
    public sealed class VisibleSurface
    {
        readonly Part part;
        readonly SurfaceGeometry geometry;
        static SurfaceVector V(Vector3 v){return new SurfaceVector(v.x,v.y,v.z);}
        public static Vector3 RightTangent(Vector3 outward,Vector3 up)
        {
            SurfaceVector right=SurfaceGeometry.RightTangent(V(outward),V(up));
            return new Vector3(right.X,right.Y,right.Z);
        }
        public VisibleSurface(Part part)
        {
            this.part=part;
            var triangles=new List<SurfaceTriangle>();
            foreach(MeshFilter filter in part.GetComponentsInChildren<MeshFilter>()){
                MeshRenderer renderer=filter.GetComponent<MeshRenderer>();
                if(renderer==null || !renderer.enabled || !filter.gameObject.activeInHierarchy
                    || filter.GetComponentInParent<Part>()!=part || filter.gameObject.name=="GraffitiArtistPaint")continue;
                Mesh source=filter.sharedMesh;
                if(source==null)continue;
                if(!source.isReadable)throw new InvalidOperationException("This hull's visible mesh is not CPU-readable; paint projection is unavailable.");
                Matrix4x4 matrix=part.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                Matrix4x4 normals=matrix.inverse.transpose;
                Vector3[] vertices=source.vertices,vertexNormals=source.normals;int[] indices=source.triangles;
                for(int i=0;i+2<indices.Length;i+=3){
                    int a=indices[i],b=indices[i+1],c=indices[i+2];
                    SurfaceVector pa=V(matrix.MultiplyPoint3x4(vertices[a])),pb=V(matrix.MultiplyPoint3x4(vertices[b])),pc=V(matrix.MultiplyPoint3x4(vertices[c]));
                    if(vertexNormals.Length==vertices.Length)
                        triangles.Add(new SurfaceTriangle(pa,pb,pc,V(normals.MultiplyVector(vertexNormals[a])),V(normals.MultiplyVector(vertexNormals[b])),V(normals.MultiplyVector(vertexNormals[c]))));
                    else triangles.Add(new SurfaceTriangle(pa,pb,pc,new SurfaceVector()));
                }
            }
            geometry=new SurfaceGeometry(triangles);
            if(geometry.Count==0)throw new InvalidOperationException("This part has no readable visible hull mesh to paint.");
        }
        public bool Project(Vector3 plane,Vector3 normal,float depth,out Vector3 point)
        { Vector3 shading;return Project(plane,normal,depth,out point,out shading); }
        public bool Project(Vector3 plane,Vector3 normal,float depth,out Vector3 point,out Vector3 shading)
        {
            Vector3 localDirection=part.transform.InverseTransformVector(normal);
            float scale=localDirection.magnitude;
            SurfaceVector hit,localNormal;
            bool found=geometry.Project(V(part.transform.InverseTransformPoint(plane)),V(localDirection),depth*scale,out hit,out localNormal);
            point=found?part.transform.TransformPoint(new Vector3(hit.X,hit.Y,hit.Z)):Vector3.zero;
            shading=found?part.transform.worldToLocalMatrix.transpose.MultiplyVector(new Vector3(localNormal.X,localNormal.Y,localNormal.Z)).normalized:Vector3.zero;
            return found;
        }
    }
}
