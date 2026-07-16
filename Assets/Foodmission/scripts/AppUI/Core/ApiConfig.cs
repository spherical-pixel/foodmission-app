using System;
using System.Collections.Generic;
using UnityEngine;

namespace eu.foodmission.platform
{
    public static class ApiConfig
    {
        private const string ConfigResourcePath = "ApiEnvironmentConfig";
        private const string PrefsEnvIndexKey = "fm_env_index";
        private const string PrefsEnvVersionKey = "fm_env_version";

        private static ApiEnvironmentConfig _cached;
        private static int? s_runtimeActiveIndex;
        private static string s_localUrlOverride;
        private static bool s_loaded;

#if UNITY_EDITOR
        private static bool _warmingCache;
#endif

        private static void EnsureLoaded()
        {
            if (s_loaded) return;
            if (_cached == null)
                _cached = Resources.Load<ApiEnvironmentConfig>(ConfigResourcePath);
            LoadRuntimeOverride();
            s_loaded = true;
        }

        private static void LoadRuntimeOverride()
        {
            s_runtimeActiveIndex = null;
            s_localUrlOverride = null;

            var savedVersion = PlayerPrefs.GetString(PrefsEnvVersionKey, "");
            if (savedVersion != Application.version)
            {
                PlayerPrefs.DeleteKey(PrefsEnvIndexKey);
                PlayerPrefs.DeleteKey(PrefsEnvVersionKey);
                PlayerPrefs.Save();
                return;
            }

            if (PlayerPrefs.HasKey(PrefsEnvIndexKey))
            {
                int index = PlayerPrefs.GetInt(PrefsEnvIndexKey, -1);
                if (_cached != null && index >= 0 && index < _cached.Environments.Count)
                    s_runtimeActiveIndex = index;
            }
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
                s_loaded = false;
                _warmingCache = false;
            };
        }
#endif

        private static int ResolveIndex()
        {
            if (s_runtimeActiveIndex.HasValue)
                return s_runtimeActiveIndex.Value;
            if (_cached != null)
                return _cached.ActiveIndex;
            return 0;
        }

        public static int ActiveEnvironmentIndex
        {
            get
            {
                EnsureLoaded();
                return ResolveIndex();
            }
        }

        public static IReadOnlyList<EnvironmentDefinition> Environments
        {
            get
            {
                EnsureLoaded();
                if (_cached != null)
                    return _cached.Environments;
                return Array.Empty<EnvironmentDefinition>();
            }
        }

        public static void SetActiveEnvironment(int index)
        {
            EnsureLoaded();
            if (_cached == null || index < 0 || index >= _cached.Environments.Count) return;

            s_runtimeActiveIndex = index;

            // Local environment is ephemeral — don't persist across restarts
            if (_cached.Environments[index].Name == "Local")
            {
                PlayerPrefs.DeleteKey(PrefsEnvIndexKey);
                PlayerPrefs.DeleteKey(PrefsEnvVersionKey);
                PlayerPrefs.Save();
                return;
            }

            PlayerPrefs.SetInt(PrefsEnvIndexKey, index);
            PlayerPrefs.SetString(PrefsEnvVersionKey, Application.version);
            PlayerPrefs.Save();
        }

        public static string LocalUrl
        {
            get
            {
                EnsureLoaded();
                return s_localUrlOverride ?? "";
            }
            set
            {
                EnsureLoaded();
                s_localUrlOverride = string.IsNullOrEmpty(value) ? null : value;
            }
        }

        public static string AppVersion => Application.version;

        private static EnvironmentDefinition GetActiveEnvironment()
        {
            if (_cached == null) return null;
            int idx = ResolveIndex();
            if (idx < 0 || idx >= _cached.Environments.Count) return null;
            return _cached.Environments[idx];
        }

        public static string BaseUrl
        {
            get
            {
                EnsureLoaded();
                var env = GetActiveEnvironment();
                if (env == null) return "https://staging.api.foodmission.eu";
                if (env.Name == "Local" && !string.IsNullOrEmpty(s_localUrlOverride))
                    return s_localUrlOverride;
                return env.ApiBaseUrl;
            }
        }

        public static string AuthBaseUrl
        {
            get
            {
                EnsureLoaded();
                var env = GetActiveEnvironment();
                if (env == null) return "https://staging.auth.foodmission.eu";
                return env.AuthBaseUrl;
            }
        }

        public static bool UseDirectOpenFoodFactsClient
        {
            get
            {
                EnsureLoaded();
                var env = GetActiveEnvironment();
                return env?.UseDirectOpenFoodFactsClient ?? false;
            }
        }

        public static string OpenFoodFactsBaseUrl
        {
            get
            {
                EnsureLoaded();
                var env = GetActiveEnvironment();
                return env != null && !string.IsNullOrEmpty(env.OpenFoodFactsBaseUrl)
                    ? env.OpenFoodFactsBaseUrl
                    : "https://world.openfoodfacts.org";
            }
        }

        public static int OpenFoodFactsSearchMinIntervalMs
        {
            get
            {
                EnsureLoaded();
                var env = GetActiveEnvironment();
                return env?.OpenFoodFactsSearchMinIntervalMs ?? 6000;
            }
        }

        public static int OpenFoodFactsSearchCacheTtlMinutes
        {
            get
            {
                EnsureLoaded();
                var env = GetActiveEnvironment();
                return env?.OpenFoodFactsSearchCacheTtlMinutes ?? 1440;
            }
        }

        public static int OpenFoodFactsSearchPageSize
        {
            get
            {
                EnsureLoaded();
                var env = GetActiveEnvironment();
                return env?.OpenFoodFactsSearchPageSize ?? 20;
            }
        }
    }
}
