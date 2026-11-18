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
        private readonly Dictionary<string, AssetBundle> loaded = new Dictionary<string, AssetBundle>();
        private UnityWebRequest pending;

        public T Load<T>(string bundle, string asset) where T : UnityEngine.Object
        {
            AssetBundle source;
            if (!Ready || !loaded.TryGetValue(bundle, out source)) throw new InvalidOperationException("Content is not ready.");
            T value = source.LoadAsset<T>(asset);
            if (value == null) throw new InvalidDataException("Required asset is absent: " + asset);
            return value;
        }

        public IEnumerator Initialize()
        {
            if (Ready || pending != null) yield break;
            Error = null;
            ContentManifest manifest = null;
            ContentCache cache = null;
            try
            {
                TextAsset json = Resources.Load<TextAsset>("IosContentManifest");
                if (json == null) throw new InvalidDataException("iOS resource manifest is absent.");
                manifest = ContentManifest.Parse(json.text, Application.unityVersion);
                cache = new ContentCache(Path.Combine(Application.persistentDataPath, "content-v1"));
            }
            catch (Exception error) { Error = error.Message; }
            if (Error != null) yield break;
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
                        string file = Path.Combine(Application.streamingAssetsPath, "RecoveryContent", item.file);
                        using (UnityWebRequest request = UnityWebRequest.Get(new Uri(file).AbsoluteUri))
                        {
                            pending = request;
                            request.downloadHandler = new DownloadHandlerFile(stage) { removeFileOnAbort = true };
                            request.timeout = 30;
                            yield return request.SendWebRequest();
                            try
                            {
                                if (request.result != UnityWebRequest.Result.Success) throw new IOException(request.error);
                                cache.Commit(item, stage);
                                installed = true;
                                Installed++;
                            }
                            catch (Exception error) { lastError = error.Message; }
                            finally { pending = null; if (File.Exists(stage)) File.Delete(stage); }
                        }
                        if (!installed && attempt < 2) yield return new WaitForSeconds(0.5f * (attempt + 1));
                    }
                    if (!installed) { Error = lastError; yield break; }
                }
                AssetBundleCreateRequest loading = AssetBundle.LoadFromFileAsync(cache.PathFor(item));
                yield return loading;
                if (loading.assetBundle == null) { Error = "The verified iOS resource bundle could not be loaded: " + item.name; yield break; }
                loaded.Add(item.name, loading.assetBundle);
            }
            Ready = true;
            Debug.Log("Recovery content ready: installed=" + Installed + ", reused=" + Reused + ", content=" + manifest.contentId);
        }

        private void OnDestroy()
        {
            if (pending != null) pending.Abort();
            foreach (AssetBundle bundle in loaded.Values) bundle.Unload(false);
            loaded.Clear();
        }
    }
}
