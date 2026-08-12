using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class OnboardingSurveyViewModelTests
    {
        private TestStoreService _storeService;
        private Mock<ICatalogService> _catalogServiceMock;
        private OnboardingSurveyViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _catalogServiceMock = new Mock<ICatalogService>();

            _catalogServiceMock.Setup(c => c.GetWeeklyMeatRangesAsync(It.IsAny<string>()))
                .ReturnsAsync((new[] { new CatalogItem { code = "ZERO_TO_FOUR", label = "0-4" }, new CatalogItem { code = "FIVE_TO_NINE", label = "5-9" }, new CatalogItem { code = "TEN_TO_FOURTEEN", label = "10-14" }, new CatalogItem { code = "FIFTEEN_PLUS", label = "15+" } }, null));

            _catalogServiceMock.Setup(c => c.GetWeeklyBeefFrequenciesAsync(It.IsAny<string>()))
                .ReturnsAsync((new[] { new CatalogItem { code = "NEVER", label = "Never" }, new CatalogItem { code = "LESS_THAN_ONCE_PER_WEEK", label = "<1" }, new CatalogItem { code = "ONE_TO_TWO_TIMES_PER_WEEK", label = "1-2" }, new CatalogItem { code = "THREE_PLUS_TIMES_PER_WEEK", label = "3+" } }, null));

            _catalogServiceMock.Setup(c => c.GetWeeklyFoodWasteRangesAsync(It.IsAny<string>()))
                .ReturnsAsync((new[] { new CatalogItem { code = "ZERO", label = "0" }, new CatalogItem { code = "ONE_TO_TWO", label = "1-2" }, new CatalogItem { code = "THREE_TO_FOUR", label = "3-4" }, new CatalogItem { code = "FIVE_PLUS", label = "5+" } }, null));

            _catalogServiceMock.Setup(c => c.GetWeeklyUpfRangesAsync(It.IsAny<string>()))
                .ReturnsAsync((new[] { new CatalogItem { code = "ZERO_TO_THREE", label = "0-3" }, new CatalogItem { code = "FOUR_TO_NINE", label = "4-9" }, new CatalogItem { code = "TEN_TO_FOURTEEN", label = "10-14" }, new CatalogItem { code = "FIFTEEN_PLUS", label = "15+" } }, null));

            _catalogServiceMock.Setup(c => c.GetWeeklyReusableRangesAsync(It.IsAny<string>()))
                .ReturnsAsync((new[] { new CatalogItem { code = "ZERO_TO_TWO", label = "0-2" }, new CatalogItem { code = "THREE_TO_SIX", label = "3-6" }, new CatalogItem { code = "SEVEN_TO_NINE", label = "7-9" }, new CatalogItem { code = "TEN_PLUS", label = "10+" } }, null));

            _vm = new OnboardingSurveyViewModel(_storeService, _catalogServiceMock.Object);
            _vm.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public void Initialize_SetsStepCountAndInitialState()
        {
            Assert.AreEqual(6, _vm.StepCount);
            Assert.AreEqual(0, _vm.CurrentStepIndex);
            Assert.IsTrue(_vm.IsFirstStep);
            Assert.IsFalse(_vm.IsLastStep);
            Assert.IsFalse(_vm.CanGoPrevious);
            Assert.IsTrue(_vm.CanGoNext); // Step 0 (Welcome) is valid by default
            /*Assert.AreEqual("Welcome", _vm.StepTitle);*/
        }

        [Test]
        public async Task Step1_MeatMeals_RequiresSelection()
        {
            await _vm.GoNextAsync(); // Move to Step 1 (Meat Meals)
            Assert.AreEqual(1, _vm.CurrentStepIndex);
            Assert.IsFalse(_vm.CanGoNext); // Initially false because no option is selected (-1)

            _vm.MeatMealsIndex = 0; // Select "0–4 meals per week"
            _vm.InvalidateValidation();
            Assert.IsTrue(_vm.CanGoNext);
        }

        [Test]
        public async Task Step2_BeefFrequency_RequiresSelection()
        {
            _vm.MeatMealsIndex = 1;
            _vm.InvalidateValidation();
            await _vm.GoNextAsync(); // Move to Step 1
            await _vm.GoNextAsync(); // Move to Step 2 (Beef Frequency)

            Assert.AreEqual(2, _vm.CurrentStepIndex);
            Assert.IsFalse(_vm.CanGoNext);

            _vm.BeefFrequencyIndex = 2; // Select "1–2 times per week"
            _vm.InvalidateValidation();
            Assert.IsTrue(_vm.CanGoNext);
        }

        [Test]
        public async Task FullFlow_NavigatesThroughAllStepsAndCompletes()
        {
            // Step 0: Welcome
            Assert.AreEqual(0, _vm.CurrentStepIndex);
            Assert.IsTrue(_vm.CanGoNext);
            await _vm.GoNextAsync();

            // Step 1: Meat Meals
            Assert.AreEqual(1, _vm.CurrentStepIndex);
            _vm.MeatMealsIndex = 0;
            _vm.InvalidateValidation();
            await _vm.GoNextAsync();

            // Step 2: Beef Frequency
            Assert.AreEqual(2, _vm.CurrentStepIndex);
            _vm.BeefFrequencyIndex = 1;
            _vm.InvalidateValidation();
            await _vm.GoNextAsync();

            // Step 3: Food Waste
            Assert.AreEqual(3, _vm.CurrentStepIndex);
            _vm.FoodWasteFrequencyIndex = 0;
            _vm.InvalidateValidation();
            await _vm.GoNextAsync();

            // Step 4: Ultra Processed
            Assert.AreEqual(4, _vm.CurrentStepIndex);
            _vm.UltraProcessedFrequencyIndex = 2;
            _vm.InvalidateValidation();
            await _vm.GoNextAsync();

            // Step 5: Reusable Containers (Last Step)
            Assert.AreEqual(5, _vm.CurrentStepIndex);
            Assert.IsTrue(_vm.IsLastStep);
            _vm.ReusableContainersFrequencyIndex = 3;
            _vm.InvalidateValidation();
            Assert.IsTrue(_vm.CanGoNext);

            string requestedAction = null;
            _vm.NavigationRequested += (action, args) => { requestedAction = action; };

            await _vm.GoNextAsync(); // Complete flow

            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.onboardingprofile_to_onboardingavatar, requestedAction);
            var appState = _storeService.GetAppState();
            Assert.IsNotNull(appState.userOnboardingSurvey);
            Assert.AreEqual("ZERO_TO_FOUR", appState.userOnboardingSurvey.weeklyMeatConsumption);
            Assert.AreEqual("LESS_THAN_ONCE_PER_WEEK", appState.userOnboardingSurvey.weeklyBeefConsumption);
            Assert.AreEqual("ZERO", appState.userOnboardingSurvey.weeklyFoodWaste);
            Assert.AreEqual("TEN_TO_FOURTEEN", appState.userOnboardingSurvey.weeklyUpfConsumption);
            Assert.AreEqual("TEN_PLUS", appState.userOnboardingSurvey.weeklyReusableOrRefill);
            Assert.AreEqual("ZERO_TO_FOUR", appState.userOnboardingSurvey.meatMeals);
            Assert.AreEqual("LESS_THAN_ONCE_PER_WEEK", appState.userOnboardingSurvey.beefFrequency);
        }

        [Test]
        public async Task FullFlow_UpdateProfileFails_SetsErrorDetail()
        {
            var mockAuthService = new Moq.Mock<IAuthService>();
            var expectedError = new ApiErrorResponse { statusCode = 500, error = "SERVER_ERROR", message = "Server error occurred" };
            mockAuthService.Setup(a => a.UpdateProfileAsync(Moq.It.IsAny<ProfileUpdateRequest>()))
                .Returns(Task.FromResult((false, expectedError)));

            var vm = new OnboardingSurveyViewModel(_storeService, _catalogServiceMock.Object, mockAuthService.Object);
            vm.Initialize();

            // Set selections for all steps
            vm.MeatMealsIndex = 0;
            vm.BeefFrequencyIndex = 0;
            vm.FoodWasteFrequencyIndex = 0;
            vm.UltraProcessedFrequencyIndex = 0;
            vm.ReusableContainersFrequencyIndex = 0;

            // Navigate through all 6 steps (0→1→2→3→4→5→complete)
            for (int i = 0; i < 6; i++)
                await vm.GoNextAsync();

            // Log warning is expected when UpdateProfileAsync returns false
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning, "[OnboardingSurveyViewModel] Failed to sync survey data with server via PATCH");

            Assert.IsNotNull(vm.ErrorDetail);
            Assert.AreEqual("Server error occurred", vm.ErrorDetail.message);
        }
    }
}
