using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Kairisei.Recovery
{
    // Rebuilds the recorded NGUI scene hierarchy, rather than arranging a new screen.
    // Missing model/background dependencies keep home unavailable; there is no dashboard fallback.
    public sealed class OriginalSceneRenderer : MonoBehaviour
    {
        public const string BundleName = "recovery/scenes.dat";
        private JObject layout;
        private RecoveryContentLoader content;
        private RecoveryBootstrap owner;
        private Camera sceneCamera;
        private Shader spriteShader;
        private Font originalFont;
        private GameObject sceneRoot;
        private string currentScene;
        private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private readonly List<Widget> widgets = new List<Widget>();
        private readonly Dictionary<string, GameObject> objects = new Dictionary<string, GameObject>();
        private TouchScreenKeyboard nameKeyboard;
        private Camera worldCamera;
        private GameObject worldRoot;
        private OriginalNaviModel navi;
        private GameObject titleCover;
        private Action dialogAction;
        private string returnScene;
        private string dialogBody;
        private bool displayedBonus;
        private bool homeResourcesReady;
        private bool rebuildingFont;
        private readonly List<Mesh> ownedMeshes=new List<Mesh>();
        private readonly List<Material> ownedMaterials=new List<Material>();
        public bool CanPresentHome { get { return layout != null && homeResourcesReady && (bool?)layout["originalSceneResourcesPrepared"] == true; } }
        public bool Initialized { get { return layout != null; } }

        private sealed class Widget
        {
            public GameObject Object;
            public JObject Data;
            public JObject Node;
            public GlyphLabel Label;
            public int Depth;
            public Action Click;
            public MeshRenderer Renderer;
            public Color SourceColor;
            public Mesh ImageMesh;
        }

        private sealed class GlyphLabel
        {
            public Font Font;
            public Mesh Mesh;
            public Color Color;
            public int Pivot;
            public float MaxWidth;
            private string value = "";
            public string text { get { return value; } set { if (this.value == value) return; this.value = value ?? ""; Render(); } }
            public void Render()
            {
                // NGUI labels use font atlas glyphs on a mesh, not a replacement TextMesh component.
                string visible = Regex.Replace(value, @"\[(?:[a-fA-F0-9]{6}|[a-fA-F0-9]{8}|-)\]", "").Replace('\r','\n');
                Font.RequestCharactersInTexture(visible,64,FontStyle.Normal);
                var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>(); var colors = new List<Color>();
                float x = 0, y = 0, maxX = 0, minY = 0, maxY = 0;
                foreach (char character in visible)
                {
                    if (character == '\n') { maxX = Mathf.Max(maxX,x); x = 0; y -= 1.2f; continue; }
                    CharacterInfo info;
                    if (!Font.GetCharacterInfo(character,out info,64,FontStyle.Normal)) continue;
                    if(MaxWidth>0&&x+info.advance/64f>MaxWidth){maxX=Mathf.Max(maxX,x);x=0;y-=1.2f;}
                    int start = vertices.Count;
                    float left = x + info.minX/64f, right = x + info.maxX/64f, bottom = y + info.minY/64f, top = y + info.maxY/64f;
                    vertices.Add(new Vector3(left,bottom,0)); vertices.Add(new Vector3(right,bottom,0)); vertices.Add(new Vector3(left,top,0)); vertices.Add(new Vector3(right,top,0));
                    uv.Add(info.uvBottomLeft); uv.Add(info.uvBottomRight); uv.Add(info.uvTopLeft); uv.Add(info.uvTopRight);
                    triangles.AddRange(new[] {start,start+2,start+1,start+2,start+3,start+1}); colors.AddRange(new[] {Color,Color,Color,Color});
                    x += info.advance/64f; minY = Mathf.Min(minY,bottom); maxY = Mathf.Max(maxY,top);
                }
                maxX = Mathf.Max(maxX,x);
                Vector2 pivot = OriginalSceneRenderer.Pivot(Pivot);
                Vector3 offset = new Vector3(-maxX*pivot.x,-minY-(maxY-minY)*pivot.y,0);
                for (int i=0;i<vertices.Count;i++) vertices[i] += offset;
                Mesh.Clear(); Mesh.SetVertices(vertices); Mesh.SetUVs(0,uv); Mesh.SetTriangles(triangles,0); Mesh.SetColors(colors); Mesh.RecalculateBounds();
            }
        }

        public void Initialize(RecoveryContentLoader loader, Font font, RecoveryBootstrap controller)
        {
            content = loader; originalFont = font; owner = controller;
            layout = JObject.Parse(content.Load<TextAsset>(BundleName, "originalscenes").text);
            if ((int?)layout["schemaVersion"] != 1 || !(layout["definitions"] is JArray)) throw new FormatException("原场景布局数据无效。");
            if ((bool?)layout["originalSceneResourcesPrepared"] != true) throw new InvalidOperationException("主页原场景资源尚未准备完成。");
            OriginalNaviModel.ValidateContent(content);
            homeResourcesReady = true;
            spriteShader = content.Load<Shader>(BundleName, "original-scene-sprite");
            GameObject cameraObject = new GameObject("OriginalSceneCamera");
            cameraObject.transform.SetParent(transform, false);
            sceneCamera = cameraObject.AddComponent<Camera>();
            sceneCamera.orthographic = true; sceneCamera.orthographicSize = 1;
            sceneCamera.transform.localPosition = new Vector3(0, 0, -100);
            sceneCamera.clearFlags = CameraClearFlags.Depth; sceneCamera.depth = 50;
            sceneCamera.cullingMask = 1 << 29;
            Font.textureRebuilt += RebuildFont;
            Show("Title");
        }

        public void Show(string name)
        {
            if (layout == null || currentScene == name) return;
            if (name == "Home" && !CanPresentHome) throw new InvalidOperationException("主页背景与导航角色资源尚未完整就绪。");
            JObject definition = null;
            foreach (JObject item in (JArray)layout["definitions"]) if ((string)item["name"] == name) definition = item;
            if (definition == null) throw new InvalidOperationException("缺少原场景：" + name);
            if (sceneRoot != null) Destroy(sceneRoot);
            foreach(Mesh mesh in ownedMeshes)Destroy(mesh);ownedMeshes.Clear();
            foreach(Material material in ownedMaterials)Destroy(material);ownedMaterials.Clear();
            objects.Clear(); widgets.Clear();
            sceneRoot = new GameObject("Restored_" + name); sceneRoot.transform.SetParent(transform, false);
            currentScene = name;
            Build(definition, sceneRoot.transform);
            if(titleCover!=null)Destroy(titleCover);
            if(name=="Title")BuildTitleCover();
            if (name == "Home")
            {
                BuildHomeWorld();
                foreach (JObject item in (JArray)layout["definitions"])
                    if ((string)item["name"] == "Menu") Build(item, sceneRoot.transform);
            }
            else if(worldRoot!=null)worldRoot.SetActive(false);
        }

        private void Build(JObject definition, Transform parent)
        {
            string scene = (string)definition["name"];
            var localObjects = new Dictionary<string, GameObject>();
            foreach (JObject node in (JArray)definition["nodes"])
            {
                string id = (string)node["id"];
                GameObject item = new GameObject((string)node["name"]); item.layer = 29;
                localObjects.Add(id, item); objects[scene + ":" + id] = item;
            }
            foreach (JObject node in (JArray)definition["nodes"])
            {
                GameObject item = localObjects[(string)node["id"]];
                string parentId = (string)node["parent"];
                item.transform.SetParent(parentId == null ? parent : localObjects[parentId].transform, false);
                item.transform.localPosition = Vector(node["position"]);
                item.transform.localScale = Vector(node["scale"]);
                JArray rotation = (JArray)node["rotation"];
                item.transform.localRotation = new Quaternion((float)rotation[0], (float)rotation[1], (float)rotation[2], (float)rotation[3]);
                item.SetActive((bool)node["active"]);
                    // Preserve navigation buttons; submenu groups are selected by Menu_Item.
                foreach (JObject script in (JArray)node["scripts"])
                {
                    string type = (string)script["type"];
                    // Exported NGUI prefabs park UIRoot at y=-10000 while they
                    // are templates. The original UIRoot component restores it
                    // when instantiated; this reconstruction has to do that
                    // explicitly or the whole authored interface stays offscreen.
                    if (type == "UIRoot")
                    {
                        Vector3 position = item.transform.localPosition;
                        item.transform.localPosition = new Vector3(position.x, 0, position.z);
                    }
                    if ((string)script["method"] == "changeSDKID") item.SetActive(false);
                }
                if (node["anchor"] is JObject)
                {
                    int side = (int)node["anchor"]["side"];
                    float height = 640;
                    JArray pixels = (JArray)node["anchor"]["pixels"], relative = (JArray)node["anchor"]["relative"];
                    Vector3 position = item.transform.localPosition;
                    int[] horizontal={-1,-1,-1,0,1,1,1,0,0},vertical={-1,0,1,1,1,0,-1,-1,0};
                    position.x = horizontal[side] * height * sceneCamera.aspect / 2 + (float)pixels[0] + (float)relative[0] * height * sceneCamera.aspect;
                    position.y = vertical[side] * height / 2 + (float)pixels[1] + (float)relative[1] * height;
                    item.transform.localPosition = position;
                }
                foreach (JObject data in (JArray)node["widgets"])
                {
                    if (!(bool)data["enabled"]) continue;
                    var widget = new Widget { Object = item, Node = node, Data = data, Depth = (int)data["depth"],SourceColor=ColorValue(data["color"]) };
                    if ((string)data["kind"] == "UILabel") BuildLabel(widget);
                    else if (data["resolved"] is JObject) BuildImage(widget);
                    else continue; // Dynamic slots stay unresolved; readiness is recorded separately.
                    widgets.Add(widget);
                }
            }
            // Apply authored enter end-poses: serialized scene positions can be offscreen.
            foreach(JObject node in (JArray)definition["nodes"])
            {
                JObject animation=node["enterAnimation"] as JObject;
                if(animation==null)continue;
                Transform origin=localObjects[(string)node["id"]].transform;
                foreach(string kind in new[]{"position","scale"})foreach(JObject curve in (JArray)animation[kind])
                {
                    string hierarchy=(string)curve["path"];
                    Transform target=String.IsNullOrEmpty(hierarchy)?origin:origin.Find(hierarchy);
                    JArray keys=(JArray)curve["keys"];
                    if(target==null||keys.Count==0)continue;
                    Vector3 value=Vector(keys[keys.Count-1]["value"]);
                    if(kind=="position")target.localPosition=value;else target.localScale=value;
                }
            }
            if(scene=="Confirm"||scene=="ServerList"||scene=="Message"||scene=="Download")
                foreach(GameObject node in localObjects.Values)if(node.transform.parent==parent)node.transform.localScale*=2f/640f;
            if(scene=="Menu")foreach(JObject subgroup in (JArray)definition["menuSubgroups"])
                foreach(string id in ((JArray)subgroup["groups"]).Values<string>())
                    if(localObjects.ContainsKey(id))localObjects[id].SetActive((string)subgroup["type"]=="Menu_HomeItem");
            if(scene=="IntroSlPl")SelectRole(0);
            if(scene=="Title")foreach(Widget widget in widgets)if(localObjects.ContainsValue(widget.Object)&&widget.Object.name=="Bg_Title00")widget.Object.SetActive(false);
            foreach (Widget widget in widgets)
                if(localObjects.ContainsValue(widget.Object))BindAction(widget, definition, localObjects);
        }

        private void BuildImage(Widget widget)
        {
            JObject source = (JObject)widget.Data["resolved"];
            string key = (string)source["texture"];
            Texture2D texture;
            if (!textures.TryGetValue(key, out texture)) { texture = content.Load<Texture2D>(BundleName, key); textures.Add(key, texture); }
            JArray uv = (JArray)source["uv"];
            Vector2 pivot = Pivot((int)widget.Data["pivot"]);
            var mesh = new Mesh();
            ownedMeshes.Add(mesh);
            float x = (float)uv[0], y = (float)uv[1], w = (float)uv[2], h = (float)uv[3];
            if ((int?)widget.Data["spriteType"] == 1 && source["outer"] is JArray && source["inner"] is JArray)
                BuildSlicedImageMesh(mesh, pivot, widget.Object.transform.localScale, source, x, y, w, h, ColorValue(widget.Data["color"]));
            else
            {
                mesh.vertices = new[] { new Vector3(-pivot.x,-pivot.y,0), new Vector3(1-pivot.x,-pivot.y,0), new Vector3(-pivot.x,1-pivot.y,0), new Vector3(1-pivot.x,1-pivot.y,0) };
                mesh.uv = new[] { new Vector2(x,y),new Vector2(x+w,y),new Vector2(x,y+h),new Vector2(x+w,y+h) };
                mesh.triangles = new[] { 0,2,1,2,3,1 };
                Color color = ColorValue(widget.Data["color"]); mesh.colors = new[] { color,color,color,color };
                mesh.RecalculateBounds();
            }
            GameObject image = new GameObject("OriginalSprite_" + (string)widget.Data["id"]); image.layer = 29;
            image.transform.SetParent(widget.Object.transform, false);
            image.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = image.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(spriteShader) { mainTexture = texture };
            ownedMaterials.Add(renderer.sharedMaterial);
            renderer.sortingOrder = widget.Depth;
            widget.Renderer=renderer;
            widget.ImageMesh=mesh;
        }

        private void BuildLabel(Widget widget)
        {
            GameObject label = new GameObject("OriginalLabel_" + (string)widget.Data["id"]); label.layer = 29;
            label.transform.SetParent(widget.Object.transform, false);
            var mesh = new Mesh(); label.AddComponent<MeshFilter>().sharedMesh = mesh;
            ownedMeshes.Add(mesh);
            MeshRenderer renderer = label.AddComponent<MeshRenderer>(); renderer.sharedMaterial = originalFont.material; renderer.sortingOrder = widget.Depth;
            var text = new GlyphLabel {Font=originalFont,Mesh=mesh,Color=ColorValue(widget.Data["color"]),Pivot=(int)widget.Data["pivot"],MaxWidth=(float?)widget.Data["maxLineWidth"]??0};
            text.text = (string)widget.Data["text"] ?? "";
            widget.Label = text;
            widget.Renderer=renderer;
        }

        private void BindAction(Widget widget, JObject definition, Dictionary<string,GameObject> page)
        {
            JObject node = widget.Node;
            while (node != null)
            {
                foreach (JObject script in (JArray)node["scripts"])
                {
                    string method = (string)script["method"];
                    int role = (int?)script["role"] ?? 0;
                    if (method == "OnStartLogin") widget.Click = owner.BeginFromOriginalTitle;
                    if (method == "showServerListWindow") widget.Click = delegate { Show("ServerList"); };
                    if ((string)script["type"] == "IntroSlPlChr" && role >= 1 && role <= 4)
                    {
                        int choice = role; widget.Click = delegate { owner.SelectOriginalArthur(choice);SelectRole(choice); };
                    }
                    if (method == "OnClickOK") widget.Click = ShowNameInput;
                    if(method=="OnClick"&&(string)script["type"]=="UIButtonMessage"&&widget.Object.name=="Back")widget.Click=delegate{SelectRole(0);};
                    if ((string)script["type"] == "UIInput") widget.Click = OpenNameInput;
                    if(method=="OnCenterButton")widget.Click=owner.CreateOriginalCharacter;
                }
                string objectName=(string)node["name"];
                if(currentScene=="Confirm"&&(objectName=="Btn01"||objectName=="Btn02"))
                {
                    bool accept=objectName=="Btn01";
                    widget.Click=delegate { Action action=accept?dialogAction:null;dialogAction=null;Show(returnScene??"Title");if(action!=null)action(); };
                }
                string parent = (string)node["parent"];
                node = null;
                if (parent != null) foreach (JObject candidate in (JArray)definition["nodes"]) if ((string)candidate["id"] == parent) node = candidate;
            }
            string name = widget.Object.name;
            if (currentScene == "ServerList" && name == "Btn01")
                widget.Click = delegate { Show("Title"); owner.BeginFromOriginalTitle(); };
        }

        private void OpenNameInput() { nameKeyboard = TouchScreenKeyboard.Open(owner.OriginalCharacterName, TouchScreenKeyboardType.Default); }

        private void Update()
        {
            if (layout == null) return;
            if (nameKeyboard != null && nameKeyboard.status == TouchScreenKeyboard.Status.Done)
            {
                owner.SetOriginalCharacterName(nameKeyboard.text); nameKeyboard = null;
            }
            bool dialog=currentScene=="Confirm"||currentScene=="Message";
            if (!dialog&&owner.OriginalEntryStep == CnEntryStep.Character && currentScene != "IntroSlPl") Show("IntroSlPl");
            if (!dialog&&owner.OriginalHome != null && CanPresentHome && currentScene != "Home") Show("Home");
            if(currentScene=="Home"&&!displayedBonus&&!String.IsNullOrEmpty(owner.OriginalBonusNotice))
            {
                displayedBonus=true;returnScene="Home";dialogBody=owner.OriginalBonusNotice;dialogAction=owner.AcknowledgeOriginalBonus;Show("Confirm");
            }
            foreach (Widget widget in widgets)
            {
                if (widget.Label == null) continue;
                if (widget.Object.name == "Label_Input01") widget.Label.text = owner.OriginalCharacterName;
                if(widget.Object.name=="Label_PreInput")widget.Object.SetActive(String.IsNullOrEmpty(owner.OriginalCharacterName));
                if (widget.Label.text.StartsWith("Ver.",StringComparison.Ordinal)) widget.Label.text = "Ver." + Application.version;
                if(currentScene=="Confirm")
                {
                    if(widget.Object.name=="Label_Text")widget.Label.text=dialogBody??"";
                    if(widget.Object.name=="Label_Title")widget.Label.text="确认";
                    if(widget.Object.name=="Label_01")widget.Label.text="继续";
                    if(widget.Object.name=="Label_02")widget.Label.text="返回";
                }
                if(currentScene=="ServerList")
                {
                    if(widget.Object.name=="Label_IP"||widget.Object.name=="Label_NowServer")widget.Label.text=owner.OriginalServerAddress;
                    if(widget.Object.name=="Label_Name")widget.Label.text="游戏服务器";
                    if(widget.Object.name=="Label_Vers")widget.Label.text=Application.version;
                }
                if(currentScene=="Home"&&owner.OriginalHome!=null)BindHomeLabel(widget);
            }
            if (owner.OriginalBusy) return;
            bool pressed = Input.GetMouseButtonDown(0);
            Vector2 screen = Input.mousePosition;
            if (Input.touchCount > 0) { Touch touch = Input.GetTouch(0); pressed = touch.phase == TouchPhase.Began; screen = touch.position; }
            if (!pressed) return;
            Ray ray = sceneCamera.ScreenPointToRay(screen);
            Widget hit = null;
            foreach (Widget widget in widgets)
            {
                if (widget.Click == null || !widget.Object.activeInHierarchy) continue;
                Plane plane = new Plane(widget.Object.transform.forward, widget.Object.transform.position);
                float distance; if (!plane.Raycast(ray,out distance)) continue;
                Vector3 point = widget.Object.transform.InverseTransformPoint(ray.GetPoint(distance));
                Vector2 pivot = Pivot((int)widget.Data["pivot"]);
                if (point.x < -pivot.x || point.x > 1-pivot.x || point.y < -pivot.y || point.y > 1-pivot.y) continue;
                if (hit == null || widget.Depth > hit.Depth) hit = widget;
            }
            if (hit != null) hit.Click();
        }

        private void SelectRole(int role)
        {
            if(currentScene!="IntroSlPl")return;
            foreach(JObject scene in (JArray)layout["definitions"])
            {
                if((string)scene["name"]!="IntroSlPl")continue;
                foreach(JObject node in (JArray)scene["nodes"])
                    foreach(JObject script in (JArray)node["scripts"])
                        if((string)script["type"]=="IntroSlPlChr")
                        {
                            GameObject item;if(objects.TryGetValue("IntroSlPl:"+(string)node["id"],out item))item.SetActive(role==0||(int)script["role"]==role);
                        }
                        else if((string)script["type"]=="IntroSlPlSideBtn"||(string)script["type"]=="IntroSlPlBackBtn"||(string)script["type"]=="IntroSlPlInfo")
                        {
                            GameObject item;if(objects.TryGetValue("IntroSlPl:"+(string)node["id"],out item))item.SetActive(role!=0);
                        }
                        else if((string)script["type"]=="IntroSlPlInfoChrDetail"||(string)script["type"]=="IntroSlPlInfoChr"||(string)script["type"]=="CmnInputWindow")
                        {
                            GameObject item;if(objects.TryGetValue("IntroSlPl:"+(string)node["id"],out item))item.SetActive(false);
                        }
            }
        }

        private void ShowNameInput()
        {
            foreach(JObject scene in (JArray)layout["definitions"])
                if((string)scene["name"]=="IntroSlPl")foreach(JObject node in (JArray)scene["nodes"])
                    foreach(JObject script in (JArray)node["scripts"])
                        if((string)script["type"]=="CmnInputWindow")
                        {
                            GameObject item;if(objects.TryGetValue("IntroSlPl:"+(string)node["id"],out item))item.SetActive(true);
                        }
            OpenNameInput();
        }

        public void ConfirmHomeEntry()
        {
            returnScene=currentScene;dialogBody="进入主页会读取角色状态、领取可用登录奖励，并结束服务器残留的单人开场。";
            dialogAction=owner.EnterOriginalHome;Show("Confirm");
        }
        public void NotifyHomeUpdated(){displayedBonus=false;}
        public void ShowError(string message)
        {
            if(currentScene=="Confirm"&&dialogBody==message)return;
            returnScene=currentScene=="Confirm"?returnScene:currentScene;dialogBody=message;
            dialogAction=owner.OriginalCanRetry?(Action)owner.RetryOriginalEntry:null;Show("Confirm");
        }
        private void BuildTitleCover()
        {
            string key=(string)layout["titleCover"];if(String.IsNullOrEmpty(key))throw new InvalidOperationException("原始标题贴图缺失。");
            titleCover=new GameObject("OriginalTitleCover");titleCover.layer=29;titleCover.transform.SetParent(transform,false);
            titleCover.transform.localScale=new Vector3(2*sceneCamera.aspect,2,1);
            var widget=new Widget{Object=titleCover,Node=new JObject(),Data=new JObject{{"id","title-cover"},{"pivot",4},{"color",new JArray(1,1,1,1)},{"resolved",new JObject{{"texture",key},{"uv",new JArray(0,0,1,1)}}}},Depth=-100};BuildImage(widget);
        }
        private void BuildHomeWorld()
        {
            if(worldRoot!=null){worldRoot.SetActive(true);return;}
            worldRoot=new GameObject("OriginalHomeWorld");worldRoot.transform.SetParent(transform,false);
            GameObject background=Instantiate(content.Load<GameObject>(OriginalNaviModel.BundleName,"home-background-01000"));background.transform.SetParent(worldRoot.transform,false);
            GameObject cameraObject=new GameObject("OriginalHomeCamera");cameraObject.transform.SetParent(worldRoot.transform,false);
            worldCamera=cameraObject.AddComponent<Camera>();worldCamera.transform.localPosition=new Vector3(0,1,-10);worldCamera.fieldOfView=60;worldCamera.orthographic=false;worldCamera.depth=0;
            worldCamera.clearFlags=CameraClearFlags.SolidColor;worldCamera.backgroundColor=Color.black;worldCamera.cullingMask=1<<28;
            foreach(Transform part in background.GetComponentsInChildren<Transform>(true))part.gameObject.layer=28;
            GameObject canvas=new GameObject("OriginalUathachCanvas");canvas.layer=28;canvas.transform.SetParent(worldRoot.transform,false);
            canvas.transform.localPosition=new Vector3(-2,0.2f,0);canvas.transform.localScale=new Vector3(0.45f,1,0.45f);canvas.transform.localRotation=Quaternion.Euler(-90,0,0);
            navi=canvas.AddComponent<OriginalNaviModel>();navi.Initialize(content,worldCamera);
        }
        private void BindHomeLabel(Widget widget)
        {
            CnHomeState home=owner.OriginalHome;
            JObject definition=null;foreach(JObject item in (JArray)layout["definitions"])if((string)item["name"]=="Menu")definition=item;
            JObject bindings=definition==null?null:definition["bindings"] as JObject;
            if(bindings!=null)
            {
                string id=(string)widget.Data["id"];
                if(id==(string)bindings["m_name_label"])widget.Label.text=home.Name;
                if(id==(string)bindings["m_bp_now_label"])widget.Label.text=home.Bp.ToString();
                if(id==(string)bindings["m_bp_max_label"])widget.Label.text=home.BpMax.ToString();
                if(id==(string)bindings["m_coin_label"])widget.Label.text=home.Gold.ToString();
                if(id==(string)bindings["m_ap_label"])widget.Label.text=home.Ap+"/"+home.ApMax;
                if(id==(string)bindings["m_bp_label"])widget.Label.text=home.Bp+"/"+home.BpMax;
                if(id==(string)bindings["m_menu_crystal_label"])
                {
                    JToken coin=home.Wire["user"]["coin"],free=home.Wire["user"]["coin_free"];
                    if(coin!=null&&free!=null)widget.Label.text=((long)coin+(long)free).ToString();
                }
            }
            switch(widget.Object.name)
            {
                case "Label_NumBp":widget.Label.text=home.Bp.ToString();break;
                case "Label_NumBpMax":widget.Label.text=home.BpMax.ToString();break;
                case "Label_Name":widget.Label.text=home.Name;break;
                case "Label_NumGold":widget.Label.text=home.Gold.ToString();break;
            }
        }
        private void RebuildFont(Font rebuilt)
        {
            if(rebuilt!=originalFont||rebuildingFont)return;rebuildingFont=true;
            try{foreach(Widget widget in widgets)if(widget.Label!=null)widget.Label.Render();}finally{rebuildingFont=false;}
        }
        private static void BuildSlicedImageMesh(Mesh mesh, Vector2 pivot, Vector3 objectScale, JObject source, float u, float v, float widthUv, float heightUv, Color color)
        {
            JArray outer=(JArray)source["outer"],inner=(JArray)source["inner"];
            float outerWidth=(float)outer[2],outerHeight=(float)outer[3];
            float left=(float)inner[0]-(float)outer[0],right=(float)outer[0]+outerWidth-(float)inner[0]-(float)inner[2];
            float top=(float)inner[1]-(float)outer[1],bottom=(float)outer[1]+outerHeight-(float)inner[1]-(float)inner[3];
            float targetWidth=Mathf.Max(0.0001f,Mathf.Abs(objectScale.x)),targetHeight=Mathf.Max(0.0001f,Mathf.Abs(objectScale.y));
            float horizontalScale=left+right>targetWidth?targetWidth/(left+right):1;
            float verticalScale=top+bottom>targetHeight?targetHeight/(top+bottom):1;
            float[] xs={-pivot.x,-pivot.x+left*horizontalScale/targetWidth,1-pivot.x-right*horizontalScale/targetWidth,1-pivot.x};
            float[] ys={-pivot.y,-pivot.y+bottom*verticalScale/targetHeight,1-pivot.y-top*verticalScale/targetHeight,1-pivot.y};
            float[] us={u,u+widthUv*left/outerWidth,u+widthUv*(outerWidth-right)/outerWidth,u+widthUv};
            float[] vs={v,v+heightUv*bottom/outerHeight,v+heightUv*(outerHeight-top)/outerHeight,v+heightUv};
            var vertices=new Vector3[16];var texcoords=new Vector2[16];var colors=new Color[16];var triangles=new int[54];
            for(int row=0;row<4;row++)for(int column=0;column<4;column++)
            {
                int index=row*4+column;vertices[index]=new Vector3(xs[column],ys[row],0);texcoords[index]=new Vector2(us[column],vs[row]);colors[index]=color;
            }
            int triangle=0;
            for(int row=0;row<3;row++)for(int column=0;column<3;column++)
            {
                int lower=row*4+column;triangles[triangle++]=lower;triangles[triangle++]=lower+4;triangles[triangle++]=lower+1;
                triangles[triangle++]=lower+4;triangles[triangle++]=lower+5;triangles[triangle++]=lower+1;
            }
            mesh.vertices=vertices;mesh.uv=texcoords;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();
        }
        private void OnDestroy(){Font.textureRebuilt-=RebuildFont;if(sceneRoot!=null)Destroy(sceneRoot);if(worldRoot!=null)Destroy(worldRoot);if(titleCover!=null)Destroy(titleCover);foreach(Mesh mesh in ownedMeshes)Destroy(mesh);foreach(Material material in ownedMaterials)Destroy(material);}
        private static Vector3 Vector(JToken values) { return new Vector3((float)values[0],(float)values[1],(float)values[2]); }
        private static Vector2 Pivot(int value) { return new Vector2((value % 3)*0.5f,1-(value/3)*0.5f); }
        private static Color ColorValue(JToken value) { return new Color((float)value[0],(float)value[1],(float)value[2],(float)value[3]); }
    }
}
