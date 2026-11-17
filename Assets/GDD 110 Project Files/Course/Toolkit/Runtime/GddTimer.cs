using UnityEngine;
namespace GDD110
{
    [AddComponentMenu("GDD 110/Functions/Timer")]
    public class GddTimer : GddEntity
    {
        [Min(0.05f)] public float seconds=2;
        public bool runOnStart=true;
        public bool repeat=true;
        float remaining;
        bool running;
        void OnEnable(){remaining=Mathf.Max(0.05f,seconds);running=runOnStart;}
        void Update(){if(!running)return;remaining-=Time.deltaTime;if(remaining>0)return;if(repeat)remaining=Mathf.Max(0.05f,seconds);else running=false;RaiseActivated();}
        public void StartTimer(){remaining=Mathf.Max(0.05f,seconds);running=true;}
        public void StopTimer(){running=false;}
    }
}
