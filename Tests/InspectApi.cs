using System;
using System.IO;
using System.Reflection;
class InspectApi
{
    static int Main(string[] args)
    {
        string root=args[0];
        AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{
            string name=new AssemblyName(e.Name).Name+".dll";
            foreach(string dir in new[]{"KSP_x64_Data/Managed","GameData/KIS/Plugins"}){
                string path=Path.Combine(root,dir,name);if(File.Exists(path))return Assembly.LoadFrom(path);
            }return null;
        };
        var a=Assembly.LoadFrom(Path.Combine(root,"KSP_x64_Data/Managed/Assembly-CSharp.dll"));
        foreach(string name in new[]{"UIPartActionController","UIPartActionWindow","ControlTypes"}) {
            Type type=a.GetType(name);Console.WriteLine("TYPE "+type);
            foreach(MemberInfo member in type.GetMembers(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static|BindingFlags.DeclaredOnly))Console.WriteLine(member);
        }
        return 0;
    }
}
