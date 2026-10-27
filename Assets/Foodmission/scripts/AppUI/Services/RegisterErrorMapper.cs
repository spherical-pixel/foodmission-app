namespace eu.foodmission.platform
{
    /// <summary>
    /// Reason a registration was rejected that points at one register form field.
    /// </summary>
    public enum RegisterErrorReason
    {
        None,
        UsernameInvalid,
        UsernameTaken,
        EmailInvalid,
        EmailTaken,
        PasswordPolicy
    }

    /// <summary>
    /// Maps a failed POST /api/v1/auth/register response to a <see cref="RegisterErrorReason"/>.
    /// The backend forwards Keycloak's error code in "message" (e.g. "error-username-invalid-character",
    /// "invalidPasswordMinLengthMessage", "User exists with same email") and class-validator errors in
    /// "details.errors", so the whole body is searched.
    /// </summary>
    public static class RegisterErrorMapper
    {
        public static RegisterErrorReason Map(long statusCode, string body)
        {
            string text = body?.ToLowerInvariant() ?? string.Empty;

            if (statusCode == 400)
            {
                if (text.Contains("error-username-invalid-character"))
                {
                    return RegisterErrorReason.UsernameInvalid;
                }
                if (text.Contains("invalidpassword"))
                {
                    return RegisterErrorReason.PasswordPolicy;
                }
                if (text.Contains("error-invalid-email") || text.Contains("email must be an email"))
                {
                    return RegisterErrorReason.EmailInvalid;
                }
            }
            else if (statusCode == 409)
            {
                if (text.Contains("email"))
                {
                    return RegisterErrorReason.EmailTaken;
                }
                if (text.Contains("username"))
                {
                    return RegisterErrorReason.UsernameTaken;
                }
            }

            return RegisterErrorReason.None;
        }

        /// <summary>
        /// UI table key describing the reason, or null for <see cref="RegisterErrorReason.None"/>.
        /// </summary>
        public static string GetLocalizationKey(RegisterErrorReason reason)
        {
            return reason switch
            {
                RegisterErrorReason.UsernameInvalid => "ERROR_USERNAME_INVALID",
                RegisterErrorReason.UsernameTaken => "API_ERROR_REGISTER_USERNAME_TAKEN",
                RegisterErrorReason.EmailInvalid => "ERROR_EMAIL_INVALID",
                RegisterErrorReason.EmailTaken => "API_ERROR_REGISTER_EMAIL_TAKEN",
                RegisterErrorReason.PasswordPolicy => "API_ERROR_REGISTER_PASSWORD_POLICY",
                _ => null
            };
        }
    }
}
