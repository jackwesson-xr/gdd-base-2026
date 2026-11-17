using System.Collections.Generic;
using UnityEngine;
namespace GDD110
{
    [AddComponentMenu("GDD 110/Functions/Spawner")]
    public class GddSpawn : GddEntity
    {
        public GameObject prefab;
        public Transform spawnPoint;
        public bool automatic=true;
        [Min(0.1f)] public float interval=3;
        [Min(1)] public int maximumAlive=5;
        [Min(0.1f)] public float lifetime=12;
        readonly List<GameObject> alive=new List<GameObject>();
        float nextSpawn;
        void Update(){if(automatic&&Time.time>=nextSpawn){Spawn();nextSpawn=Time.time+Mathf.Max(0.1f,interval);}}
        public void Spawn()
        {
            alive.RemoveAll(g=>g==null);if(prefab==null||alive.Count>=Mathf.Max(1,maximumAlive))return;
            var point=spawnPoint!=null?spawnPoint:transform;var obj=Instantiate(prefab,point.position+Vector3.up,point.rotation);alive.Add(obj);Destroy(obj,Mathf.Max(0.1f,lifetime));RaiseActivated();
        }
        public override void Activate()=>Spawn();
        public void ClearSpawned(){foreach(var obj in alive)if(obj!=null)Destroy(obj);alive.Clear();}
    }
}
