using UnityEngine;
namespace GDD110
{
    [RequireComponent(typeof(Rigidbody)), AddComponentMenu("GDD 110/Functions/Projectile")]
    public class GddProjectile : GddEntity
    {
        GddActor owner;
        float damage=10;
        public void Launch(GddActor actor,Vector3 direction,float speed,float amount,float lifetime)
        {
            owner=actor;damage=amount;var body=GetComponent<Rigidbody>();body.useGravity=false;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.linearVelocity=direction.normalized*speed;
            foreach(var shotCollider in GetComponentsInChildren<Collider>())foreach(var ownCollider in actor.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(shotCollider,ownCollider);
            Destroy(gameObject,Mathf.Max(0.1f,lifetime));
        }
        void OnCollisionEnter(Collision collision)
        {
            if(owner!=null&&collision.transform.IsChildOf(owner.transform))return;
            collision.collider.GetComponentInParent<GddHealth>()?.Damage(damage);
            var oldEnemy=collision.collider.GetComponentInParent<Enemy>();if(oldEnemy!=null)oldEnemy.Slay();
            RaiseActivated();Destroy(gameObject);
        }
    }
}
