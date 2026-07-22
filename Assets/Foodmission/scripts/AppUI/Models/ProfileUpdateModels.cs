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

        [JsonProperty("yearOfBirth", NullValueHandling = NullValueHandling.Ignore)]
        public int? yearOfBirth;

        [JsonProperty("country", NullValueHandling = NullValueHandling.Ignore)]
        public string country;

        [JsonProperty("region", NullValueHandling = NullValueHandling.Ignore)]
        public string region;

        [JsonProperty("zip", NullValueHandling = NullValueHandling.Ignore)]
        public string zip;

        public string ToJson() => JsonConvert.SerializeObject(this, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
    }

    /// <summary>
    /// Nested preferences object for dietary and shopping data.
    /// dietaryPreference is an array to support multi-select.
    /// </summary>
    public class ProfileUpdatePreferences
    {
        [JsonProperty("dietaryPreference", NullValueHandling = NullValueHandling.Ignore)]
        public string[] dietaryPreference;

        [JsonProperty("shoppingResponsibility", NullValueHandling = NullValueHandling.Ignore)]
        public string shoppingResponsibility;

        [JsonProperty("onboardingSurvey", NullValueHandling = NullValueHandling.Ignore)]
        public OnboardingSurveyData onboardingSurvey;

        [JsonProperty("lastShoppingListId", NullValueHandling = NullValueHandling.Ignore)]
        public string lastShoppingListId;
    }

    /// <summary>
    /// Survey answers payload stored in preferences using language-agnostic enum codes.
    /// </summary>
    [Serializable]
    public class OnboardingSurveyData
    {
        [JsonProperty("meatMeals", NullValueHandling = NullValueHandling.Ignore)]
        public string meatMeals;

        [JsonProperty("beefFrequency", NullValueHandling = NullValueHandling.Ignore)]
        public string beefFrequency;

        [JsonProperty("foodWasteFrequency", NullValueHandling = NullValueHandling.Ignore)]
        public string foodWasteFrequency;

        [JsonProperty("ultraProcessedFrequency", NullValueHandling = NullValueHandling.Ignore)]
        public string ultraProcessedFrequency;

        [JsonProperty("reusableContainersFrequency", NullValueHandling = NullValueHandling.Ignore)]
        public string reusableContainersFrequency;
    }
}
