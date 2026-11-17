using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Timeline;

namespace GDD110.Juice.EditorTools
{
    /// <summary>
    /// Block Showcase layout: a plaza with six districts around it, one per way a block gets activated
    /// (on entry, on touch, on a timer, press a key, pick up and use, creatures). Triggerables sit next to
    /// whatever switches them, joined by Links.
    /// </summary>
    public static partial class GddJuiceSetup
    {
        // ---------- materials ----------

        static Material FloorMat(string name, Color a, Color b, Color accent, float tile = 1f, float major = 4f, float glow = 0.4f, float bevel = 0.1f)
        {
            Folder(MatDir);
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("GDD/Training Floor");
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_ColorA", a); m.SetColor("_ColorB", b);
            m.SetColor("_LineColor", Color.white); m.SetColor("_AccentColor", accent);
            m.SetFloat("_TileSize", tile); m.SetFloat("_MajorEvery", major);
            m.SetFloat("_Emission", glow); m.SetFloat("_Bevel", bevel);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Mat(string name, Color color, float smooth = 0.4f, Color? emission = null, bool transparent = false)
        {
            Folder(MatDir);
            string path = MatDir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, path); }
            m.shader = lit;
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smooth);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else m.DisableKeyword("_EMISSION");
            if (transparent)
            {
                m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Sky()
        {
            string path = MatDir + "/Showcase Sky.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { Folder(MatDir); m = new Material(Shader.Find("Skybox/Procedural")); AssetDatabase.CreateAsset(m, path); }
            m.SetFloat("_SunSize", 0.035f);
            m.SetFloat("_SunSizeConvergence", 6f);
            m.SetFloat("_AtmosphereThickness", 0.7f);
            m.SetColor("_SkyTint", new Color(0.52f, 0.66f, 0.95f));
            m.SetColor("_GroundColor", new Color(0.8f, 0.83f, 0.9f));
            m.SetFloat("_Exposure", 1.25f);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------- primitives ----------

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            var col = go.GetComponent<Collider>();
            if (!collider || type == PrimitiveType.Cylinder) Object.DestroyImmediate(col);
            // Primitive cylinders get a capsule collider, which balloons into a sphere when the cylinder is flat.
            if (collider && type == PrimitiveType.Cylinder) go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            return go;
        }

        static TMP_FontAsset Font(string weight) => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/GDD 110 Project Files/Wesson Assets/Fonts/Lato/Lato " + weight + " SDF.asset");

        static TextMeshPro Text(string text, Transform parent, Vector3 worldPos, Quaternion rot, float size, Color color, FontStyles style = FontStyles.Bold, float width = 14, TMP_FontAsset font = null)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(worldPos, rot);
            var t = go.AddComponent<TextMeshPro>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.fontStyle = style;
            t.rectTransform.sizeDelta = new Vector2(width, 3);
            return t;
        }

