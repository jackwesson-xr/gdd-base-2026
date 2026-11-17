using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GDD110
{
    public sealed class GddOpening : MonoBehaviour
    {
        public GddCharacterProfile character;
        public Material stageMaterial;
        public Sprite loadingImage;
        public string title = "GDD 6.6";
        public bool playIntroImmediately;
        public bool returnToTitle;
        const string Playground = "The Fourth Bell";
        static readonly Color Ink = new Color(.055f,.09f,.074f);
        static readonly Color Ice = new Color(.66f,.91f,.78f);
        static readonly Color Paper = new Color(.92f,.96f,.91f);
        const string Showcase = "Block Showcase";
        Camera shot;
        Transform performer;
        Animator animator;
        CanvasGroup menu;
        CanvasGroup cover;
        Button start;
        Button showcase;
        EventSystem events;
        bool ready, started;
        Mesh treeSilhouette;

        IEnumerator Start()
        {
            Cursor.lockState=CursorLockMode.None;
            Cursor.visible=true;
            SetupUi();
            SetupStage();
            // Draw the loading card before constructing the character.
            yield return null;
            animator=character.CreateVisual(performer,1.85f,0,Vector3.zero);
            animator.updateMode=AnimatorUpdateMode.UnscaledTime;
            animator.SetBool("Grounded",true);
            yield return null;
            yield return Fade(cover,0,.38f);
            cover.gameObject.SetActive(false);
            ready=true;
            if(playIntroImmediately) Begin();
            else
            {
                start.interactable = true;
                if (showcase != null) showcase.interactable = true;
                events.SetSelectedGameObject(start.gameObject);
            }
        }

        void LateUpdate()
        {
            if(ready && !started && performer!=null)
                performer.rotation=Quaternion.Euler(0,18+Mathf.Sin(Time.unscaledTime*1.25f)*4,0);
        }

        public void Begin()
        {
            if(!ready || started) return;
            started=true;
            if(returnToTitle) SceneManager.LoadScene(0);
            else StartCoroutine(Opening(Playground));
        }

        void OpenShowcase()
        {
            if (!ready || started) return;
            started = true;
            StartCoroutine(Opening(Showcase));
        }

        IEnumerator Opening(string destination)
        {
            menu.interactable=false;
            menu.blocksRaycasts=false;
            events.SetSelectedGameObject(null);
            var load = Application.CanStreamedLevelBeLoaded(destination)
                ? SceneManager.LoadSceneAsync(destination, LoadSceneMode.Single)
                : null;
            if(load==null)
            {
                Debug.LogError($"Opening scene destination is missing from the build: {destination}", this);
                started=false;
                menu.interactable=true;
                menu.blocksRaycasts=true;
                if(start!=null) events.SetSelectedGameObject(start.gameObject);
                yield break;
            }
            GddMusicPlayer.Instance?.FadeOutForIntro();
            load.allowSceneActivation=false;
            Vector3 from=performer.position, cam=shot.transform.position;
            var original=performer.rotation;
            var walk=Quaternion.LookRotation(new Vector3(.75f,0,-1));
            float elapsed=0;
            while(elapsed<2.25f)
            {
                elapsed+=Time.unscaledDeltaTime;
                float turn=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1.10f,elapsed));
                float stride=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.75f,2.25f,elapsed));
                float frame=Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/2.25f));
                menu.alpha=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(elapsed/.5f));
                performer.rotation=Quaternion.Slerp(original,walk,turn);
                performer.position=from+walk*Vector3.forward*(1.85f*stride);
                animator.SetFloat("Speed",Mathf.SmoothStep(0,2,Mathf.InverseLerp(.55f,1.15f,elapsed)));
                animator.SetFloat("MotionSpeed",1);
                shot.transform.position=Vector3.Lerp(cam,new Vector3(-.7f,1.62f,3.22f),frame);
                shot.transform.LookAt(Vector3.Lerp(new Vector3(0,.94f,0),performer.position+Vector3.up,frame));
                yield return null;
            }

            var curtain=MakeCanvas("Scene transition",110);
            var veil=Panel(curtain.transform,"Transition curtain",Ink).gameObject.AddComponent<CanvasGroup>();
            veil.alpha=0;
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(curtain.gameObject);
            yield return Fade(veil,1,.26f);
            load.allowSceneActivation=true;
            while(!load.isDone) yield return null;
            yield return null;
            yield return Fade(veil,0,.42f);
            Destroy(curtain.gameObject);
            Destroy(gameObject);
        }

        void SetupStage()
        {
            var stage=new GameObject("Opening stage");
            var cameraObject=new GameObject("Opening camera",typeof(Camera),typeof(AudioListener));
            cameraObject.transform.SetParent(stage.transform,false);
            shot=cameraObject.GetComponent<Camera>();
            cameraObject.tag="MainCamera";
            shot.clearFlags=CameraClearFlags.SolidColor;
            shot.backgroundColor=new Color(.17f,.24f,.20f);
            shot.fieldOfView=36;
            shot.farClipPlane=100;
            shot.transform.position=new Vector3(0,1.43f,4.6f);
            shot.transform.LookAt(new Vector3(0,.94f,0));
            // The camera looks along -Z, so negative world X is screen right.
            performer=new GameObject("Title character").transform;
            performer.SetParent(stage.transform,false);
            performer.position=new Vector3(-.83f,0,0);
            performer.rotation=Quaternion.Euler(0,18,0);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name="Studio floor";
            floor.transform.SetParent(stage.transform,false);
            floor.transform.position=Vector3.down*.035f;
            floor.transform.localScale=new Vector3(3,1,3);
            floor.GetComponent<Renderer>().sharedMaterial=stageMaterial;
            DressStage(stage.transform);
            var plate=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plate.name="Character platform";
            plate.transform.SetParent(stage.transform,false);
            plate.transform.position=new Vector3(-.83f,-.027f,0);
            plate.transform.localScale=new Vector3(1.95f,.028f,1.95f);
            plate.GetComponent<Renderer>().sharedMaterial=character.trim;
            var primitiveCollider = plate.GetComponent<Collider>();
            primitiveCollider.enabled = false;
            Destroy(primitiveCollider);
            plate.AddComponent<MeshCollider>().sharedMesh = plate.GetComponent<MeshFilter>().sharedMesh;
            Key(stage.transform,"Key",new Vector3(38,155,0),new Color(1f,.84f,.69f),2.2f);
            Key(stage.transform,"Rim",new Vector3(26,-38,0),new Color(.62f,.97f,.78f),1.35f);
        }

        void DressStage(Transform stage)
        {
            var dark = new Color(.12f,.20f,.16f);
            var pine = new Color(.22f,.32f,.25f);
            var copper = new Color(.62f,.36f,.22f);
            var moon = StageProp(stage, "Moon window", PrimitiveType.Cylinder,
                new Vector3(-3.45f,2.55f,-6.45f), new Vector3(1.5f,.04f,1.5f), Ice);
            moon.localRotation = Quaternion.Euler(90,0,0);
            StageProp(stage, "Painted backdrop", PrimitiveType.Cube,
                new Vector3(0,2.6f,-7), new Vector3(17f,5.7f,.2f), dark);
            StageProp(stage, "Low backdrop", PrimitiveType.Cube,
                new Vector3(.3f,.24f,-4.7f), new Vector3(14f,.65f,.24f), pine);
            treeSilhouette = new Mesh { name = "Title pine silhouette" };
            treeSilhouette.vertices = new[]
            {
                new Vector3(-.5f,0,0), new Vector3(.5f,0,0), new Vector3(0,.65f,0),
                new Vector3(-.4f,.24f,0), new Vector3(.4f,.24f,0), new Vector3(0,.85f,0),
                new Vector3(-.3f,.47f,0), new Vector3(.3f,.47f,0), new Vector3(0,1,0)
            };
            treeSilhouette.triangles = new[] {0,1,2, 3,4,5, 6,7,8};
            treeSilhouette.RecalculateNormals();
            treeSilhouette.RecalculateBounds();
            for (int i=0;i<10;i++)
            {
                float x=-6.6f+i*1.49f;
                float h=1.15f+Mathf.PerlinNoise(i*.71f,3.1f)*2.2f;
                var tree=StageProp(stage,"Pine silhouette",PrimitiveType.Cube,new Vector3(x,.14f,-6.35f+(i%3)*.42f),
                    new Vector3(h*.57f,h,1),i%3==0?pine:dark);
                tree.GetComponent<MeshFilter>().sharedMesh=treeSilhouette;
            }
            for(int side=-1;side<=1;side+=2)
            {
                float x=-.83f+side*1.8f;
                StageProp(stage,"Copper stage column",PrimitiveType.Cube,new Vector3(x,1.65f,-2.35f),
                    new Vector3(.14f,3.3f,.15f),copper);
                StageProp(stage,"Signal lantern",PrimitiveType.Cube,new Vector3(x,2.24f,-2.23f),
                    new Vector3(.23f,.56f,.12f),Ice);
            }
            StageProp(stage,"Stage header",PrimitiveType.Cube,new Vector3(-.83f,3.25f,-2.35f),
                new Vector3(3.7f,.16f,.15f),copper);
        }

        void OnDestroy()
        {
            if (treeSilhouette) Destroy(treeSilhouette);
        }

        Transform StageProp(Transform stage, string label, PrimitiveType shape, Vector3 position,
            Vector3 scale, Color color)
        {
            var prop=GameObject.CreatePrimitive(shape);
            prop.name=label;
            prop.transform.SetParent(stage,false);
            prop.transform.localPosition=position;
            prop.transform.localScale=scale;
            prop.GetComponent<Collider>().enabled=false;
            var paint=new MaterialPropertyBlock();
            paint.SetColor("_BaseColor",color);
            paint.SetColor("_Color",color);
            var renderer=prop.GetComponent<Renderer>();
            renderer.sharedMaterial=stageMaterial;
            renderer.SetPropertyBlock(paint);
            return prop.transform;
        }

        static void Key(Transform parent,string name,Vector3 angles,Color color,float intensity)
        {
            var g=new GameObject(name,typeof(Light));
            g.transform.SetParent(parent,false);
            g.transform.rotation=Quaternion.Euler(angles);
            var light=g.GetComponent<Light>();
            light.type=LightType.Directional; light.color=color; light.intensity=intensity;
        }

        void SetupUi()
        {
            var canvas=MakeCanvas("Title overlay",0);
            var eventObject=new GameObject("Opening input",typeof(EventSystem),typeof(InputSystemUIInputModule));
            events=eventObject.GetComponent<EventSystem>();
            eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            menu=Panel(canvas.transform,"Menu",Color.clear).gameObject.AddComponent<CanvasGroup>();
            menu.GetComponent<Image>().raycastTarget=false;
            if(!playIntroImmediately)
            {
                var dock=Panel(menu.transform,"Menu panel",new Color(.050f,.082f,.068f,.94f));
                dock.raycastTarget=false;
                Anchor(dock.rectTransform,new Vector2(.055f,.20f),new Vector2(.432f,.80f));
                var kicker = TextAt(dock.transform, title + "  /  GAME DESIGN 110", 17, Ice, TextAlignmentOptions.Left);
                kicker.fontStyle = FontStyles.Bold;
                Anchor(kicker.rectTransform, new Vector2(.09f,.86f), new Vector2(.94f,.95f));
                var heading = TextAt(dock.transform, returnToTitle ? "Crossing complete" : "The Fourth Bell", 54, Paper, TextAlignmentOptions.Left);
                GddHudFonts.Apply(heading, GddHudFonts.Title);
                Anchor(heading.rectTransform, new Vector2(.085f,.57f), new Vector2(.94f,.85f));
                var subtitle = TextAt(dock.transform,
                    returnToTitle ? "The Bell Moth is home." : "An after-hours mystery", 23, Paper, TextAlignmentOptions.Left);
                Anchor(subtitle.rectTransform, new Vector2(.09f,.46f), new Vector2(.94f,.61f));
                start = AddButton(dock.transform, returnToTitle ? "Return to title" : "Start game",
                    new Vector2(.09f,.25f), new Vector2(.91f,.41f), true, Begin);
                if (!returnToTitle && Application.CanStreamedLevelBeLoaded(Showcase))
                    showcase = AddButton(dock.transform, "Block Showcase",
                        new Vector2(.09f,.07f), new Vector2(.91f,.23f), false, OpenShowcase);
            }
            var placeholder=Panel(canvas.transform,"Loading character",Ink);
            cover=placeholder.gameObject.AddComponent<CanvasGroup>();
            if(loadingImage!=null)
            {
                var image=Panel(placeholder.transform,"Placeholder artwork",Color.white);
                image.sprite=loadingImage; image.preserveAspect=true;
            }
            var loading=TextAt(placeholder.transform,"GDD 110  /  LOADING CHARACTER",20,Ice,TextAlignmentOptions.Center);
            Anchor(loading.rectTransform,new Vector2(.1f,.06f),new Vector2(.9f,.15f));
        }

        static Button AddButton(Transform parent, string label, Vector2 min, Vector2 max,
            bool primary, UnityEngine.Events.UnityAction click)
        {
            var background = Panel(parent, label, primary ? Ice : new Color(.17f,.255f,.198f));
            Anchor(background.rectTransform, min, max);
            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.highlightedColor = new Color(.88f,1f,1f);
            colors.selectedColor = new Color(.81f,.95f,1f);
            colors.pressedColor = new Color(.65f,.83f,.88f);
            button.colors = colors;
            button.onClick.AddListener(click);
            button.interactable = false;
            var text = TextAt(background.transform, label, 26, primary ? Ink : Paper, TextAlignmentOptions.Left);
            text.fontStyle = FontStyles.Bold;
            Anchor(text.rectTransform, new Vector2(.08f,0), new Vector2(.90f,1));
            TextAt(background.transform, ">", 32, primary ? Ink : Paper, TextAlignmentOptions.Center)
                .rectTransform.anchorMin = new Vector2(.83f,0);
            return button;
        }

        static Canvas MakeCanvas(string name,int order)
        {
            var g=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=g.GetComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder=order;
            var scaler=g.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1280,720);
            scaler.matchWidthOrHeight=.5f;
            return canvas;
        }
        static Image Panel(Transform parent,string name,Color color)
        {
            var g=new GameObject(name,typeof(RectTransform),typeof(Image));
            g.transform.SetParent(parent,false);
            var i=g.GetComponent<Image>(); i.color=color;
            Anchor(i.rectTransform,Vector2.zero,Vector2.one);
            return i;
        }
        static TextMeshProUGUI TextAt(Transform parent,string value,float size,Color color,TextAlignmentOptions align)
        {
            var g=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));
            g.transform.SetParent(parent,false);
            var t=g.GetComponent<TextMeshProUGUI>();
            t.text=value; t.fontSize=size; t.color=color; t.alignment=align; t.raycastTarget=false;
            t.enableAutoSizing=true; t.fontSizeMin=size*.48f; t.fontSizeMax=size;
            GddHudFonts.Apply(t, GddHudFonts.Body);
            t.fontStyle = FontStyles.Normal;
            Anchor(t.rectTransform,Vector2.zero,Vector2.one);
            return t;
        }
        static void Anchor(RectTransform rect,Vector2 min,Vector2 max)
        {
            rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
        static IEnumerator Fade(CanvasGroup group,float alpha,float duration)
        {
            float initial=group.alpha;
            for(float time=0;time<duration;time+=Time.unscaledDeltaTime)
            {
                group.alpha=Mathf.Lerp(initial,alpha,Mathf.SmoothStep(0,1,time/duration));
                yield return null;
            }
            group.alpha=alpha;
        }
    }
}
