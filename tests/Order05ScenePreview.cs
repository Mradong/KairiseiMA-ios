#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Kairisei.Recovery;
using Kairisei.Recovery.Cubism2;

// Copied only into an isolated Windows editor project by the preview runner.
public sealed class Order05ScenePreview : MonoBehaviour
{
    public string Output;
    private const string SceneAssets = "Assets/Recovery/OriginalSceneContent";
    private const string WorldAssets = "Assets/Recovery/OriginalWorldContent";
    private const int Width = 2769, Height = 1280;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run()
    {
        try
        {
            string output = Path.GetFullPath(Environment.GetEnvironmentVariable("ORDER05_PREVIEW_OUTPUT") ?? "scene-preview");
            Directory.CreateDirectory(output);
            string bundleFolder = Path.GetFullPath("preview-bundles");
            Directory.CreateDirectory(bundleFolder);
            string[] scene = Directory.GetFiles(SceneAssets).Where(f => f.EndsWith(".png") || f.EndsWith(".json"))
                .Select(f => f.Replace('\\', '/')).Concat(new[] { "Assets/Recovery/Runtime/OriginalSceneSprite.shader" }).ToArray();
            string worldFile = WorldAssets + "/OriginalWorld.json";
            JObject world = JObject.Parse(File.ReadAllText(worldFile));
            JObject[] assets = ((JArray)world["addressableAssets"]).Concat((JArray)world["shaderAssets"]).Cast<JObject>().ToArray();
            var bundles = new[]
            {
                new AssetBundleBuild {assetBundleName="scene", assetNames=scene,
                    addressableNames=scene.Select(f => f.EndsWith(".shader") ? "original-scene-sprite" : Path.GetFileNameWithoutExtension(f).ToLowerInvariant()).ToArray()},
                new AssetBundleBuild {assetBundleName="world", assetNames=new[] {worldFile}.Concat(assets.Select(row=>(string)row["asset"])).ToArray(),
                    addressableNames=new[] {"originalworld"}.Concat(assets.Select(row=>(string)row["address"])).ToArray()}
            };
            if (BuildPipeline.BuildAssetBundles(bundleFolder, bundles, BuildAssetBundleOptions.StrictMode, BuildTarget.StandaloneWindows64) == null)
                throw new InvalidOperationException("Could not build Windows editor preview bundles.");
            var sceneHandle = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Preview").AddComponent<Order05ScenePreview>().Output = output;
            PlayerSettings.bundleVersion = "0.0.11";
            if (!EditorSceneManager.SaveScene(sceneHandle, "Assets/Preview.unity")) throw new InvalidOperationException("Cannot save preview scene.");
            EditorApplication.EnterPlaymode();
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }

    private static FieldInfo Field(Type type, string name) { return type.GetField(name, Private); }
    private static MethodInfo Method(string name) { return typeof(OriginalSceneRenderer).GetMethod(name, Private); }
    private static void Require(bool result, string detail) { if (!result) throw new InvalidOperationException(detail); }

    private IEnumerator Start()
    {
        var steps = Preview();
        while (true)
        {
            object wait;
            try
            {
                if (!steps.MoveNext()) break;
                wait = steps.Current;
            }
            catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); yield break; }
            yield return wait;
        }
        Debug.Log("Order05 actual renderer capture complete.");
        EditorApplication.Exit(0);
    }

    private IEnumerator Preview()
    {
        var stateHost = new GameObject("Unconnected preview fixture");
        stateHost.SetActive(false);
        var state = stateHost.AddComponent<RecoveryBootstrap>();
        var loader = gameObject.AddComponent<RecoveryContentLoader>();
        var renderer = gameObject.AddComponent<OriginalSceneRenderer>();
        var target = new RenderTexture(Width, Height, 24) { antiAliasing = 1 };
        var main = new GameObject("PreviewMain").AddComponent<Camera>();
        main.clearFlags = CameraClearFlags.SolidColor;
        main.backgroundColor = Color.black;
        main.cullingMask = 0;
        var loaded = (Dictionary<string, AssetBundle>)Field(typeof(RecoveryContentLoader), "loaded").GetValue(loader);
        loaded.Add(OriginalSceneRenderer.BundleName, AssetBundle.LoadFromFile(Path.GetFullPath("preview-bundles/scene")));
        loaded.Add(OriginalNaviModel.BundleName, AssetBundle.LoadFromFile(Path.GetFullPath("preview-bundles/world")));
        typeof(RecoveryContentLoader).GetProperty("Ready").SetValue(loader, true);
        renderer.Initialize(loader, Resources.Load<Font>("RecoveryChinese"), state);
        var ui = (Camera)Field(typeof(OriginalSceneRenderer), "sceneCamera").GetValue(renderer);
        ui.targetTexture = target;
        ui.aspect = (float)Width / Height;
        var worldBundle = loaded[OriginalNaviModel.BundleName];
        loaded.Remove(OriginalNaviModel.BundleName);
        try { renderer.RefreshContent(); }
        catch (InvalidOperationException) { }
        Require(!renderer.CanPresentHome && renderer.Initialized, "The active resource failure did not block Home while preserving the title.");
        loaded.Add(OriginalNaviModel.BundleName, worldBundle);
        renderer.RefreshContent();
        Require(renderer.CanPresentHome, "The active resource validation did not recover on retry.");
        renderer.Show("IntroSlPl");
        renderer.Show("Title");
        Method("Update").Invoke(renderer, null);
        Capture("00-title-entry-start", main, renderer, ui, target);
        yield return new WaitForSecondsRealtime(0.55f);
        var promptMotion = (OriginalSceneMotion)Field(typeof(OriginalSceneRenderer), "titleMotion").GetValue(renderer);
        float promptStart = (float)Field(typeof(OriginalSceneMotion), "started").GetValue(promptMotion);
        promptMotion.Sample(promptStart);
        Capture("01-title", main, renderer, ui, target);
        foreach (var point in new[] { new Vector2(160, 900), new Vector2(Width / 2, Height / 2), new Vector2(Width - 160, 900) })
            Require((bool)Method("IsTitlePress").Invoke(renderer, new object[] { ui.ScreenPointToRay(point) }), "Title input missed outside the prompt.");
        promptMotion.Sample(promptStart + 0.25f);
        Capture("02-title-prompt-cycle", main, renderer, ui, target);
        var titleTap = new Vector2(160, 900);
        Method("HandlePress").Invoke(renderer, new object[] { ui.ScreenPointToRay(titleTap), titleTap });
        Require((bool)Field(typeof(OriginalSceneRenderer), "titleWaiting").GetValue(renderer), "The title outside its prompt gave no immediate feedback.");
        Field(typeof(RecoveryBootstrap), "busy").SetValue(state, true);
        var connectMotion = (OriginalSceneMotion)Field(typeof(OriginalSceneRenderer), "titleMotion").GetValue(renderer);
        float connectStart = (float)Field(typeof(OriginalSceneMotion), "started").GetValue(connectMotion);
        connectMotion.Sample(connectStart + 0.55f);
        Capture("03-title-connecting", main, renderer, ui, target);
        var connecting = (GameObject)Field(typeof(OriginalSceneRenderer), "titleConnecting").GetValue(renderer);
        Require(connecting.activeInHierarchy, "The original connecting sprite did not activate.");
        Field(typeof(RecoveryBootstrap), "busy").SetValue(state, false);
        var entry = (CnEntryFlow)Field(typeof(RecoveryBootstrap), "entry").GetValue(state);
        entry.BeginHealth();
        entry.Fail(new CnTransportException(0, "fixture transport error"));
        renderer.ShowError("无法连接服务器，请检查网络后再试。连接恢复后会继续使用当前游客身份。");
        Require((string)Field(typeof(OriginalSceneRenderer), "dialogAcceptLabel").GetValue(renderer) == "重试", "The connection error did not surface the retry action.");
        var retry = (Action)Field(typeof(OriginalSceneRenderer), "dialogAction").GetValue(renderer);
        Require(retry.Target == state && retry.Method.Name == nameof(RecoveryBootstrap.RetryOriginalEntry), "The retry button is not bound to the real entry flow.");
        yield return new WaitForSecondsRealtime(0.28f);
        Capture("04-title-error", main, renderer, ui, target);
        var endpoint = ServerEndpoint.Parse("http://127.0.0.1:27040");
        entry.BeginLogin();
        entry.ReceiveLogin(new JObject {
            { "res_code", 0 }, { "userid", 1 }, { "sess_key", "order05-fixture-session-0123456789" },
            { "api_url", endpoint.ToString() }, { "version_url", endpoint.Route("/local/content/iOS/6000.3.26f1/fixture/").ToString() }
        }.ToString(), endpoint);
        entry.ResourcesReady();
        entry.ReceiveConnect("{\"res_code\":0}\n{\"is_user\":0,\"tutorial_flag\":0}\n{}");
        Field(typeof(RecoveryBootstrap), "login").SetValue(state, entry.Login);
        Field(typeof(RecoveryBootstrap), "connectState").SetValue(state, entry.CharacterState);
        Field(typeof(RecoveryBootstrap), "serverContentReady").SetValue(state, true);
        Field(typeof(RecoveryBootstrap), "address").SetValue(state, endpoint.ToString());
        Field(typeof(RecoveryBootstrap), "verifiedAddress").SetValue(state, endpoint.ToString());
        Field(typeof(RecoveryBootstrap), "originalScene").SetValue(state, renderer);
        Method("CloseDialog").Invoke(renderer, null);
        yield return new WaitForSecondsRealtime(0.62f);
        Transform roles = renderer.transform.Find("Restored_IntroSlPl/UI Root (2D)/Camera/Center Anchor");
        Require(roles != null, "A fresh Connect response did not open the authored role screen.");
        Capture("05-role-selection", main, renderer, ui, target);
        Transform back = renderer.transform.Find("Restored_IntroSlPl/UI Root (2D)/Camera/Right Bottom Anchor/IntroSlPl_InfoBackBtn/Back/Back");
        foreach (int role in new[] { 1, 3, 4, 2 })
        {
            Transform portrait = roles.Find("IntroSlPl_Chr" + role.ToString("D2") + "/Chr");
            Press(renderer, ui, portrait);
            Require(state.OriginalArthurRole == role, "Tapping portrait " + role + " did not select its authored role.");
            string body = entry.BeginCreate("预览测试", state.OriginalArthurRole);
            var request = JObject.Parse(body.Substring(entry.Login.SessionKey.Length));
            Require((int)request["arthur_type"] == role, "The serialized creation request lost the selected role.");
            entry.BeginConnect();
            entry.ReceiveConnect("{\"res_code\":0}\n{\"is_user\":0,\"tutorial_flag\":0}\n{}");
            Field(typeof(RecoveryBootstrap), "connectState").SetValue(state, entry.CharacterState);
            yield return new WaitForSecondsRealtime(0.4f);
            if (role == 2) break;
            Press(renderer, ui, back);
            for (int choice = 1; choice <= 4; choice++)
                Require(roles.Find("IntroSlPl_Chr" + choice.ToString("D2")).gameObject.activeInHierarchy, "Back did not restore all portraits.");
        }
        Capture("06-role-selected", main, renderer, ui, target);
        state.CreateOriginalCharacter();
        Require(entry.Step == CnEntryStep.Character && !entry.CreationMayHaveCommitted, "A blank role name left the editable character step.");
        Require((string)Field(typeof(OriginalSceneRenderer), "dialogAcceptLabel").GetValue(renderer) == "确定" &&
                (string)Field(typeof(OriginalSceneRenderer), "dialogBody").GetValue(renderer) == "请输入不含空格的角色名称。", "A blank name did not display the local validation error.");
        yield return new WaitForSecondsRealtime(0.45f);
        Capture("17-role-name-error", main, renderer, ui, target);
        entry.Reset();
        JObject homeWire = JObject.Parse("{\"server_time\":100,\"medal_num\":7,\"user\":{\"userid\":1,\"name\":\"画面检查\",\"lv\":3,\"active_arthur_type\":2,\"ap\":2,\"ap_max\":3,\"bp\":5,\"bp_max\":20,\"gold\":200,\"coin\":1,\"coin_free\":2,\"bp_next_sec\":85,\"now_lv_exp\":20,\"next_lv_exp\":60}}");
        CnHomeState home = FixtureHome(homeWire);
        Field(typeof(RecoveryBootstrap), "homeState").SetValue(state, home);
        renderer.Show("Home");
        Capture("07-home-entry-start", main, renderer, ui, target);
        var world = (Camera)Field(typeof(OriginalSceneRenderer), "worldCamera").GetValue(renderer);
        var model = (OriginalNaviModel)Field(typeof(OriginalSceneRenderer), "navi").GetValue(renderer);
        var cubism = (Live2DModelUnity)Field(typeof(OriginalNaviModel), "model").GetValue(model);
        Require(HasUathachPose(cubism, 1, 1), "The first Home frame did not hide alternative arms.");
        float enteringPosition = model.transform.position.x;
        yield return new WaitForSecondsRealtime(0.18f);
        Capture("08-home-entry-mid", main, renderer, ui, target);
        yield return new WaitForSecondsRealtime(0.25f);
        Capture("09-home", main, renderer, ui, target);
        Require(HasUathachPose(cubism, 1, 1), "The idle motion exposed an alternative arm.");
        Require(model.transform.position.x > enteringPosition + 10, "The actual navi transform did not animate.");
        Transform status = renderer.transform.Find("Restored_Home/Menu/UI Root (2D)/Camera/Left Top Anchor/Menu_Status");
        var bpBar = status.Find("BP/Anm_BpBar/Bar").GetComponentInChildren<MeshRenderer>();
        var expBar = status.Find("EXP/Anm_ExpBar/Bar").GetComponentInChildren<MeshRenderer>();
        Require(Mathf.Approximately(bpBar.transform.localScale.x, 0.25f), "The BP bar did not use current and maximum BP.");
        Require(Mathf.Approximately(expBar.transform.localScale.x, 0.25f), "The EXP bar did not use current and remaining EXP.");
        Vector2 apOne = status.Find("AP/Dmy_Ap1").GetComponentInChildren<MeshFilter>().sharedMesh.uv[0];
        Vector2 apTwo = status.Find("AP/Dmy_Ap2").GetComponentInChildren<MeshFilter>().sharedMesh.uv[0];
        Vector2 apThree = status.Find("AP/Dmy_Ap3").GetComponentInChildren<MeshFilter>().sharedMesh.uv[0];
        Require(apOne == apTwo && apTwo != apThree, "The original AP indicators did not show two occupied points out of three.");
        bool touched = false;
        for (int x = 300; x <= Width / 2 && !touched; x += 45)
            for (int y = 200; y < Height - 140 && !touched; y += 45)
                touched = model.TryPlayTouch(world.ScreenPointToRay(new Vector2(x, y)));
        Require(touched, "The original navi hit area did not receive a visible tap.");
        yield return new WaitForSecondsRealtime(0.8f);
        Require(HasUathachPose(cubism, 1, 2), "The touch motion did not switch to its authored right arm.");
        Capture("10-home-navi-touch", main, renderer, ui, target);
        float idleDeadline = Time.unscaledTime + 15;
        while (!HasUathachPose(cubism, 1, 1) && Time.unscaledTime < idleDeadline) yield return null;
        Require(HasUathachPose(cubism, 1, 1), "The model did not restore its idle arm pose after the touch motion.");
        Capture("19-home-navi-idle-restored", main, renderer, ui, target);
        Transform buttons = (Transform)Field(typeof(OriginalSceneRenderer), "menuButtons").GetValue(renderer);
        Transform right = (Transform)Field(typeof(OriginalSceneRenderer), "menuRightArrow").GetValue(renderer);
        float initialMenuX = buttons.localPosition.x;
        Transform rightMenu = renderer.transform.Find("Restored_Home/Menu/UI Root (2D)/Camera/Right Anchor");
        Transform homePanel = rightMenu.Find("Menu_HomeBtnR");
        Transform bannerItem = homePanel.Find("DynamicList/ListItem");
        Require(!bannerItem.gameObject.activeSelf, "A HomeShow reply without a banner exposed the prefab placeholder.");
        Transform battlePanel = rightMenu.Find("Menu_BattleBtn02");
        Transform cardPanel = rightMenu.Find("Menu_KnightBtn");
        Transform shopPanel = rightMenu.Find("Menu_ShoptBtn");
        Transform battleButton = buttons.Find("MainBtn_Battle/base");
        Mesh battleButtonMesh = battleButton.GetComponentInChildren<MeshFilter>().sharedMesh;
        Vector2 unselectedBattleUv = battleButtonMesh.uv[0];
        Press(renderer, ui, battleButton);
        yield return new WaitForSecondsRealtime(0.45f);
        Require(battlePanel.gameObject.activeInHierarchy && !homePanel.gameObject.activeInHierarchy, "The Battle button did not open the authored Battle submenu.");
        Require(battleButtonMesh.uv[0] != unselectedBattleUv, "The Battle tab did not switch to its original selected sprite.");
        Require(battlePanel.Find("Menu_AlchemistBattleBtn/AlchemistBattleBtn01").GetComponentInChildren<MeshRenderer>() != null, "The Alchemist Battle icon was left unresolved.");
        Capture("14-home-battle-menu", main, renderer, ui, target);
        Press(renderer, ui, buttons.Find("MainBtn_Card/base"));
        yield return new WaitForSecondsRealtime(0.45f);
        Require(cardPanel.gameObject.activeInHierarchy && !battlePanel.gameObject.activeInHierarchy, "The Card button did not replace the Battle submenu with its own.");
        Require(battleButtonMesh.uv[0] == unselectedBattleUv, "The previous tab kept its selected sprite after switching.");
        Capture("15-home-card-menu", main, renderer, ui, target);
        Press(renderer, ui, buttons.Find("MainBtn_Home/base"));
        yield return new WaitForSecondsRealtime(0.45f);
        Require(homePanel.gameObject.activeInHierarchy && !cardPanel.gameObject.activeInHierarchy, "The Home button did not restore the authored Home shortcuts.");
        Vector2 arrowTap = ui.WorldToScreenPoint(right.Find("SwipeMark_Right").position);
        Method("HandlePress").Invoke(renderer, new object[] { ui.ScreenPointToRay(arrowTap), arrowTap });
        Require(buttons.localPosition.x < initialMenuX, "The original scroll arrow did not move the clipped menu.");
        Capture("11-home-menu-scroll", main, renderer, ui, target);
        Press(renderer, ui, buttons.Find("MainBtn_Shop/base"));
        yield return new WaitForSecondsRealtime(0.45f);
        Require(shopPanel.gameObject.activeInHierarchy && !homePanel.gameObject.activeInHierarchy, "The Shop button did not open after scrolling the menu.");
        Capture("16-home-shop-menu", main, renderer, ui, target);
        Transform menuBar = renderer.transform.Find("Restored_Home/Menu/UI Root (2D)/Camera/Top Anchor/Menu_MenuBar");
        Transform end = menuBar.Find("MenuBase3");
        Require(ui.WorldToViewportPoint(end.TransformPoint(new Vector3(0.5f, 0, 0))).x >= 0.999f, "The original end cap stops before the wide right edge.");
        Transform menuButton = renderer.transform.Find("Restored_Home/Menu/UI Root (2D)/Camera/Right Top Anchor/Menu_MenuBtn/Menu_MenuBtn/MenuBtn");
        Vector2 menuTap = ui.WorldToScreenPoint(menuButton.position);
        Method("HandlePress").Invoke(renderer, new object[] { ui.ScreenPointToRay(menuTap), menuTap });
        yield return new WaitForSecondsRealtime(0.45f);
        Require(!menuBar.gameObject.activeInHierarchy && !buttons.gameObject.activeInHierarchy && !shopPanel.gameObject.activeInHierarchy && model.isActiveAndEnabled, "The top-right button did not close the selected submenu and navigation while keeping the scene.");
        Capture("12-home-menu-closed", main, renderer, ui, target);
        Method("HandlePress").Invoke(renderer, new object[] { ui.ScreenPointToRay(menuTap), menuTap });
        yield return new WaitForSecondsRealtime(0.45f);
        Require(menuBar.gameObject.activeInHierarchy && buttons.gameObject.activeInHierarchy && shopPanel.gameObject.activeInHierarchy && buttons.localPosition.x < initialMenuX, "The original menu did not reopen with its previous submenu and scroll offset.");
        Capture("13-home-menu-reopened", main, renderer, ui, target);

        string bannerFixture = Environment.GetEnvironmentVariable("ORDER05_BANNER_FIXTURE_URL");
        if (!String.IsNullOrEmpty(bannerFixture))
        {
            Transform left = (Transform)Field(typeof(OriginalSceneRenderer), "menuLeftArrow").GetValue(renderer);
            Press(renderer, ui, left.Find("SwipeMark_Left"));
            Press(renderer, ui, buttons.Find("MainBtn_Home/base"));
            JObject updated = (JObject)homeWire.DeepClone();
            updated["banners"] = new JArray(
                Banner(13, bannerFixture + "ignored.png"), Banner(12, bannerFixture + "missing.png"),
                Banner(12, bannerFixture + "cover-1.png"), Banner(12, bannerFixture + "cover-2.png"));
            FieldInfo homeState = Field(typeof(RecoveryBootstrap), "homeState");
            homeState.SetValue(state, FixtureHome(updated));
            float deadline = Time.unscaledTime + 8;
            while (!bannerItem.gameObject.activeInHierarchy && Time.unscaledTime < deadline) yield return null;
            Require(bannerItem.gameObject.activeInHierarchy, "A valid HomeShow cover did not reach the authored banner template.");
            yield return new WaitForSecondsRealtime(0.4f);
            Material bannerMaterial = bannerItem.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            Texture firstCover = bannerMaterial.mainTexture;
            Require(firstCover is Texture2D && !bannerItem.Find("Menu_Banner/Label").gameObject.activeSelf,
                "The loaded banner did not replace the authored placeholder text.");
            Capture("18-home-banner", main, renderer, ui, target);
            yield return new WaitForSecondsRealtime(5.6f);
            Require((int)Field(typeof(OriginalSceneRenderer), "homeBannerIndex").GetValue(renderer) == 1 &&
                    bannerMaterial.mainTexture != firstCover, "The Home carousel did not advance on its original timer.");
            Vector2 bannerTap = ui.WorldToScreenPoint(bannerItem.position);
            var bannerTouch = Method("HandleHomeBannerTouch");
            var bannerIndex = Field(typeof(OriginalSceneRenderer), "homeBannerIndex");
            Require((bool)bannerTouch.Invoke(renderer, new object[] { 1, TouchPhase.Began, bannerTap }), "The original banner did not capture a touch.");
            bannerTouch.Invoke(renderer, new object[] { 1, TouchPhase.Ended, bannerTap + new Vector2(0, 110) });
            Require((int)bannerIndex.GetValue(renderer) == 1, "A vertical swipe unexpectedly changed the banner.");
            bannerTouch.Invoke(renderer, new object[] { 1, TouchPhase.Began, bannerTap });
            bannerTouch.Invoke(renderer, new object[] { 1, TouchPhase.Ended, bannerTap + new Vector2(110, 0) });
            Require((int)bannerIndex.GetValue(renderer) == 0, "A right swipe did not show the previous banner.");
            bannerTouch.Invoke(renderer, new object[] { 1, TouchPhase.Began, bannerTap });
            bannerTouch.Invoke(renderer, new object[] { 1, TouchPhase.Ended, bannerTap - new Vector2(110, 0) });
            Require((int)bannerIndex.GetValue(renderer) == 1, "A left swipe did not show the next banner.");
            Press(renderer, ui, menuButton);
            yield return new WaitForSecondsRealtime(0.45f);
            Require(!bannerItem.gameObject.activeInHierarchy, "The banner stayed visible with the Home menu closed.");
            Press(renderer, ui, menuButton);
            yield return new WaitForSecondsRealtime(0.45f);
            Require(bannerItem.gameObject.activeInHierarchy, "The Home menu did not restore its loaded banner.");
            homeState.SetValue(state, FixtureHome(homeWire));
            yield return null;
            Require(!bannerItem.gameObject.activeSelf && bannerMaterial.mainTexture == null, "An empty refreshed reply kept the previous banner.");
        }
    }

    private static bool HasUathachPose(Live2DModelUnity model, int left, int right)
    {
        return Mathf.Approximately(model.getPartsOpacity("PARTS_01_ARM_L_01"), left == 1 ? 1 : 0) &&
               Mathf.Approximately(model.getPartsOpacity("PARTS_01_ARM_L_02"), left == 2 ? 1 : 0) &&
               Mathf.Approximately(model.getPartsOpacity("PARTS_01_ARM_R_01"), right == 1 ? 1 : 0) &&
               Mathf.Approximately(model.getPartsOpacity("PARTS_01_ARM_R_02"), right == 2 ? 1 : 0) &&
               Mathf.Approximately(model.getPartsOpacity("PARTS_01_ARM_R_03"), right == 3 ? 1 : 0);
    }

    private static CnHomeState FixtureHome(JObject wire)
    {
        return CnProtocol.ParseHome("{\"res_code\":0}\n" + wire.ToString(Newtonsoft.Json.Formatting.None) + "\n{}", 1);
    }

    private static JObject Banner(int type, string url)
    {
        return new JObject { { "home_banner_type", type }, { "image_url", url }, { "open_url", "none" } };
    }

    private static void Press(OriginalSceneRenderer renderer, Camera ui, Transform target)
    {
        Vector2 point = ui.WorldToScreenPoint(target.position);
        Method("HandlePress").Invoke(renderer, new object[] { ui.ScreenPointToRay(point), point });
    }

    private void Capture(string name, Camera main, OriginalSceneRenderer renderer, Camera ui, RenderTexture target)
    {
        var background = (Camera)Field(typeof(OriginalSceneRenderer), "worldCamera").GetValue(renderer);
        main.targetTexture = target;
        main.Render();
        if (background != null && background.isActiveAndEnabled)
        {
            background.targetTexture = target;
            background.aspect = (float)Width / Height;
            background.fieldOfView = 45f * (4f / 3f) / background.aspect;
            background.Render();
        }
        ui.Render();
        RenderTexture.active = target;
        var pixels = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
        File.WriteAllBytes(Path.Combine(Output, name + ".png"), pixels.EncodeToPNG());
        RenderTexture.active = null;
        Destroy(pixels);
    }
}
#endif
