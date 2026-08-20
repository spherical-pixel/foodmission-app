#if UNITY_EDITOR && UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace eu.foodmission.platform.Editor
{
    /// <summary>
    /// Automatically configures the exported Xcode project with:
    /// 1. Push Notifications Capability (aps-environment)
    /// 2. Background Modes Capability (Remote notifications)
    /// 3. Info.plist UIBackgroundModes (remote-notification)
    /// </summary>
    public static class iOSPostProcessBuild
    {
        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            Debug.Log($"[iOSPostProcessBuild] Configuring Xcode project at: {pathToBuiltProject}");

            string projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);

            string mainTargetGuid = proj.GetUnityMainTargetGuid();
            const string mainTargetName = "Unity-iPhone";

            // 1. Configure Entitlements & Capabilities
            string entitlementsFileName = $"{mainTargetName}.entitlements";
            string entitlementsPath = Path.Combine(pathToBuiltProject, entitlementsFileName);

            var capabilityManager = new ProjectCapabilityManager(projPath, entitlementsPath, mainTargetName);
            capabilityManager.AddPushNotifications(true); // true = development
            capabilityManager.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);
            capabilityManager.WriteToFile();

            // 2. Configure Info.plist for Remote Notifications
            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            if (File.Exists(plistPath))
            {
                var plist = new PlistDocument();
                plist.ReadFromFile(plistPath);

                PlistElementDict rootDict = plist.root;

                // Ensure UIBackgroundModes array exists
                PlistElementArray backgroundModes = rootDict["UIBackgroundModes"] as PlistElementArray;
                if (backgroundModes == null)
                {
                    backgroundModes = rootDict.CreateArray("UIBackgroundModes");
                }

                // Add remote-notification if not already present
                bool hasRemoteNotification = false;
                foreach (var element in backgroundModes.values)
                {
                    if (element.AsString() == "remote-notification")
                    {
                        hasRemoteNotification = true;
                        break;
                    }
                }

                if (!hasRemoteNotification)
                {
                    backgroundModes.AddString("remote-notification");
                }

                plist.WriteToFile(plistPath);
            }

            Debug.Log("[iOSPostProcessBuild] Xcode project capabilities successfully configured.");
        }
    }
}
#endif
