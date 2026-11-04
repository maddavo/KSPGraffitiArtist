using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KSPGraffitiArtist
{
    public static class PaintImages
    {
        public const int MaxBytes=4*1024*1024, MaxDimension=2048, MaxImages=64;
        public static string Folder { get { return Path.Combine(KSPUtil.ApplicationRootPath,"GameData/KSPGraffitiArtist/Images"); } }
        static readonly Dictionary<string,Texture2D> cache=new Dictionary<string,Texture2D>(StringComparer.OrdinalIgnoreCase);
        static long cachedPixels;
        public static string[] Names()
        {
            if(!Directory.Exists(Folder))return new string[0];
            return Directory.GetFiles(Folder,"*.png").Select(Path.GetFileName).Where(MarkingData.SafeImageName)
                .OrderBy(n=>n).Take(MaxImages).ToArray();
        }
        public static Texture2D Load(string name)
        {
            if(!MarkingData.SafeImageName(name))return null;
            Texture2D existing; if(cache.TryGetValue(name,out existing))return existing;
            string path=Path.Combine(Folder,name);
            if(!File.Exists(path))return null;
            var info=new FileInfo(path);
            if(info.Length<24 || info.Length>MaxBytes || (info.Attributes&FileAttributes.ReparsePoint)!=0)return null;
            byte[] bytes=File.ReadAllBytes(path);
            // Validate PNG dimensions before Unity allocates native pixel storage.
            byte[] magic={137,80,78,71,13,10,26,10};
            for(int i=0;i<8;i++)if(bytes[i]!=magic[i])return null;
            if(bytes[12]!=73 || bytes[13]!=72 || bytes[14]!=68 || bytes[15]!=82)return null;
            uint width=BigEndian(bytes,16),height=BigEndian(bytes,20);
            if(width==0 || height==0 || width>MaxDimension || height>MaxDimension)return null;
            if(cachedPixels+(long)width*height>16*1024*1024)return null; // 64 MiB decoded RGBA ceiling.
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try {
                if(!ImageConversion.LoadImage(texture,bytes,false)){ UnityEngine.Object.Destroy(texture); return null; }
                texture.wrapMode=TextureWrapMode.Clamp; cache[name]=texture; cachedPixels+=(long)width*height;return texture;
            } catch { UnityEngine.Object.Destroy(texture); throw; }
        }
        static uint BigEndian(byte[] data,int i) { return ((uint)data[i]<<24)|((uint)data[i+1]<<16)|((uint)data[i+2]<<8)|data[i+3]; }
        public static void Clear() { foreach(Texture2D t in cache.Values)UnityEngine.Object.Destroy(t); cache.Clear();cachedPixels=0; }
    }

    // A subdivided mesh follows this part's visible hull; there are no decal parts or colliders.
    public sealed class PaintVisual : IDisposable
    {
        static Font font;
        public static Font PaintFont { get { if(font==null)font=Resources.GetBuiltinResource<Font>("Arial.ttf"); return font; } }
        readonly GameObject root;
        readonly Mesh mesh;
        readonly Material material;
        public int TriangleCount { get { return mesh.triangles.Length/3; } }
        public bool MissingImage { get; private set; }
        PaintVisual(Part part)
        {
            root=new GameObject("GraffitiArtistPaint"); root.layer=part.gameObject.layer;
            root.transform.SetParent(part.transform,false);
            mesh=new Mesh(); mesh.name="Graffiti surface mesh";
            root.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=root.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows=false;
            Shader shader=Shader.Find("Sprites/Default");
            if(shader==null) { UnityEngine.Object.Destroy(mesh); UnityEngine.Object.Destroy(root); throw new InvalidOperationException("Sprites/Default shader missing"); }
            material=new Material(shader); material.renderQueue=3001;
            renderer.sharedMaterial=material;
        }
        public static PaintVisual Create(Part part,MarkingData data)
        {
            var visual=new PaintVisual(part);
            try { visual.Build(part,data); return visual; }
            catch { visual.Dispose(); throw; }
        }
        void Build(Part part,MarkingData m)
        {
            var surface=new VisibleSurface(part);
            Vector3 centre=part.transform.TransformPoint(new Vector3(m.X,m.Y,m.Z));
            Vector3 normal=part.transform.TransformDirection(new Vector3(m.NX,m.NY,m.NZ)).normalized;
            Vector3 up=part.transform.TransformDirection(new Vector3(m.UX,m.UY,m.UZ)).normalized;
            // Positive rotation is clockwise when looking at the painted face.
            up=Quaternion.AngleAxis(-m.Rotation,normal)*up;
            Vector3 right=Vector3.Cross(up,normal).normalized;
            float depth=Mathf.Clamp(m.Width*.5f,.15f,1.25f);
            var vertices=new List<Vector3>(); var uv=new List<Vector2>(); var triangles=new List<int>();
            if(m.Kind=="image") {
                Texture2D image=PaintImages.Load(m.Image); MissingImage=image==null;
                if(image==null)return;
                material.mainTexture=image; material.color=Color.white;
                float height=m.Width*image.height/image.width;
                if(height>4)throw new InvalidOperationException("Picture height exceeds 4 m; reduce its width.");
                AddQuad(part,surface,centre,normal,right,up,depth,
                    -m.Width/2,-height/2,m.Width/2,height/2,new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1),vertices,uv,triangles);
            } else {
                var generator=new TextGenerator();
                try {
                    var settings=new TextGenerationSettings {
                        font=PaintFont,fontSize=96,fontStyle=FontStyle.Normal,color=Color.white,
                        lineSpacing=1,richText=false,scaleFactor=1,textAnchor=TextAnchor.MiddleCenter,
                        horizontalOverflow=HorizontalWrapMode.Overflow,verticalOverflow=VerticalWrapMode.Overflow,
                        generationExtents=new Vector2(8192,8192),pivot=new Vector2(.5f,.5f),generateOutOfBounds=true
                    };
                    if(!generator.Populate(m.Text,settings))throw new InvalidOperationException("Font cannot lay out the entered text");
                    var chars=generator.verts;
                    // Unity's TextGenerator appends an empty quad; ignore all degenerate quads.
                    var quads=new List<UIVertex[]>();
                    float minX=float.MaxValue,minY=float.MaxValue,maxX=float.MinValue,maxY=float.MinValue;
                    for(int i=0;i+3<chars.Count;i+=4) {
                        UIVertex[] q={chars[i],chars[i+1],chars[i+2],chars[i+3]};
                        if((q[0].position-q[2].position).sqrMagnitude<.001f)continue;
                        quads.Add(q);
                        foreach(UIVertex v in q){minX=Mathf.Min(minX,v.position.x);minY=Mathf.Min(minY,v.position.y);maxX=Mathf.Max(maxX,v.position.x);maxY=Mathf.Max(maxY,v.position.y);}
                    }
                    if(quads.Count==0 || maxX<=minX)throw new InvalidOperationException("Text contains no visible glyphs");
                    float scale=m.Width/(maxX-minX),cx=(minX+maxX)/2,cy=(minY+maxY)/2;
                    if((maxY-minY)*scale>4)throw new InvalidOperationException("Text height exceeds 4 m; reduce its width or line count.");
                    foreach(UIVertex[] q in quads) {
                        AddQuad(part,surface,centre,normal,right,up,depth,
                            (q[3].position.x-cx)*scale,(q[3].position.y-cy)*scale,
                            (q[1].position.x-cx)*scale,(q[1].position.y-cy)*scale,
                            q[3].uv0,q[2].uv0,q[1].uv0,q[0].uv0,vertices,uv,triangles);
                    }
                    material.mainTexture=PaintFont.material.mainTexture;
                    material.color=new Color(m.R,m.G,m.B,1);
                } finally { ((IDisposable)generator).Dispose(); }
            }
            if(vertices.Count>65535)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
        }
        static void AddQuad(Part part,VisibleSurface surface,Vector3 centre,Vector3 normal,Vector3 right,Vector3 up,float depth,
            float x0,float y0,float x1,float y1,Vector2 uv00,Vector2 uv10,Vector2 uv11,Vector2 uv01,
            List<Vector3> vertices,List<Vector2> uv,List<int> triangles)
        {
            // Finer sampling reduces inward chords between samples on curved hulls.
            int nx=Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(x1-x0)/SurfaceGeometry.SampleSpacing),1,200);
            int ny=Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(y1-y0)/SurfaceGeometry.SampleSpacing),1,200);
            Vector3[] points=new Vector3[(nx+1)*(ny+1)]; bool[] valid=new bool[points.Length];
            for(int y=0;y<=ny;y++) for(int x=0;x<=nx;x++) {
                Vector3 plane=centre+right*Mathf.Lerp(x0,x1,(float)x/nx)+up*Mathf.Lerp(y0,y1,(float)y/ny);
                Vector3 point;
                int i=y*(nx+1)+x; valid[i]=surface.Project(plane,normal,depth,out point);
                points[i]=part.transform.InverseTransformPoint(point);
            }
            for(int y=0;y<ny;y++)for(int x=0;x<nx;x++) {
                int a=y*(nx+1)+x,b=a+1,d=a+nx+1,c=d+1;
                if(!valid[a]||!valid[b]||!valid[c]||!valid[d])continue;
                Vector3 ignored;
                Vector3 mid=centre+right*Mathf.Lerp(x0,x1,(x+.5f)/nx)+up*Mathf.Lerp(y0,y1,(y+.5f)/ny);
                if(!surface.Project(mid,normal,depth,out ignored))continue;
                int first=vertices.Count;
                vertices.Add(points[a]); vertices.Add(points[b]); vertices.Add(points[c]); vertices.Add(points[d]);
                float s0=(float)x/nx,s1=(float)(x+1)/nx,t0=(float)y/ny,t1=(float)(y+1)/ny;
                uv.Add(Vector2.Lerp(Vector2.Lerp(uv00,uv10,s0),Vector2.Lerp(uv01,uv11,s0),t0));
                uv.Add(Vector2.Lerp(Vector2.Lerp(uv00,uv10,s1),Vector2.Lerp(uv01,uv11,s1),t0));
                uv.Add(Vector2.Lerp(Vector2.Lerp(uv00,uv10,s1),Vector2.Lerp(uv01,uv11,s1),t1));
                uv.Add(Vector2.Lerp(Vector2.Lerp(uv00,uv10,s0),Vector2.Lerp(uv01,uv11,s0),t1));
                triangles.AddRange(new int[]{first,first+1,first+2,first,first+2,first+3});
            }
        }
        public void Dispose() { UnityEngine.Object.Destroy(root); UnityEngine.Object.Destroy(mesh); UnityEngine.Object.Destroy(material); }
    }
}
