using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class WhatsNewService : IWhatsNewService
    {
        private const string LastSeenVersionKey = "whats_new_last_seen_version";
        private const string VersionJsonUrl =
            "https://raw.githubusercontent.com/spherical-pixel/foodmission-app/refs/heads/main/version-check/latest-version.json";

        private readonly ILocalStorageService _localStorage;

        public WhatsNewService(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public async Task<(bool ShouldShow, string ReleaseNotes)> CheckShouldShowAsync()
        {
            try
            {
                string lastSeenVersion = _localStorage.GetValue<string>(LastSeenVersionKey, "");
                string currentVersion = Application.version;

                if (string.IsNullOrEmpty(currentVersion))
                    return (false, null);

                if (currentVersion == lastSeenVersion)
                    return (false, null);

                using UnityWebRequest request = UnityWebRequest.Get(VersionJsonUrl);
                UnityWebRequestAsyncOperation op = request.SendWebRequest();

                while (!op.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                    return (false, null);

                string json = request.downloadHandler.text;
                var response = JsonUtility.FromJson<AppVersionCheckResponse>(json);

                if (response == null)
                    return (false, null);

                PlatformVersionInfo platformInfo = null;
#if UNITY_IOS
                platformInfo = response.ios;
#elif UNITY_ANDROID
                platformInfo = response.android;
#else
                platformInfo = response.android ?? response.ios;
#endif
                if (platformInfo == null)
                    return (false, null);

                string localeCode = "en";
                if (LocalizationSettings.SelectedLocale != null)
                {
                    localeCode = LocalizationSettings.SelectedLocale.Identifier.Code;
                    if (localeCode.Contains("-"))
                        localeCode = localeCode.Split('-')[0];
                }

                string releaseNotes = platformInfo.GetLocalizedReleaseNotes(localeCode);

                return (true, releaseNotes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WhatsNewService] Check failed: {ex.Message}");
                return (false, null);
            }
        }

        public async Task MarkAsSeenAsync()
        {
            await Task.Yield();
            _localStorage.SetValue(LastSeenVersionKey, Application.version);
        }
    }
}
