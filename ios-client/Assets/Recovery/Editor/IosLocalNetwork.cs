#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Kairisei.Recovery.Editor
{
    public static class IosLocalNetwork
    {
        [PostProcessBuild(100)]
        public static void Configure(BuildTarget target, string output)
        {
            if (target != BuildTarget.iOS) return;
            string path = Path.Combine(output, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(path);
            plist.root.SetString("NSLocalNetworkUsageDescription", "连接你指定的局域网游戏服务器。");
            PlistElement transportElement;
            PlistElementDict transport;
            if (plist.root.values.TryGetValue("NSAppTransportSecurity", out transportElement) && transportElement is PlistElementDict)
                transport = (PlistElementDict)transportElement;
            else
                transport = plist.root.CreateDict("NSAppTransportSecurity");
            // The existing game server advertises HTTP URLs. This private test build must be able to reach them.
            transport.SetBoolean("NSAllowsArbitraryLoads", true);
            transport.SetBoolean("NSAllowsLocalNetworking", true);
            plist.WriteToFile(path);
        }
    }
}
#endif
