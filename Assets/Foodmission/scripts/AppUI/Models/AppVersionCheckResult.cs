using System;

namespace eu.foodmission.platform
{
    [Serializable]
    public class AppVersionCheckResult
    {
        public bool updateAvailable;
        public bool isForced;
        public string latestVersion;
        public string storeUrl;
        public string releaseNotes;
    }

    [Serializable]
    public class AppVersionCheckResponse
    {
        public PlatformVersionInfo ios;
        public PlatformVersionInfo android;
    }

    [Serializable]
    public class PlatformVersionInfo
    {
        public string latestVersion;
        public bool isForced;
        public string storeUrl;
        public string releaseNotes;

        public string releaseNotes_nl;
        public string releaseNotes_de;
        public string releaseNotes_el;
        public string releaseNotes_it;
        public string releaseNotes_no;
        public string releaseNotes_pl;
        public string releaseNotes_sl;
        public string releaseNotes_es;

        public string GetLocalizedReleaseNotes(string localeCode)
        {
            return localeCode switch
            {
                "nl" => releaseNotes_nl ?? releaseNotes,
                "de" => releaseNotes_de ?? releaseNotes,
                "el" => releaseNotes_el ?? releaseNotes,
                "it" => releaseNotes_it ?? releaseNotes,
                "no" => releaseNotes_no ?? releaseNotes,
                "pl" => releaseNotes_pl ?? releaseNotes,
                "sl" => releaseNotes_sl ?? releaseNotes,
                "es" => releaseNotes_es ?? releaseNotes,
                _ => releaseNotes
            };
        }
    }
}
