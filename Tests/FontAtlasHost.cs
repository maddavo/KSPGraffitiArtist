// Explicit CPU stand-ins for testing the production atlas capture/crop code.
// This emulates pixel movement, not Unity GPU upload timing or rendering.
using System;
namespace UnityEngine
{
    public static class Debug {public static void LogWarning(object message){}}
    public class Object { public static void Destroy(Object value){} }
    public enum FontStyle {Normal,Bold,Italic,BoldAndItalic}
    public enum TextureFormat {RGBA32}
    public enum RenderTextureFormat {ARGB32}
    public enum RenderTextureReadWrite {Linear}
    public enum TextureWrapMode {Clamp}
    public enum FilterMode {Bilinear}
    public struct Color32 {public byte r,g,b,a;public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;}}
    public struct Color
    {
        public float r,g,b,a;
        public static Color white {get{return new Color(1,1,1,1);}}
        public static Color black {get{return new Color(0,0,0,1);}}
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
    public sealed class Shader : Object
    {
        public bool isSupported=true;
        public static bool Available=true;
        public static Shader Find(string name){return Available?new Shader():null;}
    }
    public sealed class MeshRenderer {public bool receiveShadows;public Rendering.ShadowCastingMode shadowCastingMode;}
    public sealed class Material
    {
        public Texture mainTexture;
        public int renderQueue;
        public System.Collections.Generic.Dictionary<string,float> values=new System.Collections.Generic.Dictionary<string,float>();
        public System.Collections.Generic.Dictionary<string,Color> colors=new System.Collections.Generic.Dictionary<string,Color>();
        public System.Collections.Generic.HashSet<string> keywords=new System.Collections.Generic.HashSet<string>();
        public Material(){}public Material(Shader shader){}
        public void SetFloat(string key,float value){values[key]=value;}
        public void SetInt(string key,int value){values[key]=value;}
        public void SetColor(string key,Color value){colors[key]=value;}
        public void EnableKeyword(string key){keywords.Add(key);}
        public void DisableKeyword(string key){keywords.Remove(key);}
    }
    public static class Resources
    { public static T GetBuiltinResource<T>(string name) where T:new(){return new T();} }
    public sealed class Font : Object
    {
        public Material material=new Material();
        public bool dynamic=true;
        public string loadedPath;
        public static bool PathLoadsDynamic=true;
        public Font(){}
        public Font(string path){loadedPath=path;dynamic=PathLoadsDynamic;}
        public static int creates;
        public static string[] InstalledNames={"Arial","Consolas","consolas","Times New Roman","bad\nfont"};
        public static string[] GetOSInstalledFontNames(){return InstalledNames;}
        public static Font CreateDynamicFontFromOSFont(string name,int size){creates++;return new Font();}
        public static event Action<Font> textureRebuilt;
        public static void Rebuild(Font f){if(textureRebuilt!=null)textureRebuilt(f);}
    }
}
namespace UnityEngine.Rendering
{
    public enum BlendMode {One=1,SrcAlpha=5,OneMinusSrcAlpha=10}
    public enum ShadowCastingMode {Off,On}
}
