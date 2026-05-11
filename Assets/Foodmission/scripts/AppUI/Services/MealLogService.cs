using System;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class MealLogService : IMealLogService
    {
        private readonly IStoreService _storeService;

        public MealLogService(IStoreService storeService)
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

        public async Task<(MealLog Result, ApiErrorResponse Error)> CreateAsync(CreateMealLogRequest request)
        {
            if (request == null) return (null, null);

            byte[] body = request.ToJsonBody();
            string url = $"{ApiConfig.BaseUrl}/api/v1/meal-logs";

            Debug.Log($"[{GetType().Name}] CreateAsync calling: {url} body={Encoding.UTF8.GetString(body)}");

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

            string raw = req.downloadHandler.text;
            Debug.Log($"[{GetType().Name}] CreateAsync response: {raw}");
            return (JsonUtility.FromJson<MealLog>(raw), null);
        }

        public async Task<(PaginatedMealLogResponse Result, ApiErrorResponse Error)> GetLogsAsync(
            int page = 1,
            int limit = 20,
            string typeOfMeal = null,
            string dateFrom = null,
            string dateTo = null)
        {
            var sb = new StringBuilder($"{ApiConfig.BaseUrl}/api/v1/meal-logs?page={page}&limit={limit}");

            if (!string.IsNullOrEmpty(typeOfMeal))
                sb.Append($"&typeOfMeal={Uri.EscapeDataString(typeOfMeal)}");
            if (!string.IsNullOrEmpty(dateFrom))
                sb.Append($"&dateFrom={Uri.EscapeDataString(dateFrom)}");
            if (!string.IsNullOrEmpty(dateTo))
                sb.Append($"&dateTo={Uri.EscapeDataString(dateTo)}");

            string url = sb.ToString();
            Debug.Log($"[{GetType().Name}] GetLogsAsync calling: {url}");

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetLogsAsync"));
            }

            string raw = request.downloadHandler.text;
            Debug.Log($"[{GetType().Name}] GetLogsAsync response ({raw.Length} chars): {raw}");
            return (JsonUtility.FromJson<PaginatedMealLogResponse>(raw), null);
        }

        public async Task<(MealLog Result, ApiErrorResponse Error)> GetLogAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return (null, null);

            string url = $"{ApiConfig.BaseUrl}/api/v1/meal-logs/{Uri.EscapeDataString(id)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetLogAsync {id}"));
            }

            return (JsonUtility.FromJson<MealLog>(request.downloadHandler.text), null);
        }

        public async Task<(bool Success, ApiErrorResponse Error)> DeleteLogAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return (false, null);

            string url = $"{ApiConfig.BaseUrl}/api/v1/meal-logs/{Uri.EscapeDataString(id)}";
            Debug.Log($"[{GetType().Name}] DeleteLogAsync calling: {url}");

            using UnityWebRequest request = new UnityWebRequest(url, "DELETE")
            {
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Authorization", AuthHeader);

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (false, ApiErrorHelper.Parse(request, $"[{GetType().Name}] DeleteLogAsync {id}"));
            }

            return (true, null);
        }
    }
}
