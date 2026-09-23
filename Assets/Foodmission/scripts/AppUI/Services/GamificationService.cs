using System;
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

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

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

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

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

        public async Task<(GamificationProfileResponse Result, ApiErrorResponse Error)> GetGamificationProfileAsync(int eventsLimit = 20, int walletEntriesLimit = 20)
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/users/me/gamification?eventsLimit={eventsLimit}&walletEntriesLimit={walletEntriesLimit}";
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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetGamificationProfileAsync"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                var profile = JsonConvert.DeserializeObject<GamificationProfileResponse>(raw);
                return (profile, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize GamificationProfileResponse: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }
    }
}
