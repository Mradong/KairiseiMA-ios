using System;
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
            AssetBundleManifest build = BuildPipeline.BuildAssetBundles(temporary, new[] {
                new AssetBundleBuild { assetBundleName = "recovery-ui.bundle", assetNames = paths,
                    addressableNames = paths.Select(f => Path.GetFileNameWithoutExtension(f).ToLowerInvariant()).ToArray() }
            }, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, BuildTarget.iOS);
            if (build == null) throw new InvalidOperationException("iOS resource compilation failed.");
            string built = Path.Combine(temporary, "recovery-ui.bundle");
            string hash = ContentIntegrity.HashFile(built);
            string crc = ContentIntegrity.FileCrc(built).ToString("X8", CultureInfo.InvariantCulture);
            ContentBundle item = new ContentBundle { name = "recovery/ui.dat", file = hash + ".bundle", bytes = new FileInfo(built).Length, sha256 = hash, crc32 = crc, dependencies = new string[0] };
            ContentManifest manifest = new ContentManifest { unityVersion = Application.unityVersion, bundles = new[] { item } };
            manifest.contentId = ContentManifest.ComputeId(manifest.bundles);
            string output = "Assets/StreamingAssets/RecoveryContent";
            Directory.CreateDirectory(output);
            File.Copy(built, Path.Combine(output,item.file),true);
            string clientManifest = JsonConvert.SerializeObject(manifest,Formatting.Indented);
            File.WriteAllText("Assets/Recovery/Resources/IosContentManifest.json",clientManifest,new UTF8Encoding(false));
            string publicManifest = Path.Combine(output,"client-manifest.json");
            File.WriteAllText(publicManifest,clientManifest,new UTF8Encoding(false));
            // Same bundle bytes can be served by the patched platform delivery layer.
            string catalogPath = Path.Combine(output,"catalog.dat");
            using (FileStream stream = File.Create(catalogPath)) using (GZipStream gzip = new GZipStream(stream,CompressionMode.Compress))
            {
                byte[] catalog = Encoding.UTF8.GetBytes("<v>,1\n<b>,recovery/ui.dat,"+crc+",00000000,"+item.bytes.ToString(CultureInfo.InvariantCulture)+",N,1,0,0\n");
                gzip.Write(catalog,0,catalog.Length);
            }
            var profile = new { platform = "iOS", unity_version = Application.unityVersion, content_id = manifest.contentId, artifact_root = ".",
                catalog = new { file = "catalog.dat", bytes = new FileInfo(catalogPath).Length, sha256 = ContentIntegrity.HashFile(catalogPath) },
                client_manifest = new { file = "client-manifest.json", bytes = new FileInfo(publicManifest).Length, sha256 = ContentIntegrity.HashFile(publicManifest) },
                bundles = new[] { new { name = "recovery/ui.dat", file = item.file, bytes = item.bytes, sha256 = hash, crc32 = crc } } };
            File.WriteAllText(Path.Combine(output,"ios-delivery.json"),JsonConvert.SerializeObject(profile,Formatting.Indented),new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(output,"resource-build.json"),JsonConvert.SerializeObject(new { generatedAt=DateTime.UtcNow.ToString("o"),platform="iOS",unityVersion=Application.unityVersion,manifest.contentId,bundleSha256=hash,bundleBytes=item.bytes,sourceAssets=paths,fontGlyphCheck=true,scope="Partial recovered UI content and persistent-cache bootstrap; not a complete game resource set.",gameplayReady=false },Formatting.Indented),new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Debug.Log("Compiled real iOS resource bundle: " + hash);
        }
    }
}
