using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class GroupService : IGroupService
    {
        private readonly IStoreService _storeService;

        public GroupService(IStoreService storeService)
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

        // ── Groups CRUD ────────────────────────────────────────────────────

        public async Task<(UserGroup[] Result, ApiErrorResponse Error)> GetGroupsAsync()
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups";

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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetGroups"));
            }

            string json = request.downloadHandler.text;
            UserGroupArrayWrapper wrapper = JsonUtility.FromJson<UserGroupArrayWrapper>("{\"items\":" + json + "}");
            return (wrapper?.items, null);
        }

        public async Task<(UserGroup Result, ApiErrorResponse Error)> GetGroupAsync(string id)
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups/{Uri.EscapeDataString(id)}";

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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetGroup {id}"));
            }

            return (JsonUtility.FromJson<UserGroup>(request.downloadHandler.text), null);
        }

        public async Task<(UserGroup Result, ApiErrorResponse Error)> CreateGroupAsync(string name, string description = null)
        {
            CreateGroupRequest body = new()
            {
                name = name,
                description = description
            };

            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups";

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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] CreateGroup"));
            }

            return (JsonUtility.FromJson<UserGroup>(request.downloadHandler.text), null);
        }

        public async Task<(bool Success, ApiErrorResponse Error)> UpdateGroupAsync(string id, string name, string description = null)
        {
            UpdateGroupRequest body = new()
            {
                name = name,
                description = description
            };

            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups/{Uri.EscapeDataString(id)}";

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
                return (false, ApiErrorHelper.Parse(request, $"[{GetType().Name}] UpdateGroup {id}"));
            }

            return (true, null);
        }

        public async Task<(bool Success, ApiErrorResponse Error)> DeleteGroupAsync(string id)
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups/{Uri.EscapeDataString(id)}";

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
                return (false, ApiErrorHelper.Parse(request, $"[{GetType().Name}] DeleteGroup {id}"));
            }

            return (true, null);
        }

        // ── Join / Leave ───────────────────────────────────────────────────

        public async Task<(UserGroup Result, ApiErrorResponse Error)> JoinGroupAsync(string inviteCode)
        {
            JoinGroupRequest body = new() { inviteCode = inviteCode };

            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups/join";

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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] JoinGroup"));
            }

            return (JsonUtility.FromJson<UserGroup>(request.downloadHandler.text), null);
        }

        public async Task<(bool Success, ApiErrorResponse Error)> LeaveGroupAsync(string id)
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups/{Uri.EscapeDataString(id)}/leave";

            using UnityWebRequest request = new UnityWebRequest(url, "POST")
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
                return (false, ApiErrorHelper.Parse(request, $"[{GetType().Name}] LeaveGroup {id}"));
            }

            return (true, null);
        }

        // ── Invite Code ────────────────────────────────────────────────────

        public async Task<(string Code, ApiErrorResponse Error)> GetInviteCodeAsync(string id)
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups/{Uri.EscapeDataString(id)}/invite-code";

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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetInviteCode {id}"));
            }

            InviteCodeResponse response = JsonUtility.FromJson<InviteCodeResponse>(request.downloadHandler.text);
            return (response?.inviteCode, null);
        }

        public async Task<(string Code, ApiErrorResponse Error)> RegenerateInviteCodeAsync(string id)
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups/{Uri.EscapeDataString(id)}/regenerate-code";

            using UnityWebRequest request = new UnityWebRequest(url, "POST")
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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] RegenerateInviteCode {id}"));
            }

            InviteCodeResponse response = JsonUtility.FromJson<InviteCodeResponse>(request.downloadHandler.text);
            return (response?.inviteCode, null);
        }

        // ── Members ────────────────────────────────────────────────────────

        public async Task<(GroupMember[] Result, ApiErrorResponse Error)> GetMembersAsync(string id)
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups/{Uri.EscapeDataString(id)}/members";

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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetMembers {id}"));
            }

            string json = request.downloadHandler.text;
            GroupMemberArrayWrapper wrapper = JsonUtility.FromJson<GroupMemberArrayWrapper>("{\"items\":" + json + "}");
            return (wrapper?.items, null);
        }

        public async Task<(GroupMember Result, ApiErrorResponse Error)> AddVirtualMemberAsync(string groupId, string name, int yearOfBirth = 0)
        {
            AddMemberRequest body = new()
            {
                nickname = name,
                yearOfBirth = yearOfBirth > 0 ? yearOfBirth : null
            };

            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups/{Uri.EscapeDataString(groupId)}/members";

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
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] AddVirtualMember {groupId}"));
            }

            return (JsonUtility.FromJson<GroupMember>(request.downloadHandler.text), null);
        }

        public async Task<(bool Success, ApiErrorResponse Error)> UpdateVirtualMemberAsync(string groupId, string memberId, string name, int yearOfBirth = 0)
        {
            UpdateMemberRequest body = new()
            {
                nickname = name,
                yearOfBirth = yearOfBirth > 0 ? yearOfBirth : null
            };

            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups/{Uri.EscapeDataString(groupId)}/members/{Uri.EscapeDataString(memberId)}";

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
                return (false, ApiErrorHelper.Parse(request, $"[{GetType().Name}] UpdateVirtualMember {memberId}"));
            }

            return (true, null);
        }

        public async Task<(bool Success, ApiErrorResponse Error)> RemoveMemberAsync(string groupId, string memberId)
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups/{Uri.EscapeDataString(groupId)}/members/{Uri.EscapeDataString(memberId)}";

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
                return (false, ApiErrorHelper.Parse(request, $"[{GetType().Name}] RemoveMember {memberId}"));
            }

            return (true, null);
        }

        public async Task<(bool Success, ApiErrorResponse Error)> MakeAdminAsync(string groupId, string memberId)
        {
            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups/{Uri.EscapeDataString(groupId)}/members/{Uri.EscapeDataString(memberId)}/make-admin";

            using UnityWebRequest request = new UnityWebRequest(url, "POST")
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
                return (false, ApiErrorHelper.Parse(request, $"[{GetType().Name}] MakeAdmin {memberId}"));
            }

            return (true, null);
        }

        // ── Request DTOs ───────────────────────────────────────────────────

        [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
        private class CreateGroupRequest
        {
            [JsonProperty("name")]
            public string name;

            [JsonProperty("description")]
            public string description;

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
        private class UpdateGroupRequest
        {
            [JsonProperty("name")]
            public string name;

            [JsonProperty("description")]
            public string description;

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
        private class JoinGroupRequest
        {
            [JsonProperty("inviteCode")]
            public string inviteCode;

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
        private class AddMemberRequest
        {
            [JsonProperty("nickname")]
            public string nickname;

            [JsonProperty("yearOfBirth")]
            public int? yearOfBirth;

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
        private class UpdateMemberRequest
        {
            [JsonProperty("nickname")]
            public string nickname;

            [JsonProperty("yearOfBirth")]
            public int? yearOfBirth;

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
