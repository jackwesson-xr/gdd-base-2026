using UnityEngine;
using UnityEngine.Events;
namespace GDD110
{
    [DisallowMultipleComponent, AddComponentMenu("GDD 110/Functions/Health")]
    public class GddHealth : GddEntity
    {
        [Min(1)] public float maximum=100;
        [Tooltip("0 = damage and healing use their amounts. Above 0 = health is split into this many chunks (like 5 hearts): every hit removes one chunk and every heal adds one back, whatever the amount.")]
        [Min(0)] public int chunks=0;
        [Tooltip("Chunks mode: seconds you can't be hit again after a hit, so touching an enemy doesn't drain every heart at once.")]
        [Min(0)] public float hitCooldown=0.6f;
        [Tooltip("Leave off for a player. Connect On Empty to a reset, scene action, or your own event.")]
        public bool destroyWhenEmpty;
        public UnityEvent<float> onHealthChanged=new UnityEvent<float>();
        public UnityEvent onEmpty=new UnityEvent();
        public float Current {get;private set;}
        /// <summary>0-1 share of maximum.</summary>
        public float Fraction => maximum>0?Current/maximum:0;
        /// <summary>Developer option: the player ignores all damage.</summary>
        public static bool PlayerInvulnerable;
        bool dead;
        float nextHit;
        void Awake(){Current=maximum;}
        float Step(float amount)=>chunks>0?maximum/chunks:Mathf.Max(0,amount);
        public void Damage(float amount)
        {
            if(dead)return;
            if(PlayerInvulnerable&&GetComponent<GddActor>()!=null)return;
            if(chunks>0){ if(Time.time<nextHit)return; nextHit=Time.time+hitCooldown; }
            Current=Mathf.Clamp(Current-Step(amount),0,maximum);
            if(Current<0.001f)Current=0;
            onHealthChanged.Invoke(Current);
            if(Current<=0){dead=true;RaiseActivated();onEmpty.Invoke();if(destroyWhenEmpty&&this!=null)Destroy(gameObject);}
        }
        /// <summary>Empty all health at once, ignoring hearts mode and the hit cooldown (instant kill).</summary>
        public void Kill()
        {
            if(dead)return;
            if(PlayerInvulnerable&&GetComponent<GddActor>()!=null)return;
            Current=0;onHealthChanged.Invoke(Current);
            dead=true;RaiseActivated();onEmpty.Invoke();if(destroyWhenEmpty&&this!=null)Destroy(gameObject);
        }
        public void Heal(float amount){Current=Mathf.Clamp(Current+Step(amount),0,maximum);if(Current>0)dead=false;onHealthChanged.Invoke(Current);}
        public void ResetHealth(){dead=false;Current=maximum;onHealthChanged.Invoke(Current);}
    }
}
