using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class GenericFoodService : IGenericFoodService
    {
        private readonly IStoreService _storeService;
        private readonly Dictionary<string, GenericFood> _cache = new();
        private string _cachedLang;
        private PaginatedGenericFoodResponse _defaultSearchCache;
        private FoodGroupItem[] _foodGroupsCache;

        public GenericFoodService(IStoreService storeService)
        {
            _storeService = storeService;
        }

        private string AuthHeader
        {
            get
            {
                AppState s = _storeService.GetAppState();
                return $"{s.tokenType} {s.accessToken}";
            }
        }

        private string Lang
        {
            get
            {
                AppState s = _storeService.GetAppState();
                return s.lang ?? "en";
            }
        }

        private void InvalidateCacheIfLangChanged()
        {
            string currentLang = Lang;
            if (_cachedLang != currentLang)
            {
                _cachedLang = currentLang;
                _defaultSearchCache = null;
                _foodGroupsCache = null;
                _cache.Clear();
            }
        }

        public async Task<(PaginatedGenericFoodResponse Result, ApiErrorResponse Error)> SearchGenericFoodsAsync(
            string query = null, string foodGroup = null, int page = 1, int pageSize = 20)
        {
            InvalidateCacheIfLangChanged();

            bool isDefaultSearch = string.IsNullOrEmpty(query) && string.IsNullOrEmpty(foodGroup) && page == 1 && pageSize >= 100;
            if (isDefaultSearch && _defaultSearchCache != null)
            {
                return (_defaultSearchCache, null);
            }

            var sb = new StringBuilder($"{ApiConfig.BaseUrl}/api/v1/generic-foods?page={page}&limit={pageSize}");

            if (!string.IsNullOrEmpty(query))
            {
                sb.Append($"&search={Uri.EscapeDataString(query)}");
            }

            if (!string.IsNullOrEmpty(foodGroup))
            {
                sb.Append($"&foodGroup={Uri.EscapeDataString(foodGroup)}");
            }

            sb.Append($"&lang={Uri.EscapeDataString(Lang)}");

            using UnityWebRequest request = UnityWebRequest.Get(sb.ToString());
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] SearchGenericFoods"));
            }

            var response = JsonUtility.FromJson<PaginatedGenericFoodResponse>(request.downloadHandler.text);

            if (isDefaultSearch && response != null)
            {
                _defaultSearchCache = response;
            }

            return (response, null);
        }


        public async Task<(GenericFood Result, ApiErrorResponse Error)> GetGenericFoodByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return (null, null);
            }

            InvalidateCacheIfLangChanged();

            if (_cache.TryGetValue(id, out GenericFood cached))
            {
                return (cached, null);
            }


            string url = $"{ApiConfig.BaseUrl}/api/v1/generic-foods/{Uri.EscapeDataString(id)}?lang={Uri.EscapeDataString(Lang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetGenericFoodById {id}"));
            }

            GenericFood category = JsonUtility.FromJson<GenericFood>(request.downloadHandler.text);

            if (category != null && !string.IsNullOrEmpty(category.id))
            {
                _cache[category.id] = category;
            }

            return (category, null);
        }

        public async Task<(GenericFoodDetail Result, ApiErrorResponse Error)> GetGenericFoodDetailAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return (null, null);

            if (!Guid.TryParse(id, out _))
            {
                Debug.LogWarning($"[{GetType().Name}] GetGenericFoodDetailAsync — invalid UUID: {id}");
                return (null, null);
            }

            InvalidateCacheIfLangChanged();

            string url = $"{ApiConfig.BaseUrl}/api/v1/generic-foods/{Uri.EscapeDataString(id)}?lang={Uri.EscapeDataString(Lang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetGenericFoodDetail {id}"));

            try
            {
                GenericFoodDetail detail = JsonConvert.DeserializeObject<GenericFoodDetail>(request.downloadHandler.text);
                return (detail, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Parse error: {ex.Message}");
                return (null, null);
            }
        }

        public async Task<(FoodGroupItem[] Result, ApiErrorResponse Error)> GetFoodGroupsAsync()
        {
            InvalidateCacheIfLangChanged();

            if (_foodGroupsCache != null)
            {
                return (_foodGroupsCache, null);
            }

            string url = $"{ApiConfig.BaseUrl}/api/v1/generic-foods/food-groups?lang={Uri.EscapeDataString(Lang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetFoodGroups"));
            }

            string json = request.downloadHandler.text;
            FoodGroupItemArrayWrapper wrapper = JsonUtility.FromJson<FoodGroupItemArrayWrapper>("{\"items\":" + json + "}");
            if (wrapper?.items != null)
            {
                _foodGroupsCache = wrapper.items;
            }
            return (wrapper?.items, null);
        }
    }

    // Internal wrapper for top-level FoodGroupItem array responses
    [System.Serializable]
    internal class FoodGroupItemArrayWrapper
    {
        public FoodGroupItem[] items;
    }
}
