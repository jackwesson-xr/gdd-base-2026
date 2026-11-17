using UnityEngine;

namespace GDD110
{
    public sealed class GddCaseShot : MonoBehaviour
    {
        Vector3 direction;
        GddCaseSentry origin;
        GddActor target;
        GameObject impact;
        float hearts;

        public void Fire(GddCaseSentry sentry,Vector3 heading,GddActor player,GameObject burst,float damage)
        {
            direction=heading; origin=sentry; target=player; impact=burst; hearts=damage;
            Destroy(gameObject,3f);
        }

        void Update()
        {
            if (!origin || !origin.isActiveAndEnabled) { Destroy(gameObject); return; }
            float step = 8f*Time.deltaTime;
            RaycastHit? nearest = null;
            foreach (var hit in Physics.SphereCastAll(transform.position, .1f, direction, step, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(origin.transform)) continue;
                if (!nearest.HasValue || hit.distance < nearest.Value.distance) nearest = hit;
            }
            if (nearest.HasValue)
            {
                var hit = nearest.Value;
                if (hit.collider.GetComponentInParent<GddActor>() == target)
                    GddCaseSentry.Hurt(target, gameObject, hearts);
                GddCaseSentry.Burst(impact, hit.point, Quaternion.LookRotation(-direction));
                Destroy(gameObject);
                return;
            }
            transform.position+=direction*step;
        }
    }
}
