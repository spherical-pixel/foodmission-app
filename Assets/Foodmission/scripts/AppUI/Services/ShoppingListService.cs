using System;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class ShoppingListService : IShoppingListService
    {
        private readonly IStoreService _storeService;

        public ShoppingListService(IStoreService storeService)
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

        // ── Lists ──────────────────────────────────────────────────────────

        public async Task<ShoppingList[]> GetListsAsync()
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/shopping-lists";

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
                Debug.LogError($"[{GetType().Name}] GetLists failed: {request.responseCode}");
                return null;
            }

            string json = request.downloadHandler.text;
            ShoppingListArrayWrapper wrapper = JsonUtility.FromJson<ShoppingListArrayWrapper>("{\"items\":" + json + "}");
            return wrapper?.items;
        }

        public async Task<ShoppingList> CreateListAsync(string name, string description = null, string groupId = null)
        {
            var sb = new StringBuilder("{");
            sb.AppendFormat("\"name\":\"{0}\"", EscapeJson(name));

            if (!string.IsNullOrEmpty(description))
            {
                sb.AppendFormat(",\"description\":\"{0}\"", EscapeJson(description));
            }

            if (!string.IsNullOrEmpty(groupId))
            {
                sb.AppendFormat(",\"userGroupId\":\"{0}\"", EscapeJson(groupId));
            }

            sb.Append("}");
            byte[] body = Encoding.UTF8.GetBytes(sb.ToString());

            string url = $"{ApiConfig.BaseUrl}/api/v1/shopping-lists";

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
                Debug.LogError($"[{GetType().Name}] CreateList failed: {request.responseCode} — {request.downloadHandler?.text}");
                return null;
            }

            return JsonUtility.FromJson<ShoppingList>(request.downloadHandler.text);
        }

        public async Task<bool> UpdateListAsync(string id, string name, string description = null)
        {
            var sb = new StringBuilder("{");
            sb.AppendFormat("\"name\":\"{0}\"", EscapeJson(name));

            if (!string.IsNullOrEmpty(description))
            {
                sb.AppendFormat(",\"description\":\"{0}\"", EscapeJson(description));
            }

            sb.Append("}");
            byte[] body = Encoding.UTF8.GetBytes(sb.ToString());

            string url = $"{ApiConfig.BaseUrl}/api/v1/shopping-lists/{Uri.EscapeDataString(id)}";

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
                Debug.LogError($"[{GetType().Name}] UpdateList {id} failed: {request.responseCode}");
                return false;
            }

            return true;
        }

        public async Task<bool> DeleteListAsync(string id)
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/shopping-lists/{Uri.EscapeDataString(id)}";

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
                Debug.LogError($"[{GetType().Name}] DeleteList {id} failed: {request.responseCode}");
                return false;
            }

            return true;
        }

        // ── Items ──────────────────────────────────────────────────────────

        public async Task<ShoppingListItem[]> GetItemsAsync(string listId)
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/shopping-lists/{Uri.EscapeDataString(listId)}/items";

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
                Debug.LogError($"[{GetType().Name}] GetItems for list {listId} failed: {request.responseCode}");
                return null;
            }

            string json = request.downloadHandler.text;
            ShoppingListItemArrayWrapper wrapper = JsonUtility.FromJson<ShoppingListItemArrayWrapper>("{\"items\":" + json + "}");
            return wrapper?.items;
        }

        public async Task<ShoppingListItem> AddItemAsync(string listId, string foodId, float quantity, string unit = "PIECES", string notes = null)
        {
            var sb = new StringBuilder("{");
            sb.AppendFormat("\"foodId\":\"{0}\"", EscapeJson(foodId));
            sb.AppendFormat(",\"quantity\":{0}", quantity.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            sb.AppendFormat(",\"unit\":\"{0}\"", unit ?? "PIECES");

            if (!string.IsNullOrEmpty(notes))
            {
                sb.AppendFormat(",\"notes\":\"{0}\"", EscapeJson(notes));
            }

            sb.Append("}");
            byte[] body = Encoding.UTF8.GetBytes(sb.ToString());

            string url = $"{ApiConfig.BaseUrl}/api/v1/shopping-lists/{Uri.EscapeDataString(listId)}/items";

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

            return JsonUtility.FromJson<ShoppingListItem>(request.downloadHandler.text);
        }

        public async Task<ShoppingListItem> UpdateItemAsync(string listId, string itemId, float? quantity, string unit, string notes, bool? isChecked)
        {
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
                sb.AppendFormat("\"unit\":\"{0}\"", unit);
                hasField = true;
            }

            if (notes != null)
            {
                if (hasField) sb.Append(",");
                sb.AppendFormat("\"notes\":\"{0}\"", EscapeJson(notes));
                hasField = true;
            }

            if (isChecked.HasValue)
            {
                if (hasField) sb.Append(",");
                sb.AppendFormat("\"checked\":{0}", isChecked.Value ? "true" : "false");
            }

            sb.Append("}");
            byte[] body = Encoding.UTF8.GetBytes(sb.ToString());

            string url = $"{ApiConfig.BaseUrl}/api/v1/shopping-lists/{Uri.EscapeDataString(listId)}/items/{Uri.EscapeDataString(itemId)}";

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

            return JsonUtility.FromJson<ShoppingListItem>(request.downloadHandler.text);
        }

        public async Task<ShoppingListItem> ToggleItemCheckedAsync(string listId, string itemId)
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/shopping-lists/{Uri.EscapeDataString(listId)}/items/{Uri.EscapeDataString(itemId)}/toggle-checked";

            using UnityWebRequest request = new UnityWebRequest(url, "PATCH")
            {
                uploadHandler = new UploadHandlerRaw(Array.Empty<byte>()) { contentType = "application/json" },
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
                Debug.LogError($"[{GetType().Name}] ToggleItem {itemId} failed: {request.responseCode}");
                return null;
            }

            return JsonUtility.FromJson<ShoppingListItem>(request.downloadHandler.text);
        }

        public async Task<bool> DeleteItemAsync(string listId, string itemId)
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/shopping-lists/{Uri.EscapeDataString(listId)}/items/{Uri.EscapeDataString(itemId)}";

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

        public async Task<bool> ClearCheckedItemsAsync(string listId)
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/shopping-lists/{Uri.EscapeDataString(listId)}/items/clear-checked";

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
                Debug.LogError($"[{GetType().Name}] ClearChecked {listId} failed: {request.responseCode}");
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
