using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Kairisei.Recovery;

namespace UnityEngine {
    public class Object { }
    public class MonoBehaviour : Object { }
    public class TextAsset : Object { public string text; public byte[] bytes; }
    public static class Resources { public static T Load<T>(string name) where T : Object { return null; } }
    public static class Application { public static string unityVersion = CnProtocol.UnityVersion, streamingAssetsPath, persistentDataPath; }
    public static class Debug { public static void Log(string text) { } }
    public class WaitForSeconds { public WaitForSeconds(float seconds) { } }
    public class AssetBundle : Object {
        public static readonly HashSet<string> FailPaths = new HashSet<string>();
        public void Unload(bool all) { }
        public T LoadAsset<T>(string name) where T : Object { return null; }
        public static AssetBundleCreateRequest LoadFromFileAsync(string path) { return new AssetBundleCreateRequest { assetBundle = FailPaths.Contains(path) ? null : new AssetBundle() }; }
    }
    public class AssetBundleCreateRequest { public AssetBundle assetBundle; }
}
namespace UnityEngine.Networking {
    public class DownloadHandlerFile { public bool removeFileOnAbort; public DownloadHandlerFile(string path) { } }
    public class UnityWebRequestAsyncOperation { public bool isDone = true; }
    public class UnityWebRequest : IDisposable {
        public enum Result { Success, ConnectionError }
        public DownloadHandlerFile downloadHandler; public int timeout, redirectLimit; public ulong downloadedBytes;
        public Result result = Result.ConnectionError; public string error = "Unexpected network request";
        public static UnityWebRequest Get(string uri) { return new UnityWebRequest(); }
        public UnityWebRequestAsyncOperation SendWebRequest() { return new UnityWebRequestAsyncOperation(); }
        public void Abort() { } public void Dispose() { }
    }
}
namespace Kairisei.Recovery {
    public static class CnProtocol { public const string UnityVersion = "6000.3.26f1"; }
    public class CnLogin { public Uri VersionUrl; }
    public class NativeRegisteredBattle { public NativeRegisteredBattle(byte[] data) { } }
    public static class CombatTableSet { public const string BundleName = "combat", AssetName = "tables"; }
}
public static class ContentLoaderRealCacheTests {
    static void Run(IEnumerator iterator) { while (iterator.MoveNext()) { var child = iterator.Current as IEnumerator; if (child != null) Run(child); } }
    static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    static void Install(RecoveryContentLoader loader, ContentManifest manifest) {
        typeof(RecoveryContentLoader).GetProperty("Error").GetSetMethod(true).Invoke(loader, new object[] { null });
        typeof(RecoveryContentLoader).GetProperty("Ready").GetSetMethod(true).Invoke(loader, new object[] { false });
        var install = typeof(RecoveryContentLoader).GetMethod("Install", BindingFlags.Instance | BindingFlags.NonPublic);
        Run((IEnumerator)install.Invoke(loader, new object[] { manifest, new Func<ContentBundle,string>(b => "file:///unused") }));
    }
    static void Add(ContentCache cache, ContentBundle item, string source) {
        string stage = cache.CreateStagingPath(); File.Copy(source, stage); cache.Commit(item, stage);
    }
    public static int Main(string[] args) {
        var oldManifest = ContentManifest.Parse(File.ReadAllText(args[0]), CnProtocol.UnityVersion);
        var newManifest = ContentManifest.Parse(File.ReadAllText(args[2]), CnProtocol.UnityVersion);
        Assert(oldManifest.bundles.Length == 1 && newManifest.bundles.Length == 1, "Expected one bundle per manifest");
        var oldBundle = oldManifest.bundles[0]; var newBundle = newManifest.bundles[0];
        Assert(oldBundle.name == newBundle.name && oldBundle.sha256 != newBundle.sha256, "Expected distinct same-name revisions");
        UnityEngine.Application.persistentDataPath = args[4];
        var cache = new ContentCache(Path.Combine(args[4], "content-v1"));
        Add(cache, oldBundle, args[1]); Add(cache, newBundle, args[3]);
        var loader = new RecoveryContentLoader();
        Install(loader, oldManifest);
        Assert(loader.Ready && cache.Contains(oldBundle), "Actual previous resource must be ready");
        string newPath = cache.PathFor(newBundle), oldPath = cache.PathFor(oldBundle);
        UnityEngine.AssetBundle.FailPaths.Add(newPath);
        Install(loader, newManifest);
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var hashes = (Dictionary<string,string>)typeof(RecoveryContentLoader).GetField("hashes", flags).GetValue(loader);
        Assert(!loader.Ready && loader.Error != null && hashes.Count == 1 && hashes[oldBundle.name] == oldBundle.sha256, "Expected rollback to actual verified previous resource");
        Assert(cache.Contains(oldBundle) && cache.Contains(newBundle), "Rollback must preserve both actual cached versions");
        Console.WriteLine("PASS: real cache and manifests restore verified previous revision after simulated load failure");
        File.WriteAllText(oldPath, "corrupt");
        Install(loader, newManifest);
        Assert(!loader.Ready && loader.Error != null && loader.Error.Contains("rollback failed") && hashes.Count == 0, "Corrupt old cache must not be restored");
        Assert(!cache.Contains(oldBundle) && cache.Contains(newBundle), "New verified cache must survive failed rollback");
        Console.WriteLine("PASS: real cache rejects corrupted rollback revision and preserves new revision");
        UnityEngine.AssetBundle.FailPaths.Clear();
        Install(loader, newManifest);
        Assert(loader.Ready && hashes.Count == 1 && hashes[newBundle.name] == newBundle.sha256, "Retry must load valid new revision");
        Console.WriteLine("PASS: valid new revision remains usable on retry");
        return 0;
    }
}
