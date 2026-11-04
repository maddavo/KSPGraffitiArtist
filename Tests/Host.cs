// Offline host only. These small stand-ins exercise production persistence code without Unity.
// They do not simulate shaders, KIS, stock loading, physics, or input.
using System;
using System.Collections.Generic;
public sealed class ConfigNode
{
    public sealed class Value { public string name,value; }
    public string name;
    public List<Value> values=new List<Value>();
    readonly List<ConfigNode> nodes=new List<ConfigNode>();
    public ConfigNode(string name="MODULE"){this.name=name;}
    public ConfigNode AddNode(string name){var node=new ConfigNode(name);nodes.Add(node);return node;}
    public void AddNode(ConfigNode node){nodes.Add(node);}
    public ConfigNode[] GetNodes(string name){return nodes.FindAll(n=>n.name==name).ToArray();}
    public void RemoveNodes(string name){nodes.RemoveAll(n=>n.name==name);}
    public void AddValue(string name,string value){values.Add(new Value{name=name,value=value});}
    public ConfigNode CreateCopy(){var node=new ConfigNode(name);foreach(Value v in values)node.AddValue(v.name,v.value);foreach(ConfigNode c in nodes)node.AddNode(c.CreateCopy());return node;}
}
public class PartModule
{
    public enum StartState { Flying }
    public Part part=new Part();
    public virtual void OnLoad(ConfigNode node){}
    public virtual void OnSave(ConfigNode node){}
    public virtual void OnStart(StartState state){}
}
public sealed class Part { public string name="testHull";public Vessel vessel=new Vessel(); }
public sealed class Vessel { public bool packed; }
public static class HighLogic { public static bool LoadedSceneIsFlight; }
namespace UnityEngine
{
    public sealed class Font { public static event Action<Font> textureRebuilt {add{} remove{}} }
    public static class Debug { public static void LogWarning(object value){}public static void LogError(object value){} }
}
namespace KSPGraffitiArtist
{
    public sealed class PaintVisual : IDisposable { public static PaintVisual Create(Part p,MarkingData m){return new PaintVisual();}public void Dispose(){} }
}
