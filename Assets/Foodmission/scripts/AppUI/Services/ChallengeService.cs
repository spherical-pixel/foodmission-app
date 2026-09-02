using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class ChallengeService : IChallengeService
    {
        private readonly IStoreService _storeService;

        public ChallengeService(IStoreService storeService)
        {
            _storeService = storeService;
        }

        private string AuthHeader
        {
            get
            {
                AppState s = _storeService?.GetAppState();
                if (s == null || string.IsNullOrEmpty(s.accessToken)) return string.Empty;
                return $"{s.tokenType} {s.accessToken}";
            }
        }

        private string ResolveLang(string lang)
        {
            if (!string.IsNullOrEmpty(lang))
                return lang;

            AppState s = _storeService?.GetAppState();
            if (!string.IsNullOrEmpty(s?.lang) && s.lang != "none")
                return s.lang;

            return "en";
        }

        public async Task<(Challenge[] Result, ApiErrorResponse Error)> GetChallengesAsync(
            ChallengeFilterParams filter = null,
            string lang = null)
        {
            string effectiveLang = ResolveLang(lang ?? filter?.lang);
            var sb = new StringBuilder($"{ApiConfig.BaseUrl}/api/v1/challenges?lang={Uri.EscapeDataString(effectiveLang)}");

            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.dimensionCode))
                    sb.Append($"&dimensionCode={Uri.EscapeDataString(filter.dimensionCode)}");
                if (!string.IsNullOrEmpty(filter.level))
                    sb.Append($"&level={Uri.EscapeDataString(filter.level)}");
                if (filter.available.HasValue)
                    sb.Append($"&available={filter.available.Value.ToString().ToLowerInvariant()}");
            }

            using UnityWebRequest request = UnityWebRequest.Get(sb.ToString());
            string auth = AuthHeader;
            if (!string.IsNullOrEmpty(auth))
            {
                request.SetRequestHeader("Authorization", auth);
            }
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetChallengesAsync"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var challenges = JsonConvert.DeserializeObject<Challenge[]>(raw);
                return (challenges, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize Challenges: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(Challenge Result, ApiErrorResponse Error)> GetChallengeAsync(
            string codeOrId,
            string lang = null)
        {
            if (string.IsNullOrEmpty(codeOrId))
                return (null, null);

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/challenges/{Uri.EscapeDataString(codeOrId)}?lang={Uri.EscapeDataString(effectiveLang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            string auth = AuthHeader;
            if (!string.IsNullOrEmpty(auth))
            {
                request.SetRequestHeader("Authorization", auth);
            }
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetChallengeAsync {codeOrId}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var challenge = JsonConvert.DeserializeObject<Challenge>(raw);
                return (challenge, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize Challenge {codeOrId}: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(ChallengeProgress[] Result, ApiErrorResponse Error)> GetUserProgressListAsync(string lang = null)
        {
            string auth = AuthHeader;
            if (string.IsNullOrEmpty(auth))
                return (null, new ApiErrorResponse { message = "Authentication required" });

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/challenges/progress?lang={Uri.EscapeDataString(effectiveLang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", auth);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetUserProgressListAsync"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var progressList = JsonConvert.DeserializeObject<ChallengeProgress[]>(raw);
                return (progressList, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize user challenge progress list: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(ChallengeProgress Result, ApiErrorResponse Error)> GetChallengeProgressAsync(
            string codeOrId,
            string lang = null)
        {
            if (string.IsNullOrEmpty(codeOrId))
                return (null, null);

            string auth = AuthHeader;
            if (string.IsNullOrEmpty(auth))
                return (null, new ApiErrorResponse { message = "Authentication required" });

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/challenges/{Uri.EscapeDataString(codeOrId)}/progress?lang={Uri.EscapeDataString(effectiveLang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", auth);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetChallengeProgressAsync {codeOrId}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var progress = JsonConvert.DeserializeObject<ChallengeProgress>(raw);
                return (progress, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize ChallengeProgress {codeOrId}: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(ChallengeProgress Result, ApiErrorResponse Error)> UpdateChallengeProgressAsync(
            string codeOrId,
            bool? completed,
            float? progress,
            string lang = null)
        {
            if (string.IsNullOrEmpty(codeOrId))
                return (null, new ApiErrorResponse { message = "Challenge code or id is required" });

            string auth = AuthHeader;
            if (string.IsNullOrEmpty(auth))
                return (null, new ApiErrorResponse { message = "Authentication required" });

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/challenges/{Uri.EscapeDataString(codeOrId)}/progress?lang={Uri.EscapeDataString(effectiveLang)}";

            var reqBody = new UpdateChallengeProgressRequest
            {
                completed = completed,
                progress = progress
            };
            byte[] bodyRaw = reqBody.ToJsonBody();

            using UnityWebRequest request = new UnityWebRequest(url, "PATCH")
            {
                uploadHandler = new UploadHandlerRaw(bodyRaw) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Authorization", auth);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] UpdateChallengeProgressAsync {codeOrId}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var updatedProgress = JsonConvert.DeserializeObject<ChallengeProgress>(raw);
                return (updatedProgress, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize ChallengeProgress after update: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }
    }
}
