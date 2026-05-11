using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class MealService : IMealService
    {
        private readonly IStoreService _storeService;
        private readonly Dictionary<string, Meal> _cache = new();

        public MealService(IStoreService storeService)
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

        public async Task<(PaginatedMealResponse Result, ApiErrorResponse Error)> GetMealsAsync(
            string search = null,
            string mealCategory = null,
            string mealCourse = null,
            string dietaryPreference = null,
            string recipeId = null,
            int page = 1,
            int limit = 20)
        {
            var sb = new StringBuilder($"{ApiConfig.BaseUrl}/api/v1/meals?page={page}&limit={limit}");

            if (!string.IsNullOrEmpty(search))
            {
                sb.Append($"&search={Uri.EscapeDataString(search)}");
            }
            if (!string.IsNullOrEmpty(mealCategory))
            {
                sb.Append($"&mealCategory={Uri.EscapeDataString(mealCategory)}");
            }
            if (!string.IsNullOrEmpty(mealCourse))
            {
                sb.Append($"&mealCourse={Uri.EscapeDataString(mealCourse)}");
            }
            if (!string.IsNullOrEmpty(dietaryPreference))
            {
                sb.Append($"&dietaryPreference={Uri.EscapeDataString(dietaryPreference)}");
            }
            if (!string.IsNullOrEmpty(recipeId))
            {
                sb.Append($"&recipeId={Uri.EscapeDataString(recipeId)}");
            }

            string url = sb.ToString();
            Debug.Log($"[{GetType().Name}] GetMealsAsync calling: {url}");

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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetMealsAsync"));
            }

            string raw = request.downloadHandler.text;

            PaginatedMealResponse result = JsonUtility.FromJson<PaginatedMealResponse>(raw);
            Debug.Log($"[{GetType().Name}] GetMealsAsync parsed: {result?.data?.Length ?? 0} meals, page {result?.page}/{result?.totalPages}");
            return (result, null);
        }

        public async Task<(Meal Result, ApiErrorResponse Error)> GetMealAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return (null, null);
            }

            if (_cache.TryGetValue(id, out Meal cached))
            {
                return (cached, null);
            }

            string url = $"{ApiConfig.BaseUrl}/api/v1/meals/{Uri.EscapeDataString(id)}";

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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetMeal {id}"));
            }

            Meal meal = JsonUtility.FromJson<Meal>(request.downloadHandler.text);
            if (meal != null && !string.IsNullOrEmpty(meal.id))
            {
                _cache[meal.id] = meal;
            }

            return (meal, null);
        }

        public async Task<(Meal Result, ApiErrorResponse Error)> CreateMealAsync(CreateMealRequest request)
        {
            if (request == null)
            {
                Debug.LogError($"[{GetType().Name}] CreateMeal — request is null");
                return (null, null);
            }

            byte[] body = request.ToJsonBody();

            string url = $"{ApiConfig.BaseUrl}/api/v1/meals";

            using UnityWebRequest req = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Authorization", AuthHeader);
            req.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = req.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(req, $"[{GetType().Name}] CreateMeal"));
            }

            Meal meal = JsonUtility.FromJson<Meal>(req.downloadHandler.text);
            if (meal != null && !string.IsNullOrEmpty(meal.id))
            {
                _cache[meal.id] = meal;
            }

            return (meal, null);
        }

        public async Task<(Meal Result, ApiErrorResponse Error)> UpdateMealAsync(string id, UpdateMealRequest request)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError($"[{GetType().Name}] UpdateMeal — id is null or empty");
                return (null, null);
            }

            if (request == null)
            {
                Debug.LogError($"[{GetType().Name}] UpdateMeal — request is null");
                return (null, null);
            }

            byte[] body = request.ToJsonBody();

            string url = $"{ApiConfig.BaseUrl}/api/v1/meals/{Uri.EscapeDataString(id)}";

            using UnityWebRequest req = MakePatchRequest(url, body);
            req.SetRequestHeader("Authorization", AuthHeader);
            req.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = req.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(req, $"[{GetType().Name}] UpdateMeal {id}"));
            }

            Meal meal = JsonUtility.FromJson<Meal>(req.downloadHandler.text);
            if (meal != null && !string.IsNullOrEmpty(meal.id))
            {
                _cache[meal.id] = meal;
            }

            return (meal, null);
        }

        public async Task<(bool Success, ApiErrorResponse Error)> DeleteMealAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError($"[{GetType().Name}] DeleteMeal — id is null or empty");
                return (false, null);
            }

            string url = $"{ApiConfig.BaseUrl}/api/v1/meals/{Uri.EscapeDataString(id)}";

            using UnityWebRequest request = new UnityWebRequest(url, "DELETE")
            {
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Authorization", AuthHeader);

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (false, ApiErrorHelper.Parse(request, $"[{GetType().Name}] DeleteMeal {id}"));
            }

            _cache.Remove(id);
            return (true, null);
        }

        // uploadHandler must be assigned after construction (not in initializer) for PATCH
        // to work correctly with NestJS — matches the pattern used in AuthService.SendPatchRequest.
        // For bodyless PATCH, omit uploadHandler entirely.
        private static UnityWebRequest MakePatchRequest(string url, byte[] body)
        {
            UnityWebRequest request = new UnityWebRequest(url, "PATCH");
            if (body != null && body.Length > 0)
            {
                request.uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" };
            }
            request.downloadHandler = new DownloadHandlerBuffer();
            return request;
        }

    }
}
