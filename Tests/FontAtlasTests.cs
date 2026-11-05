using System;
using KSPGraffitiArtist;
using UnityEngine;
class FontAtlasTests
{
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static int Main()
    {
        try {
            FontCoverageAtlas.Clear();
            var source=new Texture2D(4,2,TextureFormat.RGBA32,false);
            source.Pixels[0]=new Color32(0,0,0,255); // E initially resident.
            var font=new Font();font.material.mainTexture=source;
            var previous=new RenderTexture();RenderTexture.active=previous;
            Texture2D first=FontCoverageAtlas.Get(font);
            Check(first.GetPixels32()[0].a==255,"resident E copied");
            Check(first.GetPixels32()[1].a==0,"A initially absent");
            // The critical case: additions without a rebuild event, pointer or size change.
            source.Pixels[1]=new Color32(0,0,0,223);
            source.Pixels[2]=new Color32(0,0,0,191);
            source.Pixels[3]=new Color32(0,0,0,159);
            source.Pixels[4]=new Color32(0,0,0,127);
            Texture2D second=FontCoverageAtlas.Get(font);
            Check(second.GetPixels32()[1].a==223,"new A must not be missing from a cached E-only snapshot");
            Check(second.GetPixels32()[2].a==191,"new G copied without rebuild event");
            Check(second.GetPixels32()[3].a==159,"new L copied without rebuild event");
            Check(second.GetPixels32()[4].a==127,"new numeral copied without rebuild event");
            Check(second.GetPixels32()[1].r==255&&second.GetPixels32()[1].g==255,"coverage is neutral RGB");
            Check(RenderTexture.active==previous,"prior render target restored");
            Check(Graphics.blits==2&&RenderTexture.released==2,"each capture reads current pixels and releases its temporary target");
            Vector4 remap;
            Texture2D red=FontCoverageAtlas.TintedRegion(font,new Color(1,0,0,1),new Vector2(0,0),new Vector2(1,1),out remap);
            Check(red.Pixels[1].a==223&&red.Pixels[1].r==255&&red.Pixels[1].g==0&&red.Pixels[1].b==0,"new glyph retains alpha and red pigment");
            source.Pixels[1]=new Color32(0,0,0,64);
            Check(red.Pixels[1].a==223,"existing owned text crop is independent of subsequent font updates");
            Texture2D blue=FontCoverageAtlas.TintedRegion(font,new Color(0,0,1,1),new Vector2(0,0),new Vector2(1,1),out remap);
            Check(blue.Pixels[1].a==64&&blue.Pixels[1].b==255&&blue.Pixels[1].r==0,"next crop sees in-place atlas update and new pigment");
            var larger=new Texture2D(8,4,TextureFormat.RGBA32,false);larger.Pixels[31]=new Color32(0,0,0,87);font.material.mainTexture=larger;
            Texture2D resized=FontCoverageAtlas.Get(font);
            Check(resized.width==8&&resized.height==4&&resized.GetPixels32()[31].a==87,"replaced/resized font texture captured");
            Font.Rebuild(font);larger.Pixels[31]=new Color32(0,0,0,199);
            Check(FontCoverageAtlas.Get(font).GetPixels32()[31].a==199,"explicit rebuild also captured");
            FontCoverageAtlas.Clear();Check(FontCoverageAtlas.Get(font).GetPixels32()[31].a==199,"scene teardown/reset can recapture");
            Console.WriteLine("PASS: "+checks+" production font-atlas capture/crop host assertions. GPU execution/timing not simulated.");return 0;
        }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
