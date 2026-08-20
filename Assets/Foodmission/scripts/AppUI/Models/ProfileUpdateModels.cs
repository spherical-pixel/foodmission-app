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
        public int? soundVolume;
        public int? musicVolume;
        public bool? pushNotificationsEnabled;
        public bool? backgroundPattern;
        public string notificationPreferredTime;
        public DevicePushRegistration devicePushRegistration;
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

        [JsonProperty("segment", NullValueHandling = NullValueHandling.Ignore)]
        public string segment;

        [JsonProperty("currentQuestId", NullValueHandling = NullValueHandling.Ignore)]
        public string currentQuestId;

        [JsonProperty("healthGoals", NullValueHandling = NullValueHandling.Ignore)]
        public object healthGoals;

        [JsonProperty("nutritionTargets", NullValueHandling = NullValueHandling.Ignore)]
        public object nutritionTargets;

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

        [JsonProperty("allergies", NullValueHandling = NullValueHandling.Ignore)]
        public string[] allergies;

        [JsonProperty("preferredCategories", NullValueHandling = NullValueHandling.Ignore)]
        public string[] preferredCategories;

        [JsonProperty("foodExclusions", NullValueHandling = NullValueHandling.Ignore)]
        public string[] foodExclusions;

        [JsonProperty("motivation", NullValueHandling = NullValueHandling.Ignore)]
        public string motivation;

        [JsonProperty("dailyTimeCommitmentMinutes", NullValueHandling = NullValueHandling.Ignore)]
        public int? dailyTimeCommitmentMinutes;

        [JsonProperty("showNutriScore", NullValueHandling = NullValueHandling.Ignore)]
        public bool? showNutriScore;

        [JsonProperty("avoidUpf", NullValueHandling = NullValueHandling.Ignore)]
        public bool? avoidUpf;

        [JsonProperty("shoppingResponsibility", NullValueHandling = NullValueHandling.Ignore)]
        public string shoppingResponsibility;

        [JsonProperty("onboardingSurvey", NullValueHandling = NullValueHandling.Ignore)]
        public OnboardingSurveyData onboardingSurvey;

        [JsonProperty("lastShoppingListId", NullValueHandling = NullValueHandling.Ignore)]
        public string lastShoppingListId;

        [JsonProperty("autoAddToPantry", NullValueHandling = NullValueHandling.Ignore)]
        public bool autoAddToPantry;

        [JsonProperty("avatarConfig", NullValueHandling = NullValueHandling.Ignore)]
        public AvatarConfig avatarConfig;

        [JsonProperty("hasAvatar", NullValueHandling = NullValueHandling.Ignore)]
        public bool? hasAvatar;

        [JsonProperty("onboardingProfileCompleted", NullValueHandling = NullValueHandling.Ignore)]
        public bool? onboardingProfileCompleted;

        [JsonProperty("onboardingProfileSkippedAt", NullValueHandling = NullValueHandling.Ignore)]
        public string onboardingProfileSkippedAt;
    }

    /// <summary>
    /// Survey answers payload stored in preferences using language-agnostic enum codes.
    /// Key names match NestJS ONBOARDING_BASELINE_FIELDS contract.
    /// </summary>
    [Serializable]
    public class OnboardingSurveyData
    {
        [JsonProperty("weeklyMeatConsumption", NullValueHandling = NullValueHandling.Ignore)]
        public string weeklyMeatConsumption;

        [JsonProperty("weeklyBeefConsumption", NullValueHandling = NullValueHandling.Ignore)]
        public string weeklyBeefConsumption;

        [JsonProperty("weeklyFoodWaste", NullValueHandling = NullValueHandling.Ignore)]
        public string weeklyFoodWaste;

        [JsonProperty("weeklyUpfConsumption", NullValueHandling = NullValueHandling.Ignore)]
        public string weeklyUpfConsumption;

        [JsonProperty("weeklyReusableOrRefill", NullValueHandling = NullValueHandling.Ignore)]
        public string weeklyReusableOrRefill;

        [JsonIgnore]
        public string meatMeals
        {
            get => weeklyMeatConsumption;
            set => weeklyMeatConsumption = value;
        }

        [JsonIgnore]
        public string beefFrequency
        {
            get => weeklyBeefConsumption;
            set => weeklyBeefConsumption = value;
        }

        [JsonIgnore]
        public string foodWasteFrequency
        {
            get => weeklyFoodWaste;
            set => weeklyFoodWaste = value;
        }

        [JsonIgnore]
        public string ultraProcessedFrequency
        {
            get => weeklyUpfConsumption;
            set => weeklyUpfConsumption = value;
        }

        [JsonIgnore]
        public string reusableContainersFrequency
        {
            get => weeklyReusableOrRefill;
            set => weeklyReusableOrRefill = value;
        }

        public bool HasAnswers()
        {
            return !string.IsNullOrEmpty(weeklyMeatConsumption)
                || !string.IsNullOrEmpty(weeklyBeefConsumption)
                || !string.IsNullOrEmpty(weeklyFoodWaste)
                || !string.IsNullOrEmpty(weeklyUpfConsumption)
                || !string.IsNullOrEmpty(weeklyReusableOrRefill);
        }
    }
}
