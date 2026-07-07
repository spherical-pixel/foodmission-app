using System;
using System.Collections.Generic;
using System.Linq;
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

        // Country/region caches (separate from startup data)
        private List<CatalogItem> _cachedCountries;
        private readonly Dictionary<string, List<CatalogItem>> _cachedRegions = new();

        /// <summary>
        /// EU member states + Norway (EEA). Used to filter the backend's full
        /// ISO 3166-1 list (~249 countries) down to the 28 relevant for Foodmission.
        /// </summary>
        private static readonly HashSet<string> EUCountryCodes = new()
        {
            "AT","BE","BG","HR","CY","CZ","DK","EE","FI","FR","DE","GR","HU",
            "IE","IT","LV","LT","LU","MT","NL","NO","PL","PT","RO","SK","SI",
            "ES","SE"
        };

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

        // ── Countries & Regions (paginated endpoints) ──────────────────────

        public async Task<(List<CatalogItem> Result, ApiErrorResponse Error)> GetCountriesAsync()
        {
            // Return cache if available
            if (_cachedCountries != null)
            {
                return (_cachedCountries, null);
            }

            try
            {
                string url = $"{ApiConfig.BaseUrl}/api/v1/catalog/countries?page=1&limit=300";
                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.SetRequestHeader("Accept", "application/json");

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[{GetType().Name}] GetCountriesAsync failed, falling back to local JSON: {request.error}");
                    var fallback = LoadCountriesFromJson();
                    if (fallback != null)
                        return (fallback, null);
                    return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetCountries"));
                }

                string raw = request.downloadHandler.text;
                PaginatedCatalogResponse response = JsonUtility.FromJson<PaginatedCatalogResponse>(raw);

                if (response?.data == null || response.data.Length == 0)
                {
                    Debug.LogWarning($"[{GetType().Name}] GetCountriesAsync — empty response, falling back to local JSON");
                    var fallback = LoadCountriesFromJson();
                    if (fallback != null)
                        return (fallback, null);
                    return (null, null);
                }

                _cachedCountries = response.data
                    .Where(c => EUCountryCodes.Contains(c.code))
                    .ToList();
                Debug.Log($"[{GetType().Name}] Countries loaded: {_cachedCountries.Count} (filtered to EU + Norway)");
                return (_cachedCountries, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] GetCountriesAsync exception: {ex.Message}");
                var fallback = LoadCountriesFromJson();
                if (fallback != null)
                    return (fallback, null);
                return (null, null);
            }
        }

        public async Task<(List<CatalogItem> Result, ApiErrorResponse Error)> GetRegionsAsync(string countryCode)
        {
            string cc = (countryCode ?? "").Trim().ToUpperInvariant();
            if (cc.Length != 2)
            {
                return (new List<CatalogItem>(), null);
            }

            // Return cache if available for this country
            if (_cachedRegions.TryGetValue(cc, out var cached))
            {
                return (cached, null);
            }

            try
            {
                string url = $"{ApiConfig.BaseUrl}/api/v1/catalog/regions?countryCode={cc}&page=1&limit=200";
                using UnityWebRequest request = UnityWebRequest.Get(url);
                request.SetRequestHeader("Accept", "application/json");

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[{GetType().Name}] GetRegionsAsync({cc}) failed, falling back to local JSON: {request.error}");
                    var fallback = LoadRegionsFromJson(cc);
                    if (fallback != null)
                        return (fallback, null);
                    return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetRegions({cc})"));
                }

                string raw = request.downloadHandler.text;
                PaginatedCatalogResponse response = JsonUtility.FromJson<PaginatedCatalogResponse>(raw);

                if (response?.data == null)
                {
                    Debug.LogWarning($"[{GetType().Name}] GetRegionsAsync({cc}) — null response, falling back to local JSON");
                    var fallback = LoadRegionsFromJson(cc);
                    if (fallback != null)
                        return (fallback, null);
                    return (new List<CatalogItem>(), null);
                }

                var result = response.data.ToList();
                _cachedRegions[cc] = result;
                Debug.Log($"[{GetType().Name}] Regions loaded for {cc}: {result.Count}");
                return (result, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] GetRegionsAsync({cc}) exception: {ex.Message}");
                var fallback = LoadRegionsFromJson(cc);
                if (fallback != null)
                    return (fallback, null);
                return (null, null);
            }
        }

        // ── JSON fallback (offline resilience) ──────────────────────────────

        private static List<CatalogItem> LoadCountriesFromJson()
        {
            var jsonAsset = Resources.Load<TextAsset>("ue_countries_regions");
            if (jsonAsset == null)
            {
                Debug.LogError("[CatalogService] ue_countries_regions.json fallback could not be loaded");
                return null;
            }

            var wrapper = JsonUtility.FromJson<CountriesList>("{\"countries\":" + jsonAsset.text + "}");
            if (wrapper?.countries == null) return null;

            return wrapper.countries
                .Select(c => new CatalogItem { code = c.country_iso, label = c.country_name_local })
                .ToList();
        }

        private static List<CatalogItem> LoadRegionsFromJson(string countryCode)
        {
            var jsonAsset = Resources.Load<TextAsset>("ue_countries_regions");
            if (jsonAsset == null) return null;

            var wrapper = JsonUtility.FromJson<CountriesList>("{\"countries\":" + jsonAsset.text + "}");
            if (wrapper?.countries == null) return null;

            var country = wrapper.countries.FirstOrDefault(c => c.country_iso == countryCode);
            if (country?.regions == null) return new List<CatalogItem>();

            return country.regions
                .Select(r => new CatalogItem { code = r.region_iso, label = r.region_name_local })
                .ToList();
        }
    }
}
