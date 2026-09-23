using NUnit.Framework;
using Unity.AppUI.Redux;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class GamificationReducersTests
    {
        private AppState _initialState;

        [SetUp]
        public void SetUp()
        {
            _initialState = new AppState
            {
                userXp = 50,
                userPoints = 20,
                userBadges = new[] { "badge_1" },
                userProgressIndicators = new[]
                {
                    new ProgressIndicator { id = "pi-1", kind = "DIET_CHANGES", level = 1 }
                }
            };
        }

        [Test]
        public void SetWalletBalanceReducer_ShouldSetXpAndPoints()
        {
            var action = AppActions.setWalletBalance.Invoke(new AppActions.WalletPayload(150, 80));
            var newState = AppReducers.SetWalletBalanceReducer(_initialState, action);

            Assert.AreEqual(150, newState.userXp);
            Assert.AreEqual(80, newState.userPoints);
        }

        [Test]
        public void AddWalletRewardReducer_ShouldIncrementXpAndPoints()
        {
            var action = AppActions.addWalletReward.Invoke(new AppActions.WalletPayload(25, 10));
            var newState = AppReducers.AddWalletRewardReducer(_initialState, action);

            Assert.AreEqual(75, newState.userXp);
            Assert.AreEqual(30, newState.userPoints);
        }

        [Test]
        public void SetProgressIndicatorsReducer_ShouldSetProgressIndicators()
        {
            var newIndicators = new[]
            {
                new ProgressIndicator { id = "pi-2", kind = "FOOD_WASTE", level = 3 }
            };

            var action = AppActions.setProgressIndicators.Invoke(newIndicators);
            var newState = AppReducers.SetProgressIndicatorsReducer(_initialState, action);

            Assert.AreEqual(1, newState.userProgressIndicators.Length);
            Assert.AreEqual("pi-2", newState.userProgressIndicators[0].id);
            Assert.AreEqual("FOOD_WASTE", newState.userProgressIndicators[0].kind);
        }

        [Test]
        public void SetBadgesReducer_ShouldSetBadges()
        {
            var action = AppActions.setBadges.Invoke(new[] { "badge_1", "badge_2" });
            var newState = AppReducers.SetBadgesReducer(_initialState, action);

            Assert.AreEqual(2, newState.userBadges.Length);
            Assert.AreEqual("badge_1", newState.userBadges[0]);
            Assert.AreEqual("badge_2", newState.userBadges[1]);
        }

        [Test]
        public void LogoutReducer_ShouldResetWalletAndGamificationData()
        {
            var action = AppActions.logout.Invoke();
            var newState = AppReducers.LogoutReducer(_initialState, action);

            Assert.AreEqual(0, newState.userXp);
            Assert.AreEqual(0, newState.userPoints);
            Assert.AreEqual(0, newState.userBadges.Length);
            Assert.AreEqual(0, newState.userProgressIndicators.Length);
        }
    }
}
