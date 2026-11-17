using System.Collections.Generic;
using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using StarterAssets;

namespace GDD110
{
    [DisallowMultipleComponent]
    public sealed class GddFourthBell : MonoBehaviour
    {
        public const string Key = "Return Key";
        public const float UnitsPerHeart = 20f;
        public GddActor player;
        public GddLevelBlock[] levels;
        public CinemachineCamera follow;
        public Light sun;
        public AudioClip instrumental;
        public AudioClip braaam;
        public TMP_Text area, objective, clue, hearts;
        public CanvasGroup cluePanel, fade;
        public Sprite heartEmblem;
        public Material eveningSky;
        Image reticle;
        GddPickup aimedPickup;
        GddPower heldPower;
        GddSlashAttack heldBlade;
        public GddMover yardGate, culvertGate;
        public GddFocusMoment yardFocus, culvertFocus, bellFocus;
        public GddCaseSentry melee, ranged;
        public GameObject exit;
        public GddMusicOverride music;
        public int CurrentLevel { get; private set; }
        public bool MusicStarted { get; private set; }
        public float Tension { get; private set; }
        Transform checkpoint;
        GddHealth health;
        ThirdPersonController motor;
        float clueUntil, nextBraaam;
        GddWorldCue[] areaCues;
        GddWorldCue highlighted;
        readonly HashSet<GddCaseSentry> spotted = new();
        GddCaseSentry[] sentries;
        GddCaseSentry featured;
        float featureUntil, nextFocusScan;
        bool battleLesson;
        bool travelling, finished, yardPowered, lockOpened;
        InputAction toggleObjective;
        const string ObjectivePreference = "GDD110.ShowNextStep";
        [SerializeField, HideInInspector] CanvasGroup objectiveCard;

        void Awake()
        {
            health = player.GetComponent<GddHealth>();
            motor = player.GetComponent<ThirdPersonController>();
            sentries = new[] { melee, ranged };
            checkpoint = levels[0].arrival;
            health.onEmpty.AddListener(Recover);
            for (int i=0; i<levels.Length; i++) levels[i].gameObject.SetActive(i==0);
            player.Teleport(checkpoint);
            ShowLevel(levels[0]);
        }

        void OnEnable()
        {
            toggleObjective = new InputAction("Toggle next step", InputActionType.Button);
            toggleObjective.AddBinding("<Keyboard>/h");
            toggleObjective.AddBinding("<Gamepad>/dpad/up");
            toggleObjective.performed += OnToggleObjective;
            toggleObjective.Enable();
        }

        void OnDisable()
        {
            if (toggleObjective == null) return;
            toggleObjective.Dispose();
            toggleObjective = null;
        }

        void OnToggleObjective(InputAction.CallbackContext context)
        {
            if (!GddGameRuntime.WindowOpen) ToggleNextStep();
        }

