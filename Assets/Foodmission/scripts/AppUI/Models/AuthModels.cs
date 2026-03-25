using System;

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
    /// API Register request
    /// </summary>
    [Serializable]
    public class RegisterRequest
    {
        public string username;
        public string email;
        public string password;
        public int yearOfBirth;
        public string country;
        public string region;
        public string zip;
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

    // ==================== Responses ====================

    /// <summary>
    /// API login/register response
    /// </summary>
    [Serializable]
    public class LoginResponse
    {
        public string access_token;
        public string token_type;
        public int expires_in;
        public UserData user;
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
        public string firstName;
        public string lastName;
        public string keycloakId;
    }
}
