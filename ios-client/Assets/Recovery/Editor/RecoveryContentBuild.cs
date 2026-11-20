using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Kairisei.Recovery.Editor
{
    public static class RecoveryContentBuild
    {
        public static void BuildIOS()
        {
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
            string output = "Assets/StreamingAssets/RecoveryContent";
            Directory.CreateDirectory(output);
            List<ContentBundle> items = new List<ContentBundle>();
            foreach (AssetBundleBuild spec in builds)
            {
                if (build.GetAllDependencies(spec.assetBundleName).Length != 0) throw new InvalidDataException("Unexpected recovery bundle dependency: " + spec.assetBundleName);
                string built = Path.Combine(temporary, spec.assetBundleName);
                string hash = ContentIntegrity.HashFile(built);
                ContentBundle item = new ContentBundle { name = spec.assetBundleName == "recovery-ui.bundle" ? "recovery/ui.dat" : CombatTableSet.BundleName,
                    file = hash + ".bundle", bytes = new FileInfo(built).Length, sha256 = hash,
                    crc32 = ContentIntegrity.FileCrc(built).ToString("X8", CultureInfo.InvariantCulture), dependencies = new string[0] };
                File.Copy(built, Path.Combine(output, item.file), true);
                items.Add(item);
            }
            ContentManifest manifest = new ContentManifest { unityVersion = Application.unityVersion, bundles = items.ToArray() };
            manifest.contentId = ContentManifest.ComputeId(manifest.bundles);
            string clientManifest = JsonConvert.SerializeObject(manifest,Formatting.Indented);
            File.WriteAllText("Assets/Recovery/Resources/IosContentManifest.json",clientManifest,new UTF8Encoding(false));
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
            File.WriteAllText(Path.Combine(output,"resource-build.json"),JsonConvert.SerializeObject(new { generatedAt=DateTime.UtcNow.ToString("o"),platform="iOS",unityVersion=Application.unityVersion,manifest.contentId,bundleSha256=items[0].sha256,bundleBytes=items[0].bytes,sourceAssets=paths,combatTablesIncluded=combatIncluded,fontGlyphCheck=true,scope="Partial recovered UI content; optional supplied combat tables. Completeness and gameplay remain unverified.",gameplayReady=false },Formatting.Indented),new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("Compiled real iOS resource set: " + manifest.contentId + "; combat tables included=" + combatIncluded);
        }
    }
}
