using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class AppUpdateService : IAppUpdateService
    {
        private const string VersionJsonUrl =
            "https://raw.githubusercontent.com/spherical-pixel/foodmission-app/refs/heads/main/version-check/latest-version.json";

        public async Task<(AppVersionCheckResult Result, ApiErrorResponse Error)> CheckForUpdateAsync()
        {
            try
            {
                using UnityWebRequest request = UnityWebRequest.Get(VersionJsonUrl);
                UnityWebRequestAsyncOperation op = request.SendWebRequest();

                while (!op.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                    return (null, null);

                string json = request.downloadHandler.text;
                AppVersionCheckResponse response = JsonUtility.FromJson<AppVersionCheckResponse>(json);

                if (response == null)
                    return (null, null);

                PlatformVersionInfo platformInfo = null;
#if UNITY_IOS
                platformInfo = response.ios;
#elif UNITY_ANDROID
                platformInfo = response.android;
#endif
                if (platformInfo == null)
                    return (null, null);

                Version currentVersion = ParseVersion(Application.version);
                Version latestVersion = ParseVersion(platformInfo.latestVersion);

                if (currentVersion == null || latestVersion == null)
                    return (null, null);

                bool updateAvailable = latestVersion > currentVersion;

                return (new AppVersionCheckResult
                {
                    updateAvailable = updateAvailable,
                    isForced = updateAvailable && platformInfo.isForced,
                    latestVersion = platformInfo.latestVersion,
                    storeUrl = platformInfo.storeUrl,
                    releaseNotes = platformInfo.releaseNotes
                }, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppUpdateService] Check failed: {ex.Message}");
                return (null, null);
            }
        }

        private static Version ParseVersion(string version)
        {
            if (string.IsNullOrEmpty(version)) return null;
            try { return new Version(version); } catch { return null; }
        }
    }
}
