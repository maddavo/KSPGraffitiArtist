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
    }
}
