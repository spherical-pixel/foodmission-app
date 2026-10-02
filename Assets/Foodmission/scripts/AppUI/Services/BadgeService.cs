using System;
using System.Threading.Tasks;

using Newtonsoft.Json;

using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class BadgeService : IBadgeService
    {
        private readonly IStoreService _storeService;

        public BadgeService(IStoreService storeService)
        {
            _storeService = storeService;
        }

        private string AuthHeader
        {
            get
            {
                AppState s = _storeService?.GetAppState();
                if (s == null || string.IsNullOrEmpty(s.accessToken))
                {
                    return string.Empty;
                }
                return $"{s.tokenType} {s.accessToken}";
            }
        }

        private string ResolveLang()
        {
            AppState s = _storeService?.GetAppState();
            if (!string.IsNullOrEmpty(s?.lang) && s.lang != "none")
            {
                return s.lang;
            }
            return "en";
        }

        public static string BuildMyBadgesUrl(string baseUrl, string lang)
        {
            return $"{baseUrl}/api/v1/badges/me?lang={Uri.EscapeDataString(lang)}";
        }

        public async Task<(UserBadgesResponse Result, ApiErrorResponse Error)> GetMyBadgesAsync()
        {
            string url = BuildMyBadgesUrl(ApiConfig.BaseUrl, ResolveLang());
            using UnityWebRequest request = UnityWebRequest.Get(url);

            if (!string.IsNullOrEmpty(AuthHeader))
            {
                request.SetRequestHeader("Authorization", AuthHeader);
            }
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetMyBadgesAsync"));
            }

            try
            {
                var response = JsonConvert.DeserializeObject<UserBadgesResponse>(request.downloadHandler.text);
                return (response, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize UserBadgesResponse: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }
    }
}
