using UnityEngine;

namespace GDD110
{
    [AddComponentMenu("GDD 110/Functions/Power")]
    public class GddPower : GddEntity
    {
        public enum Activation { UseButton, OnEquip, WhileEquipped }
        [Tooltip("Global preset. Duplicate it in the Project window to make your own power.")]
        public GddPowerDefinition definition;
        public Activation activation;
        [Tooltip("Optional projectile origin. Otherwise fire from the character, toward the camera aim.")]
        public Transform muzzle;
        [Tooltip("Play the shared firearm pose when firing this projectile.")]
        public bool playFireAnimation;
        public Material projectileTrail;
        public GameObject muzzleEffect;
        readonly RaycastHit[] aimHits = new RaycastHit[16];
        float nextUse;
        GddActor wearer;
        public override void Activate() { var pickup=GetComponentInParent<GddPickup>();Use(wearer!=null?wearer:pickup!=null?pickup.Holder:LastActor); }
        public void Equip(GddActor actor)
        {
            wearer=actor;
            if(activation==Activation.OnEquip)Use(actor);
            if(activation==Activation.WhileEquipped&&definition!=null)
            {
                if(definition.kind==GddPowerKind.SpeedBoost)actor.SetBoost(this,definition.multiplier,1,0);
                else if(definition.kind==GddPowerKind.JumpBoost)actor.SetBoost(this,1,definition.multiplier,0);
            }
        }
        public void Unequip(GddActor actor) { if(actor!=null)actor.RemoveBoost(this);wearer=null; }
        public void Use(GddActor actor)
        {
            if(actor==null||Time.time<nextUse)return;
            LastActor=actor;
            if(definition==null){RaiseActivated();return;}
            nextUse=Time.time+Mathf.Max(0.05f,definition.cooldown);
            switch(definition.kind)
            {
                case GddPowerKind.Projectile: Fire(actor); break;
                case GddPowerKind.Heal: actor.GetComponent<GddHealth>()?.Heal(definition.amount); break;
                case GddPowerKind.Score: actor.AddScore(Mathf.RoundToInt(definition.amount)); break;
                case GddPowerKind.SpeedBoost: actor.SetBoost(this,definition.multiplier,1,definition.duration); break;
                case GddPowerKind.JumpBoost: actor.SetBoost(this,1,definition.multiplier,definition.duration); break;
                case GddPowerKind.Push:
                    var camera=Camera.main;Vector3 direction=camera!=null?camera.transform.forward:actor.transform.forward;
                    if(Physics.Raycast(actor.transform.position+Vector3.up,direction,out var hit,definition.range)&&hit.rigidbody!=null)hit.rigidbody.AddForce(direction*definition.amount,ForceMode.Impulse);
                    break;
            }
            RaiseActivated();
        }
        void Fire(GddActor actor)
        {
            if(definition.projectilePrefab==null)return;
            var camera = Camera.main;
            Vector3 direction = camera ? camera.transform.forward : actor.transform.forward;
            Vector3 origin = muzzle ? muzzle.position : actor.transform.position + Vector3.up * 1.3f + actor.transform.forward * .65f;
            if (camera)
            {
                Ray ray = camera.ViewportPointToRay(new Vector3(.5f, .5f, 0));
                float distance = 45f;
                int count = Physics.RaycastNonAlloc(ray, aimHits, distance, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                    if (!aimHits[i].transform.IsChildOf(actor.transform))
                        distance = Mathf.Min(distance, aimHits[i].distance);
                Vector3 toAimPoint = ray.GetPoint(distance) - origin;
                if (Vector3.Dot(toAimPoint, direction) > .08f) direction = toAimPoint.normalized;
            }
            if (playFireAnimation) GddCombatAnimation.Play(actor, "Fire Pulse");
            if (muzzleEffect) Destroy(Instantiate(muzzleEffect,origin,Quaternion.LookRotation(direction)),1.4f);
            var shot=Instantiate(definition.projectilePrefab,origin,Quaternion.LookRotation(direction));
            if (projectileTrail)
            {
                var trail=shot.AddComponent<TrailRenderer>();
                trail.sharedMaterial=projectileTrail;
                trail.time=.18f;
                trail.startWidth=.22f;
                trail.endWidth=0f;
                trail.minVertexDistance=.07f;
            }
            var projectile=shot.GetComponent<GddProjectile>();if(projectile!=null)projectile.Launch(actor,direction,definition.projectileSpeed,definition.amount,definition.projectileLifetime);
            else {var rb=shot.GetComponent<Rigidbody>();if(rb!=null)rb.linearVelocity=direction*definition.projectileSpeed;Destroy(shot,definition.projectileLifetime);}
        }
        void OnDisable(){if(wearer!=null)wearer.RemoveBoost(this);}
    }
}
