#if UNITY_EDITOR && UNITY_IOS && ENABLE_GPGS_SDK
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Areung_Plugin.SDK.Social.EditorTools
{
    public static class GameCenterPostProcess
    {
        private const string EntitlementFileName = "Areung.entitlements";

        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS) return;

            var config = Resources.Load<SocialConfig>(SocialConfig.ResourcePath);
            if (config == null || !config.addGameCenterCapabilityOnBuild) return;

            var projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);

            var mainTargetGuid = proj.GetUnityMainTargetGuid();
            var frameworkTargetGuid = proj.GetUnityFrameworkTargetGuid();

            proj.AddFrameworkToProject(mainTargetGuid, "GameKit.framework", false);
            proj.AddFrameworkToProject(frameworkTargetGuid, "GameKit.framework", false);
            proj.WriteToFile(projPath);

            var capability = new ProjectCapabilityManager(projPath, EntitlementFileName, null, mainTargetGuid);
            capability.AddGameCenter();
            capability.WriteToFile();

            Debug.Log("[Social] iOS 빌드 후처리 완료 — GameKit.framework + Game Center capability 추가");
        }
    }
}
#endif
