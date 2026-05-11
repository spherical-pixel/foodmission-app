using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class FoodCategoryService : IFoodCategoryService
    {
        private readonly IStoreService _storeService;
        private readonly Dictionary<string, FoodCategory> _cache = new();

        public FoodCategoryService(IStoreService storeService)
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

        public async Task<(PaginatedFoodCategoryResponse Result, ApiErrorResponse Error)> SearchCategoriesAsync(
            string query = null, string foodGroup = null, int page = 1, int pageSize = 20)
        {
            var sb = new StringBuilder($"{ApiConfig.BaseUrl}/api/v1/food-categories?page={page}&limit={pageSize}");

            if (!string.IsNullOrEmpty(query))
            {
                sb.Append($"&search={Uri.EscapeDataString(query)}");
            }

            if (!string.IsNullOrEmpty(foodGroup))
            {
                sb.Append($"&foodGroup={Uri.EscapeDataString(foodGroup)}");
            }

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
                Debug.LogError($"[{GetType().Name}] SearchCategories failed: {request.responseCode} {request.error}");
                return (null, null);
            }

            return (JsonUtility.FromJson<PaginatedFoodCategoryResponse>(request.downloadHandler.text), null);
        }

        public async Task<(FoodCategory Result, ApiErrorResponse Error)> GetCategoryByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return (null, null);
            }

            if (_cache.TryGetValue(id, out FoodCategory cached))
            {
                return (cached, null);
            }

            string url = $"{ApiConfig.BaseUrl}/api/v1/food-categories/{Uri.EscapeDataString(id)}";

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
                Debug.LogWarning($"[{GetType().Name}] GetCategoryById {id} failed: {request.responseCode}");
                return (null, null);
            }

            FoodCategory category = JsonUtility.FromJson<FoodCategory>(request.downloadHandler.text);

            if (category != null && !string.IsNullOrEmpty(category.id))
            {
                _cache[category.id] = category;
            }

            return (category, null);
        }

        public async Task<(string[] Result, ApiErrorResponse Error)> GetFoodGroupsAsync()
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/food-categories/food-groups";

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
                Debug.LogError($"[{GetType().Name}] GetFoodGroups failed: {request.responseCode}");
                return (null, null);
            }

            string json = request.downloadHandler.text;
            StringArrayWrapper wrapper = JsonUtility.FromJson<StringArrayWrapper>("{\"items\":" + json + "}");
            return (wrapper?.items, null);
        }
    }

    // Internal wrapper for top-level string array responses
    [System.Serializable]
    internal class StringArrayWrapper
    {
        public string[] items;
    }
}
