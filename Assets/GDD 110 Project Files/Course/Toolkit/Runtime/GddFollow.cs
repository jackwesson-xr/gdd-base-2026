using UnityEngine;
namespace GDD110
{
    [RequireComponent(typeof(Rigidbody)), AddComponentMenu("GDD 110/Functions/Follow Player or Enemy")]
    public class GddFollow : GddEntity
    {
        public Transform target;
        [Min(0)] public float speed=2;
        [Min(0)] public float stopDistance=1.5f;
        [Tooltip("Zero makes a friendly companion. A positive value damages an actor on contact.")]
        [Min(0)] public float contactDamage;
        Rigidbody body;
        float nextHit;
        void Start(){body=GetComponent<Rigidbody>();if(target==null){var actor=FindAnyObjectByType<GddActor>();if(actor!=null)target=actor.transform;}}
        void FixedUpdate()
        {
            if(target==null||body==null)return;
            Vector3 delta=target.position-body.position;delta.y=0;
            if(delta.magnitude>stopDistance)body.MovePosition(body.position+delta.normalized*speed*Time.fixedDeltaTime);
        }
        void OnCollisionStay(Collision collision)
        {
            if(contactDamage<=0||Time.time<nextHit)return;
            var actor=collision.collider.GetComponentInParent<GddActor>();if(actor==null)return;
            if(GddArmor.Block(actor,gameObject)){nextHit=Time.time+0.5f;return;}   // armor: bounce off instead
            actor.GetComponent<GddHealth>()?.Damage(contactDamage);nextHit=Time.time+1;LastActor=actor;RaiseActivated();
        }
    }
}
