using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace GDD110
{
    [RequireComponent(typeof(BoxCollider),typeof(Rigidbody)), AddComponentMenu("GDD 110/Functions/Trigger Zone")]
    public class GddTrigger : GddEntity
    {
        public bool once;
        public UnityEvent onExited=new UnityEvent();
        public UnityEvent<GameObject> onActorExited=new UnityEvent<GameObject>();
        readonly Dictionary<GddActor,HashSet<Collider>> occupants=new Dictionary<GddActor,HashSet<Collider>>();
        bool fired;
        void Reset(){GetComponent<BoxCollider>().isTrigger=true;var rb=GetComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;}
        void OnTriggerEnter(Collider other)
        {
            var actor=other.GetComponentInParent<GddActor>();if(actor==null||(once&&fired))return;
            if(!occupants.TryGetValue(actor,out var colliders)){colliders=new HashSet<Collider>();occupants.Add(actor,colliders);}
            if(!colliders.Add(other)||colliders.Count!=1)return;
            LastActor=actor;fired=true;RaiseActivated();if(this!=null&&actor!=null)OnActorEntered(actor);
        }
        void OnTriggerExit(Collider other)
        {
            var actor=other.GetComponentInParent<GddActor>();if(actor==null||!occupants.TryGetValue(actor,out var colliders))return;
            colliders.Remove(other);if(colliders.Count!=0)return;occupants.Remove(actor);onExited.Invoke();onActorExited.Invoke(actor.gameObject);
        }
        protected virtual void OnActorEntered(GddActor actor){}
        public void ResetTrigger(){fired=false;occupants.Clear();}
        void OnDisable(){occupants.Clear();}
        void OnDrawGizmosSelected(){var box=GetComponent<BoxCollider>();if(box==null)return;Gizmos.color=new Color(0.1f,0.8f,1,0.3f);Gizmos.matrix=transform.localToWorldMatrix;Gizmos.DrawWireCube(box.center,box.size);}
    }
}
