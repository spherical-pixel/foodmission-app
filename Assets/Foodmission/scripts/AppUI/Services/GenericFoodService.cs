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

        private static List<GenericFood> FallbackGenericFoods => new()
        {
            new GenericFood { id = "fb-veg-01", foodName = "Tomato", foodGroup = "Vegetables" },
            new GenericFood { id = "fb-veg-02", foodName = "Lettuce", foodGroup = "Vegetables" },
            new GenericFood { id = "fb-veg-03", foodName = "Carrot", foodGroup = "Vegetables" },
            new GenericFood { id = "fb-veg-04", foodName = "Onion", foodGroup = "Vegetables" },
            new GenericFood { id = "fb-veg-05", foodName = "Broccoli", foodGroup = "Vegetables" },
            new GenericFood { id = "fb-veg-06", foodName = "Spinach", foodGroup = "Vegetables" },
            new GenericFood { id = "fb-veg-07", foodName = "Potato", foodGroup = "Vegetables" },
            new GenericFood { id = "fb-veg-08", foodName = "Garlic", foodGroup = "Vegetables" },
            new GenericFood { id = "fb-veg-09", foodName = "Bell Pepper", foodGroup = "Vegetables" },
            new GenericFood { id = "fb-veg-10", foodName = "Cucumber", foodGroup = "Vegetables" },
            new GenericFood { id = "fb-frt-01", foodName = "Apple", foodGroup = "Fruits" },
            new GenericFood { id = "fb-frt-02", foodName = "Banana", foodGroup = "Fruits" },
            new GenericFood { id = "fb-frt-03", foodName = "Orange", foodGroup = "Fruits" },
            new GenericFood { id = "fb-frt-04", foodName = "Strawberry", foodGroup = "Fruits" },
            new GenericFood { id = "fb-frt-05", foodName = "Grapes", foodGroup = "Fruits" },
            new GenericFood { id = "fb-frt-06", foodName = "Lemon", foodGroup = "Fruits" },
            new GenericFood { id = "fb-frt-07", foodName = "Watermelon", foodGroup = "Fruits" },
            new GenericFood { id = "fb-dry-01", foodName = "Milk", foodGroup = "Dairy" },
            new GenericFood { id = "fb-dry-02", foodName = "Cheese", foodGroup = "Dairy" },
            new GenericFood { id = "fb-dry-03", foodName = "Yogurt", foodGroup = "Dairy" },
            new GenericFood { id = "fb-dry-04", foodName = "Butter", foodGroup = "Dairy" },
            new GenericFood { id = "fb-dry-05", foodName = "Eggs", foodGroup = "Dairy" },
            new GenericFood { id = "fb-grn-01", foodName = "Rice", foodGroup = "Grains" },
            new GenericFood { id = "fb-grn-02", foodName = "Bread", foodGroup = "Grains" },
            new GenericFood { id = "fb-grn-03", foodName = "Pasta", foodGroup = "Grains" },
            new GenericFood { id = "fb-grn-04", foodName = "Oats", foodGroup = "Grains" },
            new GenericFood { id = "fb-grn-05", foodName = "Flour", foodGroup = "Grains" },
            new GenericFood { id = "fb-prt-01", foodName = "Chicken Breast", foodGroup = "Proteins" },
            new GenericFood { id = "fb-prt-02", foodName = "Ground Beef", foodGroup = "Proteins" },
            new GenericFood { id = "fb-prt-03", foodName = "Pork", foodGroup = "Proteins" },
            new GenericFood { id = "fb-prt-04", foodName = "Salmon", foodGroup = "Proteins" },
            new GenericFood { id = "fb-prt-05", foodName = "Tofu", foodGroup = "Proteins" },
            new GenericFood { id = "fb-prt-06", foodName = "Beans", foodGroup = "Proteins" },
            new GenericFood { id = "fb-prt-07", foodName = "Lentils", foodGroup = "Proteins" },
            new GenericFood { id = "fb-cnd-01", foodName = "Olive Oil", foodGroup = "Condiments" },
            new GenericFood { id = "fb-cnd-02", foodName = "Salt", foodGroup = "Condiments" },
            new GenericFood { id = "fb-cnd-03", foodName = "Sugar", foodGroup = "Condiments" },
            new GenericFood { id = "fb-cnd-04", foodName = "Vinegar", foodGroup = "Condiments" },
            new GenericFood { id = "fb-cnd-05", foodName = "Soy Sauce", foodGroup = "Condiments" },
            new GenericFood { id = "fb-cnd-06", foodName = "Ketchup", foodGroup = "Condiments" },
            new GenericFood { id = "fb-bvg-01", foodName = "Water", foodGroup = "Beverages" },
            new GenericFood { id = "fb-bvg-02", foodName = "Orange Juice", foodGroup = "Beverages" },
            new GenericFood { id = "fb-bvg-03", foodName = "Coffee", foodGroup = "Beverages" },
            new GenericFood { id = "fb-bvg-04", foodName = "Tea", foodGroup = "Beverages" },
        };

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
