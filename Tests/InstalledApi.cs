using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
class InstalledApi
{
    static int count;
    static void Check(bool ok,string name){count++;if(!ok)throw new Exception(name);}
    static List<MethodBase> Calls(MethodBase method)
    {
        var codes=typeof(OpCodes).GetFields(BindingFlags.Public|BindingFlags.Static).Where(f=>f.FieldType==typeof(OpCode)).Select(f=>(OpCode)f.GetValue(null)).ToDictionary(c=>c.Value);
        byte[] il=method.GetMethodBody().GetILAsByteArray();var calls=new List<MethodBase>();int i=0;
        while(i<il.Length){
            short raw=il[i++];if(raw==0xfe)raw=(short)(0xfe00|il[i++]);OpCode op=codes[raw];
            if(op.OperandType==OperandType.InlineMethod){calls.Add(method.Module.ResolveMethod(BitConverter.ToInt32(il,i)));i+=4;continue;}
            switch(op.OperandType){
                case OperandType.InlineNone:break;
                case OperandType.ShortInlineI:case OperandType.ShortInlineVar:case OperandType.ShortInlineBrTarget:i++;break;
                case OperandType.InlineVar:i+=2;break;
                case OperandType.InlineI8:case OperandType.InlineR:i+=8;break;
                case OperandType.InlineSwitch:int n=BitConverter.ToInt32(il,i);i+=4+n*4;break;
                default:i+=4;break;
            }
        }
        return calls;
    }
    static int Main(string[] args)
    {
        string game=args[0];
        AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{
            string name=new AssemblyName(e.Name).Name+".dll";
            foreach(string dir in new[]{"KSP_x64_Data/Managed","GameData/KIS/Plugins"}) {
                string path=Path.Combine(game,dir,name);if(File.Exists(path))return Assembly.LoadFrom(path);
            }return null;
        };
        try {
            var plugin=Assembly.LoadFrom(args[1]);
            var gameAssembly=Assembly.LoadFrom(Path.Combine(game,"KSP_x64_Data/Managed/Assembly-CSharp.dll"));
            var kis=Assembly.LoadFrom(Path.Combine(game,"GameData/KIS/Plugins/KIS.dll"));
            Type module=plugin.GetType("KSPGraffitiArtist.ModuleGraffitiSurface");
            Check(module!=null && module.BaseType==gameAssembly.GetType("PartModule"),"part-module base");
            Check(plugin.GetType("KSPGraffitiArtist.PaintTool").GetCustomAttributes(false).Any(a=>a.GetType().Name=="KSPAddon"),"flight addon attribute");
            Check(plugin.GetName().Version.ToString()=="0.2.5.0","assembly version");
            Type inventory=kis.GetType("KIS.ModuleKISInventory"),item=kis.GetType("KIS.KIS_Item");
            Check(inventory.GetField("items")!=null,"KIS inventory items");
            Check(item.GetProperty("equipped")!=null,"KIS equipped property");
            Check(item.GetField("availablePart")!=null,"KIS part identification");
            Check(gameAssembly.GetType("UIPartActionController").GetMethod("ItemListGet")!=null,"stock menu lookup API");
            Check(gameAssembly.GetType("UIPartActionController").GetMethod("Deselect",new[]{typeof(bool)})!=null,"native PAW dismissal API");
            Check(gameAssembly.GetType("UIPartActionWindow").GetProperty("Pinned")!=null,"pinned window preservation API");
            Check(gameAssembly.GetType("GameEvents").GetField("onCrewBoardVessel")!=null,"boarding cleanup event");
            Check(gameAssembly.GetType("GameEvents").GetField("onVesselChange")!=null,"vessel change cleanup event");
            Type tool=plugin.GetType("KSPGraffitiArtist.PaintTool");
            var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
            Type paintMaterial=plugin.GetType("KSPGraffitiArtist.PaintMaterial"),visual=plugin.GetType("KSPGraffitiArtist.PaintVisual");
            Check((string)paintMaterial.GetField("ShaderName").GetRawConstantValue()=="Standard (Specular setup)","installed lit transparency shader selected");
            Check(Calls(paintMaterial.GetMethod("Create")).Any(m=>m.Name=="EnableKeyword"),"material configures transparent variant");
            Check(Calls(paintMaterial.GetMethod("Create")).Any(m=>m.Name=="set_receiveShadows"),"material path enables shadow reception");
            Check(Calls(visual.GetConstructors(flags).Single()).Any(m=>m.Name=="Create"&&m.DeclaringType==paintMaterial),"visual creates the lit paint material");
            var visualBuild=Calls(visual.GetMethod("Build",flags));
            Check(visualBuild.Any(m=>m.Name=="SetNormals"),"visual supplies sampled hull shading normals");
            Check(!visualBuild.Any(m=>m.Name=="RecalculateNormals"),"visual does not replace smooth hull normals with cell normals");
            Check(visualBuild.Any(m=>m.Name=="TintedRegion"),"lit text texture contains pigment rather than relying on shader tint");
            Check(visualBuild.Any(m=>m.Name=="RequestCharactersInTexture"),"text glyphs explicitly requested before capture");
            Check(visualBuild.Any(m=>m.DeclaringType.Name=="WindowsTextRasterizer"&&m.Name=="Render"),"visual routes non-dynamic user fonts to Windows coverage renderer");
            Check(Calls(plugin.GetType("KSPGraffitiArtist.FontCoverageAtlas").GetMethod("TintedRegion")).Any(m=>m.Name=="TintCoverage"),"texture tint path uses checked coverage conversion");
            var dismiss=Calls(plugin.GetType("KSPGraffitiArtist.PawIntegration").GetMethod("DismissForPaint"));
            Check(dismiss.Any(m=>m.Name=="Deselect"&&m.DeclaringType.Name=="UIPartActionController"),"candidate invokes native PAW dismissal");
            Check(!dismiss.Any(m=>m.Name.StartsWith("Destroy")),"PAW adapter never destroys window objects");
            var late=Calls(tool.GetMethod("LateUpdate",flags));
            Check(late.Any(m=>m.Name=="DismissForPaint"),"late update uses PAW adapter");
            Check(!late.Any(m=>m.Name.StartsWith("Destroy")),"late update never destroys windows");
            Check(late.Any(m=>m.Name=="Equipped")&&late.Any(m=>m.Name=="Close"),"late update rechecks EVA eligibility before UI interaction");
            foreach(string name in new[]{"OnBoard","OnVesselChange","OnDisable","OnDestroy"})
                Check(Calls(tool.GetMethod(name,flags)).Any(m=>m.Name=="Close"),name+" closes editor");
            Check(Calls(tool.GetMethod("Close",flags)).Any(m=>m.Name=="RemoveControlLock"),"editor cleanup releases named input lock");
            Check(!Calls(tool.GetMethod("CanReach",flags)).Any(m=>m.Name.StartsWith("Raycast")),"reach check no longer requires Kerbal line of sight");
            Check(Calls(tool.GetMethod("Open",flags)).Any(m=>m.DeclaringType.Name=="PaintPicking"&&m.Name=="Hits"),"editor uses local-space picking query");
            Check(plugin.GetReferencedAssemblies().Any(a=>a.Name=="KIS"),"KIS dependency linked");
            Check(!plugin.GetReferencedAssemblies().Any(a=>a.Name=="ConformalDecals"),"no Conformal Decals dependency");
            Type worldScenario=plugin.GetType("KSPGraffitiArtist.WorldPaintScenario");
            Check(worldScenario.BaseType==gameAssembly.GetType("ScenarioModule"),"world paint uses save-local scenario");
            Check(worldScenario.GetCustomAttributes(false).Any(a=>a.GetType().Name=="KSPScenario"),"world scenario auto-registration attribute exists");
            Check(Calls(worldScenario.GetMethod("OnLoad")).Any(m=>m.DeclaringType.Name=="WorldPaintStore"&&m.Name=="Load"),"scenario loads through tested production world store");
            Check(Calls(worldScenario.GetMethod("OnSave")).Any(m=>m.DeclaringType.Name=="WorldPaintStore"&&m.Name=="Save"),"scenario saves through tested production world store");
            Check(!plugin.GetReferencedAssemblies().Any(a=>a.Name=="KerbalKonstructs"),"KK support remains optional");
            Check(gameAssembly.GetType("PQ")!=null&&gameAssembly.GetType("PQS")!=null,"PQS quad identity APIs present");
            Type terrainQuad=gameAssembly.GetType("PQ");
            Check(terrainQuad.GetField("sphereRoot",flags)!=null&&terrainQuad.GetField("meshCollider",flags)!=null&&terrainQuad.GetField("meshFilter",flags)!=null,"installed quad provides explicit owner and geometry links");
            Check(gameAssembly.GetType("PQS").GetField("LocalSpacePQList",flags)!=null,"installed PQS exposes local-space quad field for read-only adapter");
            string kkPath=Path.Combine(game,"GameData/KerbalKonstructs/KerbalKonstructs.dll");
            if(File.Exists(kkPath)){
                Assembly kkAssembly=Assembly.LoadFrom(kkPath);
                Type database=kkAssembly.GetType("KerbalKonstructs.Core.StaticDatabase"),instance=kkAssembly.GetType("KerbalKonstructs.Core.StaticInstance");
                Check(database!=null&&database.GetMethod("GetAllStatics",BindingFlags.Public|BindingFlags.Static)!=null,"installed KK enumeration adapter contract");
                Check(instance!=null&&instance.GetField("UUID",flags)!=null&&instance.GetField("gameObject",flags)!=null&&instance.GetField("CelestialBody",flags)!=null,"installed KK identity/root/body adapter contract");
            }
            Type nodeType=gameAssembly.GetType("ConfigNode");
            MethodInfo load=nodeType.GetMethod("Load",new[]{typeof(string)});
            MethodInfo nodes=nodeType.GetMethod("GetNodes",new[]{typeof(string)});
            MethodInfo value=nodeType.GetMethod("GetValue",new[]{typeof(string)});
            var partConfig=load.Invoke(null,new object[]{Path.Combine(args[2],"GameData/KSPGraffitiArtist/Parts/SprayCan/part.cfg")});
            Array parts=(Array)nodes.Invoke(partConfig,new object[]{"PART"});
            Check(parts.Length==1,"stock parser reads tool config");
            object part=parts.GetValue(0);
            Check((string)value.Invoke(part,new object[]{"name"})=="gaSprayCan","inventory identifier matches config");
            Array modules=(Array)nodes.Invoke(part,new object[]{"MODULE"});
            Check(modules.Length==1,"one KIS module");
            Check((string)value.Invoke(modules.GetValue(0),new object[]{"name"})=="ModuleKISItem","configured KIS module exists");
            Check((string)value.Invoke(modules.GetValue(0),new object[]{"stackable"})=="true","spray cans permit KIS inventory stacking");
            Console.WriteLine("PASS: "+count+" installed-assembly/config API checks (no Unity scene execution).");return 0;
        }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
