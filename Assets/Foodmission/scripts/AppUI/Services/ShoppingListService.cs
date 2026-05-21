using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

        public async Task<(ShoppingList[] Result, ApiErrorResponse Error)> GetListsAsync()
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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetLists"));
            }

            string json = request.downloadHandler.text;
            ShoppingListPagedResponse response = JsonUtility.FromJson<ShoppingListPagedResponse>(json);
            return (response?.data, null);
        }

        public async Task<(ShoppingList Result, ApiErrorResponse Error)> CreateListAsync(string name)
        {
            CreateShoppingListRequest body = new()
            {
                title = name
            };

            string url = $"{ApiConfig.BaseUrl}/api/v1/shopping-lists";

            using UnityWebRequest request = new UnityWebRequest(url, "POST")
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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] CreateList"));
            }

            return (JsonUtility.FromJson<ShoppingList>(request.downloadHandler.text), null);
        }

        public async Task<(bool Success, ApiErrorResponse Error)> UpdateListAsync(string id, string name)
        {
            UpdateShoppingListRequest body = new()
            {
                title = name
            };

            string url = $"{ApiConfig.BaseUrl}/api/v1/shopping-lists/{Uri.EscapeDataString(id)}";

            using UnityWebRequest request = MakePatchRequest(url, body.ToJsonBody());
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (false, ApiErrorHelper.Parse(request, $"[{GetType().Name}] UpdateList {id}"));
            }

            return (true, null);
        }

        public async Task<(bool Success, ApiErrorResponse Error)> DeleteListAsync(string id)
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
                return (false, ApiErrorHelper.Parse(request, $"[{GetType().Name}] DeleteList {id}"));
            }

            return (true, null);
        }

        // ── Items ──────────────────────────────────────────────────────────

        public async Task<(ShoppingListItem[] Result, ApiErrorResponse Error)> GetItemsAsync(string listId)
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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetItems for list {listId}"));
            }

            string json = request.downloadHandler.text;
            ShoppingListItemPagedResponse response = JsonUtility.FromJson<ShoppingListItemPagedResponse>(json);
            return (response?.data, null);
        }

        public async Task<(ShoppingListItem Result, ApiErrorResponse Error)> AddItemAsync(string listId, string foodProductId, float quantity, string unit = "PIECES", string notes = null, bool? checkedState = null)
        {
            AddShoppingListItemRequest body = new()
            {
                foodProductId = foodProductId,
                quantity = quantity,
                unit = unit ?? "PIECES",
                notes = notes,
                @checked = checkedState
            };

            string url = $"{ApiConfig.BaseUrl}/api/v1/shopping-lists/{Uri.EscapeDataString(listId)}/items";

            using UnityWebRequest request = new UnityWebRequest(url, "POST")
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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] AddItem"));
            }

            return (JsonUtility.FromJson<ShoppingListItem>(request.downloadHandler.text), null);
        }

        public async Task<(ShoppingListItem Result, ApiErrorResponse Error)> UpdateItemAsync(string listId, string itemId, float? quantity, string unit, string notes, bool? isChecked)
        {
            UpdateShoppingListItemRequest body = new()
            {
                quantity = quantity,
                unit = unit,
                notes = notes,
                isChecked = isChecked
            };

            string url = $"{ApiConfig.BaseUrl}/api/v1/shopping-lists/{Uri.EscapeDataString(listId)}/items/{Uri.EscapeDataString(itemId)}";

            using UnityWebRequest request = MakePatchRequest(url, body.ToJsonBody());
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

            return (JsonUtility.FromJson<ShoppingListItem>(request.downloadHandler.text), null);
        }

        public async Task<(bool Success, ApiErrorResponse Error)> DeleteItemAsync(string listId, string itemId)
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
                return (false, ApiErrorHelper.Parse(request, $"[{GetType().Name}] DeleteItem {itemId}"));
            }

            return (true, null);
        }

        public async Task<(bool Success, ApiErrorResponse Error)> ClearCheckedItemsAsync(string listId)
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
                return (false, ApiErrorHelper.Parse(request, $"[{GetType().Name}] ClearChecked {listId}"));
            }

            return (true, null);
        }

        // uploadHandler must be assigned after construction (not in initializer) for PATCH
        // to work correctly with NestJS — matches the pattern used in AuthService.SendPatchRequest.
        private static UnityWebRequest MakePatchRequest(string url, byte[] body)
        {
            UnityWebRequest request = new UnityWebRequest(url, "PATCH");
            if (body != null && body.Length > 0)
            {
                request.uploadHandler = new UploadHandlerRaw(body)
                {
                    contentType = "application/json"
                };
            }
            request.downloadHandler = new DownloadHandlerBuffer();
            return request;
        }

        // ── Request DTOs ───────────────────────────────────────────────────

        [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
        private class CreateShoppingListRequest
        {
            [JsonProperty("title")]
            public string title;

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
        private class UpdateShoppingListRequest
        {
            [JsonProperty("title")]
            public string title;

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
        private class AddShoppingListItemRequest
        {
            [JsonProperty("foodProductId")]
            public string foodProductId;

            [JsonProperty("quantity")]
            public float quantity;

            [JsonProperty("unit")]
            public string unit;

            [JsonProperty("notes")]
            public string notes;

            [JsonProperty("checked")]
            public bool? @checked;

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
        private class UpdateShoppingListItemRequest
        {
            [JsonProperty("quantity")]
            public float? quantity;

            [JsonProperty("unit")]
            public string unit;

            [JsonProperty("notes")]
            public string notes;

            [JsonProperty("checked")]
            public bool? isChecked;

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
