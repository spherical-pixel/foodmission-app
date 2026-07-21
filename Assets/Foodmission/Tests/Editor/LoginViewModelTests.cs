using System;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

using Unity.AppUI.UI;

using eu.foodmission.platform;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class LoginViewModelTests
    {
        private Mock<IAuthService> _mockAuthService;
        private Mock<ICatalogService> _mockCatalogService;
        private TestStoreService _storeService;
        private LoginViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockCatalogService = new Mock<ICatalogService>();
            _storeService = new TestStoreService();
            _vm = new LoginViewModel(_mockAuthService.Object, _storeService, _mockCatalogService.Object);
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
            Assert.AreEqual("", _vm.Password);
            Assert.AreEqual(DisplayStyle.None, _vm.IsLoading);
            Assert.IsFalse(_vm.IsAuthenticated);
        }

        [Test]
        public void ValidateUsername_WithEmpty_ReturnsFalse()
        {
            _vm.Username = "";

            bool result = _vm.ValidateUsername();

            Assert.IsFalse(result);
            Assert.AreEqual("@UI:ERROR_NO_EMPTY", _vm.UsernameHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Destructive, _vm.UsernameHelpTextVariant);
        }

        [Test]
        public void ValidateUsername_WithValue_ReturnsTrue()
        {
            _vm.Username = "testuser";

            bool result = _vm.ValidateUsername();

            Assert.IsTrue(result);
            Assert.AreEqual("", _vm.UsernameHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Default, _vm.UsernameHelpTextVariant);
        }

        [Test]
        public void ValidatePassword_WithEmpty_ReturnsFalse()
        {
            _vm.Password = "";

            bool result = _vm.ValidatePassword();

            Assert.IsFalse(result);
            Assert.AreEqual("@UI:ERROR_NO_EMPTY", _vm.PasswordHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Destructive, _vm.PasswordHelpTextVariant);
        }

        [Test]
        public void ValidatePassword_WithShortPassword_ReturnsFalse()
        {
            _vm.Password = "abc";

            bool result = _vm.ValidatePassword();

            Assert.IsFalse(result);
            Assert.AreEqual("@UI:ERROR_PASS_SHORT", _vm.PasswordHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Destructive, _vm.PasswordHelpTextVariant);
        }

        [Test]
        public void ValidatePassword_WithValidPassword_ReturnsTrue()
        {
            _vm.Password = "password123";

            bool result = _vm.ValidatePassword();

            Assert.IsTrue(result);
            Assert.AreEqual("", _vm.PasswordHelpTextValue);
            Assert.AreEqual(HelpTextVariant.Default, _vm.PasswordHelpTextVariant);
        }

        [Test]
        public async Task Login_WithValidCredentials_CallsAuthService()
        {
            _mockAuthService
                .Setup(x => x.LoginAsync("user", "password123"))
                .ReturnsAsync((true, "uid", null));

            _vm.Username = "user";
            _vm.Password = "password123";

            _vm.Login(null);

            await Task.Delay(200);

            _mockAuthService.Verify(x => x.LoginAsync("user", "password123"), Times.Once);
        }

        [Test]
        public void Login_WithValidationFailure_DoesNotCallService()
        {
            _vm.Username = "";

            _vm.Login(null);

            _mockAuthService.Verify(
                x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Test]
        public async Task Login_WithAuthError_FiresShowErrorRequest()
        {
            _mockAuthService
                .Setup(x => x.LoginAsync("user", "password123"))
                .ThrowsAsync(new Exception("Login failed"));

            _vm.Username = "user";
            _vm.Password = "password123";

            bool eventFired = false;
            string capturedMessage = null;
            _vm.ShowErrorRequest += (msg) =>
            {
                eventFired = true;
                capturedMessage = msg;
            };

            LogAssert.Expect(LogType.Error, "[LoginViewModel] Login exception: Login failed");
            _vm.Login(null);

            await Task.Delay(200);

            Assert.IsTrue(eventFired);
            Assert.AreEqual("@UI:LOGIN_FAILED", capturedMessage);
        }
    }
}
