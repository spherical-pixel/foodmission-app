using System;
using System.Text;
using System.Threading.Tasks;

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

        public async Task<UserGroup[]> GetGroupsAsync()
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
                Debug.LogError($"[{GetType().Name}] GetGroups failed: {request.responseCode}");
                return null;
            }

            string json = request.downloadHandler.text;
            UserGroupArrayWrapper wrapper = JsonUtility.FromJson<UserGroupArrayWrapper>("{\"items\":" + json + "}");
            return wrapper?.items;
        }

        public async Task<UserGroup> GetGroupAsync(string id)
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
                Debug.LogError($"[{GetType().Name}] GetGroup {id} failed: {request.responseCode}");
                return null;
            }

            return JsonUtility.FromJson<UserGroup>(request.downloadHandler.text);
        }

        public async Task<UserGroup> CreateGroupAsync(string name, string description = null)
        {
            var sb = new StringBuilder("{");
            sb.AppendFormat("\"name\":\"{0}\"", EscapeJson(name));

            if (!string.IsNullOrEmpty(description))
            {
                sb.AppendFormat(",\"description\":\"{0}\"", EscapeJson(description));
            }

            sb.Append("}");
            byte[] body = Encoding.UTF8.GetBytes(sb.ToString());

            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups";

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
                Debug.LogError($"[{GetType().Name}] CreateGroup failed: {request.responseCode} — {request.downloadHandler?.text}");
                return null;
            }

            return JsonUtility.FromJson<UserGroup>(request.downloadHandler.text);
        }

        public async Task<bool> UpdateGroupAsync(string id, string name, string description = null)
        {
            var sb = new StringBuilder("{");
            sb.AppendFormat("\"name\":\"{0}\"", EscapeJson(name));

            if (!string.IsNullOrEmpty(description))
            {
                sb.AppendFormat(",\"description\":\"{0}\"", EscapeJson(description));
            }

            sb.Append("}");
            byte[] body = Encoding.UTF8.GetBytes(sb.ToString());

            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups/{Uri.EscapeDataString(id)}";

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
                Debug.LogError($"[{GetType().Name}] UpdateGroup {id} failed: {request.responseCode}");
                return false;
            }

            return true;
        }

        public async Task<bool> DeleteGroupAsync(string id)
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
                Debug.LogError($"[{GetType().Name}] DeleteGroup {id} failed: {request.responseCode}");
                return false;
            }

            return true;
        }

        // ── Join / Leave ───────────────────────────────────────────────────

        public async Task<UserGroup> JoinGroupAsync(string inviteCode)
        {
            var sb = new StringBuilder("{");
            sb.AppendFormat("\"inviteCode\":\"{0}\"", EscapeJson(inviteCode));
            sb.Append("}");
            byte[] body = Encoding.UTF8.GetBytes(sb.ToString());

            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups/join";

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
                Debug.LogError($"[{GetType().Name}] JoinGroup failed: {request.responseCode} — {request.downloadHandler?.text}");
                return null;
            }

            return JsonUtility.FromJson<UserGroup>(request.downloadHandler.text);
        }

        public async Task<bool> LeaveGroupAsync(string id)
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
                Debug.LogError($"[{GetType().Name}] LeaveGroup {id} failed: {request.responseCode}");
                return false;
            }

            return true;
        }

        // ── Invite Code ────────────────────────────────────────────────────

        public async Task<string> GetInviteCodeAsync(string id)
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
                Debug.LogError($"[{GetType().Name}] GetInviteCode {id} failed: {request.responseCode}");
                return null;
            }

            InviteCodeResponse response = JsonUtility.FromJson<InviteCodeResponse>(request.downloadHandler.text);
            return response?.inviteCode;
        }

        public async Task<string> RegenerateInviteCodeAsync(string id)
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
                Debug.LogError($"[{GetType().Name}] RegenerateInviteCode {id} failed: {request.responseCode}");
                return null;
            }

            InviteCodeResponse response = JsonUtility.FromJson<InviteCodeResponse>(request.downloadHandler.text);
            return response?.inviteCode;
        }

        // ── Members ────────────────────────────────────────────────────────

        public async Task<GroupMember[]> GetMembersAsync(string id)
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
                Debug.LogError($"[{GetType().Name}] GetMembers {id} failed: {request.responseCode}");
                return null;
            }

            string json = request.downloadHandler.text;
            GroupMemberArrayWrapper wrapper = JsonUtility.FromJson<GroupMemberArrayWrapper>("{\"items\":" + json + "}");
            return wrapper?.items;
        }

        public async Task<GroupMember> AddVirtualMemberAsync(string groupId, string name, int yearOfBirth = 0)
        {
            var sb = new StringBuilder("{");
            sb.AppendFormat("\"name\":\"{0}\"", EscapeJson(name));

            if (yearOfBirth > 0)
            {
                sb.AppendFormat(",\"yearOfBirth\":{0}", yearOfBirth);
            }

            sb.Append("}");
            byte[] body = Encoding.UTF8.GetBytes(sb.ToString());

            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups/{Uri.EscapeDataString(groupId)}/members";

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
                Debug.LogError($"[{GetType().Name}] AddVirtualMember {groupId} failed: {request.responseCode} — {request.downloadHandler?.text}");
                return null;
            }

            return JsonUtility.FromJson<GroupMember>(request.downloadHandler.text);
        }

        public async Task<bool> UpdateVirtualMemberAsync(string groupId, string memberId, string name, int yearOfBirth = 0)
        {
            var sb = new StringBuilder("{");
            sb.AppendFormat("\"name\":\"{0}\"", EscapeJson(name));

            if (yearOfBirth > 0)
            {
                sb.AppendFormat(",\"yearOfBirth\":{0}", yearOfBirth);
            }

            sb.Append("}");
            byte[] body = Encoding.UTF8.GetBytes(sb.ToString());

            string url = $"{ApiConfig.BaseUrl}/api/v1/user-groups/{Uri.EscapeDataString(groupId)}/members/{Uri.EscapeDataString(memberId)}";

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
                Debug.LogError($"[{GetType().Name}] UpdateVirtualMember {memberId} failed: {request.responseCode}");
                return false;
            }

            return true;
        }

        public async Task<bool> RemoveMemberAsync(string groupId, string memberId)
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
                Debug.LogError($"[{GetType().Name}] RemoveMember {memberId} failed: {request.responseCode}");
                return false;
            }

            return true;
        }

        public async Task<bool> MakeAdminAsync(string groupId, string memberId)
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
                Debug.LogError($"[{GetType().Name}] MakeAdmin {memberId} failed: {request.responseCode}");
                return false;
            }

            return true;
        }

        // ── Helpers ────────────────────────────────────────────────────────

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
