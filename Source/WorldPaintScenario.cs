using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KSPGraffitiArtist
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames,GameScenes.FLIGHT)]
    public sealed class WorldPaintScenario : ScenarioModule
    {
        public static WorldPaintScenario Instance {get;private set;}
        readonly WorldPaintStore store=new WorldPaintStore();
        List<WorldSurfaceData> records {get{return store.Records;}}
        readonly Dictionary<WorldSurfaceData,PaintTarget> targets=new Dictionary<WorldSurfaceData,PaintTarget>();
        float refreshAt;
        public override void OnAwake(){base.OnAwake();Instance=this;Font.textureRebuilt+=FontChanged;}
        void FontChanged(Font font){InvalidateAll();}
        public void InvalidateAll(){foreach(PaintTarget target in targets.Values)target.Invalidate();}
        public override void OnLoad(ConfigNode node)
        {base.OnLoad(node);Clear();store.Load(node);}
        public override void OnSave(ConfigNode node)
        {base.OnSave(node);store.Save(node);}
        public PaintTarget Acquire(RaycastHit hit,CelestialBody body)
        {
            KkSurfaces.Item instance=KkSurfaces.At(hit.collider);
            WorldSurfaceData data=null;
            if(instance!=null){
                if(instance.Body!=body)return null;
                data=records.FirstOrDefault(r=>r.Kind=="kk" && r.Body==body.bodyName && r.Id==instance.Id);
                if(data==null)data=new WorldSurfaceData{Kind="kk",Body=body.bodyName,Id=instance.Id};
            }else{
                if(!PaintTarget.IsTerrain(hit.collider,body))return null;
                data=records.Where(r=>r.Kind=="terrain" && r.Body==body.bodyName)
                    .OrderBy(r=>(body.GetWorldSurfacePosition(r.Latitude,r.Longitude,r.Altitude)-(Vector3d)hit.point).sqrMagnitude)
                    .FirstOrDefault(r=>(body.GetWorldSurfacePosition(r.Latitude,r.Longitude,r.Altitude)-(Vector3d)hit.point).sqrMagnitude<16);
                if(data==null)data=new WorldSurfaceData{Body=body.bodyName,Latitude=body.GetLatitude(hit.point),Longitude=body.GetLongitude(hit.point),Altitude=body.GetAltitude(hit.point)};
            }
            if(!records.Contains(data)){
                if(!data.Valid() || records.Count>=WorldSurfaceData.MaxSurfaces)return null;
                records.Add(data);
            }
            return Resolve(data,body);
        }
        PaintTarget Resolve(WorldSurfaceData data,CelestialBody body)
        {
            PaintTarget target;
            if(targets.TryGetValue(data,out target)){
                if(target.Active)return target;
                target.Dispose();targets.Remove(data);
            }
            if(data.Kind=="kk"){
                KkSurfaces.Item item=KkSurfaces.Items().FirstOrDefault(i=>i.Body==body && i.Id==data.Id && i.Root!=null && i.Root.gameObject.activeInHierarchy);
                if(item==null)return null;target=new PaintTarget(data,item.Root,body);
            }else{
                var root=new GameObject("GraffitiArtistTerrainAnchor");root.layer=15;
                target=new PaintTarget(data,root.transform,body);target.FollowBody();
            }
            targets[data]=target;return target;
        }
        public void RemoveEmpty(PaintTarget target)
        {
            if(target.World.Marks.Snapshot().Count!=0)return;
            target.Dispose();targets.Remove(target.World);records.Remove(target.World);
        }
        void LateUpdate()
        {
            foreach(PaintTarget target in targets.Values)target.FollowBody();
            if(Time.unscaledTime<refreshAt)return;refreshAt=Time.unscaledTime+1;
            Vessel vessel=FlightGlobals.ActiveVessel;CelestialBody body=FlightGlobals.currentMainBody;
            if(vessel==null || body==null)return;
            foreach(var pair in targets.ToArray())if(pair.Value.Body!=body){pair.Value.Dispose();targets.Remove(pair.Key);}
            foreach(WorldSurfaceData data in records){
                if(data.Body!=body.bodyName)continue;
                PaintTarget target=Resolve(data,body);if(target==null)continue;
                if(!target.Editing && Vector3.Distance(target.transform.position,vessel.transform.position)>250){target.Dispose();targets.Remove(data);continue;}
                // Terrain chunks are rebuilt as PQS LOD changes; refresh projection nearby.
                target.RefreshTerrain();target.RenderEdited();
            }
        }
        void Clear(){foreach(PaintTarget target in targets.Values)target.Dispose();targets.Clear();}
        void OnDestroy(){Font.textureRebuilt-=FontChanged;Clear();if(Instance==this)Instance=null;}
    }
}
