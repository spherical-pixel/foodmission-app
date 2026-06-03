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
        private readonly IStoreService _storeService;
        private CatalogData _cachedData;
        private string _cachedLang;

        public CatalogService(IStoreService storeService)
        {
            _storeService = storeService;
        }

        public async Task<(CatalogData Result, ApiErrorResponse Error)> LoadStartupAsync(string lang)
        {
            // Return cache if language matches
            if (_cachedData != null && _cachedLang == lang)
            {
                return (_cachedData, null);
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
                    return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] LoadStartup"));
                }

                string responseJson = request.downloadHandler.text;
                Debug.Log($"[{GetType().Name}] Catalog response: {responseJson}");

                StartupResponse response = JsonUtility.FromJson<StartupResponse>(responseJson);

                if (response?.data == null)
                {
                    Debug.LogError($"[{GetType().Name}] Invalid catalog response");
                    return (null, null);
                }

                _cachedData = response.data;
                _cachedLang = lang;

                Debug.Log($"[{GetType().Name}] Catalog loaded successfully (lang={lang})");
                return (_cachedData, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadStartupAsync exception: {ex.Message}");
                return (null, null);
            }
        }

        private async Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetCatalogListAsync(string endpoint)
        {
            try
            {
                string url = $"{ApiConfig.BaseUrl}/api/v1/catalog/{endpoint}";
                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.SetRequestHeader("Authorization", _storeService.GetAppState().tokenType + " " + _storeService.GetAppState().accessToken);
                request.SetRequestHeader("Accept", "application/json");

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                    return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] Get{endpoint}"));

                string raw = request.downloadHandler.text;
                CatalogListResponse response = JsonUtility.FromJson<CatalogListResponse>(raw);
                return (response?.data, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] GetCatalogListAsync({endpoint}) exception: {ex.Message}");
                return (null, null);
            }
        }

        public Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetTypeOfMealsAsync()
            => GetCatalogListAsync("type-of-meals");

        public Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetMealCategoriesAsync()
            => GetCatalogListAsync("meal-categories");

        public Task<(CatalogItem[] Result, ApiErrorResponse Error)> GetMealCoursesAsync()
            => GetCatalogListAsync("meal-courses");
    }
}
