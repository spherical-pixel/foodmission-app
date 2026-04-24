using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class GroupModelsTests
    {
        [Test]
        public void UserGroup_Roundtrips_Via_JsonUtility()
        {
            var group = new UserGroup
            {
                id = "g-1",
                name = "Family",
                description = "Our family group",
                inviteCode = "ABC123",
                createdAt = "2026-04-24T10:00:00Z"
            };
            string json = JsonUtility.ToJson(group);
            var result = JsonUtility.FromJson<UserGroup>(json);

            Assert.AreEqual("g-1", result.id);
            Assert.AreEqual("Family", result.name);
            Assert.AreEqual("ABC123", result.inviteCode);
        }

        [Test]
        public void GroupMember_Role_And_IsVirtual_Roundtrip()
        {
            var member = new GroupMember
            {
                id = "m-1",
                name = "Alice",
                email = "alice@example.com",
                role = "ADMIN",
                isVirtual = false,
                userId = "u-1"
            };
            string json = JsonUtility.ToJson(member);
            var result = JsonUtility.FromJson<GroupMember>(json);

            Assert.AreEqual("ADMIN", result.role);
            Assert.IsFalse(result.isVirtual);
            Assert.AreEqual("u-1", result.userId);
        }

        [Test]
        public void UserGroupArrayWrapper_Deserializes_Api_Array_Response()
        {
            string apiJson = "[{\"id\":\"g-1\",\"name\":\"Family\",\"description\":\"\",\"inviteCode\":\"ABC123\",\"createdAt\":\"\",\"members\":[]}," +
                             "{\"id\":\"g-2\",\"name\":\"Work\",\"description\":\"\",\"inviteCode\":\"XYZ789\",\"createdAt\":\"\",\"members\":[]}]";
            string wrapped = "{\"items\":" + apiJson + "}";

            var wrapper = JsonUtility.FromJson<UserGroupArrayWrapper>(wrapped);

            Assert.IsNotNull(wrapper.items);
            Assert.AreEqual(2, wrapper.items.Length);
            Assert.AreEqual("Family", wrapper.items[0].name);
            Assert.AreEqual("Work", wrapper.items[1].name);
        }

        [Test]
        public void UserGroup_Members_Embedded_Deserialize_Correctly()
        {
            string json = "{\"id\":\"g-1\",\"name\":\"Family\",\"description\":\"\",\"inviteCode\":\"ABC123\",\"createdAt\":\"\"," +
                          "\"members\":[{\"id\":\"m-1\",\"name\":\"Alice\",\"email\":\"alice@test.com\",\"role\":\"ADMIN\",\"isVirtual\":false,\"userId\":\"u-1\"}]}";

            var group = JsonUtility.FromJson<UserGroup>(json);

            Assert.IsNotNull(group.members);
            Assert.AreEqual(1, group.members.Length);
            Assert.AreEqual("ADMIN", group.members[0].role);
            Assert.AreEqual("u-1", group.members[0].userId);
        }
    }
}
