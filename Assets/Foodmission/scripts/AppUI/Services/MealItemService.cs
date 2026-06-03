using System.Threading.Tasks;

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

            string url = $"{ApiConfig.BaseUrl}/api/v1/meals/{mealId}/meal-items";

            using UnityWebRequest req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Authorization", AuthHeader);
            req.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                return (null, ApiErrorHelper.Parse(req, $"[{GetType().Name}] GetByMealIdAsync"));

            string raw = req.downloadHandler.text;
            string wrapped = "{\"data\":" + raw + "}";
            var response = JsonUtility.FromJson<MealItemDetailList>(wrapped);
            return (response?.data, null);
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

            return (JsonUtility.FromJson<MealItem>(req.downloadHandler.text), null);
        }
    }
}
