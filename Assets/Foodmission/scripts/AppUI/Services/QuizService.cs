using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class QuizService : IQuizService
    {
        private readonly IStoreService _storeService;

        public QuizService(IStoreService storeService)
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

        private string ResolveLang(string lang)
        {
            if (!string.IsNullOrEmpty(lang))
                return lang;

            AppState s = _storeService.GetAppState();
            if (!string.IsNullOrEmpty(s.lang) && s.lang != "none")
                return s.lang;

            return "en";
        }

        public async Task<(PaginatedQuizResponse Result, ApiErrorResponse Error)> GetQuizzesAsync(
            QuizFilterParams filters = null,
            int page = 1,
            int limit = 10,
            string lang = null)
        {
            string effectiveLang = ResolveLang(lang);
            var sb = new StringBuilder($"{ApiConfig.BaseUrl}/api/v1/quizzes?page={page}&limit={limit}&lang={Uri.EscapeDataString(effectiveLang)}");

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
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetQuizzesAsync"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var response = JsonConvert.DeserializeObject<PaginatedQuizResponse>(raw);
                return (response, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize PaginatedQuizResponse: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(Quiz Result, ApiErrorResponse Error)> GetQuizAsync(
            string codeOrId,
            string lang = null)
        {
            if (string.IsNullOrEmpty(codeOrId))
            {
                return (null, null);
            }

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/quizzes/{Uri.EscapeDataString(codeOrId)}?lang={Uri.EscapeDataString(effectiveLang)}";

            Debug.Log("GetQuizAsync -> " + url);

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetQuizAsync {codeOrId}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var quiz = JsonConvert.DeserializeObject<Quiz>(raw);
                return (quiz, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize Quiz {codeOrId}: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(QuizProgress[] Result, ApiErrorResponse Error)> GetUserProgressListAsync(
            string lang = null)
        {
            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/quizzes/progress?lang={Uri.EscapeDataString(effectiveLang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetUserProgressListAsync"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var list = JsonConvert.DeserializeObject<QuizProgress[]>(raw);
                return (list, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize QuizProgress[]: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(QuizProgress Result, ApiErrorResponse Error)> GetQuizProgressAsync(
            string codeOrId,
            string lang = null)
        {
            if (string.IsNullOrEmpty(codeOrId))
                return (null, null);

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/quizzes/{Uri.EscapeDataString(codeOrId)}/progress?lang={Uri.EscapeDataString(effectiveLang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetQuizProgressAsync {codeOrId}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var progress = JsonConvert.DeserializeObject<QuizProgress>(raw);
                return (progress, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize QuizProgress {codeOrId}: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(QuizProgress Result, ApiErrorResponse Error)> SubmitQuizAnswerAsync(
            string codeOrId,
            string selectedLabel,
            string lang = null)
        {
            if (string.IsNullOrEmpty(codeOrId))
                return (null, new ApiErrorResponse { message = "Quiz code or id is required" });

            if (string.IsNullOrEmpty(selectedLabel))
                return (null, new ApiErrorResponse { message = "Selected option label is required" });

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/quizzes/{Uri.EscapeDataString(codeOrId)}/progress?lang={Uri.EscapeDataString(effectiveLang)}";

            var reqBody = new UpdateQuizProgressRequest
            {
                selectedLabel = selectedLabel.Trim().ToUpperInvariant()
            };
            byte[] bodyRaw = reqBody.ToJsonBody();

            using UnityWebRequest request = new UnityWebRequest(url, "PATCH")
            {
                uploadHandler = new UploadHandlerRaw(bodyRaw) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] SubmitQuizAnswerAsync {codeOrId}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var progress = JsonConvert.DeserializeObject<QuizProgress>(raw);
                return (progress, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize QuizProgress after answer: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }
    }
}
