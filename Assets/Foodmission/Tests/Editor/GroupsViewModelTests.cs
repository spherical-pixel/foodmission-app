using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class GroupsViewModelTests
    {
        private Mock<IGroupService> _mockGroupService;
        private TestStoreService _storeService;
        private GroupsViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockGroupService = new Mock<IGroupService>();
            _storeService = new TestStoreService();
            _vm = new GroupsViewModel(_storeService, _mockGroupService.Object);
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
            Assert.IsNotNull(_vm.Groups);
            Assert.AreEqual(0, _vm.Groups.Count);
            Assert.AreEqual("", _vm.SearchText);
            Assert.IsFalse(_vm.IsLoading);
            Assert.AreEqual("", _vm.ErrorMessage);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task LoadGroupsAsync_OnSuccess_PopulatesGroupsAndAppliesFilter()
        {
            _mockGroupService
                .Setup(x => x.GetGroupsAsync())
                .Returns(Task.FromResult<(UserGroup[] Result, ApiErrorResponse Error)>((new UserGroup[]
                {
                    new UserGroup { id = "g1", name = "Family", description = "Home family group" },
                    new UserGroup { id = "g2", name = "Work", description = "Office team" }
                }, null)));

            await _vm.LoadGroupsAsync();

            Assert.AreEqual(2, _vm.Groups.Count);
            Assert.IsFalse(_vm.IsLoading);
            Assert.IsNull(_vm.ErrorDetail);
        }

        [Test]
        public async Task ApplyFilter_FiltersByNameAndDescription()
        {
            _mockGroupService
                .Setup(x => x.GetGroupsAsync())
                .Returns(Task.FromResult<(UserGroup[] Result, ApiErrorResponse Error)>((new UserGroup[]
                {
                    new UserGroup { id = "g1", name = "Family Group", description = "Home food" },
                    new UserGroup { id = "g2", name = "Work Group", description = "Office lunch" }
                }, null)));

            await _vm.LoadGroupsAsync();

            _vm.SearchText = "Office";
            _vm.ApplyFilter();

            Assert.AreEqual(1, _vm.Groups.Count);
            Assert.AreEqual("Work Group", _vm.Groups[0].name);
        }
    }
}
