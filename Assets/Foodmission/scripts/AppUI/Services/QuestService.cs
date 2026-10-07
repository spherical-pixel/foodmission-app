using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class QuestService : IQuestService
    {
        /// <summary>How long a fetched quest is reused: it is catalog content, and Home asks for it from several prompts at once.</summary>
        public static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

        private readonly IStoreService _storeService;
        private readonly Dictionary<string, (DateTime FetchedAt, Quest Quest)> _questCache = new Dictionary<string, (DateTime, Quest)>();
        private readonly Dictionary<string, Task<(Quest, ApiErrorResponse)>> _questRequests = new Dictionary<string, Task<(Quest, ApiErrorResponse)>>();

        /// <summary>Loads one quest (code or id, language) from the backend; replaceable in tests.</summary>
        public Func<string, string, Task<(Quest Result, ApiErrorResponse Error)>> Fetch { get; set; }
        public Func<DateTime> Clock { get; set; } = () => DateTime.UtcNow;

        public QuestService(IStoreService storeService)
        {
            _storeService = storeService;
            Fetch = FetchQuestAsync;
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
            string key = codeOrId + "|" + effectiveLang;
            if (_questCache.TryGetValue(key, out var cached) && Clock() - cached.FetchedAt < CacheDuration)
            {
                return (cached.Quest, null);
            }

            // Concurrent callers share one request: the backend throttles bursts on the same route (429)
            if (!_questRequests.TryGetValue(key, out Task<(Quest, ApiErrorResponse)> request))
            {
                request = Fetch(codeOrId, effectiveLang);
                _questRequests[key] = request;
            }

            (Quest Result, ApiErrorResponse Error) result;
            try
            {
                result = await request;
            }
            finally
            {
                if (_questRequests.TryGetValue(key, out var current) && current == request)
                {
                    _questRequests.Remove(key);
                }
            }

            if (result.Error == null && result.Result != null)
            {
                _questCache[key] = (Clock(), result.Result);
            }
            return result;
        }

        private async Task<(Quest Result, ApiErrorResponse Error)> FetchQuestAsync(string codeOrId, string effectiveLang)
        {
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
            float? progress,
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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] UpdateQuestProgressAsync {codeOrId}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var questProgress = JsonConvert.DeserializeObject<QuestProgress>(raw);
                return (questProgress, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize QuestProgress after update: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }
    }
}