        static GameObject Block(string kind, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Toolkit + "/" + kind + ".prefab");
            if (prefab == null) { Debug.LogWarning("[GDD Juice] Missing toolkit prefab " + kind); return null; }
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }

        static TMP_FontAsset DisplayFont()
        {
            foreach (var g in AssetDatabase.FindAssets("PoetsenOne-Regular SDF t:TMP_FontAsset"))
            {
                var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g));
                if (f != null) return f;
            }
            return Font("Black");
        }

        /// <summary>
        /// Give a busy district more room: pushes every station apart by k and grows the floor to match,
        /// while the entrance gate stays where the path from the plaza meets it.
        /// </summary>
        static void Spread(District d, float k)
        {
            foreach (Transform c in d.root)
            {
                if (c.name == "District Floor" || c.name == "Trim" || c.name == "Gate") continue;
                var p = c.localPosition;
                c.localPosition = new Vector3(p.x * k, p.y, p.z * k);
            }
            var floor = d.root.Find("District Floor");
            if (floor != null) floor.localScale = new Vector3(floor.localScale.x * k, floor.localScale.y, floor.localScale.z * k);
            var trim = d.root.Find("Trim");
            if (trim != null) { trim.localScale = new Vector3(trim.localScale.x * k, trim.localScale.y, trim.localScale.z); trim.localPosition = new Vector3(0, trim.localPosition.y, trim.localPosition.z * k); }
            float shift = (k - 1f) * d.size.y / 2f;
            d.root.position += d.root.forward * shift;
            var gate = d.root.Find("Gate");
            if (gate != null) gate.localPosition -= new Vector3(0, 0, shift);
        }

        static GameObject KayKit(string name)
        {
            foreach (var g in AssetDatabase.FindAssets(name + " t:Prefab", new[] { "Assets/GDD 110 Project Files/Wesson Assets/Prefabs/Platformer" }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == name) return AssetDatabase.LoadAssetAtPath<GameObject>(p);
            }
            Debug.LogWarning("[GDD Juice] KayKit prefab not found: " + name);
            return null;
        }

        /// <summary>
        /// Dress a block in a KayKit model: hides the block's own mesh (its collider and behavior stay) and puts the
        /// model on it, scaled to the given width. Bottom-aligned, or centered for floating collectibles.
        /// </summary>
        static GameObject Skin(GameObject block, string kaykit, float width, bool centered = false, float yaw = 0f)
            => SkinWith(block, KayKit(kaykit), kaykit, width, centered, yaw);

        static GameObject SkinWith(GameObject block, GameObject prefab, string kaykit, float width, bool centered = false, float yaw = 0f)
        {
            if (block == null || prefab == null) return null;
            var own = block.GetComponent<Renderer>();
            // Already dressed (the model was applied into the toolkit prefab by hand): keep that tuning.
            var existing = block.transform.Find("Model (" + kaykit + ")");
            if (existing != null) { if (own != null) own.enabled = false; return existing.gameObject; }
            Bounds target = own != null ? own.bounds : new Bounds(block.transform.position, Vector3.one);
            if (own != null) own.enabled = false;
            var skin = (GameObject)PrefabUtility.InstantiatePrefab(prefab, block.transform);
            skin.name = "Model (" + kaykit + ")";
            skin.transform.rotation = block.transform.rotation * Quaternion.Euler(0, yaw, 0);
            skin.transform.localScale = Vector3.one;
            var rs = skin.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float k = width / Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.z));
            var ls = block.transform.lossyScale;
            skin.transform.localScale = new Vector3(k / ls.x, k / ls.y, k / ls.z);
            rs = skin.GetComponentsInChildren<Renderer>();
            b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            Vector3 want = centered ? target.center : new Vector3(target.center.x, target.min.y, target.center.z);
            Vector3 have = centered ? b.center : new Vector3(b.center.x, b.min.y, b.center.z);
            skin.transform.position += want - have;
            foreach (var r in rs) r.shadowCastingMode = ShadowCastingMode.On;
            return skin;
        }

        /// <summary>KayKit color set used by each district's building pieces (gates, walls, doors).</summary>
        static string KitColor(string districtTitle) =>
            districtTitle == "Interactions" ? "green" : districtTitle == "NPCs" ? "red" : districtTitle == "Pickups" ? "yellow" : "blue";

        /// <summary>Instantiates a KayKit model by name with a box collider matching its mesh.</summary>
        static GameObject KitModel(string model, string name, Transform parent, bool collider = true)
        {
            var prefab = KayKit(model);
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = name;
            if (collider)
            {
                var mf = go.GetComponentInChildren<MeshFilter>();
                var box = go.AddComponent<BoxCollider>();
                if (mf != null && mf.sharedMesh != null) { box.center = mf.sharedMesh.bounds.center; box.size = mf.sharedMesh.bounds.size; }
            }
            foreach (var r in go.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
            return go;
        }

        /// <summary>
        /// A wall, post or door built from a KayKit barrier in a district color, placed like a primitive cube
        /// (center and size in the parent's space). Long walls running along Z are turned to face the right way.
        /// This is the building style from the hand-made Interactions gate; use it instead of plain cubes.
        /// </summary>
        static GameObject KitBox(string name, Transform parent, Vector3 center, Vector3 size, string color, bool collider = true)
        {
            bool alongZ = size.z > size.x * 1.5f;
            float w = alongZ ? size.z : size.x, t = alongZ ? size.x : size.z, h = size.y;
            int wi = w <= 1.5f ? 1 : w <= 2.5f ? 2 : w <= 3.5f ? 3 : 4;
            int hi = h <= 1.4f ? 1 : h <= 2.8f ? 2 : 4;
            var go = KitModel("barrier_" + wi + "x1x" + hi + (color == "neutral" ? "" : "_" + color), name, parent, collider);
            if (go == null) return Prim(PrimitiveType.Cube, name, parent, center, size, Mat("Wall Fallback", new Color(0.9f, 0.92f, 0.97f)), collider);
            go.transform.localRotation = alongZ ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
            go.transform.localPosition = center - Vector3.up * (h / 2f);
            go.transform.localScale = new Vector3(w / wi, h / hi, t);
            return go;
        }

        static GameObject Zone(string name, Transform parent, Vector3 localPos, Vector3 size, Color tint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.AddComponent<GddTrigger>();
            var box = go.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            var rb = go.GetComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
            var glass = Prim(PrimitiveType.Cube, "Zone Preview", go.transform, Vector3.zero, size, Mat("Zone Glass " + ColorUtility.ToHtmlStringRGB(tint), new Color(tint.r, tint.g, tint.b, 0.1f), 0.9f, tint * 0.25f, true), false);
            glass.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        static GddLink Link(GameObject from, GddEntity to, GddLinkAction onActivate, GddLinkAction onExit = GddLinkAction.Nothing, float delay = 0)
        {
            var link = from.GetComponent<GddLink>();
            if (link == null) link = from.AddComponent<GddLink>();
            link.targets.Add(new GddLink.Target { target = to, whenActivated = onActivate, whenExited = onExit, delay = delay });
            return link;
        }

        /// <summary>A round creature: body, eyes, and brows (angry) or blush (friendly).</summary>
        static GameObject Critter(string name, Transform parent, Vector3 localPos, Color color, float size, bool angry, PrimitiveType shape = PrimitiveType.Sphere)
        {
            var body = Prim(shape, name, parent, localPos, Vector3.one * size, Mat("Critter " + ColorUtility.ToHtmlStringRGB(color), color, 0.6f, color * 0.1f));
            Face(body.transform, angry);
            return body;
        }

        static void Face(Transform body, bool angry)
        {
            var white = Mat("Critter Eye", Color.white, 0.9f);
            var black = Mat("Critter Pupil", new Color(0.05f, 0.05f, 0.08f), 0.9f);
            foreach (float s in new[] { -1f, 1f })
            {
                var eye = Prim(PrimitiveType.Sphere, "Eye", body, new Vector3(0.2f * s, 0.14f, 0.4f), Vector3.one * 0.28f, white, false);
                Prim(PrimitiveType.Sphere, "Pupil", eye.transform, new Vector3(0, 0, 0.32f), Vector3.one * 0.55f, black, false);
                if (angry)
                {
                    var brow = Prim(PrimitiveType.Cube, "Brow", body, new Vector3(0.2f * s, 0.36f, 0.42f), new Vector3(0.3f, 0.07f, 0.08f), black, false);
                    brow.transform.localRotation = Quaternion.Euler(0, 0, -28f * s);
                }
                else Prim(PrimitiveType.Sphere, "Blush", body, new Vector3(0.3f * s, -0.04f, 0.4f), new Vector3(0.16f, 0.08f, 0.05f), Mat("Critter Blush", new Color(1f, 0.5f, 0.65f), 0.5f, new Color(1f, 0.3f, 0.5f) * 0.3f), false);
            }
        }

        static TimelineAsset LiftTimeline(string name, Transform animated, float from, float to, float seconds, PlayableDirector director)
        {
            Folder(AnimDir);
            string path = AnimDir + "/" + name + ".playable";
            AssetDatabase.DeleteAsset(path);
            var tl = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(tl, path);
            var track = tl.CreateTrack<AnimationTrack>(null, name);
            var clip = new AnimationClip { name = name + " Motion" };
            var curve = AnimationCurve.EaseInOut(0, from, seconds, to);
            // Animate the object as a child of the Animator: Timeline only offsets the Animator's own transform,
            // so child curves play exactly the values written here.
            clip.SetCurve(animated.name, typeof(Transform), "m_LocalPosition.y", curve);
            AssetDatabase.AddObjectToAsset(clip, tl);
            var tc = track.CreateClip(clip);
            tc.start = 0;
            tc.duration = seconds;
            EditorUtility.SetDirty(tl);
            AssetDatabase.SaveAssets();

            var rigRoot = animated.parent;
            var animator = rigRoot.GetComponent<Animator>();
            if (animator == null) animator = rigRoot.gameObject.AddComponent<Animator>();
            director.playableAsset = tl;
            director.playOnAwake = false;
            director.SetGenericBinding(track, animator);
            return tl;
        }

        // ---------- districts ----------

        class District
        {
            public string title, subtitle;
            public Color accent;
            public Transform root;
            public Vector2 size;
            public Vector3 Pos(float x, float z, float y = 0) => root.TransformPoint(new Vector3(x, y, z));
            public Quaternion ReadFromPlaza => root.rotation;
        }

        static District MakeDistrict(Transform world, string title, string subtitle, Color accent, float angle, float radius, Vector2 size)
        {
            var d = new District { title = title, subtitle = subtitle, accent = accent, size = size };
            var dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            d.root = new GameObject(title).transform;
            d.root.SetParent(world, false);
            d.root.position = dir * radius;
            d.root.rotation = Quaternion.LookRotation(dir);

            var pad = FloorMat("Floor " + title, Color.Lerp(new Color(0.95f, 0.96f, 0.99f), accent, 0.08f), Color.Lerp(new Color(0.9f, 0.92f, 0.97f), accent, 0.13f), accent, 1f, 4f, 0.55f);
            Prim(PrimitiveType.Cube, "District Floor", d.root, new Vector3(0, 0.025f, 0), new Vector3(size.x, 0.05f, size.y), pad);
            var trim = Mat("Trim " + title, accent * 0.7f, 0.7f, accent * 0.9f);
            Prim(PrimitiveType.Cube, "Trim", d.root, new Vector3(0, 0.03f, -size.y / 2f), new Vector3(size.x, 0.06f, 0.12f), trim, false);

            // Path back to the plaza
            var path = FloorMat("Path " + title, new Color(0.93f, 0.94f, 0.98f), new Color(0.89f, 0.91f, 0.96f), accent, 0.5f, 2f, 0.6f);
            float pathLen = radius - size.y / 2f - 9.5f;
            if (pathLen > 0.5f)
            {
                var p = Prim(PrimitiveType.Cube, "Path", world, Vector3.zero, new Vector3(3.4f, 0.04f, pathLen), path);
                p.transform.position = dir * (9.5f + pathLen / 2f) + Vector3.up * 0.02f;
                p.transform.rotation = d.root.rotation;
            }

            // Gate: a KayKit arch over the entrance with the district's name on a banner above it
            var sign = new GameObject("Gate").transform;
            sign.SetParent(d.root, false);
            sign.localPosition = new Vector3(0, 0, -size.y / 2f - 0.6f);
            string kayColor = KitColor(title);
            var archPrefab = KayKit("arch_wide_" + kayColor);
            if (archPrefab != null)
            {
                var arch = (GameObject)PrefabUtility.InstantiatePrefab(archPrefab, sign);
                arch.transform.localPosition = new Vector3(0, -0.16f, 0.39f);
                arch.transform.localRotation = Quaternion.identity;
                arch.transform.localScale = new Vector3(1f, 1.173f, 1f);
                foreach (var mf in arch.GetComponentsInChildren<MeshFilter>()) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            }
            // Billboard: a KayKit barrier laid on its back, with a slightly bigger glowing one behind it as the rim.
            var banner = new GameObject("Banner").transform;
            banner.SetParent(sign, false);
            var board = KitModel("barrier_4x1x4_" + kayColor, "Billboard", banner, false);
            if (board != null)
            {
                board.transform.localPosition = new Vector3(0, 4.291f, 0.785f);
                board.transform.localRotation = Quaternion.Euler(270f, 0, 0);
                board.transform.localScale = new Vector3(1.169f, 0.2f, 1.252f);
                var rim = KitModel("barrier_4x1x4_" + kayColor, "Billboard Highlight", banner, false);
                rim.transform.localPosition = new Vector3(0, 4.291f, 0.683f);
                rim.transform.localRotation = Quaternion.Euler(270f, 0, 0);
                rim.transform.localScale = new Vector3(1.252f, 0.15f, 1.517f);
                foreach (var r in rim.GetComponentsInChildren<Renderer>()) r.sharedMaterial = Mat("Banner " + title, accent * 0.85f, 0.5f, accent * 0.6f);
            }
            else Prim(PrimitiveType.Cube, "Billboard", banner, new Vector3(0, 4.35f, 0), new Vector3(5.2f, 1.05f, 0.14f), Mat("Banner " + title, accent * 0.85f, 0.5f, accent * 0.6f), false);
            var titleText = Text(title.ToUpperInvariant(), sign, sign.TransformPoint(new Vector3(0, 4.461f, -0.09f)), d.ReadFromPlaza, 5.8f, Color.white, FontStyles.Normal, 5.0f, DisplayFont());
            titleText.characterSpacing = 1f;
            var sub = Text(subtitle, sign, sign.TransformPoint(new Vector3(0, 4.021f, -0.09f)), d.ReadFromPlaza, 2.5f, new Color(1f, 1f, 1f, 0.95f), FontStyles.Normal, 5.0f, Font("BoldItalic"));
            sub.characterSpacing = -5f;
            return d;
        }

        /// <summary>Pedestal plus floor label. Returns the transform blocks sit on.</summary>
        static Transform Station(District d, string label, float x, float z, bool flat = false)
        {
            var s = new GameObject(label).transform;
            s.SetParent(d.root, false);
            s.localPosition = new Vector3(x, 0.05f, z);
            s.localRotation = Quaternion.Euler(0, 180, 0);
            float top = flat ? 0.02f : 0.32f;
            var ped = FloorMat("Pedestal " + d.title, Color.white, new Color(0.93f, 0.94f, 0.98f), d.accent, 0.5f, 2f, 0.7f, 0.06f);
            Prim(PrimitiveType.Cylinder, "Pedestal", s, new Vector3(0, top / 2f, 0), new Vector3(2.6f, Mathf.Max(0.01f, top / 2f), 2.6f), ped, !flat);
            Prim(PrimitiveType.Cylinder, "Rim", s, new Vector3(0, 0.012f, 0), new Vector3(2.95f, 0.012f, 2.95f), Mat("Rim " + d.title, Color.black, 0.8f, d.accent * 2f), false);
            Sign(d, label, d.accent, x, z);
            var top0 = new GameObject("Top").transform;
            top0.SetParent(s, false);
            top0.localPosition = new Vector3(0, top, 0);
            return top0;
        }

        /// <summary>
        /// Small upright sign on a post, front-right of a station, facing the plaza.
        /// Replaces floor text, which was hard to read at a glance.
        /// </summary>
        static void Sign(District d, string text, Color accent, float x, float z)
        {
            var root = new GameObject("Sign - " + text).transform;
            root.SetParent(d.root, false);
            root.localPosition = new Vector3(x + 1.2f, 0, z - 1.9f);
            var post = Mat("Sign Post", new Color(0.92f, 0.94f, 0.98f), 0.6f);
            Prim(PrimitiveType.Cylinder, "Post", root, new Vector3(0, 0.28f, 0.04f), new Vector3(0.05f, 0.28f, 0.05f), post, false);
            // Condensed Lato Bold Italic; the board is cut to the text's real width.
            var t = Text(text, root, root.TransformPoint(new Vector3(0, 0.66f, -0.03f)), d.ReadFromPlaza, 1.5f, new Color(0.12f, 0.14f, 0.22f), FontStyles.Normal, 4f, Font("BoldItalic"));
            t.characterSpacing = -5f;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            float width = Mathf.Clamp(t.GetPreferredValues(text).x + 0.22f, 0.7f, 2.4f);
            t.enableAutoSizing = true; t.fontSizeMin = 0.9f; t.fontSizeMax = 1.5f;
            t.rectTransform.sizeDelta = new Vector2(width - 0.14f, 0.26f);
            Prim(PrimitiveType.Cube, "Board", root, new Vector3(0, 0.66f, 0), new Vector3(width, 0.28f, 0.04f), Mat("Sign Board", new Color(0.98f, 0.98f, 1f), 0.5f), false);
            Prim(PrimitiveType.Cube, "Stripe", root, new Vector3(0, 0.82f, -0.004f), new Vector3(width, 0.04f, 0.05f), Mat("Stripe " + ColorUtility.ToHtmlStringRGB(accent), accent * 0.85f, 0.5f, accent * 0.5f), false);
        }

        static GameObject Place(string kind, Transform top, float lift = 0.35f, Color? tint = null)
        {
            var b = Block(kind, top);
            if (b == null) return null;
            b.transform.localRotation = Quaternion.identity;
            var r = b.GetComponent<Renderer>();
            float half = r != null ? r.bounds.extents.y : 0.3f;
            b.transform.localPosition = new Vector3(0, half + lift, 0);
            if (r != null && tint.HasValue) r.sharedMaterial = Mat("Block " + kind, tint.Value * 0.8f, 0.75f, tint.Value * 0.2f);
            return b;
        }

        static void TallTrigger(GameObject flatBlock, float worldHeight)
        {
            // Thin pads keep a tall trigger so the player's capsule always registers.
            var box = flatBlock.GetComponent<BoxCollider>();
            float sy = Mathf.Max(0.001f, flatBlock.transform.lossyScale.y);
            box.size = new Vector3(1, worldHeight / sy, 1);
            box.center = new Vector3(0, worldHeight / sy / 2f - 0.5f, 0);
        }

        // ---------- the scene ----------

        [MenuItem("Tools/GDD 110/Juice/Build Block Showcase Scene", false, 201)]
        public static void BuildShowcase()
        {
            if (EditorApplication.isPlaying) { Debug.LogError("Stop Play mode first."); return; }
            var settings = BuildAssets();
            GDD110.Editor.GddCourseMenus.GenerateLibrary();
            GDD110.Editor.GddTriggerableMenus.DefaultRules();
            var stay = GDD110.Editor.GddTriggerableMenus.Rule(GddDropRule.Mode.StayOnGround, "Stay On Ground");
            var back = GDD110.Editor.GddTriggerableMenus.Rule(GddDropRule.Mode.ReturnRightAway, "Return Right Away");
            var wait = GDD110.Editor.GddTriggerableMenus.Rule(GddDropRule.Mode.WaitThenReturn, "Wait 6s Then Return");
            System.Func<string, Color> accent = n => settings.Find(n) != null ? settings.Find(n).accent : Color.white;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            foreach (var cam in Object.FindObjectsByType<Camera>()) Object.DestroyImmediate(cam.gameObject);

            // Daylight training ground
            var sun = Object.FindAnyObjectByType<Light>();
            sun.transform.rotation = Quaternion.Euler(50f, 35f, 0f);
            sun.color = new Color(1f, 0.96f, 0.9f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.65f;
            RenderSettings.skybox = Sky();
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.1f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.007f;
            RenderSettings.fogColor = new Color(0.8f, 0.86f, 0.96f);

            var world = new GameObject("Showcase World").transform;
            Prim(PrimitiveType.Cylinder, "Ground", world, new Vector3(0, -0.1f, 0), new Vector3(150, 0.1f, 150),
                FloorMat("Floor Ground", new Color(0.86f, 0.88f, 0.94f), new Color(0.82f, 0.85f, 0.92f), new Color(0.62f, 0.7f, 0.92f), 1f, 5f, 0.15f));
            Prim(PrimitiveType.Cylinder, "Plaza", world, new Vector3(0, 0.02f, 0), new Vector3(19, 0.02f, 19),
                FloorMat("Floor Plaza", new Color(0.98f, 0.98f, 1f), new Color(0.94f, 0.95f, 0.99f), new Color(0.25f, 0.75f, 1f), 1f, 4f, 0.5f));
            Prim(PrimitiveType.Cylinder, "Plaza Rim", world, new Vector3(0, 0.01f, 0), new Vector3(19.6f, 0.02f, 19.6f), Mat("Plaza Rim", Color.black, 0.8f, new Color(0.25f, 0.75f, 1f) * 1.6f), false);
            Text("GDD 110 <color=#2fa8ff>Block Showcase</color>", world, new Vector3(0, 4.4f, 8.5f), Quaternion.identity, 9, new Color(0.15f, 0.18f, 0.28f), FontStyles.Normal, 20, DisplayFont());
            Text("Five sections: Touchables, Interactions, Automations, NPCs, Pickups. Walk up to anything for a guide.", world, new Vector3(0, 3.4f, 8.5f), Quaternion.identity, 3.6f, new Color(0.3f, 0.35f, 0.48f), FontStyles.Normal, 20);

            const float R = 31f;
            var size = new Vector2(26, 18);

            // 1. Touchables: walk into it or step on it
            var touch = MakeDistrict(world, "Touchables", "Walk into it or step on it", Cyan, 0, R, size);
            {
                // Event Trigger -> Toggle Objects (pillars rise while you stand in the zone)
                var zone = Zone("Event Trigger", touch.root, new Vector3(-9f, 1f, -3.5f), new Vector3(2.4f, 2f, 2.4f), Cyan);
                Sign(touch, "Event Trigger", Cyan, -9f, -3.5f);
                var pillars = new GameObject("Toggle Objects (pillars)");
                pillars.transform.SetParent(touch.root, false);
                pillars.transform.localPosition = new Vector3(-9f, 0, 4.5f);
                var toggle = pillars.AddComponent<GddToggleObjects>();
                toggle.stagger = 0.12f;
                for (int i = 0; i < 3; i++)
                {
                    var p = KitBox("Pillar " + (i + 1), pillars.transform, new Vector3((i - 1) * 1.4f, 1f + i * 0.4f, 0), new Vector3(0.9f, 2f + i * 0.8f, 0.9f), "green");
                    toggle.objects.Add(p);
                }
                Sign(touch, "Toggle Objects", Green, -9f, 4.5f);
                Link(zone, toggle, GddLinkAction.TurnOn, GddLinkAction.TurnOff);

                // ...and a cannon that pops up while you stand in the zone, then fires when you leave
                var cannonPrefab = KayKit("cannon_base_blue");
                if (cannonPrefab != null)
                {
                    var lift = new GameObject("Mover (cannon pops up)");
                    lift.transform.SetParent(touch.root, false);
                    lift.transform.localPosition = new Vector3(-12f, -2.3f, 0.5f);
                    lift.transform.localRotation = Quaternion.Euler(0, 20f, 0);
                    var cannon = (GameObject)PrefabUtility.InstantiatePrefab(cannonPrefab, lift.transform);
                    cannon.transform.localScale = Vector3.one * 0.62f;
                    var pop = lift.AddComponent<GddMover>();
                    pop.moveBy = new Vector3(0, 2.32f, 0);
                    pop.duration = 0.45f;
                    var barrel = cannon.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name.Contains("barrel"));
                    var muzzle = new GameObject("Muzzle").transform;
                    muzzle.SetParent(barrel != null ? barrel : cannon.transform, false);
                    var br = (barrel != null ? barrel : cannon.transform).GetComponentInChildren<Renderer>();
                    muzzle.position = br != null ? br.bounds.center + lift.transform.forward * br.bounds.extents.z : cannon.transform.position + Vector3.up;
                    muzzle.rotation = lift.transform.rotation;
                    var shooter = lift.AddComponent<GddShooter>();
                    shooter.friendlyName = "Cannon";
                    shooter.projectile = KayKit("cannon_bullet");
                    shooter.muzzle = muzzle;
                    shooter.recoilPart = barrel;
                    shooter.speed = 15f; shooter.arc = 0.45f; shooter.lifetime = 3.5f;
                    var tl = zone.GetComponent<GddLink>();
                    tl.targets.Add(new GddLink.Target { target = pop, whenActivated = GddLinkAction.TurnOn, whenExited = GddLinkAction.Nothing });
                    tl.targets.Add(new GddLink.Target { target = shooter, whenActivated = GddLinkAction.Nothing, whenExited = GddLinkAction.Activate });
                    tl.targets.Add(new GddLink.Target { target = pop, whenActivated = GddLinkAction.Nothing, whenExited = GddLinkAction.TurnOff, delay = 1.2f });
                    Sign(touch, "Cannon: up on enter, fires on leave", Cyan, -12f, 1.5f);
                }

                // Door: a zone on both sides opens the door, leaving closes it
                var doorRoot = new GameObject("Door");
                doorRoot.transform.SetParent(touch.root, false);
                doorRoot.transform.localPosition = new Vector3(0, 0, 3.5f);
                string kit = KitColor(touch.title);
                KitBox("Wall L", doorRoot.transform, new Vector3(-2.3f, 1.6f, 0), new Vector3(2.2f, 3.2f, 0.4f), kit);
                KitBox("Wall R", doorRoot.transform, new Vector3(2.3f, 1.6f, 0), new Vector3(2.2f, 3.2f, 0.4f), kit);
                KitBox("Lintel", doorRoot.transform, new Vector3(0, 3.4f, 0), new Vector3(6.8f, 0.4f, 0.4f), kit);
                var door = KitBox("Mover (door)", doorRoot.transform, new Vector3(0, 1.6f, 0), new Vector3(2.4f, 3.2f, 0.25f), "green");
                var mover = door.AddComponent<GddMover>();
                mover.moveBy = new Vector3(0, 3.1f, 0);
                mover.duration = 0.55f;
                var doorZone = Zone("Door Trigger", doorRoot.transform, new Vector3(0, 1f, 0), new Vector3(2.4f, 2f, 6f), Cyan);
                Link(doorZone, mover, GddLinkAction.TurnOn, GddLinkAction.TurnOff);
                Sign(touch, "Trigger -> Mover door", Cyan, 0f, 0.2f);

                // Security room: step inside and the view cuts to a grainy wall camera
                var room = new GameObject("Security Room");
                room.transform.SetParent(touch.root, false);
                room.transform.localPosition = new Vector3(9f, 0, 4f);
                KitBox("Back", room.transform, new Vector3(0, 1.5f, 2.2f), new Vector3(4.6f, 3f, 0.25f), kit);
                KitBox("Left", room.transform, new Vector3(-2.2f, 1.5f, 0), new Vector3(0.25f, 3f, 4.6f), kit);
                KitBox("Right", room.transform, new Vector3(2.2f, 1.5f, 0), new Vector3(0.25f, 3f, 4.6f), kit);
                KitBox("Front L", room.transform, new Vector3(-1.5f, 1.5f, -2.2f), new Vector3(1.6f, 3f, 0.25f), kit);
                KitBox("Front R", room.transform, new Vector3(1.5f, 1.5f, -2.2f), new Vector3(1.6f, 3f, 0.25f), kit);
                var roof = KitModel("platform_4x4x1_" + kit, "Roof", room.transform);
                if (roof != null) { roof.transform.localPosition = new Vector3(0, 3.0f, 0); roof.transform.localScale = new Vector3(4.7f / 4f, 0.2f, 4.7f / 4f); }
                else Prim(PrimitiveType.Cube, "Roof", room.transform, new Vector3(0, 3.1f, 0), new Vector3(4.7f, 0.2f, 4.7f), Mat("Wall Fallback", new Color(0.9f, 0.92f, 0.97f)));
                var camPos = room.transform.TransformPoint(new Vector3(1.75f, 2.6f, 1.8f));
                var camProp = Prim(PrimitiveType.Cube, "Security Camera Prop", room.transform, new Vector3(1.75f, 2.65f, 1.85f), new Vector3(0.35f, 0.25f, 0.5f), Mat("Camera Body", new Color(0.15f, 0.16f, 0.2f), 0.7f), false);
                Prim(PrimitiveType.Sphere, "REC Light", camProp.transform, new Vector3(0.3f, 0.3f, -0.4f), Vector3.one * 0.25f, Mat("REC Light", Color.red, 0.5f, Color.red * 4f), false);
                var switchGo = new GameObject("Camera Switch");
                switchGo.transform.SetParent(room.transform, false);
                switchGo.transform.localPosition = new Vector3(0, 1f, 0);
                var camSwitch = switchGo.AddComponent<GddCameraSwitch>();
                camSwitch.feedLabel = "CAM 02  SECURITY";
                var secCam = new GameObject("Security CinemachineCamera", typeof(CinemachineCamera));
                secCam.transform.SetParent(switchGo.transform, false);
                secCam.transform.position = camPos + room.transform.TransformDirection(new Vector3(-0.1f, -0.15f, -0.3f));
                secCam.transform.rotation = Quaternion.LookRotation(room.transform.TransformPoint(new Vector3(-0.6f, 0.6f, -1.6f)) - secCam.transform.position);
                var lens = secCam.GetComponent<CinemachineCamera>().Lens;
                lens.FieldOfView = 78f;
                secCam.GetComponent<CinemachineCamera>().Lens = lens;
                secCam.SetActive(false);
                camSwitch.cameraToUse = secCam.GetComponent<CinemachineCamera>();
                var roomZone = Zone("Room Trigger", room.transform, new Vector3(0, 1.2f, 0.1f), new Vector3(4f, 2.4f, 4f), Cyan);
                Link(roomZone, camSwitch, GddLinkAction.TurnOn, GddLinkAction.TurnOff);
                Sign(touch, "Security room: step inside", Cyan, 9f, 0.6f);

                var bounce = Place("Bounce Pad", Station(touch, "Bounce Pad", -4.5f, -4.5f, true), 0.02f);
                bounce.GetComponent<Renderer>().sharedMaterial = Mat("Bounce Glow", new Color(0.1f, 0.3f, 0.35f), 0.7f, new Color(0.2f, 0.9f, 1f) * 1.3f);
                bounce.transform.localPosition = new Vector3(0, 0.1f, 0);
                if (bounce.transform.Find("Model (spring_pad_blue)") == null)   // otherwise the prefab already carries hand-tuned size and trigger
                {
                    TallTrigger(bounce, 1.2f);
                    var bls = bounce.transform.localScale;
                    bounce.transform.localScale = new Vector3(2.3f, bls.y, 2.3f);
                }
                Skin(bounce, "spring_pad_blue", 2.3f);

                var hazard = Place("Hazard", Station(touch, "Hazard (hurts every 0.4 s)", 0f, -5f, true), 0.02f);
                hazard.transform.localScale = new Vector3(2.3f, 0.1f, 2.3f);
                hazard.transform.localPosition = new Vector3(0, 0.06f, 0);
                hazard.GetComponent<Renderer>().sharedMaterial = Mat("Hazard Glow", new Color(0.35f, 0.05f, 0.05f), 0.6f, new Color(1f, 0.15f, 0.05f) * 1.4f);
                TallTrigger(hazard, 1.4f);
                var hz = hazard.GetComponent<GddContactEffect>();
                hz.amount = 20; hz.repeatEvery = 0.4f;

                var tele = Place("Teleporter", Station(touch, "Teleporter", 4.5f, -4.5f, true), 0f);
                tele.GetComponent<Renderer>().sharedMaterial = Mat("Teleporter Glass", new Color(0.55f, 0.35f, 1f, 0.45f), 0.95f, Violet * 0.5f, true);
                var dest = tele.GetComponent<GddContactEffect>().destination;
                if (dest != null)
                {
                    var landing = touch.root.position.normalized * 6f;
                    dest.position = landing + Vector3.up * 0.2f;
                    dest.rotation = Quaternion.LookRotation(touch.root.forward);
                    Prim(PrimitiveType.Cylinder, "Arrival Pad", world, landing + Vector3.up * 0.04f, new Vector3(1.8f, 0.01f, 1.8f), Mat("Arrival Pad", Color.black, 0.8f, Violet * 2f), false);
                }

                var portal = Place("Next Scene Portal", Station(touch, "Next Scene Portal", 9f, -5f, true), 0f);
                portal.transform.localScale = new Vector3(1.8f, 2.6f, 0.5f);
                portal.transform.localPosition = new Vector3(0, 1.32f, 0);
                portal.GetComponent<Renderer>().sharedMaterial = Mat("Portal Glass", new Color(0.35f, 0.2f, 1f, 0.4f), 0.95f, new Color(0.5f, 0.38f, 1f) * 0.6f, true);
            }

            Spread(touch, 1.18f);

            // 2. Interactions: walk up and press E
            var key = MakeDistrict(world, "Interactions", "Walk up and press E", Mint, 72, R, size);
            {
                var b1 = Place("Interact Button", Station(key, "Button -> Spawner", -8f, -3.5f), 0.35f, Mint);
                b1.GetComponent<GddInteractable>().actionLabel = "Press";
                b1.GetComponent<GddEntity>().friendlyName = "Spawn Button";
                Skin(b1, "button_base_green", 1.2f);
                var sp = Place("Spawner", Station(key, "Spawner (on demand)", -8f, 3f), 0.35f, Orange);
                var s = sp.GetComponent<GddSpawn>();
                s.automatic = false; s.maximumAlive = 6; s.lifetime = 10;
                Link(b1, s, GddLinkAction.Activate);

                var lever = Place("Interact Button", Station(key, "Lever -> Timeline lift", 0f, -3.5f), 0.35f, Mint);
                lever.GetComponent<GddInteractable>().actionLabel = "Pull";
                lever.GetComponent<GddEntity>().friendlyName = "Lift Lever";
                Skin(lever, "lever_floor_base_green", 0.9f, false, 90f);
                var liftRoot = new GameObject("Play Timeline (lift)");
                liftRoot.transform.SetParent(key.root, false);
                liftRoot.transform.localPosition = new Vector3(0, 0.05f, 3.5f);
                var platform = KitModel("platform_4x4x1_" + KitColor(key.title), "Lift Platform", liftRoot.transform);
                platform.transform.localScale = new Vector3(0.65f, 0.3f, 0.65f);
                var dir1 = liftRoot.AddComponent<PlayableDirector>();
                LiftTimeline("Lift Timeline", platform.transform, 0f, 3.05f, 2f, dir1);
                var liftT = liftRoot.AddComponent<GddPlayTimeline>();
                liftT.director = dir1;
                liftT.playAfterReset = false;
                Link(lever, liftT, GddLinkAction.Toggle);

                var clock = Place("Interact Button", Station(key, "Button -> gate (Reset)", 8f, -4.5f), 0.35f, Mint);
                clock.GetComponent<GddInteractable>().actionLabel = "Start";
                clock.GetComponent<GddEntity>().friendlyName = "Clock Button";
                Skin(clock, "button_base_yellow", 1.2f);
                var gateRoot = new GameObject("Play Timeline (gate)");
                gateRoot.transform.SetParent(key.root, false);
                gateRoot.transform.localPosition = new Vector3(8f, 0.05f, 1f);
                // Pocket walls and gate: KayKit barriers, placed by hand in the editor and copied here.
                void Pocket(string n, Vector3 pos, float yaw)
                {
                    var w = KitModel("barrier_4x1x4_" + KitColor(key.title), n, gateRoot.transform);
                    if (w == null) return;
                    w.transform.localPosition = pos;
                    w.transform.localRotation = Quaternion.Euler(0, yaw, 0);
                    w.transform.localScale = new Vector3(0.841f, 0.841f, 0.693f);
                }
                Pocket("Pocket R", new Vector3(1.77f, -0.11f, 1.53f), 270f);
                Pocket("Pocket L", new Vector3(-1.775f, -0.11f, 1.527f), 270f);
                Pocket("Pocket Back", new Vector3(-0.118f, -0.11f, 2.871f), 180f);
                var gate = KitModel("barrier_4x1x4_yellow", "Gate", gateRoot.transform)
                    ?? Prim(PrimitiveType.Cube, "Gate", gateRoot.transform, Vector3.zero, new Vector3(2.9f, 2.4f, 0.25f), Mat("Gate", new Color(1f, 0.45f, 0.3f), 0.7f));
                gate.transform.localPosition = new Vector3(0, 1.99f, 0.08f);
                gate.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                gate.transform.localScale = new Vector3(0.739f, 0.739f, 0.609f);
                var dir2 = gateRoot.AddComponent<PlayableDirector>();
                LiftTimeline("Gate Timeline", gate.transform, 1.99f, 0f, 5f, dir2);   // the barrier's pivot is its bottom edge
                var gateT = gateRoot.AddComponent<GddPlayTimeline>();
                gateT.director = dir2;
                gateT.startOn = true;
                gateT.playAfterReset = true;
                Link(clock, gateT, GddLinkAction.Reset);
                // A second button inside the pocket reopens the gate, so nobody gets shut in
                var inside = Block("Interact Button", gateRoot.transform);
                inside.transform.localPosition = new Vector3(0.9f, 0.25f, 2.6f);
                inside.transform.localScale = Vector3.one * 0.6f;
                inside.GetComponent<GddInteractable>().actionLabel = "Open";
                inside.GetComponent<GddEntity>().friendlyName = "Gate (from inside)";
                Link(inside, gateT, GddLinkAction.Reset);
                Skin(inside, "button_base_yellow", 0.8f);
                var prize = Block("Score Collectible", gateRoot.transform);
                prize.transform.localPosition = new Vector3(0, 0.8f, 2f);
                prize.transform.localScale = new Vector3(0.55f, 0.55f, 0.14f);
                prize.GetComponent<Renderer>().sharedMaterial = Mat("Block Coin", new Color(1f, 0.72f, 0.15f), 0.9f, new Color(1f, 0.55f, 0.1f) * 0.45f);
                Sign(key, "Beat the clock: run through before it shuts", Mint, 8f, -1.6f);
            }

            // 3. Automations: fires on its own
            var auto = MakeDistrict(world, "Automations", "Fires on its own, on a beat", Blue, 144, R, size);
            {
                var t1 = Place("Timer", Station(auto, "Timer -> lamp", -8f, -3.5f), 0.35f, Blue);
                t1.GetComponent<GddTimer>().seconds = 1.5f;
                var lamp = new GameObject("Toggle Objects (lamp)");
                lamp.transform.SetParent(auto.root, false);
                lamp.transform.localPosition = new Vector3(-8f, 0, 3f);
                Prim(PrimitiveType.Cylinder, "Lamp Post", lamp.transform, new Vector3(0, 1.1f, 0), new Vector3(0.15f, 1.1f, 0.15f), Mat("Sign Post", new Color(0.92f, 0.94f, 0.98f), 0.6f));
                var bulb = Prim(PrimitiveType.Sphere, "Bulb", lamp.transform, new Vector3(0, 2.45f, 0), Vector3.one * 0.7f, Mat("Bulb", new Color(1f, 0.95f, 0.7f), 0.9f, new Color(1f, 0.85f, 0.4f) * 4f), false);
                var light = new GameObject("Light").AddComponent<Light>();
                light.transform.SetParent(bulb.transform, false);
                light.type = LightType.Point; light.range = 6; light.intensity = 3; light.color = new Color(1f, 0.85f, 0.5f);
                var lampToggle = lamp.AddComponent<GddToggleObjects>();
                lampToggle.objects.Add(bulb);
                Link(t1, lampToggle, GddLinkAction.Toggle);

                var t2 = Place("Timer", Station(auto, "Timer -> 3 movers", 0f, -3.5f), 0.35f, Blue);
                t2.GetComponent<GddTimer>().seconds = 2.5f;
                for (int i = 0; i < 3; i++)
                {
                    var cube = KitModel("platform_decorative_1x1x1_blue", "Mover cube " + (i + 1), auto.root);
                    cube.transform.localPosition = new Vector3((i - 1) * 1.5f, 0.05f, 3f);
                    var m = cube.AddComponent<GddMover>();
                    m.moveBy = new Vector3(0, 1.4f, 0);
                    m.rotateBy = new Vector3(0, 90, 0);
                    m.duration = 0.28f;
                    m.autoReturnAfter = 0.35f;
                    Link(t2, m, GddLinkAction.TurnOn, GddLinkAction.Nothing, i * 0.18f);
                }

                var asp = Place("Spawner", Station(auto, "Spawner (automatic)", 8f, -3.5f), 0.35f, Orange);
                var aspawn = asp.GetComponent<GddSpawn>();
                aspawn.automatic = true; aspawn.interval = 3f; aspawn.maximumAlive = 4; aspawn.lifetime = 9f;

                var rot = Place("Rotating Object", Station(auto, "Rotating Object", 8f, 3.5f), 0.6f, new Color(0.7f, 0.8f, 1f));
                var motion = rot.GetComponent<GddMotion>();
                motion.rotationPerSecond = new Vector3(20, 70, 0);
                motion.bobHeight = 0.25f; motion.bobSpeed = 1.5f;
            }

            // 4. NPCs: the creature zoo (dialog characters join in phase 4)
            var npcs = MakeDistrict(world, "NPCs", "Creatures and characters", Pink, 216, R + 6f, new Vector2(36, 30));
            {
                BuildPen(npcs, "Friendly Pen", -9.5f, Pink, false, 5f);
                BuildPen(npcs, "Angry Pen", 9.5f, new Color(1f, 0.25f, 0.3f), true, 4f);
                BuildTalkers(npcs);
                BuildArmor(npcs, 16f, -6.5f);
            }

            Spread(npcs, 1.1f);

            // 5. Pickups: press E to pick up, touch to collect
            var pick = MakeDistrict(world, "Pickups", "E to pick up, click to use, Q to drop", Gold, 288, R + 4f, new Vector2(26, 26));
            {
                void Item(string kind, string label, float x, float z, string friendly, GddDropRule rule, Color tint)
                {
                    var b = Place(kind, Station(pick, label, x, z), 0.4f, tint);
                    b.GetComponent<GddEntity>().friendlyName = friendly;
                    var r = b.AddComponent<GddPickupReturn>();
                    r.rule = rule;
                }
                // Bomb: click to throw, it blows up on whatever it hits, then reappears on its pedestal
                var bomb = Place("Hand Pickup", Station(pick, "Bomb: click to throw", -9f, -8f), 0.4f, Gold);
                bomb.GetComponent<GddEntity>().friendlyName = "Bomb";
                Skin(bomb, "bomb", 0.75f, true);
                var bombBody = bomb.GetComponent<Rigidbody>();
                if (bombBody != null) { bombBody.isKinematic = true; bombBody.useGravity = false; }
                var throwable = bomb.AddComponent<GddThrowable>();
                throwable.friendlyName = "Bomb";
                throwable.explosionEffect = KayKitVfx("VFX_Bomb_Explosion");
                throwable.explosionSound = Load<AudioClip>("Assets/GDD 110 Project Files/Wesson Assets/Audio/SFX/SFX_Bomb_Explosion.wav");
                Item("Hat Pickup", "Hat Pickup (returns right away)", -3f, -8f, "Hat", back, Pink);
                Item("Chest Pickup", "Chest Pickup (waits 6 s)", 3f, -8f, "Power Badge", wait, accent("Chest Pickup"));
                var badge = pick.root.Find("Chest Pickup (waits 6 s)/Top").GetChild(0).gameObject;
                Skin(badge, "power_blue", 0.55f, true);
                Skin(Place("Health Collectible", Station(pick, "Health Collectible", 9f, -8f), 0.4f, Green), "heart_red", 0.8f, true);
                Item("Speed Boots", "Speed Boots", -9f, -0.5f, "Speed Boots", wait, accent("Speed Boots"));
                Item("Jump Boots", "Jump Boots", -3f, -0.5f, "Jump Boots", wait, accent("Jump Boots"));
                foreach (var (label, style, tint) in new[] { ("Speed Boots", GddBootsJuice.Style.Speed, new Color(0.3f, 0.85f, 1f)), ("Jump Boots", GddBootsJuice.Style.Jump, new Color(0.6f, 1f, 0.35f)) })
                {
                    var boots = pick.root.Find(label + "/Top").GetChild(0).gameObject;
                    var bp = boots.GetComponent<GddPickup>();
                    bp.attachment = GddSocket.RightFoot;   // the left one is mirrored automatically
                    bp.mirrorToOtherFoot = true;
                    var juice = boots.AddComponent<GddBootsJuice>();
                    juice.style = style; juice.color = tint;
                    juice.chargeEffect = KayKitVfx("HCFX_Appear_02_Start");
                    juice.motionEffect = KayKitVfx("VFX_Dash");
                    juice.chargeSound = Load<AudioClip>("Assets/GDD 110 Project Files/Wesson Assets/Audio/SFX/FeelBounceCharge.wav");
                    juice.motionSound = juice.chargeSound;
                }
                Item("Blaster Pickup", "Blaster (waits 6 s)", 3f, -0.5f, "Blaster", wait, accent("Blaster Pickup"));
                BuildCollectibles(pick);
                var coinTop = Station(pick, "Score Collectibles", 9f, -0.5f, true);
                for (int c = 0; c < 6; c++)
                {
                    var coin = Block("Score Collectible", coinTop);
                    float a = (c - 2.5f) * 0.42f;
                    coin.transform.localPosition = new Vector3(Mathf.Sin(a) * 1.5f, 0.6f, Mathf.Cos(a) * 1.5f - 1.3f);
                    coin.transform.localScale = new Vector3(0.45f, 0.45f, 0.12f);
                    coin.GetComponent<Renderer>().sharedMaterial = Mat("Block Coin", new Color(1f, 0.72f, 0.15f), 0.9f, new Color(1f, 0.55f, 0.1f) * 0.45f);
                    Skin(coin, "star_yellow", 0.6f, true);
                }
            }

            Spread(pick, 1.14f);

            // Town radio: the song's vocals, heard only near it, locked in sync with the instrumental level music
            {
                var radio = new GameObject("Synced Radio (vocals)");
                radio.transform.SetParent(world, false);
                radio.transform.position = new Vector3(4.5f, 0, -4f);
                radio.transform.rotation = Quaternion.LookRotation(new Vector3(-4.5f, 0, 4f));
                var standPrefab = KayKit("platform_decorative_1x1x1_yellow");
                if (standPrefab != null) { var stand = (GameObject)PrefabUtility.InstantiatePrefab(standPrefab, radio.transform); stand.transform.localPosition = Vector3.zero; stand.transform.localRotation = Quaternion.identity; stand.transform.localScale = new Vector3(1.1f, 0.7f, 0.8f); }
                var shell = Mat("Radio Shell", new Color(0.95f, 0.35f, 0.4f), 0.75f);
                var dark = Mat("Radio Speaker", new Color(0.1f, 0.1f, 0.14f), 0.6f);
                var body = Prim(PrimitiveType.Cube, "Boombox", radio.transform, new Vector3(0, 1.05f, 0), new Vector3(1.2f, 0.6f, 0.38f), shell);
                var cones = new List<Transform>();
                foreach (float x in new[] { -0.33f, 0.33f })
                {
                    var cone = Prim(PrimitiveType.Cylinder, "Speaker", body.transform, new Vector3(x / 1.2f, -0.05f, 0.52f), new Vector3(0.3f, 0.04f, 0.7f), dark, false);
                    cone.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    cones.Add(cone.transform);
                }
                Prim(PrimitiveType.Cube, "Handle", radio.transform, new Vector3(0, 1.45f, 0), new Vector3(0.8f, 0.07f, 0.08f), dark, false);
                Prim(PrimitiveType.Cylinder, "Antenna", radio.transform, new Vector3(0.45f, 1.7f, 0.05f), new Vector3(0.025f, 0.35f, 0.025f), dark, false);
                var r = radio.AddComponent<GddSyncedRadio>();
                r.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GDD 110 Project Files/Wesson Assets/Audio/Music/Feel the beat the city's waking up (Remastered) (Lead Vocal) (1).mp3");
                r.syncWith = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/GDD 110 Project Files/Wesson Assets/Audio/Music/Feel the beat the city's waking up (Remastered) (Without Lead Vocal) (1).mp3");
                r.fullVolumeWithin = 2f; r.silentBeyond = 8f;
                r.pulseWithSound = cones.ToArray();
                var note = radio.AddComponent<GddPopupMessage>();
                note.title = "Synced Radio";
                note.message = "Plays the song's vocals in sync with the instrumental level music. Walk away and the singer fades out.";
                note.nearDistance = 2.6f; note.height = 2.4f; note.accent = new Color(0.95f, 0.35f, 0.4f);
            }

            // Body swap booth: a stand-in body you can wear (swap the stand-in for your own Mixamo character)
            {
                const string standIn = "Assets/GDD 110 Project Files/Wesson Assets/Models/Player/Characters/Stand-In Body.fbx";
                if (AssetDatabase.LoadMainAssetAtPath(standIn) == null)
                {
                    Folder("Assets/GDD 110 Project Files/Wesson Assets/Models/Player/Characters");
                    AssetDatabase.CopyAsset("Assets/GDD 110 Project Files/Wesson Assets/Models/Course Levels/spell caster stand.fbx", standIn);
                }
                var imp = AssetImporter.GetAtPath(standIn) as ModelImporter;
                if (imp != null) GDD110.Editor.GddCharacterSwapTool.PrepareImport(imp, standIn);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(standIn);
                var booth = new GameObject("Body Swap Booth");
                booth.transform.SetParent(world, false);
                booth.transform.position = new Vector3(-4.5f, 0, -4f);
                booth.transform.rotation = Quaternion.LookRotation(new Vector3(4.5f, 0, 4f));
                var padPrefab = KayKit("platform_decorative_2x2x2_blue");
                if (padPrefab != null) { var pad = (GameObject)PrefabUtility.InstantiatePrefab(padPrefab, booth.transform); pad.transform.localPosition = Vector3.zero; pad.transform.localScale = new Vector3(1f, 0.12f, 1f); }
                if (model != null)
                {
                    var body = (GameObject)PrefabUtility.InstantiatePrefab(model, booth.transform);
                    body.name = "Stand-In Body (Wearable)";
                    body.transform.localPosition = new Vector3(0, 0.25f, 0);
                    body.transform.localRotation = Quaternion.identity;
                    var rs = body.GetComponentsInChildren<Renderer>();
                    if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); if (b.size.y > 0.1f) body.transform.localScale = Vector3.one * (1.8f / b.size.y); }
                    var wear = body.AddComponent<GddWearableBody>();
                    wear.friendlyName = "Stand-In Body";
                }
            }

            // Popup Message kiosk on the open side of the plaza
            {
                var kiosk = new GameObject("Popup Message (kiosk)");
                kiosk.transform.SetParent(world, false);
                kiosk.transform.position = new Vector3(0, 0, -7.5f);
                kiosk.transform.rotation = Quaternion.Euler(0, 0, 0);
                var flagPrefab = KayKit("signage_arrow_stand_blue");
                if (flagPrefab != null) { var stand = (GameObject)PrefabUtility.InstantiatePrefab(flagPrefab, kiosk.transform); stand.transform.localPosition = Vector3.zero; }
                var popup = kiosk.AddComponent<GddPopupMessage>();
                popup.friendlyName = "Welcome Kiosk";
                popup.title = "Popup Message";
                popup.message = "Walk close and a card like this appears. Students: Add Component > GDD 110 > Triggerables > Popup Message. Switch it on from a Link to show it anywhere.";
                popup.nearDistance = 3.5f;
                popup.height = 2.6f;
                var col = kiosk.AddComponent<BoxCollider>();
                col.isTrigger = true; col.center = new Vector3(0, 1f, 0); col.size = new Vector3(1.2f, 2f, 1.2f);
            }

            BuildHazardRun(world, Red);
            BuildSecrets(world, touch, key, pick);
            BuildEdge(world);

            // Player
            var rig = AssetDatabase.LoadAssetAtPath<GameObject>(Toolkit + "/Course Player Rig.prefab");
            if (rig != null)
            {
                var actions = rig.GetComponentInChildren<GddSceneActions>(true);
                if (actions != null && actions.delay < 1f) { actions.delay = 1.2f; PrefabUtility.SavePrefabAsset(rig); }
                var player = (GameObject)PrefabUtility.InstantiatePrefab(rig);
                var actor = player.GetComponentInChildren<GddActor>();
                if (actor != null) player.transform.position += new Vector3(0, 0.08f, -1.5f) - actor.transform.position;
            }
            else Debug.LogWarning("[GDD Juice] Course Player Rig.prefab missing. Run Tools > GDD 110 > Prepare Course Project once.");

            var vol = new GameObject("Showcase Look").AddComponent<Volume>();
            vol.isGlobal = true;
            vol.sharedProfile = settings.postProfile;
            new GameObject("Showcase").AddComponent<GddJuiceShowcase>();

            Folder(Root + "/Scenes");
            EditorSceneManager.SaveScene(scene, Root + "/Scenes/Block Showcase.unity");
            Debug.Log("[GDD Juice] Built Block Showcase scene.");
        }

        static GddCollectible Collectible(GameObject go, GddCollectible.Kind kind, string friendly)
        {
            foreach (var col in go.GetComponents<Collider>()) Object.DestroyImmediate(col);
            var c = go.AddComponent<GddCollectible>();
            var box = go.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = Vector3.one * 1.4f;
            c.kind = kind;
            c.friendlyName = friendly;
            return c;
        }

        /// <summary>The slime keeps the pack's own materials (converted to URP); this just stops it vanishing at screen edges while it jiggles.</summary>
        static void SlimeLook(GameObject slime)
        {
            foreach (var smr in slime.GetComponentsInChildren<SkinnedMeshRenderer>(true)) smr.updateWhenOffscreen = true;
        }

        /// <summary>Third Pickups row: snap to hand, jiggly snap to head, a gem counter, and a key with a locked door.</summary>
        static void BuildCollectibles(District pick)
        {
            // Snap to the right hand (rigid). The sword's pivot is the middle of its grip, which lands in the palm.
            var swordTop = Station(pick, "Snap to hand", -9f, 7f);
            var sword = new GameObject("Snap Collectible (sword)");
            sword.transform.SetParent(swordTop, false);
            sword.transform.localPosition = new Vector3(0, 0.75f, 0);
            var gold = Mat("Gold Trim", new Color(1f, 0.75f, 0.25f), 0.9f);
            // Blade edge runs along Z: grip sockets point Z along the fingers, so the edge lines up with the knuckles.
            Prim(PrimitiveType.Cube, "Blade", sword.transform, new Vector3(0, 0.56f, 0), new Vector3(0.03f, 0.8f, 0.08f), Mat("Blade", new Color(0.85f, 0.9f, 1f), 0.95f, new Color(0.3f, 0.6f, 1f) * 0.4f), false);
            Prim(PrimitiveType.Cube, "Guard", sword.transform, new Vector3(0, 0.15f, 0), new Vector3(0.06f, 0.05f, 0.3f), gold, false);
            Prim(PrimitiveType.Cube, "Grip", sword.transform, Vector3.zero, new Vector3(0.045f, 0.25f, 0.045f), Mat("Grip", new Color(0.3f, 0.18f, 0.12f), 0.3f), false);
            Prim(PrimitiveType.Sphere, "Pommel", sword.transform, new Vector3(0, -0.14f, 0), Vector3.one * 0.07f, gold, false);
            var s = Collectible(sword, GddCollectible.Kind.SnapToPlayer, "Sword");
            s.snapTo = GddSocket.RightHand;
            s.grabbedPosition = Vector3.zero;
            s.grabbedRotation = Vector3.zero;
            var slash = sword.AddComponent<GddSlashAttack>();
            slash.friendlyName = "Sword Slash";
            slash.slashEffect = KayKitVfx("vfx_KassSlash_v6_purple_orange");
            slash.slashSound = Load<AudioClip>("Assets/GDD 110 Project Files/Wesson Assets/VFX/Stylized VFX and Sounds/Stylized VFX/Sounds/Slashes Piercing/WAV/SFX_Demo_Slash_Circle_Dark.wav");

            // Snap to the head with jiggle: a rigged slime (three bones) that sits fixed on the head while its top wobbles.
            var hatTop = Station(pick, "Snap to head (jiggle)", -3f, 7f);
            var slime = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GDD 110 Project Files/Third-Party Art/Kawaii Slimes/Prefabs/Slime_01_King.prefab");
            GameObject hat;
            if (slime != null)
            {
                hat = (GameObject)PrefabUtility.InstantiatePrefab(slime, hatTop);
                hat.name = "Snap Collectible (slime king hat)";
                hat.transform.localPosition = new Vector3(0, 0.45f, 0);
                hat.transform.localScale = Vector3.one * 0.6f;
                SlimeLook(hat);
            }
            else
            {
                hat = new GameObject("Snap Collectible (antenna crown)");
                hat.transform.SetParent(hatTop, false);
                hat.transform.localPosition = new Vector3(0, 0.8f, 0);
                Prim(PrimitiveType.Cylinder, "Band", hat.transform, Vector3.zero, new Vector3(0.36f, 0.06f, 0.36f), gold, false);
            }
            var cr = Collectible(hat, GddCollectible.Kind.SnapToPlayer, "Slime King Hat");
            hat.GetComponent<BoxCollider>().center = new Vector3(0, 0.4f, 0);
            cr.snapTo = GddSocket.Head;
            cr.grabbedPosition = new Vector3(0, 0.12f, 0.01f);
            cr.grabbedScale = Vector3.one * 0.6f;
            cr.useJiggle = true;
            cr.jiggleFirmness = 0.12f;

            // Gem counter: five gems sharing one "Gems" counter, icon made by the icon tool
            var gemTop = Station(pick, "Counter collectibles", 3f, 7f, true);
            Sprite gemIcon = null;
            for (int i = 0; i < 5; i++)
            {
                float a = (i - 2f) * 0.5f;
                var gem = Prim(PrimitiveType.Cube, "Counter Collectible (gem " + (i + 1) + ")", gemTop, new Vector3(Mathf.Sin(a) * 1.4f, 0.7f, Mathf.Cos(a) * 1.4f - 1.1f), Vector3.one * 0.32f, Mat("Gem", new Color(0.3f, 0.95f, 1f), 0.95f, new Color(0.1f, 0.7f, 1f) * 0.8f));
                gem.transform.localRotation = Quaternion.Euler(45, 0, 45);
                gem.AddComponent<GddMotion>().rotationPerSecond = new Vector3(0, 90, 0);
                var g = Collectible(gem, GddCollectible.Kind.PickupCounter, "Gem");
                g.counterName = "Gems";
                if (gemIcon == null) gemIcon = GDD110.Editor.GddIconMaker.Make(gem, "Gem");
                g.icon = gemIcon;
            }

            // Key item and a Key Lock that opens a door
            var keyTop = Station(pick, "Key item", 7.5f, 7f);
            var key = new GameObject("Key Item (red key)");
            key.transform.SetParent(keyTop, false);
            key.transform.localPosition = new Vector3(0, 0.9f, 0);
            key.transform.localRotation = Quaternion.Euler(0, 0, 90);
            var red = Mat("Red Key", new Color(1f, 0.2f, 0.2f), 0.9f, new Color(1f, 0.1f, 0.1f) * 0.5f);
            Prim(PrimitiveType.Cylinder, "Bow", key.transform, new Vector3(0, 0.22f, 0), new Vector3(0.28f, 0.03f, 0.28f), red, false).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Prim(PrimitiveType.Cube, "Shaft", key.transform, Vector3.zero, new Vector3(0.06f, 0.4f, 0.06f), red, false);
            Prim(PrimitiveType.Cube, "Bit", key.transform, new Vector3(0.06f, -0.15f, 0), new Vector3(0.1f, 0.06f, 0.06f), red, false);
            // Gold trim made from KayKit pieces (chain link bow, barrier teeth), arranged by hand in the editor.
            var keyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/GDD 110 Project Files/Wesson Assets/Materials/Platformer/Key.mat");
            var trim = new GameObject("Key Model").transform;
            trim.SetParent(key.transform, false);
            void KeyPart(string model, Vector3 pos, float roll, Vector3 scale)
            {
                var part = KitModel(model, model, trim, false);
                if (part == null) return;
                part.transform.localPosition = pos;
                part.transform.localRotation = Quaternion.Euler(0, 0, roll);
                part.transform.localScale = scale;
                if (keyMat != null) foreach (var r in part.GetComponentsInChildren<Renderer>()) r.sharedMaterial = keyMat;
            }
            KeyPart("chain_link", new Vector3(0, 0.228f, -0.009f), 270f, new Vector3(0.463f, 0.435f, 0.912f));
            KeyPart("barrier_4x1x2_yellow", new Vector3(-0.054f, 0.038f, -0.009f), 270f, new Vector3(0.057f, 0.045f, 0.105f));
            KeyPart("barrier_4x1x2_yellow", new Vector3(-0.054f, -0.161f, -0.009f), 270f, new Vector3(0.057f, 0.045f, 0.105f));
            KeyPart("barrier_4x1x4_yellow", new Vector3(-0.073f, -0.13f, -0.003f), 180f, new Vector3(0.032f, 0.034f, 0.066f));
            KeyPart("barrier_4x1x4_yellow", new Vector3(-0.035f, 0.05f, -0.003f), 180f, new Vector3(0.032f, 0.034f, 0.066f));
            key.AddComponent<GddMotion>().rotationPerSecond = new Vector3(0, 60, 0);
            var k = Collectible(key, GddCollectible.Kind.KeyItem, "Red Key");
            k.keyName = "Red Key";

            var doorRoot = new GameObject("Locked Door");
            doorRoot.transform.SetParent(pick.root, false);
            doorRoot.transform.localPosition = new Vector3(11.2f, 0, 9f);
            KitBox("Post L", doorRoot.transform, new Vector3(-1.4f, 1.3f, 0), new Vector3(0.3f, 2.6f, 0.3f), KitColor(pick.title));
            KitBox("Post R", doorRoot.transform, new Vector3(1.4f, 1.3f, 0), new Vector3(0.3f, 2.6f, 0.3f), KitColor(pick.title));
            var door = KitBox("Mover (locked door)", doorRoot.transform, new Vector3(0, 1.25f, 0), new Vector3(2.5f, 2.5f, 0.2f), "red");
            var mover = door.AddComponent<GddMover>();
            mover.moveBy = new Vector3(0, 2.6f, 0);
            mover.duration = 0.7f;
            var lockBox = Prim(PrimitiveType.Cube, "Key Lock", doorRoot.transform, new Vector3(1.4f, 1.1f, -0.35f), new Vector3(0.35f, 0.45f, 0.2f), Mat("Lock", new Color(1f, 0.75f, 0.25f), 0.9f, new Color(1f, 0.6f, 0.1f) * 0.3f));
            Object.DestroyImmediate(lockBox.GetComponent<Collider>());
            var kl = lockBox.AddComponent<GddKeyLock>();
            var lockCol = lockBox.GetComponent<BoxCollider>();
            lockCol.isTrigger = true;
            lockCol.size = new Vector3(4f, 3f, 6f);
            kl.keyName = "Red Key";
            kl.friendlyName = "Red Door";
            Link(lockBox, mover, GddLinkAction.TurnOn);
            Sign(pick, "Key Lock -> door", Gold, 10.2f, 7.4f);
        }

        /// <summary>Two dialog characters and a Focus Moment box between them.</summary>
        static void BuildTalkers(District d)
        {
            // Mystery box: a Focus Moment the camera swings to when Pip mentions it
            var box = Prim(PrimitiveType.Cube, "Mystery Box (Focus Moment)", d.root, new Vector3(0, 0.6f, -2.5f), Vector3.one * 1.1f, Mat("Mystery Box", new Color(0.55f, 0.35f, 1f), 0.8f, new Color(0.5f, 0.3f, 1f) * 0.6f));
            box.transform.localRotation = Quaternion.Euler(0, 25, 0);
            var mark = Text("?", box.transform, box.transform.position + d.root.rotation * new Vector3(0, 0.05f, -0.56f), d.ReadFromPlaza, 9, Color.white, FontStyles.Bold, 1.2f);
            mark.transform.localScale = Vector3.one / 1.1f;
            var focus = box.AddComponent<GddFocusMoment>();
            focus.focusTime = 2.2f;
            focus.loop = true;
            focus.enabled = false;
            Sign(d, "Focus Moment (Pip points at it)", new Color(0.6f, 0.4f, 1f), 2.3f, -3.4f);

            var healthPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Toolkit + "/Health Collectible.prefab");
            var blip = AssetDatabase.FindAssets("MMSequencingNote2 t:AudioClip").Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault();

            // Pip: press E, lines in order, one advanced line drops a heart and triggers the focus moment
            var pip = Critter("Pip (Dialog)", d.root, new Vector3(-4.5f, 0.75f, -7f), new Color(0.45f, 0.75f, 1f), 1.3f, false);
            pip.transform.localRotation = Quaternion.Euler(0, 180, 0);
            Prim(PrimitiveType.Cylinder, "Hat", pip.transform, new Vector3(0, 0.55f, 0), new Vector3(0.55f, 0.12f, 0.55f), Mat("Pip Hat", new Color(1f, 0.75f, 0.25f), 0.7f), false);
            var talk = pip.AddComponent<GddDialog>();
            talk.friendlyName = "Pip";
            talk.activation = GddDialog.Activation.PressInteract;
            talk.mouth = new Vector3(0, -0.12f, 0.46f);
            talk.bubbleOffset = new Vector2(0.8f, 0.55f);
            talk.talkBlip = blip;
            talk.lines = new List<GddDialog.Line>
            {
                new GddDialog.Line { text = "Hi! I'm Pip. Press E to keep me talking." },
                new GddDialog.Line { text = "I can say my lines in order, pick one at random, or wait for an event to tell me what to say." },
                new GddDialog.Line { text = "Here, have a heart. And look at that box!", advanced = true, dropItem = healthPrefab, dropOffset = new Vector3(0.8f, 0.6f, 1.4f) },
                new GddDialog.Line { text = "That was a Focus Moment: the camera visits something, then comes back." },
            };
            UnityEventTools.AddVoidPersistentListener(talk.lines[2].onLine, focus.Play);

            // Dreamy: walk close, thought bubbles, random lines, moves on by itself
            var dreamy = Critter("Dreamy (Thought Dialog)", d.root, new Vector3(4.5f, 0.75f, -7f), new Color(0.8f, 0.7f, 1f), 1.3f, false);
            dreamy.transform.localRotation = Quaternion.Euler(0, 160, 0);
            var think = dreamy.AddComponent<GddDialog>();
            think.friendlyName = "Dreamy";
            think.activation = GddDialog.Activation.Proximity;
            think.proximityRadius = 3.5f;
            think.style = GddDialog.Style.Thought;
            think.order = GddDialog.Order.RandomLine;
            think.autoAdvanceSeconds = 2.6f;
            think.loop = true;
            think.mouth = new Vector3(0, 0.3f, 0.3f);
            think.bubbleOffset = new Vector2(-0.6f, 0.7f);
            think.lines = new List<GddDialog.Line>
            {
                new GddDialog.Line { text = "...I wonder what's in that box." },
                new GddDialog.Line { text = "...thought bubbles trail little circles." },
                new GddDialog.Line { text = "...walk away and I stop thinking out loud." },
                new GddDialog.Line { text = "...snacks. It's probably snacks." },
            };
            Sign(d, "Dialog: press E (Pip)", new Color(0.45f, 0.75f, 1f), -7.4f, -7.6f);
            Sign(d, "Dialog: walk close (Dreamy)", new Color(0.8f, 0.7f, 1f), 7.4f, -7.6f);
        }

        // ---------- armor, hazards, secrets, the edge ----------

        /// <summary>A helmet that makes enemies bounce off you and opens the pit's Armor Gate.</summary>
        static void BuildArmor(District d, float x, float z)
        {
            var top = Station(d, "Armor: enemies bounce off you", x, z);
            var armor = new GameObject("Snap Collectible (armor helmet)");
            armor.transform.SetParent(top, false);
            armor.transform.localPosition = new Vector3(0, 0.55f, 0);
            var helmet = Load<GameObject>("Assets/GDD 110 Project Files/Third-Party Art/Kawaii Slimes/Models/Top/MetalHelmet.fbx");
            if (helmet != null)
            {
                var look = (GameObject)PrefabUtility.InstantiatePrefab(helmet, armor.transform);
                look.name = "Model (helmet)";
                look.transform.localPosition = Vector3.zero;
                look.transform.localScale = Vector3.one * 0.5f;
                var hat = Load<Material>("Assets/GDD 110 Project Files/Third-Party Art/Kawaii Slimes/Materials/Hat.mat");
                foreach (var r in look.GetComponentsInChildren<Renderer>()) r.sharedMaterial = hat;
            }
            var c = Collectible(armor, GddCollectible.Kind.SnapToPlayer, "Armor Helmet");
            armor.GetComponent<BoxCollider>().center = new Vector3(0, 0.2f, 0);
            c.snapTo = GddSocket.Head;
            c.grabbedPosition = new Vector3(0, 0.08f, 0.01f);
            c.grabbedScale = Vector3.one * 0.85f;
            var a = armor.AddComponent<GddArmor>();
            a.friendlyName = "Armor";
            a.shieldEffect = Load<GameObject>(Etfx + "Combat/Shield/ShieldSoftBlue.prefab");
        }

        /// <summary>
        /// A moving hazard: a KayKit model that bobs up and down and/or spins, with a touch zone riding along on it.
        /// Damage hurts; Knockback shoves you back the way you came (and can hurt too).
        /// </summary>
        static GameObject Hazard(string model, string name, Transform parent, Vector3 pos, float size, float bob, float bobSpeed, float spinSpeed, int spinAxis,
                                 GddContactEffect.Effect effect, float amount, float knockDamage = 0f)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = pos;
            var mover = new GameObject("Bob").transform;
            mover.SetParent(root, false);
            var bm = mover.gameObject.AddComponent<GddMotion>();
            bm.rotationPerSecond = Vector3.zero; bm.bobHeight = bob; bm.bobSpeed = bobSpeed;
            var spinner = new GameObject("Spin").transform;
            spinner.SetParent(mover, false);
            var look = KitModel(model, "Model (" + model + ")", spinner, false);
            if (look == null) return root.gameObject;
            var mesh = look.GetComponentInChildren<MeshFilter>().sharedMesh;
            var mb = mesh.bounds;
            float k = size / Mathf.Max(mb.size.x, mb.size.y, mb.size.z);
            look.transform.localScale = Vector3.one * k;
            // Rollers and saws spin around their middle; everything else stands on the floor.
            bool aroundMiddle = spinSpeed != 0 && spinAxis != 1;
            look.transform.localPosition = aroundMiddle ? -mb.center * k : new Vector3(-mb.center.x, -mb.min.y, -mb.center.z) * k;
            if (spinSpeed != 0)
            {
                // spinAxis < 0: spin around the model's thinnest axis (a saw blade's hub); 0/1/2: x/y/z
                int axis = spinAxis;
                if (axis < 0) axis = mb.size.x <= mb.size.y && mb.size.x <= mb.size.z ? 0 : mb.size.y <= mb.size.z ? 1 : 2;
                if (spinAxis == 3) axis = mb.size.x >= mb.size.y && mb.size.x >= mb.size.z ? 0 : mb.size.y >= mb.size.z ? 1 : 2;   // longest axis (rollers)
                var sm = spinner.gameObject.AddComponent<GddMotion>();
                sm.rotationPerSecond = (axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward) * spinSpeed;
                if (spinAxis < 0 && axis == 1) spinner.localRotation = Quaternion.Euler(0, 0, 90);   // flat blade: stand it on its edge
            }
            var zone = new GameObject(effect == GddContactEffect.Effect.Damage ? "Hurt Zone" : "Knockback Zone");
            zone.transform.SetParent(spinner, false);
            var fx = zone.AddComponent<GddContactEffect>();
            var box = zone.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = mb.size * k * 0.9f;
            box.center = aroundMiddle ? Vector3.zero : Vector3.up * (mb.size.y * k * 0.5f);
            var rb = zone.GetComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;
            fx.effect = effect;
            fx.amount = amount;
            fx.repeatEvery = effect == GddContactEffect.Effect.Damage ? 0.6f : 0f;
            fx.knockbackDamage = knockDamage;
            fx.bounceCooldown = 0.6f;
            fx.friendlyName = name;
            return root.gameObject;
        }

        /// <summary>A walled lane out past the districts: spikes, saws, a spiked roller and a swiper, with a secret at the end.</summary>
        static void BuildHazardRun(Transform world, Color accent)
        {
            var run = new GameObject("Hazard Run").transform;
            run.SetParent(world, false);
            Vector3 dir = Quaternion.Euler(0, 36f, 0) * Vector3.forward;
            run.position = dir * 44f;
            run.rotation = Quaternion.LookRotation(dir);
            const float len = 28f, half = len * 0.5f;
            Prim(PrimitiveType.Cube, "Lane Floor", run, new Vector3(0, 0.03f, 0), new Vector3(6.4f, 0.06f, len),
                FloorMat("Floor Hazard Run", new Color(0.98f, 0.94f, 0.94f), new Color(0.95f, 0.88f, 0.88f), accent, 1f, 4f, 0.5f));
            for (float zz = -half + 2f; zz < half; zz += 4f)
            {
                KitBox("Lane Wall L", run, new Vector3(-3.4f, 0.75f, zz), new Vector3(0.5f, 1.5f, 4f), "red");
                KitBox("Lane Wall R", run, new Vector3(3.4f, 0.75f, zz), new Vector3(0.5f, 1.5f, 4f), "red");
            }
            KitBox("End Wall", run, new Vector3(0, 1f, half + 0.3f), new Vector3(6.4f, 2f, 0.5f), "red");
            var arch = KayKit("arch_wide_red");
            if (arch != null)
            {
                var a = (GameObject)PrefabUtility.InstantiatePrefab(arch, run);
                a.transform.localPosition = new Vector3(0, 0, -half);
                a.transform.localRotation = Quaternion.identity;
                foreach (var mf in a.GetComponentsInChildren<MeshFilter>()) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            }
            var title = Text("HAZARD RUN", run, run.TransformPoint(new Vector3(0, 4.3f, -half - 0.1f)), run.rotation, 5.5f, Color.white, FontStyles.Normal, 6f, DisplayFont());
            title.characterSpacing = 1f;
            Text("Spikes and saws hurt. Rollers and swipers shove you back.", run, run.TransformPoint(new Vector3(0, 3.6f, -half - 0.1f)), run.rotation, 1.8f, new Color(1f, 0.85f, 0.85f), FontStyles.Normal, 6f, Font("BoldItalic"));

            for (int i = 0; i < 3; i++)
                Hazard("floor_spikes_trap_2x2x1_red", "Floor Spikes " + (i + 1), run, new Vector3((i - 1) * 2f, 0.1f, -9f), 1.9f, 0.35f, 2.2f + i * 0.4f, 0, 0, GddContactEffect.Effect.Damage, 15f);
            for (int i = 0; i < 2; i++)
                Hazard("sawblade", "Saw " + (i + 1), run, new Vector3(i == 0 ? -1.4f : 1.4f, 0.3f, -3.5f + i * 2f), 1.4f, 0.45f, 1.6f + i * 0.7f, 360f, -1, GddContactEffect.Effect.Damage, 20f);
            Hazard("spikeroller_horizontal", "Spike Roller", run, new Vector3(0, 0.6f, 3f), 5.6f, 0f, 1f, 160f, 3, GddContactEffect.Effect.Knockback, 9f, 15f);
            Hazard("swiper_long_red", "Swiper", run, new Vector3(0, 0.06f, 8.5f), 5.6f, 0f, 1f, 110f, 1, GddContactEffect.Effect.Knockback, 10f);
            Hazard("saw_trap_red", "Saw Trap", run, new Vector3(0, 0.2f, 12f), 2.2f, 0.4f, 1.3f, 0, 0, GddContactEffect.Effect.Damage, 20f);
        }

        static void SecretStar(string color, Transform parent, Vector3 worldPos, Sprite icon)
        {
            var go = KitModel("star_" + color, "Secret Star (" + color + ")", parent, false);
            if (go == null) return;
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            go.transform.localScale = Vector3.one * (0.6f / Mathf.Max(0.01f, Mathf.Max(b.size.x, b.size.y)));
            go.transform.position = worldPos;
            var m = go.AddComponent<GddMotion>();
            m.rotationPerSecond = new Vector3(0, 90, 0); m.bobHeight = 0.12f; m.bobSpeed = 2f;
            var c = Collectible(go, GddCollectible.Kind.PickupCounter, "Secret Star");
            c.counterName = "Secret Stars";
            c.secret = true;
            c.icon = icon;
        }

        /// <summary>Four hidden stars, one of each color. Find them all and a portal gun appears in the plaza.</summary>
        static void BuildSecrets(Transform world, District touch, District key, District pick)
        {
            var secrets = new GameObject("Secrets (stars)").transform;
            secrets.SetParent(world, false);
            Sprite icon = null;
            var probe = KitModel("star_yellow", "Icon Probe", secrets, false);
            if (probe != null) { icon = GDD110.Editor.GddIconMaker.Make(probe, "Secret Star"); Object.DestroyImmediate(probe); }
            var room = touch.root.Find("Security Room");
            if (room != null) SecretStar("blue", secrets, room.TransformPoint(new Vector3(0, 0.7f, 3.6f)), icon);
            var pocket = key.root.Find("Play Timeline (gate)");
            if (pocket != null) SecretStar("green", secrets, pocket.TransformPoint(new Vector3(0, 0.8f, 5.2f)), icon);
            var door = pick.root.Find("Locked Door");
            if (door != null) SecretStar("yellow", secrets, door.TransformPoint(new Vector3(0, 0.8f, 1.8f)), icon);
            var run = world.Find("Hazard Run");
            if (run != null) SecretStar("red", secrets, run.TransformPoint(new Vector3(0, 0.8f, 15.6f)), icon);

            // The reward: a portal gun on a pedestal in the plaza, hidden until every star is found.
            var stand = new GameObject("Portal Gun (reward)").transform;
            stand.SetParent(world, false);
            stand.position = new Vector3(0, 0, 3.4f);
            var ped = KitModel("platform_decorative_1x1x1_green", "Pedestal", stand);
            if (ped != null) ped.transform.localScale = new Vector3(1.1f, 0.9f, 1.1f);
            var gun = Block("Hand Pickup", stand);
            gun.name = "Portal Gun";
            gun.transform.localPosition = new Vector3(0, 1.45f, 0);
            gun.GetComponent<GddEntity>().friendlyName = "Portal Gun";
            var gb = gun.GetComponent<Rigidbody>();
            if (gb != null) { gb.isKinematic = true; gb.useGravity = false; }
            SkinWith(gun, Load<GameObject>("Assets/GDD 110 Project Files/Wesson Assets/Models/Course Levels/PortalGun.fbx"), "PortalGun", 0.26f, true, 90f);   // about 45 cm long
            var gunGrip = gun.GetComponent<GddPickup>();
            gunGrip.attachmentRotation = new Vector3(90f, 180f, 0f);   // barrel along the knuckles, glowing tip forward
            gunGrip.attachmentPosition = new Vector3(0f, 0.06f, 0.08f);
            var exit = new GameObject("Portal Exit (plaza)").transform;
            exit.SetParent(world, false);
            exit.position = new Vector3(0, 0.2f, -3.5f);
            exit.rotation = Quaternion.LookRotation(Vector3.back);
            var pg = gun.AddComponent<GddPortalGun>();
            pg.friendlyName = "Portal Gun";
            pg.destination = exit;
            pg.portalLook = Load<GameObject>(Etfx + "Interactive/Portals/SwirlPortal/SwirlPortalGreen.prefab");
            pg.muzzleEffect = KayKitVfx("HCFX_Hit_01");
            pg.impactEffect = KayKitVfx("HCFX_Explosion_01");
            pg.shootSound = Load<AudioClip>("Assets/GDD 110 Project Files/Wesson Assets/Audio/SFX/FeelBounceCharge.wav");
            var hunt = new GameObject("Star Hunt").AddComponent<GddStarHunt>();
            hunt.transform.SetParent(world, false);
            hunt.needed = 4;
            hunt.reward = stand.gameObject;
            hunt.revealEffect = KayKitVfx("HCFX_Appear_02_Start");
            hunt.revealSound = Load<AudioClip>("Assets/GDD 110 Project Files/Wesson Assets/Audio/SFX/SFX_Braaam.wav");
        }

        /// <summary>Warning signs ring the edge; the Mimic Warden handles the warning, the scare and the lurking.</summary>
        static void BuildEdge(Transform world)
        {
            var edge = new GameObject("Map Edge").transform;
            edge.SetParent(world, false);
            var warden = edge.gameObject.AddComponent<GddMimicWarden>();
            warden.roarSound = Load<AudioClip>("Assets/GDD 110 Project Files/Wesson Assets/Audio/SFX/SFX_Mimic_Roar.wav");
            warden.braaamSound = Load<AudioClip>("Assets/GDD 110 Project Files/Wesson Assets/Audio/SFX/SFX_Braaam.wav");
            warden.roarEffect = KayKitVfx("VFX_Explosion_Floor");
            var mp = Load<GameObject>("Assets/GDD 110 Project Files/Third-Party Art/Mimic/Prefabs/Mimic.prefab");
            if (mp != null)
            {
                var m = (GameObject)PrefabUtility.InstantiatePrefab(mp, edge);
                PrefabUtility.UnpackPrefabInstance(m, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                var move = m.GetComponent<MimicSpace.Movement>();
                if (move != null) Object.DestroyImmediate(move);
                m.name = "Mimic";
                m.transform.position = new Vector3(0, 1.6f, 70f);
                warden.mimic = m.GetComponent<MimicSpace.Mimic>();
                m.SetActive(false);
            }
            for (int i = 0; i < 12; i++)
            {
                float a = (i * 30f + 15f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                var sign = KitModel("sign", "Warning Sign", edge);
                if (sign == null) continue;
                sign.transform.position = dir * 63f;
                sign.transform.rotation = Quaternion.LookRotation(-dir);
                sign.transform.localScale = Vector3.one * 2f;
                var t = Text("TURN BACK", sign.transform, dir * 62.55f + Vector3.up * 1.75f, Quaternion.LookRotation(dir), 2.4f, new Color(0.85f, 0.1f, 0.1f), FontStyles.Bold, 1.6f, DisplayFont());
                t.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        const string BlueSlimePath = "Assets/GDD 110 Project Files/Third-Party Art/RPGMonsterCouplePBRPolyart/Prefab/BlueSlime.prefab";
        const string GhostPath = "Assets/GDD 110 Project Files/Third-Party Art/GhostCharacter_Free/Prefabs/Ghost.prefab";
        const string OctopusPath = "Assets/GDD 110 Project Files/Third-Party Art/Monsters Ultimate Pack 10/Octopus Baby/Prefabs/No Root/Octopus Baby Purple No Root.prefab";
        const string HoloMatPath = "Assets/GDD 110 Project Files/Plugins/AllIn1Assets/AllIn1ShaderNodes/Demo/Materials/DemoMaterials/MAT_Demo_ChomperHologram.mat";
        const string Etfx = "Assets/GDD 110 Project Files/Third-Party Art/Reference Imports/Epic Toon FX 1/Prefabs/";

        static T Load<T>(string path) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) Debug.LogWarning("[GDD Juice] Missing asset: " + path);
            return a;
        }

        /// <summary>Swap a primitive critter's look for a character model (keeps its physics and behavior). Returns the model.</summary>
        static GameObject Dress(GameObject critter, string prefabPath, float height, string idle, string moving, bool centered = false)
        {
            var prefab = Load<GameObject>(prefabPath);
            if (critter == null || prefab == null) return null;
            var own = critter.GetComponent<Renderer>();
            Bounds slot = own != null ? own.bounds : new Bounds(critter.transform.position, Vector3.one * 0.6f);
            if (own != null) own.enabled = false;
            for (int k = critter.transform.childCount - 1; k >= 0; k--) Object.DestroyImmediate(critter.transform.GetChild(k).gameObject);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, critter.transform);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            foreach (var mb in model.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(mb);
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var rb in model.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rb);
            model.name = "Model (" + prefab.name + ")";
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            var rs = model.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            float k2 = height / Mathf.Max(0.01f, b.size.y);
            var ls = critter.transform.lossyScale;
            model.transform.localScale = new Vector3(k2 / ls.x, k2 / ls.y, k2 / ls.z);
            rs = model.GetComponentsInChildren<Renderer>();
            b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            Vector3 want = centered ? slot.center : new Vector3(slot.center.x, slot.min.y, slot.center.z);
            Vector3 have = centered ? b.center : new Vector3(b.center.x, b.min.y, b.center.z);
            model.transform.position += want - have;
            foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>()) smr.updateWhenOffscreen = true;
            var anim = model.GetComponentInChildren<Animator>();
            if (anim != null) anim.applyRootMotion = false;
            var st = model.AddComponent<GddAnimState>();
            st.idleState = idle; st.movingState = moving;
            return model;
        }

        /// <summary>A KayKit slope whose high end meets a ledge: rises to the given height over the given length.</summary>
        static GameObject Ramp(string name, Transform parent, Vector3 lowEnd, Vector3 upDirection, float width, float rise, float length, string color)
        {
            var go = KitModel("platform_slope_4x6x4_" + color, name, parent, false);
            if (go == null) return null;
            var mesh = go.GetComponentInChildren<MeshFilter>().sharedMesh;
            // Find which end of the model is the tall one.
            float hiZ = 0, n = 0, maxY = mesh.bounds.max.y;
            foreach (var v in mesh.vertices) if (v.y > maxY - 0.05f) { hiZ += v.z; n++; }
            bool highAtPlusZ = n == 0 || hiZ / n > 0;
            Vector3 flat = Vector3.ProjectOnPlane(upDirection, Vector3.up).normalized;
            go.transform.localRotation = Quaternion.LookRotation(highAtPlusZ ? flat : -flat);
            go.transform.localScale = new Vector3(width / 4f, rise / 4f, length / 6f);
            go.transform.localPosition = lowEnd + flat * (length * 0.5f);
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        /// <summary>
        /// Round creature enclosure. The friendly one is a fenced ring at ground level. The angry one is a pit: a raised
        /// viewing deck (with a ramp up) rings it, so you look down at the creatures, and the only way in is a doorway
        /// sealed by an Armor Gate. Small buttons outside send hologram copies of each creature out into the world.
        /// </summary>
        static void BuildPen(District d, string name, float x, Color color, bool angry, float z = 1f)
        {
            var pen = new GameObject(name);
            pen.transform.SetParent(d.root, false);
            pen.transform.localPosition = new Vector3(x, 0.05f, z);
            var p = pen.AddComponent<GddCreaturePen>();
            const float radius = 5.5f;
            p.round = true;
            p.size = new Vector3(radius * 2f, 3.2f, radius * 2f);
            p.flyerHeight = 1.2f;
            string kit = angry ? "red" : "neutral";
            var glass = Mat("Fence Glass", new Color(0.75f, 0.9f, 1f, 0.16f), 0.95f, null, true);
            Prim(PrimitiveType.Cylinder, "Pen Floor", pen.transform, new Vector3(0, 0.01f, 0), new Vector3(radius * 2f + 0.4f, 0.01f, radius * 2f + 0.4f),
                FloorMat("Pen " + name, Color.Lerp(Color.white, color, 0.1f), Color.Lerp(Color.white, color, 0.18f), color, 1f, 4f, 0.5f), false);
            const int n = 18;
            float deckH = 2.4f, inner = radius + 0.3f, outer = radius + 2.8f;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                Vector3 dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                var rot = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0);
                if (!angry)
                {
                    float seg = 2f * Mathf.PI * (radius + 0.15f) / n * 1.06f;
                    var rail = KitBox("Fence", pen.transform, dir * (radius + 0.15f) + Vector3.up * 0.2f, new Vector3(seg, 0.4f, 0.25f), kit);
                    rail.transform.localRotation = rot;
                    var high = Prim(PrimitiveType.Cube, "Fence Glass", pen.transform, dir * (radius + 0.15f) + Vector3.up * 1.5f, new Vector3(seg, 2.2f, 0.08f), glass);
                    high.transform.localRotation = rot;
                    high.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    p.fence.Add(rail.GetComponent<Collider>());
                    p.fence.Add(high.GetComponent<Collider>());
                    continue;
                }
                float mid = (inner + outer) * 0.5f, depth = outer - inner;
                float width = 2f * Mathf.PI * mid / n * 1.05f;
                bool door = i == n / 2;   // faces the plaza
                var block = KitModel(door ? "platform_4x2x1_" + kit : "platform_4x2x2_" + kit, door ? "Doorway Bridge" : "Deck", pen.transform);
                block.transform.localRotation = rot;
                block.transform.localPosition = dir * mid + (door ? Vector3.up * (deckH - 0.35f) : Vector3.zero);
                block.transform.localScale = new Vector3(width / 4f, door ? 0.35f : deckH / 2f, depth / 2f);
                var rail2 = Prim(PrimitiveType.Cube, "Fence Glass", pen.transform, dir * (inner + 0.06f) + Vector3.up * (deckH + 0.6f), new Vector3(2f * Mathf.PI * inner / n * 1.06f, 1.2f, 0.08f), glass);
                rail2.transform.localRotation = rot;
                rail2.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                p.fence.Add(rail2.GetComponent<Collider>());
                if (door)
                {
                    var field = Prim(PrimitiveType.Cube, "Armor Gate (force field)", pen.transform, dir * mid + Vector3.up * ((deckH - 0.35f) * 0.5f), new Vector3(width * 0.95f, deckH - 0.35f, 0.15f),
                        Mat("Force Field", new Color(0.35f, 0.8f, 1f, 0.3f), 0.95f, new Color(0.3f, 0.7f, 1f) * 0.8f, true));
                    field.transform.localRotation = rot;
                    field.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    field.AddComponent<GddArmorGate>();
                    p.fence.Add(field.GetComponent<Collider>());
                    foreach (float side in new[] { -1f, 1f })
                    {
                        var post = KitBox("Gate Post", pen.transform, dir * mid + rot * Vector3.right * (side * width * 0.5f) + Vector3.up * (deckH * 0.5f), new Vector3(depth, deckH, 0.35f), kit);
                        post.transform.localRotation = rot * Quaternion.Euler(0, 90, 0);
                    }
                }
                else foreach (var c in block.GetComponentsInChildren<Collider>()) p.fence.Add(c);
            }
            if (angry)
            {
                Ramp("Ramp", pen.transform, new Vector3(0, 0, outer + 6f), Vector3.back, 3f, deckH, 6f, kit);
                Sign(d, "Enemy pit: look from the deck, or wear Armor to walk in", color, x, z - outer - 1.4f);
            }
            else Sign(d, "Friendly: they just want to be near you", color, x, z - radius - 0.6f);

            var facing = Quaternion.Euler(0, 180, 0);
            if (!angry)
            {
                var companion = Block("Companion", pen.transform);
                companion.transform.localPosition = new Vector3(0, 0.6f, 0.5f);
                companion.transform.localRotation = facing;
                companion.transform.localScale = Vector3.one * 0.9f;
                companion.GetComponent<Renderer>().sharedMaterial = Mat("Critter Companion", new Color(1f, 0.55f, 0.75f), 0.6f, new Color(1f, 0.4f, 0.6f) * 0.15f);
                Face(companion.transform, false);
                for (int i = 0; i < 2; i++)
                {
                    var hop = Critter(i == 0 ? "Octopus Friend" : "Hopper Friend", pen.transform, new Vector3(i == 0 ? -2.5f : 2.5f, 0.5f, 1.5f), i == 0 ? new Color(1f, 0.75f, 0.85f) : new Color(0.75f, 0.85f, 1f), 0.6f, false);
                    hop.transform.localRotation = facing;
                    var rb = hop.AddComponent<Rigidbody>();
                    rb.mass = 0.5f; rb.constraints = RigidbodyConstraints.FreezeRotation;
                    var cc = hop.AddComponent<CompanionCreature>();
                    cc.jumpForce = 2.6f; cc.minJumpInterval = 0.9f; cc.maxJumpInterval = 2.2f; cc.randomness = 0.4f;
                    if (i == 0) Dress(hop, OctopusPath, 0.9f, "Float", "Move Forward In Place");
                }
                return;
            }

            var enemy = Block("Enemy", pen.transform);
            enemy.transform.localPosition = new Vector3(0, 0.6f, 0.5f);
            enemy.transform.localRotation = facing;
            enemy.transform.localScale = Vector3.one * 0.9f;
            enemy.GetComponent<Renderer>().sharedMaterial = Mat("Critter Enemy", new Color(0.9f, 0.2f, 0.25f), 0.6f, new Color(1f, 0.1f, 0.1f) * 0.15f);
            enemy.GetComponent<GddFollow>().speed = 1.6f;
            Face(enemy.transform, true);
            var styles = new[] { (Enemy.MovementType.Jump, "Slime Enemy (hops)", new Color(0.3f, 0.6f, 1f), 3f, -2.6f),
                                 (Enemy.MovementType.GhostChase, "Ghost Enemy (flies)", new Color(0.7f, 0.45f, 1f), 1.4f, 0f),
                                 (Enemy.MovementType.RollTowardsPlayer, "Roller Enemy", new Color(1f, 0.65f, 0.2f), 2.5f, 2.6f) };
            var holoMat = Load<Material>(HoloMatPath);
            int b = 0;
            foreach (var (type, label, col, speed, px) in styles)
            {
                var c = Critter(label, pen.transform, new Vector3(px, type == Enemy.MovementType.GhostChase ? 1.4f : 0.5f, 2.2f), col, 0.6f, true);
                c.transform.localRotation = facing;
                GameObject look = null;
                if (type == Enemy.MovementType.Jump) look = Dress(c, BlueSlimePath, 0.9f, "BlueSlime_IdleNormal", "BlueSlime_RunFWD");
                if (type == Enemy.MovementType.GhostChase) look = Dress(c, GhostPath, 1.2f, "idle", "move", true);
                if (type != Enemy.MovementType.GhostChase)
                {
                    var rb = c.AddComponent<Rigidbody>();
                    rb.mass = 0.6f;
                    if (type == Enemy.MovementType.Jump) rb.constraints = RigidbodyConstraints.FreezeRotation;
                }
                var e = c.AddComponent<Enemy>();
                e.movementType = type;
                e.speed = speed;

                // Hologram preview button for this creature, beside the doorway
                var btn = Block("Interact Button", pen.transform);
                btn.name = "Hologram Button - " + label;
                btn.transform.localPosition = new Vector3(2.4f + b * 1.1f, 0.35f, -outer - 0.9f);
                btn.transform.localScale = Vector3.one * 0.5f;
                btn.GetComponent<GddInteractable>().actionLabel = "Preview";
                btn.GetComponent<GddEntity>().friendlyName = "Hologram: " + label.Split(' ')[0];
                Skin(btn, "button_base_red", 0.6f);
                var holo = new GameObject("Hologram Preview - " + label);
                holo.transform.SetParent(btn.transform, false);
                var hp = holo.AddComponent<GddHologramPreview>();
                hp.friendlyName = "Hologram " + label.Split(' ')[0];
                hp.creature = type == Enemy.MovementType.Jump ? Load<GameObject>(BlueSlimePath) : type == Enemy.MovementType.GhostChase ? Load<GameObject>(GhostPath) : c;
                hp.style = type == Enemy.MovementType.Jump ? GddHologramPreview.Style.Hop : type == Enemy.MovementType.GhostChase ? GddHologramPreview.Style.Float : GddHologramPreview.Style.Roll;
                hp.hologramMaterial = holoMat;
                hp.speed = type == Enemy.MovementType.GhostChase ? 3f : 4.5f;
                hp.appearEffect = KayKitVfx("HCFX_Appear_02_Start");
                hp.fizzleEffect = KayKitVfx("HCFX_Hit_02");
                Link(btn, hp, GddLinkAction.Activate);
                b++;
            }
            Sign(d, "Hologram previews: press to watch each one move", color, x + 3.5f, z - outer - 2.6f);
        }

        static GameObject KayKitVfx(string name)
        {
            foreach (var g in AssetDatabase.FindAssets(name + " t:Prefab"))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == name && !p.Contains("_Unreferenced")) return AssetDatabase.LoadAssetAtPath<GameObject>(p);
            }
            Debug.LogWarning("[GDD Juice] Effect not found: " + name);
            return null;
        }
    }
}
