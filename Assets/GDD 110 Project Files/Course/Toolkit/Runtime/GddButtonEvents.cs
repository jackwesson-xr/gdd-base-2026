using UnityEngine;
using UnityEngine.Events;
namespace GDD110
{
    [AddComponentMenu("GDD 110/Functions/Keyboard and Controller Events")]
    public class GddButtonEvents : GddEntity
    {
        public enum Button { Interact, Use, Drop, Restart }
        public Button button=Button.Interact;
        [Tooltip("Enable only while the player is in a trigger using UnityEvents if this should be local to an object.")]
        public bool listen=true;
        void Update(){if(!listen)return;bool pressed=button==Button.Interact?GddControls.Interact:button==Button.Use?GddControls.Use:button==Button.Drop?GddControls.Drop:GddControls.Restart;if(pressed)RaiseActivated();}
        public void StartListening()=>listen=true;
        public void StopListening()=>listen=false;
    }
}
