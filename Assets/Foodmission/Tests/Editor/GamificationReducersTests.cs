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

        [Test]
        public void SetUserSegmentReducer_ShouldSetSegment()
        {
            var newState = AppReducers.SetUserSegmentReducer(_initialState, AppActions.setUserSegment.Invoke("ADVANCED"));
            Assert.AreEqual("ADVANCED", newState.userSegment);

            var cleared = AppReducers.SetUserSegmentReducer(newState, AppActions.setUserSegment.Invoke(null));
            Assert.AreEqual("", cleared.userSegment);
        }

        [Test]
        public void SetProgressWheelsReducer_StoresDetachedCopy()
        {
            var wheel = new ProgressWheel { kind = "CO2_REDUCTION", percentComplete = 10f };
            var newState = AppReducers.SetProgressWheelsReducer(_initialState, AppActions.setProgressWheels.Invoke(new[] { wheel }));

            wheel.percentComplete = 99f;

            Assert.AreEqual(1, newState.progressWheels.Length);
            Assert.AreEqual(10f, newState.progressWheels[0].percentComplete);
        }

        [Test]
        public void SetProgressWheelsReducer_NullPayload_StoresEmpty()
        {
            var newState = AppReducers.SetProgressWheelsReducer(_initialState, AppActions.setProgressWheels.Invoke(null));
            Assert.IsNotNull(newState.progressWheels);
            Assert.AreEqual(0, newState.progressWheels.Length);
        }

        [Test]
        public void SetHiddenProgressWheelsReducer_StoresCopy_AndNullBecomesEmpty()
        {
            var hidden = new[] { "WATER_SAVINGS" };
            var newState = AppReducers.SetHiddenProgressWheelsReducer(_initialState, AppActions.setHiddenProgressWheels.Invoke(hidden));
            hidden[0] = "X";
            CollectionAssert.AreEqual(new[] { "WATER_SAVINGS" }, newState.hiddenProgressWheels);

            var cleared = AppReducers.SetHiddenProgressWheelsReducer(newState, AppActions.setHiddenProgressWheels.Invoke(null));
            Assert.AreEqual(0, cleared.hiddenProgressWheels.Length);
        }

        [Test]
        public void LogoutReducer_ClearsWheelsAndHiddenKinds()
        {
            _initialState.progressWheels = new[] { new ProgressWheel { kind = "CO2_REDUCTION" } };
            _initialState.hiddenProgressWheels = new[] { "CO2_REDUCTION" };

            var newState = AppReducers.LogoutReducer(_initialState, AppActions.logout.Invoke());

            Assert.AreEqual(0, newState.progressWheels.Length);
            Assert.AreEqual(0, newState.hiddenProgressWheels.Length);
        }

        [Test]
        public void AppStateCopy_DeepCopiesWheelsAndHidden()
        {
            _initialState.progressWheels = new[] { new ProgressWheel { kind = "CO2_REDUCTION", stage = 1 } };
            _initialState.hiddenProgressWheels = new[] { "LAND_USE_REDUCTION" };

            AppState copy = _initialState.Copy();
            _initialState.progressWheels[0].stage = 5;
            _initialState.hiddenProgressWheels[0] = "X";

            Assert.AreEqual(1, copy.progressWheels[0].stage);
            Assert.AreEqual("LAND_USE_REDUCTION", copy.hiddenProgressWheels[0]);
        }

        [Test]
        public void AppState_JsonUtilityRoundTrip_KeepsWheels()
        {
            _initialState.progressWheels = new[] { new ProgressWheel { kind = "WATER_SAVINGS", stage = 3, percentComplete = 42.5f, unit = "L" } };
            _initialState.hiddenProgressWheels = new[] { "CO2_REDUCTION" };

            AppState restored = UnityEngine.JsonUtility.FromJson<AppState>(UnityEngine.JsonUtility.ToJson(_initialState));

            Assert.AreEqual("WATER_SAVINGS", restored.progressWheels[0].kind);
            Assert.AreEqual(3, restored.progressWheels[0].stage);
            Assert.AreEqual(42.5f, restored.progressWheels[0].percentComplete);
            CollectionAssert.AreEqual(new[] { "CO2_REDUCTION" }, restored.hiddenProgressWheels);
        }
    }
}
