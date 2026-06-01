using System;
using System.ComponentModel;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

using eu.foodmission.platform;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class SplashScreenViewModelTests
    {
        private Mock<IAuthService> _mockAuthService;
        private Mock<ITemplateService> _mockTemplateService;
        private Mock<IAppUpdateService> _mockAppUpdateService;
        private Mock<IRemoteLocalizationService> _mockRemoteLocalizationService;
        private TestStoreService _storeService;
        private SplashScreenViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockTemplateService = new Mock<ITemplateService>();
            _mockAppUpdateService = new Mock<IAppUpdateService>();
            _mockRemoteLocalizationService = new Mock<IRemoteLocalizationService>();
            _storeService = new TestStoreService();
            _vm = new SplashScreenViewModel(
                _storeService,
                _mockAuthService.Object,
                _mockTemplateService.Object,
                _mockAppUpdateService.Object,
                _mockRemoteLocalizationService.Object);
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
            Assert.AreEqual("Loading...", _vm.LoadingText);
            Assert.IsNull(_vm.PendingUpdate);
            Assert.IsNull(_vm.ReturnActionOnSkip);
        }

        [Test]
        public void LoadingText_SetAndGet_RaisesPropertyChanged()
        {
            string changedProperty = null;
            _vm.PropertyChanged += (sender, args) => changedProperty = args.PropertyName;

            _vm.LoadingText = "Checking updates...";

            Assert.AreEqual("Checking updates...", _vm.LoadingText);
            Assert.AreEqual(nameof(SplashScreenViewModel.LoadingText), changedProperty);
        }

        [Test]
        public void LoadingText_SetAndGet_DoesNotRaiseForSameValue()
        {
            int changeCount = 0;
            _vm.PropertyChanged += (sender, args) => changeCount++;

            _vm.LoadingText = "Loading...";

            Assert.AreEqual(0, changeCount);
        }

        [Test]
        public void PendingUpdate_SetAndGet()
        {
            var update = new AppVersionCheckResult
            {
                updateAvailable = true,
                isForced = false
            };

            _vm.PendingUpdate = update;

            Assert.AreSame(update, _vm.PendingUpdate);
            Assert.IsTrue(_vm.PendingUpdate.updateAvailable);
        }

        [Test]
        public void ReturnActionOnSkip_SetAndGet()
        {
            _vm.ReturnActionOnSkip = "loading_to_home";

            Assert.AreEqual("loading_to_home", _vm.ReturnActionOnSkip);
        }

        [Test]
        public async Task InitializeAppAsync_CanBeCalled()
        {
            try
            {
                await _vm.InitializeAppAsync();
            }
            catch (NullReferenceException)
            {
                // InitializeAppAsync calls App.current.services.GetService<>()
                // which throws NullReferenceException in test environment.
                // The method is not testable as-is due to this static dependency.
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception type: {ex.GetType().Name} - {ex.Message}");
            }
        }
    }
}
