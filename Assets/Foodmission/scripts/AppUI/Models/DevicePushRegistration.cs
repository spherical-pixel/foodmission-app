using System;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Encapsulates push notification device registration info.
    /// Persisted locally in PlayerPrefs/AppState and ready for migration
    /// to backend /api/v1/users/me/devices when implemented.
    /// </summary>
    [Serializable]
    public class DevicePushRegistration
    {
        public string token = "";
        public string platform = ""; // "android", "ios", "editor"
        public string deviceModel = "";
        public string appVersion = "";
        public string registeredAt = "";
        public bool notificationsEnabled = false;

        public DevicePushRegistration() { }

        public DevicePushRegistration Copy()
        {
            return new DevicePushRegistration
            {
                token = this.token,
                platform = this.platform,
                deviceModel = this.deviceModel,
                appVersion = this.appVersion,
                registeredAt = this.registeredAt,
                notificationsEnabled = this.notificationsEnabled
            };
        }
    }
}
