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
}
