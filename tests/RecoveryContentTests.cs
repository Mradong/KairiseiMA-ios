using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Kairisei.Recovery;

public static class RecoveryContentTests
{
    private static int checks;
    private static void Check(bool value, string label) { checks++; if (!value) throw new Exception(label); }
    private static void Reject(Action action, string label) { bool rejected=false; try { action(); } catch (Exception error) { if (error is FormatException || error is JsonException || error is ArgumentException || error is InvalidDataException) rejected=true; else throw; } Check(rejected,label); }
    private static ContentBundle Bundle(string name, byte[] bytes, string directory)
    {
        string source=Path.Combine(directory,Guid.NewGuid().ToString("N"));File.WriteAllBytes(source,bytes);
        string hash=ContentIntegrity.Hash(bytes);
        return new ContentBundle {name=name,file=hash+".bundle",sha256=hash,bytes=bytes.Length,crc32=ContentIntegrity.FileCrc(source).ToString("X8"),dependencies=new string[0]};
    }
    private static string Json(params ContentBundle[] items) { return JsonConvert.SerializeObject(new ContentManifest {unityVersion=CnProtocol.UnityVersion,bundles=items,contentId=ContentManifest.ComputeId(items)}); }
    private static string Stage(ContentCache cache, byte[] bytes) {string file=cache.CreateStagingPath();File.WriteAllBytes(file,bytes);return file;}
    public static int Main(string[] args)
    {
        string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
        byte[] original=Encoding.ASCII.GetBytes("123456789"), updated=Encoding.ASCII.GetBytes("223456789");
        ContentBundle first=Bundle("recovery/ui",original,root), next=Bundle("recovery/ui",updated,root);
        Check(first.crc32=="CBF43926","IEEE CRC must match independent standard vector");
        Check(first.sha256=="15e2b0d3c33891ebb0f1ef609ec419420c20e320ce94c65fbc8c3312448eb225","SHA-256 standard vector");
        string manifest=Json(first);
        Check(ContentManifest.Parse(manifest,CnProtocol.UnityVersion).bundles[0].name==first.name,"manifest round trip");
        Reject(()=>ContentManifest.Parse(manifest,"5.3.5p1"),"old Unity serialized content rejected");
        Reject(()=>ContentManifest.Parse(manifest.Replace("iOS","Android"),CnProtocol.UnityVersion),"different platform rejected");
        Reject(()=>ContentManifest.Parse(manifest.Replace("\"platform\":\"iOS\",",""),CnProtocol.UnityVersion),"missing platform rejected");
        Reject(()=>ContentManifest.Parse(manifest.Replace(first.sha256,new string('0',64)),CnProtocol.UnityVersion),"changed manifest identity rejected");
        Reject(()=>ContentManifest.Parse(Json(first,first),CnProtocol.UnityVersion),"duplicate bundle rejected");
        string[] invalid={"../outside","/absolute","C:/file","ui\\file","a/../../b","a\nfile"};
        foreach(string name in invalid){ContentBundle bad=Bundle(name,original,root);Reject(()=>ContentManifest.Parse(Json(bad),CnProtocol.UnityVersion),"unsafe bundle name rejected");}
        ContentBundle dependency=Bundle("recovery/shared",updated,root);first.dependencies=new[]{dependency.name};
        ContentManifest ordered=ContentManifest.Parse(Json(first,dependency),CnProtocol.UnityVersion);
        Check(ordered.DependencyOrder()[0].name==dependency.name,"dependencies load before dependents");
        Reject(()=>ContentManifest.Parse(Json(first),CnProtocol.UnityVersion),"missing dependency rejected");
        dependency.dependencies=new[]{first.name};Reject(()=>ContentManifest.Parse(Json(first,dependency),CnProtocol.UnityVersion),"cycle rejected");
        first.dependencies=new string[0];dependency.dependencies=new string[0];
        ContentCache cache=new ContentCache(Path.Combine(root,"persistent"));
        Check(!cache.Contains(first),"cold cache absent");
        string committed=cache.Commit(first,Stage(cache,original));
        Check(File.ReadAllBytes(committed).SequenceEqual(original),"verified content committed");
        DateTime timestamp=File.GetLastWriteTimeUtc(committed);
        string duplicate=Stage(cache,original);cache.Commit(first,duplicate);
        Check(!File.Exists(duplicate)&&File.GetLastWriteTimeUtc(committed)==timestamp,"warm cache never rewrites valid bytes");
        ContentCache restarted=new ContentCache(Path.Combine(root,"persistent"));
        Check(restarted.Contains(first),"new cache instance reuses content after restart");
        string partial=Stage(cache,original.Take(5).ToArray());
        Reject(()=>cache.Commit(first,partial),"interrupted download rejected");
        Check(cache.Contains(first),"interruption leaves verified resource intact");
        string wrong=Stage(cache,updated);Reject(()=>cache.Commit(first,wrong),"same-length corruption rejected");
        Check(cache.Contains(first),"corruption cannot overwrite verified version");
        string nextPath=cache.Commit(next,Stage(cache,updated));
        Check(cache.Contains(first)&&cache.Contains(next)&&nextPath!=committed,"content update retains previous version");
        File.WriteAllBytes(nextPath,original);Check(!cache.Contains(next),"corrupt on-disk cache detected on restart");
        cache.Commit(next,Stage(cache,updated));Check(cache.Contains(next),"corrupt cache repaired with verified bytes");
        string outside=Path.Combine(root,"outside.partial");File.WriteAllBytes(outside,original);
        Reject(()=>cache.Commit(first,outside),"foreign staging path rejected");
        Console.WriteLine("Passed "+checks+" content integrity, dependency and persistent-cache checks; no Unity/iOS runtime.");
        return 0;
    }
}
