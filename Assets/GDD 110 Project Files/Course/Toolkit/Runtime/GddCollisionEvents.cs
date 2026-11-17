using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace GDD110
{
    [AddComponentMenu("GDD 110/Functions/Collision Events")]
    public class GddCollisionEvents : GddEntity
    {
        public bool actorsOnly=true;
        public UnityEvent onCollisionExited=new UnityEvent();
        readonly Dictionary<Collider,float> contacts=new Dictionary<Collider,float>();
        readonly List<Collider> expired=new List<Collider>();
        public void NotifyActorContact(GddActor actor)
        {
            if(actor!=null)Touch(actor.GetComponent<Collider>(),actor);
        }
        void Touch(Collider collider,GddActor actor)
        {
            if(collider==null||(actorsOnly&&actor==null))return;
            bool first=!contacts.ContainsKey(collider);contacts[collider]=Time.time;
            if(first){LastActor=actor;RaiseActivated();}
        }
        void OnCollisionEnter(Collision collision)=>Touch(collision.collider,collision.collider.GetComponentInParent<GddActor>());
        void OnCollisionStay(Collision collision)=>Touch(collision.collider,collision.collider.GetComponentInParent<GddActor>());
        void OnCollisionExit(Collision collision){if(contacts.Remove(collision.collider))onCollisionExited.Invoke();}
        void OnControllerColliderHit(ControllerColliderHit hit){if(!actorsOnly)Touch(hit.collider,null);}
        void Update()
        {
            expired.Clear();foreach(var pair in contacts)if(pair.Key==null||Time.time-pair.Value>0.15f)expired.Add(pair.Key);
            foreach(var collider in expired){contacts.Remove(collider);onCollisionExited.Invoke();}
        }
        void OnDisable()=>contacts.Clear();
    }
}
