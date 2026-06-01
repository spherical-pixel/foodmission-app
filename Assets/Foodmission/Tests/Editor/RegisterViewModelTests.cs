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
        private TestStoreService _storeService;
        private RegisterViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockAuthService = new Mock<IAuthService>();
            _storeService = new TestStoreService();
            _vm = new RegisterViewModel(_mockAuthService.Object, _storeService);
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
            Assert.AreEqual(0, _vm.YearOfBirth);
            Assert.AreEqual(-1, _vm.SelectedCountryIndex);
            Assert.AreEqual(-1, _vm.SelectedRegionIndex);
            Assert.AreEqual("", _vm.PostalCode);
            Assert.AreEqual(CheckboxState.Unchecked, _vm.HasAcceptedTerms);
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
            _vm.YearOfBirth = 2000;

            bool result = _vm.ValidateYearOfBirth();

            Assert.IsTrue(result);
            Assert.AreEqual("", _vm.YearOfBirthHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Default, _vm.YearOfBirthHelpTextVariant);
        }

        [Test]
        public void ValidateCountry_WithNoSelection_ReturnsFalse()
        {
            // SelectedCountryIndex defaults to -1

            bool result = _vm.ValidateCountry();

            Assert.IsFalse(result);
            Assert.AreEqual("@UI:ERROR_COUNTRY_SELECT", _vm.CountryHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Destructive, _vm.CountryHelpTextVariant);
        }

        [Test]
        public void ValidateRegion_WithNoSelection_ReturnsFalse()
        {
            // SelectedRegionIndex defaults to -1

            bool result = _vm.ValidateRegion();

            Assert.IsFalse(result);
            Assert.AreEqual("@UI:ERROR_REGION_SELECT", _vm.RegionHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Destructive, _vm.RegionHelpTextVariant);
        }

        [Test]
        public void ValidateTerms_WithUnchecked_ReturnsFalse()
        {
            // HasAcceptedTerms defaults to CheckboxState.Unchecked

            bool result = _vm.ValidateTerms();

            Assert.IsFalse(result);
            Assert.AreEqual("@UI:ACCEPT_TERMS", _vm.TermsHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Destructive, _vm.TermsHelpTextVariant);
        }

        [Test]
        public void ValidatePostalCode_WithEmpty_ReturnsTrue()
        {
            // PostalCode defaults to ""

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
            _vm.YearOfBirth = 2000;
            _vm.SelectedCountryIndex = 0;
            _vm.SelectedRegionIndex = 0;
            _vm.PostalCode = "12345";
            _vm.HasAcceptedTerms = CheckboxState.Checked;

            _vm.Register();

            await Task.Delay(200);

            _mockAuthService.Verify(x => x.RegisterAsync(
                "testuser", "test@example.com", "password123", 2000,
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }
    }
}
