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

        public async Task<(PaginatedGenericFoodResponse Result, ApiErrorResponse Error)> SearchGenericFoodsAsync(
            string query = null, string foodGroup = null, int page = 1, int pageSize = 20)
        {
            var sb = new StringBuilder($"{ApiConfig.BaseUrl}/api/v1/generic-foods?page={page}&limit={pageSize}");

            if (!string.IsNullOrEmpty(query))
            {
                sb.Append($"&search={Uri.EscapeDataString(query)}");
            }

            if (!string.IsNullOrEmpty(foodGroup))
            {
                sb.Append($"&foodGroup={Uri.EscapeDataString(foodGroup)}");
            }

            // TODO: Uncomment this line when the backend supports the lang parameter for generic foods search
            //sb.Append($"&lang={Uri.EscapeDataString(Lang)}");

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

            // if (response?.items != null && response.items.Length > 0)
            // {
            return (response, null);
            // }
        }


        public async Task<(GenericFood Result, ApiErrorResponse Error)> GetGenericFoodByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return (null, null);
            }

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

            string url = $"{ApiConfig.BaseUrl}/api/v1/generic-foods/{Uri.EscapeDataString(id)}";

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

        public async Task<(string[] Result, ApiErrorResponse Error)> GetFoodGroupsAsync()
        {
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
