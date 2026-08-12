using System;
using Newtonsoft.Json;

namespace eu.foodmission.platform
{
    // ==================== Requests ====================

    /// <summary>
    /// API Login request
    /// </summary>
    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    /// <summary>
    /// API Register request — only non-null fields are serialized.
    /// </summary>
    public class RegisterRequest
    {
        [JsonProperty("username")]
        public string username;

        [JsonProperty("email")]
        public string email;

        [JsonProperty("password")]
        public string password;

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
    /// API Register response
    /// </summary>
    [Serializable]
    public class RegisterResponse
    {
        public KeycloakUserData createdUser;
        public LocalUserData localUser;
    }

    /// <summary>
    /// Keycloak user data in register response
    /// </summary>
    [Serializable]
    public class KeycloakUserData
    {
        public string id;
        public string username;
        public string email;
    }

    /// <summary>
    /// Local user data in register response
    /// </summary>
    [Serializable]
    public class LocalUserData
    {
        public string id;
        public string email;
        public string username;
        public string keycloakId;
    }

    public class RefreshRequest
    {
        [JsonProperty("token")]
        public string token;

        public string ToJson() => JsonConvert.SerializeObject(this);
    }

    public class ForgotPasswordRequest
    {
        [JsonProperty("email")]
        public string email;

        public string ToJson() => JsonConvert.SerializeObject(this);
    }

    public class RevokeTokenRequest
    {
        [JsonProperty("token")]
        public string token;

        [JsonProperty("tokenTypeHint")]
        public string tokenTypeHint;

        public string ToJson() => JsonConvert.SerializeObject(this);
    }

    // ==================== Responses ====================

    /// <summary>
    /// API login/register response
    /// </summary>
    [Serializable]
    public class LoginResponse
    {
        public string access_token;
        public string refresh_token;
        public string token_type;
        public int expires_in;
        public int refresh_expires_in;
        public UserData user;
    }

    /// <summary>
    /// Response from POST /api/v1/auth/refresh
    /// </summary>
    [Serializable]
    public class RefreshResponse
    {
        public string access_token;
        public string refresh_token;   // may be empty if backend does not rotate the refresh token
        public string token_type;
        public int expires_in;
        public int refresh_expires_in;
    }

    /// <summary>
    /// User data in response
    /// </summary>
    [Serializable]
    public class UserData
    {
        public string id;
        public string email;
        public string firstName;
        public string lastName;
    }

    /// <summary>
    /// Profile response from GET /api/v1/auth/profile
    /// </summary>
    [Serializable]
    public class ProfileResponse
    {
        public string id;
        public string email;
        public string username;
        public string keycloakId;
        public int yearOfBirth;
        public string country;
        public string region;
        public string zip;
        public string gender;
        public string annualIncome;
        public string educationLevel;
        public string activityLevel;
        public string language;
        public string segment;
        public string currentQuestId;
        public UserSettingsDto settings;
        public ProfilePreferences preferences;
    }

    /// <summary>
    /// Newtonsoft.Json converter that accepts either a JSON string or a JSON array
    /// of strings and deserializes both into a <c>string[]</c>.
    /// Used for <see cref="ProfilePreferences.dietaryPreference"/> to handle
    /// backward compatibility with old DB records that stored a single string.
    /// </summary>
    public class StringOrArrayConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(System.Type objectType)
        {
            return objectType == typeof(string[]);
        }

        public override object ReadJson(Newtonsoft.Json.JsonReader reader, System.Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.TokenType == Newtonsoft.Json.JsonToken.String)
            {
                string value = (string)reader.Value;
                return string.IsNullOrEmpty(value) ? new string[0] : new[] { value };
            }
            if (reader.TokenType == Newtonsoft.Json.JsonToken.StartArray)
            {
                return serializer.Deserialize<string[]>(reader) ?? new string[0];
            }
            return new string[0];
        }

        public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;
    }

    /// <summary>
    /// Nested preferences object returned by GET /api/v1/auth/profile.
    /// Mirrors the free-form JSON stored in the users.preferences column.
    /// </summary>
    [Serializable]
    public class ProfilePreferences
    {
        [Newtonsoft.Json.JsonConverter(typeof(StringOrArrayConverter))]
        public string[] dietaryPreference;
        public string[] allergies;
        public string[] preferredCategories;
        public string[] foodExclusions;
        public string motivation;
        public int? dailyTimeCommitmentMinutes;
        public bool? showNutriScore;
        public bool? avoidUpf;
        public string shoppingResponsibility;
        public OnboardingSurveyData onboardingSurvey;
        public string lastShoppingListId;
        public bool autoAddToPantry;
        public AvatarConfig avatarConfig;
        public bool hasAvatar;
    }
}
