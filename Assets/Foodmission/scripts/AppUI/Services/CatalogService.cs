using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Implementation of ICatalogService that fetches reference data
    /// from GET /api/v1/catalog/startup with in-memory caching by language.
    /// </summary>
    public class CatalogService : ICatalogService
    {
        private CatalogData _cachedData;
        private string _cachedLang;

        /// <summary>
        /// Loads catalog data from the backend. Returns cached data
        /// if available for the requested language, otherwise fetches from API.
        /// </summary>
        /// <param name="lang">Language code for localized labels (e.g. "es", "en").</param>
        /// <returns>CatalogData with all reference lists, or null on error.</returns>
        public async Task<CatalogData> LoadStartupAsync(string lang)
        {
            // Return cache if language matches
            if (_cachedData != null && _cachedLang == lang)
            {
                return _cachedData;
            }

            try
            {
                string url = $"{ApiConfig.BaseUrl}/api/v1/catalog/startup?lang={Uri.EscapeDataString(lang)}";

                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.SetRequestHeader("Accept", "application/json");

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[{GetType().Name}] LoadStartup failed: {request.responseCode} {request.error}");
                    return null;
                }

                string responseJson = request.downloadHandler.text;
                Debug.Log($"[{GetType().Name}] Catalog response: {responseJson}");

                StartupResponse response = JsonUtility.FromJson<StartupResponse>(responseJson);

                if (response?.data == null)
                {
                    Debug.LogError($"[{GetType().Name}] Invalid catalog response");
                    return null;
                }

                _cachedData = response.data;
                _cachedLang = lang;

                Debug.Log($"[{GetType().Name}] Catalog loaded successfully (lang={lang})");
                return _cachedData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadStartupAsync exception: {ex.Message}");
                return null;
            }
        }
    }
}