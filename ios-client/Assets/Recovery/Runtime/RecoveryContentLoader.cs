using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Kairisei.Recovery
{
    public sealed class RecoveryContentLoader : MonoBehaviour
    {
        public bool Ready { get; private set; }
        public string Error { get; private set; }
        public int Reused { get; private set; }
        public int Installed { get; private set; }
        public int TotalBundles { get; private set; }
        public long TotalBytes { get; private set; }
        public long VerifiedBytes { get; private set; }
        public string ActiveContentId { get { return activeManifest == null ? null : activeManifest.contentId; } }
        public string[] RequiredBundles { get; set; }
        private readonly Dictionary<string, AssetBundle> loaded = new Dictionary<string, AssetBundle>();
        private readonly Dictionary<string, string> hashes = new Dictionary<string, string>();
        private ContentManifest activeManifest;
        private ContentBundle[] activeOrder;
        private Dictionary<string, ContentBundle> activeBundles;
        private ContentCache activeCache;
        private UnityWebRequest pending;
        private bool running;
        private const string AddressAliasBundle = "recovery/address-aliases.dat";
        private ContentManifest addressAliasManifest;
        private Dictionary<string, string> addressAliases;

        public NativeRegisteredBattle CreateRegisteredBattle()
        {
            // Load only from the active, hash/CRC-verified resource set. Never
            // fall back to the synthetic NativeValidationTables resource.
            TextAsset tables = Load<TextAsset>(CombatTableSet.BundleName, CombatTableSet.AssetName);
            return new NativeRegisteredBattle(tables.bytes);
        }

        public T Load<T>(string bundle, string asset) where T : UnityEngine.Object
        {
            T value = LoadOptional<T>(bundle, asset);
            if (value == null) throw new InvalidDataException("Required asset is absent: " + asset);
            return value;
        }

        // The revision is downloaded and verified before activation. Open optional bundles
        // from disk when a scene needs them, in dependency order.
        public T LoadOptional<T>(string bundle, string asset) where T : UnityEngine.Object
        {
            if (!Ready) throw new InvalidOperationException("Content is not ready.");
            asset = ResolveOriginalAddress(bundle, asset);
            AssetBundle mounted;
            if (loaded.TryGetValue(bundle, out mounted)) return mounted.LoadAsset<T>(asset);
            if (activeBundles == null || !activeBundles.ContainsKey(bundle)) return null;

            var selected = SelectDependencies(activeBundles, new[] { bundle });
            foreach (ContentBundle item in activeOrder)
            {
                if (!selected.Contains(item.name) || loaded.ContainsKey(item.name)) continue;
                AssetBundle source = AssetBundle.LoadFromFile(activeCache.PathFor(item));
                if (source == null) throw new InvalidDataException("Verified iOS resource bundle could not be loaded: " + item.name);
                loaded.Add(item.name, source);
                hashes[item.name] = item.sha256;
            }
            return loaded[bundle].LoadAsset<T>(asset);
        }

        private string ResolveOriginalAddress(string bundle, string address)
        {
            if (bundle == AddressAliasBundle || activeBundles == null || !activeBundles.ContainsKey(AddressAliasBundle)) return address;
            if (addressAliases == null || !ReferenceEquals(addressAliasManifest, activeManifest))
            {
                TextAsset source = Load<TextAsset>(AddressAliasBundle, "address-aliases");
                JObject index = JObject.Parse(source.text);
                var entries = index["entries"] as JArray;
                if ((int?)index["format"] != 1 || entries == null)
                    throw new InvalidDataException("Invalid original resource address index.");

                var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (JObject entry in entries)
                {
                    string owner = (string)entry["bundle"];
                    string original = (string)entry["original"];
                    string targetAddress = (string)entry["target"];
                    if (string.IsNullOrEmpty(owner) || owner == AddressAliasBundle || !activeBundles.ContainsKey(owner) ||
                        string.IsNullOrEmpty(original) || string.IsNullOrEmpty(targetAddress) ||
                        original.IndexOf('\0') >= 0 || targetAddress.IndexOf('\0') >= 0 ||
                        StringComparer.OrdinalIgnoreCase.Equals(original, targetAddress))
                        throw new InvalidDataException("Invalid original resource address alias.");
                    string key = owner + "\0" + original;
                    if (aliases.ContainsKey(key)) throw new InvalidDataException("Duplicate original resource address alias.");
                    aliases.Add(key, targetAddress);
                }
                foreach (KeyValuePair<string, string> alias in aliases)
                {
                    string prefix = alias.Key.Substring(0, alias.Key.IndexOf('\0') + 1);
                    if (aliases.ContainsKey(prefix + alias.Value)) throw new InvalidDataException("Resource address alias points to another alias.");
                }
                addressAliases = aliases;
                addressAliasManifest = activeManifest;
            }
            string target;
            return addressAliases.TryGetValue(bundle + "\0" + address, out target) ? target : address;
        }

        private const string MediaIndexBundle = "media/index.dat";
        private ContentManifest mediaIndexManifest;
        private Dictionary<string, MediaEntry> mediaIndex;

        private sealed class MediaEntry
        {
            public ContentBundle bundle;
            public int bytes;
            public string sha256;
        }

        public byte[] LoadAudioCpk(string logicalName)
        {
            return LoadMediaBytes("audio", logicalName);
        }

        public byte[] LoadMovieMp4(string logicalName)
        {
            return LoadMediaBytes("movies", logicalName);
        }

        private byte[] LoadMediaBytes(string kind, string logicalName)
        {
            if (!Ready) throw new InvalidOperationException("Content is not ready.");
            if (!ValidMediaName(logicalName)) throw new ArgumentException("Invalid original media name.", nameof(logicalName));
            if (activeBundles == null || !activeBundles.ContainsKey(MediaIndexBundle)) return null;
            if (mediaIndex == null || !ReferenceEquals(mediaIndexManifest, activeManifest))
            {
                mediaIndex = ReadMediaIndex(Load<TextAsset>(MediaIndexBundle, "media-index").text);
                mediaIndexManifest = activeManifest;
            }
            if (!mediaIndex.TryGetValue(kind + "/" + logicalName, out MediaEntry entry)) return null;

            bool alreadyMounted = loaded.TryGetValue(entry.bundle.name, out AssetBundle bundle);
            if (!alreadyMounted) bundle = AssetBundle.LoadFromFile(activeCache.PathFor(entry.bundle));
            if (bundle == null) throw new InvalidDataException("Could not open verified media bundle: " + entry.bundle.name);
            try
            {
                TextAsset asset = bundle.LoadAsset<TextAsset>("media-bytes");
                byte[] bytes = asset == null ? null : asset.bytes;
                if (bytes == null || bytes.Length != entry.bytes || ContentIntegrity.Hash(bytes) != entry.sha256)
                    throw new InvalidDataException("Media bytes differ from the index: " + logicalName);
                return bytes;
            }
            finally
            {
                if (!alreadyMounted) bundle.Unload(true);
            }
        }

        private static bool ValidMediaName(string name)
        {
            return !String.IsNullOrEmpty(name) && name.EndsWith(".cpk", StringComparison.OrdinalIgnoreCase) &&
                name.IndexOf('/') < 0 && ContentManifest.ValidName("media/" + name.ToLowerInvariant());
        }

        private Dictionary<string, MediaEntry> ReadMediaIndex(string json)
        {
            JObject root = JObject.Parse(json);
            if ((int?)root["format"] != 1 || !ContentIntegrity.ValidHash((string)root["pcManifestSha256"]) ||
                !(root["entries"] is JArray entries) || entries.Count == 0)
                throw new InvalidDataException("Invalid original media index.");

            var index = new Dictionary<string, MediaEntry>(StringComparer.OrdinalIgnoreCase);
            var indexedBundles = new HashSet<string>(StringComparer.Ordinal);
            foreach (JObject item in entries)
            {
                string kind = (string)item["kind"];
                string name = (string)item["logicalName"];
                string sha256 = (string)item["sha256"];
                string bundle = (string)item["bundle"];
                int? bytes = (int?)item["bytes"];
                string key = kind + "/" + name;
                if ((kind != "audio" && kind != "movies") || !ValidMediaName(name) ||
                    (string)item["format"] != (kind == "audio" ? "cpk" : "mp4") || (string)item["address"] != "media-bytes" ||
                    !ContentIntegrity.ValidHash(sha256) || bundle != "media/" + kind + "/" + sha256 + ".dat" ||
                    !(bytes > 0) || !activeBundles.TryGetValue(bundle, out ContentBundle source) ||
                    source.dependencies.Length != 0 || index.ContainsKey(key) || !indexedBundles.Add(bundle))
                    throw new InvalidDataException("Invalid or duplicated original media entry: " + key);
                index.Add(key, new MediaEntry { bundle = source, bytes = bytes.Value, sha256 = sha256 });
            }
            foreach (string name in activeBundles.Keys)
            {
                if ((name.StartsWith("media/audio/", StringComparison.Ordinal) ||
                    name.StartsWith("media/movies/", StringComparison.Ordinal)) && !indexedBundles.Contains(name))
                    throw new InvalidDataException("Media bundle missing from the index: " + name);
            }
            return index;
        }

        // Compute the transitive set of bundles that must be opened together.
        private static HashSet<string> SelectDependencies(Dictionary<string, ContentBundle> index, IEnumerable<string> roots)
        {
            var selected = new HashSet<string>(StringComparer.Ordinal);
            var remaining = new Stack<string>(roots);
            while (remaining.Count > 0)
            {
                string name = remaining.Pop();
                ContentBundle item;
                if (!index.TryGetValue(name, out item) || !selected.Add(name)) continue;
                foreach (string dependency in item.dependencies ?? new string[0]) remaining.Push(dependency);
            }
            return selected;
        }

#if !UNITY_IOS || UNITY_EDITOR
        // Desktop tools can open explicitly staged fixtures.
        public IEnumerator Initialize()
        {
            if (running) yield break;
            running = true; Ready = false; Error = null;
            try
            {
                ContentManifest manifest = null;
                try
                {
                    TextAsset json = Resources.Load<TextAsset>("IosContentManifest");
                    if (json == null) throw new InvalidDataException("iOS resource manifest is absent.");
                    manifest = ContentManifest.Parse(json.text, Application.unityVersion);
                }
                catch (Exception error) { Error = error.Message; }
                if (Error != null) yield break;
                yield return Install(manifest, delegate(ContentBundle item) {
                    return new Uri(Path.Combine(Application.streamingAssetsPath,"RecoveryContent",item.file)).AbsoluteUri;
                });
            }
            finally { running = false; }
        }

#endif

        public IEnumerator UpdateFromServer(CnLogin login)
        {
            if (running) yield break;
            running = true; Ready = false; Error = null;
            TotalBundles = 0; TotalBytes = 0; VerifiedBytes = 0;
            string stage = null;
            try
            {
                ContentCache cache = null;
                try { cache = NewCache(); stage = cache.CreateStagingPath(); }
                catch (Exception error) { Error = error.Message; }
                if (Error != null) yield break;
                yield return Download(new Uri(login.VersionUrl,"client-manifest.json").AbsoluteUri,stage,
                    ContentManifest.MaximumManifestBytes,delegate(string error) { Error = error; });
                if (Error != null) yield break;
                ContentManifest manifest = null;
                try
                {
                    manifest = ContentManifest.Parse(File.ReadAllText(stage),Application.unityVersion);
                    string revision = login.VersionUrl.Segments[login.VersionUrl.Segments.Length-1].Trim('/');
                    if (revision != manifest.contentId) throw new InvalidDataException("Server resource revision differs from login.");
                }
                catch (Exception error) { Error = error.Message; }
                if (Error != null) yield break;
                yield return Install(manifest,delegate(ContentBundle item) { return new Uri(login.VersionUrl,"patch/"+item.name+".v"+item.crc32).AbsoluteUri; });
            }
            finally { if (stage != null && File.Exists(stage)) File.Delete(stage); running = false; }
        }

        private ContentCache NewCache() { return new ContentCache(Path.Combine(Application.persistentDataPath,"content-v1")); }

        private IEnumerator Install(ContentManifest manifest, Func<ContentBundle,string> location)
        {
            ContentBundle[] ordered = manifest.DependencyOrder();
            var index = new Dictionary<string, ContentBundle>(ordered.Length, StringComparer.Ordinal);
            foreach (ContentBundle item in ordered) index.Add(item.name, item);
            var previouslyLoaded = new HashSet<string>(loaded.Keys, StringComparer.Ordinal);
            HashSet<string> loadNames = null;
            if (RequiredBundles != null)
            {
                var roots = new List<string>(previouslyLoaded);
                foreach (string required in RequiredBundles)
                {
                    if (!index.ContainsKey(required))
                    {
                        Error = "当前资源缺少必需的真实场景包：" + required;
                        yield break;
                    }
                    roots.Add(required);
                }
                loadNames = SelectDependencies(index, roots);
            }
            ContentCache cache = null;
            try { cache = NewCache(); }
            catch (Exception error) { Error = error.Message; }
            if (Error != null) yield break;
            Installed = 0; Reused = 0; VerifiedBytes = 0;
            TotalBundles = ordered.Length;
            TotalBytes = 0;
            foreach (ContentBundle item in ordered) TotalBytes += item.bytes;
            foreach (ContentBundle item in ordered)
            {
                bool cached = false;
                try { cached = cache.Contains(item); }
                catch (Exception error) { Error = error.Message; }
                if (Error != null) yield break;
                if (cached) Reused++;
                else
                {
                    bool installed = false;
                    string lastError = null;
                    for (int attempt = 0; attempt < 3 && !installed; attempt++)
                    {
                        string stage = cache.CreateStagingPath();
                        string uri = null;
                        try { uri = location(item); }
                        catch (Exception error) { lastError = error.Message; }
                        if (uri == null) { Error = lastError; yield break; }
                        yield return Download(uri,stage,item.bytes,delegate(string error) { lastError = error; });
                        try
                        {
                            if (lastError == null) { cache.Commit(item,stage); installed = true; Installed++; }
                        }
                        catch (Exception error) { lastError = error.Message; }
                        finally { if (File.Exists(stage)) File.Delete(stage); }
                        if (!installed && attempt < 2) yield return new WaitForSeconds(0.5f*(attempt+1));
                    }
                    if (!installed) { Error = lastError; yield break; }
                }
                VerifiedBytes += item.bytes;
                // Let the UI update while rechecking many cached files.
                if ((Installed + Reused) % 16 == 0) yield return null;
            }
            // Activate only after every file is verified. On iOS, mount the startup
            // bundles and their dependencies, leaving the remaining files on disk.
            foreach (ContentBundle item in ordered)
            {
                if (loadNames != null && !loadNames.Contains(item.name)) continue;
                string previousHash;
                if (hashes.TryGetValue(item.name,out previousHash) && previousHash == item.sha256) continue;
                AssetBundle previous;
                if (loaded.TryGetValue(item.name,out previous)) { previous.Unload(false); loaded.Remove(item.name); hashes.Remove(item.name); }
                AssetBundleCreateRequest loading = null;
                try { loading = AssetBundle.LoadFromFileAsync(cache.PathFor(item)); }
                catch (Exception error) { Error = error.Message; }
                if (Error != null) { yield return Rollback(previouslyLoaded); yield break; }
                yield return loading;
                if (loading.assetBundle == null) { Error = "Verified iOS resource bundle could not be loaded: "+item.name; yield return Rollback(previouslyLoaded); yield break; }
                loaded.Add(item.name,loading.assetBundle); hashes[item.name] = item.sha256;
            }
            foreach (string obsolete in new List<string>(loaded.Keys))
            {
                if (index.ContainsKey(obsolete)) continue;
                loaded[obsolete].Unload(false); loaded.Remove(obsolete); hashes.Remove(obsolete);
            }
            activeManifest = manifest;
            activeOrder = ordered;
            activeBundles = index;
            activeCache = cache;
            Ready = true;
            Debug.Log("Recovery content ready: installed="+Installed+", reused="+Reused+", content="+manifest.contentId);
        }

        // Restore the previously opened bundle set from the prior verified revision.
        private IEnumerator Rollback(HashSet<string> previouslyLoaded)
        {
            string failure = Error;
            foreach (AssetBundle bundle in loaded.Values) bundle.Unload(false);
            loaded.Clear(); hashes.Clear();
            string rollbackError = null;
            if (activeManifest != null)
            {
                foreach (ContentBundle item in activeOrder)
                {
                    if (!previouslyLoaded.Contains(item.name)) continue;
                    AssetBundleCreateRequest loading = null;
                    try
                    {
                        if (!activeCache.Contains(item)) throw new InvalidDataException("Previous cached resource failed verification: " + item.name);
                        loading = AssetBundle.LoadFromFileAsync(activeCache.PathFor(item));
                    }
                    catch (Exception error) { rollbackError = error.Message; }
                    if (rollbackError != null) break;
                    yield return loading;
                    if (loading.assetBundle == null) { rollbackError = "Previous resource could not be loaded: " + item.name; break; }
                    loaded.Add(item.name, loading.assetBundle); hashes[item.name] = item.sha256;
                }
            }
            if (rollbackError != null)
            {
                foreach (AssetBundle bundle in loaded.Values) bundle.Unload(false);
                loaded.Clear(); hashes.Clear(); activeManifest = null;
                activeOrder = null; activeBundles = null; activeCache = null;
            }
            // The attempted update failed even if the previous set was restored.
            // Callers must not mistake recovery of old content for update success.
            Ready = false;
            Error = rollbackError == null ? failure : failure + "; rollback failed: " + rollbackError;
        }

        private IEnumerator Download(string uri, string stage, long maximum, Action<string> receive)
        {
            UnityWebRequest request = null;
            UnityWebRequestAsyncOperation operation = null;
            string setupError = null;
            try
            {
                request = UnityWebRequest.Get(uri);
                request.downloadHandler = new DownloadHandlerFile(stage) { removeFileOnAbort = true };
                // Stop inactive transfers below, while allowing large bundles to complete over a slow connection.
                request.timeout = 0;
                request.redirectLimit = 0;
                pending = request;
                operation = request.SendWebRequest();
            }
            catch (Exception error) { setupError = error.Message; }
            if (setupError != null) { if (request != null) request.Dispose(); pending = null; receive(setupError); yield break; }
            using (request)
            {
                ulong observed = 0;
                string failure = null;
                var lastProgress = System.Diagnostics.Stopwatch.StartNew();
                while (!operation.isDone)
                {
                    ulong current = request.downloadedBytes;
                    if (current > (ulong)maximum) failure = "Resource exceeds its declared size.";
                    if (current > observed) { observed = current; lastProgress.Restart(); }
                    if (lastProgress.Elapsed.TotalSeconds >= 60) failure = "资源下载已长时间没有进度，请重试。";
                    if (failure != null) { request.Abort(); break; }
                    yield return null;
                }
                pending = null;
                if (request.downloadedBytes > (ulong)maximum) failure = "Resource exceeds its declared size.";
                receive(failure ?? (request.result != UnityWebRequest.Result.Success ? request.error : null));
            }
        }

        private void OnDestroy()
        {
            if (pending != null) pending.Abort();
            foreach (AssetBundle bundle in loaded.Values) bundle.Unload(false);
            loaded.Clear(); hashes.Clear();
        }
    }
}
