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
        private JObject motionData;
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
        private GameObject titleArtwork;
        private GameObject titlePrompt;
        private GameObject titleConnecting;
        private Transform titleHitTarget;
        private Vector3 titleHitCenter, titleHitSize;
        private bool titleWaiting;
        private float titleWaitingSince;
        private OriginalSceneMotion titleMotion;
        private Transform menuClipPanel, menuButtons, menuLeftArrow, menuRightArrow;
        private Rect menuClip, menuWorldClip;
        private float menuScrollMin, menuScrollMax;
        private bool draggingMenu;
        private bool menuDragged;
        private Vector2 menuTouchStart;
        private int menuTouchId;
        private static readonly int ClipRectId = Shader.PropertyToID("_ClipRect");
        private MaterialPropertyBlock menuClipProperties;
        private readonly List<OriginalSceneMotion> sceneMotions = new List<OriginalSceneMotion>();
        private readonly List<OriginalSceneMotion> dialogMotions = new List<OriginalSceneMotion>();
        private GameObject dialogRoot;
        private string currentDialog;
        private int firstDialogWidget, firstDialogMesh, firstDialogMaterial;
        private Action dialogAction;
        private string dialogTitle;
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
            public Color[] Tint;
            public string DisplayedFrame;
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
                string visible = Regex.Replace(value, @"\[(?:[a-fA-F0-9]{6}|[a-fA-F0-9]{8}|-)\]", "").Replace("\r\n", "\n").Replace('\r','\n');
                Font.RequestCharactersInTexture(visible,64,FontStyle.Normal);
                var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>(); var colors = new List<Color>();
                var lineWidths = new List<float>();
                var lineStarts = new List<int> { 0 };
                float x = 0, y = 0, minY = 0, maxY = 0;
                foreach (char character in visible)
                {
                    bool newline = character == '\n';
                    CharacterInfo info = default;
                    if (!newline && !Font.GetCharacterInfo(character, out info, 64, FontStyle.Normal)) continue;
                    if (newline || (x > 0 && MaxWidth > 0 && x + info.advance / 64f > MaxWidth))
                    {
                        lineWidths.Add(x);
                        lineStarts.Add(vertices.Count);
                        x = 0;
                        y -= 1.2f;
                        if (newline || character == ' ') continue;
                    }
                    int start = vertices.Count;
                    float left = x + info.minX/64f, right = x + info.maxX/64f, bottom = y + info.minY/64f, top = y + info.maxY/64f;
                    vertices.Add(new Vector3(left,bottom,0)); vertices.Add(new Vector3(right,bottom,0)); vertices.Add(new Vector3(left,top,0)); vertices.Add(new Vector3(right,top,0));
                    uv.Add(info.uvBottomLeft); uv.Add(info.uvBottomRight); uv.Add(info.uvTopLeft); uv.Add(info.uvTopRight);
                    triangles.AddRange(new[] {start,start+2,start+1,start+2,start+3,start+1}); colors.AddRange(new[] {Color,Color,Color,Color});
                    x += info.advance/64f; minY = Mathf.Min(minY,bottom); maxY = Mathf.Max(maxY,top);
                }
                lineWidths.Add(x);
                lineStarts.Add(vertices.Count);
                Vector2 pivot = OriginalSceneRenderer.Pivot(Pivot);
                float vertical = -minY - (maxY - minY) * pivot.y;
                for (int line = 0; line < lineWidths.Count; line++)
                {
                    var offset = new Vector3(-lineWidths[line] * pivot.x, vertical, 0);
                    for (int i = lineStarts[line]; i < lineStarts[line + 1]; i++) vertices[i] += offset;
                }
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
            TextAsset motion = Resources.Load<TextAsset>("OriginalSceneMotion");
            if (motion == null) throw new InvalidOperationException("原场景动画数据缺失。");
            motionData = JObject.Parse(motion.text);
            if ((int?)motionData["schemaVersion"] != 1) throw new FormatException("原场景动画数据无效。");
            titleHitCenter = Vector(motionData["titleHitBox"]["center"]);
            titleHitSize = Vector(motionData["titleHitBox"]["size"]);
            homeResourcesReady = true;
            spriteShader = Resources.Load<Shader>("OriginalSceneUI");
            if (spriteShader == null) throw new InvalidOperationException("原场景界面着色器缺失。");
            menuClipProperties = new MaterialPropertyBlock();
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
            if (layout == null) return;
            if (name == "Confirm" || name == "ServerList" || name == "Message" || name == "Download")
            {
                ShowDialog(name);
                return;
            }
            CloseDialog();
            if (currentScene == name) return;
            if (name == "Home" && !CanPresentHome) throw new InvalidOperationException("主页背景与导航角色资源尚未完整就绪。");
            JObject definition = Definition(name);
            if (sceneRoot != null) { sceneRoot.SetActive(false); Destroy(sceneRoot); }
            if (worldRoot != null) { worldRoot.SetActive(false); Destroy(worldRoot); worldRoot = null; }
            worldCamera = null;
            navi = null;
            ReleaseObjects(ownedMeshes, 0);
            ReleaseObjects(ownedMaterials, 0);
            sceneMotions.Clear();
            titleArtwork = titlePrompt = titleConnecting = null;
            titleHitTarget = null;
            titleMotion = null;
            menuClipPanel = menuButtons = menuLeftArrow = menuRightArrow = null;
            menuWorldClip = default;
            draggingMenu = false;
            objects.Clear(); widgets.Clear();
            sceneRoot = new GameObject("Restored_" + name); sceneRoot.transform.SetParent(transform, false);
            currentScene = name;
            Build(definition, sceneRoot.transform);
            if (name == "Home")
            {
                BuildHomeWorld(definition);
                Build(Definition("Menu"), sceneRoot.transform);
            }
        }

        private JObject Definition(string name)
        {
            foreach (JObject item in (JArray)layout["definitions"])
                if ((string)item["name"] == name) return item;
            throw new InvalidOperationException("缺少原场景：" + name);
        }

        private static void ReleaseObjects<T>(List<T> items, int first) where T : UnityEngine.Object
        {
            for (int i = first; i < items.Count; i++) Destroy(items[i]);
            items.RemoveRange(first, items.Count - first);
        }

        private void ShowDialog(string name)
        {
            CloseDialog();
            currentDialog = name;
            firstDialogWidget = widgets.Count;
            firstDialogMesh = ownedMeshes.Count;
            firstDialogMaterial = ownedMaterials.Count;
            dialogRoot = new GameObject("Restored_" + name);
            dialogRoot.transform.SetParent(transform, false);
            dialogRoot.transform.localScale = Vector3.one * (2f / 640f);
            Build(Definition(name), dialogRoot.transform);
        }

        private void CloseDialog()
        {
            if (dialogRoot == null) return;
            dialogRoot.SetActive(false);
            Destroy(dialogRoot);
            dialogRoot = null;
            currentDialog = null;
            dialogMotions.Clear();
            widgets.RemoveRange(firstDialogWidget, widgets.Count - firstDialogWidget);
            ReleaseObjects(ownedMeshes, firstDialogMesh);
            ReleaseObjects(ownedMaterials, firstDialogMaterial);
        }

        private void Build(JObject definition, Transform parent)
        {
            string scene = (string)definition["name"];
            bool isDialog = dialogRoot != null && dialogRoot.transform == parent;
            int firstWidget = widgets.Count;
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
                    // The original game opens these templates only when it has
                    // a corresponding status, notice, or banner to display.
                    if (type == "Menu_StatusInfoPopup" || type == "HomeMessageWindow" ||
                        type == "BannerListItem" || type == "Menu_Pop" || type == "Menu_Pop_Special")
                        item.SetActive(false);
                }
                if (scene == "Menu" && (item.name == "Menu_Filter" || item.name == "Label_Message2"))
                    item.SetActive(false);
                if (scene == "IntroSlPl" && (item.name == "IntroSlPl_Download" || item.name == "Label_Illust" ||
                    item.name == "Label_Cv" || item.name == "Label_Info" || item.name == "Voice" ||
                    item.name == "Cv" || item.name == "Illust"))
                    item.SetActive(false);
                if (scene == "Title")
                {
                    if (item.name == "Anniversary_Chara") titleArtwork = item;
                    if (item.name == "Title_Btn03") titlePrompt = item;
                    if (item.name == "Title_Connect") titleConnecting = item;
                    if ((string)node["id"] == (string)motionData["titleHitBox"]["node"]) titleHitTarget = item.transform;
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
                    var widget = new Widget { Object = item, Node = node, Data = data, Depth = (int)data["depth"] + (isDialog ? 1000 : 0),SourceColor=ColorValue(data["color"]) };
                    if ((string)data["kind"] == "UILabel") BuildLabel(widget);
                    else if (data["resolved"] is JObject) BuildImage(widget);
                    else continue; // Dynamic slots stay unresolved; readiness is recorded separately.
                    widgets.Add(widget);
                }
            }
            if (scene == "Menu")
            {
                var allGroups = new HashSet<string>();
                var homeGroups = new HashSet<string>();
                foreach (JObject subgroup in (JArray)definition["menuSubgroups"])
                {
                    var groups = (JArray)subgroup["groups"];
                    for (int i = 0; i < groups.Count; i++)
                    {
                        string id = (string)groups[i];
                        allGroups.Add(id);
                        // Menu_HomeItem.OpenMenu opens the common group and
                        // one of the TOP/STORY groups. Initial entry is TOP.
                        if ((string)subgroup["type"] == "Menu_HomeItem" && i < 2) homeGroups.Add(id);
                    }
                }
                foreach (string id in allGroups) localObjects[id].SetActive(homeGroups.Contains(id));
                ConfigureMenuClip(definition, localObjects);
            }
            if (isDialog)
                foreach (GameObject item in localObjects.Values)
                    if (item.name == "Black" && item.transform.parent.name == "Anm_Fade")
                    {
                        Vector3 scale = item.transform.localScale;
                        scale.x = Mathf.Max(scale.x, 640 * sceneCamera.aspect);
                        item.transform.localScale = scale;
                    }
            if (scene == "IntroSlPl") SelectRole(0);
            if (scene == "Title")
            {
                FitTitleArtwork();
                StartTitleMotion(false);
            }
            foreach (JObject node in (JArray)definition["nodes"])
            {
                GameObject item = localObjects[(string)node["id"]];
                if (item.activeInHierarchy && item.name != "Menu_SwipeMark")
                    PlayEnter(node, item, isDialog ? dialogMotions : sceneMotions);
            }
            for (int i = firstWidget; i < widgets.Count; i++) BindAction(widgets[i], definition);
        }

        private void PlayEnter(JObject node, GameObject item, List<OriginalSceneMotion> motions)
        {
            JObject animation = node["enterAnimation"] as JObject;
            string source = (string)animation?["source"];
            if (!String.IsNullOrEmpty(source))
                motions.Add(StartMotion(System.IO.Path.GetFileNameWithoutExtension(source), item.transform));
        }

        private OriginalSceneMotion StartMotion(string name, Transform origin)
        {
            JObject clip = (JObject)motionData["clips"][name];
            if (clip == null) throw new InvalidOperationException("缺少原场景动画：" + name);
            float now = Time.unscaledTime;
            var motion = new OriginalSceneMotion(clip, origin, now, BindOpacity);
            motion.Sample(now);
            return motion;
        }

        private Action<float> BindOpacity(Transform target)
        {
            var affected = widgets.FindAll(widget => widget.Object.transform == target || widget.Object.transform.IsChildOf(target));
            return alpha =>
            {
                foreach (Widget widget in affected)
                {
                    Color tint = widget.SourceColor;
                    tint.a *= alpha;
                    if (widget.Label != null) widget.Label.Color = tint;
                    Mesh mesh = widget.Label == null ? widget.ImageMesh : widget.Label.Mesh;
                    if (widget.Tint == null || widget.Tint.Length != mesh.vertexCount) widget.Tint = new Color[mesh.vertexCount];
                    for (int i = 0; i < widget.Tint.Length; i++) widget.Tint[i] = tint;
                    mesh.colors = widget.Tint;
                }
            };
        }

        private static void UpdateMotions(List<OriginalSceneMotion> motions)
        {
            for (int i = motions.Count - 1; i >= 0; i--)
                if (!motions[i].Sample(Time.unscaledTime)) motions.RemoveAt(i);
        }

        private void ConfigureMenuClip(JObject definition, Dictionary<string, GameObject> page)
        {
            foreach (JObject node in (JArray)definition["nodes"])
            {
                GameObject item = page[(string)node["id"]];
                switch (item.name)
                {
                    case "Right Anchor":
                        foreach (Transform panel in item.transform)
                            if (panel.name.StartsWith("Menu_", StringComparison.Ordinal) && panel.name != "Menu_HomeBtnR")
                                panel.gameObject.SetActive(false);
                        break;
                    case "Menu_HomeBtnL":
                    case "Menu_MainQuestBtn":
                        // The authored badges and quest callout are populated
                        // from data that this home page does not bind yet.
                        item.SetActive(false);
                        break;
                    case "MainBtns":
                        JArray range = (JArray)node["panel"]["range"];
                        menuClip = new Rect((float)range[0] - (float)range[2] / 2, (float)range[1] - (float)range[3] / 2, (float)range[2], (float)range[3]);
                        menuClipPanel = item.transform;
                        break;
                    case "Btns": menuButtons = item.transform; break;
                    case "Anim_SwipeMark_Left": menuLeftArrow = item.transform; break;
                    case "Anim_SwipeMark_Right": menuRightArrow = item.transform; break;
                }
            }
            float first = Single.PositiveInfinity, last = Single.NegativeInfinity;
            foreach (Transform button in menuButtons)
            {
                if (!button.gameObject.activeSelf) continue;
                first = Mathf.Min(first, button.localPosition.x);
                last = Mathf.Max(last, button.localPosition.x);
            }
            const float iconHalfWidth = 80; // The authored Menu_MainBtn base sprites are 160 px wide.
            const float edgePadding = 14; // Matches Menu_ItemMgr's initial mMaxX for the first item.
            menuScrollMax = menuClip.xMin - first + iconHalfWidth + edgePadding;
            menuScrollMin = Mathf.Min(menuScrollMax, menuClip.xMax - last - iconHalfWidth - edgePadding);
            ScrollMenu(menuScrollMax - menuButtons.localPosition.x);
        }

        private void ScrollMenu(float delta)
        {
            Vector3 position = menuButtons.localPosition;
            position.x = Mathf.Clamp(position.x + delta, menuScrollMin, menuScrollMax);
            menuButtons.localPosition = position;
            SetMenuArrow(menuLeftArrow, "SwipeMark_Left", position.x < menuScrollMax);
            SetMenuArrow(menuRightArrow, "SwipeMark_Right", position.x > menuScrollMin);
        }

        private static void SetMenuArrow(Transform arrow, string name, bool enabled)
        {
            arrow.Find(name).gameObject.SetActive(enabled);
            arrow.Find(name + "_Disable").gameObject.SetActive(!enabled);
        }

        private void UpdateMenuClip()
        {
            if (menuClipPanel == null) return;
            Vector3 min = menuClipPanel.TransformPoint(menuClip.min);
            Vector3 max = menuClipPanel.TransformPoint(menuClip.max);
            Rect bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            if (bounds == menuWorldClip) return;
            menuWorldClip = bounds;
            menuClipProperties.SetVector(ClipRectId, new Vector4(min.x, min.y, max.x, max.y));
            foreach (Widget widget in widgets)
                if (widget.ImageMesh != null && widget.Object.transform.IsChildOf(menuClipPanel))
                    widget.Renderer.SetPropertyBlock(menuClipProperties);
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
            float glyphWidth = Mathf.Abs(widget.Object.transform.localScale.x);
            var text = new GlyphLabel {Font=originalFont,Mesh=mesh,Color=widget.SourceColor,Pivot=(int)widget.Data["pivot"],
                MaxWidth=glyphWidth > 0 ? ((float?)widget.Data["maxLineWidth"] ?? 0) / glyphWidth : 0};
            text.text = (string)widget.Data["text"] ?? "";
            widget.Label = text;
            widget.Renderer=renderer;
        }

        private void BindAction(Widget widget, JObject definition)
        {
            string scene = (string)definition["name"];
            JObject node = widget.Node;
            while (node != null)
            {
                foreach (JObject script in (JArray)node["scripts"])
                {
                    string method = (string)script["method"];
                    int role = (int?)script["role"] ?? 0;
                    if (method == "showServerListWindow") widget.Click = delegate { Show("ServerList"); };
                    if ((string)script["type"] == "IntroSlPlChr" && role >= 1 && role <= 4)
                    {
                        int choice = role; widget.Click = delegate { owner.SelectOriginalArthur(choice);SelectRole(choice); };
                    }
                    if (method == "OnClickOK") widget.Click = ShowNameInput;
                    if (scene == "Menu" && method == "OnLeftButton") widget.Click = () => ScrollMenu(648);
                    if (scene == "Menu" && method == "OnRightButton") widget.Click = () => ScrollMenu(-648);
                    if(method=="OnClick"&&(string)script["type"]=="UIButtonMessage"&&widget.Object.name=="Back")widget.Click=delegate{SelectRole(0);};
                    if ((string)script["type"] == "UIInput") widget.Click = OpenNameInput;
                    if(method=="OnCenterButton")widget.Click=owner.CreateOriginalCharacter;
                }
                string objectName=(string)node["name"];
                if(scene=="Confirm"&&(objectName=="Btn01"||objectName=="Btn02"))
                {
                    bool accept=objectName=="Btn01";
                    widget.Click=delegate { Action action=accept?dialogAction:null;dialogAction=null;CloseDialog();if(action!=null)action(); };
                }
                string parent = (string)node["parent"];
                node = null;
                if (parent != null) foreach (JObject candidate in (JArray)definition["nodes"]) if ((string)candidate["id"] == parent) node = candidate;
            }
            string name = widget.Object.name;
            if (scene == "ServerList" && name == "Btn01")
                widget.Click = delegate { CloseDialog(); owner.BeginFromOriginalTitle(); };
        }

        private void OpenNameInput() { nameKeyboard = TouchScreenKeyboard.Open(owner.OriginalCharacterName, TouchScreenKeyboardType.Default); }

        private void Update()
        {
            if (layout == null) return;
            if (nameKeyboard != null && nameKeyboard.status == TouchScreenKeyboard.Status.Done)
            {
                owner.SetOriginalCharacterName(nameKeyboard.text); nameKeyboard = null;
            }
            if (dialogRoot == null && owner.OriginalEntryStep == CnEntryStep.Character && currentScene != "IntroSlPl") Show("IntroSlPl");
            if (dialogRoot == null && owner.OriginalHome != null && CanPresentHome && currentScene != "Home") Show("Home");
            UpdateMotions(sceneMotions);
            UpdateMotions(dialogMotions);
            UpdateMenuClip();
            if (currentScene == "Home" && sceneMotions.Count == 0 && dialogRoot == null && !displayedBonus && !String.IsNullOrEmpty(owner.OriginalBonusNotice))
            {
                displayedBonus = true;
                dialogTitle = "登录奖励";
                dialogBody = owner.OriginalBonusNotice;
                dialogAction = owner.AcknowledgeOriginalBonus;
                Show("Confirm");
            }
            if (currentScene == "Title")
            {
                FitTitleArtwork();
                bool waiting = dialogRoot == null && (owner.OriginalBusy || (titleWaiting && Time.unscaledTime - titleWaitingSince < 0.25f));
                if (titleWaiting != waiting) StartTitleMotion(waiting);
                titleMotion.Sample(Time.unscaledTime);
            }
            if (worldCamera != null) worldCamera.fieldOfView = 45f * (4f / 3f) / worldCamera.aspect;
            for (int i = 0; i < widgets.Count; i++)
            {
                Widget widget = widgets[i];
                bool dialogWidget = dialogRoot != null && i >= firstDialogWidget;
                if (!dialogWidget && currentScene == "Home" && owner.OriginalHome != null && widget.ImageMesh != null)
                    BindHomeSprite(widget);
                if (widget.Label == null) continue;
                if (widget.Object.name == "Label_Input01") widget.Label.text = owner.OriginalCharacterName;
                if(widget.Object.name=="Label_PreInput")widget.Object.SetActive(String.IsNullOrEmpty(owner.OriginalCharacterName));
                if (widget.Label.text.StartsWith("Ver.",StringComparison.Ordinal)) widget.Label.text = "Ver." + Application.version;
                if(dialogWidget && currentDialog=="Confirm")
                {
                    if(widget.Object.name=="Label_Text")widget.Label.text=dialogBody??"";
                    if(widget.Object.name=="Label_Title")widget.Label.text=dialogTitle;
                    if(widget.Object.name=="Label_01")widget.Label.text="继续";
                    if(widget.Object.name=="Label_02")widget.Label.text="返回";
                }
                if(dialogWidget && currentDialog=="ServerList")
                {
                    if(widget.Object.name=="Label_IP"||widget.Object.name=="Label_NowServer")widget.Label.text=owner.OriginalServerAddress;
                    if(widget.Object.name=="Label_Name")widget.Label.text="游戏服务器";
                    if(widget.Object.name=="Label_Vers")widget.Label.text=Application.version;
                }
                if(!dialogWidget && currentScene=="Home"&&owner.OriginalHome!=null)BindHomeLabel(widget);
            }
            if (owner.OriginalBusy) return;
            bool pressed = Input.GetMouseButtonDown(0);
            Vector2 screen = Input.mousePosition;
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                pressed = touch.phase == TouchPhase.Began;
                screen = touch.position;
                if (pressed && menuClipPanel != null && dialogRoot == null)
                {
                    draggingMenu = MenuContains(sceneCamera.ScreenPointToRay(screen));
                    menuTouchId = touch.fingerId;
                    menuTouchStart = touch.position;
                    menuDragged = false;
                }
                if (draggingMenu && touch.fingerId == menuTouchId && dialogRoot == null)
                {
                    if (touch.phase == TouchPhase.Moved)
                    {
                        float offset = touch.position.x - menuTouchStart.x;
                        if (menuDragged || Mathf.Abs(offset) >= 12)
                        {
                            ScrollMenu((menuDragged ? touch.deltaPosition.x : offset) * 640 / sceneCamera.pixelHeight);
                            menuDragged = true;
                        }
                    }
                    if (touch.phase == TouchPhase.Ended && !menuDragged)
                        HandlePress(sceneCamera.ScreenPointToRay(screen), screen);
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) draggingMenu = false;
                    return;
                }
            }
            if (pressed) HandlePress(sceneCamera.ScreenPointToRay(screen), screen);
        }

        private void HandlePress(Ray ray, Vector2 screen)
        {
            Widget hit = null;
            for (int i = dialogRoot == null ? 0 : firstDialogWidget; i < widgets.Count; i++)
            {
                Widget widget = widgets[i];
                if (widget.Click == null || !widget.Object.activeInHierarchy) continue;
                if (menuClipPanel != null && widget.Object.transform.IsChildOf(menuClipPanel) && !MenuContains(ray)) continue;
                Plane plane = new Plane(widget.Object.transform.forward, widget.Object.transform.position);
                float distance; if (!plane.Raycast(ray,out distance)) continue;
                Vector3 point = widget.Object.transform.InverseTransformPoint(ray.GetPoint(distance));
                Vector2 pivot = Pivot((int)widget.Data["pivot"]);
                if (point.x < -pivot.x || point.x > 1-pivot.x || point.y < -pivot.y || point.y > 1-pivot.y) continue;
                if (hit == null || widget.Depth > hit.Depth) hit = widget;
            }
            if (hit != null) { hit.Click(); return; }
            if (dialogRoot != null) return;
            if (currentScene == "Title" && IsTitlePress(ray))
            {
                if (titleWaiting) return;
                StartTitleMotion(true);
                owner.BeginFromOriginalTitle();
            }
            else if (currentScene == "Home" && navi != null && !MenuContains(ray))
                navi.TryPlayTouch(worldCamera.ScreenPointToRay(screen));
        }

        private bool MenuContains(Ray ray)
        {
            return menuClipPanel != null && new Plane(Vector3.forward, menuClipPanel.position).Raycast(ray, out float distance) && menuWorldClip.Contains(ray.GetPoint(distance));
        }

        private bool IsTitlePress(Ray ray)
        {
            if (titleHitTarget == null) return false;
            Vector3 center = titleHitTarget.TransformPoint(titleHitCenter);
            if (!new Plane(titleHitTarget.forward, center).Raycast(ray, out float distance)) return false;
            Vector3 local = titleHitTarget.InverseTransformPoint(ray.GetPoint(distance)) - titleHitCenter;
            return Mathf.Abs(local.x) <= Mathf.Max(titleHitSize.x, 640 * sceneCamera.aspect) / 2f && Mathf.Abs(local.y) <= titleHitSize.y / 2f;
        }

        private void StartTitleMotion(bool waiting)
        {
            titleWaiting = waiting;
            titleWaitingSince = Time.unscaledTime;
            titlePrompt.SetActive(!waiting);
            titleConnecting.SetActive(waiting);
            titleMotion = waiting
                ? StartMotion("Title_Connect_Loop_00", titleConnecting.transform)
                : StartMotion("Title_TS_Loop_00", titleHitTarget);
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
                            GameObject item;
                            if (objects.TryGetValue("IntroSlPl:" + (string)node["id"], out item))
                            {
                                bool opening = role != 0 && !item.activeSelf;
                                item.SetActive(role != 0);
                                if (opening) PlayEnter(node, item, sceneMotions);
                            }
                        }
                        else if((string)script["type"]=="IntroSlPlInfoChrDetail"||(string)script["type"]=="IntroSlPlInfoChr"||(string)script["type"]=="CmnInputWindow")
                        {
                            GameObject item;if(objects.TryGetValue("IntroSlPl:"+(string)node["id"],out item))item.SetActive(false);
                        }
            }
            if (role != 0)
                foreach (Widget widget in widgets)
                    if ((string)widget.Data["frame"] == "IntroSlPl_Name01" && widget.Object.name == "Dmy_Name")
                        ApplyAtlasFrame(widget, "roleName", "IntroSlPl_Name" + role.ToString("D2"));
        }

        private void ShowNameInput()
        {
            foreach(JObject scene in (JArray)layout["definitions"])
                if((string)scene["name"]=="IntroSlPl")foreach(JObject node in (JArray)scene["nodes"])
                    foreach(JObject script in (JArray)node["scripts"])
                        if((string)script["type"]=="CmnInputWindow")
                        {
                            GameObject item;
                            if (objects.TryGetValue("IntroSlPl:" + (string)node["id"], out item))
                            {
                                item.SetActive(true);
                                PlayEnter(node, item, sceneMotions);
                            }
                        }
            OpenNameInput();
        }

        public void ConfirmHomeEntry()
        {
            dialogTitle="确认";
            dialogBody="进入主页会读取角色状态、领取可用登录奖励，并结束服务器残留的单人开场。";
            dialogAction=owner.EnterOriginalHome;Show("Confirm");
        }
        public void NotifyHomeUpdated(){displayedBonus=false;}
        public void ShowError(string message)
        {
            if(currentDialog=="Confirm"&&dialogBody==message)return;
            dialogTitle="提示";
            dialogBody=message;
            dialogAction=owner.OriginalCanRetry?(Action)owner.RetryOriginalEntry:null;Show("Confirm");
        }
        private void FitTitleArtwork()
        {
            if (titleArtwork == null) return;
            const float artworkAspect = 16f / 9f; // TitleArtworkLayout.backgroundAspect in Title.unity.
            float height = 640 * Mathf.Max(1, sceneCamera.aspect / artworkAspect);
            titleArtwork.transform.localScale = new Vector3(height * artworkAspect, height, 1);
        }
        private void BuildHomeWorld(JObject definition)
        {
            worldRoot = new GameObject("OriginalHomeWorld");
            worldRoot.transform.SetParent(transform, false);
            GameObject background = Instantiate(content.Load<GameObject>(OriginalNaviModel.BundleName, "home-background-01000"), worldRoot.transform, false);
            foreach (Transform part in background.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = 28;
            GameObject cameraObject = HomeObject(definition, "Main Camera");
            cameraObject.transform.SetParent(worldRoot.transform, false);
            worldCamera = cameraObject.AddComponent<Camera>();
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = Color.black;
            worldCamera.cullingMask = 1 << 28;
            worldCamera.depth = 1;
            worldCamera.fieldOfView = 45f * (4f / 3f) / worldCamera.aspect;
            HomeObject(definition, "Anm_Live2D").transform.SetParent(worldRoot.transform, false);
            GameObject canvas = HomeObject(definition, "Live2D_Canvas_1024");
            canvas.layer = 28;
            navi = canvas.AddComponent<OriginalNaviModel>();
            navi.Initialize(content, worldCamera);
        }

        private GameObject HomeObject(JObject definition, string name)
        {
            foreach (JObject node in (JArray)definition["nodes"])
                if ((string)node["name"] == name) return objects["Home:" + (string)node["id"]];
            throw new InvalidOperationException("缺少主页场景对象：" + name);
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
                case "Label_Message":widget.Label.text="主页";break;
                case "Label_BpTime":
                    long? seconds = (long?)home.Wire["user"]["bp_next_sec"];
                    widget.Label.text = home.Bp < home.BpMax && seconds >= 0 ? String.Format("{0}:{1:D2}", seconds / 60, seconds % 60) : "";
                    break;
                case "Label_NumBp":widget.Label.text=home.Bp.ToString();break;
                case "Label_NumBpMax":widget.Label.text=home.BpMax.ToString();break;
                case "Label_Name":widget.Label.text=home.Name;break;
                case "Label_NumGold":widget.Label.text=home.Gold.ToString();break;
            }
        }
        private void BindHomeSprite(Widget widget)
        {
            if (widget.Object.transform.parent.name != "Num_Lv") return;
            long level = owner.OriginalHome.Level;
            int place = widget.Object.name == "Num_1" ? 1 : widget.Object.name == "Num_2" ? 10 : 100;
            ApplyAtlasFrame(widget, "level", "Num_600_" + (level / place % 10));
        }

        private void ApplyAtlasFrame(Widget widget, string atlas, string frame)
        {
            if (widget.DisplayedFrame == frame) return;
            JArray uv = (JArray)motionData["atlases"][atlas]["frames"][frame];
            float x = (float)uv[0], y = (float)uv[1], w = (float)uv[2], h = (float)uv[3];
            widget.ImageMesh.uv = new[] { new Vector2(x, y), new Vector2(x + w, y), new Vector2(x, y + h), new Vector2(x + w, y + h) };
            widget.DisplayedFrame = frame;
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
        private void OnDestroy()
        {
            Font.textureRebuilt -= RebuildFont;
            if (sceneRoot != null) Destroy(sceneRoot);
            if (worldRoot != null) Destroy(worldRoot);
            if (dialogRoot != null) Destroy(dialogRoot);
            ReleaseObjects(ownedMeshes, 0);
            ReleaseObjects(ownedMaterials, 0);
        }
        private static Vector3 Vector(JToken values) { return new Vector3((float)values[0],(float)values[1],(float)values[2]); }
        private static Vector2 Pivot(int value) { return new Vector2((value % 3)*0.5f,1-(value/3)*0.5f); }
        private static Color ColorValue(JToken value) { return new Color((float)value[0],(float)value[1],(float)value[2],(float)value[3]); }
    }
}
