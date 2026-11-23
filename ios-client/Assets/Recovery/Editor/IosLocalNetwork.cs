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
            // On iOS 10+, presence of any of these keys overrides the global
            // exception to NO, even if the key itself is set to false.
            transport.values.Remove("NSAllowsLocalNetworking");
            transport.values.Remove("NSAllowsArbitraryLoadsForMedia");
            transport.values.Remove("NSAllowsArbitraryLoadsInWebContent");
            plist.WriteToFile(path);
            string projectPath = PBXProject.GetPBXProjectPath(output);
            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);
            string framework = project.GetUnityFrameworkTargetGuid();
            project.AddFrameworkToProject(framework, "Security.framework", false);
            project.AddFrameworkToProject(framework, "Foundation.framework", false);
            project.AddFrameworkToProject(framework, "CoreFoundation.framework", false);
            project.AddBuildProperty(framework, "OTHER_LDFLAGS", "-lresolv");
            project.WriteToFile(projectPath);
        }
    }
}
#endif
