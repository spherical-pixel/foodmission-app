using System;
using System.ComponentModel;
using System.Threading.Tasks;

using Moq;
using NUnit.Framework;

using Unity.AppUI.Navigation.Generated;

using eu.foodmission.platform;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class SplashScreenViewModelTests
    {
        private Mock<IAuthService> _mockAuthService;
        private Mock<ITemplateService> _mockTemplateService;
        private Mock<IAppUpdateService> _mockAppUpdateService;
        private Mock<IGenericFoodService> _mockGenericFoodService;
        private TestStoreService _storeService;
        private SplashScreenViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockTemplateService = new Mock<ITemplateService>();
            _mockAppUpdateService = new Mock<IAppUpdateService>();
            _mockGenericFoodService = new Mock<IGenericFoodService>();
            _storeService = new TestStoreService();
            _vm = new SplashScreenViewModel(
                _storeService,
                _mockAuthService.Object,
                _mockTemplateService.Object,
                _mockAppUpdateService.Object,
                _mockGenericFoodService.Object);
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

        // A failing init step (here App.current is null in EditMode) must not leave the app on the splash
        [Test]
        public async Task InitializeAppAsync_StepFails_WithSavedSession_GoesHome()
        {
            AppState state = _storeService.GetAppState().Copy();
            state.accessToken = "tok-123";
            _storeService.SetAppState(state);

            string action = await InitializeIgnoringLogsAsync();

            Assert.AreEqual(Actions.loading_to_home, action);
        }

        [Test]
        public async Task InitializeAppAsync_StepFails_WithoutSession_GoesToAuth()
        {
            string action = await InitializeIgnoringLogsAsync();

            Assert.AreEqual(Actions.loading_to_auth, action);
        }

        private async Task<string> InitializeIgnoringLogsAsync()
        {
            bool previousIgnore = UnityEngine.TestTools.LogAssert.ignoreFailingMessages;
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try
            {
                return await _vm.InitializeAppAsync();
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = previousIgnore;
            }
        }
    }
}
