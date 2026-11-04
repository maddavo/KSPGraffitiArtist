using System;
using UnityEngine;

namespace KSPGraffitiArtist
{
    public static class FontCoverageAtlas
    {
        static Font cachedFont;
        static Texture source;
        static Texture2D coverage;
        static bool dirty=true;
        static FontCoverageAtlas(){Font.textureRebuilt+=Changed;}
        static void Changed(Font font){if(font==cachedFont)dirty=true;}
        public static Texture2D Get(Font font)
        {
            Texture atlas=font.material.mainTexture;
            if(atlas==null)throw new InvalidOperationException("Font atlas is unavailable");
            if(!dirty && cachedFont==font && source==atlas && coverage!=null)return coverage;
            // Dynamic font textures may not be CPU-readable; copy through a temporary render target.
            RenderTexture previous=RenderTexture.active;
            RenderTexture temporary=RenderTexture.GetTemporary(atlas.width,atlas.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
            Texture2D next=null;
            try {
                Graphics.Blit(atlas,temporary);
                RenderTexture.active=temporary;
                next=new Texture2D(atlas.width,atlas.height,TextureFormat.RGBA32,false);
                next.ReadPixels(new Rect(0,0,atlas.width,atlas.height),0,0,false);
                Color32[] pixels=next.GetPixels32();
                for(int i=0;i<pixels.Length;i++){
                    Color32 p=pixels[i];
                    uint packed=(uint)p.r|((uint)p.g<<8)|((uint)p.b<<16)|((uint)p.a<<24);
                    packed=FontCoverage.WhiteWithCoverage(packed);
                    pixels[i]=new Color32((byte)packed,(byte)(packed>>8),(byte)(packed>>16),(byte)(packed>>24));
                }
                next.SetPixels32(pixels);next.Apply(false,false);
                next.wrapMode=TextureWrapMode.Clamp;next.filterMode=atlas.filterMode;
                if(coverage!=null)UnityEngine.Object.Destroy(coverage);
                coverage=next;cachedFont=font;source=atlas;dirty=false;
                return coverage;
            }catch{if(next!=null)UnityEngine.Object.Destroy(next);throw;}
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(temporary);}
        }
        public static void Clear()
        {
            if(coverage!=null)UnityEngine.Object.Destroy(coverage);
            coverage=null;cachedFont=null;source=null;dirty=true;
        }
        public static Texture2D TintedRegion(Font font,Color color,Vector2 minUV,Vector2 maxUV,out Vector4 remap)
        {
            Texture2D atlas=Get(font);
            int left=Mathf.Clamp(Mathf.FloorToInt(minUV.x*atlas.width)-1,0,atlas.width-1);
            int bottom=Mathf.Clamp(Mathf.FloorToInt(minUV.y*atlas.height)-1,0,atlas.height-1);
            int right=Mathf.Clamp(Mathf.CeilToInt(maxUV.x*atlas.width)+1,left+1,atlas.width);
            int top=Mathf.Clamp(Mathf.CeilToInt(maxUV.y*atlas.height)+1,bottom+1,atlas.height);
            int width=right-left,height=top-bottom;
            var region=new Texture2D(width,height,TextureFormat.RGBA32,false);
            try {
                Color32 ink=color;
                Color32[] original=atlas.GetPixels32(),pixels=new Color32[width*height];
                for(int y=0;y<height;y++)for(int x=0;x<width;x++){
                    Color32 p=original[(bottom+y)*atlas.width+left+x];
                    uint packed=FontCoverage.TintCoverage((uint)p.a<<24,ink.r,ink.g,ink.b);
                    pixels[y*width+x]=new Color32((byte)packed,(byte)(packed>>8),(byte)(packed>>16),(byte)(packed>>24));
                }
                region.SetPixels32(pixels);region.Apply(false,true);region.wrapMode=TextureWrapMode.Clamp;region.filterMode=atlas.filterMode;
                remap=new Vector4((float)atlas.width/width,(float)atlas.height/height,-(float)left/width,-(float)bottom/height);
                return region;
            }catch{UnityEngine.Object.Destroy(region);throw;}
        }
    }
}
