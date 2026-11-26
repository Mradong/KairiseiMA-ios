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
            string output = Path.GetFullPath("scene-preview");
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
            PlayerSettings.bundleVersion = "0.0.10";
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
        renderer.Show("IntroSlPl");
        renderer.Show("Title");
        Method("Update").Invoke(renderer, null);
        Capture("00-title-entry-start", main, renderer, ui, target);
        yield return new WaitForSecondsRealtime(0.55f);
        Capture("01-title", main, renderer, ui, target);
        foreach (var point in new[] { new Vector2(160, 900), new Vector2(Width / 2, Height / 2), new Vector2(Width - 160, 900) })
            Require((bool)Method("IsTitlePress").Invoke(renderer, new object[] { ui.ScreenPointToRay(point) }), "Title input missed outside the prompt.");
        yield return new WaitForSecondsRealtime(0.3f);
        Capture("02-title-prompt-cycle", main, renderer, ui, target);
        var titleTap = new Vector2(160, 900);
        Method("HandlePress").Invoke(renderer, new object[] { ui.ScreenPointToRay(titleTap), titleTap });
        Require((bool)Field(typeof(OriginalSceneRenderer), "titleWaiting").GetValue(renderer), "The title outside its prompt gave no immediate feedback.");
        Field(typeof(RecoveryBootstrap), "busy").SetValue(state, true);
        yield return new WaitForSecondsRealtime(0.55f);
        Capture("03-title-connecting", main, renderer, ui, target);
        var connecting = (GameObject)Field(typeof(OriginalSceneRenderer), "titleConnecting").GetValue(renderer);
        Require(connecting.activeInHierarchy, "The original connecting sprite did not activate.");
        Field(typeof(RecoveryBootstrap), "busy").SetValue(state, false);
        renderer.ShowError("无法连接服务器，请检查网络后再试。连接恢复后会继续使用当前游客身份。");
        yield return new WaitForSecondsRealtime(0.28f);
        Capture("04-title-error", main, renderer, ui, target);
        renderer.Show("IntroSlPl");
        yield return new WaitForSecondsRealtime(0.62f);
        Capture("05-role-selection", main, renderer, ui, target);
        Method("SelectRole").Invoke(renderer, new object[] { 2 });
        yield return new WaitForSecondsRealtime(0.4f);
        Capture("06-role-selected", main, renderer, ui, target);
        var home = new CnHomeState
        {
            UserId=1, Name="画面检查", Level=3, Bp=20, BpMax=20, Ap=1, ApMax=3, Gold=200,
            Wire=JObject.Parse("{\"user\":{\"coin\":0,\"coin_free\":2,\"bp_next_sec\":0}}")
        };
        Field(typeof(RecoveryBootstrap), "homeState").SetValue(state, home);
        renderer.Show("Home");
        Capture("07-home-entry-start", main, renderer, ui, target);
        var world = (Camera)Field(typeof(OriginalSceneRenderer), "worldCamera").GetValue(renderer);
        var model = (OriginalNaviModel)Field(typeof(OriginalSceneRenderer), "navi").GetValue(renderer);
        float enteringPosition = model.transform.position.x;
        yield return new WaitForSecondsRealtime(0.18f);
        Capture("08-home-entry-mid", main, renderer, ui, target);
        yield return new WaitForSecondsRealtime(0.25f);
        Capture("09-home", main, renderer, ui, target);
        Require(model.transform.position.x > enteringPosition + 10, "The actual navi transform did not animate.");
        bool touched = false;
        for (int x = 300; x <= Width / 2 && !touched; x += 45)
            for (int y = 200; y < Height - 140 && !touched; y += 45)
                touched = model.TryPlayTouch(world.ScreenPointToRay(new Vector2(x, y)));
        Require(touched, "The original navi hit area did not receive a visible tap.");
        yield return new WaitForSecondsRealtime(0.8f);
        Capture("10-home-navi-touch", main, renderer, ui, target);
        Transform buttons = (Transform)Field(typeof(OriginalSceneRenderer), "menuButtons").GetValue(renderer);
        Transform right = (Transform)Field(typeof(OriginalSceneRenderer), "menuRightArrow").GetValue(renderer);
        float initialMenuX = buttons.localPosition.x;
        Vector2 arrowTap = ui.WorldToScreenPoint(right.Find("SwipeMark_Right").position);
        Method("HandlePress").Invoke(renderer, new object[] { ui.ScreenPointToRay(arrowTap), arrowTap });
        Require(buttons.localPosition.x < initialMenuX, "The original scroll arrow did not move the clipped menu.");
        Capture("11-home-menu-scroll", main, renderer, ui, target);
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
