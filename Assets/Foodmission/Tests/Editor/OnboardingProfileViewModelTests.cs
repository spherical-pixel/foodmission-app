using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class OnboardingProfileViewModelTests
    {
        private Mock<ICatalogService> _mockCatalogService;
        private Mock<IAuthService> _mockAuthService;
        private TestStoreService _storeService;
        private OnboardingProfileViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockCatalogService = new Mock<ICatalogService>();
            _mockAuthService = new Mock<IAuthService>();
            _storeService = new TestStoreService();

            _storeService.SetAppState(new AppState
            {
                userGender = "male",
                userActivityLevel = "moderate",
                userEducationLevel = "bachelor",
                userAnnualIncome = "30k_50k"
            });

            _vm = new OnboardingProfileViewModel(_storeService, _mockCatalogService.Object, _mockAuthService.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public void Constructor_InitializesDefaults()
        {
            Assert.AreEqual(-1, _vm.SelectedGenderIndex);
            Assert.AreEqual(-1, _vm.SelectedActivityLevelIndex);
            Assert.AreEqual(-1, _vm.SelectedEducationLevelIndex);
            Assert.AreEqual(-1, _vm.SelectedAnnualIncomeIndex);
            Assert.AreEqual(-1, _vm.SelectedShoppingResponsibilityIndex);
            Assert.AreEqual(0, _vm.SelectedDietaryPreferenceIndices.Length);
            Assert.IsFalse(_vm.IsLoading);
            Assert.IsFalse(_vm.IsSubmitting);
            Assert.AreEqual("", _vm.ErrorMessage);
            Assert.IsNotNull(_vm.GenderOptions);
            Assert.IsNotNull(_vm.ActivityLevelOptions);
        }

        [Test]
        public void StepFlow_Initialization_SetsStepCountSevenAndNutriMessages()
        {
            _vm.Initialize();
            Assert.AreEqual(7, _vm.StepCount);
            Assert.AreEqual(0, _vm.CurrentStepIndex);
            Assert.IsTrue(_vm.IsFirstStep);
            Assert.IsFalse(_vm.IsLastStep);

        }

        [Test]
        public void IsFormValid_WithNoSelection_ReturnsTrue()
        {
            Assert.IsTrue(_vm.IsFormValid);
        }

        [Test]
        public async Task LoadCatalogDataAsync_OnSuccess_PopulatesOptions()
        {
            var catalogData = new CatalogData
            {
                genders = new[] { new CatalogItem { code = "male", label = "Male" } },
                activityLevels = new[] { new CatalogItem { code = "sedentary", label = "Sedentary" } },
                educationLevels = new[] { new CatalogItem { code = "bachelor", label = "Bachelor" } },
                annualIncomeLevels = new[] { new CatalogItem { code = "30k_50k", label = "30k-50k" } },
                shoppingResponsibilities = new[] { new CatalogItem { code = "primary", label = "Primary" } },
                dietaryPreferences = new[] { new CatalogItem { code = "vegetarian", label = "Vegetarian" } }
            };

            _mockCatalogService
                .Setup(x => x.LoadStartupAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<(CatalogData Result, ApiErrorResponse Error)>((catalogData, null)));

            await _vm.LoadCatalogDataAsync();

            Assert.AreEqual(1, _vm.GenderOptions.Count);
            Assert.AreEqual(1, _vm.ActivityLevelOptions.Count);
            Assert.AreEqual(1, _vm.EducationLevelOptions.Count);
            Assert.AreEqual(1, _vm.AnnualIncomeOptions.Count);
            Assert.AreEqual(1, _vm.ShoppingResponsibilityOptions.Count);
            Assert.AreEqual(1, _vm.DietaryPreferenceOptions.Count);
            Assert.IsFalse(_vm.IsLoading);
        }

        [Test]
        public async Task LoadCatalogDataAsync_WithNullData_ShowsError()
        {
            _mockCatalogService
                .Setup(x => x.LoadStartupAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<(CatalogData Result, ApiErrorResponse Error)>(((CatalogData)null, null)));

            bool eventFired = false;
            _vm.ShowErrorRequest += (msg) => eventFired = true;

            await _vm.LoadCatalogDataAsync();

            Assert.IsTrue(eventFired);
            Assert.IsFalse(_vm.IsLoading);
        }

        [Test]
        public async Task LoadCatalogDataAsync_WithException_FiresShowErrorRequest()
        {
            _mockCatalogService
                .Setup(x => x.LoadStartupAsync(It.IsAny<string>()))
                .Throws(new Exception("Network error"));

            LogAssert.Expect(LogType.Error, "[OnboardingProfileViewModel] LoadCatalogDataAsync exception: Network error");

            bool eventFired = false;
            _vm.ShowErrorRequest += (msg) => eventFired = true;

            await _vm.LoadCatalogDataAsync();

            Assert.IsTrue(eventFired);
            Assert.IsFalse(_vm.IsLoading);
        }

        [Test]
        public void PrePopulateFromState_WithoutCatalogData_DoesNothing()
        {
            _vm.PrePopulateFromState();
            Assert.AreEqual(-1, _vm.SelectedGenderIndex);
        }

        [Test]
        public async Task PrePopulateFromState_PopulatesIndicesFromState()
        {
            var catalogData = new CatalogData
            {
                genders = new[] { new CatalogItem { code = "male", label = "Male" } },
                activityLevels = new[] { new CatalogItem { code = "moderate", label = "Moderate" } },
                educationLevels = new[] { new CatalogItem { code = "bachelor", label = "Bachelor" } },
                annualIncomeLevels = new[] { new CatalogItem { code = "30k_50k", label = "30k-50k" } },
            };

            _mockCatalogService
                .Setup(x => x.LoadStartupAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<(CatalogData Result, ApiErrorResponse Error)>((catalogData, null)));

            await _vm.LoadCatalogDataAsync();
            _vm.PrePopulateFromState();

            Assert.AreEqual(0, _vm.SelectedGenderIndex);
            Assert.AreEqual(0, _vm.SelectedActivityLevelIndex);
            Assert.AreEqual(0, _vm.SelectedEducationLevelIndex);
            Assert.AreEqual(0, _vm.SelectedAnnualIncomeIndex);
        }

        [Test]
        public async Task SkipAsync_DispatchesSetSkippedExtendedProfile()
        {
            _mockAuthService
                .Setup(x => x.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                .ReturnsAsync((true, null));

            _storeService.DispatchedActionTypes.Clear();

            await _vm.SkipAsync();

            Assert.IsFalse(_vm.IsSubmitting);
            Assert.Contains("app/setSkippedExtendedProfile", _storeService.DispatchedActionTypes);
            _mockAuthService.Verify(x => x.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()), Times.Once);
        }

        [Test]
        public async Task SubmitAsync_OnSuccess_DispatchesSetExtendedProfile()
        {
            var catalogData = new CatalogData
            {
                genders = new[] { new CatalogItem { code = "male", label = "Male" }, new CatalogItem { code = "female", label = "Female" } },
                activityLevels = new[] { new CatalogItem { code = "moderate", label = "Moderate" } },
                educationLevels = new[] { new CatalogItem { code = "bachelor", label = "Bachelor" } },
                annualIncomeLevels = new[] { new CatalogItem { code = "30k_50k", label = "30k-50k" } },
                shoppingResponsibilities = new[] { new CatalogItem { code = "primary", label = "Primary" } },
                dietaryPreferences = new[] { new CatalogItem { code = "vegetarian", label = "Vegetarian" } }
            };

            _mockCatalogService
                .Setup(x => x.LoadStartupAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<(CatalogData Result, ApiErrorResponse Error)>((catalogData, null)));

            await _vm.LoadCatalogDataAsync();

            _vm.SelectedGenderIndex = 0;
            _vm.SelectedActivityLevelIndex = 0;
            _vm.SelectedEducationLevelIndex = 0;
            _vm.SelectedAnnualIncomeIndex = 0;

            _mockAuthService
                .Setup(x => x.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                .ReturnsAsync((true, null));

            _storeService.DispatchedActionTypes.Clear();

            await _vm.SubmitAsync();

            Assert.IsFalse(_vm.IsSubmitting);
            Assert.Contains("app/setExtendedProfile", _storeService.DispatchedActionTypes);
            _mockAuthService.Verify(x => x.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()), Times.Once);
        }

        [Test]
        public async Task SubmitAsync_WhenApiFails_SetsErrorDetail()
        {
            var catalogData = new CatalogData
            {
                genders = new[] { new CatalogItem { code = "male", label = "Male" } },
                activityLevels = new[] { new CatalogItem { code = "moderate", label = "Moderate" } },
            };

            _mockCatalogService
                .Setup(x => x.LoadStartupAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<(CatalogData Result, ApiErrorResponse Error)>((catalogData, null)));

            await _vm.LoadCatalogDataAsync();

            _vm.SelectedGenderIndex = 0;
            _vm.SelectedActivityLevelIndex = 0;

            var expectedError = new ApiErrorResponse { statusCode = 500, error = "ERR", message = "Save failed" };
            _mockAuthService
                .Setup(x => x.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                .ReturnsAsync((false, expectedError));

            await _vm.SubmitAsync();

            Assert.IsNotNull(_vm.ErrorDetail);
            Assert.IsFalse(_vm.IsSubmitting);
        }
    }
}
