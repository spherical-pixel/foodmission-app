using System;
using Newtonsoft.Json;

namespace eu.foodmission.platform
{
    /// <summary>
    /// App settings synced with the server as free-form JSON.
    /// Used both for writing (via Newtonsoft in ProfileUpdateRequest)
    /// and reading (via JsonUtility in ProfileResponse).
    /// </summary>
    [Serializable]
    public class UserSettingsDto
    {
        public string theme;
        public string scale;
        public string font;
        public int soundVolume;
        public int musicVolume;
        public bool pushNotificationsEnabled;
        public bool backgroundPattern;
    }

    /// <summary>
    /// Request body for PATCH /api/v1/users/me — extended profile update.
    /// Only non-null fields are serialized (NullValueHandling.Ignore).
    /// </summary>
    public class ProfileUpdateRequest
    {
        [JsonProperty("language", NullValueHandling = NullValueHandling.Ignore)]
        public string language;

        [JsonProperty("gender", NullValueHandling = NullValueHandling.Ignore)]
        public string gender;

        [JsonProperty("activityLevel", NullValueHandling = NullValueHandling.Ignore)]
        public string activityLevel;

        [JsonProperty("educationLevel", NullValueHandling = NullValueHandling.Ignore)]
        public string educationLevel;

        [JsonProperty("annualIncome", NullValueHandling = NullValueHandling.Ignore)]
        public string annualIncome;

        [JsonProperty("preferences", NullValueHandling = NullValueHandling.Ignore)]
        public ProfileUpdatePreferences preferences;

        [JsonProperty("settings", NullValueHandling = NullValueHandling.Ignore)]
        public UserSettingsDto settings;

        public string ToJson() => JsonConvert.SerializeObject(this, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
    }

    /// <summary>
    /// Nested preferences object for dietary and shopping data.
    /// Backend currently supports a single dietary preference value.
    /// </summary>
    public class ProfileUpdatePreferences
    {
        [JsonProperty("dietaryPreference", NullValueHandling = NullValueHandling.Ignore)]
        public string dietaryPreference;

        [JsonProperty("shoppingResponsibility", NullValueHandling = NullValueHandling.Ignore)]
        public string shoppingResponsibility;
    }
}
