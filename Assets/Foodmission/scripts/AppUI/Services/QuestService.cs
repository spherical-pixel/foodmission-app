using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class QuestService : IQuestService
    {
        private readonly IStoreService _storeService;

        public QuestService(IStoreService storeService)
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

        public async Task<(Quest[] Result, ApiErrorResponse Error)> GetQuestsAsync(
            string dimensionCode = null,
            string level = null,
            string lang = null)
        {
            string effectiveLang = ResolveLang(lang);
            var sb = new StringBuilder($"{ApiConfig.BaseUrl}/api/v1/quests?lang={Uri.EscapeDataString(effectiveLang)}");

            if (!string.IsNullOrEmpty(dimensionCode))
                sb.Append($"&dimensionCode={Uri.EscapeDataString(dimensionCode)}");
            if (!string.IsNullOrEmpty(level))
                sb.Append($"&level={Uri.EscapeDataString(level)}");

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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetQuestsAsync"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var quests = JsonConvert.DeserializeObject<Quest[]>(raw);
                return (quests, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize Quests: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(Quest Result, ApiErrorResponse Error)> GetQuestAsync(
            string codeOrId,
            string lang = null)
        {
            if (string.IsNullOrEmpty(codeOrId))
                return (null, null);

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/quests/{Uri.EscapeDataString(codeOrId)}?lang={Uri.EscapeDataString(effectiveLang)}";

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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetQuestAsync {codeOrId}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var quest = JsonConvert.DeserializeObject<Quest>(raw);
                return (quest, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize Quest {codeOrId}: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(QuestProgress[] Result, ApiErrorResponse Error)> GetUserProgressListAsync(string lang = null)
        {
            string auth = AuthHeader;
            if (string.IsNullOrEmpty(auth))
                return (null, new ApiErrorResponse { message = "Authentication required" });

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/quests/progress?lang={Uri.EscapeDataString(effectiveLang)}";

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
                var progressList = JsonConvert.DeserializeObject<QuestProgress[]>(raw);
                return (progressList, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize user quest progress list: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(QuestProgress Result, ApiErrorResponse Error)> GetQuestProgressAsync(
            string codeOrId,
            string lang = null)
        {
            if (string.IsNullOrEmpty(codeOrId))
                return (null, null);

            string auth = AuthHeader;
            if (string.IsNullOrEmpty(auth))
                return (null, new ApiErrorResponse { message = "Authentication required" });

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/quests/{Uri.EscapeDataString(codeOrId)}/progress?lang={Uri.EscapeDataString(effectiveLang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", auth);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetQuestProgressAsync {codeOrId}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var progress = JsonConvert.DeserializeObject<QuestProgress>(raw);
                return (progress, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize QuestProgress {codeOrId}: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(QuestProgress Result, ApiErrorResponse Error)> UpdateQuestProgressAsync(
            string codeOrId,
            bool? completed,
            float? progressPercent,
            string lang = null)
        {
            if (string.IsNullOrEmpty(codeOrId))
                return (null, new ApiErrorResponse { message = "Quest code or id is required" });

            string auth = AuthHeader;
            if (string.IsNullOrEmpty(auth))
                return (null, new ApiErrorResponse { message = "Authentication required" });

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/quests/{Uri.EscapeDataString(codeOrId)}/progress?lang={Uri.EscapeDataString(effectiveLang)}";

            var reqBody = new UpdateQuestProgressRequest
            {
                completed = completed,
                progressPercent = progressPercent
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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] UpdateQuestProgressAsync {codeOrId}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var progress = JsonConvert.DeserializeObject<QuestProgress>(raw);
                return (progress, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize QuestProgress after update: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }
    }
}
