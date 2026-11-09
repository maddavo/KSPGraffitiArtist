using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace KSPGraffitiArtist
{
    // Optional read-only adapter. Never changes KK configuration or creates instance UUIDs.
    public static class KkSurfaces
    {
        public sealed class Item {public string Id;public CelestialBody Body;public Transform Root;}
        static float refreshAt;
        static readonly List<Item> items=new List<Item>();
        public static IList<Item> Items()
        {
            if(Time.unscaledTime<refreshAt)return items;
            refreshAt=Time.unscaledTime+1;items.Clear();
            foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()){
                Type database=assembly.GetType("KerbalKonstructs.Core.StaticDatabase");
                if(database==null)continue;
                var method=database.GetMethod("GetAllStatics",BindingFlags.Public|BindingFlags.Static);
                if(method==null)break;
                try {
                    foreach(object instance in (IEnumerable)method.Invoke(null,null)){
                        if(instance==null)continue;Type type=instance.GetType();
                        var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
                        FieldInfo id=type.GetField("UUID",flags),body=type.GetField("CelestialBody",flags),root=type.GetField("gameObject",flags);
                        if(id==null || body==null || root==null)continue;
                        GameObject gameObject=root.GetValue(instance) as GameObject;
                        string uuid=id.GetValue(instance) as string;Guid parsed;
                        if(gameObject!=null && Guid.TryParse(uuid,out parsed))items.Add(new Item{Id=uuid,Body=body.GetValue(instance) as CelestialBody,Root=gameObject.transform});
                    }
                }catch(Exception ex){Debug.LogWarning("[GraffitiArtist] KK surfaces unavailable: "+ex.Message);}
                break;
            }
            return items;
        }
        public static Item At(Collider collider)
        {
            foreach(Item item in Items())if(item.Root!=null && collider.transform.IsChildOf(item.Root))return item;
            return null;
        }
    }
}
