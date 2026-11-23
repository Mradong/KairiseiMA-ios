using System;
using System.IO;
using System.Linq;
using Kairisei.Recovery;

public static class RealResourceTransitionTests
{
    private static int checks;
    private static void Check(bool ok,string label) { if(!ok)throw new Exception(label);checks++; }
    private static string Stage(ContentCache cache,byte[] bytes) { string path=cache.CreateStagingPath();File.WriteAllBytes(path,bytes);return path; }

    public static void Main(string[] args)
    {
        ContentManifest embedded=ContentManifest.Parse(File.ReadAllText(args[0]),CnProtocol.UnityVersion);
        ContentManifest server=ContentManifest.Parse(File.ReadAllText(args[2]),CnProtocol.UnityVersion);
        Check(embedded.contentId!=server.contentId,"Expected distinct embedded/server revisions");
        Check(embedded.bundles.Length==1 && server.bundles.Length==1,"Unexpected resource inventory");
        ContentBundle first=embedded.bundles[0],second=server.bundles[0];
        Check(first.name=="recovery/ui.dat" && second.name==first.name && first.sha256!=second.sha256,"Expected two versions of the UI bundle");
        byte[] firstBytes=File.ReadAllBytes(args[1]),secondBytes=File.ReadAllBytes(args[3]);
        ContentCache cache=new ContentCache(args[4]);
        Check(!cache.Contains(first) && !cache.Contains(second),"Expected fresh isolated cache");
        string firstPath=cache.Commit(first,Stage(cache,firstBytes));
        Check(cache.Contains(first) && !cache.Contains(second),"Embedded revision must install first");
        string broken=Stage(cache,secondBytes.Take(secondBytes.Length-1).ToArray());
        bool rejected=false;try {cache.Commit(second,broken);}catch(InvalidDataException){rejected=true;}
        Check(rejected && cache.Contains(first) && !cache.Contains(second),"Interrupted server update must preserve embedded revision");
        File.Delete(broken);
        string secondPath=cache.Commit(second,Stage(cache,secondBytes));
        Check(firstPath!=secondPath && cache.Contains(first) && cache.Contains(second),"Both verified revisions must coexist");
        ContentCache reopened=new ContentCache(args[4]);
        Check(reopened.Contains(first) && reopened.Contains(second),"Both revisions must survive cache reopen");
        byte[] altered=(byte[])secondBytes.Clone();altered[altered.Length/2]^=1;
        string corrupt=Stage(reopened,altered);rejected=false;
        try {reopened.Commit(second,corrupt);}catch(InvalidDataException){rejected=true;}
        Check(rejected && reopened.Contains(first) && reopened.Contains(second),"Corrupt replacement must not alter either version");
        File.Delete(corrupt);
        Check(File.ReadAllBytes(firstPath).SequenceEqual(firstBytes) && File.ReadAllBytes(secondPath).SequenceEqual(secondBytes),"Cache bytes differ from verified source bundles");
        Console.WriteLine("Passed "+checks+" real iOS resource transition checks.");
    }
}
