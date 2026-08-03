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
        private string _cachedLang;

        public FoodProductService(IStoreService storeService)
        {
            _storeService = storeService;
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
                _cache.Clear();
            }
        }

        public async Task<(PaginatedFoodProductResponse Result, ApiErrorResponse Error)> SearchFoodsAsync(string query, int page = 1, int pageSize = 20)
        {
            AppState state = _storeService.GetAppState();
            string url = $"{ApiConfig.BaseUrl}/api/v1/food-products?search={Uri.EscapeDataString(query ?? "")}&page={page}&limit={pageSize}&lang={Uri.EscapeDataString(Lang)}";

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

        public async Task<(PaginatedFoodProductResponse Result, ApiErrorResponse Error)> SearchFoodsByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrEmpty(barcode))
                return (null, null);

            AppState state = _storeService.GetAppState();
            string url = $"{ApiConfig.BaseUrl}/api/v1/food-products?barcode={Uri.EscapeDataString(barcode)}&page=1&limit=10&lang={Uri.EscapeDataString(Lang)}";

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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] SearchFoodsByBarcode {barcode}"));
            }

            return (JsonUtility.FromJson<PaginatedFoodProductResponse>(request.downloadHandler.text), null);
        }

        public async Task<(FoodProduct Result, ApiErrorResponse Error)> GetFoodByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return (null, null);
            }

            InvalidateCacheIfLangChanged();

            if (_cache.TryGetValue(id, out FoodProduct cached))
            {
                return (cached, null);
            }

            AppState state = _storeService.GetAppState();
            string url = $"{ApiConfig.BaseUrl}/api/v1/food-products/{Uri.EscapeDataString(id)}?lang={Uri.EscapeDataString(Lang)}";

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
                         $"?query={Uri.EscapeDataString(query ?? "")}&page={page}&limit={pageSize}&lang={Uri.EscapeDataString(Lang)}";

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

            InvalidateCacheIfLangChanged();

            AppState state = _storeService.GetAppState();
            string url = $"{ApiConfig.BaseUrl}/api/v1/food-products/barcode/{Uri.EscapeDataString(barcode)}?includeOpenFoodFacts={(includeOpenFoodFacts ? "true" : "false")}&lang={Uri.EscapeDataString(Lang)}";

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
                bool is404 = request.responseCode == 404;
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] FindByBarcode {barcode}", logAsError: !is404));
            }

            FoodProduct food = JsonUtility.FromJson<FoodProduct>(request.downloadHandler.text);

            if (food != null && !string.IsNullOrEmpty(food.id))
            {
                _cache[food.id] = food;
            }

            return (food, null);
        }

        public async Task<(FoodProduct Result, ApiErrorResponse Error)> CreateAsync(CreateFoodProductRequest requestDto)
        {
            if (requestDto == null)
            {
                return (null, null);
            }

            AppState state = _storeService.GetAppState();
            string url = $"{ApiConfig.BaseUrl}/api/v1/food-products";
            string bodyJson = requestDto.ToJsonBody();

            UnityEngine.Debug.Log($"[FoodProductService] CreateAsync request JSON payload: {bodyJson}");

            using UnityWebRequest request = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(bodyJson))
                {
                    contentType = "application/json"
                },
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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] Create"));
            }

            FoodProduct food = JsonUtility.FromJson<FoodProduct>(request.downloadHandler.text);

            if (food != null && !string.IsNullOrEmpty(food.id))
            {
                _cache[food.id] = food;
            }

            return (food, null);
        }

        public async Task<(FoodProductDetail Result, ApiErrorResponse Error)> GetFoodProductDetailAsync(string idOrBarcode)
        {
            if (string.IsNullOrEmpty(idOrBarcode))
                return (null, null);

            InvalidateCacheIfLangChanged();

            AppState state = _storeService.GetAppState();
            string endpoint = Guid.TryParse(idOrBarcode, out _)
                ? $"{ApiConfig.BaseUrl}/api/v1/food-products/{Uri.EscapeDataString(idOrBarcode)}?includeOpenFoodFacts=true&lang={Uri.EscapeDataString(Lang)}"
                : $"{ApiConfig.BaseUrl}/api/v1/food-products/barcode/{Uri.EscapeDataString(idOrBarcode)}?includeOpenFoodFacts=true&lang={Uri.EscapeDataString(Lang)}";

            using UnityWebRequest request = UnityWebRequest.Get(endpoint);
            request.SetRequestHeader("Authorization", $"{state.tokenType} {state.accessToken}");
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetFoodProductDetail {idOrBarcode}"));

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
