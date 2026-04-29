using System;
using System.Collections.Generic;
using System.Globalization;
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

        public async Task<PaginatedMealResponse> GetMealsAsync(
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
                Debug.LogError($"[{GetType().Name}] GetMeals failed: {request.responseCode}");
                return null;
            }

            return JsonUtility.FromJson<PaginatedMealResponse>(request.downloadHandler.text);
        }

        public async Task<Meal> GetMealAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            if (_cache.TryGetValue(id, out Meal cached))
            {
                return cached;
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
                Debug.LogWarning($"[{GetType().Name}] GetMeal {id} failed: {request.responseCode}");
                return null;
            }

            Meal meal = JsonUtility.FromJson<Meal>(request.downloadHandler.text);
            if (meal != null && !string.IsNullOrEmpty(meal.id))
            {
                _cache[meal.id] = meal;
            }

            return meal;
        }

        public async Task<Meal> CreateMealAsync(CreateMealRequest request)
        {
            if (request == null)
            {
                Debug.LogError($"[{GetType().Name}] CreateMeal — request is null");
                return null;
            }

            byte[] body = BuildBody(request.name, request.recipeId, request.calories,
                request.proteins, request.nutritionalInfo, request.sustainabilityScore,
                request.price, request.barcode, request.mealCategories,
                request.mealCourse, request.dietaryPreferences);

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
                Debug.LogError($"[{GetType().Name}] CreateMeal failed: {req.responseCode} — {req.downloadHandler?.text}");
                return null;
            }

            Meal meal = JsonUtility.FromJson<Meal>(req.downloadHandler.text);
            if (meal != null && !string.IsNullOrEmpty(meal.id))
            {
                _cache[meal.id] = meal;
            }

            return meal;
        }

        public async Task<Meal> UpdateMealAsync(string id, UpdateMealRequest request)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError($"[{GetType().Name}] UpdateMeal — id is null or empty");
                return null;
            }

            if (request == null)
            {
                Debug.LogError($"[{GetType().Name}] UpdateMeal — request is null");
                return null;
            }

            byte[] body = BuildBody(request.name, request.recipeId, request.calories,
                request.proteins, request.nutritionalInfo, request.sustainabilityScore,
                request.price, request.barcode, request.mealCategories,
                request.mealCourse, request.dietaryPreferences);

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
                Debug.LogError($"[{GetType().Name}] UpdateMeal {id} failed: {req.responseCode}");
                return null;
            }

            Meal meal = JsonUtility.FromJson<Meal>(req.downloadHandler.text);
            if (meal != null && !string.IsNullOrEmpty(meal.id))
            {
                _cache[meal.id] = meal;
            }

            return meal;
        }

        public async Task<bool> DeleteMealAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError($"[{GetType().Name}] DeleteMeal — id is null or empty");
                return false;
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
                Debug.LogError($"[{GetType().Name}] DeleteMeal {id} failed: {request.responseCode}");
                return false;
            }

            _cache.Remove(id);
            return true;
        }

        private static byte[] BuildBody(
            string name,
            string recipeId,
            float? calories,
            float? proteins,
            MealNutritionalInfo nutritionalInfo,
            float? sustainabilityScore,
            float? price,
            string barcode,
            string[] mealCategories,
            string mealCourse,
            string[] dietaryPreferences)
        {
            var sb = new StringBuilder("{");
            bool hasField = false;

            void Append(string fragment) { if (hasField) sb.Append(","); sb.Append(fragment); hasField = true; }
            string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(name))
            {
                Append($"\"name\":\"{EscapeJson(name)}\"");
            }
            if (!string.IsNullOrEmpty(recipeId))
            {
                Append($"\"recipeId\":\"{EscapeJson(recipeId)}\"");
            }
            if (calories.HasValue)
            {
                Append($"\"calories\":{F(calories.Value)}");
            }
            if (proteins.HasValue)
            {
                Append($"\"proteins\":{F(proteins.Value)}");
            }
            if (nutritionalInfo != null)
            {
                Append($"\"nutritionalInfo\":{{\"carbs\":{F(nutritionalInfo.carbs)},\"fats\":{F(nutritionalInfo.fats)},\"sugar\":{F(nutritionalInfo.sugar)}}}");
            }
            if (sustainabilityScore.HasValue)
            {
                Append($"\"sustainabilityScore\":{F(sustainabilityScore.Value)}");
            }
            if (price.HasValue)
            {
                Append($"\"price\":{F(price.Value)}");
            }
            if (!string.IsNullOrEmpty(barcode))
            {
                Append($"\"barcode\":\"{EscapeJson(barcode)}\"");
            }
            if (mealCategories != null && mealCategories.Length > 0)
            {
                var arr = new StringBuilder("[");
                for (int i = 0; i < mealCategories.Length; i++)
                {
                    if (i > 0)
                    {
                        arr.Append(",");
                    }
                    arr.Append($"\"{EscapeJson(mealCategories[i])}\"");
                }
                arr.Append("]");
                Append($"\"mealCategories\":{arr}");
            }
            if (!string.IsNullOrEmpty(mealCourse))
            {
                Append($"\"mealCourse\":\"{EscapeJson(mealCourse)}\"");
            }
            if (dietaryPreferences != null && dietaryPreferences.Length > 0)
            {
                var arr = new StringBuilder("[");
                for (int i = 0; i < dietaryPreferences.Length; i++)
                {
                    if (i > 0)
                    {
                        arr.Append(",");
                    }
                    arr.Append($"\"{EscapeJson(dietaryPreferences[i])}\"");
                }
                arr.Append("]");
                Append($"\"dietaryPreferences\":{arr}");
            }

            sb.Append("}");
            return Encoding.UTF8.GetBytes(sb.ToString());
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

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }
            return s
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}
