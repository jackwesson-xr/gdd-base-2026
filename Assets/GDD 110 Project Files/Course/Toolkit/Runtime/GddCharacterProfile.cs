using System;
using UnityEngine;
using StarterAssets;

namespace GDD110
{
    public sealed class GddCharacterProfile : ScriptableObject
    {
        [Tooltip("Optional replacement FBX. Import as Humanoid with its own Avatar and keep Optimize Game Objects off.")]
        public GameObject character;
        [Tooltip("Correction for unusual models. Start at zero.")]
        [Range(-180,180)] public float extraYaw;
        [Min(0)] public float heightOverride;
        [HideInInspector] public GameObject defaultHumanoid;
        [HideInInspector] public RuntimeAnimatorController locomotion;
        [HideInInspector] public Material shell, trim, glow;

        public Animator CreateVisual(Transform root, float height, float floor, Vector3 center)
        {
            if(character != null)
            {
                try { return Create(character,root,height,floor,center,false); }
                catch(Exception e) { Debug.LogWarning("Global Character is using the course robot: "+e.Message); }
            }
            return Create(defaultHumanoid,root,height,floor,center,true);
        }

        Animator Create(GameObject source, Transform root, float height, float floor, Vector3 center, bool robot)
        {
            if(source == null) throw new InvalidOperationException("Character model missing.");
            var mount = new GameObject(robot ? "Course Robot" : "Global Humanoid");
            mount.transform.SetParent(root,false);
            try
            {
                var model = Instantiate(source,mount.transform);
                model.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);
                var a = model.GetComponent<Animator>();
                if(a == null || a.avatar == null || !a.avatar.isValid || !a.avatar.isHuman)
                    throw new InvalidOperationException("Assign a character FBX with a valid Humanoid Avatar.");
                GddHumanoidFit.Fit(mount.transform,a,root,height,floor,center,robot?0:heightOverride,robot?0:extraYaw);
                var wolfMotion = model.GetComponent<GddWolfSecondaryMotion>();
                if (wolfMotion) wolfMotion.CalibrateFootPlacement(a, root);
                if(robot) DressRobot(a,root.rotation,height/1.8f);
                a.runtimeAnimatorController=locomotion;
                a.applyRootMotion=false;
                a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                a.enabled=true;
                var events=model.GetComponent<GddSkinAnimationEvents>() ?? model.AddComponent<GddSkinAnimationEvents>();
                events.motor=null;
                return a;
            }
            catch { mount.SetActive(false); Destroy(mount); throw; }
        }

        public void ApplyToPlayer(ThirdPersonController motor)
        {
            var rig=motor.GetComponent<GddRig>();
            var capsule=motor.GetComponent<CharacterController>();
            if(rig==null || capsule==null) return;
            var oldRenderers=motor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            var oldAnimator=motor.CharacterAnimator != null ? motor.CharacterAnimator : motor.GetComponent<Animator>();
            var next=CreateVisual(motor.transform,capsule.height,capsule.center.y-capsule.height*.5f,capsule.center);
            foreach(var r in oldRenderers) r.enabled=false;
            if(oldAnimator!=null) oldAnimator.enabled=false;
            motor.CharacterAnimator=next;
            next.GetComponent<GddSkinAnimationEvents>().motor=motor;
            rig.MapNewSkin(next);
        }

        void DressRobot(Animator a,Quaternion facing,float size)
        {
            foreach(var r in a.GetComponentsInChildren<SkinnedMeshRenderer>(true)) r.enabled=false;
            Transform Bone(HumanBodyBones bone)=>a.GetBoneTransform(bone);
            var hip=Bone(HumanBodyBones.Hips);
            var spine=Bone(HumanBodyBones.Chest) ?? Bone(HumanBodyBones.Spine);
            var head=Bone(HumanBodyBones.Head);
            Vector3 Up(float n)=>facing*Vector3.up*n*size;
            Vector3 Front(float n)=>facing*Vector3.forward*n*size;
            Part("Pelvis",hip,hip.position,facing,new Vector3(.32f,.21f,.22f)*size,trim);
            Part("Chest shell",spine,spine.position+Up(.06f),facing,new Vector3(.41f,.38f,.27f)*size,shell);
            Part("Core light",spine,spine.position+Up(.10f)+Front(.143f),facing,new Vector3(.135f,.10f,.028f)*size,glow);
            Part("Helmet",head,head.position+Up(.082f),facing,new Vector3(.29f,.25f,.25f)*size,shell);
            Part("Visor",head,head.position+Up(.10f)+Front(.128f),facing,new Vector3(.24f,.09f,.032f)*size,trim);
            Part("Optics",head,head.position+Up(.10f)+Front(.151f),facing,new Vector3(.18f,.032f,.018f)*size,glow);
            void Limb(HumanBodyBones start,HumanBodyBones end,float width,Material material)
            {
                var x=Bone(start);var y=Bone(end);if(x==null||y==null)return;
                var dir=y.position-x.position;if(dir.sqrMagnitude<.0001f)return;
                var q=Quaternion.FromToRotation(facing*Vector3.up,dir.normalized)*facing;
                Part(start.ToString(),x,(x.position+y.position)*.5f,q,new Vector3(width*size,dir.magnitude*.9f,width*size),material);
            }
            void Tip(HumanBodyBones bone,Vector3 shape,Vector3 shift)
            {
                var b=Bone(bone);if(b!=null)Part(bone.ToString(),b,b.position+facing*shift*size,facing,shape*size,trim);
            }
            Limb(HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,.14f,shell);
            Limb(HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,.14f,shell);
            Limb(HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,.13f,trim);
            Limb(HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,.13f,trim);
            Limb(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,.19f,trim);
            Limb(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,.19f,trim);
            Limb(HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,.16f,shell);
            Limb(HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,.16f,shell);
            Tip(HumanBodyBones.LeftHand,new Vector3(.105f,.10f,.10f),Vector3.zero);
            Tip(HumanBodyBones.RightHand,new Vector3(.105f,.10f,.10f),Vector3.zero);
            Tip(HumanBodyBones.LeftFoot,new Vector3(.15f,.11f,.25f),new Vector3(0,-.04f,.05f));
            Tip(HumanBodyBones.RightFoot,new Vector3(.15f,.11f,.25f),new Vector3(0,-.04f,.05f));
        }

        static void Part(string name,Transform bone,Vector3 position,Quaternion rotation,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name=name;
            var c=go.GetComponent<Collider>();c.enabled=false;Destroy(c);
            go.transform.SetPositionAndRotation(position,rotation);
            go.transform.localScale=size;
            go.transform.SetParent(bone,true);
            go.GetComponent<Renderer>().sharedMaterial=material;
        }
    }
}
