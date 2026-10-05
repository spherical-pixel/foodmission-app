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
        private static readonly System.Collections.Generic.Dictionary<string, string> s_MissionIdToCodeMap = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly object s_MissionCacheLock = new object();

        private readonly IStoreService _storeService;

        public MissionService(IStoreService storeService)
        {
            _storeService = storeService;
        }

        public string GetCachedCode(string missionId)
        {
            if (string.IsNullOrEmpty(missionId)) return null;
            lock (s_MissionCacheLock)
            {
                if (s_MissionIdToCodeMap.TryGetValue(missionId, out var code))
                    return code;
            }
            return null;
        }

        /// <summary>
        /// The progress endpoints return <c>missionId</c> but no <c>missionCode</c>: fills the code where it is missing.
        /// Returns how many entries are still without a code.
        /// </summary>
        public static int FillMissionCodes(MissionProgress[] list, Func<string, string> codeForId)
        {
            int missing = 0;
            if (list == null)
            {
                return missing;
            }
            foreach (MissionProgress progress in list)
            {
                if (progress == null || !string.IsNullOrEmpty(progress.missionCode))
                {
                    continue;
                }
                progress.missionCode = codeForId?.Invoke(progress.missionId);
                if (string.IsNullOrEmpty(progress.missionCode))
                {
                    missing++;
                }
            }
            return missing;
        }

        private static void CacheMission(Mission mission)
        {
            if (mission == null || string.IsNullOrEmpty(mission.id) || string.IsNullOrEmpty(mission.code)) return;
            lock (s_MissionCacheLock)
            {
                s_MissionIdToCodeMap[mission.id] = mission.code;
            }
        }

        private static void CacheMissions(Mission[] missions)
        {
            if (missions == null) return;
            lock (s_MissionCacheLock)
            {
                foreach (var m in missions)
                {
                    if (m != null && !string.IsNullOrEmpty(m.id) && !string.IsNullOrEmpty(m.code))
                    {
                        s_MissionIdToCodeMap[m.id] = m.code;
                    }
                }
            }
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
                CacheMissions(missions);
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
                CacheMission(mission);
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
                if (FillMissionCodes(progressList, GetCachedCode) > 0)
                {
                    // Unknown ids: load the catalog once so callers can match progress by code
                    await GetMissionsAsync();
                    FillMissionCodes(progressList, GetCachedCode);
                }
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

            bool isUuid = Guid.TryParse(codeOrId, out _);
            string url = BuildProgressUrl(ApiConfig.BaseUrl, codeOrId, ResolveLang(lang));

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
                FillMissionCodes(new[] { progress }, id => isUuid ? GetCachedCode(id) : codeOrId);
                return (progress, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize MissionProgress {codeOrId}: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public Task<(MissionProgress Result, ApiErrorResponse Error)> UpdateMissionProgressAsync(
            string codeOrId,
            bool? completed,
            float? progress,
            string lang = null)
        {
            byte[] body = new UpdateMissionProgressRequest { completed = completed, progress = progress }.ToJsonBody();
            return SendProgressRequestAsync(codeOrId, lang, null, "PATCH", body, nameof(UpdateMissionProgressAsync));
        }

        public Task<(MissionProgress Result, ApiErrorResponse Error)> RestartMissionProgressAsync(string codeOrId, string lang = null)
        {
            return SendProgressRequestAsync(codeOrId, lang, "/restart", "POST", null, nameof(RestartMissionProgressAsync));
        }

        public Task<(MissionProgress Result, ApiErrorResponse Error)> FailMissionProgressAsync(string codeOrId, string lang = null)
        {
            byte[] body = new UpdateMissionProgressRequest { failed = true }.ToJsonBody();
            return SendProgressRequestAsync(codeOrId, lang, null, "PATCH", body, nameof(FailMissionProgressAsync));
        }

        public static string BuildProgressUrl(string baseUrl, string codeOrId, string lang, string suffix = null)
        {
            bool isUuid = Guid.TryParse(codeOrId, out _);
            string pathSegment = isUuid ? Uri.EscapeDataString(codeOrId) : $"by-code/{Uri.EscapeDataString(codeOrId)}";
            return $"{baseUrl}/api/v1/missions/{pathSegment}/progress{suffix}?lang={Uri.EscapeDataString(lang)}";
        }

        private async Task<(MissionProgress Result, ApiErrorResponse Error)> SendProgressRequestAsync(
            string codeOrId,
            string lang,
            string suffix,
            string method,
            byte[] body,
            string caller)
        {
            if (string.IsNullOrEmpty(codeOrId))
            {
                return (null, new ApiErrorResponse { message = "Mission code or id is required" });
            }

            string auth = AuthHeader;
            if (string.IsNullOrEmpty(auth))
            {
                return (null, new ApiErrorResponse { message = "Authentication required" });
            }

            string url = BuildProgressUrl(ApiConfig.BaseUrl, codeOrId, ResolveLang(lang), suffix);
            using UnityWebRequest request = new UnityWebRequest(url, method)
            {
                uploadHandler = new UploadHandlerRaw(body ?? Array.Empty<byte>()) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Authorization", auth);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] {caller} {codeOrId}"));
            }

            try
            {
                var progress = JsonConvert.DeserializeObject<MissionProgress>(request.downloadHandler.text);
                bool isUuid = Guid.TryParse(codeOrId, out _);
                FillMissionCodes(new[] { progress }, id => isUuid ? GetCachedCode(id) : codeOrId);
                return (progress, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize MissionProgress after {caller}: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }
    }
}
