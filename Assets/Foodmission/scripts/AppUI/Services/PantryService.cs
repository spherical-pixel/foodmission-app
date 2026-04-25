using System;
using System.Text;
using System.Threading.Tasks;

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

        private async Task<string> EnsurePantryIdAsync()
        {
            if (!string.IsNullOrEmpty(_pantryId))
            {
                return _pantryId;
            }

            Pantry pantry = await GetPantryAsync();
            return pantry?.id ?? string.Empty;
        }

        // ── Pantry ─────────────────────────────────────────────────────────

        public async Task<Pantry> GetPantryAsync()
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
                Debug.LogError($"[{GetType().Name}] GetPantry failed: {request.responseCode}");
                return null;
            }

            Pantry pantry = JsonUtility.FromJson<Pantry>(request.downloadHandler.text);

            if (pantry?.id != null)
            {
                _pantryId = pantry.id;
            }

            return pantry;
        }

        // ── Items ──────────────────────────────────────────────────────────

        public async Task<PantryItem[]> GetItemsAsync()
        {
            string pantryId = await EnsurePantryIdAsync();

            if (string.IsNullOrEmpty(pantryId))
            {
                Debug.LogWarning($"[{GetType().Name}] GetItems — pantryId unavailable");
                return null;
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
                Debug.LogError($"[{GetType().Name}] GetItems failed: {request.responseCode}");
                return null;
            }

            string json = request.downloadHandler.text;
            PantryItemArrayWrapper wrapper = JsonUtility.FromJson<PantryItemArrayWrapper>("{\"items\":" + json + "}");
            return wrapper?.items;
        }

        public async Task<PantryItem> GetItemAsync(string itemId)
        {
            string pantryId = await EnsurePantryIdAsync();

            if (string.IsNullOrEmpty(pantryId))
            {
                Debug.LogWarning($"[{GetType().Name}] GetItems — pantryId unavailable");
                return null;
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
                Debug.LogError($"[{GetType().Name}] GetItem {itemId} failed: {request.responseCode}");
                return null;
            }

            return JsonUtility.FromJson<PantryItem>(request.downloadHandler.text);
        }

        public async Task<PantryItem> AddItemAsync(
            string foodId,
            string foodCategoryId,
            float quantity,
            string unit = "PIECES",
            string notes = null,
            string location = null,
            string expiryDate = null)
        {
            string pantryId = await EnsurePantryIdAsync();

            if (string.IsNullOrEmpty(pantryId))
            {
                Debug.LogWarning($"[{GetType().Name}] GetItems — pantryId unavailable");
                return null;
            }

            var sb = new StringBuilder("{");

            if (!string.IsNullOrEmpty(foodId))
            {
                sb.AppendFormat("\"foodId\":\"{0}\"", EscapeJson(foodId));
            }
            else
            {
                sb.AppendFormat("\"foodCategoryId\":\"{0}\"", EscapeJson(foodCategoryId));
            }

            sb.AppendFormat(",\"quantity\":{0}", quantity.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            sb.AppendFormat(",\"unit\":\"{0}\"", unit ?? "PIECES");

            if (!string.IsNullOrEmpty(notes))
            {
                sb.AppendFormat(",\"notes\":\"{0}\"", EscapeJson(notes));
            }

            if (!string.IsNullOrEmpty(location))
            {
                sb.AppendFormat(",\"location\":\"{0}\"", EscapeJson(location));
            }

            if (!string.IsNullOrEmpty(expiryDate))
            {
                sb.AppendFormat(",\"expiryDate\":\"{0}\"", EscapeJson(expiryDate));
            }

            sb.Append("}");
            byte[] body = Encoding.UTF8.GetBytes(sb.ToString());

            string url = $"{ApiConfig.BaseUrl}/api/v1/pantry/{Uri.EscapeDataString(pantryId)}/items";

            using UnityWebRequest request = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" },
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
                Debug.LogError($"[{GetType().Name}] AddItem failed: {request.responseCode} — {request.downloadHandler?.text}");
                return null;
            }

            return JsonUtility.FromJson<PantryItem>(request.downloadHandler.text);
        }

        public async Task<PantryItem> UpdateItemAsync(
            string itemId,
            float? quantity,
            string unit,
            string notes,
            string location,
            string expiryDate)
        {
            string pantryId = await EnsurePantryIdAsync();

            if (string.IsNullOrEmpty(pantryId))
            {
                Debug.LogWarning($"[{GetType().Name}] GetItems — pantryId unavailable");
                return null;
            }

            var sb = new StringBuilder("{");
            bool hasField = false;

            if (quantity.HasValue)
            {
                sb.AppendFormat("\"quantity\":{0}", quantity.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
                hasField = true;
            }

            if (!string.IsNullOrEmpty(unit))
            {
                if (hasField) sb.Append(",");
                sb.AppendFormat("\"unit\":\"{0}\"", EscapeJson(unit));
                hasField = true;
            }

            if (notes != null)
            {
                if (hasField) sb.Append(",");
                sb.AppendFormat("\"notes\":\"{0}\"", EscapeJson(notes));
                hasField = true;
            }

            if (location != null)
            {
                if (hasField) sb.Append(",");
                sb.AppendFormat("\"location\":\"{0}\"", EscapeJson(location));
                hasField = true;
            }

            if (!string.IsNullOrEmpty(expiryDate))
            {
                if (hasField) sb.Append(",");
                sb.AppendFormat("\"expiryDate\":\"{0}\"", EscapeJson(expiryDate));
            }

            sb.Append("}");
            byte[] body = Encoding.UTF8.GetBytes(sb.ToString());

            string url = $"{ApiConfig.BaseUrl}/api/v1/pantry/{Uri.EscapeDataString(pantryId)}/items/{Uri.EscapeDataString(itemId)}";

            using UnityWebRequest request = new UnityWebRequest(url, "PATCH")
            {
                uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" },
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
                Debug.LogError($"[{GetType().Name}] UpdateItem {itemId} failed: {request.responseCode}");
                return null;
            }

            return JsonUtility.FromJson<PantryItem>(request.downloadHandler.text);
        }

        public async Task<bool> DeleteItemAsync(string itemId)
        {
            string pantryId = await EnsurePantryIdAsync();

            if (string.IsNullOrEmpty(pantryId))
            {
                Debug.LogWarning($"[{GetType().Name}] GetItems — pantryId unavailable");
                return false;
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
                Debug.LogError($"[{GetType().Name}] DeleteItem {itemId} failed: {request.responseCode}");
                return false;
            }

            return true;
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            return s
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }
    }
}
