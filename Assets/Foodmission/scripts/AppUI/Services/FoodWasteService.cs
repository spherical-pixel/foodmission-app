using System;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class FoodWasteService : IFoodWasteService
    {
        private readonly IStoreService _storeService;

        public FoodWasteService(IStoreService storeService)
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

        public async Task<(PaginatedFoodWasteResponse Result, ApiErrorResponse Error)> GetListAsync(
            int page = 1,
            int limit = 20,
            string wasteReason = null,
            string detectionMethod = null,
            string dateFrom = null,
            string dateTo = null)
        {
            var sb = new StringBuilder($"{ApiConfig.BaseUrl}/api/v1/food-waste?page={page}&limit={limit}");

            if (!string.IsNullOrEmpty(wasteReason))
                sb.Append($"&wasteReason={Uri.EscapeDataString(wasteReason)}");
            if (!string.IsNullOrEmpty(detectionMethod))
                sb.Append($"&detectionMethod={Uri.EscapeDataString(detectionMethod)}");
            if (!string.IsNullOrEmpty(dateFrom))
                sb.Append($"&dateFrom={Uri.EscapeDataString(dateFrom)}");
            if (!string.IsNullOrEmpty(dateTo))
                sb.Append($"&dateTo={Uri.EscapeDataString(dateTo)}");

            using UnityWebRequest request = UnityWebRequest.Get(sb.ToString());
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetListAsync"));
            }

            return (JsonUtility.FromJson<PaginatedFoodWasteResponse>(request.downloadHandler.text), null);
        }

        public async Task<(FoodWaste Result, ApiErrorResponse Error)> CreateAsync(CreateFoodWasteRequest request)
        {
            if (request == null) return (null, null);

            byte[] body = request.ToJsonBody();
            string url = $"{ApiConfig.BaseUrl}/api/v1/food-waste";

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
            {
                return (null, ApiErrorHelper.Parse(req, $"[{GetType().Name}] CreateAsync"));
            }

            return (JsonUtility.FromJson<FoodWaste>(req.downloadHandler.text), null);
        }

        public async Task<(FoodWaste Result, ApiErrorResponse Error)> GetByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return (null, null);

            string url = $"{ApiConfig.BaseUrl}/api/v1/food-waste/{Uri.EscapeDataString(id)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetByIdAsync {id}"));
            }

            return (JsonUtility.FromJson<FoodWaste>(request.downloadHandler.text), null);
        }

        public async Task<(FoodWaste Result, ApiErrorResponse Error)> UpdateAsync(string id, UpdateFoodWasteRequest request)
        {
            if (string.IsNullOrEmpty(id) || request == null) return (null, null);

            byte[] body = request.ToJsonBody();
            string url = $"{ApiConfig.BaseUrl}/api/v1/food-waste/{Uri.EscapeDataString(id)}";

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
            {
                return (null, ApiErrorHelper.Parse(req, $"[{GetType().Name}] UpdateAsync {id}"));
            }

            return (JsonUtility.FromJson<FoodWaste>(req.downloadHandler.text), null);
        }

        public async Task<(bool Success, ApiErrorResponse Error)> DeleteAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return (false, null);

            string url = $"{ApiConfig.BaseUrl}/api/v1/food-waste/{Uri.EscapeDataString(id)}";

            using UnityWebRequest request = new UnityWebRequest(url, "DELETE")
            {
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Authorization", AuthHeader);

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (false, ApiErrorHelper.Parse(request, $"[{GetType().Name}] DeleteAsync {id}"));
            }

            return (true, null);
        }

        public async Task<(FoodWasteStatistics Result, ApiErrorResponse Error)> GetStatisticsAsync(string dateFrom = null, string dateTo = null)
        {
            var sb = new StringBuilder($"{ApiConfig.BaseUrl}/api/v1/food-waste/statistics");

            bool hasParam = false;
            if (!string.IsNullOrEmpty(dateFrom))
            {
                sb.Append($"?dateFrom={Uri.EscapeDataString(dateFrom)}");
                hasParam = true;
            }
            if (!string.IsNullOrEmpty(dateTo))
            {
                sb.Append($"{(hasParam ? "&" : "?")}dateTo={Uri.EscapeDataString(dateTo)}");
            }

            using UnityWebRequest request = UnityWebRequest.Get(sb.ToString());
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetStatisticsAsync"));
            }

            return (JsonUtility.FromJson<FoodWasteStatistics>(request.downloadHandler.text), null);
        }

        public async Task<(FoodWasteTrends Result, ApiErrorResponse Error)> GetTrendsAsync(string dateFrom, string dateTo, string interval = "day")
        {
            var sb = new StringBuilder($"{ApiConfig.BaseUrl}/api/v1/food-waste/trends?dateFrom={Uri.EscapeDataString(dateFrom)}&dateTo={Uri.EscapeDataString(dateTo)}&interval={Uri.EscapeDataString(interval)}");

            using UnityWebRequest request = UnityWebRequest.Get(sb.ToString());
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetTrendsAsync"));
            }

            return (JsonUtility.FromJson<FoodWasteTrends>(request.downloadHandler.text), null);
        }
    }
}
