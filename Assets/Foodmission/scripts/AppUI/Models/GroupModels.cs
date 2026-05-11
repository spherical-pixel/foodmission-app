using System;

using UnityEngine;

namespace eu.foodmission.platform
{
    [Serializable]
    public class UserGroup
    {
        public string id;
        public string name;
        public string description;
        public string inviteCode;
        public string createdAt;
        public GroupMember[] members;
    }

    [Serializable]
    public class GroupMember
    {
        public string id;
        public string name;
        public string nickname;
        public string email;
        public string role;       // "ADMIN" | "MEMBER"
        public bool isVirtual;
        public string userId;     // empty string for virtual members
    }

    // API returns top-level JSON array — JsonUtility needs an object wrapper
    [Serializable]
    public class UserGroupArrayWrapper
    {
        public UserGroup[] items;
    }

    [Serializable]
    public class GroupMemberArrayWrapper
    {
        public GroupMember[] items;
    }

    // Response shape for GET /invite-code and POST /regenerate-code
    [Serializable]
    internal class InviteCodeResponse
    {
        public string inviteCode;
    }
}
