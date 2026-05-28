using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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

            if (response?.items != null && response.items.Length > 0)
            {
                return (response, null);
            }

            // Fallback: no seed data on this environment
            List<GenericFood> fallback = GetFallbackGenericFoods(query);
            return (new PaginatedGenericFoodResponse
            {
                items = fallback.ToArray(),
                total = fallback.Count,
                page = 1,
                limit = fallback.Count,
                totalPages = 1
            }, null);
        }

        private static List<GenericFood> GetFallbackGenericFoods(string query = null)
        {
            List<GenericFood> all = FallbackGenericFoods;

            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<GenericFood>(all);
            }

            string q = query.Trim().ToLowerInvariant();
            var filtered = new List<GenericFood>();
            foreach (var cat in all)
            {
                if (cat.foodName.ToLowerInvariant().Contains(q) ||
                    cat.foodGroup.ToLowerInvariant().Contains(q))
                {
                    filtered.Add(cat);
                }
            }
            return filtered;
        }

        private static List<GenericFood> s_FallbackGenericFoods;

        private static List<GenericFood> FallbackGenericFoods
        {
            get
            {
                if (s_FallbackGenericFoods == null)
                {
                    TextAsset asset = Resources.Load<TextAsset>("fallback-generic-foods");
                    if (asset != null)
                    {
                        var response = JsonUtility.FromJson<PaginatedGenericFoodResponse>(asset.text);
                        s_FallbackGenericFoods = response?.items != null
                            ? new List<GenericFood>(response.items)
                            : new List<GenericFood>();
                    }
                    else
                    {
                        s_FallbackGenericFoods = new List<GenericFood>();
                    }
                }
                return s_FallbackGenericFoods;
            }
        }

        private static string[] s_FallbackFoodGroups;

        private static string[] FallbackFoodGroups
        {
            get
            {
                if (s_FallbackFoodGroups == null)
                {
                    TextAsset asset = Resources.Load<TextAsset>("fallback-food-groups");
                    if (asset != null)
                    {
                        var wrapper = JsonUtility.FromJson<StringArrayWrapper>("{\"items\":" + asset.text + "}");
                        s_FallbackFoodGroups = wrapper?.items ?? Array.Empty<string>();
                    }
                    else
                    {
                        s_FallbackFoodGroups = Array.Empty<string>();
                    }
                }
                return s_FallbackFoodGroups;
            }
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

            foreach (var fb in FallbackGenericFoods)
            {
                if (fb.id == id)
                {
                    _cache[id] = fb;
                    return (fb, null);
                }
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

        public async Task<(string[] Result, ApiErrorResponse Error)> GetFoodGroupsAsync()
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/generic-foods/food-groups";

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
                string[] fallback = FallbackFoodGroups;
                if (fallback.Length > 0)
                {
                    return (fallback, null);
                }
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
