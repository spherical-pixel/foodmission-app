using System.Text.RegularExpressions;

namespace eu.foodmission.platform
{
    /// <summary>Addressable sprite addresses for badges: badges/badge_{CODE} and badges/badge_{CODE}_disabled.</summary>
    public static class BadgeSprites
    {
        private static readonly Regex s_CodePattern = new Regex("^[A-Z][A-Z0-9_]*$");

        public static string Address(string code, bool earned)
        {
            return earned ? $"badges/badge_{code}" : $"badges/badge_{code}_disabled";
        }

        /// <summary>
        /// True for backend badge codes (FIRST_STEP, CHEF…). Anything else (a UUID, free text) has no sprite,
        /// so callers skip the Addressables load instead of logging an InvalidKeyException.
        /// </summary>
        public static bool LooksLikeCode(string code)
        {
            return !string.IsNullOrEmpty(code) && s_CodePattern.IsMatch(code);
        }
    }
}
