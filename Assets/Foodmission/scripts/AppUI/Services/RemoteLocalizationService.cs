using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class RemoteLocalizationService : IRemoteLocalizationService
    {
        private const string JsonUrl =
            "https://raw.githubusercontent.com/spherical-pixel/foodmission-app/refs/heads/main/version-check/localization-overrides.json";

        private const string CacheFileName = "localization-cache.json";

        private LocalizationOverrides _overrides;

        public async Task InitializeAsync()
        {
            LocalizationOverrides downloadedData = null;

            try
            {
                using UnityWebRequest request = UnityWebRequest.Get(JsonUrl);
                UnityWebRequestAsyncOperation op = request.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    downloadedData = DeserializeOverrides(json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RemoteLocalizationService] Download failed: {ex.Message}");
            }

            // Validate downloaded data against minAppVersion
            if (downloadedData != null && IsObsoleteForCurrentBuild(downloadedData))
            {
                Debug.Log($"[RemoteLocalizationService] Downloaded overrides obsolete for build v{Application.version}, ignoring");
                downloadedData = null;
            }

            if (downloadedData != null)
            {
                // Check if downloaded version is newer than cached
                var cached = LoadCache();
                if (cached == null || downloadedData.version > cached.version)
                {
                    SaveCache(downloadedData);
                    _overrides = downloadedData;
                    RegisterPatcher();
                    Debug.Log($"[RemoteLocalizationService] Applied remote overrides v{downloadedData.version}");
                    return;
                }

                // Downloaded version same or older — keep cache
                Debug.Log($"[RemoteLocalizationService] Downloaded overrides v{downloadedData.version} not newer than cache, keeping cache");
                downloadedData = null;
            }

            // Fall back to cache
            LocalizationOverrides pendingCachedData = LoadCache();
            if (pendingCachedData != null && !IsObsoleteForCurrentBuild(pendingCachedData))
            {
                _overrides = pendingCachedData;
                RegisterPatcher();
                Debug.Log($"[RemoteLocalizationService] Applied cached overrides v{pendingCachedData.version}");
                return;
            }

            // No valid overrides — use built-in tables.
            // Note: this service must initialize before any string table is accessed,
            // otherwise tables may load without the patcher and show built-in strings.
            Debug.Log($"[RemoteLocalizationService] No valid overrides found, using built-in tables");
        }

        // Overrides are obsolete if app version exceeds the minAppVersion the JSON was exported for.
        // This prevents stale overrides (exported for v1.0) from overwriting strings in a newer build (v1.5).
        private bool IsObsoleteForCurrentBuild(LocalizationOverrides data)
        {
            if (string.IsNullOrEmpty(data.minAppVersion)) return false;

            Version appVersion = ParseVersion(Application.version);
            Version minVersion = ParseVersion(data.minAppVersion);
            if (appVersion == null || minVersion == null) return false;

            return appVersion > minVersion;
        }

        private static Version ParseVersion(string version)
        {
            if (string.IsNullOrEmpty(version)) return null;
            try { return new Version(version); } catch { return null; }
        }

        // Sets the global ITablePostprocessor. Only one postprocessor can be active at a time —
        // the project owns the localization pipeline exclusively, so this is safe.
        private void RegisterPatcher()
        {
            LocalizationSettings.StringDatabase.TablePostprocessor = new RemoteOverlayPatcher(_overrides);
        }

        private static LocalizationOverrides DeserializeOverrides(string json)
        {
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<LocalizationOverrides>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RemoteLocalizationService] Failed to parse overrides: {ex.Message}");
                return null;
            }
        }

        private void SaveCache(LocalizationOverrides data)
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, CacheFileName);
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RemoteLocalizationService] Failed to save cache: {ex.Message}");
            }
        }

        private LocalizationOverrides LoadCache()
        {
            try
            {
                string path = Path.Combine(Application.persistentDataPath, CacheFileName);
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                return DeserializeOverrides(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RemoteLocalizationService] Failed to load cache: {ex.Message}");
                return null;
            }
        }

        private class RemoteOverlayPatcher : ITablePostprocessor
        {
            private readonly LocalizationOverrides _overrides;

            public RemoteOverlayPatcher(LocalizationOverrides overrides)
            {
                _overrides = overrides;
            }

            public void PostprocessTable(LocalizationTable table)
            {
                if (table is not StringTable stringTable) return;
                if (_overrides?.strings == null) return;

                if (!_overrides.strings.TryGetValue(stringTable.TableCollectionName, out var tableOverrides))
                    return;

                var localeCode = stringTable.LocaleIdentifier.Code;
                if (!tableOverrides.TryGetValue(localeCode, out var localeStrings))
                    return;

                foreach (var kvp in localeStrings)
                {
                    stringTable.AddEntry(kvp.Key, kvp.Value);
                }
            }
        }
    }
}
