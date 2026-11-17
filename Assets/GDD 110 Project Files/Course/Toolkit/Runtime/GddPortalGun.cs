using UnityEngine;
using UnityEngine.Events;

namespace GDD110
{
    /// <summary>
    /// Carried gun (on a Pickup): press Use to shoot where the camera points. A portal opens on whatever it hits;
    /// walking into it takes you to Destination. Each shot links to a return portal at Destination. A new shot replaces the pair.
    /// </summary>
    [RequireComponent(typeof(GddPickup)), AddComponentMenu("GDD 110/Functions/Portal Gun")]
    public class GddPortalGun : GddEntity, IGddUsable
    {
        [Tooltip("Where the portal sends you.")]
        public Transform destination;
        [Tooltip("The portal's look, e.g. a swirling portal effect. A glowing disc is used if empty.")]
        public GameObject portalLook;
        public GameObject muzzleEffect, impactEffect;
        public AudioClip shootSound;
        public float range = 60f;
        public float portalSize = 2f;
        public UnityEvent onPortalOpened = new UnityEvent();

        GameObject portal, returnPortal;

        public void Use(GddActor holder)
        {
            var cam = Camera.main;
            if (!cam || !holder || !destination) return;
            LastActor = holder;
            Vector3 muzzle = transform.position;
            if (muzzleEffect != null) Destroy(Instantiate(muzzleEffect, muzzle, cam.transform.rotation), 2f);
            if (shootSound != null) AudioSource.PlayClipAtPoint(shootSound, muzzle, 0.9f);
            var hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, range, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(holder.transform) || hit.transform.IsChildOf(transform)) continue;
                Open(hit.point, hit.normal);
                return;
            }
        }

        void Open(Vector3 point, Vector3 normal)
        {
            if (portal) Destroy(portal);
            if (returnPortal) Destroy(returnPortal);
            bool floor = Vector3.Dot(normal, Vector3.up) > .6f;
            Vector3 face = floor ? Vector3.ProjectOnPlane(Camera.main.transform.position - point, Vector3.up) :
                Vector3.ProjectOnPlane(normal, Vector3.up);
            if (face.sqrMagnitude < .01f) return;
            face.Normalize();
            Vector3 sourceFeet = point + face * 1.25f - (floor ? Vector3.zero : Vector3.up * portalSize * .45f);
            portal = MakePortal("Portal entrance", point + face*.05f + (floor ? Vector3.up * portalSize*.5f : Vector3.zero),
                face, destination);
            var returnPoint = new GameObject("Portal return landing").transform;
            returnPoint.SetParent(portal.transform, true);
            returnPoint.SetPositionAndRotation(sourceFeet, Quaternion.LookRotation(face));
            Vector3 exitFace = Vector3.ProjectOnPlane(destination.forward,Vector3.up).normalized;
            if (exitFace.sqrMagnitude < .1f) exitFace=Vector3.forward;
            returnPortal = MakePortal("Linked return portal",
                destination.position - exitFace*1.5f + Vector3.up*portalSize*.5f, exitFace, returnPoint);
            if (impactEffect) Destroy(Instantiate(impactEffect, point, Quaternion.LookRotation(normal)),3f);
            onPortalOpened.Invoke();
            RaiseActivated();
        }

        GameObject MakePortal(string portalName, Vector3 point, Vector3 face, Transform landing)
        {
            var portal = new GameObject(portalName);
            portal.transform.SetPositionAndRotation(point,Quaternion.LookRotation(face));
            if (portalLook != null)
            {
                var look = Instantiate(portalLook, portal.transform);
                look.transform.localPosition = Vector3.zero;
                look.transform.localRotation = Quaternion.identity;
                look.transform.localScale *= portalSize * 0.5f;
            }
            else
            {
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(disc.GetComponent<Collider>());
                disc.transform.SetParent(portal.transform, false);
                disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
                disc.transform.localScale = new Vector3(portalSize, 0.02f, portalSize);
            }
            var box = portal.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(portalSize * 0.9f, portalSize, 1.2f);
            var rb = portal.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
            var tele = portal.AddComponent<GddContactEffect>();
            tele.effect = GddContactEffect.Effect.Teleport;
            tele.destination = landing;
            tele.friendlyName = "Portal";
            return portal;
        }

        void OnDestroy()
        {
            if (portal) Destroy(portal);
            if (returnPortal) Destroy(returnPortal);
        }
    }
}
