using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Newtonsoft.Json;

using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class FoodService : IFoodService
    {
        private readonly IStoreService _storeService;
        private readonly Dictionary<string, FoodItem> _cache = new();

        public FoodService(IStoreService storeService)
        {
            _storeService = storeService;
        }

        public async Task<(PaginatedFoodResponse Result, ApiErrorResponse Error)> SearchFoodsAsync(string query, int page = 1, int pageSize = 20)
        {
            AppState state = _storeService.GetAppState();
            string url = $"{ApiConfig.BaseUrl}/api/v1/foods?search={Uri.EscapeDataString(query ?? "")}&page={page}&pageSize={pageSize}";

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

            return (JsonUtility.FromJson<PaginatedFoodResponse>(request.downloadHandler.text), null);
        }

        public async Task<(FoodItem Result, ApiErrorResponse Error)> GetFoodByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return (null, null);
            }

            if (_cache.TryGetValue(id, out FoodItem cached))
            {
                return (cached, null);
            }

            AppState state = _storeService.GetAppState();
            string url = $"{ApiConfig.BaseUrl}/api/v1/foods/{Uri.EscapeDataString(id)}";

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

            FoodItem food = JsonUtility.FromJson<FoodItem>(request.downloadHandler.text);

            if (food != null && !string.IsNullOrEmpty(food.id))
            {
                _cache[food.id] = food;
            }

            return (food, null);
        }

        public async Task<(OpenFoodFactsSearchResponse Result, ApiErrorResponse Error)> SearchOpenFoodFactsAsync(string query, int page = 1, int pageSize = 20)
        {
            AppState state = _storeService.GetAppState();
            string url = $"{ApiConfig.BaseUrl}/api/v1/foods/search/openfoodfacts" +
                         $"?query={Uri.EscapeDataString(query ?? "")}&page={page}&pageSize={pageSize}";

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

        public async Task<(FoodItem Result, ApiErrorResponse Error)> ImportFromBarcodeAsync(string barcode)
        {
            AppState state = _storeService.GetAppState();
            string url = $"{ApiConfig.BaseUrl}/api/v1/foods/import/openfoodfacts/{Uri.EscapeDataString(barcode)}";

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

            FoodItem food = JsonUtility.FromJson<FoodItem>(request.downloadHandler.text);

            if (food != null && !string.IsNullOrEmpty(food.id))
            {
                _cache[food.id] = food;
            }

            return (food, null);
        }

        public async Task<(FoodItem Result, ApiErrorResponse Error)> FindByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrEmpty(barcode))
            {
                return (null, null);
            }

            AppState state = _storeService.GetAppState();
            string url = $"{ApiConfig.BaseUrl}/api/v1/foods/barcode/{Uri.EscapeDataString(barcode)}";

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

            FoodItem food = JsonUtility.FromJson<FoodItem>(request.downloadHandler.text);

            if (food != null && !string.IsNullOrEmpty(food.id))
            {
                _cache[food.id] = food;
            }

            return (food, null);
        }
    }
}
