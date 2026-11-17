using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

namespace GDD110
{
    [DisallowMultipleComponent, RequireComponent(typeof(GddActor)), AddComponentMenu("GDD 110/Player/Interact and Use")]
    public class GddInteractor : GddEntity
    {
        [Min(0.25f)] public float reach = 2.5f;
        [Tooltip("Ignore walls only when deliberately teaching through-wall interactions.")]
        public bool requireLineOfSight = true;
        public LayerMask interactionLayers = ~0;
        public TMP_Text promptText;
        public GddEntity Candidate { get; private set; }
        public GddPickup Equipped { get; private set; }
        readonly Collider[] nearby = new Collider[64];
        GddActor actor;
        Camera view;
        Canvas ownedCanvas;
        void Awake()
        {
            actor=GetComponent<GddActor>();view=Camera.main;
            if(promptText!=null)
            {
                var canvas=promptText.GetComponentInParent<Canvas>();
                if(canvas!=null&&canvas.renderMode==RenderMode.ScreenSpaceOverlay&&canvas.transform.IsChildOf(transform))
                {
                    ownedCanvas=canvas;canvas.transform.SetParent(null,false);
                }
            }
        }
        void OnEnable(){if(ownedCanvas!=null)ownedCanvas.enabled=true;}
        void OnDisable(){if(ownedCanvas!=null)ownedCanvas.enabled=false;}
        void OnDestroy(){if(ownedCanvas!=null)Destroy(ownedCanvas.gameObject);}
        void Update()
        {
            if (view == null) view=Camera.main;
            Candidate=FindCandidate();
            if (GddControls.Interact) InteractNearest();
            if (GddControls.Drop) DropHeld();
            if (GddControls.Use && !GddGameRuntime.WindowOpen &&
                (Cursor.lockState == CursorLockMode.Locked || EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())) UseHeld();
            if (promptText != null)
            {
                string message=Candidate!=null ? "["+GddControls.InteractHint+"] "+Candidate.Prompt+" "+Candidate.DisplayName : "Move: WASD / left stick   Look: mouse / right stick   Jump: Space / A";
                if (Equipped!=null) message+="\n"+Equipped.DisplayName+"   Use: "+GddControls.UseHint+"   Drop: "+GddControls.DropHint;
                else if (GddCollectible.AnyWorn(actor)) message+="\nTake off: "+GddControls.DropHint;
                else message+="\nClick the game to focus it. Press a controller button to connect.";
                if (promptText.text!=message) promptText.text=message;
            }
        }
        GddEntity FindCandidate()
        {
            Vector3 origin=transform.position+Vector3.up;
            int count=Physics.OverlapSphereNonAlloc(origin,reach,nearby,interactionLayers,QueryTriggerInteraction.Collide);
            GddEntity best=null;float bestDistance=float.PositiveInfinity;
            for(int i=0;i<count;i++)
            {
                foreach(var e in nearby[i].GetComponentsInParent<GddEntity>())
                {
                    if(!e.isActiveAndEnabled || !e.CanInteract(actor)) continue;
                    Vector3 target=nearby[i].bounds.center;float d=(target-origin).sqrMagnitude;
                    if(d>=bestDistance)continue;
                    Vector3 delta=target-origin;
                    if(requireLineOfSight && Physics.Raycast(origin,delta.normalized,out var hit,delta.magnitude,interactionLayers,QueryTriggerInteraction.Ignore)
                        && hit.collider.GetComponentInParent<GddEntity>()!=e && !hit.transform.IsChildOf(e.transform)) continue;
                    best=e;bestDistance=d;
                }
            }
            return best;
        }
        public void InteractNearest() { if(Candidate!=null)Candidate.Interact(actor); }
        public void UseHeld()
        {
            if (Equipped) Equipped.UsePowers();
            else foreach (var slash in actor.GetComponentsInChildren<GddSlashAttack>()) slash.Use(actor);
        }
        public void DropHeld() { if(Equipped!=null)Equipped.Drop(); else GddCollectible.TakeOffLatest(actor); }
        public void Equip(GddPickup item)
        {
            foreach(var other in actor.Rig.GetComponentsInChildren<GddPickup>(true))
                if(other!=item && other.Holder==actor && other.attachment==item.attachment)other.Drop();
            Equipped=item;
        }
        public void Forget(GddPickup item) { if(Equipped==item)Equipped=null; }
    }
}
