using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GDD110
{
    /// <summary>
    /// A comic-style speech or thought bubble that pops out of a character's mouth and types out lines.
    /// Start it by walking close, by pressing Interact, or only from UnityEvents. Interact also skips ahead and
    /// moves to the next line. Each line can have its own voice clip; a short talk blip can play while typing.
    /// Tick Advanced on a line to fire an event or drop an item in front of the player on that line.
    /// </summary>
    [AddComponentMenu("GDD 110/Functions/Dialog")]
    public class GddDialog : GddEntity
    {
        public enum Activation { Proximity, PressInteract, EventsOnly }
        public enum Order { InOrder, RandomLine }
        public enum Style { Speech, Thought }

        [Serializable]
        public class Line
        {
            [TextArea(2, 4)] public string text;
            [Tooltip("Optional spoken line. Plays alongside the text, or instead of it if the text is empty.")]
            public AudioClip voice;
            [Tooltip("Show extra options for this line: fire an event, drop an item.")]
            public bool advanced;
            public UnityEvent onLine = new UnityEvent();
            [Tooltip("Spawned in front of the player when this line starts (a pickup, a key, anything).")]
            public GameObject dropItem;
            [Tooltip("Where the item appears, relative to the player (x right, y up, z forward).")]
            public Vector3 dropOffset = new Vector3(0, 0.6f, 1.6f);
        }

        public Activation activation = Activation.PressInteract;
        [Min(0.5f)] public float proximityRadius = 4f;
        public Order order = Order.InOrder;
        public Style style = Style.Speech;
        public List<Line> lines = new List<Line> { new Line { text = "Hello!" } };

        [Header("Bubble")]
        [Tooltip("Where the tail points (the mouth), relative to this object. Drag the handle in the Scene view.")]
        public Vector3 mouth = new Vector3(0, 1.5f, 0.35f);
        [Tooltip("Where the bubble floats, relative to the mouth: x = to the side (on screen), y = up.")]
        public Vector2 bubbleOffset = new Vector2(0.7f, 0.9f);
        [Min(0)] public float charactersPerSecond = 40f;
        [Tooltip("Short sound repeated while typing (optional).")]
        public AudioClip talkBlip;
        [Range(0, 1)] public float blipVolume = 0.45f;
        [Tooltip("Move on by itself this many seconds after a line finishes. 0 = wait for Interact.")]
        [Min(0)] public float autoAdvanceSeconds = 0f;
        [Tooltip("Start over after the last line instead of closing.")]
        public bool loop;

        public UnityEvent onDialogStarted = new UnityEvent();
        public UnityEvent onDialogFinished = new UnityEvent();

        public bool IsOpen { get; private set; }
        public bool Typing => bubble != null && bubble.Typing;

        GddSpeechBubble bubble;
        AudioSource voice;
        int index = -1, shown;
        float idle;
        bool closedByDistance;

        public override string Prompt => !IsOpen ? "Talk to" : Typing ? "Skip" : HasNext ? "Next" : "Close";
        public override bool CanInteract(GddActor actor) => activation == Activation.PressInteract || IsOpen;
        bool HasNext => loop || order == Order.RandomLine || index < lines.Count - 1;

        public override void Interact(GddActor actor)
        {
            LastActor = actor;
            if (!IsOpen) StartDialog(); else Advance();
        }

        public override void Activate() { if (!IsOpen) StartDialog(); else Advance(); }

        /// <summary>Open the bubble at the first line (or a random one).</summary>
        public void StartDialog()
        {
            if (lines.Count == 0) return;
            IsOpen = true;
            index = -1;
            shown = 0;
            onDialogStarted.Invoke();
            RaiseActivated();
            ShowNext();
        }

        /// <summary>Finish typing the current line, or go to the next one, or close after the last.</summary>
        public void Advance()
        {
            if (!IsOpen) { StartDialog(); return; }
            if (Typing) { bubble.FinishTyping(); return; }
            if (!HasNext) { Close(); return; }
            ShowNext();
        }

        /// <summary>Say a specific line from the list (0 = first). Handy from UnityEvents.</summary>
        public void Say(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= lines.Count) return;
            IsOpen = true;
            index = lineIndex;
            Show(lines[lineIndex]);
        }

        /// <summary>Say any text right now, outside the list.</summary>
        public void Say(string text) { IsOpen = true; Show(new Line { text = text }); }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            closedByDistance = true;   // proximity dialogs wait for the player to step away before starting again
            if (bubble != null) bubble.Hide();
            if (voice != null) voice.Stop();
            onDialogFinished.Invoke();
        }

        void ShowNext()
        {
            if (order == Order.RandomLine && lines.Count > 1)
            {
                int next;
                do next = UnityEngine.Random.Range(0, lines.Count); while (next == index);
                index = next;
            }
            else index = (index + 1) % lines.Count;
            shown++;
            Show(lines[index]);
        }

        void Show(Line line)
        {
            idle = 0f;
            if (bubble == null)
            {
                var go = new GameObject("Dialog Bubble - " + DisplayName);
                bubble = go.AddComponent<GddSpeechBubble>();
                bubble.Init(this);
            }
            if (!string.IsNullOrEmpty(line.text)) bubble.Show(line.text, style == Style.Thought, charactersPerSecond, talkBlip, blipVolume);
            else bubble.Hide();
            if (line.voice != null)
            {
                if (voice == null) { voice = gameObject.AddComponent<AudioSource>(); voice.playOnAwake = false; voice.spatialBlend = 0.6f; voice.maxDistance = 20f; }
                voice.clip = line.voice;
                voice.Play();
            }
            if (line.advanced)
            {
                line.onLine.Invoke();
                if (line.dropItem != null) Drop(line);
            }
        }

        void Drop(Line line)
        {
            var actor = LastActor != null ? LastActor : FindAnyObjectByType<GddActor>();
            Transform around = actor != null ? actor.transform : transform;
            Vector3 pos = around.position + around.right * line.dropOffset.x + Vector3.up * line.dropOffset.y + around.forward * line.dropOffset.z;
            var item = Instantiate(line.dropItem, pos, Quaternion.LookRotation(-around.forward));
            item.name = line.dropItem.name;
            item.SetActive(true);
            GddEvents.RaiseSpawned(item);
        }

        public Vector3 MouthWorld => transform.TransformPoint(mouth);

        void Update()
        {
            var actor = LastActor != null ? LastActor : FindAnyObjectByType<GddActor>();
            if (actor == null) return;
            if (LastActor == null) LastActor = actor;
            float dist = Vector3.Distance(actor.transform.position, transform.position);

            if (activation == Activation.Proximity)
            {
                if (dist > proximityRadius * 1.3f) closedByDistance = false;
                if (!IsOpen && !closedByDistance && dist < proximityRadius) StartDialog();
            }
            if (IsOpen && dist > Mathf.Max(proximityRadius, 3f) * 1.3f) Close();

            if (IsOpen && !Typing && autoAdvanceSeconds > 0f)
            {
                idle += Time.deltaTime;
                if (idle >= autoAdvanceSeconds) { idle = 0f; if (HasNext) ShowNext(); else Close(); }
            }
        }

        void OnDisable() { Close(); }
        void OnDestroy() { if (bubble != null) Destroy(bubble.gameObject); }
    }
}
