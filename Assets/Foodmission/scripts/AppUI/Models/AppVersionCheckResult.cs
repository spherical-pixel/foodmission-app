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
    }
}
