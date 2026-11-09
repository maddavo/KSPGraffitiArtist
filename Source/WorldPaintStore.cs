using System.Collections.Generic;
using System.Linq;

namespace KSPGraffitiArtist
{
    // The scenario's actual persistence path is exercised by the offline ConfigNode host.
    public sealed class WorldPaintStore
    {
        public readonly List<WorldSurfaceData> Records=new List<WorldSurfaceData>();
        readonly List<ConfigNode> retained=new List<ConfigNode>();
        public void Load(ConfigNode node)
        {
            Records.Clear();retained.Clear();
            foreach(ConfigNode child in node.GetNodes("WORLD_SURFACE")){
                WorldSurfaceData data;var values=new Dictionary<string,string>();
                foreach(ConfigNode.Value value in child.values)values[value.name]=value.value;
                bool valid=WorldSurfaceData.TryDecode(values,out data);
                if(valid)foreach(ConfigNode mark in child.GetNodes("MARKING")){
                    var encoded=new Dictionary<string,string>();foreach(ConfigNode.Value value in mark.values)encoded[value.name]=value.value;
                    MarkingData marking;if(!MarkingData.TryDecode(encoded,out marking) || !data.Marks.Restore(marking)){valid=false;break;}
                }
                // Unknown children and duplicate anchors are retained as complete nodes.
                if(!valid || Records.Count>=WorldSurfaceData.MaxSurfaces || Records.Any(r=>r.Body==data.Body && r.Kind==data.Kind && r.Id==data.Id))retained.Add(child.CreateCopy());
                else Records.Add(data);
            }
        }
        public void Save(ConfigNode node)
        {
            node.RemoveNodes("WORLD_SURFACE");
            foreach(WorldSurfaceData data in Records){
                if(data.Marks.Snapshot().Count==0)continue;
                ConfigNode child=node.AddNode("WORLD_SURFACE");foreach(var entry in data.Encode())child.AddValue(entry.Key,entry.Value);
                foreach(MarkingData marking in data.Marks.Snapshot()){
                    ConfigNode mark=child.AddNode("MARKING");foreach(var entry in marking.Encode())mark.AddValue(entry.Key,entry.Value);
                }
            }
            foreach(ConfigNode child in retained)node.AddNode(child.CreateCopy());
        }
    }
}
