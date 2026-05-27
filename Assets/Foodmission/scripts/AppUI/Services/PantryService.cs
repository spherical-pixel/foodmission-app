using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class PantryService : IPantryService
    {
        private readonly IStoreService _storeService;
        private string _pantryId;

        public PantryService(IStoreService storeService)
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

        private async Task<(string PantryId, ApiErrorResponse Error)> EnsurePantryIdAsync()
        {
            if (!string.IsNullOrEmpty(_pantryId))
            {
                return (_pantryId, null);
            }

            var (pantry, error) = await GetPantryAsync();
            if (error != null) return (null, error);
            return (pantry?.id, null);
        }

        // ── Pantry ─────────────────────────────────────────────────────────

        public async Task<(Pantry Result, ApiErrorResponse Error)> GetPantryAsync()
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/pantry";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetPantry"));
            }

            Pantry pantry = JsonUtility.FromJson<Pantry>(request.downloadHandler.text);

            if (pantry?.id != null)
            {
                _pantryId = pantry.id;
            }

            return (pantry, null);
        }

        // ── Items ──────────────────────────────────────────────────────────

        public async Task<(PantryItem[] Result, ApiErrorResponse Error)> GetItemsAsync()
        {
            var (pantryId, error) = await EnsurePantryIdAsync();
            if (error != null) return (null, error);

            if (string.IsNullOrEmpty(pantryId))
            {
                Debug.LogWarning($"[{GetType().Name}] GetItems — pantryId unavailable");
                return (null, null);
            }

            string url = $"{ApiConfig.BaseUrl}/api/v1/pantry/{Uri.EscapeDataString(pantryId)}/items";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetItems"));
            }

            PantryItemListResponse response = JsonUtility.FromJson<PantryItemListResponse>(request.downloadHandler.text);
            return (response?.data, null);
        }

        public async Task<(PantryItem Result, ApiErrorResponse Error)> GetItemAsync(string itemId)
        {
            var (pantryId, error) = await EnsurePantryIdAsync();
            if (error != null) return (null, error);

            if (string.IsNullOrEmpty(pantryId))
            {
                Debug.LogWarning($"[{GetType().Name}] GetItems — pantryId unavailable");
                return (null, null);
            }

            string url = $"{ApiConfig.BaseUrl}/api/v1/pantry/{Uri.EscapeDataString(pantryId)}/items/{Uri.EscapeDataString(itemId)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetItem {itemId}"));
            }

            return (JsonUtility.FromJson<PantryItem>(request.downloadHandler.text), null);
        }

        public async Task<(PantryItem Result, ApiErrorResponse Error)> AddItemAsync(
            string foodProductId,
            string genericFoodId,
            float quantity,
            string unit = "PIECES",
            string notes = null,
            string location = null,
            string expiryDate = null)
        {
            var (pantryId, error) = await EnsurePantryIdAsync();
            if (error != null) return (null, error);

            if (string.IsNullOrEmpty(pantryId))
            {
                Debug.LogWarning($"[{GetType().Name}] GetItems — pantryId unavailable");
                return (null, null);
            }

            string effectiveExpiryDate = !string.IsNullOrEmpty(expiryDate)
                ? expiryDate
                : DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");

            AddPantryItemRequest body = new()
            {
                foodProductId = foodProductId,
                genericFoodId = genericFoodId,
                quantity = quantity,
                unit = unit ?? "PIECES",
                notes = notes,
                location = location,
                expiryDate = effectiveExpiryDate
            };

            string url = $"{ApiConfig.BaseUrl}/api/v1/pantry/{Uri.EscapeDataString(pantryId)}/items";

            byte[] bodyJson = body.ToJsonBody();

            using UnityWebRequest request = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(bodyJson) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            Debug.Log($"[{GetType().Name}] AddItemAsync: Sending request to {url} with body: {Encoding.UTF8.GetString(bodyJson)}");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] AddItem"));
            }

            return (JsonUtility.FromJson<PantryItem>(request.downloadHandler.text), null);
        }

        public async Task<(PantryItem Result, ApiErrorResponse Error)> UpdateItemAsync(
            string itemId,
            float? quantity,
            string unit,
            string notes,
            string location,
            string expiryDate,
            string foodProductId = null,
            string genericFoodId = null)
        {
            var (pantryId, error) = await EnsurePantryIdAsync();
            if (error != null) return (null, error);

            if (string.IsNullOrEmpty(pantryId))
            {
                Debug.LogWarning($"[{GetType().Name}] GetItems — pantryId unavailable");
                return (null, null);
            }

            UpdatePantryItemRequest body = new()
            {
                quantity = quantity,
                unit = unit,
                notes = notes,
                location = location,
                expiryDate = expiryDate,
                foodProductId = foodProductId,
                genericFoodId = genericFoodId
            };

            string url = $"{ApiConfig.BaseUrl}/api/v1/pantry/{Uri.EscapeDataString(pantryId)}/items/{Uri.EscapeDataString(itemId)}";

            using UnityWebRequest request = new UnityWebRequest(url, "PATCH")
            {
                uploadHandler = new UploadHandlerRaw(body.ToJsonBody()) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] UpdateItem {itemId}"));
            }

            return (JsonUtility.FromJson<PantryItem>(request.downloadHandler.text), null);
        }

        public async Task<(ExpiredPantryItem[] Result, ApiErrorResponse Error)> GetExpiredItemsAsync()
        {
            var (pantryId, error) = await EnsurePantryIdAsync();
            if (error != null) return (null, error);
            if (string.IsNullOrEmpty(pantryId)) return (null, null);

            string url = $"{ApiConfig.BaseUrl}/api/v1/pantry/{Uri.EscapeDataString(pantryId)}/items/expired";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetExpiredItemsAsync"));
            }

            ExpiredPantryItemArrayWrapper wrapper = JsonUtility.FromJson<ExpiredPantryItemArrayWrapper>(
                "{\"items\":" + request.downloadHandler.text + "}");
            return (wrapper?.items, null);
        }

        public async Task<(BatchWasteResult Result, ApiErrorResponse Error)> BatchWasteAsync(BatchWasteRequest request)
        {
            if (request?.items == null || request.items.Length == 0) return (null, null);

            var (pantryId, error) = await EnsurePantryIdAsync();
            if (error != null) return (null, error);
            if (string.IsNullOrEmpty(pantryId)) return (null, null);

            string url = $"{ApiConfig.BaseUrl}/api/v1/pantry/{Uri.EscapeDataString(pantryId)}/items/batch-waste";

            using UnityWebRequest req = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(request.ToJsonBody()) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Authorization", AuthHeader);
            req.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(req, $"[{GetType().Name}] BatchWasteAsync"));
            }

            return (JsonUtility.FromJson<BatchWasteResult>(req.downloadHandler.text), null);
        }

        public async Task<(bool Success, ApiErrorResponse Error)> DeleteItemAsync(string itemId)
        {
            var (pantryId, error) = await EnsurePantryIdAsync();
            if (error != null) return (false, error);

            if (string.IsNullOrEmpty(pantryId))
            {
                Debug.LogWarning($"[{GetType().Name}] GetItems — pantryId unavailable");
                return (false, null);
            }

            string url = $"{ApiConfig.BaseUrl}/api/v1/pantry/{Uri.EscapeDataString(pantryId)}/items/{Uri.EscapeDataString(itemId)}";

            using UnityWebRequest request = new UnityWebRequest(url, "DELETE")
            {
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Authorization", AuthHeader);

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (false, ApiErrorHelper.Parse(request, $"[{GetType().Name}] DeleteItem {itemId}"));
            }

            return (true, null);
        }

        // ── Request DTOs ───────────────────────────────────────────────────

        [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
        private class AddPantryItemRequest
        {
            [JsonProperty("foodProductId")]
            public string foodProductId;

            [JsonProperty("genericFoodId")]
            public string genericFoodId;

            [JsonProperty("quantity")]
            public float quantity;

            [JsonProperty("unit")]
            public string unit;

            [JsonProperty("notes")]
            public string notes;

            [JsonProperty("location")]
            public string location;

            [JsonProperty("expiryDate")]
            public string expiryDate;

            public byte[] ToJsonBody()
            {
                string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                return Encoding.UTF8.GetBytes(json);
            }
        }

        [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
        private class UpdatePantryItemRequest
        {
            [JsonProperty("quantity")]
            public float? quantity;

            [JsonProperty("unit")]
            public string unit;

            [JsonProperty("notes")]
            public string notes;

            [JsonProperty("location")]
            public string location;

            [JsonProperty("expiryDate")]
            public string expiryDate;

            [JsonProperty("foodProductId")]
            public string foodProductId;

            [JsonProperty("genericFoodId")]
            public string genericFoodId;

            public byte[] ToJsonBody()
            {
                string json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                return Encoding.UTF8.GetBytes(json);
            }
        }

    }
}
