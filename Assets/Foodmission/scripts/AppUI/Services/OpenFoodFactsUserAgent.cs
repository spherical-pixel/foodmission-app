using UnityEngine;

namespace eu.foodmission.platform
{
    public static class OpenFoodFactsUserAgent
    {
        private static string s_cachedUserAgent;

        public static string Build()
        {
            if (s_cachedUserAgent != null)
            {
                return s_cachedUserAgent;
            }

            string platform = Application.platform switch
            {
                RuntimePlatform.Android => "Android",
                RuntimePlatform.IPhonePlayer => "iOS",
                _ => "Editor"
            };

            s_cachedUserAgent = $"FOODMISSION - {platform} - Version {Application.version} - dev@foodmission.eu";
            return s_cachedUserAgent;
        }
    }
}
