using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.AppUI.MVVM;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class FoodFactService : IFoodFactService
    {
        private const string LocalReadFactsKey = "fm_read_food_facts";

        private readonly IStoreService _storeService;
        private readonly ILocalStorageService _localStorageService;

        public FoodFactService(IStoreService storeService, ILocalStorageService localStorageService = null)
        {
            _storeService = storeService;
            _localStorageService = localStorageService ?? App.current?.services?.GetService<ILocalStorageService>();
        }

        private string AuthHeader
        {
            get
            {
                AppState s = _storeService?.GetAppState();
                if (s == null || string.IsNullOrEmpty(s.accessToken))
                    return string.Empty;
                return $"{s.tokenType} {s.accessToken}";
            }
        }

        private string ResolveLang(string lang)
        {
            if (!string.IsNullOrEmpty(lang))
                return lang;

            AppState s = _storeService?.GetAppState();
            if (s != null && !string.IsNullOrEmpty(s.lang) && s.lang != "none")
                return s.lang;

            return "en";
        }

        public async Task<(PaginatedFoodFactResponse Result, ApiErrorResponse Error)> GetFoodFactsAsync(
            FoodFactFilterParams filters = null,
            int page = 1,
            int limit = 10,
            string lang = null)
        {
            string effectiveLang = ResolveLang(lang);
            var sb = new StringBuilder($"{ApiConfig.BaseUrl}/api/v1/food-facts?page={page}&limit={limit}&lang={Uri.EscapeDataString(effectiveLang)}");

            if (filters != null)
            {
                if (!string.IsNullOrEmpty(filters.dimensionCode))
                    sb.Append($"&dimensionCode={Uri.EscapeDataString(filters.dimensionCode)}");
                if (!string.IsNullOrEmpty(filters.topicCode))
                    sb.Append($"&topicCode={Uri.EscapeDataString(filters.topicCode)}");
                if (!string.IsNullOrEmpty(filters.level))
                    sb.Append($"&level={Uri.EscapeDataString(filters.level)}");
                if (filters.health.HasValue)
                    sb.Append($"&health={filters.health.Value.ToString().ToLowerInvariant()}");
                if (filters.foodChoice.HasValue)
                    sb.Append($"&foodChoice={filters.foodChoice.Value.ToString().ToLowerInvariant()}");
                if (filters.foodWaste.HasValue)
                    sb.Append($"&foodWaste={filters.foodWaste.Value.ToString().ToLowerInvariant()}");
                if (!string.IsNullOrEmpty(filters.search))
                    sb.Append($"&search={Uri.EscapeDataString(filters.search)}");
            }

            using UnityWebRequest request = UnityWebRequest.Get(sb.ToString());
            if (!string.IsNullOrEmpty(AuthHeader))
            {
                request.SetRequestHeader("Authorization", AuthHeader);
            }
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetFoodFactsAsync"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var response = JsonConvert.DeserializeObject<PaginatedFoodFactResponse>(raw);
                return (response, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize PaginatedFoodFactResponse: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(FoodFact Result, ApiErrorResponse Error)> GetFoodFactAsync(
            string codeOrId,
            string lang = null)
        {
            if (string.IsNullOrEmpty(codeOrId))
            {
                return (null, null);
            }

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/food-facts/{Uri.EscapeDataString(codeOrId)}?lang={Uri.EscapeDataString(effectiveLang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            if (!string.IsNullOrEmpty(AuthHeader))
            {
                request.SetRequestHeader("Authorization", AuthHeader);
            }
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetFoodFactAsync {codeOrId}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var fact = JsonConvert.DeserializeObject<FoodFact>(raw);
                return (fact, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize FoodFact {codeOrId}: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(FoodFact Result, ApiErrorResponse Error)> GetFoodFactByCodeAsync(
            string code,
            string lang = null)
        {
            if (string.IsNullOrEmpty(code))
            {
                return (null, null);
            }

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/food-facts/by-code/{Uri.EscapeDataString(code)}?lang={Uri.EscapeDataString(effectiveLang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            if (!string.IsNullOrEmpty(AuthHeader))
            {
                request.SetRequestHeader("Authorization", AuthHeader);
            }
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetFoodFactByCodeAsync {code}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var fact = JsonConvert.DeserializeObject<FoodFact>(raw);
                return (fact, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize FoodFact by code {code}: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(FoodFactProgressResponse Result, ApiErrorResponse Error)> MarkAsReadAsync(string codeOrId)
        {
            if (string.IsNullOrEmpty(codeOrId))
            {
                return (null, null);
            }

            string url = $"{ApiConfig.BaseUrl}/api/v1/food-facts/{Uri.EscapeDataString(codeOrId)}/read";

            using UnityWebRequest request = UnityWebRequest.Post(url, "", "application/json");
            if (!string.IsNullOrEmpty(AuthHeader))
            {
                request.SetRequestHeader("Authorization", AuthHeader);
            }
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] MarkAsReadAsync {codeOrId}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                Debug.Log($"[{GetType().Name}] MarkAsReadAsync {codeOrId} response: {raw}");
                var progress = JsonConvert.DeserializeObject<FoodFactProgressResponse>(raw);
                if (progress != null)
                {
                    SaveLocalReadFact(codeOrId, progress.foodFactCode, progress.foodFactId);
                }
                return (progress, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize FoodFactProgressResponse {codeOrId}: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(FoodFactProgressResponse[] Result, ApiErrorResponse Error)> GetUserProgressListAsync()
        {
            string auth = AuthHeader;
            if (string.IsNullOrEmpty(auth))
            {
                return (GetLocalProgressFallback(), new ApiErrorResponse { message = "Authentication required" });
            }

            string url = $"{ApiConfig.BaseUrl}/api/v1/food-facts/progress";
            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", auth);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string raw = request.downloadHandler.text;
                    var list = JsonConvert.DeserializeObject<FoodFactProgressResponse[]>(raw);
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            SaveLocalReadFact(item.foodFactCode, item.foodFactCode, item.foodFactId);
                        }
                        return (list, null);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{GetType().Name}] Failed to parse FoodFactProgress list: {ex.Message}");
                }
            }

            // Fallback to local storage (e.g. while endpoint is being added by backend partners)
            return (GetLocalProgressFallback(), null);
        }

        private HashSet<string> GetLocalReadFactCodes()
        {
            try
            {
                var list = _localStorageService?.GetValue<List<string>>(LocalReadFactsKey);
                if (list != null)
                {
                    return new HashSet<string>(list, StringComparer.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] Failed to read local read facts: {ex.Message}");
            }
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private void SaveLocalReadFact(string codeOrId, string code, string id)
        {
            try
            {
                var set = GetLocalReadFactCodes();
                if (!string.IsNullOrEmpty(codeOrId)) set.Add(codeOrId);
                if (!string.IsNullOrEmpty(code)) set.Add(code);
                if (!string.IsNullOrEmpty(id)) set.Add(id);
                _localStorageService?.SetValue(LocalReadFactsKey, set.ToList());
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] Failed to save local read fact: {ex.Message}");
            }
        }

        private FoodFactProgressResponse[] GetLocalProgressFallback()
        {
            var codes = GetLocalReadFactCodes();
            return codes.Select(c => new FoodFactProgressResponse
            {
                foodFactCode = c,
                foodFactId = c,
                readAt = DateTime.UtcNow.ToString("o")
            }).ToArray();
        }
    }
}
