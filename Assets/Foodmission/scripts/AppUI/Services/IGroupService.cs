using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public interface IGroupService
    {
        // Groups CRUD
        Task<UserGroup[]> GetGroupsAsync();
        Task<UserGroup> GetGroupAsync(string id);
        Task<UserGroup> CreateGroupAsync(string name, string description = null);
        Task<bool> UpdateGroupAsync(string id, string name, string description = null);
        Task<bool> DeleteGroupAsync(string id);

        // Join / Leave
        Task<UserGroup> JoinGroupAsync(string inviteCode);
        Task<bool> LeaveGroupAsync(string id);

        // Invite code (admin only)
        Task<string> GetInviteCodeAsync(string id);
        Task<string> RegenerateInviteCodeAsync(string id);

        // Members
        Task<GroupMember[]> GetMembersAsync(string id);
        Task<GroupMember> AddVirtualMemberAsync(string groupId, string name, int yearOfBirth = 0);
        Task<bool> UpdateVirtualMemberAsync(string groupId, string memberId, string name, int yearOfBirth = 0);
        Task<bool> RemoveMemberAsync(string groupId, string memberId);
        Task<bool> MakeAdminAsync(string groupId, string memberId);
    }
}
