using System;
using System.IO;
using System.Linq;
using System.Reflection;
class InstalledApi
{
    static int count;
    static void Check(bool ok,string name){count++;if(!ok)throw new Exception(name);}
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
            Check(plugin.GetName().Version.ToString()=="0.1.1.0","assembly version");
            Type inventory=kis.GetType("KIS.ModuleKISInventory"),item=kis.GetType("KIS.KIS_Item");
            Check(inventory.GetField("items")!=null,"KIS inventory items");
            Check(item.GetProperty("equipped")!=null,"KIS equipped property");
            Check(item.GetField("availablePart")!=null,"KIS part identification");
            Check(gameAssembly.GetType("UIPartActionController").GetMethod("ItemListGet")!=null,"stock menu lookup API");
            Check(plugin.GetReferencedAssemblies().Any(a=>a.Name=="KIS"),"KIS dependency linked");
            Check(!plugin.GetReferencedAssemblies().Any(a=>a.Name=="ConformalDecals"),"no Conformal Decals dependency");
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
            Console.WriteLine("PASS: "+count+" installed-assembly/config API checks (no Unity scene execution).");return 0;
        }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
