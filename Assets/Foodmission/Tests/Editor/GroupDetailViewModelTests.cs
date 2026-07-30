using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class GroupDetailViewModelTests
    {
        private Mock<IGroupService> _mockGroupService;
        private TestStoreService _storeService;
        private GroupDetailViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockGroupService = new Mock<IGroupService>();
            _storeService = new TestStoreService();
            _vm = new GroupDetailViewModel(_storeService, _mockGroupService.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public void Constructor_InitializesWithDefaults()
        {
            Assert.IsNull(_vm.Group);
            Assert.IsNotNull(_vm.Members);
            Assert.AreEqual(0, _vm.Members.Count);
            Assert.IsFalse(_vm.IsAdmin);
            Assert.AreEqual("", _vm.InviteCode);
            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task LoadAsync_WithEmptyId_DoesNothing()
        {
            await _vm.LoadAsync("");

            _mockGroupService.Verify(
                x => x.GetGroupAsync(It.IsAny<string>()),
                Times.Never);
        }

        [Test]
        public async Task LoadAsync_OnSuccess_SetsGroupAndMembers()
        {
            _mockGroupService
                .Setup(x => x.GetGroupAsync("group1"))
                .Returns(Task.FromResult<(UserGroup Result, ApiErrorResponse Error)>((new UserGroup
                {
                    id = "group1",
                    name = "Test Group",
                    members = new GroupMember[]
                    {
                        new GroupMember { id = "m1", userId = "user1", role = "ADMIN" }
                    }
                }, null)));

            await _vm.LoadAsync("group1");

            Assert.IsNotNull(_vm.Group);
            Assert.AreEqual("Test Group", _vm.Group.name);
            Assert.AreEqual(1, _vm.Members.Count);
            Assert.IsFalse(_vm.IsLoading);
        }

        [Test]
        public async Task LoadAsync_WithApiError_SetsErrorDetail()
        {
            _mockGroupService
                .Setup(x => x.GetGroupAsync("group1"))
                .Returns(Task.FromResult<(UserGroup Result, ApiErrorResponse Error)>(((UserGroup)null, new ApiErrorResponse { statusCode = 500 })));

            await _vm.LoadAsync("group1");

            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.AreEqual(500, _vm.ErrorDetail.statusCode);
            Assert.IsFalse(_vm.IsLoading);
        }

        [Test]
        public async Task LoadInviteCodeAsync_OnSuccess_SetsInviteCode()
        {
            _mockGroupService
                .Setup(x => x.GetGroupAsync("group1"))
                .Returns(Task.FromResult<(UserGroup Result, ApiErrorResponse Error)>((new UserGroup { id = "group1", members = new GroupMember[0] }, null)));
            _mockGroupService
                .Setup(x => x.GetInviteCodeAsync("group1"))
                .Returns(Task.FromResult<(string Code, ApiErrorResponse Error)>(("ABC123", null)));

            await _vm.LoadAsync("group1");
            await _vm.LoadInviteCodeAsync();

            Assert.AreEqual("ABC123", _vm.InviteCode);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task LoadInviteCodeAsync_WithApiError_SetsErrorDetail()
        {
            _mockGroupService
                .Setup(x => x.GetGroupAsync("group1"))
                .Returns(Task.FromResult<(UserGroup Result, ApiErrorResponse Error)>((new UserGroup { id = "group1", members = new GroupMember[0] }, null)));
            _mockGroupService
                .Setup(x => x.GetInviteCodeAsync("group1"))
                .Returns(Task.FromResult<(string Code, ApiErrorResponse Error)>((null, new ApiErrorResponse { statusCode = 500 })));

            await _vm.LoadAsync("group1");
            await _vm.LoadInviteCodeAsync();

            Assert.IsNotNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task RegenerateInviteCodeAsync_OnSuccess_UpdatesCode()
        {
            _mockGroupService
                .Setup(x => x.GetGroupAsync("group1"))
                .Returns(Task.FromResult<(UserGroup Result, ApiErrorResponse Error)>((new UserGroup { id = "group1", members = new GroupMember[0] }, null)));
            _mockGroupService
                .Setup(x => x.RegenerateInviteCodeAsync("group1"))
                .Returns(Task.FromResult<(string Code, ApiErrorResponse Error)>(("NEW456", null)));

            await _vm.LoadAsync("group1");
            await _vm.RegenerateInviteCodeAsync();

            Assert.AreEqual("NEW456", _vm.InviteCode);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task AddVirtualMemberAsync_OnSuccess_AddsToMembersList()
        {
            _mockGroupService
                .Setup(x => x.GetGroupAsync("group1"))
                .Returns(Task.FromResult<(UserGroup Result, ApiErrorResponse Error)>((new UserGroup { id = "group1", members = new GroupMember[0] }, null)));
            GroupMember newMember = new GroupMember { id = "vm1", nickname = "VirtualUser", role = "MEMBER", isVirtual = true };
            _mockGroupService
                .Setup(x => x.AddVirtualMemberAsync("group1", "VirtualUser", 2000))
                .Returns(Task.FromResult<(GroupMember Result, ApiErrorResponse Error)>((newMember, null)));

            await _vm.LoadAsync("group1");
            await _vm.AddVirtualMemberAsync("VirtualUser", 2000);

            Assert.AreEqual(1, _vm.Members.Count);
            Assert.AreEqual("VirtualUser", _vm.Members[0].nickname);
        }

        [Test]
        public async Task RemoveMemberAsync_OnSuccess_RemovesFromList()
        {
            _mockGroupService
                .Setup(x => x.GetGroupAsync("group1"))
                .Returns(Task.FromResult<(UserGroup Result, ApiErrorResponse Error)>((new UserGroup
                {
                    id = "group1",
                    members = new GroupMember[]
                    {
                        new GroupMember { id = "m1", userId = "user1", role = "MEMBER" },
                        new GroupMember { id = "m2", userId = "user2", role = "MEMBER" }
                    }
                }, null)));
            _mockGroupService
                .Setup(x => x.RemoveMemberAsync("group1", "m1"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((true, null)));

            await _vm.LoadAsync("group1");
            await _vm.RemoveMemberAsync("m1");

            Assert.AreEqual(1, _vm.Members.Count);
            Assert.AreEqual("m2", _vm.Members[0].id);
        }

        [Test]
        public async Task RemoveMemberAsync_WithApiError_SetsErrorDetail()
        {
            _mockGroupService
                .Setup(x => x.GetGroupAsync("group1"))
                .Returns(Task.FromResult<(UserGroup Result, ApiErrorResponse Error)>((new UserGroup { id = "group1", members = new GroupMember[0] }, null)));
            _mockGroupService
                .Setup(x => x.RemoveMemberAsync("group1", "m1"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((false, new ApiErrorResponse { statusCode = 500 })));

            await _vm.LoadAsync("group1");
            await _vm.RemoveMemberAsync("m1");

            Assert.IsNotNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task MakeAdminAsync_OnSuccess_ReloadsGroup()
        {
            _mockGroupService
                .Setup(x => x.GetGroupAsync("group1"))
                .Returns(Task.FromResult<(UserGroup Result, ApiErrorResponse Error)>((new UserGroup
                {
                    id = "group1",
                    members = new GroupMember[]
                    {
                        new GroupMember { id = "m1", role = "MEMBER" }
                    }
                }, null)));
            _mockGroupService
                .Setup(x => x.MakeAdminAsync("group1", "m1"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((true, null)));

            await _vm.LoadAsync("group1");

            _vm.MakeAdminAsync("m1").Wait();

            _mockGroupService.Verify(x => x.GetGroupAsync("group1"), Times.AtLeast(2));
        }

        [Test]
        public async Task MakeAdminAsync_WithApiError_SetsErrorDetail()
        {
            _mockGroupService
                .Setup(x => x.GetGroupAsync("group1"))
                .Returns(Task.FromResult<(UserGroup Result, ApiErrorResponse Error)>((new UserGroup { id = "group1", members = new GroupMember[0] }, null)));
            _mockGroupService
                .Setup(x => x.MakeAdminAsync("group1", "m1"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((false, new ApiErrorResponse { statusCode = 500 })));

            await _vm.LoadAsync("group1");
            await _vm.MakeAdminAsync("m1");

            Assert.IsNotNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task UpdateGroupAsync_OnSuccess_ReloadsGroup()
        {
            _mockGroupService
                .Setup(x => x.GetGroupAsync("group1"))
                .Returns(Task.FromResult<(UserGroup Result, ApiErrorResponse Error)>((new UserGroup { id = "group1" }, null)));
            _mockGroupService
                .Setup(x => x.UpdateGroupAsync("group1", "New Name", "New Description"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((true, null)));

            await _vm.LoadAsync("group1");
            await _vm.UpdateGroupAsync("New Name", "New Description");

            _mockGroupService.Verify(x => x.GetGroupAsync("group1"), Times.AtLeast(2));
        }

        [Test]
        public async Task UpdateGroupAsync_WithApiError_SetsErrorDetail()
        {
            _mockGroupService
                .Setup(x => x.GetGroupAsync("group1"))
                .Returns(Task.FromResult<(UserGroup Result, ApiErrorResponse Error)>((new UserGroup { id = "group1", members = new GroupMember[0] }, null)));
            _mockGroupService
                .Setup(x => x.UpdateGroupAsync("group1", "Name", "Desc"))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((false, new ApiErrorResponse { statusCode = 500 })));

            await _vm.LoadAsync("group1");
            await _vm.UpdateGroupAsync("Name", "Desc");

            Assert.IsNotNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task UpdateVirtualMemberAsync_OnSuccess_ReloadsGroup()
        {
            _mockGroupService
                .Setup(x => x.GetGroupAsync("group1"))
                .Returns(Task.FromResult<(UserGroup Result, ApiErrorResponse Error)>((new UserGroup { id = "group1" }, null)));
            _mockGroupService
                .Setup(x => x.UpdateVirtualMemberAsync("group1", "vm1", "Updated Name", 1995))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((true, null)));

            await _vm.LoadAsync("group1");
            await _vm.UpdateVirtualMemberAsync("vm1", "Updated Name", 1995);

            _mockGroupService.Verify(x => x.GetGroupAsync("group1"), Times.AtLeast(2));
        }

        [Test]
        public async Task UpdateVirtualMemberAsync_WithApiError_SetsErrorDetail()
        {
            _mockGroupService
                .Setup(x => x.GetGroupAsync("group1"))
                .Returns(Task.FromResult<(UserGroup Result, ApiErrorResponse Error)>((new UserGroup { id = "group1", members = new GroupMember[0] }, null)));
            _mockGroupService
                .Setup(x => x.UpdateVirtualMemberAsync("group1", "vm1", "Updated Name", 1995))
                .Returns(Task.FromResult<(bool Success, ApiErrorResponse Error)>((false, new ApiErrorResponse { statusCode = 500 })));

            await _vm.LoadAsync("group1");
            await _vm.UpdateVirtualMemberAsync("vm1", "Updated Name", 1995);

            Assert.IsNotNull(_vm.ErrorDetail);
        }
    }
}
