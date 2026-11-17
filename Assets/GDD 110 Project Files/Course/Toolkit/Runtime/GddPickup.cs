using UnityEngine;
using UnityEngine.Events;

namespace GDD110
{
    [DisallowMultipleComponent, AddComponentMenu("GDD 110/Functions/Pickup")]
    public class GddPickup : GddEntity
    {
        public enum PickupMode { Carry, Collect }
        public PickupMode mode;
        public GddSocket attachment=GddSocket.RightHand;
        [Tooltip("Collect by touching the item instead of pressing Interact. Best for coins and health.")]
        public bool collectOnTouch;
        public bool dropWithPhysics = true;
        [Tooltip("Foot items: put a mirrored copy on the other foot too, so boots come as a pair. Set the right foot; the left one is worked out.")]
        public bool mirrorToOtherFoot = true;
        public Vector3 attachmentPosition;
        public Vector3 attachmentRotation;
        public bool playGunDraw;
        public UnityEvent onEquipped=new UnityEvent();
        public UnityEvent onDropped=new UnityEvent();
        public UnityEvent onCollected=new UnityEvent();
        public GddActor Holder { get; private set; }
        Rigidbody body;
        Collider[] colliders;
        bool[] colliderEnabled;
        bool previousKinematic,previousGravity;
        Vector3 worldScale;
        bool collecting;
        float canPickUpAt;
        public override string Prompt => mode==PickupMode.Carry ? "Pick up" : "Collect";
        public override bool CanInteract(GddActor actor) => Holder==null&&!collecting&&Time.time>=canPickUpAt;
        void Awake() { CachePhysics(); }
        void CachePhysics()
        {
            if(colliders!=null)return;
            body=GetComponent<Rigidbody>();colliders=GetComponentsInChildren<Collider>(true);
            colliderEnabled=new bool[colliders.Length];
            for(int i=0;i<colliders.Length;i++)colliderEnabled[i]=colliders[i].enabled;
        }
        public override void Interact(GddActor actor)
        {
            if(actor==null||!CanInteract(actor))return;
            LastActor=actor;
            if(mode==PickupMode.Collect)
            {
                collecting=true;
                foreach(var power in GetComponentsInChildren<GddPower>(true))power.Use(actor);
                RaiseActivated();onCollected.Invoke();gameObject.SetActive(false);return;
            }
            CachePhysics();Holder=actor;worldScale=transform.lossyScale;
            if(body!=null){previousKinematic=body.isKinematic;previousGravity=body.useGravity;body.isKinematic=true;body.detectCollisions=false;}
            foreach(var c in colliders)if(c!=null)c.enabled=false;
            actor.Interactor?.Equip(this);
            transform.SetParent(actor.Rig.Socket(attachment),false);
            transform.localPosition=attachmentPosition;transform.localRotation=Quaternion.Euler(attachmentRotation);
            Vector3 parentScale=transform.parent.lossyScale;
            transform.localScale=new Vector3(worldScale.x/Mathf.Max(0.001f,Mathf.Abs(parentScale.x)),worldScale.y/Mathf.Max(0.001f,Mathf.Abs(parentScale.y)),worldScale.z/Mathf.Max(0.001f,Mathf.Abs(parentScale.z)));
            foreach(var power in GetComponentsInChildren<GddPower>(true))power.Equip(actor);
            if(mirrorToOtherFoot&&(attachment==GddSocket.RightFoot||attachment==GddSocket.LeftFoot))MakePair(actor);
            if (playGunDraw) GddCombatAnimation.Play(actor, "Draw Pulse");
            onEquipped.Invoke();RaiseActivated();
        }
        public void UsePowers()
        {
            if(Holder==null)return;
            var holder=Holder;
            foreach(var usable in GetComponentsInChildren<IGddUsable>(true))usable.Use(holder);
            if(Holder==null)return;   // a usable (like a throw) let go of it
            foreach(var power in GetComponentsInChildren<GddPower>(true))if(power.activation==GddPower.Activation.UseButton)power.Use(Holder);
        }