        void Start()
        {
            var plate = objective.transform.parent.gameObject;
            if (!plate.TryGetComponent(out objectiveCard)) objectiveCard = plate.AddComponent<CanvasGroup>();
            objectiveCard.blocksRaycasts = false;
            objectiveCard.interactable = false;
            GddHudFonts.AddPanelShadow(plate.GetComponent<Image>());
            SetNextStepVisible(PlayerPrefs.GetInt(ObjectivePreference, 1) != 0);
            if (cluePanel) GddHudFonts.AddPanelShadow(cluePanel.GetComponent<Image>());
            if (hearts && heartEmblem)
            {
                var heartRow = new GameObject("Patchwork hearts", typeof(RectTransform)).GetComponent<RectTransform>();
                heartRow.SetParent(hearts.rectTransform.parent.parent, false);
                heartRow.anchorMin = heartRow.anchorMax = heartRow.pivot = new Vector2(0, 1);
                heartRow.anchoredPosition = new Vector2(20, -20);
                var backing = new GameObject("Health backing", typeof(RectTransform), typeof(Image));
                var background = (RectTransform)backing.transform;
                background.SetParent(heartRow.parent, false);
                background.SetSiblingIndex(heartRow.GetSiblingIndex());
                background.anchorMin = background.anchorMax = background.pivot = new Vector2(0, 1);
                background.anchoredPosition = new Vector2(13, -13);
                background.sizeDelta = new Vector2(43 * (health.chunks > 0 ? health.chunks : 5) *
                    heartEmblem.rect.width / heartEmblem.rect.height + 14, 57);
                var image = backing.GetComponent<Image>();
                image.color = new Color(.035f,.06f,.049f,.84f);
                image.raycastTarget = false;
                GddHudFonts.AddPanelShadow(image);
                var bar = heartRow.gameObject.AddComponent<GddHealthHud>();
                bar.health = health;
                bar.icon = heartEmblem;
                bar.iconSize = 43;
                bar.pureWhiteIsClear = false;
                hearts.rectTransform.parent.gameObject.SetActive(false);
            }

            var sight = new GameObject("Aim reticle", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)sight.transform;
            rect.SetParent(fade.transform.parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(7, 7);
            reticle = sight.GetComponent<Image>();
            reticle.color = new Color(1, .91f, .62f);
            reticle.raycastTarget = false;
            reticle.enabled = false;
        }

        void OnDestroy()
        {
            if (health) health.onEmpty.RemoveListener(Recover);
        }

        void Update()
        {
            if (Keyboard.current?.rKey.wasPressedThisFrame == true && !GddGameRuntime.WindowOpen) Recover();
            if (!travelling && player.transform.position.y < -4) Recover();
            if (!MusicStarted && (CurrentLevel > 0 || player.transform.position.z > 2 || Time.timeSinceLevelLoad > 9))
            {
                MusicStarted = true;
                if (music) music.TurnOn();
            }
            float near = Mathf.Max(Threat(melee),Threat(ranged));
            if (near > .72f && Time.time >= nextBraaam)
            {
                nextBraaam = Time.time + 14;
                if (braaam) AudioSource.PlayClipAtPoint(braaam, player.transform.position, .38f);
            }
            Tension = Mathf.MoveTowards(Tension, near, Time.unscaledDeltaTime * (near > Tension ? 2 : .57f));
            cluePanel.alpha = Mathf.MoveTowards(cluePanel.alpha, Time.unscaledTime < clueUntil ? 1 : 0, Time.unscaledDeltaTime * 5);
            var equipped = player.Interactor.Equipped;
            if (equipped != aimedPickup)
            {
                aimedPickup = equipped;
                heldPower = equipped ? equipped.GetComponentInChildren<GddPower>() : null;
                heldBlade = equipped ? equipped.GetComponentInChildren<GddSlashAttack>() : null;
            }
            bool hasBlaster = heldPower && heldPower.definition && heldPower.definition.kind == GddPowerKind.Projectile;
            bool aiming = hasBlaster && GddControls.Aim && !GddGameRuntime.WindowOpen && !travelling;
            motor.AimFacing = aiming;
            UpdateAttention(aiming);
            if (reticle) reticle.enabled = aiming;
            GddCombatAnimation.SetGunReady(player, aiming && !heldBlade &&
                motor.CharacterAnimator && motor.CharacterAnimator.GetFloat("Speed") < .22f);
        }

        public static void ToggleNextStep()
        {
            bool show = PlayerPrefs.GetInt(ObjectivePreference, 1) == 0;
            var current = FindAnyObjectByType<GddFourthBell>();
            if (current && current.objectiveCard) current.SetNextStepVisible(show);
            else PlayerPrefs.SetInt(ObjectivePreference, show ? 1 : 0);
        }

        public void SetNextStepVisible(bool visible)
        {
            if (!objectiveCard) return;
            objectiveCard.alpha = visible ? 1f : 0f;
            PlayerPrefs.SetInt(ObjectivePreference, visible ? 1 : 0);
        }

        void UpdateAttention(bool aiming)
        {
            if (travelling || aiming || GddGameRuntime.WindowOpen)
            {
                SetAttention(null, 0);
                return;
            }
            if (Time.unscaledTime < nextFocusScan) return;
            nextFocusScan = Time.unscaledTime + .10f;
            var view=Camera.main;
            if (!view || areaCues == null) return;
            foreach (var enemy in sentries)
            {
                if (featured && featured.Alive && Time.unscaledTime < featureUntil) break;
                if (!enemy || !enemy.isActiveAndEnabled || !enemy.Alive || spotted.Contains(enemy) ||
                    !enemy.TryGetComponent<GddWorldCue>(out var cue) || !Visible(view, cue, 11)) continue;
                spotted.Add(enemy);
                featured=enemy;
                featureUntil=Time.unscaledTime+1.1f;
                if (!battleLesson)
                {
                    battleLesson=true;
                    Read("Custodians attack. The shield blocks hits; the helmet reduces damage.");
                }
                break;
            }
            if (featured && featured.Alive && Time.unscaledTime < featureUntil &&
                featured.TryGetComponent<GddWorldCue>(out var target) && Visible(view,target,11))
            {
                SetAttention(target, 9);
                return;
            }
            GddWorldCue best=null;
            float nearest=64;
            foreach (var cue in areaCues)
            {
                if (!cue || !cue.isActiveAndEnabled || !cue.pulse || !cue.Available) continue;
                float distance=(cue.transform.position-player.transform.position).sqrMagnitude;
                if (distance >= nearest || !Visible(view,cue,8)) continue;
                nearest=distance;
                best=cue;
            }
            SetAttention(best, 5);
        }

        bool Visible(Camera view, GddWorldCue cue, float range)
        {
            if ((cue.transform.position-player.transform.position).sqrMagnitude > range*range) return false;
            Vector3 viewport=view.WorldToViewportPoint(cue.FocusPoint);
            if (viewport.z <= 0 || viewport.x < .12f || viewport.x > .88f || viewport.y < .12f || viewport.y > .88f)
                return false;
            Vector3 delta=cue.FocusPoint-view.transform.position;
            foreach (var hit in Physics.RaycastAll(view.transform.position,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(player.transform) && !hit.transform.IsChildOf(cue.transform)) return false;
            return true;
        }

        void SetAttention(GddWorldCue cue, float degrees)
        {
            if (highlighted && highlighted != cue) highlighted.Highlighted=false;
            highlighted=cue;
            if (highlighted) highlighted.Highlighted=true;
            motor.CameraInterest=highlighted ? highlighted.FocusPoint : null;
            motor.CameraInterestDegrees=degrees;
        }

        float Threat(GddCaseSentry sentry) => sentry && sentry.gameObject.activeInHierarchy
            ? Mathf.Clamp01((9 - Vector3.Distance(sentry.transform.position, player.transform.position)) / 5)
            : 0;

        public void Read(string line)
        {
            clue.text = line;
            clueUntil = Time.unscaledTime + 6;
        }

        public void PowerLift()
        {
            if (yardPowered) return;
            yardPowered=true;
            yardGate.TurnOn();
            Read("Service lift powered.");
            objective.text="Take the service lift.";
            yardFocus.Play();
        }

        public void MissingKey() => Read("The reader needs the brass return key.");

        public void FoundKey()
        {
            objective.text="Unlock the orchard lift.";
            Read("Return key collected.");
        }

        public void OpenCulvert()
        {
            lockOpened=true;
            culvertGate.TurnOn();
            Read("Orchard lift unlocked.");
            objective.text="Take the orchard lift.";
            culvertFocus.Play();
        }

        public void SetCheckpoint(Transform marker)
        {
            if (checkpoint == marker) return;
            checkpoint=marker;
            Read("Checkpoint saved. Press R to return here.");
        }

        public void Travel(GddLevelBlock to)
        {
            int index=System.Array.IndexOf(levels,to);
            if (travelling || index != CurrentLevel+1 || !to || !to.arrival) return;
            if (CurrentLevel==0 && !yardPowered || CurrentLevel==1 && !lockOpened) return;
            StartCoroutine(Transfer(to,index));
        }

        public void Recover()
        {
            if (travelling || !checkpoint) return;
            StartCoroutine(Transfer(levels[CurrentLevel],CurrentLevel));
        }

        public void RingBell()
        {
            if (finished) return;
            finished=true;
            exit.SetActive(true);
            objective.text="Enter the crossing.";
            Read("The crossing is open.");
            bellFocus.Play();
        }

        public void Finish()
        {
            if (!finished || travelling) return;
            travelling=true;
            GddScenes.Load(3);
        }

        void ShowLevel(GddLevelBlock level)
        {
            areaCues=level.GetComponentsInChildren<GddWorldCue>(true);
            area.text=level.areaName;
            objective.text=level.objective;
            RenderSettings.ambientLight=level.ambient;
            bool indoors = level == levels[0];
            RenderSettings.fog = !indoors;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 20f;
            RenderSettings.fogEndDistance = 67f;
            RenderSettings.fogColor = level.fog;
            if (eveningSky) RenderSettings.skybox = eveningSky;
            if (Camera.main)
            {
                Camera.main.backgroundColor = level.fog;
                Camera.main.clearFlags = !indoors && eveningSky ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            }
            sun.color=level.sunlight;
            sun.intensity = indoors ? .33f : 1.05f;
        }

        IEnumerator Transfer(GddLevelBlock next, int index)
        {
            travelling=true;
            SetAttention(null, 0);
            motor.enabled=false;
            var interactor = player.Interactor;
            bool interactionEnabled = interactor.enabled;
            interactor.enabled = false;
            var previous=levels[CurrentLevel];
            bool riding=next!=previous && previous.departingLift && next.arrivingLift;
            if (riding) yield return Ride(previous.departingLift, Vector3.zero, Vector3.up*previous.liftTravel, 0, 1);
            else yield return FadeTo(1);
            if (next!=previous)
            {
                next.gameObject.SetActive(true);
                checkpoint=next.arrival;
                CurrentLevel=index;
                ShowLevel(next);
            }
            var old=player.transform.position;
            player.Teleport(checkpoint);
            Vector3 ingress=riding ? Vector3.down*previous.liftTravel : Vector3.zero;
            if (riding) player.transform.position+=ingress;
            if (follow && motor.CinemachineCameraTarget)
                follow.OnTargetObjectWarped(motor.CinemachineCameraTarget.transform, player.transform.position-old);
            motor.SetLook(checkpoint.eulerAngles.y,0);
            if (next==previous) health.ResetHealth();
            if (next!=previous) previous.gameObject.SetActive(false);
            if (riding) yield return Ride(next.arrivingLift, ingress, Vector3.zero, 1, 0);
            else yield return FadeTo(0);
            motor.enabled=true;
            interactor.enabled = interactionEnabled;
            travelling=false;
        }

        IEnumerator Ride(Transform platform, Vector3 from, Vector3 to, float fadeFrom, float fadeTo)
        {
            var capsule=player.GetComponent<CharacterController>();
            capsule.enabled=false;
            Vector3 platformHome=platform.position;
            Vector3 feet=player.transform.position-from;
            const float seconds=.85f;
            float time=0;
            while (time < seconds)
            {
                float progress=Mathf.Clamp01(time/seconds);
                Vector3 offset=Vector3.Lerp(from,to,Mathf.SmoothStep(0,1,progress));
                platform.position=platformHome+offset;
                player.transform.position=feet+offset;
                fade.alpha=Mathf.Lerp(fadeFrom,fadeTo,Mathf.SmoothStep(0,1,progress));
                time+=Time.unscaledDeltaTime;
                yield return null;
            }
            platform.position=platformHome+to;
            player.transform.position=feet+to;
            fade.alpha=fadeTo;
            capsule.enabled=true;
        }

        IEnumerator FadeTo(float value)
        {
            while (!Mathf.Approximately(fade.alpha,value))
            {
                fade.alpha=Mathf.MoveTowards(fade.alpha,value,Time.unscaledDeltaTime*4f);
                yield return null;
            }
        }
    }
}
