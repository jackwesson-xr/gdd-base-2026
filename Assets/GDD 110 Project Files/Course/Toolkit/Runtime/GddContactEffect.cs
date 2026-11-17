using UnityEngine;
namespace GDD110
{
    [AddComponentMenu("GDD 110/Functions/Contact Effect")]
    public class GddContactEffect : GddTrigger
    {
        public enum Effect { Damage, Heal, Teleport, NextScene, RestartScene, Launch, Knockback }
        public Effect effect;
        public float amount=20;
        public Transform destination;
        [Tooltip("Optional scene build index for a Next Scene effect. -1 advances to the next scene.")]
        public int sceneOverride = -1;
        [Tooltip("Launch impulse in world space. Uses the character motor when available.")]
        public Vector3 launchVelocity=new Vector3(0,12,0);
        [Tooltip("Repeat the effect every this many seconds while the player stays inside. 0 = only once on entry. Good for lava and healing zones.")]
        [Min(0)] public float repeatEvery;
        [Tooltip("Launch: sideways speed added in the direction the player was moving when they landed on it (or facing, if standing still).")]
        [Min(0)] public float carryForward=6f;
        [Tooltip("Launch: each bounce within a second of the last goes higher (x1, x1.3, x1.6), like a trampoline.")]
        public bool chainBounces=true;
        [Tooltip("Knockback: also take this much damage (0 = just a shove).")]
        [Min(0)] public float knockbackDamage;
        [Tooltip("Launch: seconds the pad ignores the player after a bounce, so nobody gets stuck on it.")]
        [Min(0)] public float bounceCooldown=0.45f;
        GddActor inside;
        float nextTick, readyAt, lastBounce;
        int chain;

        void Awake(){onActorExited.AddListener(ActorLeft);}
        void ActorLeft(GameObject go){if(inside!=null&&go==inside.gameObject)inside=null;}

        void Update()
        {
            if(repeatEvery<=0||inside==null||Time.time<nextTick)return;
            var capsule=inside.GetComponent<CharacterController>();
            if(!inside.isActiveAndEnabled || capsule && !GetComponent<BoxCollider>().bounds.Intersects(capsule.bounds))
            {
                inside=null;
                return;
            }
            LastActor=inside;RaiseActivated();if(this!=null&&inside!=null)Apply(inside);
            nextTick=Time.time+repeatEvery;
        }

        public override void Activate()
        {
            var actor=LastActor!=null?LastActor:FindAnyObjectByType<GddActor>();
            if(actor==null)return;
            LastActor=actor;RaiseActivated();if(this!=null&&actor!=null)OnActorEntered(actor);
        }
        protected override void OnActorEntered(GddActor actor)
        {
            inside=actor;nextTick=Time.time+repeatEvery;
            Apply(actor);
        }
        void Apply(GddActor actor)
        {
            switch(effect)
            {
                case Effect.Damage:actor.GetComponent<GddHealth>()?.Damage(amount);break;
                case Effect.Heal:actor.GetComponent<GddHealth>()?.Heal(amount);break;
                case Effect.Teleport:actor.Teleport(destination);inside=null;break;
                case Effect.NextScene:NextScene();break;
                case Effect.RestartScene:RestartScene();break;
                case Effect.Launch:Bounce(actor);break;
                case Effect.Knockback:Knock(actor);break;
            }
        }
        void Bounce(GddActor actor)
        {
            if(Time.time<readyAt)return;
            readyAt=Time.time+bounceCooldown;
            inside=null;
            chain=chainBounces&&Time.time-lastBounce<1.2f?Mathf.Min(chain+1,2):0;
            lastBounce=Time.time;
            float boost=1f+0.3f*chain;
            var cc=actor.GetComponent<CharacterController>();
            Vector3 flat=cc!=null?Vector3.ProjectOnPlane(cc.velocity,Vector3.up):Vector3.zero;
            Vector3 dir=flat.sqrMagnitude>0.25f?flat.normalized:actor.transform.forward;
            Vector3 impulse=new Vector3(launchVelocity.x,launchVelocity.y*boost,launchVelocity.z)+dir*carryForward;
            var launch=actor.GetComponent<GddLaunch>();if(launch==null)launch=actor.gameObject.AddComponent<GddLaunch>();
            launch.Launch(impulse);
        }
        /// <summary>Shoves the player back the way they came (away from where they were heading) and up a little.</summary>
        void Knock(GddActor actor)
        {
            if(Time.time<readyAt)return;
            readyAt=Time.time+bounceCooldown;
            inside=null;
            var cc=actor.GetComponent<CharacterController>();
            Vector3 flat=cc!=null?Vector3.ProjectOnPlane(cc.velocity,Vector3.up):Vector3.zero;
            Vector3 fromHere=Vector3.ProjectOnPlane(actor.transform.position-transform.position,Vector3.up);
            Vector3 back=flat.sqrMagnitude>0.25f?-flat.normalized:(fromHere.sqrMagnitude>0.01f?fromHere.normalized:-actor.transform.forward);
            var launch=actor.GetComponent<GddLaunch>();if(launch==null)launch=actor.gameObject.AddComponent<GddLaunch>();
            launch.Launch(back*amount+Vector3.up*Mathf.Max(3f,amount*0.4f));
            if(knockbackDamage>0)actor.GetComponent<GddHealth>()?.Damage(knockbackDamage);
        }
        public void NextScene()
        {
            if (sceneOverride >= 0) GddScenes.Load(sceneOverride);
            else GddScenes.Next();
        }
        public void RestartScene()=>GddScenes.Restart();
        public void FirstScene()=>GddScenes.First();
    }
}
