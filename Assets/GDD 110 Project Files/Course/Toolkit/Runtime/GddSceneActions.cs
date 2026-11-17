using System.Collections;
using UnityEngine;
namespace GDD110
{
    [AddComponentMenu("GDD 110/Functions/Scene Actions")]
    public class GddSceneActions : GddEntity
    {
        [Tooltip("Seconds to wait before changing scene, so a death or win moment can play first. Uses real time, so slow motion does not stretch it.")]
        [Min(0)] public float delay;
        bool loading;

        public override void Activate(){RaiseActivated();if(this!=null)NextScene();}
        public void NextScene()=>Run(GddScenes.Next);
        public void RestartScene()=>Run(GddScenes.Restart);
        public void FirstScene()=>Run(GddScenes.First);
        public void ReleaseCursor(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}

        void Run(System.Action load)
        {
            if(loading)return;
            if(delay<=0||!isActiveAndEnabled){load();return;}
            loading=true;StartCoroutine(Later(load));
        }
        IEnumerator Later(System.Action load){yield return new WaitForSecondsRealtime(delay);loading=false;load();}
    }
}
