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
        public string firstName;
        public string lastName;
        public int yearOfBirth;
        public string country;
        public string region;
        public string zip;
        public string gender;
        public string annualIncome;
        public string educationLevel;
        public string activityLevel;
        public float weightKg;
        public float heightCm;
        public string language;
        public UserSettingsDto settings;
    }
}
