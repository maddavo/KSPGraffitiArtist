using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using KSPGraffitiArtist;
class Tests
{
    static int count;
    static void Check(bool ok,string name){count++;if(!ok)throw new Exception("FAIL: "+name);}
    static Dictionary<string,string> Encoded(){return new MarkingData().Encode();}
    static void Bad(string key,string value,string description){var d=Encoded();d[key]=value;MarkingData m;Check(!MarkingData.TryDecode(d,out m),description);}
    static ConfigNode Node(MarkingData m){var n=new ConfigNode("MARKING");foreach(var v in m.Encode())n.AddValue(v.Key,v.Value);return n;}
    static int Main()
    {
        try {
            var menuController=new UIPartActionController();var hull=new Part();
            PawIntegration.DismissForPaint(null,hull);PawIntegration.DismissForPaint(menuController,null);
            Check(menuController.dismissCalls==0,"null PAW inputs do nothing");
            PawIntegration.DismissForPaint(menuController,hull);
            Check(menuController.dismissCalls==0,"no target PAW does not dismiss unrelated UI");
            var pinned=new UIPartActionWindow{part=hull,Pinned=true};menuController.windows.Add(pinned);
            PawIntegration.DismissForPaint(menuController,hull);
            Check(menuController.dismissCalls==0&&menuController.windows.Contains(pinned),"pinned PAW retained");
            pinned.Pinned=false;PawIntegration.DismissForPaint(menuController,hull);
            Check(menuController.dismissCalls==1&&menuController.dismissEvents==1,"native dismiss adapter invoked once");
            Check(menuController.windows.Count==0&&menuController.hidden.Contains(pinned),"host stock path retains reusable window");
            PawIntegration.DismissForPaint(menuController,hull);
            Check(menuController.dismissCalls==1,"subsequent editor frame does not dismiss twice");
            menuController.hidden.Remove(pinned);menuController.windows.Add(pinned);
            PawIntegration.DismissForPaint(menuController,hull);
            Check(menuController.dismissEvents==2&&menuController.hidden.Count==1,"reopened PAW can be dismissed normally again");
            Check(FontCoverage.WhiteWithCoverage(0xFF000000u)==0xFFFFFFFFu,"opaque black font RGB becomes neutral white coverage");
            Check(FontCoverage.WhiteWithCoverage(0x80000000u)==0x80FFFFFFu,"font antialias alpha preserved");
            Check(FontCoverage.WhiteWithCoverage(0x00123456u)==0x00FFFFFFu,"transparent font background stays transparent");
            for(uint alpha=0;alpha<256;alpha++){
                uint whitened=FontCoverage.WhiteWithCoverage((alpha<<24)|0x007A1234u);
                Check(whitened==((alpha<<24)|0x00FFFFFFu),"font RGB independence and alpha "+alpha);
            }
            Check((FontCoverage.WhiteWithCoverage(0xFF000000u)&0xFF)==255,"red tint is not multiplied by black atlas RGB");
            for(uint alpha=0;alpha<256;alpha++)
                Check(FontCoverage.TintCoverage((alpha<<24)|0x00123456u,255,0,0)==((alpha<<24)|0x000000FFu),"red pigment and alpha "+alpha);
            Check(FontCoverage.TintCoverage(0xFF000000u,1,127,240)==0xFFF07F01u,"RGB pigment channels preserved");
            Check(FontCoverage.TintCoverage(0x00123456u,255,255,255)==0x00FFFFFFu,"transparent pigment remains transparent");
            var original=new MarkingData{Text="HAB 03\n燃料 🚀 = {part}\r\n",Rotation=359.9f,Width=.1f};
            MarkingData decoded;
            Check(MarkingData.TryDecode(original.Encode(),out decoded),"decode valid text");
            Check(decoded.Text==original.Text && decoded.Id==original.Id,"Unicode/newlines/identity preserved");
            Check(decoded.Width==original.Width && decoded.Rotation==original.Rotation,"floats round-trip");
            CultureInfo before=CultureInfo.CurrentCulture;
            try {
                foreach(string culture in new[]{"en-AU","de-DE","fr-FR"}){
                    CultureInfo.CurrentCulture=new CultureInfo(culture);
                    Check(MarkingData.TryDecode(original.Encode(),out decoded)&&decoded.Width==.1f,"invariant codec in "+culture);
                }
            }finally{CultureInfo.CurrentCulture=before;}
            Bad("schema","99","future schema rejected");Bad("id","not-a-guid","malformed ID");
            Bad("width","NaN","NaN width");Bad("width","Infinity","infinite width");
            Bad("width","0.09","minimum width");Bad("width","4.01","maximum width");
            Bad("rotation","-1","negative rotation");Bad("rotation","360","unnormalized rotation");
            Bad("x","Infinity","infinite anchor");Bad("x","10001","excessive anchor");
            Bad("nz","0","zero normal");Bad("uy","2","invalid up length");Bad("uz","1","parallel up component");
            Bad("r","-0.1","negative color");Bad("b","1.1","excess color");
            Bad("kind","script","unknown content");Bad("font","bad\nfont","unsafe font");
            Bad("style","Outline","unknown style");Bad("style","","empty style");
            foreach(string family in new[]{"Arial","Consolas","Times New Roman","Missing Font", "游ゴシック"})
                foreach(string style in new[]{"Regular","Bold","Italic","BoldItalic"}){
                    var styled=original.Copy();styled.Font=family;styled.Style=style;
                    Check(MarkingData.TryDecode(styled.Encode(),out decoded)&&decoded.Font==family&&decoded.Style==style,"font/style round trip "+family+style);
                }
            var legacy=original.Encode();legacy["schema"]="1";legacy.Remove("style");
            Check(MarkingData.TryDecode(legacy,out decoded)&&decoded.Font=="Arial"&&decoded.Style=="Regular"&&decoded.Text==original.Text,"legacy schema 1 defaults to regular Arial");
            Check(decoded.Encode()["schema"]=="2","new records use schema 2 for safe retention by older plugins");
            foreach(string name in new[]{""," "," bad","bad ","bad\rfont","bad{font}","bad=font","bad/font",new string('A',129)})
                Check(!MarkingData.SafeFontName(name),"unsafe font metadata rejected");
            Bad("text64","%%%","invalid base64");Bad("text64",Convert.ToBase64String(new byte[]{255}),"invalid UTF8");
            Bad("text64",Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(new string('x',513))),"text length bound");
            Bad("text64",new string('x',4097),"encoded length bound");
            var incomplete=Encoded();incomplete.Remove("nx");Check(!MarkingData.TryDecode(incomplete,out decoded),"missing field");
            incomplete=Encoded();incomplete["width"]=null;Check(!MarkingData.TryDecode(incomplete,out decoded),"null value");
            Check(!MarkingData.TryDecode(null,out decoded),"null dictionary");
            foreach(string path in new[]{"../escape.png","..\\escape.png","C:\\secret.png","a.png:stream","a.jpg","","/root.png"})Check(!MarkingData.SafeImageName(path),"reject path "+path);
            Check(MarkingData.SafeImageName("direction-arrow.png"),"safe PNG name");
            Check(MarkingData.SafeImageName("Fuel 02.PNG"),"uppercase extension");
            var image=new MarkingData{Kind="image",Image="gone.png",Text=""};
            Check(MarkingData.TryDecode(image.Encode(),out decoded)&&decoded.Image=="gone.png","missing asset reference is retained");
            var random=new Random(1234);
            for(int i=0;i<200;i++){
                var m=new MarkingData{X=(float)random.NextDouble()*20,Y=(float)random.NextDouble()*20,Width=.1f+(float)random.NextDouble()*3.9f,Rotation=(float)random.NextDouble()*359.9f,Text="test "+i,R=(float)random.NextDouble()};
                Check(MarkingData.TryDecode(m.Encode(),out decoded)&&decoded.X==m.X&&decoded.Width==m.Width&&decoded.Text==m.Text,"randomized round-trip "+i);
            }
            Check(MarkingData.Wrapped(-1)==359 && MarkingData.Wrapped(720)==0,"rotation wrapping");
            var store=new MarkingCollection();Check(store.Commit(original),"commit initial record");
            var draft=store.Snapshot()[0].Copy();draft.Text="UNCOMMITTED";
            Check(store.Snapshot()[0].Text==original.Text,"cancel draft preserves stored text");
            var exposed=store.Snapshot()[0];exposed.Text="MUTATION";
            Check(store.Snapshot()[0].Text==original.Text,"snapshot cannot mutate store");
            Check(store.Commit(draft),"apply edited draft");draft.Text="LATER";
            Check(store.Snapshot()[0].Text=="UNCOMMITTED","commit copies draft");
            Check(!store.Restore(original),"duplicate restore refused");
            var invalid=original.Copy();invalid.Width=0;Check(!store.Commit(invalid)&&store.Snapshot()[0].Width==.1f,"failed apply is atomic");
            for(int i=1;i<32;i++)Check(store.Commit(new MarkingData()),"capacity slot "+i);
            Check(!store.Commit(new MarkingData())&&store.Snapshot().Count==32,"capacity ceiling");
            draft.Width=2;Check(store.Commit(draft),"editing existing at capacity");
            store.Erase(original.Id);Check(store.Snapshot().Count==31&&!store.Snapshot().Any(m=>m.Id==original.Id),"erase only selected marking");
            Check(store.Commit(new MarkingData()),"erase releases capacity");
            var module=new ModuleGraffitiSurface();module.Commit(original);
            var save=new ConfigNode();module.OnSave(save);Check(save.GetNodes("MARKING").Length==1,"module writes one marking");
            var restored=new ModuleGraffitiSurface();restored.OnLoad(save);restored.OnLoad(save);
            Check(restored.Records.Count==1&&restored.Records[0].Text==original.Text,"repeated load does not duplicate");
            var future=new ConfigNode("MARKING");future.AddValue("schema","99");future.AddValue("custom","retain me");save.AddNode(future);
            save.AddNode(Node(original)); // duplicate is retained, rather than silently overwritten.
            restored.OnLoad(save);var output=new ConfigNode();restored.OnSave(output);
            Check(output.GetNodes("MARKING").Length==3,"unknown schema and duplicate preserved");
            Check(output.GetNodes("MARKING").Any(n=>n.values.Any(v=>v.name=="custom"&&v.value=="retain me")),"unknown fields preserved");
            restored.Suppress(original.Id);var previewSave=new ConfigNode();restored.OnSave(previewSave);
            Check(previewSave.GetNodes("MARKING").Length==3,"preview suppression never deletes saved records");
            restored.Erase(original.Id);output=new ConfigNode();restored.OnSave(output);
            Check(restored.Records.Count==0&&output.GetNodes("MARKING").Length==2,"erase retains unrecognized records");
            var world=new WorldSurfaceData{Body="Mun",Latitude=12.345678912345,Longitude=-123.45678912345,Altitude=4321.123456789};
            world.Marks.Commit(new MarkingData{Font="Consolas",Style="BoldItalic",Text="MUNBASE"});
            WorldSurfaceData anchor;
            foreach(string culture in new[]{"en-AU","de-DE","fr-FR"}){
                CultureInfo.CurrentCulture=new CultureInfo(culture);
                Check(WorldSurfaceData.TryDecode(world.Encode(),out anchor)&&anchor.Latitude==world.Latitude&&anchor.Longitude==world.Longitude&&anchor.Altitude==world.Altitude,"double terrain coordinates round trip in "+culture);
            }
            CultureInfo.CurrentCulture=before;
            foreach(var invalidWorld in new[]{new[]{"latitude","91"},new[]{"latitude","NaN"},new[]{"longitude","181"},new[]{"altitude","Infinity"},new[]{"altitude","10000001"},new[]{"kind","ocean"},new[]{"id","not-an-id"},new[]{"body",""},new[]{"body","bad\nname"},new[]{"schema","99"}}){
                var encoded=world.Encode();encoded[invalidWorld[0]]=invalidWorld[1];
                Check(!WorldSurfaceData.TryDecode(encoded,out anchor),"invalid world anchor rejected "+invalidWorld[0]+invalidWorld[1]);
            }
            var worldStore=new WorldPaintStore();worldStore.Records.Add(world);
            var worldSave=new ConfigNode();worldStore.Save(worldSave);
            var worldRestored=new WorldPaintStore();worldRestored.Load(worldSave);worldRestored.Load(worldSave);
            Check(worldRestored.Records.Count==1,"world reload does not duplicate patches");
            Check(worldRestored.Records[0].Marks.Snapshot()[0].Font=="Consolas"&&worldRestored.Records[0].Marks.Snapshot()[0].Style=="BoldItalic","world save retains font and style");
            Check(worldRestored.Records[0].Latitude==world.Latitude,"scenario store retains double coordinates");
            var worldDraft=worldRestored.Records[0].Marks.Snapshot()[0];worldDraft.Text="CANCELLED";worldDraft.X=1;
            Check(worldRestored.Records[0].Marks.Snapshot()[0].Text=="MUNBASE","world draft cancel leaves saved data intact");
            worldRestored.Records[0].Marks.Commit(worldDraft);var movedSave=new ConfigNode();worldRestored.Save(movedSave);worldRestored.Load(movedSave);
            Check(worldRestored.Records[0].Marks.Snapshot()[0].X==1,"world applied movement survives save reload");
            var unknown=new ConfigNode("WORLD_SURFACE");unknown.AddValue("schema","99");unknown.AddValue("recovery","do not erase");worldSave.AddNode(unknown);
            var badChild=worldSave.GetNodes("WORLD_SURFACE")[0].CreateCopy();badChild.AddNode(new ConfigNode("MARKING"));worldSave.AddNode(badChild);
            var duplicate=worldSave.GetNodes("WORLD_SURFACE")[0].CreateCopy();worldSave.AddNode(duplicate);
            worldRestored.Load(worldSave);var preserved=new ConfigNode();worldRestored.Save(preserved);
            Check(worldRestored.Records.Count==1&&preserved.GetNodes("WORLD_SURFACE").Length==4,"unknown anchor, malformed child and duplicate preserved intact");
            Check(preserved.GetNodes("WORLD_SURFACE").Any(n=>n.values.Any(v=>v.name=="recovery"&&v.value=="do not erase")),"future world data survives older reader");
            worldRestored.Records.Add(new WorldSurfaceData{Body="Mun"});preserved=new ConfigNode();worldRestored.Save(preserved);
            Check(preserved.GetNodes("WORLD_SURFACE").Length==4,"empty cancelled world patches are not saved");
            var kk=new WorldSurfaceData{Kind="kk",Body="Kerbin"};kk.Marks.Commit(new MarkingData{Text="HANGAR"});
            worldRestored.Records.Add(kk);preserved=new ConfigNode();worldRestored.Save(preserved);var reload=new WorldPaintStore();reload.Load(preserved);
            Check(reload.Records.Any(r=>r.Kind=="kk"&&r.Id==kk.Id&&r.Marks.Snapshot()[0].Text=="HANGAR"),"KK UUID and local label retained independently of a live instance");
            Console.WriteLine("PASS: "+count+" assertions using production record/store/module/coverage source.");
            GeometryTests.Run();
            Console.WriteLine("Host substitutes ConfigNode and engine lifecycle only; no in-game result is implied.");return 0;
        }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
