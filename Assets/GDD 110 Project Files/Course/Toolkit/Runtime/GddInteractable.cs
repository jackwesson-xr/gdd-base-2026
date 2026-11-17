using UnityEngine;
namespace GDD110
{
    [AddComponentMenu("GDD 110/Functions/Interact Button")]
    public class GddInteractable : GddEntity
    {
        public string actionLabel="Use";
        public bool once;
        bool used;
        public override string Prompt=>actionLabel;
        public override bool CanInteract(GddActor actor)=>!once||!used;
        public override void Interact(GddActor actor){if(!CanInteract(actor))return;LastActor=actor;used=true;RaiseActivated();}
        public void ResetInteraction(){used=false;}
    }
}
