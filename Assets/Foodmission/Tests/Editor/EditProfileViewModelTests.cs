using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class EditProfileViewModelTests
    {
        private Mock<ICatalogService> _mockCatalogService;
        private Mock<IAuthService> _mockAuthService;
        private TestStoreService _storeService;
        private EditProfileViewModel _vm;

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
                userAnnualIncome = "30k_50k",
                userCountry = "ES",
                userRegion = "CT",
                userZip = "08001",
                userYearOfBirth = 1990
            });

            _vm = new EditProfileViewModel(_storeService, _mockCatalogService.Object, _mockAuthService.Object);
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
            Assert.AreEqual(-1, _vm.SelectedCountryIndex);
            Assert.AreEqual(-1, _vm.SelectedRegionIndex);
            Assert.AreEqual(-1, _vm.SelectedYearOfBirthIndex);
            Assert.Greater(_vm.YearOfBirthOptions.Count, 0);
            Assert.AreEqual("", _vm.PostalCode);
            Assert.IsFalse(_vm.IsLoading);
            Assert.IsFalse(_vm.IsSubmitting);
            Assert.AreEqual("", _vm.ErrorMessage);
            Assert.IsNotNull(_vm.GenderOptions);
            Assert.IsNotNull(_vm.ActivityLevelOptions);
            Assert.IsNotNull(_vm.CountryOptions);
            Assert.IsNotNull(_vm.RegionOptions);
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
                genders = new[] { new CatalogItem { code = "male", label = "Male" }, new CatalogItem { code = "female", label = "Female" } },
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

            Assert.AreEqual(2, _vm.GenderOptions.Count);
            Assert.AreEqual("Male", _vm.GenderOptions[0]);
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

            bool eventFired = false;
            _vm.ShowErrorRequest += (msg) => eventFired = true;

            LogAssert.Expect(LogType.Error, "[EditProfileViewModel] LoadCatalogDataAsync exception: Network error");

            await _vm.LoadCatalogDataAsync();

            Assert.IsTrue(eventFired);
            Assert.IsFalse(_vm.IsLoading);
        }

        [Test]
        public async Task PrePopulateFromState_WithoutCatalogData_DoesNothing()
        {
            await _vm.PrePopulateFromState();
            Assert.AreEqual(-1, _vm.SelectedGenderIndex);
        }

        [Test]
        public async Task LoadCountriesAsync_PopulatesCountryOptionsWithFlags()
        {
            var countries = new List<CatalogItem>
            {
                new CatalogItem { code = "ES", label = "Spain" },
                new CatalogItem { code = "AT", label = "Austria" }
            };

            _mockCatalogService
                .Setup(x => x.GetCountriesAsync())
                .Returns(Task.FromResult<(List<CatalogItem> Result, ApiErrorResponse Error)>((countries, null)));

            await _vm.LoadCountriesAsync();

            Assert.AreEqual(2, _vm.CountryOptions.Count);
            Assert.IsTrue(_vm.CountryOptions[0].Contains("Spain"));
            Assert.IsTrue(_vm.CountryOptions[0].Contains("\U0001F1EA\U0001F1F8")); // 🇪🇸
            Assert.IsTrue(_vm.CountryOptions[1].Contains("Austria"));
        }

        [Test]
        public async Task LoadCountriesAsync_ThenPrePopulate_RestoresCountryIndex()
        {
            var catalogData = new CatalogData
            {
                genders = new[] { new CatalogItem { code = "male", label = "Male" } },
                activityLevels = new[] { new CatalogItem { code = "moderate", label = "Moderate" } },
                educationLevels = new[] { new CatalogItem { code = "bachelor", label = "Bachelor" } },
                annualIncomeLevels = new[] { new CatalogItem { code = "30k_50k", label = "30k-50k" } }
            };
            var countries = new List<CatalogItem>
            {
                new CatalogItem { code = "ES", label = "Spain" },
                new CatalogItem { code = "AT", label = "Austria" }
            };

            _mockCatalogService
                .Setup(x => x.LoadStartupAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<(CatalogData Result, ApiErrorResponse Error)>((catalogData, null)));
            _mockCatalogService
                .Setup(x => x.GetCountriesAsync())
                .Returns(Task.FromResult<(List<CatalogItem> Result, ApiErrorResponse Error)>((countries, null)));

            await _vm.LoadCatalogDataAsync();
            await _vm.LoadCountriesAsync();

            _storeService.SetAppState(new AppState { userCountry = "AT" });

            await _vm.PrePopulateFromState();

            Assert.AreEqual(1, _vm.SelectedCountryIndex);
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

            await _vm.PrePopulateFromState();

            Assert.AreEqual(0, _vm.SelectedGenderIndex);
            Assert.AreEqual(0, _vm.SelectedActivityLevelIndex);
            Assert.AreEqual(0, _vm.SelectedEducationLevelIndex);
            Assert.AreEqual(0, _vm.SelectedAnnualIncomeIndex);
            Assert.AreEqual(_vm.YearOfBirthOptions.IndexOf("1990"), _vm.SelectedYearOfBirthIndex);
        }

        [Test]
        public async Task PrePopulateFromState_WithUnsetYear_KeepsNegativeIndex()
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

            _storeService.SetAppState(new AppState { userYearOfBirth = 0 });

            await _vm.PrePopulateFromState();

            Assert.AreEqual(-1, _vm.SelectedYearOfBirthIndex,
                "Year of birth index should stay -1 when stored value is 0 (unset)");
        }

        [Test]
        public async Task SubmitAsync_SendsCorrectYearOfBirth()
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
            _vm.SelectedYearOfBirthIndex = _vm.YearOfBirthOptions.IndexOf("1995");

            ProfileUpdateRequest capturedRequest = null;
            _mockAuthService
                .Setup(x => x.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                .Callback<ProfileUpdateRequest>(r => capturedRequest = r)
                .ReturnsAsync(true);

            await _vm.SubmitAsync();

            Assert.IsNotNull(capturedRequest);
            Assert.AreEqual(1995, capturedRequest.yearOfBirth);
        }

        [Test]
        public async Task UpdateRegionsForSelectedCountry_WithInvalidIndex_ClearsRegions()
        {
            await _vm.UpdateRegionsForSelectedCountryAsync();

            Assert.AreEqual(0, _vm.RegionOptions.Count);
            Assert.AreEqual(-1, _vm.SelectedRegionIndex);
        }

        [Test]
        public void GetSelectedCountryIso_WithNoSelection_ReturnsNull()
        {
            Assert.IsNull(_vm.GetSelectedCountryIso());
        }

        [Test]
        public void GetSelectedRegionIso_WithNoSelection_ReturnsNull()
        {
            Assert.IsNull(_vm.GetSelectedRegionIso());
        }

        [Test]
        public async Task SubmitAsync_WithValidForm_CallsUpdateProfile()
        {
            var catalogData = new CatalogData
            {
                genders = new[] { new CatalogItem { code = "male", label = "Male" } },
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
                .ReturnsAsync(true);

            await _vm.SubmitAsync();

            Assert.IsFalse(_vm.IsSubmitting);
            _mockAuthService.Verify(x => x.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()), Times.Once);
        }

        [Test]
        public async Task SubmitAsync_WhenApiFails_FiresShowErrorRequest()
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

            _mockAuthService
                .Setup(x => x.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                .ReturnsAsync(false);

            bool eventFired = false;
            _vm.ShowErrorRequest += (msg) => eventFired = true;

            await _vm.SubmitAsync();

            Assert.IsTrue(eventFired);
            Assert.IsFalse(_vm.IsSubmitting);
        }

        [Test]
        public async Task SubmitAsync_WithException_FiresShowErrorRequest()
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

            _mockAuthService
                .Setup(x => x.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                .Throws(new Exception("Server error"));

            bool eventFired = false;
            _vm.ShowErrorRequest += (msg) => eventFired = true;

            LogAssert.Expect(LogType.Error, "[EditProfileViewModel] SubmitAsync exception: Server error");

            await _vm.SubmitAsync();

            Assert.IsTrue(eventFired);
            Assert.IsFalse(_vm.IsSubmitting);
        }

        [Test]
        public async Task PrePopulateFromState_PopulatesDietaryAndShoppingIndices()
        {
            var catalogData = new CatalogData
            {
                genders = new[] { new CatalogItem { code = "male", label = "Male" } },
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

            _storeService.SetAppState(new AppState
            {
                userDietaryPreference = new[] { "vegetarian" },
                userShoppingResponsibility = "primary"
            });

            await _vm.PrePopulateFromState();

            Assert.AreEqual(0, _vm.SelectedShoppingResponsibilityIndex);
            Assert.AreEqual(1, _vm.SelectedDietaryPreferenceIndices.Length);
            Assert.AreEqual(0, _vm.SelectedDietaryPreferenceIndices[0]);
        }

        [Test]
        public async Task PrePopulateFromState_PopulatesMultipleDietaryIndices()
        {
            var catalogData = new CatalogData
            {
                dietaryPreferences = new[]
                {
                    new CatalogItem { code = "VEGAN", label = "Vegan" },
                    new CatalogItem { code = "GLUTEN_FREE", label = "Gluten Free" },
                    new CatalogItem { code = "HALAL", label = "Halal" }
                }
            };

            _mockCatalogService
                .Setup(x => x.LoadStartupAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<(CatalogData Result, ApiErrorResponse Error)>((catalogData, null)));

            await _vm.LoadCatalogDataAsync();

            _storeService.SetAppState(new AppState
            {
                userDietaryPreference = new[] { "GLUTEN_FREE", "HALAL" }
            });

            await _vm.PrePopulateFromState();

            Assert.AreEqual(2, _vm.SelectedDietaryPreferenceIndices.Length);
            CollectionAssert.AreEqual(new[] { 1, 2 }, _vm.SelectedDietaryPreferenceIndices);
        }

        [Test]
        public async Task PrePopulateFromState_WithUnknownDietaryCode_LeavesEmpty()
        {
            var catalogData = new CatalogData
            {
                dietaryPreferences = new[] { new CatalogItem { code = "vegetarian", label = "Vegetarian" } }
            };

            _mockCatalogService
                .Setup(x => x.LoadStartupAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<(CatalogData Result, ApiErrorResponse Error)>((catalogData, null)));

            await _vm.LoadCatalogDataAsync();

            _storeService.SetAppState(new AppState { userDietaryPreference = new[] { "unknown_code" } });

            await _vm.PrePopulateFromState();

            Assert.AreEqual(0, _vm.SelectedDietaryPreferenceIndices.Length);
        }

        [Test]
        public async Task SubmitAsync_IncludesDietaryPreferenceInRequest()
        {
            var catalogData = new CatalogData
            {
                genders = new[] { new CatalogItem { code = "male", label = "Male" } },
                dietaryPreferences = new[] { new CatalogItem { code = "VEGETARIAN", label = "Vegetarian" } },
                shoppingResponsibilities = new[] { new CatalogItem { code = "PRIMARY", label = "Primary" } }
            };

            _mockCatalogService
                .Setup(x => x.LoadStartupAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<(CatalogData Result, ApiErrorResponse Error)>((catalogData, null)));

            await _vm.LoadCatalogDataAsync();

            _vm.SelectedGenderIndex = 0;
            _vm.SelectedDietaryPreferenceIndices = new[] { 0 };

            ProfileUpdateRequest capturedRequest = null;
            _mockAuthService
                .Setup(x => x.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                .Callback<ProfileUpdateRequest>(r => capturedRequest = r)
                .ReturnsAsync(true);

            await _vm.SubmitAsync();

            Assert.IsNotNull(capturedRequest);
            Assert.IsNotNull(capturedRequest.preferences);
            CollectionAssert.AreEqual(new[] { "VEGETARIAN" }, capturedRequest.preferences.dietaryPreference);
        }

        [Test]
        public async Task SubmitAsync_IncludesMultipleDietaryPreferencesInRequest()
        {
            var catalogData = new CatalogData
            {
                genders = new[] { new CatalogItem { code = "male", label = "Male" } },
                dietaryPreferences = new[]
                {
                    new CatalogItem { code = "VEGAN", label = "Vegan" },
                    new CatalogItem { code = "GLUTEN_FREE", label = "Gluten Free" },
                    new CatalogItem { code = "HALAL", label = "Halal" }
                },
                shoppingResponsibilities = new[] { new CatalogItem { code = "PRIMARY", label = "Primary" } }
            };

            _mockCatalogService
                .Setup(x => x.LoadStartupAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<(CatalogData Result, ApiErrorResponse Error)>((catalogData, null)));

            await _vm.LoadCatalogDataAsync();

            _vm.SelectedGenderIndex = 0;
            _vm.SelectedDietaryPreferenceIndices = new[] { 0, 2 }; // VEGAN + HALAL

            ProfileUpdateRequest capturedRequest = null;
            _mockAuthService
                .Setup(x => x.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                .Callback<ProfileUpdateRequest>(r => capturedRequest = r)
                .ReturnsAsync(true);

            await _vm.SubmitAsync();

            Assert.IsNotNull(capturedRequest);
            Assert.IsNotNull(capturedRequest.preferences);
            CollectionAssert.AreEqual(new[] { "VEGAN", "HALAL" }, capturedRequest.preferences.dietaryPreference);
        }

        [Test]
        public async Task SubmitAsync_WithNoDietarySelection_SendsNullPreferencesWhenShoppingAlsoEmpty()
        {
            var catalogData = new CatalogData
            {
                genders = new[] { new CatalogItem { code = "male", label = "Male" } },
                dietaryPreferences = new[] { new CatalogItem { code = "VEGETARIAN", label = "Vegetarian" } },
                shoppingResponsibilities = new[] { new CatalogItem { code = "PRIMARY", label = "Primary" } }
            };

            _mockCatalogService
                .Setup(x => x.LoadStartupAsync(It.IsAny<string>()))
                .Returns(Task.FromResult<(CatalogData Result, ApiErrorResponse Error)>((catalogData, null)));

            await _vm.LoadCatalogDataAsync();

            _vm.SelectedGenderIndex = 0;
            // No dietary / shopping selection

            ProfileUpdateRequest capturedRequest = null;
            _mockAuthService
                .Setup(x => x.UpdateProfileAsync(It.IsAny<ProfileUpdateRequest>()))
                .Callback<ProfileUpdateRequest>(r => capturedRequest = r)
                .ReturnsAsync(true);

            await _vm.SubmitAsync();

            Assert.IsNotNull(capturedRequest);
            Assert.IsNull(capturedRequest.preferences);
        }
    }
}
