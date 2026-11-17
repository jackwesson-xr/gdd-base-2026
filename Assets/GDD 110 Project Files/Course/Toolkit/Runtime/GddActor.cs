using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GDD110
{
    [DisallowMultipleComponent, RequireComponent(typeof(GddRig)), AddComponentMenu("GDD 110/Player/Actor")]
    public class GddActor : GddEntity
    {
        public UnityEvent<int> onScoreChanged = new UnityEvent<int>();
        [Tooltip("Show the score at the top of the screen.")]
        public bool showScore = true;
        public int Score { get; private set; }
        public GddRig Rig => GetComponent<GddRig>();
        public GddInteractor Interactor => GetComponent<GddInteractor>();
        StarterAssets.ThirdPersonController motor;
        float baseWalk, baseSprint, baseJump;
        readonly Dictionary<object, Vector3> modifiers = new Dictionary<object, Vector3>();
        readonly List<object> expired = new List<object>();
        void Awake()
        {
            motor = GetComponent<StarterAssets.ThirdPersonController>();
            if (motor != null) { baseWalk=motor.MoveSpeed; baseSprint=motor.SprintSpeed; baseJump=motor.JumpHeight; }
            Rig.AutoMap();
        }
        void Start() { if (showScore) GddHud.Get().ShowScore(this); }
        void Update()
        {
            expired.Clear();
            foreach (var m in modifiers) if (m.Value.z <= Time.time) expired.Add(m.Key);
            if (expired.Count == 0) return;
            foreach (var key in expired) modifiers.Remove(key);
            ApplyModifiers();
        }
        public void AddScore(int points) { Score += points; onScoreChanged.Invoke(Score); }
        public void SetBoost(object source, float speed, float jump, float duration)
        {
            modifiers[source] = new Vector3(Mathf.Max(0.1f,speed), Mathf.Max(0.1f,jump), duration <= 0 ? float.PositiveInfinity : Time.time+duration);
            ApplyModifiers();
        }
        public void RemoveBoost(object source) { if (modifiers.Remove(source)) ApplyModifiers(); }
        void ApplyModifiers()
        {
            if (motor == null) return;
            float speed=1, jump=1;
            foreach (var m in modifiers.Values) { speed=Mathf.Max(speed,m.x); jump=Mathf.Max(jump,m.y); }
            motor.MoveSpeed=baseWalk*speed; motor.SprintSpeed=baseSprint*speed; motor.JumpHeight=baseJump*jump;
        }
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            foreach(var receiver in hit.collider.GetComponentsInParent<GddCollisionEvents>())receiver.NotifyActorContact(this);
        }
        void OnDisable() { modifiers.Clear(); ApplyModifiers(); }
        public void Teleport(Transform destination)
        {
            if (destination == null) return;
            var cc=GetComponent<CharacterController>(); bool wasEnabled=cc!=null&&cc.enabled;
            if (wasEnabled) cc.enabled=false;
            transform.SetPositionAndRotation(destination.position,destination.rotation);
            if (wasEnabled) cc.enabled=true;
        }
    }
}
