using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GDD110.Editor
{
    public static class GddCourseMenus
    {
        public const string Root="Assets/GDD 110 Project Files/Course/Toolkit";
        public static readonly string[] Templates={"Event Trigger","Interact Button","Hand Pickup","Hat Pickup","Chest Pickup","Blaster Pickup","Speed Boots","Jump Boots","Health Collectible","Score Collectible","Hazard","Bounce Pad","Teleporter","Next Scene Portal","Spawner","Companion","Enemy","Timer","Rotating Object"};
        public static void EnsureFolder(string path){if(AssetDatabase.IsValidFolder(path))return;var parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(path));}
        static Material Material(string name,Color color)
        {
            EnsureFolder(Root+"/Materials");string path=Root+"/Materials/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat!=null)return mat;
            var shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)throw new Exception("Install URP before creating GDD objects.");
            mat=new Material(shader){name=name};mat.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(mat,path);return mat;
        }
        static GameObject Shape(string name,PrimitiveType type,Color color,Vector3 size)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.localScale=size;
            go.GetComponent<Renderer>().sharedMaterial=Material("GDD "+ColorUtility.ToHtmlStringRGB(color),color);return go;
        }
        static void Volume(GameObject go)
        {
            var box=go.GetComponent<BoxCollider>();if(box==null)box=go.AddComponent<BoxCollider>();box.isTrigger=true;
            var rb=go.GetComponent<Rigidbody>();if(rb==null)rb=go.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
        }
        public static GddPowerDefinition Definition(GddPowerKind kind)
        {
            EnsureFolder(Root+"/Powers");string path=Root+"/Powers/"+ObjectNames.NicifyVariableName(kind.ToString())+".asset";
            var d=AssetDatabase.LoadAssetAtPath<GddPowerDefinition>(path);if(d!=null)return d;
            d=ScriptableObject.CreateInstance<GddPowerDefinition>();d.kind=kind;d.amount=kind==GddPowerKind.Score?1:25;
            if(kind==GddPowerKind.Projectile)d.projectilePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Power Projectile.prefab");
            AssetDatabase.CreateAsset(d,path);return d;
        }
        static void Power(GameObject go,GddPowerKind kind,GddPower.Activation activation=GddPower.Activation.UseButton)
        {
            var p=go.AddComponent<GddPower>();p.definition=Definition(kind);p.activation=activation;
        }
        static GameObject Make(string kind)
        {
            var color=new Color(0.13f,0.7f,0.9f);var go=Shape(kind,PrimitiveType.Cube,color,new Vector3(0.65f,0.65f,0.65f));
            if(kind.Contains("Pickup")||kind.Contains("Boots")||kind.Contains("Collectible"))
            {
                var pickup=go.AddComponent<GddPickup>();var rb=go.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
                if(kind=="Hat Pickup"){pickup.attachment=GddSocket.Head;go.transform.localScale=new Vector3(0.5f,0.2f,0.5f);pickup.attachmentPosition=new Vector3(0,0.15f,0);}
                if(kind=="Chest Pickup"){pickup.attachment=GddSocket.Chest;go.transform.localScale=new Vector3(0.6f,0.6f,0.2f);pickup.attachmentPosition=new Vector3(0,0,0.15f);}
                if(kind=="Blaster Pickup"){Power(go,GddPowerKind.Projectile);go.transform.localScale=new Vector3(0.2f,0.2f,0.55f);}
                if(kind.Contains("Boots")){pickup.attachment=GddSocket.LeftFoot;go.transform.localScale=new Vector3(0.25f,0.18f,0.4f);Power(go,kind=="Speed Boots"?GddPowerKind.SpeedBoost:GddPowerKind.JumpBoost,GddPower.Activation.WhileEquipped);}
                if(kind.Contains("Collectible")){pickup.mode=GddPickup.PickupMode.Collect;pickup.collectOnTouch=true;Volume(go);Power(go,kind=="Health Collectible"?GddPowerKind.Heal:GddPowerKind.Score);}
            }
            else if(kind=="Event Trigger"){go.AddComponent<GddTrigger>();Volume(go);go.GetComponent<Renderer>().enabled=false;go.transform.localScale=new Vector3(2,2,2);}
            else if(kind=="Interact Button")go.AddComponent<GddInteractable>();
            else if(new[]{"Hazard","Bounce Pad","Teleporter","Next Scene Portal"}.Contains(kind))
            {
                var effect=go.AddComponent<GddContactEffect>();Volume(go);
                effect.effect=kind=="Hazard"?GddContactEffect.Effect.Damage:kind=="Bounce Pad"?GddContactEffect.Effect.Launch:kind=="Teleporter"?GddContactEffect.Effect.Teleport:GddContactEffect.Effect.NextScene;
                if(kind=="Teleporter")
                {
                    var destination=new GameObject("Teleport Destination — move me").transform;destination.SetParent(go.transform,false);destination.localPosition=Vector3.forward*5;effect.destination=destination;
                }
                go.transform.localScale=kind=="Bounce Pad"?new Vector3(2,0.2f,2):new Vector3(1,1.5f,1);
            }
            else if(kind=="Spawner"){var s=go.AddComponent<GddSpawn>();s.prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Spawned Cube.prefab");}
            else if(kind=="Companion"||kind=="Enemy")
            {
                var rb=go.AddComponent<Rigidbody>();rb.constraints=RigidbodyConstraints.FreezeRotation;var f=go.AddComponent<GddFollow>();
                var h=go.AddComponent<GddHealth>();h.destroyWhenEmpty=true;if(kind=="Enemy"){f.contactDamage=10;f.stopDistance=0.5f;}
            }
            else if(kind=="Timer")go.AddComponent<GddTimer>();
            else if(kind=="Rotating Object")go.AddComponent<GddMotion>();
            return go;
        }
        [MenuItem("Tools/GDD 110/Refresh Ready-Made Objects")]
        public static void GenerateLibrary()
        {
            EnsureFolder(Root+"/Prefabs");
            if(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Power Projectile.prefab")==null)
            {
                var shot=Shape("Power Projectile",PrimitiveType.Sphere,Color.yellow,Vector3.one*0.15f);shot.AddComponent<GddProjectile>();shot.GetComponent<Rigidbody>().useGravity=false;
                PrefabUtility.SaveAsPrefabAsset(shot,Root+"/Prefabs/Power Projectile.prefab");UnityEngine.Object.DestroyImmediate(shot);
            }
            if(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Spawned Cube.prefab")==null)
            {
                var cube=Shape("Spawned Cube",PrimitiveType.Cube,new Color(0.9f,0.5f,0.1f),Vector3.one*0.4f);cube.AddComponent<Rigidbody>();cube.AddComponent<GddEntity>();
                PrefabUtility.SaveAsPrefabAsset(cube,Root+"/Prefabs/Spawned Cube.prefab");UnityEngine.Object.DestroyImmediate(cube);
            }
            foreach(var kind in Templates)
            {
                string path=Root+"/Prefabs/"+kind+".prefab";if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)continue;
                var go=Make(kind);PrefabUtility.SaveAsPrefabAsset(go,path);UnityEngine.Object.DestroyImmediate(go);
            }
            AssetDatabase.SaveAssets();
        }
        public static int CleanPrefabEvents(string path)
        {
            var root=PrefabUtility.LoadPrefabContents(path);int removed=0;
            try
            {
                foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c!=null))
                {
                    var serialized=new SerializedObject(component);var iterator=serialized.GetIterator();var paths=new System.Collections.Generic.List<string>();
                    while(iterator.Next(true))if(iterator.isArray&&iterator.propertyPath.EndsWith("m_PersistentCalls.m_Calls"))paths.Add(iterator.propertyPath);
                    foreach(var propertyPath in paths)
                    {
                        var calls=serialized.FindProperty(propertyPath);
                        for(int i=calls.arraySize-1;i>=0;i--)
                        {
                            var call=calls.GetArrayElementAtIndex(i);
                            if(call.FindPropertyRelative("m_Target").objectReferenceValue==null&&!string.IsNullOrEmpty(call.FindPropertyRelative("m_MethodName").stringValue)){calls.DeleteArrayElementAtIndex(i);removed++;}
                        }
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                if(removed>0)PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            return removed;
        }
        public static void Create(string kind,MenuCommand command)
        {
            GenerateLibrary();var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+kind+".prefab");
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);Undo.RegisterCreatedObjectUndo(go,"Create "+kind);
            var parent=command!=null?command.context as GameObject:null;
            if(parent!=null)GameObjectUtility.SetParentAndAlign(go,parent);
            else if(SceneView.lastActiveSceneView!=null)
            {
                var camera=SceneView.lastActiveSceneView.camera;var ray=new Ray(camera.transform.position,camera.transform.forward);
                go.transform.position=Physics.Raycast(ray,out var hit,30)?hit.point+Vector3.up*0.6f:SceneView.lastActiveSceneView.pivot+Vector3.up*0.6f;
            }
            Selection.activeGameObject=go;EditorSceneManager.MarkSceneDirty(go.scene);
        }
        [MenuItem("GameObject/GDD 110/Create/Event Trigger",false,10)]static void C0(MenuCommand c)=>Create("Event Trigger",c);
        [MenuItem("GameObject/GDD 110/Create/Interact Button",false,11)]static void C1(MenuCommand c)=>Create("Interact Button",c);
        [MenuItem("GameObject/GDD 110/Create/Pickups/Hand Pickup",false,12)]static void C2(MenuCommand c)=>Create("Hand Pickup",c);
        [MenuItem("GameObject/GDD 110/Create/Pickups/Hat Pickup",false,13)]static void C3(MenuCommand c)=>Create("Hat Pickup",c);
        [MenuItem("GameObject/GDD 110/Create/Pickups/Chest Pickup",false,14)]static void C4(MenuCommand c)=>Create("Chest Pickup",c);
        [MenuItem("GameObject/GDD 110/Create/Powers/Blaster Pickup",false,15)]static void C5(MenuCommand c)=>Create("Blaster Pickup",c);
        [MenuItem("GameObject/GDD 110/Create/Powers/Speed Boots",false,16)]static void C6(MenuCommand c)=>Create("Speed Boots",c);
        [MenuItem("GameObject/GDD 110/Create/Powers/Jump Boots",false,17)]static void C7(MenuCommand c)=>Create("Jump Boots",c);
        [MenuItem("GameObject/GDD 110/Create/Collectibles/Health",false,18)]static void C8(MenuCommand c)=>Create("Health Collectible",c);
        [MenuItem("GameObject/GDD 110/Create/Collectibles/Score",false,19)]static void C9(MenuCommand c)=>Create("Score Collectible",c);
        [MenuItem("GameObject/GDD 110/Create/Hazard",false,20)]static void C10(MenuCommand c)=>Create("Hazard",c);
        [MenuItem("GameObject/GDD 110/Create/Bounce Pad",false,21)]static void C11(MenuCommand c)=>Create("Bounce Pad",c);
        [MenuItem("GameObject/GDD 110/Create/Teleporter",false,22)]static void C12(MenuCommand c)=>Create("Teleporter",c);
        [MenuItem("GameObject/GDD 110/Create/Next Scene Portal",false,23)]static void C13(MenuCommand c)=>Create("Next Scene Portal",c);
        [MenuItem("GameObject/GDD 110/Create/Spawner",false,24)]static void C14(MenuCommand c)=>Create("Spawner",c);
        [MenuItem("GameObject/GDD 110/Create/Companion",false,25)]static void C15(MenuCommand c)=>Create("Companion",c);
        [MenuItem("GameObject/GDD 110/Create/Enemy",false,26)]static void C16(MenuCommand c)=>Create("Enemy",c);
        [MenuItem("GameObject/GDD 110/Create/Timer",false,27)]static void C17(MenuCommand c)=>Create("Timer",c);
        [MenuItem("GameObject/GDD 110/Create/Rotating Object",false,28)]static void C18(MenuCommand c)=>Create("Rotating Object",c);
        [MenuItem("GameObject/GDD 110/Create/Player Rig",false,29)]static void C19(MenuCommand c)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Course Player Rig.prefab");
            if(prefab==null){EditorUtility.DisplayDialog("Player rig","Run Tools > GDD 110 > Prepare Course Project once to create the course rig.","OK");return;}
            var existing=UnityEngine.Object.FindAnyObjectByType<GddActor>();
            if(existing!=null){Selection.activeGameObject=existing.gameObject;Debug.Log("This scene already has a GDD player. Selected it instead of adding duplicate cameras.");return;}
            var cameras=UnityEngine.Object.FindObjectsByType<Camera>();
            var systems=UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>();
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);Undo.RegisterCreatedObjectUndo(go,"Create player rig");
            var actor=go.GetComponentInChildren<GddActor>();
            if(actor!=null){Vector3 point=SceneView.lastActiveSceneView!=null?SceneView.lastActiveSceneView.pivot:Vector3.zero;go.transform.position+=point+Vector3.up-actor.transform.position;}
            foreach(var old in cameras){Undo.RecordObject(old,"Use player camera");old.enabled=false;var audio=old.GetComponent<AudioListener>();if(audio!=null){Undo.RecordObject(audio,"Use player audio");audio.enabled=false;}}
            var rigSystem=go.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true);
            if(rigSystem!=null)foreach(var old in systems){Undo.RecordObject(old,"Use player UI input");old.enabled=false;}
            if(rigSystem==null&&systems.Length==0)
            {
                var ui=new GameObject("EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(ui,"Add player UI input");ui.transform.SetParent(go.transform,false);
                ui.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
            }
            Selection.activeGameObject=go;EditorSceneManager.MarkSceneDirty(go.scene);
        }
        public static GameObject Target(MenuCommand command)=>command?.context as GameObject ?? Selection.activeGameObject;
        public static T Add<T>(GameObject go) where T:Component {var existing=go.GetComponent<T>();return existing!=null?existing:Undo.AddComponent<T>(go);}
        public static void AddFunction(GameObject go,Type type)
        {
            if(go==null)return;
            if(typeof(GddTrigger).IsAssignableFrom(type))
            {
                var zone=new GameObject("GDD Trigger Zone");Undo.RegisterCreatedObjectUndo(zone,"Add trigger zone");GameObjectUtility.SetParentAndAlign(zone,go);
                zone.AddComponent(type);Volume(zone);zone.GetComponent<BoxCollider>().size=Vector3.one*2;Selection.activeGameObject=zone;return;
            }
            var component=Undo.AddComponent(go,type);
            if(component is GddPickup){if(go.GetComponent<Collider>()==null)Undo.AddComponent<BoxCollider>(go);var rb=Add<Rigidbody>(go);rb.isKinematic=true;rb.useGravity=false;}
            if(component is GddInteractable&&go.GetComponent<Collider>()==null)Undo.AddComponent<BoxCollider>(go);
            if(component is GddPower power)power.definition=Definition(GddPowerKind.EventsOnly);
            if(component is GddSpawn spawn)spawn.prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Spawned Cube.prefab");
            EditorSceneManager.MarkSceneDirty(go.scene);
        }
        [MenuItem("GameObject/GDD 110/Add Function/Choose Function...",false,50)]static void Palette(MenuCommand c)=>GddFunctionPalette.Open(Target(c));
        [MenuItem("GameObject/GDD 110/Add Function/Interact with E or Controller X",false,51)]static void A0(MenuCommand c)=>AddFunction(Target(c),typeof(GddInteractable));
        [MenuItem("GameObject/GDD 110/Add Function/Trigger Zone (child)",false,52)]static void A1(MenuCommand c)=>AddFunction(Target(c),typeof(GddTrigger));
        [MenuItem("GameObject/GDD 110/Add Function/Pickup and Attach",false,53)]static void A2(MenuCommand c)=>AddFunction(Target(c),typeof(GddPickup));
        [MenuItem("GameObject/GDD 110/Add Function/Power",false,54)]static void A3(MenuCommand c)=>AddFunction(Target(c),typeof(GddPower));
        [MenuItem("GameObject/GDD 110/Add Function/Health",false,55)]static void A4(MenuCommand c)=>AddFunction(Target(c),typeof(GddHealth));
        [MenuItem("GameObject/GDD 110/Add Function/Timer",false,56)]static void A5(MenuCommand c)=>AddFunction(Target(c),typeof(GddTimer));
        [MenuItem("GameObject/GDD 110/Add Function/Spin or Bob",false,57)]static void A6(MenuCommand c)=>AddFunction(Target(c),typeof(GddMotion));
        [MenuItem("GameObject/GDD 110/Add Function/Collision Events",false,58)]static void A7(MenuCommand c)=>AddFunction(Target(c),typeof(GddCollisionEvents));
        [MenuItem("GameObject/GDD 110/Add Function/Keyboard and Controller Events",false,59)]static void A8(MenuCommand c)=>AddFunction(Target(c),typeof(GddButtonEvents));
        [MenuItem("GameObject/GDD 110/Add Function/Scene Actions",false,60)]static void A9(MenuCommand c)=>AddFunction(Target(c),typeof(GddSceneActions));
        [MenuItem("GameObject/GDD 110/Set Up Player Interactions",false,80)]static void Player(MenuCommand c){var go=Target(c);if(go!=null)PreparePlayer(go);}
        public static void PreparePlayer(GameObject go)
        {
            var controller=go.GetComponentInChildren<StarterAssets.ThirdPersonController>();if(controller!=null)go=controller.gameObject;
            Add<GddRig>(go).AutoMap();Add<GddActor>(go);Add<GddHealth>(go);Add<GddLegacyInputBridge>(go);var input=Add<GddInteractor>(go);
            if(input.promptText==null)
            {
                var canvasObject=new GameObject("GDD Interaction Help",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
                Undo.RegisterCreatedObjectUndo(canvasObject,"Add interaction help");canvasObject.transform.SetParent(go.transform,false);var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
                var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=0.5f;
                var label=new GameObject("Controls and Nearby Object",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(canvasObject.transform,false);
                var rt=label.GetComponent<RectTransform>();rt.anchorMin=new Vector2(0,1);rt.anchorMax=new Vector2(1,1);rt.pivot=new Vector2(0,1);rt.offsetMin=new Vector2(24,-104);rt.offsetMax=new Vector2(-24,-16);
                input.promptText=label.GetComponent<TextMeshProUGUI>();input.promptText.fontSize=20;input.promptText.color=Color.white;input.promptText.raycastTarget=false;
            }
            EditorUtility.SetDirty(go);EditorSceneManager.MarkSceneDirty(go.scene);
        }
    }
}
