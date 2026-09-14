using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class MissionService : IMissionService
    {
        private readonly IStoreService _storeService;

        public MissionService(IStoreService storeService)
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

        public async Task<(Mission[] Result, ApiErrorResponse Error)> GetMissionsAsync(
            MissionFilterParams filter = null,
            string lang = null)
        {
            string effectiveLang = ResolveLang(lang ?? filter?.lang);
            var sb = new StringBuilder($"{ApiConfig.BaseUrl}/api/v1/missions?lang={Uri.EscapeDataString(effectiveLang)}");

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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetMissionsAsync"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var missions = JsonConvert.DeserializeObject<Mission[]>(raw);
                return (missions, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize Missions: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(Mission Result, ApiErrorResponse Error)> GetMissionAsync(
            string codeOrId,
            string lang = null)
        {
            if (string.IsNullOrEmpty(codeOrId))
                return (null, null);

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/missions/{Uri.EscapeDataString(codeOrId)}?lang={Uri.EscapeDataString(effectiveLang)}";

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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetMissionAsync {codeOrId}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var mission = JsonConvert.DeserializeObject<Mission>(raw);
                return (mission, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize Mission {codeOrId}: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(MissionProgress[] Result, ApiErrorResponse Error)> GetUserProgressListAsync(string lang = null)
        {
            string auth = AuthHeader;
            if (string.IsNullOrEmpty(auth))
                return (null, new ApiErrorResponse { message = "Authentication required" });

            string effectiveLang = ResolveLang(lang);
            string url = $"{ApiConfig.BaseUrl}/api/v1/missions/progress?lang={Uri.EscapeDataString(effectiveLang)}";

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
                var progressList = JsonConvert.DeserializeObject<MissionProgress[]>(raw);
                return (progressList, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize user mission progress list: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(MissionProgress Result, ApiErrorResponse Error)> GetMissionProgressAsync(
            string codeOrId,
            string lang = null)
        {
            if (string.IsNullOrEmpty(codeOrId))
                return (null, null);

            string auth = AuthHeader;
            if (string.IsNullOrEmpty(auth))
                return (null, new ApiErrorResponse { message = "Authentication required" });

            string effectiveLang = ResolveLang(lang);
            bool isUuid = Guid.TryParse(codeOrId, out _);
            string pathSegment = isUuid ? Uri.EscapeDataString(codeOrId) : $"by-code/{Uri.EscapeDataString(codeOrId)}";
            string url = $"{ApiConfig.BaseUrl}/api/v1/missions/{pathSegment}/progress?lang={Uri.EscapeDataString(effectiveLang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", auth);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetMissionProgressAsync {codeOrId}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var progress = JsonConvert.DeserializeObject<MissionProgress>(raw);
                return (progress, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize MissionProgress {codeOrId}: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(MissionProgress Result, ApiErrorResponse Error)> UpdateMissionProgressAsync(
            string codeOrId,
            bool? completed,
            float? progress,
            string lang = null)
        {
            if (string.IsNullOrEmpty(codeOrId))
                return (null, new ApiErrorResponse { message = "Mission code or id is required" });

            string auth = AuthHeader;
            if (string.IsNullOrEmpty(auth))
                return (null, new ApiErrorResponse { message = "Authentication required" });

            string effectiveLang = ResolveLang(lang);
            bool isUuid = Guid.TryParse(codeOrId, out _);
            string pathSegment = isUuid ? Uri.EscapeDataString(codeOrId) : $"by-code/{Uri.EscapeDataString(codeOrId)}";
            string url = $"{ApiConfig.BaseUrl}/api/v1/missions/{pathSegment}/progress?lang={Uri.EscapeDataString(effectiveLang)}";

            var reqBody = new UpdateMissionProgressRequest
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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] UpdateMissionProgressAsync {codeOrId}"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var updatedProgress = JsonConvert.DeserializeObject<MissionProgress>(raw);
                return (updatedProgress, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize MissionProgress after update: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }
    }
}
