using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

using Unity.AppUI.UI;

using eu.foodmission.platform;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class RegisterViewModelTests
    {
        private Mock<IAuthService> _mockAuthService;
        private Mock<ICatalogService> _mockCatalogService;
        private TestStoreService _storeService;
        private RegisterViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockCatalogService = new Mock<ICatalogService>();
            _storeService = new TestStoreService();
            _vm = new RegisterViewModel(_mockAuthService.Object, _mockCatalogService.Object, _storeService);
            _vm.Initialize();
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
            Assert.AreEqual("", _vm.Username);
            Assert.AreEqual("", _vm.Email);
            Assert.AreEqual("", _vm.Password);
            Assert.AreEqual(-1, _vm.SelectedYearOfBirthIndex);
            Assert.Greater(_vm.YearOfBirthOptions.Count, 0);
            Assert.AreEqual(-1, _vm.SelectedCountryIndex);
            Assert.AreEqual(-1, _vm.SelectedRegionIndex);
            Assert.AreEqual("", _vm.PostalCode);
            Assert.AreEqual(CheckboxState.Unchecked, _vm.HasAcceptedTerms);
            Assert.AreEqual(CheckboxState.Unchecked, _vm.HasAcceptedPrivacyPolicy);
            Assert.AreEqual(CheckboxState.Unchecked, _vm.HasAcceptedPilotConsent);
            Assert.AreEqual(9, _vm.StepCount);
            Assert.AreEqual(0, _vm.CurrentStepIndex);
            Assert.IsTrue(_vm.CanGoNext, "Step 0 (Welcome) should allow GoNext");
        }

        [Test]
        public void ValidateEmail_WithInvalidFormat_ReturnsFalse()
        {
            _vm.Email = "invalid";

            bool result = _vm.ValidateEmail();

            Assert.IsFalse(result);
            Assert.AreEqual("@UI:ERROR_EMAIL_INVALID", _vm.EmailHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Destructive, _vm.EmailHelpTextVariant);
        }

        [Test]
        public void ValidateEmail_WithValidEmail_ReturnsTrue()
        {
            _vm.Email = "test@example.com";

            bool result = _vm.ValidateEmail();

            Assert.IsTrue(result);
            Assert.AreEqual("", _vm.EmailHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Default, _vm.EmailHelpTextVariant);
        }

        [Test]
        public void ValidateYearOfBirth_WithValidValue_ReturnsTrue()
        {
            int index2000 = _vm.YearOfBirthOptions.IndexOf("2000");
            Assert.AreNotEqual(-1, index2000, "Year 2000 should be in the options list");

            _vm.SelectedYearOfBirthIndex = index2000;

            bool result = _vm.ValidateYearOfBirth();

            Assert.IsTrue(result);
            Assert.AreEqual("", _vm.YearOfBirthHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Default, _vm.YearOfBirthHelpTextVariant);
        }

        [Test]
        public void ValidateYearOfBirth_WithUnsetIndex_ReturnsTrue()
        {
            _vm.SelectedYearOfBirthIndex = -1;

            bool result = _vm.ValidateYearOfBirth();

            Assert.IsTrue(result);
            Assert.AreEqual("", _vm.YearOfBirthHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Default, _vm.YearOfBirthHelpTextVariant);
        }

        [Test]
        public void YearOfBirthOptions_AreDescendingAndInRange()
        {
            var options = _vm.YearOfBirthOptions;
            Assert.Greater(options.Count, 0, "Options should not be empty");

            int firstYear = int.Parse(options[0]);
            int lastYear = int.Parse(options[options.Count - 1]);

            Assert.AreEqual(DateTime.Now.Year - 18, firstYear, "First year should be 18 years ago (newest)");
            Assert.AreEqual(DateTime.Now.Year - 100, lastYear, "Last year should be 100 years ago (oldest)");
            Assert.Greater(firstYear, lastYear, "Options should be in descending order");
        }

        [Test]
        public void ValidateYearOfBirth_WithOutOfRangeIndex_ReturnsFalse()
        {
            _vm.SelectedYearOfBirthIndex = _vm.YearOfBirthOptions.Count;

            bool result = _vm.ValidateYearOfBirth();

            Assert.IsFalse(result);
            Assert.AreEqual("@UI:ERROR_BIRTH_1", _vm.YearOfBirthHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Destructive, _vm.YearOfBirthHelpTextVariant);
        }

        [Test]
        public void YearOfBirthOptions_ExcludesMinors()
        {
            int minorYear = DateTime.Now.Year - 17;
            string minorYearStr = minorYear.ToString();

            Assert.IsFalse(_vm.YearOfBirthOptions.Contains(minorYearStr),
                $"Year {minorYearStr} (under 18) should not be selectable");
        }

        [Test]
        public void ValidateCountry_WithNoSelection_ReturnsFalse()
        {
            bool result = _vm.ValidateCountry();

            Assert.IsFalse(result);
            Assert.AreEqual("@UI:ERROR_COUNTRY_SELECT", _vm.CountryHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Destructive, _vm.CountryHelpTextVariant);
        }

        [Test]
        public void ValidateRegion_WithNoSelection_ReturnsFalse()
        {
            bool result = _vm.ValidateRegion();

            Assert.IsFalse(result);
            Assert.AreEqual("@UI:ERROR_REGION_SELECT", _vm.RegionHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Destructive, _vm.RegionHelpTextVariant);
        }

        [Test]
        public void ValidateTerms_WithUnchecked_ReturnsFalse()
        {
            bool result = _vm.ValidateTerms();

            Assert.IsFalse(result);
            Assert.AreEqual("@UI:ACCEPT_TERMS", _vm.TermsHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Destructive, _vm.TermsHelpTextVariant);
        }

        [Test]
        public void ValidatePrivacyPolicy_WithUnchecked_ReturnsFalse()
        {
            bool result = _vm.ValidatePrivacyPolicy();

            Assert.IsFalse(result);
            Assert.AreEqual("@UI:ACCEPT_PRIVACY", _vm.PrivacyHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Destructive, _vm.PrivacyHelpTextVariant);
        }

        [Test]
        public void ValidatePilotConsent_WithUnchecked_ReturnsFalse()
        {
            _vm.IsPilotCountry = true;
            bool result = _vm.ValidatePilotConsent();

            Assert.IsFalse(result);
            Assert.AreEqual("@UI:ACCEPT_PILOT_CONSENT", _vm.ConsentHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Destructive, _vm.ConsentHelpTextVariant);
        }

        [Test]
        public void ValidatePilotConsent_WhenIsNotPilotCountry_ReturnsTrue()
        {
            _vm.IsPilotCountry = false;
            bool result = _vm.ValidatePilotConsent();

            Assert.IsTrue(result, "Non-pilot country should pass consent validation automatically");
            Assert.AreEqual("", _vm.ConsentHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Default, _vm.ConsentHelpTextVariant);
        }

        [Test]
        public async Task CheckAndLoadPilotConsentAsync_WithPilotCountryFromCatalog_PopulatesConsentText()
        {
            var consentData = new ConsentFormData { countryCode = "de", content = "# Consent Form Content" };
            _mockCatalogService
                .Setup(x => x.GetConsentFormAsync("de", It.IsAny<string>()))
                .ReturnsAsync((consentData, null));

            // Populate catalog countries list in vm via reflection or LoadCountriesAsync test helper
            var field = typeof(RegisterViewModel).GetField("_countries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_vm, new List<CatalogItem> { new CatalogItem { code = "de", label = "Germany" } });
            _vm.SelectedCountryIndex = 0;

            await _vm.CheckAndLoadPilotConsentAsync();

            Assert.IsTrue(_vm.IsPilotCountry);
            Assert.AreEqual("# Consent Form Content", _vm.PilotConsentText);
        }

        [Test]
        public async Task CheckAndLoadPilotConsentAsync_WithNonPilotCountry_SetsIsPilotCountryFalse()
        {
            _mockCatalogService
                .Setup(x => x.GetConsentFormAsync("us", It.IsAny<string>()))
                .ReturnsAsync(((ConsentFormData)null, null));

            var field = typeof(RegisterViewModel).GetField("_countries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_vm, new List<CatalogItem> { new CatalogItem { code = "us", label = "United States" } });
            _vm.SelectedCountryIndex = 0;

            await _vm.CheckAndLoadPilotConsentAsync();

            Assert.IsFalse(_vm.IsPilotCountry);
            Assert.AreEqual("", _vm.PilotConsentText);
        }

        [Test]
        public void ValidatePostalCode_WithEmpty_ReturnsTrue()
        {
            bool result = _vm.ValidatePostalCode();

            Assert.IsTrue(result);
            Assert.AreEqual("", _vm.PostalCodeHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Default, _vm.PostalCodeHelpTextVariant);
        }

        [Test]
        public void ValidatePostalCode_WithValidLength_ReturnsTrue()
        {
            _vm.PostalCode = "12345";

            bool result = _vm.ValidatePostalCode();

            Assert.IsTrue(result);
            Assert.AreEqual("", _vm.PostalCodeHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Default, _vm.PostalCodeHelpTextVariant);
        }

        [Test]
        public void ValidatePostalCode_WithTooShort_ReturnsFalse()
        {
            _vm.PostalCode = "1";

            bool result = _vm.ValidatePostalCode();

            Assert.IsFalse(result);
            Assert.AreEqual("@UI:ERROR_PC_FORMAT", _vm.PostalCodeHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Destructive, _vm.PostalCodeHelpTextVariant);
        }

        [Test]
        public async Task StepFlow_Progression_ValidatesStepByStep()
        {
            Assert.AreEqual(0, _vm.CurrentStepIndex);
            Assert.IsTrue(_vm.CanGoNext);

            // Move to Step 1 (Username)
            await _vm.GoNextAsync();
            Assert.AreEqual(1, _vm.CurrentStepIndex);
            Assert.IsFalse(_vm.CanGoNext, "Step 1 should be blocked until username is provided");

            _vm.Username = "testuser";
            Assert.IsTrue(_vm.CanGoNext);

            // Move to Step 2 (Email)
            await _vm.GoNextAsync();
            Assert.AreEqual(2, _vm.CurrentStepIndex);
            Assert.IsFalse(_vm.CanGoNext);

            _vm.Email = "test@example.com";
            Assert.IsTrue(_vm.CanGoNext);

            // Move to Step 3 (Password)
            await _vm.GoNextAsync();
            Assert.AreEqual(3, _vm.CurrentStepIndex);
            Assert.IsFalse(_vm.CanGoNext);

            _vm.Password = "password123";
            Assert.IsTrue(_vm.CanGoNext);
        }

        [Test]
        public async Task StepFlow_EnteringStep_DoesNotShowErrorInitially()
        {
            // Enter Step 1 (Username)
            await _vm.GoNextAsync();
            Assert.AreEqual(1, _vm.CurrentStepIndex);
            Assert.IsFalse(_vm.CanGoNext);
            Assert.AreEqual(string.Empty, _vm.UsernameHelpTextValue, "Field should not display error on initial step entry");
            Assert.AreEqual(HelpTextVariant.Default, _vm.UsernameHelpTextVariant);
        }

        [Test]
        public async Task StepFlow_GoNextOnInvalidStep_ShowsErrorHelpText()
        {
            // Enter Step 1 (Username)
            await _vm.GoNextAsync();
            Assert.AreEqual(1, _vm.CurrentStepIndex);

            // Attempt GoNext while Username is empty
            await _vm.GoNextAsync();
            Assert.AreEqual(1, _vm.CurrentStepIndex, "Should stay on step 1");
            Assert.AreEqual("@UI:ERROR_NO_EMPTY", _vm.UsernameHelpTextValue, "GoNext attempt on empty field should show error message");
            Assert.AreEqual(HelpTextVariant.Destructive, _vm.UsernameHelpTextVariant);
        }

        [Test]
        public async Task Register_WithAllValid_CallsAuthService()
        {
            _mockAuthService
                .Setup(x => x.RegisterAsync(
                    "testuser", "test@example.com", "password123", 2000,
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((false, null, "error"));

            _vm.Username = "testuser";
            _vm.Email = "test@example.com";
            _vm.Password = "password123";
            int index2000 = _vm.YearOfBirthOptions.IndexOf("2000");
            _vm.SelectedYearOfBirthIndex = index2000;
            _vm.SelectedCountryIndex = 0;
            _vm.SelectedRegionIndex = 0;
            _vm.PostalCode = "12345";
            _vm.HasAcceptedTerms = CheckboxState.Checked;
            _vm.HasAcceptedPrivacyPolicy = CheckboxState.Checked;
            _vm.HasAcceptedPilotConsent = CheckboxState.Checked;

            _vm.Register();

            await Task.Delay(200);

            _mockAuthService.Verify(x => x.RegisterAsync(
                "testuser", "test@example.com", "password123", 2000,
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }
    }
}
