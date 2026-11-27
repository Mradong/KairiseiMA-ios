using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        public string ActiveContentId { get { return activeManifest == null ? null : activeManifest.contentId; } }
        public string[] RequiredBundles { get; set; }
        private readonly Dictionary<string, AssetBundle> loaded = new Dictionary<string, AssetBundle>();
        private readonly Dictionary<string, string> hashes = new Dictionary<string, string>();
        private ContentManifest activeManifest;
        private UnityWebRequest pending;
        private bool running;

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

        // Decode optional assets from an already verified bundle. The bundle itself must be ready.
        public T LoadOptional<T>(string bundle, string asset) where T : UnityEngine.Object
        {
            AssetBundle source;
            if (!Ready || !loaded.TryGetValue(bundle, out source)) throw new InvalidOperationException("Content is not ready.");
            return source.LoadAsset<T>(asset);
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
            string stage = null;
            try
            {
                ContentCache cache = null;
                try { cache = NewCache(); stage = cache.CreateStagingPath(); }
                catch (Exception error) { Error = error.Message; }
                if (Error != null) yield break;
                yield return Download(new Uri(login.VersionUrl,"client-manifest.json").AbsoluteUri,stage,1024*1024,delegate(string error) { Error = error; });
                if (Error != null) yield break;
                ContentManifest manifest = null;
                try
                {
                    manifest = ContentManifest.Parse(File.ReadAllText(stage),Application.unityVersion);
                    string revision = login.VersionUrl.Segments[login.VersionUrl.Segments.Length-1].Trim('/');
                    if (revision != manifest.contentId) throw new InvalidDataException("Server resource revision differs from login.");
                    if (RequiredBundles != null)
                        foreach (string required in RequiredBundles)
                        {
                            bool found = false;
                            foreach (ContentBundle bundle in manifest.DependencyOrder()) if (bundle.name == required) found = true;
                            if (!found) throw new InvalidDataException("服务器资源尚未包含当前客户端所需的真实场景，请先更新资源交付配置。");
                        }
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
            if(RequiredBundles!=null)
                foreach(string required in RequiredBundles)
                {
                    bool found=false;
                    foreach(ContentBundle item in manifest.DependencyOrder())if(item.name==required)found=true;
                    if(!found){Error="当前资源缺少必需的真实场景包："+required;yield break;}
                }
            ContentCache cache = null;
            try { cache = NewCache(); }
            catch (Exception error) { Error = error.Message; }
            if (Error != null) yield break;
            Installed = 0; Reused = 0;
            foreach (ContentBundle item in manifest.DependencyOrder())
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
            }
            // Verify every download before replacing any currently loaded bundle.
            // Unity may reject two revisions of the same bundle loaded at once,
            // so loading still happens sequentially after the cache is complete.
            foreach (ContentBundle item in manifest.DependencyOrder())
            {
                string previousHash;
                if (hashes.TryGetValue(item.name,out previousHash) && previousHash == item.sha256) continue;
                AssetBundle previous;
                if (loaded.TryGetValue(item.name,out previous)) { previous.Unload(false); loaded.Remove(item.name); hashes.Remove(item.name); }
                AssetBundleCreateRequest loading = null;
                try { loading = AssetBundle.LoadFromFileAsync(cache.PathFor(item)); }
                catch (Exception error) { Error = error.Message; }
                if (Error != null) { yield return Rollback(cache); yield break; }
                yield return loading;
                if (loading.assetBundle == null) { Error = "Verified iOS resource bundle could not be loaded: "+item.name; yield return Rollback(cache); yield break; }
                loaded.Add(item.name,loading.assetBundle); hashes[item.name] = item.sha256;
            }
            HashSet<string> currentNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (ContentBundle item in manifest.bundles) currentNames.Add(item.name);
            foreach (string obsolete in new List<string>(loaded.Keys))
            {
                if (currentNames.Contains(obsolete)) continue;
                loaded[obsolete].Unload(false); loaded.Remove(obsolete); hashes.Remove(obsolete);
            }
            activeManifest = manifest;
            Ready = true;
            Debug.Log("Recovery content ready: installed="+Installed+", reused="+Reused+", content="+manifest.contentId);
        }

        private IEnumerator Rollback(ContentCache cache)
        {
            string failure = Error;
            foreach (AssetBundle bundle in loaded.Values) bundle.Unload(false);
            loaded.Clear(); hashes.Clear();
            string rollbackError = null;
            if (activeManifest != null)
            {
                foreach (ContentBundle item in activeManifest.DependencyOrder())
                {
                    AssetBundleCreateRequest loading = null;
                    try
                    {
                        if (!cache.Contains(item)) throw new InvalidDataException("Previous cached resource failed verification: " + item.name);
                        loading = AssetBundle.LoadFromFileAsync(cache.PathFor(item));
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
                // A known 37-second scene download exceeds the old 30-second limit. Bound larger transfers by size.
                request.timeout = (int)Math.Min(600L, Math.Max(60L, 30L + maximum / (256L * 1024)));
                request.redirectLimit = 0;
                pending = request;
                operation = request.SendWebRequest();
            }
            catch (Exception error) { setupError = error.Message; }
            if (setupError != null) { if (request != null) request.Dispose(); pending = null; receive(setupError); yield break; }
            using (request)
            {
                while (!operation.isDone)
                {
                    if (request.downloadedBytes > (ulong)maximum) { request.Abort(); break; }
                    yield return null;
                }
                pending = null;
                receive(request.downloadedBytes > (ulong)maximum ? "Resource exceeds its declared size." : request.result != UnityWebRequest.Result.Success ? request.error : null);
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
