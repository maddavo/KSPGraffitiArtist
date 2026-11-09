using System.Collections.Generic;
using UnityEngine;

namespace KSPGraffitiArtist
{
    public sealed class PaintTarget
    {
        public readonly Part Part;
        public readonly Transform transform;
        public readonly WorldSurfaceData World;
        public readonly CelestialBody Body;
        readonly ModuleGraffitiSurface module;
        readonly List<PaintVisual> visuals=new List<PaintVisual>();
        string suppressed;
        bool dirty=true;
        int terrainRevision=int.MinValue;
        public bool Editing {get;private set;}
        public bool Terrain {get{return World!=null && World.Kind=="terrain";}}
        public bool Active {get{return transform!=null && transform.gameObject.activeInHierarchy &&
            (Part==null || (Part.vessel!=null && !Part.vessel.packed));}}
        public PaintTarget(Part part)
        {Part=part;transform=part.transform;module=part.FindModuleImplementing<ModuleGraffitiSurface>();}
        public PaintTarget(WorldSurfaceData data,Transform frame,CelestialBody body)
        {World=data;transform=frame;Body=body;}
        public IList<MarkingData> Records {get{return module!=null?module.Records:World.Marks.Snapshot();}}
        public bool Commit(MarkingData data)
        {dirty=true;return module!=null?module.Commit(data):World.Marks.Commit(data);}
        public void Erase(string id)
        {dirty=true;if(module!=null)module.Erase(id);else World.Marks.Erase(id);}
        public void Suppress(string id)
        {
            if(module!=null){module.Suppress(id);return;}
            suppressed=id;Editing=id!=null;dirty=true;
            if(!Editing && WorldPaintScenario.Instance!=null)WorldPaintScenario.Instance.RemoveEmpty(this);
        }
        public void Invalidate(){dirty=true;}
        public void RefreshTerrain()
        {if(Terrain){int revision=VisibleSurface.TerrainRevision(this);if(revision!=terrainRevision){terrainRevision=revision;dirty=true;}}}
        public bool Owns(Collider collider)
        {
            if(collider==null)return false;
            if(Part!=null)return collider.GetComponentInParent<Part>()==Part;
            if(Terrain)return IsTerrain(collider,Body);
            return transform!=null && collider.transform.IsChildOf(transform);
        }
        public static bool IsTerrain(Collider collider,CelestialBody body)
        {
            return TerrainSurfaces.Find(collider,body)!=null;
        }
        public void FollowBody()
        {
            if(Terrain && transform!=null){
                transform.position=Body.GetWorldSurfacePosition(World.Latitude,World.Longitude,World.Altitude);
                transform.rotation=Body.transform.rotation;
            }
        }
        public void Render()
        {
            if(!dirty || Editing)return;dirty=false;ClearVisuals();
            foreach(MarkingData record in Records){
                if(record.Id==suppressed)continue;
                try{visuals.Add(PaintVisual.Create(this,record));}
                catch(System.Exception ex){Debug.LogWarning("[GraffitiArtist] World paint: "+ex.Message);}
            }
        }
        // Called while editing too, so other applied markings remain visible.
        public void RenderEdited()
        {
            if(!dirty)return;bool editing=Editing;Editing=false;Render();Editing=editing;
        }
        public void ClearVisuals(){foreach(PaintVisual visual in visuals)visual.Dispose();visuals.Clear();}
        public void Dispose()
        {ClearVisuals();if(Terrain && transform!=null)UnityEngine.Object.Destroy(transform.gameObject);}
    }
}
