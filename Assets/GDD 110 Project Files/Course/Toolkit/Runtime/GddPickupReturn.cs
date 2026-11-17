using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GDD110
{
    /// <summary>Decides what a pickup does after it is dropped, using a shared Drop Rule asset.</summary>
    [RequireComponent(typeof(GddPickup)), AddComponentMenu("GDD 110/Functions/Pickup Return")]
    public class GddPickupReturn : MonoBehaviour
    {
        [Tooltip("Shared setting asset. Create one with Create > GDD 110 > Drop Rule.")]
        public GddDropRule rule;
        public UnityEvent onDissolve = new UnityEvent();
        public UnityEvent onReturned = new UnityEvent();

        /// <summary>Seconds until this item goes home, or -1 when it is not waiting.</summary>
        public float SecondsLeft { get; private set; } = -1;

        GddPickup pickup;
        Rigidbody body;
        Transform homeParent;
        Vector3 homePos, homeScale;
        Quaternion homeRot;
        bool homeKinematic, homeGravity;
        Coroutine routine;

        void Awake()
        {
            pickup = GetComponent<GddPickup>();
            body = GetComponent<Rigidbody>();
            homeParent = transform.parent;
            homePos = transform.localPosition;
            homeRot = transform.localRotation;
            homeScale = transform.localScale;
            if (body != null) { homeKinematic = body.isKinematic; homeGravity = body.useGravity; }
            pickup.onDropped.AddListener(OnDropped);
            pickup.onEquipped.AddListener(Cancel);
        }

        void OnDropped()
        {
            if (rule == null || rule.mode == GddDropRule.Mode.StayOnGround) return;
            Cancel();
            routine = StartCoroutine(GoHome(rule.mode == GddDropRule.Mode.WaitThenReturn ? rule.waitSeconds : 0f));
        }

        void Cancel()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            SecondsLeft = -1;
        }

        /// <summary>Send it home now, whatever the rule says.</summary>
        public void ReturnNow() { Cancel(); routine = StartCoroutine(GoHome(0)); }

        IEnumerator GoHome(float wait)
        {
            SecondsLeft = wait;
            while (SecondsLeft > 0)
            {
                if (pickup.Holder != null) { SecondsLeft = -1; yield break; }
                SecondsLeft -= Time.deltaTime;
                yield return null;
            }
            SecondsLeft = -1;
            if (pickup.Holder != null) yield break;

            float d = rule != null ? rule.dissolveSeconds : 0.5f;
            onDissolve.Invoke();
            Vector3 start = transform.localScale;
            for (float t = 0; t < 1; t += Time.deltaTime / d)
            {
                transform.localScale = Vector3.Lerp(start, Vector3.zero, t * t);
                yield return null;
            }

            transform.SetParent(homeParent, false);
            transform.localPosition = homePos;
            transform.localRotation = homeRot;
            if (body != null)
            {
                if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                body.isKinematic = homeKinematic;
                body.useGravity = homeGravity;
            }
            onReturned.Invoke();
            for (float t = 0; t < 1; t += Time.deltaTime / (d * 0.8f))
            {
                float k = 1 + 2.2f * Mathf.Pow(t - 1, 3) + 1.2f * Mathf.Pow(t - 1, 2);
                transform.localScale = Vector3.LerpUnclamped(Vector3.zero, homeScale, k);
                yield return null;
            }
            transform.localScale = homeScale;
            routine = null;
        }
    }
}
