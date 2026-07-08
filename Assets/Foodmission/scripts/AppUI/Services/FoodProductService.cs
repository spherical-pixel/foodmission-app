using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Newtonsoft.Json;

using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class FoodProductService : IFoodProductService
    {
        private readonly IStoreService _storeService;
        private readonly Dictionary<string, FoodProduct> _cache = new();

        public FoodProductService(IStoreService storeService)
        {
            _storeService = storeService;
        }

        public async Task<(PaginatedFoodProductResponse Result, ApiErrorResponse Error)> SearchFoodsAsync(string query, int page = 1, int pageSize = 20)
        {
            AppState state = _storeService.GetAppState();
            string url = $"{ApiConfig.BaseUrl}/api/v1/food-products?search={Uri.EscapeDataString(query ?? "")}&page={page}&limit={pageSize}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", $"{state.tokenType} {state.accessToken}");
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] SearchFoods"));
            }

            return (JsonUtility.FromJson<PaginatedFoodProductResponse>(request.downloadHandler.text), null);
        }

        public async Task<(FoodProduct Result, ApiErrorResponse Error)> GetFoodByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return (null, null);
            }

            if (_cache.TryGetValue(id, out FoodProduct cached))
            {
                return (cached, null);
            }

            AppState state = _storeService.GetAppState();
            string url = $"{ApiConfig.BaseUrl}/api/v1/food-products/{Uri.EscapeDataString(id)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", $"{state.tokenType} {state.accessToken}");
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetFoodById {id}"));
            }

            FoodProduct food = JsonUtility.FromJson<FoodProduct>(request.downloadHandler.text);

            if (food != null && !string.IsNullOrEmpty(food.id))
            {
                _cache[food.id] = food;
            }

            return (food, null);
        }

        public async Task<(OpenFoodFactsSearchResponse Result, ApiErrorResponse Error)> SearchOpenFoodFactsAsync(string query, int page = 1, int pageSize = 20)
        {
            AppState state = _storeService.GetAppState();
            string url = $"{ApiConfig.BaseUrl}/api/v1/food-products/search/openfoodfacts" +
                         $"?query={Uri.EscapeDataString(query ?? "")}&page={page}&limit={pageSize}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", $"{state.tokenType} {state.accessToken}");
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] SearchOpenFoodFacts"));
            }

            try
            {
                return (JsonConvert.DeserializeObject<OpenFoodFactsSearchResponse>(request.downloadHandler.text), null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] SearchOpenFoodFacts parse error: {ex.Message}");
                return (null, null);
            }
        }

        public async Task<(FoodProduct Result, ApiErrorResponse Error)> ImportFromBarcodeAsync(string barcode)
        {
            AppState state = _storeService.GetAppState();
            string url = $"{ApiConfig.BaseUrl}/api/v1/food-products/import/openfoodfacts/{Uri.EscapeDataString(barcode)}";

            using UnityWebRequest request = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Array.Empty<byte>()) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Authorization", $"{state.tokenType} {state.accessToken}");
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] ImportFromBarcode {barcode}"));
            }

            FoodProduct food = JsonUtility.FromJson<FoodProduct>(request.downloadHandler.text);

            if (food != null && !string.IsNullOrEmpty(food.id))
            {
                _cache[food.id] = food;
            }

            return (food, null);
        }

        public async Task<(FoodProduct Result, ApiErrorResponse Error)> FindByBarcodeAsync(string barcode, bool includeOpenFoodFacts = false)
        {
            if (string.IsNullOrEmpty(barcode))
            {
                return (null, null);
            }

            AppState state = _storeService.GetAppState();
            string url = $"{ApiConfig.BaseUrl}/api/v1/food-products/barcode/{Uri.EscapeDataString(barcode)}?includeOpenFoodFacts={(includeOpenFoodFacts ? "true" : "false")}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", $"{state.tokenType} {state.accessToken}");
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] FindByBarcode {barcode}"));
            }

            FoodProduct food = JsonUtility.FromJson<FoodProduct>(request.downloadHandler.text);

            if (food != null && !string.IsNullOrEmpty(food.id))
            {
                _cache[food.id] = food;
            }

            return (food, null);
        }

        public async Task<(FoodProductDetail Result, ApiErrorResponse Error)> GetFoodProductDetailAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return (null, null);

            if (!Guid.TryParse(id, out _))
            {
                Debug.LogWarning($"[{GetType().Name}] GetFoodProductDetailAsync — invalid UUID: {id}");
                return (null, null);
            }

            AppState state = _storeService.GetAppState();
            string url = $"{ApiConfig.BaseUrl}/api/v1/food-products/{Uri.EscapeDataString(id)}?includeOff=true";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", $"{state.tokenType} {state.accessToken}");
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetFoodProductDetail {id}"));

            try
            {
                FoodProductDetail detail = JsonConvert.DeserializeObject<FoodProductDetail>(request.downloadHandler.text);
                return (detail, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Parse error: {ex.Message}");
                return (null, null);
            }
        }
    }
}
