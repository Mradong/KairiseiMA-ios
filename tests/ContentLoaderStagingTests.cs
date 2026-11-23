using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Kairisei.Recovery;
using UnityEngine;

namespace UnityEngine {
    public class Object { }
    public class MonoBehaviour : Object { }
    public class TextAsset : Object { public string text; public byte[] bytes; }
    public static class Resources { public static T Load<T>(string name) where T : Object { return new TextAsset() as T; } }
    public static class Application { public static string unityVersion = "test", streamingAssetsPath = "C:/test", persistentDataPath = "C:/test"; }
    public static class Debug { public static void Log(string message) { } }
    public class WaitForSeconds { public WaitForSeconds(float value) { } }
    public class AssetBundle : Object {
        public bool unloaded;
        public void Unload(bool all) { unloaded = true; }
        public T LoadAsset<T>(string name) where T : Object { return null; }
        public static AssetBundleCreateRequest LoadFromFileAsync(string path) { return new AssetBundleCreateRequest { assetBundle = new AssetBundle() }; }
    }
    public class AssetBundleCreateRequest { public AssetBundle assetBundle; }
}
namespace UnityEngine.Networking {
    public class DownloadHandlerFile { public bool removeFileOnAbort; public DownloadHandlerFile(string path) { } }
    public class UnityWebRequestAsyncOperation { public bool isDone = true; }
    public class UnityWebRequest : IDisposable {
        public enum Result { Success, ConnectionError }
        public DownloadHandlerFile downloadHandler; public int timeout, redirectLimit; public ulong downloadedBytes;
        public Result result = Result.ConnectionError; public string error = "injected download failure";
        public static UnityWebRequest Get(string uri) { return new UnityWebRequest(); }
        public UnityWebRequestAsyncOperation SendWebRequest() { return new UnityWebRequestAsyncOperation(); }
        public void Abort() { } public void Dispose() { }
    }
}
namespace Kairisei.Recovery {
    public class NativeRegisteredBattle { public NativeRegisteredBattle(byte[] bytes) { } }
    public static class CombatTableSet { public const string BundleName = "combat", AssetName = "tables"; }
    public class CnLogin { public Uri VersionUrl; }
    public class ContentBundle { public string name, sha256, file, crc32; public long bytes; }
    public class ContentManifest {
        public string contentId = "test";
        public List<ContentBundle> bundles = new List<ContentBundle> {
            new ContentBundle { name="first", sha256="new", file="first", bytes=1 },
            new ContentBundle { name="second", sha256="new", file="second", bytes=1 }
        };
        public static ContentManifest Parse(string text, string version) { return new ContentManifest(); }
        public IEnumerable<ContentBundle> DependencyOrder() { return bundles; }
    }
    public class ContentCache {
        public static bool AllCached;
        public ContentCache(string path) { }
        public bool Contains(ContentBundle item) { return AllCached || item.name == "first"; }
        public string CreateStagingPath() { return Path.Combine(Path.GetTempPath(), "content-loader-staging-test"); }
        public void Commit(ContentBundle item, string stage) { throw new Exception("unexpected commit"); }
        public string PathFor(ContentBundle item) { return item.name; }
    }
}
public static class ContentLoaderStagingTests {
    static void Run(IEnumerator iterator) {
        while (iterator.MoveNext()) { IEnumerator child = iterator.Current as IEnumerator; if (child != null) Run(child); }
    }
    public static int Main() {
        var loader = new RecoveryContentLoader();
        var old = new AssetBundle();
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var loaded = (Dictionary<string, AssetBundle>)typeof(RecoveryContentLoader).GetField("loaded", flags).GetValue(loader);
        var hashes = (Dictionary<string, string>)typeof(RecoveryContentLoader).GetField("hashes", flags).GetValue(loader);
        loaded.Add("first", old); hashes.Add("first", "old");
        Run(loader.Initialize());
        if (old.unloaded || !System.Object.ReferenceEquals(loaded["first"], old) || hashes["first"] != "old" || loader.Error != "injected download failure" || loader.Ready)
            throw new Exception("Failed later download must leave the existing loaded bundle untouched and report an error.");
        Console.WriteLine("PASS: later download failure leaves previously loaded bundle intact");
        ContentCache.AllCached = true;
        Run(loader.Initialize());
        if (!old.unloaded || !loader.Ready || loader.Error != null || hashes["first"] != "new" || !loaded.ContainsKey("second"))
            throw new Exception("Complete verified cache must switch all bundles and become ready.");
        Console.WriteLine("PASS: complete verified cache switches bundles and becomes ready");
        return 0;
    }
}
