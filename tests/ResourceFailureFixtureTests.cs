using System;
using System.IO;
using Kairisei.Recovery;
namespace Kairisei.Recovery { public static class CnProtocol { public const string UnityVersion="6000.3.26f1"; } }
public static class ResourceFailureFixtureTests {
    public static int Main(string[] args) {
        var manifest=ContentManifest.Parse(File.ReadAllText(args[0]), CnProtocol.UnityVersion);
        var item=manifest.bundles[0];
        var cache=new ContentCache(args[2]);
        string stage=cache.CreateStagingPath(); File.Copy(args[1],stage);
        string cached=cache.Commit(item,stage);
        if(!cache.Contains(item) || !File.Exists(cached)) throw new Exception("Fixture does not pass actual client integrity checks.");
        Console.WriteLine("PASS: actual C# manifest and cache accept the isolated invalid-bundle fixture");
        return 0;
    }
}
