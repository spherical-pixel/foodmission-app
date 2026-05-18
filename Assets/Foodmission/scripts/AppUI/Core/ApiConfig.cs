using System;
using UnityEngine;

namespace eu.foodmission.platform
{
    public static class ApiConfig
    {
        private const string ConfigResourcePath = "ApiEnvironmentConfig";
        private static ApiEnvironmentConfig _cached;
#if UNITY_EDITOR
        private static bool _warmingCache;
#endif

        private static ApiEnvironmentConfig LoadConfig()
        {
            if (_cached != null)
                return _cached;
            _cached = Resources.Load<ApiEnvironmentConfig>(ConfigResourcePath);
            return _cached;
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void WarmCacheOnDomainReload()
        {
            if (_warmingCache) return;
            _warmingCache = true;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                _cached = Resources.Load<ApiEnvironmentConfig>(ConfigResourcePath);
                _warmingCache = false;
            };
        }
#endif

        private static string Resolve(Func<EnvironmentDefinition, string> selector, string fallback)
        {
            var config = LoadConfig();
            if (config != null && config.ActiveEnvironment != null)
                return selector(config.ActiveEnvironment);
            return fallback;
        }

        public static string BaseUrl => Resolve(e => e.ApiBaseUrl, "https://staging.api.foodmission.eu");

        public static string AuthBaseUrl => Resolve(e => e.AuthBaseUrl, "https://staging.auth.foodmission.eu");
    }
}
