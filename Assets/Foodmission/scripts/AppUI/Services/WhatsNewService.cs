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
        private const string ReleaseNotesBaseUrl =
            "https://raw.githubusercontent.com/spherical-pixel/foodmission-app/refs/heads/main/version-check/release-notes";

        private readonly ILocalStorageService _localStorage;
        private readonly Func<string, Task<string>> _downloader;

        public WhatsNewService(ILocalStorageService localStorage)
            : this(localStorage, null) { }

        public WhatsNewService(ILocalStorageService localStorage, Func<string, Task<string>> downloader)
        {
            _localStorage = localStorage;
            _downloader = downloader;
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

                string url = $"{ReleaseNotesBaseUrl}/{currentVersion}.json";
                string json = _downloader != null ? await _downloader(url) : await DownloadAsync(url);

                if (string.IsNullOrEmpty(json))
                    return (false, null);

                var notes = JsonUtility.FromJson<PlatformVersionInfo>(json);
                if (notes == null)
                    return (false, null);

                string releaseNotes = notes.GetLocalizedReleaseNotes(GetLocaleCode());
                if (string.IsNullOrEmpty(releaseNotes))
                    return (false, null);

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
    }
}