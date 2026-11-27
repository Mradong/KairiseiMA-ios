using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Kairisei.Recovery.Editor
{
    public static class RecoveryContentBuild
    {
        private const string OriginalWorkBundle = "original-work.bundle";
        private const string OriginalWorkDelivery = "recovery/work.dat";

        // Import only the scene snapshot and the textures it names and authenticates.
        private static AssetBundleBuild PrepareOriginalWorkAssets(string descriptor)
        {
            JObject layout = JObject.Parse(File.ReadAllText(descriptor));
            if ((int?)layout["schemaVersion"] != 1 || !(layout["definitions"] is JArray scenes) ||
                scenes.Count == 0 || !(layout["textures"] is JArray textures) || textures.Count == 0)
                throw new InvalidDataException("工作原场景资源清单格式不正确。");

            List<string> assets = new List<string> { descriptor };
            List<string> addresses = new List<string> { "originalwork" };
            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            string directory = Path.GetDirectoryName(descriptor).Replace('\\', '/');
            string cardDescriptor = directory + "/OriginalCards.json";
            IEnumerable<JToken> workTextures = textures;
            if (File.Exists(cardDescriptor))
            {
                JObject cards = JObject.Parse(File.ReadAllText(cardDescriptor));
                if ((int?)cards["schemaVersion"] != 1 || !(cards["cards"] is JObject) || !(cards["textures"] is JArray portraits))
                    throw new InvalidDataException("原卡牌资源清单格式不正确。");
                assets.Add(cardDescriptor);
                addresses.Add("originalcards");
                workTextures = workTextures.Concat(portraits);
            }
            foreach (JObject entry in workTextures)
            {
                string key = (string)entry["key"];
                string file = (string)entry["file"];
                if (String.IsNullOrEmpty(key) || !ContentManifest.ValidName(key) || key.Contains("/") ||
                    file != key + ".png" || !keys.Add(key))
                    throw new InvalidDataException("工作原场景贴图名称无效或重复。");
                string asset = directory + "/" + file;
                if (!File.Exists(asset) || ContentIntegrity.HashFile(asset) != (string)entry["sha256"])
                    throw new InvalidDataException("工作原场景贴图校验失败：" + file);
                TextureImporter texture = AssetImporter.GetAtPath(asset) as TextureImporter;
                if (texture == null) throw new InvalidDataException("工作原场景贴图未导入：" + file);
                texture.mipmapEnabled = false;
                texture.alphaIsTransparency = true;
                texture.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "iPhone", overridden = true, maxTextureSize = 8192, format = TextureImporterFormat.ASTC_6x6 });
                texture.SaveAndReimport();
                assets.Add(asset);
                addresses.Add(key);
            }
            foreach (JToken sprite in scenes.SelectTokens("$..resolved.texture"))
                if (!keys.Contains((string)sprite)) throw new InvalidDataException("工作原场景引用了未列出的贴图。");
            return new AssetBundleBuild { assetBundleName = OriginalWorkBundle, assetNames = assets.ToArray(), addressableNames = addresses.ToArray() };
        }

        // Server delivery is emitted outside the Unity asset import tree.
        public static void BuildIOS(string output)
        {
            output = Path.GetFullPath(output);
            string assets = Path.GetFullPath("Assets") + Path.DirectorySeparatorChar;
            if ((output.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar)
                .StartsWith(assets, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("iOS delivery resources must not be generated under Assets.");
            if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
                throw new InvalidOperationException("Use a new server resource output directory.");
            const string source = "Assets/Recovery/Content";
            string[] paths = Directory.GetFiles(source).Where(f => f.EndsWith(".png") || f.EndsWith(".json")).Select(f => f.Replace('\\','/')).OrderBy(f => f, StringComparer.Ordinal).ToArray();
            if (paths.Length != 10) throw new InvalidDataException("Expected the ten reviewed recovered texture and atlas metadata sources.");
            foreach (string file in paths.Where(f => f.EndsWith(".png")))
            {
                TextureImporter texture = AssetImporter.GetAtPath(file) as TextureImporter;
                if (texture == null) throw new InvalidDataException("Texture did not import: " + file);
                texture.mipmapEnabled = false;
                texture.alphaIsTransparency = true;
                texture.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "iPhone", overridden = true, maxTextureSize = 8192, format = TextureImporterFormat.ASTC_6x6 });
                texture.SaveAndReimport();
            }
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Recovery/Resources/RecoveryChinese.ttf");
            if (font == null || !font.HasCharacter('验') || !font.HasCharacter('战') || !font.HasCharacter('资')) throw new InvalidDataException("Recovered Chinese font failed Unity glyph checks.");
            string temporary = Path.GetFullPath("Temp/RecoveryContent/iOS");
            Directory.CreateDirectory(temporary);
            List<AssetBundleBuild> builds = new List<AssetBundleBuild> {
                new AssetBundleBuild { assetBundleName = "recovery-ui.bundle", assetNames = paths,
                    addressableNames = paths.Select(f => Path.GetFileNameWithoutExtension(f).ToLowerInvariant()).ToArray() }
            };
            const string originalSceneRoot = "Assets/Recovery/OriginalSceneContent";
            const string originalShader = "Assets/Recovery/Runtime/OriginalSceneSprite.shader";
            if (!File.Exists(Path.Combine(originalSceneRoot, "OriginalScenes.json"))) throw new InvalidDataException("原场景布局未准备。");
            string[] originalPaths = Directory.GetFiles(originalSceneRoot).Where(f => f.EndsWith(".png") || f.EndsWith(".json"))
                .Concat(new[] { originalShader }).Select(f => f.Replace('\\','/')).OrderBy(f => f, StringComparer.Ordinal).ToArray();
            foreach (string file in originalPaths.Where(f => f.EndsWith(".png")))
            {
                TextureImporter texture = AssetImporter.GetAtPath(file) as TextureImporter;
                if (texture == null) throw new InvalidDataException("原场景贴图未导入：" + file);
                texture.mipmapEnabled = false; texture.alphaIsTransparency = true;
                texture.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "iPhone", overridden = true, maxTextureSize = 8192, format = TextureImporterFormat.ASTC_6x6 });
                texture.SaveAndReimport();
            }
            builds.Add(new AssetBundleBuild { assetBundleName = "original-scenes.bundle", assetNames = originalPaths,
                addressableNames = originalPaths.Select(f => Path.GetFileNameWithoutExtension(f) == "OriginalSceneSprite" ? "original-scene-sprite" : Path.GetFileNameWithoutExtension(f).ToLowerInvariant()).ToArray() });
            const string originalWorkDescriptor = "Assets/Recovery/OriginalWorkContent/OriginalWork.json";
            bool originalWorkIncluded = File.Exists(originalWorkDescriptor);
            if (originalWorkIncluded) builds.Add(PrepareOriginalWorkAssets(originalWorkDescriptor));
            const string worldRoot="Assets/Recovery/OriginalWorldContent";
            string worldDescriptor=Path.Combine(worldRoot,"OriginalWorld.json");
            if(!File.Exists(worldDescriptor))throw new InvalidDataException("原主页背景/模型资源未准备。");
            Newtonsoft.Json.Linq.JObject world=Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(worldDescriptor));
            var worldAssets=new List<string>{worldDescriptor.Replace('\\','/')};
            var worldNames=new List<string>{"originalworld"};
            foreach(Newtonsoft.Json.Linq.JObject item in ((Newtonsoft.Json.Linq.JArray)world["addressableAssets"]).Concat((Newtonsoft.Json.Linq.JArray)world["shaderAssets"]))
            {
                string asset=(string)item["asset"];
                if(!asset.StartsWith(worldRoot+"/",StringComparison.Ordinal)||!File.Exists(asset))throw new InvalidDataException("Missing original world asset.");
                worldAssets.Add(asset);worldNames.Add((string)item["address"]);
            }
            builds.Add(new AssetBundleBuild { assetBundleName="original-world.bundle",assetNames=worldAssets.ToArray(),addressableNames=worldNames.ToArray() });
            const string combatSource = "Assets/Recovery/Combat/CombatTables.json";
            bool combatIncluded = File.Exists(combatSource);
            if (combatIncluded)
            {
                if (new FileInfo(combatSource).Length > CombatTableSet.MaximumBytes) throw new InvalidDataException("Combat snapshot exceeds the native input boundary.");
                CombatTableSet.Validate(File.ReadAllBytes(combatSource));
                AssetDatabase.ImportAsset(combatSource, ImportAssetOptions.ForceSynchronousImport);
                if (AssetDatabase.LoadAssetAtPath<TextAsset>(combatSource) == null) throw new InvalidDataException("Combat snapshot did not import as text.");
                builds.Add(new AssetBundleBuild { assetBundleName = "recovery-combat.bundle", assetNames = new[] { combatSource }, addressableNames = new[] { CombatTableSet.AssetName } });
            }
            AssetBundleManifest build = BuildPipeline.BuildAssetBundles(temporary, builds.ToArray(), BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, BuildTarget.iOS);
            if (build == null) throw new InvalidOperationException("iOS resource compilation failed.");
            Directory.CreateDirectory(output);
            List<ContentBundle> items = new List<ContentBundle>();
            foreach (AssetBundleBuild spec in builds)
            {
                if (build.GetAllDependencies(spec.assetBundleName).Length != 0) throw new InvalidDataException("Unexpected recovery bundle dependency: " + spec.assetBundleName);
                string built = Path.Combine(temporary, spec.assetBundleName);
                string hash = ContentIntegrity.HashFile(built);
                ContentBundle item = new ContentBundle { name = spec.assetBundleName == "recovery-ui.bundle" ? "recovery/ui.dat" : spec.assetBundleName == "original-scenes.bundle" ? OriginalSceneRenderer.BundleName : spec.assetBundleName == OriginalWorkBundle ? OriginalWorkDelivery : spec.assetBundleName=="original-world.bundle" ? OriginalNaviModel.BundleName : CombatTableSet.BundleName,
                    file = hash + ".bundle", bytes = new FileInfo(built).Length, sha256 = hash,
                    crc32 = ContentIntegrity.FileCrc(built).ToString("X8", CultureInfo.InvariantCulture), dependencies = new string[0] };
                File.Copy(built, Path.Combine(output, item.file), true);
                items.Add(item);
            }
            ContentManifest manifest = new ContentManifest { unityVersion = Application.unityVersion, bundles = items.ToArray() };
            manifest.contentId = ContentManifest.ComputeId(manifest.bundles);
            string clientManifest = JsonConvert.SerializeObject(manifest,Formatting.Indented);
            string publicManifest = Path.Combine(output,"client-manifest.json");
            File.WriteAllText(publicManifest,clientManifest,new UTF8Encoding(false));
            // Same bundle bytes can be served by the patched platform delivery layer.
            string catalogPath = Path.Combine(output,"catalog.dat");
            using (FileStream stream = File.Create(catalogPath)) using (GZipStream gzip = new GZipStream(stream,CompressionMode.Compress))
            {
                byte[] catalog = Encoding.UTF8.GetBytes("<v>,1\n" + String.Concat(items.Select(item => "<b>," + item.name + "," + item.crc32 + ",00000000," + item.bytes.ToString(CultureInfo.InvariantCulture) + ",N,1,0,0\n")));
                gzip.Write(catalog,0,catalog.Length);
            }
            var profile = new { platform = "iOS", unity_version = Application.unityVersion, content_id = manifest.contentId, artifact_root = ".",
                catalog = new { file = "catalog.dat", bytes = new FileInfo(catalogPath).Length, sha256 = ContentIntegrity.HashFile(catalogPath) },
                client_manifest = new { file = "client-manifest.json", bytes = new FileInfo(publicManifest).Length, sha256 = ContentIntegrity.HashFile(publicManifest) },
                bundles = items.Select(item => new { name = item.name, file = item.file, bytes = item.bytes, sha256 = item.sha256, crc32 = item.crc32 }).ToArray() };
            File.WriteAllText(Path.Combine(output,"ios-delivery.json"),JsonConvert.SerializeObject(profile,Formatting.Indented),new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(output,"resource-build.json"),JsonConvert.SerializeObject(new { generatedAt=DateTime.UtcNow.ToString("o"),platform="iOS",unityVersion=Application.unityVersion,manifest.contentId,bundleSha256=items[0].sha256,bundleBytes=items[0].bytes,sourceAssets=paths,combatTablesIncluded=combatIncluded,originalWorkIncluded,fontGlyphCheck=true,scope="Partial recovered UI content; optional supplied combat tables. Completeness and gameplay remain unverified.",gameplayReady=false },Formatting.Indented),new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("Compiled real iOS resource set: " + manifest.contentId + "; combat tables included=" + combatIncluded);
        }
    }
}
