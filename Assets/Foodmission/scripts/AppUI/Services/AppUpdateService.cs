using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class AppUpdateService : IAppUpdateService
    {
        private const string VersionJsonUrl =
            "https://raw.githubusercontent.com/spherical-pixel/foodmission-app/refs/heads/main/version-check/latest-version.json";
        private const string ReleaseNotesBaseUrl =
            "https://raw.githubusercontent.com/spherical-pixel/foodmission-app/refs/heads/main/version-check/release-notes";

        public async Task<(AppVersionCheckResult Result, ApiErrorResponse Error)> CheckForUpdateAsync()
        {
            try
            {
                string json = await DownloadAsync(VersionJsonUrl);
                if (string.IsNullOrEmpty(json))
                    return (null, null);

                AppVersionCheckResponse response = JsonUtility.FromJson<AppVersionCheckResponse>(json);

                if (response == null)
                    return (null, null);

                PlatformVersionInfo platformInfo = null;
#if UNITY_IOS
                platformInfo = response.ios;
#elif UNITY_ANDROID
                platformInfo = response.android;
#else
                platformInfo = response.android ?? response.ios;
#endif
                if (platformInfo == null)
                    return (null, null);

                Version currentVersion = ParseVersion(Application.version);
                Version latestVersion = ParseVersion(platformInfo.latestVersion);

                if (currentVersion == null || latestVersion == null)
                    return (null, null);

                bool updateAvailable = latestVersion > currentVersion;

                string releaseNotes = null;
                if (updateAvailable && !string.IsNullOrEmpty(platformInfo.latestVersion))
                {
                    string notesJson = await DownloadAsync($"{ReleaseNotesBaseUrl}/{platformInfo.latestVersion}.json");
                    if (!string.IsNullOrEmpty(notesJson))
                    {
                        var notes = JsonUtility.FromJson<PlatformVersionInfo>(notesJson);
                        releaseNotes = notes?.GetLocalizedReleaseNotes(GetLocaleCode());
                    }
                }

                return (new AppVersionCheckResult
                {
                    updateAvailable = updateAvailable,
                    isForced = updateAvailable && platformInfo.isForced,
                    latestVersion = platformInfo.latestVersion,
                    storeUrl = platformInfo.storeUrl,
                    releaseNotes = releaseNotes
                }, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AppUpdateService] Check failed: {ex.Message}");
                return (null, null);
            }
        }

        private static string GetLocaleCode()
        {
            if (LocalizationSettings.SelectedLocale == null)
                return "en";

            string localeCode = LocalizationSettings.SelectedLocale.Identifier.Code;
            if (localeCode.Contains("-"))
                localeCode = localeCode.Split('-')[0];
            return localeCode;
        }

        private static async Task<string> DownloadAsync(string url)
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);
            UnityWebRequestAsyncOperation op = request.SendWebRequest();

            while (!op.isDone)
                await Task.Yield();

            return request.result == UnityWebRequest.Result.Success ? request.downloadHandler.text : null;
        }

        private static Version ParseVersion(string version)
        {
            if (string.IsNullOrEmpty(version)) return null;
            try { return new Version(version); } catch { return null; }
        }
    }
}