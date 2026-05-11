using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IGroupService
    {
        // Groups CRUD
        Task<(UserGroup[] Result, ApiErrorResponse Error)> GetGroupsAsync();
        Task<(UserGroup Result, ApiErrorResponse Error)> GetGroupAsync(string id);
        Task<(UserGroup Result, ApiErrorResponse Error)> CreateGroupAsync(string name, string description = null);
        Task<(bool Success, ApiErrorResponse Error)> UpdateGroupAsync(string id, string name, string description = null);
        Task<(bool Success, ApiErrorResponse Error)> DeleteGroupAsync(string id);

        // Join / Leave
        Task<(UserGroup Result, ApiErrorResponse Error)> JoinGroupAsync(string inviteCode);
        Task<(bool Success, ApiErrorResponse Error)> LeaveGroupAsync(string id);

        // Invite code (admin only)
        Task<(string Code, ApiErrorResponse Error)> GetInviteCodeAsync(string id);
        Task<(string Code, ApiErrorResponse Error)> RegenerateInviteCodeAsync(string id);

        // Members
        Task<(GroupMember[] Result, ApiErrorResponse Error)> GetMembersAsync(string id);
        Task<(GroupMember Result, ApiErrorResponse Error)> AddVirtualMemberAsync(string groupId, string name, int yearOfBirth = 0);
        Task<(bool Success, ApiErrorResponse Error)> UpdateVirtualMemberAsync(string groupId, string memberId, string name, int yearOfBirth = 0);
        Task<(bool Success, ApiErrorResponse Error)> RemoveMemberAsync(string groupId, string memberId);
        Task<(bool Success, ApiErrorResponse Error)> MakeAdminAsync(string groupId, string memberId);
    }
}
