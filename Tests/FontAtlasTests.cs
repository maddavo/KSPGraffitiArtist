using System;
using System.IO;
using System.Linq;
using System.Text;
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
            Check(red.filterMode==FilterMode.Bilinear&&blue.filterMode==FilterMode.Bilinear,"owned pigment textures use bilinear coverage filtering");
            PaintFonts.Clear();
            Check(PaintFonts.Names.Length>=3&&!PaintFonts.Names.Contains("bad\nfont")&&PaintFonts.Names.Count(n=>n.Equals("Consolas",StringComparison.OrdinalIgnoreCase))==1,"font picker merges sources without duplicate/unsafe names");
            Font arial=PaintFonts.Get("Arial");
            Check(PaintFonts.Get("No Longer Installed")==arial,"missing family displays builtin without mutating saved metadata");
            Font consolas=PaintFonts.Get("Consolas");
            Check(PaintFonts.Get("CONSOLAS")==consolas&&Font.creates==1,"dynamic font reused across labels with case insensitive family");
            Check(PaintFonts.Style("Regular")==FontStyle.Normal&&PaintFonts.Style("Bold")==FontStyle.Bold&&PaintFonts.Style("Italic")==FontStyle.Italic&&PaintFonts.Style("BoldItalic")==FontStyle.BoldAndItalic,"all persisted styles map to engine styles");
            PaintFonts.Clear();Check(PaintFonts.Get("Consolas")!=consolas,"flight teardown releases dynamic font cache");
            string temp=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")+".otf");
            try{
                // Minimal SFNT naming fixture only; this is not a font for rendering.
                byte[] family=Encoding.BigEndianUnicode.GetBytes("Countdown");
                byte[] bytes=new byte[46+family.Length];
                bytes[0]=79;bytes[1]=84;bytes[2]=84;bytes[3]=79;bytes[5]=1;
                bytes[12]=110;bytes[13]=97;bytes[14]=109;bytes[15]=101;bytes[23]=28;bytes[27]=(byte)(18+family.Length);
                bytes[31]=1;bytes[33]=18;bytes[35]=3;bytes[37]=1;bytes[38]=4;bytes[39]=9;bytes[41]=1;bytes[43]=(byte)family.Length;
                Array.Copy(family,0,bytes,46,family.Length);File.WriteAllBytes(temp,bytes);
                Check(UserFonts.ReadFamily(temp)=="Countdown","read per-user OTF internal family name rather than guessing from filename");
                bytes[43]=255;File.WriteAllBytes(temp,bytes);
                Check(UserFonts.ReadFamily(temp)==null,"out of bounds font name record rejected");
                File.WriteAllBytes(temp,new byte[]{79,84,84,79});
                Check(UserFonts.ReadFamily(temp)==null,"truncated font rejected without breaking catalogue");
            }finally{if(File.Exists(temp))File.Delete(temp);}
            var userFonts=UserFonts.Discover();string countdownPath;
            if(userFonts.TryGetValue("Countdown",out countdownPath)){
                PaintFonts.Refresh();
                Check(PaintFonts.Available("Countdown"),"registered Countdown appears even when Unity host enumeration omits it");
                Check(PaintFonts.Get("Countdown").loadedPath==countdownPath.Replace('\\','/'),"Countdown uses normalized registered file path instead of name lookup");
                PaintFonts.Clear();Font.PathLoadsDynamic=false;
                Check(PaintFonts.Get("Countdown")==null,"non-dynamic native font selects Windows coverage instead of failing");
                Font.PathLoadsDynamic=true;
                Check(PaintFonts.Get("Countdown")==null,"fallback selection persists without repeatedly loading the failed native font");
                PaintFonts.Clear();
                Check(PaintFonts.Get("Countdown")!=null,"leaving flight clears failed-native route state");
            }
            Font.InstalledNames=new[]{"Arial","Fresh Installed"};PaintFonts.Refresh();
            Check(PaintFonts.Available("Fresh Installed"),"opening font picker refreshes cached enumeration");
            var renderer=new MeshRenderer();
            Material paint=PaintMaterial.Create(renderer);
            Check(paint.values["_Mode"]==3&&paint.values["_SrcBlend"]==1&&paint.values["_DstBlend"]==10,"paint uses the available premultiplied transparency variant");
            Check(paint.values["_ZWrite"]==0&&paint.renderQueue==3001,"transparent pixels do not write a rectangular depth mask");
            Check(paint.keywords.Contains("_ALPHAPREMULTIPLY_ON")&&!paint.keywords.Contains("_ALPHATEST_ON")&&!paint.keywords.Contains("_ALPHABLEND_ON"),"unavailable Fade variant and alpha clipping are disabled");
            Color spec=paint.colors["_SpecColor"];
            Check(spec.r==0&&spec.g==0&&spec.b==0&&paint.values["_Glossiness"]==0&&paint.colors["_EmissionColor"].r==0&&!paint.keywords.Contains("_EMISSION"),"black specular pigment has no transparent-quad highlights or emission");
            Check(!paint.keywords.Contains("_SPECULARHIGHLIGHTS_OFF")&&!paint.keywords.Contains("_GLOSSYREFLECTIONS_OFF"),"do not request stripped highlight/reflection variants");
            Check(renderer.receiveShadows&&renderer.shadowCastingMode==UnityEngine.Rendering.ShadowCastingMode.Off,"paint requests received shadows without a second lifted shadow caster");
            Shader.Available=false;bool refused=false;
            try{PaintMaterial.Create(renderer);}catch(InvalidOperationException){refused=true;}
            Check(refused,"unavailable lit shader fails visibly instead of reverting to unlit");Shader.Available=true;
            Console.WriteLine("PASS: "+checks+" production font-atlas capture/crop host assertions. GPU execution/timing not simulated.");return 0;
        }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
