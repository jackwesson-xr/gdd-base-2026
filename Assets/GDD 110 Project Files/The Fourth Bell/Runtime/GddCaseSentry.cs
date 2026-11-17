using System.Collections;
using UnityEngine;

namespace GDD110
{
    [RequireComponent(typeof(GddHealth))]
    public sealed class GddCaseSentry : MonoBehaviour
    {
        public GddActor player;
        public bool ranged;
        [Min(0)] public float meleeDamageHearts = 1;
        [Min(0)] public float projectileDamageHearts = .5f;
        public Transform muzzle;
        public GameObject projectileVisual, chargeEffect, impactEffect;
        GddHealth health;
        Vector3 home;
        float ready;
        bool windingUp;

        public bool Alive => health && health.Current > 0;

        void Awake()
        {
            home=transform.position;
            health=GetComponent<GddHealth>();
        }

        void OnDisable() { windingUp=false; }

        void Update()
        {
            if (!player || windingUp || health.Current<=0) return;
            var flat=Vector3.ProjectOnPlane(player.transform.position-transform.position,Vector3.up);
            float distance=flat.magnitude;
            if (distance > 9 || Mathf.Abs(player.transform.position.y-transform.position.y)>1.5f || !CanSee()) return;
            if (flat.sqrMagnitude>.01f) transform.rotation=Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(flat), Time.deltaTime*230);
            if (!ranged && distance>1.65f && Vector3.Distance(home,transform.position)<4.5f)
                transform.position+=flat.normalized*Time.deltaTime*1.6f;
            if (Time.time>=ready && distance<(ranged?8f:1.8f) && CanSee())
                StartCoroutine(Attack());
        }

        bool CanSee()
        {
            Vector3 delta=player.transform.position+Vector3.up*.75f-muzzle.position;
            foreach (var hit in Physics.RaycastAll(muzzle.position,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(transform) && !hit.transform.IsChildOf(player.transform)) return false;
            return true;
        }

        IEnumerator Attack()
        {
            windingUp=true;
            Burst(chargeEffect,muzzle.position,muzzle.rotation);
            yield return new WaitForSeconds(.65f);
            if (health.Current>0 && CanSee())
            {
                if (ranged)
                {
                    var towards=player.transform.position+Vector3.up*.7f-muzzle.position;
                    if (!projectileVisual) { windingUp=false; ready=Time.time+1f; yield break; }
                    var shot=Instantiate(projectileVisual,muzzle.position,Quaternion.LookRotation(towards));
                    shot.AddComponent<GddCaseShot>().Fire(this,towards.normalized,player,impactEffect,projectileDamageHearts);
                }
                else if (Vector3.Distance(player.transform.position,transform.position)<2f)
                    Hurt(player,gameObject,meleeDamageHearts);
            }
            ready=Time.time+(ranged?2.1f:1.25f);
            windingUp=false;
        }

        internal static void Hurt(GddActor target, GameObject source, float hearts)
        {
            if (GddArmor.Block(target,source)) return;
            var armor = GddArmor.On(target);
            target.GetComponent<GddHealth>()?.Damage(hearts*GddFourthBell.UnitsPerHeart*(armor ? armor.damageMultiplier : 1f));
        }

        internal static void Burst(GameObject fx,Vector3 p,Quaternion q)
        {
            if(fx) Destroy(Instantiate(fx,p,q),2f);
        }
    }
}
