using UnityEditor;
using UnityEngine;

namespace GDD110.Editor
{
    /// <summary>Dialog line in the inspector: text and voice always; event and dropped item only when Advanced is ticked.</summary>
    [CustomPropertyDrawer(typeof(GddDialog.Line))]
    public class GddDialogLineDrawer : PropertyDrawer
    {
        const float Pad = 2f;

        public override float GetPropertyHeight(SerializedProperty p, GUIContent label)
        {
            float h = EditorGUIUtility.singleLineHeight + Pad;   // foldout
            if (!p.isExpanded) return h;
            h += EditorGUI.GetPropertyHeight(p.FindPropertyRelative("text")) + Pad;
            h += EditorGUIUtility.singleLineHeight * 2 + Pad * 2;   // voice, advanced
            if (p.FindPropertyRelative("advanced").boolValue)
            {
                h += EditorGUI.GetPropertyHeight(p.FindPropertyRelative("onLine")) + Pad;
                h += EditorGUIUtility.singleLineHeight * 2 + Pad * 2;   // drop item, offset
            }
            return h + 4f;
        }

        public override void OnGUI(Rect r, SerializedProperty p, GUIContent label)
        {
            var text = p.FindPropertyRelative("text");
            string preview = string.IsNullOrEmpty(text.stringValue) ? "(no text)" : text.stringValue.Replace('\n', ' ');
            if (preview.Length > 48) preview = preview.Substring(0, 45) + "...";
            var line = new Rect(r.x, r.y, r.width, EditorGUIUtility.singleLineHeight);
            p.isExpanded = EditorGUI.Foldout(line, p.isExpanded, label.text + ":  " + preview, true);
            if (!p.isExpanded) return;
            EditorGUI.indentLevel++;
            line.y += line.height + Pad;
            float th = EditorGUI.GetPropertyHeight(text);
            EditorGUI.PropertyField(new Rect(r.x, line.y, r.width, th), text);
            line.y += th + Pad;
            EditorGUI.PropertyField(line, p.FindPropertyRelative("voice"));
            line.y += line.height + Pad;
            var adv = p.FindPropertyRelative("advanced");
            EditorGUI.PropertyField(line, adv, new GUIContent("Advanced (event, drop item)"));
            if (adv.boolValue)
            {
                line.y += line.height + Pad;
                var ev = p.FindPropertyRelative("onLine");
                float eh = EditorGUI.GetPropertyHeight(ev);
                EditorGUI.PropertyField(new Rect(r.x, line.y, r.width, eh), ev);
                line.y += eh + Pad;
                EditorGUI.PropertyField(line, p.FindPropertyRelative("dropItem"));
                line.y += line.height + Pad;
                EditorGUI.PropertyField(line, p.FindPropertyRelative("dropOffset"));
            }
            EditorGUI.indentLevel--;
        }
    }

    /// <summary>Drag the mouth point and see where the bubble will sit, right in the Scene view.</summary>
    [CustomEditor(typeof(GddDialog))]
    public class GddDialogEditor : UnityEditor.Editor
    {
        void OnSceneGUI()
        {
            var d = (GddDialog)target;
            var t = d.transform;
            Vector3 mouth = t.TransformPoint(d.mouth);
            EditorGUI.BeginChangeCheck();
            Handles.color = new Color(1f, 0.55f, 0.2f);
            var fmh = Quaternion.identity;
            Vector3 moved = Handles.FreeMoveHandle(mouth, HandleUtility.GetHandleSize(mouth) * 0.08f, Vector3.zero, Handles.SphereHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(d, "Move dialog mouth");
                d.mouth = t.InverseTransformPoint(moved);
            }
            var cam = SceneView.currentDrawingSceneView != null ? SceneView.currentDrawingSceneView.camera.transform : null;
            Vector3 right = cam != null ? cam.right : Vector3.right;
            Vector3 up = cam != null ? cam.up : Vector3.up;
            Vector3 bubble = mouth + right * d.bubbleOffset.x + Vector3.up * (d.bubbleOffset.y + 0.35f);
            Handles.color = new Color(1f, 1f, 1f, 0.9f);
            Handles.DrawLine(mouth, bubble - Vector3.up * 0.3f, 2f);
            Handles.DrawSolidRectangleWithOutline(new[] { bubble - right * 1.1f - up * 0.35f, bubble - right * 1.1f + up * 0.35f, bubble + right * 1.1f + up * 0.35f, bubble + right * 1.1f - up * 0.35f },
                new Color(1, 1, 1, 0.12f), Color.black);
            Handles.Label(bubble - right * 0.9f, d.lines.Count > 0 ? d.lines[0].text : "(no lines)");
            Handles.Label(mouth + up * 0.08f, "mouth");
            if (d.activation == GddDialog.Activation.Proximity)
            {
                Handles.color = new Color(1f, 0.55f, 0.2f, 0.6f);
                Handles.DrawWireDisc(t.position, Vector3.up, d.proximityRadius);
            }
        }
    }
}
