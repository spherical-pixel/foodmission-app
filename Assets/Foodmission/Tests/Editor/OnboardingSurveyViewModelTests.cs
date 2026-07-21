using System.Threading.Tasks;
using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class OnboardingSurveyViewModelTests
    {
        private TestStoreService _storeService;
        private OnboardingSurveyViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            _vm = new OnboardingSurveyViewModel(_storeService);
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
            Assert.AreEqual("Welcome", _vm.StepTitle);
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
            Assert.AreEqual("MEALS_0_4", appState.userOnboardingSurvey.meatMeals);
            Assert.AreEqual("LESS_THAN_ONCE", appState.userOnboardingSurvey.beefFrequency);
            Assert.AreEqual("NEVER", appState.userOnboardingSurvey.foodWasteFrequency);
            Assert.AreEqual("TIMES_10_14", appState.userOnboardingSurvey.ultraProcessedFrequency);
            Assert.AreEqual("ACTIONS_10_PLUS", appState.userOnboardingSurvey.reusableContainersFrequency);
        }

        [Test]
        public async Task FullFlow_UpdateProfileFails_SetsErrorDetail()
        {
            var mockAuthService = new Moq.Mock<IAuthService>();
            var expectedError = new ApiErrorResponse { statusCode = 500, error = "SERVER_ERROR", message = "Server error occurred" };
            mockAuthService.Setup(a => a.UpdateProfileAsync(Moq.It.IsAny<ProfileUpdateRequest>()))
                .Returns(Task.FromResult((false, expectedError)));

            var vm = new OnboardingSurveyViewModel(_storeService, mockAuthService.Object);
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
