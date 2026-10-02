using System;
using System.Linq;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class BadgesViewModelTests
    {
        private TestStoreService _storeService;
        private Mock<IBadgeService> _badgeService;
        private BadgesViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _storeService.SetAppState(new AppState { userId = "u1", accessToken = "t" });
            _badgeService = new Mock<IBadgeService>();
            _vm = new BadgesViewModel(_storeService, _badgeService.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        private void Returns(params UserBadge[] badges)
        {
            _badgeService.Setup(s => s.GetMyBadgesAsync()).ReturnsAsync((new UserBadgesResponse
            {
                badges = badges,
                earnedCount = badges.Count(b => b.earned),
                totalCount = badges.Length
            }, (ApiErrorResponse)null));
        }

        [Test]
        public async Task LoadBadgesAsync_SortsBySortOrderThenCode()
        {
            Returns(
                new UserBadge { code = "CHEF", sortOrder = 10 },
                new UserBadge { code = "STYLISH", sortOrder = 2 },
                new UserBadge { code = "BRAINY", sortOrder = 2 });

            await _vm.LoadBadgesAsync();

            CollectionAssert.AreEqual(new[] { "BRAINY", "STYLISH", "CHEF" }, _vm.Badges.Select(b => b.Code).ToArray());
        }

        [Test]
        public async Task LoadBadgesAsync_EarnedBadge_UsesNormalSpriteAndHidesProgress()
        {
            var earnedAt = new DateTime(2026, 10, 1, 9, 30, 0, DateTimeKind.Utc);
            Returns(new UserBadge { code = "FIRST_STEP", name = "Primer paso", description = "d", earned = true, earnedAt = earnedAt, progress = 100 });

            await _vm.LoadBadgesAsync();

            BadgeItem item = _vm.Badges[0];
            Assert.IsTrue(item.Earned);
            Assert.AreEqual("badges/badge_FIRST_STEP", item.SpriteAddress);
            Assert.IsFalse(item.ShowProgress);
            Assert.AreEqual(earnedAt, item.EarnedAt);
            Assert.AreEqual("Primer paso", item.Name);
        }

        [TestCase(60f, 0.6f)]
        [TestCase(0f, 0f)]
        [TestCase(150f, 1f)]
        [TestCase(-5f, 0f)]
        public async Task LoadBadgesAsync_LockedBadge_UsesDisabledSpriteAndShowsClampedProgress(float progress, float expected)
        {
            Returns(new UserBadge { code = "CHEF", earned = false, progress = progress });

            await _vm.LoadBadgesAsync();

            BadgeItem item = _vm.Badges[0];
            Assert.IsFalse(item.Earned);
            Assert.AreEqual("badges/badge_CHEF_disabled", item.SpriteAddress);
            Assert.IsTrue(item.ShowProgress);
            Assert.AreEqual(expected, item.Progress, 0.0001f);
        }

        [Test]
        public async Task LoadBadgesAsync_SetsCountsAndDispatchesEarnedCodes()
        {
            Returns(
                new UserBadge { code = "FIRST_STEP", earned = true, sortOrder = 1 },
                new UserBadge { code = "CHEF", earned = false, sortOrder = 2 });

            await _vm.LoadBadgesAsync();

            Assert.AreEqual(1, _vm.EarnedCount);
            Assert.AreEqual(2, _vm.TotalCount);
            CollectionAssert.AreEqual(new[] { "FIRST_STEP" }, _storeService.GetAppState().userBadges);
            Assert.IsFalse(_vm.IsLoading);
        }

        [Test]
        public async Task LoadBadgesAsync_OnError_SetsErrorDetailAndKeepsEmptyList()
        {
            var error = new ApiErrorResponse { message = "boom" };
            _badgeService.Setup(s => s.GetMyBadgesAsync()).ReturnsAsync(((UserBadgesResponse)null, error));

            await _vm.LoadBadgesAsync();

            Assert.AreSame(error, _vm.ErrorDetail);
            Assert.AreEqual(0, _vm.Badges.Count);
            Assert.IsFalse(_vm.IsLoading);
        }

        [Test]
        public async Task LoadBadgesAsync_NullBadgesArray_IsEmptyNotCrash()
        {
            _badgeService.Setup(s => s.GetMyBadgesAsync())
                .ReturnsAsync((new UserBadgesResponse { badges = null }, (ApiErrorResponse)null));

            await _vm.LoadBadgesAsync();

            Assert.AreEqual(0, _vm.Badges.Count);
            Assert.IsNull(_vm.ErrorDetail);
        }
    }
}
