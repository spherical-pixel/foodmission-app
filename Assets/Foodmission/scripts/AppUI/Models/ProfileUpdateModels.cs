using System;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Request body for PATCH /api/v1/users/me — extended profile update.
    /// Only non-null fields are sent in the request body.
    /// </summary>
    [Serializable]
    public class ProfileUpdateRequest
    {
        public string gender = null;
        public string activityLevel = null;
        public string educationLevel = null;
        public string annualIncome = null;
        public ProfileUpdatePreferences preferences = null;
    }

    /// <summary>
    /// Nested preferences object for dietary and shopping data.
    /// Sent inside ProfileUpdateRequest.preferences.
    /// </summary>
    [Serializable]
    public class ProfileUpdatePreferences
    {
        public string dietaryPreference = null;
        public string shoppingResponsibility = null;
    }
}