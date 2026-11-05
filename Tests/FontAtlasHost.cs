// Explicit CPU stand-ins for testing the production atlas capture/crop code.
// This emulates pixel movement, not Unity GPU upload timing or rendering.
using System;
namespace UnityEngine
{
    public class Object { public static void Destroy(Object value){} }
    public enum TextureFormat {RGBA32}
    public enum RenderTextureFormat {ARGB32}
    public enum RenderTextureReadWrite {Linear}
    public enum TextureWrapMode {Clamp}
    public enum FilterMode {Bilinear}
    public struct Color32 {public byte r,g,b,a;public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;}}
    public struct Color
    {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}
        public static implicit operator Color32(Color c){return new Color32((byte)(c.r*255),(byte)(c.g*255),(byte)(c.b*255),(byte)(c.a*255));}
    }
    public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
    public struct Vector4 {public float x,y,z,w;public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}}
    public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}}
    public static class Mathf
    {
        public static int Clamp(int v,int min,int max){return Math.Max(min,Math.Min(max,v));}
        public static int FloorToInt(float v){return (int)Math.Floor(v);}
        public static int CeilToInt(float v){return (int)Math.Ceiling(v);}
    }
    public class Texture : Object
    {
        public int width,height;
        public FilterMode filterMode;
        public Color32[] Pixels;
    }
    public sealed class Texture2D : Texture
    {
        public TextureWrapMode wrapMode;
        public Texture2D(int w,int h,TextureFormat format,bool mipmap){width=w;height=h;Pixels=new Color32[w*h];}
        public void ReadPixels(Rect rect,int x,int y,bool mipmap){Pixels=(Color32[])RenderTexture.active.Pixels.Clone();}
        public Color32[] GetPixels32(){return (Color32[])Pixels.Clone();}
        public void SetPixels32(Color32[] data){Pixels=(Color32[])data.Clone();}
        public void Apply(bool mipmap,bool noLongerReadable){}
    }
    public sealed class RenderTexture : Texture
    {
        public static RenderTexture active;
        public static int released;
        public static RenderTexture GetTemporary(int w,int h,int depth,RenderTextureFormat format,RenderTextureReadWrite rw){return new RenderTexture{width=w,height=h,Pixels=new Color32[w*h]};}
        public static void ReleaseTemporary(RenderTexture t){released++;}
    }
    public static class Graphics
    {
        public static int blits;
        public static void Blit(Texture source,RenderTexture target){blits++;target.Pixels=(Color32[])source.Pixels.Clone();}
    }
    public sealed class Material {public Texture mainTexture;}
    public sealed class Font : Object
    {
        public Material material=new Material();
        public static event Action<Font> textureRebuilt;
        public static void Rebuild(Font f){if(textureRebuilt!=null)textureRebuilt(f);}
    }
}
