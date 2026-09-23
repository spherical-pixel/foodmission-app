using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class FoodyService : IFoodyService
    {
        private readonly IStoreService _storeService;

        public FoodyService(IStoreService storeService)
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

        public async Task<(List<FoodyItem> Result, ApiErrorResponse Error)> GetItemsAsync(string type = null, bool? ownedOnly = null)
        {
            var sb = new StringBuilder($"{ApiConfig.BaseUrl}/api/v1/foody/items");
            bool hasParam = false;

            if (!string.IsNullOrEmpty(type))
            {
                sb.Append(hasParam ? "&" : "?");
                sb.Append($"type={Uri.EscapeDataString(FoodyItemType.Normalize(type))}");
                hasParam = true;
            }

            if (ownedOnly.HasValue)
            {
                sb.Append(hasParam ? "&" : "?");
                sb.Append($"ownedOnly={ownedOnly.Value.ToString().ToLowerInvariant()}");
                hasParam = true;
            }

            string url = sb.ToString();

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetItemsAsync"));
            }

            try
            {
                var items = JsonConvert.DeserializeObject<List<FoodyItem>>(request.downloadHandler.text);
                return (items ?? new List<FoodyItem>(), null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error deserializing Foody items: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(FoodyLoadout Result, ApiErrorResponse Error)> GetLoadoutAsync()
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/foody/loadout";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetLoadoutAsync"));
            }

            try
            {
                var loadout = JsonConvert.DeserializeObject<FoodyLoadout>(request.downloadHandler.text);
                return (loadout ?? new FoodyLoadout(), null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error deserializing Foody loadout: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(FoodyPurchaseResponse Result, ApiErrorResponse Error)> PurchaseItemAsync(string codeOrId)
        {
            if (string.IsNullOrEmpty(codeOrId))
            {
                return (null, new ApiErrorResponse { message = "Item code or ID is required" });
            }

            string url = $"{ApiConfig.BaseUrl}/api/v1/foody/items/{Uri.EscapeDataString(codeOrId)}/purchase";

            using UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(new byte[0]);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] PurchaseItemAsync {codeOrId}"));
            }

            try
            {
                var purchaseResponse = JsonConvert.DeserializeObject<FoodyPurchaseResponse>(request.downloadHandler.text);
                if (purchaseResponse != null && _storeService?.store != null)
                {
                    int currentXp = _storeService.GetAppState()?.userXp ?? 0;
                    _storeService.store.Dispatch(AppActions.setWalletBalance.Invoke(new AppActions.WalletPayload(currentXp, purchaseResponse.pointsBalance)));
                }
                return (purchaseResponse, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error deserializing purchase response: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(FoodyItem Result, ApiErrorResponse Error)> EquipItemAsync(string codeOrId)
        {
            if (string.IsNullOrEmpty(codeOrId))
            {
                return (null, new ApiErrorResponse { message = "Item code or ID is required" });
            }

            string url = $"{ApiConfig.BaseUrl}/api/v1/foody/items/{Uri.EscapeDataString(codeOrId)}/equip";

            using UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(new byte[0]);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] EquipItemAsync {codeOrId}"));
            }

            try
            {
                var item = JsonConvert.DeserializeObject<FoodyItem>(request.downloadHandler.text);
                return (item, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error deserializing equipped item: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(FoodyItem Result, ApiErrorResponse Error)> UnequipItemAsync(string codeOrId)
        {
            if (string.IsNullOrEmpty(codeOrId))
            {
                return (null, new ApiErrorResponse { message = "Item code or ID is required" });
            }

            string url = $"{ApiConfig.BaseUrl}/api/v1/foody/items/{Uri.EscapeDataString(codeOrId)}/equip";

            using UnityWebRequest request = UnityWebRequest.Delete(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] UnequipItemAsync {codeOrId}"));
            }

            try
            {
                var item = JsonConvert.DeserializeObject<FoodyItem>(request.downloadHandler.text);
                return (item, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Error deserializing unequipped item: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }
    }
}
