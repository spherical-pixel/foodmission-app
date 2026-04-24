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

        public async Task<PaginatedFoodResponse> SearchFoodsAsync(string query, int page = 1, int pageSize = 20)
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
                Debug.LogError($"[{GetType().Name}] SearchFoods failed: {request.responseCode} {request.error}");
                return null;
            }

            return JsonUtility.FromJson<PaginatedFoodResponse>(request.downloadHandler.text);
        }

        public async Task<FoodItem> GetFoodByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            if (_cache.TryGetValue(id, out FoodItem cached))
            {
                return cached;
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
                Debug.LogWarning($"[{GetType().Name}] GetFoodById {id} failed: {request.responseCode}");
                return null;
            }

            FoodItem food = JsonUtility.FromJson<FoodItem>(request.downloadHandler.text);

            if (food != null && !string.IsNullOrEmpty(food.id))
            {
                _cache[food.id] = food;
            }

            return food;
        }

        public async Task<OpenFoodFactsSearchResponse> SearchOpenFoodFactsAsync(string query, int page = 1, int pageSize = 20)
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
                Debug.LogError($"[{GetType().Name}] SearchOpenFoodFacts failed: {request.responseCode} {request.error}");
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<OpenFoodFactsSearchResponse>(request.downloadHandler.text);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] SearchOpenFoodFacts parse error: {ex.Message}");
                return null;
            }
        }

        public async Task<FoodItem> ImportFromBarcodeAsync(string barcode)
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
                Debug.LogError($"[{GetType().Name}] ImportFromBarcode {barcode} failed: {request.responseCode} — {request.downloadHandler?.text}");
                return null;
            }

            FoodItem food = JsonUtility.FromJson<FoodItem>(request.downloadHandler.text);

            if (food != null && !string.IsNullOrEmpty(food.id))
            {
                _cache[food.id] = food;
            }

            return food;
        }
    }
}
