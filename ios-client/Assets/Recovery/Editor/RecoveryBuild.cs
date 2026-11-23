using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kairisei.Recovery.Editor
{
    public static class RecoveryBuild
    {
        private const string ScenePath = "Assets/Recovery/Scenes/Recovery.unity";

        [Serializable]
        private sealed class BuildStage
        {
            public string stage = "ios-resource-cache-bootstrap";
            public bool gameplayReady = false;
            public bool nativeBattleValidation = true;
            public string unityVersion;
            public string platform = "iOS";
            public string deviceModel = "iPhone 13";
            public string deviceOS = "17.4.1";
            public string generatedAt;
        }

        [InitializeOnLoadMethod]
        private static void PrepareFirstOpen()
        {
            if (Application.isBatchMode || File.Exists(ScenePath)) return;
            EditorApplication.delayCall += delegate {
                if (!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode) CreateScene();
            };
        }

        [MenuItem("Recovery/Create Connection Test Scene")]
        public static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Recovery/Scenes");
            AssetDatabase.Refresh();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject cameraObject = new GameObject("Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.06f, 0.12f);
            new GameObject("RecoveryBootstrap").AddComponent<RecoveryBootstrap>();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save recovery scene");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Recovery/Build iOS Connection Test")]
        public static void BuildIOS()
        {
            if (Application.unityVersion != CnProtocol.UnityVersion)
                throw new InvalidOperationException("Use Unity " + CnProtocol.UnityVersion + "; resource profiles are version-specific.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
                throw new InvalidOperationException("Unity iOS Build Support is not installed.");
            RecoveryContentBuild.BuildIOS();
            CreateScene();
            PlayerSettings.companyName = "KairiseiRecovery";
            PlayerSettings.productName = "Kairisei Connection Test";
            PlayerSettings.bundleVersion = "0.0.8";
            PlayerSettings.iOS.buildNumber = "8";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "local.kairisei.recovery");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(NamedBuildTarget.iOS, 1); // ARM64
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            string output = Path.GetFullPath("Builds/iOS");
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < arguments.Length; i++) if (arguments[i] == "-recoveryOutput") output = Path.GetFullPath(arguments[i + 1]);
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = output, target = BuildTarget.iOS, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("iOS build failed: " + report.summary.result);
            File.WriteAllText(Path.Combine(output, "recovery-build.json"), JsonUtility.ToJson(new BuildStage {
                unityVersion = Application.unityVersion, generatedAt = DateTime.UtcNow.ToString("o")
            }, true));
            Debug.Log("Recovery connection test exported to: " + output);
        }
    }
}