        GameObject pair;
        /// <summary>A look-alike on the other foot: same offset, mirrored across the body.</summary>
        void MakePair(GddActor actor)
        {
            var other=actor.Rig.Socket(attachment==GddSocket.RightFoot?GddSocket.LeftFoot:GddSocket.RightFoot);
            if(other==null)return;
            pair=new GameObject(name+" (pair)");
            foreach(var mf in GetComponentsInChildren<MeshFilter>())
            {
                var r=mf.GetComponent<MeshRenderer>();if(r==null||!r.enabled)continue;
                var part=new GameObject(mf.name);part.transform.SetParent(pair.transform,false);
                part.transform.localPosition=transform.InverseTransformPoint(mf.transform.position);
                part.transform.localRotation=Quaternion.Inverse(transform.rotation)*mf.transform.rotation;
                part.transform.localScale=new Vector3(mf.transform.lossyScale.x/transform.lossyScale.x,mf.transform.lossyScale.y/transform.lossyScale.y,mf.transform.lossyScale.z/transform.lossyScale.z);
                part.AddComponent<MeshFilter>().sharedMesh=mf.sharedMesh;part.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;
            }
            // Mirror this item's offset from its foot onto the other foot. The two feet's axes are mirror images of
            // each other on a humanoid; work out which axis flips (from the current pose, snapped to the nearest
            // clean mapping) and apply the same offset, mirrored, on the other side.
            var mine=transform.parent;var root=actor.transform;
            Matrix4x4 a=Matrix4x4.Rotate(root.rotation);
            Matrix4x4 flipX=a*Matrix4x4.Scale(new Vector3(-1,1,1))*a.inverse;
            Matrix4x4 m=Matrix4x4.Rotate(other.rotation).inverse*flipX*Matrix4x4.Rotate(mine.rotation);
            Matrix4x4 snap=Matrix4x4.zero;snap[3,3]=1;
            for(int c=0;c<3;c++){int best=0;for(int r=1;r<3;r++)if(Mathf.Abs(m[r,c])>Mathf.Abs(m[best,c]))best=r;snap[best,c]=Mathf.Sign(m[best,c]);}
            pair.transform.SetParent(other,false);
            pair.transform.localPosition=snap.MultiplyVector(transform.localPosition);
            Matrix4x4 rot=snap*Matrix4x4.Rotate(transform.localRotation)*Matrix4x4.Scale(new Vector3(-1,1,1));
            pair.transform.localRotation=Quaternion.LookRotation(rot.GetColumn(2),rot.GetColumn(1));
            var ls=transform.localScale;pair.transform.localScale=new Vector3(-ls.x,ls.y,ls.z);
        }
        /// <summary>Both boots, for effects that should play on each foot.</summary>
        public Transform Pair=>pair!=null?pair.transform:null;
        public void Drop()
        {
            if(Holder==null)return;
            var previousHolder=Holder;
            foreach(var power in GetComponentsInChildren<GddPower>(true))power.Unequip(previousHolder);
            transform.SetParent(null,true);transform.localScale=worldScale;
            transform.position=previousHolder.transform.position+previousHolder.transform.forward*1.2f+Vector3.up*0.8f;
            Holder=null;canPickUpAt=Time.time+0.35f;
            if(pair!=null){Destroy(pair);pair=null;}
            for(int i=0;i<colliders.Length;i++)if(colliders[i]!=null)colliders[i].enabled=colliderEnabled[i];
            if(body!=null){body.detectCollisions=true;body.isKinematic=dropWithPhysics?false:previousKinematic;body.useGravity=dropWithPhysics||previousGravity;}
            previousHolder.Interactor?.Forget(this);onDropped.Invoke();
        }
        void OnDisable() { if(Holder!=null)Drop(); }
        void OnTriggerEnter(Collider other) { if(collectOnTouch&&mode==PickupMode.Collect){var actor=other.GetComponentInParent<GddActor>();if(actor!=null)Interact(actor);} }
    }
}
