using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KSPGraffitiArtist
{
    public sealed class ModuleGraffitiSurface : PartModule
    {
        readonly MarkingCollection records = new MarkingCollection();
        readonly List<ConfigNode> retained = new List<ConfigNode>();
        readonly List<PaintVisual> visuals = new List<PaintVisual>();
        bool dirty=true;
        string suppressedId;
        public IList<MarkingData> Records { get { return records.Snapshot(); } }
        public void Suppress(string id) { suppressedId=id;dirty=true; }
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node); records.Clear(); retained.Clear();
            foreach(ConfigNode child in node.GetNodes("MARKING")) {
                var values=new Dictionary<string,string>();
                foreach(ConfigNode.Value value in child.values) values[value.name]=value.value;
                MarkingData data;
                if(!MarkingData.TryDecode(values,out data) || !records.Restore(data)) {
                    retained.Add(child.CreateCopy()); Debug.LogWarning("[GraffitiArtist] Retaining unrecognized marking data on "+part.name);
                }
            }
            dirty=true;
        }
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node); node.RemoveNodes("MARKING");
            foreach(MarkingData data in Records) {
                ConfigNode child=node.AddNode("MARKING");
                foreach(var entry in data.Encode()) child.AddValue(entry.Key,entry.Value);
            }
            foreach(ConfigNode child in retained) node.AddNode(child.CreateCopy());
        }
        public override void OnStart(StartState state)
        { base.OnStart(state); Font.textureRebuilt+=OnFontChanged; dirty=true; }
        void OnFontChanged(Font font) { dirty=true; }
        public bool Commit(MarkingData draft)
        {
            if(!records.Commit(draft))return false;
            dirty=true; return true;
        }
        public void Erase(string id) { records.Erase(id); dirty=true; }
        public void Invalidate() { dirty=true; }
        void Update()
        {
            if(!HighLogic.LoadedSceneIsFlight || part==null || part.vessel==null || part.vessel.packed) return;
            if(!dirty) return;
            dirty=false; Clear();
            foreach(MarkingData record in Records) {
                if(record.Id==suppressedId)continue;
                try { visuals.Add(PaintVisual.Create(part,record)); }
                catch(Exception ex) { Debug.LogError("[GraffitiArtist] Marking rendering failed: "+ex); }
            }
        }
        void Clear() { foreach(PaintVisual v in visuals) v.Dispose(); visuals.Clear(); }
        void OnDestroy() { Font.textureRebuilt-=OnFontChanged; Clear(); }
    }
}
