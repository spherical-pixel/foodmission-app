using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class GamificationService : IGamificationService
    {
        private readonly IStoreService _storeService;

        public GamificationService(IStoreService storeService)
        {
            _storeService = storeService;
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

        public async Task<(WalletBalance Result, ApiErrorResponse Error)> GetWalletBalanceAsync()
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/users/me/wallet";
            using UnityWebRequest request = UnityWebRequest.Get(url);

            if (!string.IsNullOrEmpty(AuthHeader))
            {
                request.SetRequestHeader("Authorization", AuthHeader);
            }
            request.SetRequestHeader("Accept", "application/json");

            await request.SendAsync();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetWalletBalanceAsync"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var balance = JsonConvert.DeserializeObject<WalletBalance>(raw);
                return (balance, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize WalletBalance: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(UserEarnedRewardsResponse Result, ApiErrorResponse Error)> GetEarnedRewardsAsync()
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/users/me/rewards";
            using UnityWebRequest request = UnityWebRequest.Get(url);

            if (!string.IsNullOrEmpty(AuthHeader))
            {
                request.SetRequestHeader("Authorization", AuthHeader);
            }
            request.SetRequestHeader("Accept", "application/json");

            await request.SendAsync();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetEarnedRewardsAsync"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var rewards = JsonConvert.DeserializeObject<UserEarnedRewardsResponse>(raw);
                return (rewards, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize UserEarnedRewardsResponse: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        // Backend maximum for eventsLimit (GamificationProfileQueryDto @Max(100))
        private const int MaxEventsLimit = 100;

        public Task<(GamificationProfileResponse Result, ApiErrorResponse Error)> GetGamificationProfileAsync(int eventsLimit = 20, int walletEntriesLimit = 20) =>
            FetchProfileAsync(ProfileUrl(ApiConfig.BaseUrl, eventsLimit, walletEntriesLimit, null), nameof(GetGamificationProfileAsync));

        public async Task<(GamificationProfileResponse Result, ApiErrorResponse Error)> GetGamificationProfileAsync(int eventsLimit, int walletEntriesLimit,
            IReadOnlyCollection<string> eventTypes)
        {
            var (profile, error) = await FetchProfileAsync(ProfileUrl(ApiConfig.BaseUrl, eventsLimit, walletEntriesLimit, eventTypes), nameof(GetGamificationProfileAsync));
            if (error == null || error.statusCode != 400 || eventTypes == null || eventTypes.Count == 0)
            {
                return (profile, error);
            }

            // Backends without the eventTypes filter (#419) answer 400 to the unknown parameter: filter the latest events here
            Debug.LogWarning($"[{GetType().Name}] eventTypes filter not supported by the backend, filtering recent events locally");
            var (unfiltered, unfilteredError) = await FetchProfileAsync(ProfileUrl(ApiConfig.BaseUrl, MaxEventsLimit, walletEntriesLimit, null), nameof(GetGamificationProfileAsync));
            return unfilteredError != null ? (null, unfilteredError) : (KeepEventsOfTypes(unfiltered, eventTypes, eventsLimit), null);
        }

        public static string ProfileUrl(string baseUrl, int eventsLimit, int walletEntriesLimit, IReadOnlyCollection<string> eventTypes)
        {
            string url = $"{baseUrl}/api/v1/users/me/gamification?eventsLimit={eventsLimit}&walletEntriesLimit={walletEntriesLimit}";
            return eventTypes == null || eventTypes.Count == 0 ? url : $"{url}&eventTypes={Uri.EscapeDataString(string.Join(",", eventTypes))}";
        }

        /// <summary>Keeps only events of these types (at most <paramref name="limit"/>); wallet, badges and the rest stay as they are.</summary>
        public static GamificationProfileResponse KeepEventsOfTypes(GamificationProfileResponse profile, IReadOnlyCollection<string> eventTypes, int limit)
        {
            if (profile == null)
            {
                return null;
            }
            var wanted = new HashSet<string>(eventTypes ?? Array.Empty<string>(), StringComparer.Ordinal);
            profile.recentEvents = (profile.recentEvents ?? Array.Empty<UserEvent>())
                .Where(e => e != null && wanted.Contains(e.eventType))
                .Take(limit)
                .ToArray();
            return profile;
        }

        public async Task<(UserEvent[] Result, ApiErrorResponse Error)> GetRecentEventsAsync(IReadOnlyCollection<string> eventTypes, int limit)
        {
            var (profile, error) = await FetchProfileAsync(RecentEventsUrl(ApiConfig.BaseUrl, eventTypes, limit), nameof(GetRecentEventsAsync));
            return error != null ? (null, error) : (profile?.recentEvents ?? Array.Empty<UserEvent>(), null);
        }

        public static string RecentEventsUrl(string baseUrl, IReadOnlyCollection<string> eventTypes, int limit) =>
            ProfileUrl(baseUrl, limit, 1, eventTypes);

        private async Task<(GamificationProfileResponse Result, ApiErrorResponse Error)> FetchProfileAsync(string url, string caller)
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);

            if (!string.IsNullOrEmpty(AuthHeader))
            {
                request.SetRequestHeader("Authorization", AuthHeader);
            }
            request.SetRequestHeader("Accept", "application/json");

            await request.SendAsync();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] {caller}"));
            }

            try
            {
                return (JsonConvert.DeserializeObject<GamificationProfileResponse>(request.downloadHandler.text), null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize GamificationProfileResponse: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }
    
        public static string BuildOnboardingSurveyUrl(string baseUrl)
            => $"{baseUrl}/api/v1/users/me/gamification/onboarding-survey";

        public async Task<(OnboardingSurveyQuestion[] Result, ApiErrorResponse Error)> GetOnboardingSurveyAsync()
        {
            using UnityWebRequest request = UnityWebRequest.Get(BuildOnboardingSurveyUrl(ApiConfig.BaseUrl));
            if (!string.IsNullOrEmpty(AuthHeader))
            {
                request.SetRequestHeader("Authorization", AuthHeader);
            }
            request.SetRequestHeader("Accept", "application/json");
            string lang = _storeService?.GetAppState()?.lang;
            if (!string.IsNullOrEmpty(lang))
            {
                request.SetRequestHeader("Accept-Language", lang);
            }

            await request.SendAsync();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetOnboardingSurveyAsync"));
            }

            try
            {
                return (ParseOnboardingSurvey(request.downloadHandler.text), null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize OnboardingSurveyResponse: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        /// <summary>Questions with a field and at least one option with a value, in backend order.</summary>
        public static OnboardingSurveyQuestion[] ParseOnboardingSurvey(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new OnboardingSurveyQuestion[0];
            }

            var response = JsonConvert.DeserializeObject<OnboardingSurveyResponse>(json);
            var questions = new List<OnboardingSurveyQuestion>();
            foreach (OnboardingSurveyQuestion question in response?.questions ?? new OnboardingSurveyQuestion[0])
            {
                if (question == null || string.IsNullOrEmpty(question.field))
                {
                    continue;
                }

                question.options = (question.options ?? new OnboardingSurveyOption[0])
                    .Where(o => o != null && !string.IsNullOrEmpty(o.value))
                    .ToArray();
                if (question.options.Length > 0)
                {
                    questions.Add(question);
                }
            }
            return questions.ToArray();
        }

        public async Task<(OnboardingSurveyResult Result, ApiErrorResponse Error)> SubmitOnboardingSurveyAsync(OnboardingSurveyData answers)
        {
            string url = BuildOnboardingSurveyUrl(ApiConfig.BaseUrl);
            byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(answers ?? new OnboardingSurveyData()));

            using UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            if (!string.IsNullOrEmpty(AuthHeader))
            {
                request.SetRequestHeader("Authorization", AuthHeader);
            }
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            await request.SendAsync();

            if (request.result != UnityWebRequest.Result.Success)
            {
                // 409 (already onboarded) is an expected outcome; the caller falls back to PATCH.
                bool alreadySubmitted = request.responseCode == 409;
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] SubmitOnboardingSurveyAsync", logAsError: !alreadySubmitted));
            }

            try
            {
                var result = JsonConvert.DeserializeObject<OnboardingSurveyResult>(request.downloadHandler.text);
                return (result, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize OnboardingSurveyResult: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public static string BuildDimensionsUrl(string baseUrl)
            => $"{baseUrl}/api/v1/users/me/gamification/dimensions";

        public async Task<(DimensionLevelEntry[] Result, ApiErrorResponse Error)> GetDimensionLevelsAsync()
        {
            using UnityWebRequest request = UnityWebRequest.Get(BuildDimensionsUrl(ApiConfig.BaseUrl));
            if (!string.IsNullOrEmpty(AuthHeader))
            {
                request.SetRequestHeader("Authorization", AuthHeader);
            }
            request.SetRequestHeader("Accept", "application/json");

            await request.SendAsync();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetDimensionLevelsAsync"));
            }

            try
            {
                return (ParseDimensionLevels(request.downloadHandler.text), null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize dimension levels: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        /// <summary>`[{ dimensionCode, level, … }]`: the JSON keys match DimensionLevelEntry; quests and names are ignored.</summary>
        public static DimensionLevelEntry[] ParseDimensionLevels(string json)
        {
            return string.IsNullOrEmpty(json)
                ? new DimensionLevelEntry[0]
                : JsonConvert.DeserializeObject<DimensionLevelEntry[]>(json) ?? new DimensionLevelEntry[0];
        }

        public static string BuildProgressWheelsUrl(string baseUrl)
            => $"{baseUrl}/api/v1/users/me/gamification/progress-wheels";

        public async Task<(ProgressWheel[] Result, ApiErrorResponse Error)> GetProgressWheelsAsync()
        {
            using UnityWebRequest request = UnityWebRequest.Get(BuildProgressWheelsUrl(ApiConfig.BaseUrl));
            if (!string.IsNullOrEmpty(AuthHeader))
            {
                request.SetRequestHeader("Authorization", AuthHeader);
            }
            request.SetRequestHeader("Accept", "application/json");

            await request.SendAsync();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetProgressWheelsAsync", logAsError: false));
            }

            try
            {
                var wheels = JsonConvert.DeserializeObject<ProgressWheel[]>(request.downloadHandler.text);
                return (wheels ?? new ProgressWheel[0], null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize ProgressWheel[]: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public static string BuildKnowledgeProgressUrl(string baseUrl)
            => $"{baseUrl}/api/v1/users/me/gamification/knowledge-progress";

        /// <summary>The backend translates item titles only from ?lang= (Accept-Language is ignored).</summary>
        public static string BuildKnowledgeBarUrl(string baseUrl, string kind, string lang)
        {
            string url = $"{BuildKnowledgeProgressUrl(baseUrl)}/{UnityWebRequest.EscapeURL(kind ?? "")}";
            return string.IsNullOrEmpty(lang) ? url : $"{url}?lang={Uri.EscapeDataString(lang)}";
        }

        public static KnowledgeProgress[] ParseKnowledgeProgress(string json)
        {
            return string.IsNullOrEmpty(json)
                ? new KnowledgeProgress[0]
                : JsonConvert.DeserializeObject<KnowledgeProgress[]>(json) ?? new KnowledgeProgress[0];
        }

        public static KnowledgeProgress ParseKnowledgeBar(string json)
        {
            return string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<KnowledgeProgress>(json);
        }

        public async Task<(KnowledgeProgress[] Result, ApiErrorResponse Error)> GetKnowledgeProgressAsync()
        {
            var (json, error) = await GetLocalizedJsonAsync(BuildKnowledgeProgressUrl(ApiConfig.BaseUrl), nameof(GetKnowledgeProgressAsync));
            if (error != null)
            {
                return (null, error);
            }
            try
            {
                return (ParseKnowledgeProgress(json), null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize KnowledgeProgress[]: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(KnowledgeProgress Result, ApiErrorResponse Error)> GetKnowledgeBarAsync(string kind)
        {
            var (json, error) = await GetLocalizedJsonAsync(BuildKnowledgeBarUrl(ApiConfig.BaseUrl, kind, _storeService?.GetAppState()?.lang), nameof(GetKnowledgeBarAsync));
            if (error != null)
            {
                return (null, error);
            }
            try
            {
                return (ParseKnowledgeBar(json), null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize KnowledgeProgress: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public static string BuildCurrentQuestUrl(string baseUrl, string lang)
        {
            string url = $"{baseUrl}/api/v1/users/me/gamification/current-quest";
            return string.IsNullOrEmpty(lang) ? url : $"{url}?lang={Uri.EscapeDataString(lang)}";
        }

        /// <summary>The backend answers with an empty/null body when the user has no current quest.</summary>
        public static CurrentQuest ParseCurrentQuest(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? null : JsonConvert.DeserializeObject<CurrentQuest>(json);
        }

        public async Task<(CurrentQuest Result, ApiErrorResponse Error)> GetCurrentQuestAsync()
        {
            var (json, error) = await GetLocalizedJsonAsync(BuildCurrentQuestUrl(ApiConfig.BaseUrl, _storeService?.GetAppState()?.lang), nameof(GetCurrentQuestAsync));
            if (error != null)
            {
                return (null, error);
            }
            try
            {
                return (ParseCurrentQuest(json), null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize CurrentQuest: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        /// <summary>Authenticated GET with Accept-Language = app language (the backend localizes titles when it can).</summary>
        private async Task<(string Json, ApiErrorResponse Error)> GetLocalizedJsonAsync(string url, string caller)
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);
            if (!string.IsNullOrEmpty(AuthHeader))
            {
                request.SetRequestHeader("Authorization", AuthHeader);
            }
            request.SetRequestHeader("Accept", "application/json");
            string lang = _storeService?.GetAppState()?.lang;
            if (!string.IsNullOrEmpty(lang))
            {
                request.SetRequestHeader("Accept-Language", lang);
            }

            await request.SendAsync();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] {caller}", logAsError: false));
            }
            return (request.downloadHandler.text, null);
        }
    }
}
