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
                    Vector3 outward=vertexNormals.Length==vertices.Length?normals.MultiplyVector(vertexNormals[a]+vertexNormals[b]+vertexNormals[c]):Vector3.zero;
                    triangles.Add(new SurfaceTriangle(V(matrix.MultiplyPoint3x4(vertices[a])),V(matrix.MultiplyPoint3x4(vertices[b])),V(matrix.MultiplyPoint3x4(vertices[c])),V(outward)));
                }
            }
            geometry=new SurfaceGeometry(triangles);
            if(geometry.Count==0)throw new InvalidOperationException("This part has no readable visible hull mesh to paint.");
        }
        public bool Project(Vector3 plane,Vector3 normal,float depth,out Vector3 point)
        {
            Vector3 localDirection=part.transform.InverseTransformVector(normal);
            float scale=localDirection.magnitude;
            SurfaceVector hit;
            bool found=geometry.Project(V(part.transform.InverseTransformPoint(plane)),V(localDirection),depth*scale,out hit);
            point=found?part.transform.TransformPoint(new Vector3(hit.X,hit.Y,hit.Z)):Vector3.zero;
            return found;
        }
    }
}
