using System;
using System.Threading.Tasks;

using Newtonsoft.Json;

using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class MealItemService : IMealItemService
    {
        private readonly IStoreService _storeService;

        public MealItemService(IStoreService storeService)
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

        public async Task<(MealItemDetail[] Result, ApiErrorResponse Error)> GetByMealIdAsync(string mealId)
        {
            if (string.IsNullOrEmpty(mealId))
                return (null, null);

            string lang = _storeService.GetAppState().lang ?? "en";
            string url = $"{ApiConfig.BaseUrl}/api/v1/meals/{mealId}/meal-items";

            using UnityWebRequest req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Authorization", AuthHeader);
            req.SetRequestHeader("Accept", "application/json");
            req.SetRequestHeader("Accept-Language", lang);

            UnityWebRequestAsyncOperation op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                return (null, ApiErrorHelper.Parse(req, $"[{GetType().Name}] GetByMealIdAsync"));

            string raw = req.downloadHandler.text;
            MealItemDetail[] items = JsonConvert.DeserializeObject<MealItemDetail[]>(raw);
            return (items, null);
        }

        public async Task<(MealItem Result, ApiErrorResponse Error)> CreateAsync(string mealId, CreateMealItemRequest request)
        {
            if (string.IsNullOrEmpty(mealId) || request == null)
                return (null, null);

            byte[] body = request.ToJsonBody();
            string url = $"{ApiConfig.BaseUrl}/api/v1/meals/{mealId}/meal-items";

            using UnityWebRequest req = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Authorization", AuthHeader);
            req.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                return (null, ApiErrorHelper.Parse(req, $"[{GetType().Name}] CreateAsync"));

            return (JsonConvert.DeserializeObject<MealItem>(req.downloadHandler.text), null);
        }

        public async Task<(MealItem Result, ApiErrorResponse Error)> UpdateAsync(string mealId, string itemId, CreateMealItemRequest request)
        {
            if (string.IsNullOrEmpty(mealId) || string.IsNullOrEmpty(itemId) || request == null)
                return (null, null);

            byte[] body = request.ToJsonBody();
            string url = $"{ApiConfig.BaseUrl}/api/v1/meals/{mealId}/meal-items/{itemId}";

            using UnityWebRequest req = new UnityWebRequest(url, "PATCH")
            {
                uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Authorization", AuthHeader);
            req.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                return (null, ApiErrorHelper.Parse(req, $"[{GetType().Name}] UpdateAsync"));

            return (JsonConvert.DeserializeObject<MealItem>(req.downloadHandler.text), null);
        }

        public async Task<(bool Success, ApiErrorResponse Error)> DeleteAsync(string mealId, string itemId)
        {
            if (string.IsNullOrEmpty(mealId) || string.IsNullOrEmpty(itemId))
                return (true, null);

            string url = $"{ApiConfig.BaseUrl}/api/v1/meals/{mealId}/meal-items/{itemId}";

            using UnityWebRequest req = new UnityWebRequest(url, "DELETE")
            {
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Authorization", AuthHeader);
            req.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                return (false, ApiErrorHelper.Parse(req, $"[{GetType().Name}] DeleteAsync"));

            return (true, null);
        }
    }
}
