using System;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.AppUI.Redux;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class SettingsViewModelTests
    {
        private Mock<IAuthService> _mockAuthService;
        private Mock<ICatalogService> _mockCatalogService;
        private Mock<IAudioService> _mockAudioService;
        private Mock<INotificationService> _mockNotificationService;
        private TestStoreService _storeService;
        private SettingsViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockCatalogService = new Mock<ICatalogService>();
            _mockAudioService = new Mock<IAudioService>();
            _mockNotificationService = new Mock<INotificationService>();
            _mockNotificationService.Setup(n => n.RequestPermissionsAsync()).ReturnsAsync(true);
            _storeService = new TestStoreService();
            _vm = new SettingsViewModel(_storeService, _mockAuthService.Object, _mockCatalogService.Object, _mockAudioService.Object, _mockNotificationService.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _vm?.Dispose();
            _storeService?.Dispose();
        }

        [Test]
        public void Constructor_InitializesWithStoreDefaults()
        {
            Assert.AreEqual("system", _vm.Theme);
            Assert.AreEqual("none", _vm.Lang);
            Assert.AreEqual("medium", _vm.Scale);
            Assert.AreEqual("roboto", _vm.Font);
            Assert.AreEqual(100, _vm.Sound);
            Assert.AreEqual(100, _vm.Music);
            Assert.IsFalse(_vm.PushNotifications);
            Assert.AreEqual("10:00", _vm.NotificationPreferredTime);
            Assert.IsTrue(_vm.BackgroundPattern);
            Assert.AreEqual("", _vm.UserName);
        }

        [Test]
        public void Constructor_LoadsStateFromStore()
        {
            _storeService.Dispose();
            _storeService = new TestStoreService();
            var state = _storeService.GetAppState();
            state.theme = "dark";
            state.lang = "en";
            state.scale = "large";
            state.font = "open-sans";
            state.soundVolume = 50;
            state.musicVolume = 75;
            state.pushNotificationsEnabled = true;
            state.notificationPreferredTime = "09:00";
            state.backgroundPattern = false;
            state.userName = "TestUser";
            _storeService.SetAppState(state);

            _vm?.Dispose();
            _vm = new SettingsViewModel(_storeService, _mockAuthService.Object, _mockCatalogService.Object, _mockAudioService.Object, _mockNotificationService.Object);

            Assert.AreEqual("dark", _vm.Theme);
            Assert.AreEqual("en", _vm.Lang);
            Assert.AreEqual("large", _vm.Scale);
            Assert.AreEqual("open-sans", _vm.Font);
            Assert.AreEqual(50, _vm.Sound);
            Assert.AreEqual(75, _vm.Music);
            Assert.IsTrue(_vm.PushNotifications);
            Assert.AreEqual("09:00", _vm.NotificationPreferredTime);
            Assert.IsFalse(_vm.BackgroundPattern);
            Assert.AreEqual("TestUser", _vm.UserName);
        }

        [Test]
        public void SetTheme_DispatchesSetThemeAction()
        {
            _vm.SetTheme("dark");

            Assert.Contains("app/setTheme", _storeService.DispatchedActionTypes);
        }

        [Test]
        public void SetLanguage_DispatchesSetLanguageAction()
        {
            _vm.SetLanguage("en");

            Assert.Contains("app/setLanguage", _storeService.DispatchedActionTypes);
        }

        [Test]
        public void SetScale_DispatchesSetScaleAction()
        {
            _vm.SetScale("large");

            Assert.Contains("app/setScale", _storeService.DispatchedActionTypes);
        }

        [Test]
        public void SetFont_DispatchesSetFontAction()
        {
            _vm.SetFont("open-sans");

            Assert.Contains("app/setFont", _storeService.DispatchedActionTypes);
        }

        [Test]
        public void SetSound_DispatchesSetSoundActionAndCallsAudioService()
        {
            _vm.SetSound(50);

            Assert.Contains("app/setSound", _storeService.DispatchedActionTypes);
            _mockAudioService.Verify(x => x.SetSoundVolume(50), Times.Once);
        }

        [Test]
        public void SetMusic_DispatchesSetMusicActionAndCallsAudioService()
        {
            _vm.SetMusic(25);

            Assert.Contains("app/setMusic", _storeService.DispatchedActionTypes);
            _mockAudioService.Verify(x => x.SetMusicVolume(25), Times.Once);
        }

        [Test]
        public void SetPushNotifications_DispatchesSetPushNotificationsActionAndReschedules()
        {
            _vm.SetNotificationPreferredTime("11:00");
            _vm.SetPushNotifications(true);

            Assert.Contains("app/setPushNotifications", _storeService.DispatchedActionTypes);
            _mockNotificationService.Verify(n => n.SetNotificationsEnabled(true), Times.Once);
            _mockNotificationService.Verify(n => n.RescheduleAllNotifications(TimeSpan.FromHours(11)), Times.Once);
        }

        [Test]
        public void SetNotificationPreferredTime_DispatchesActionAndReschedulesIfNotificationsEnabled()
        {
            _vm.SetPushNotifications(true);
            _vm.SetNotificationPreferredTime("14:00");

            Assert.Contains("app/setNotificationPreferredTime", _storeService.DispatchedActionTypes);
            Assert.AreEqual("14:00", _vm.NotificationPreferredTime);
            _mockNotificationService.Verify(n => n.RescheduleAllNotifications(TimeSpan.FromHours(14)), Times.Once);
        }

        [Test]
        public void SetNotificationPreferredTime_NullOrEmpty_DefaultsTo10Am()
        {
            _vm.SetNotificationPreferredTime(null);

            Assert.AreEqual("10:00", _vm.NotificationPreferredTime);
            Assert.Contains("app/setNotificationPreferredTime", _storeService.DispatchedActionTypes);
        }

        [Test]
        public void SetBackgroundPattern_DispatchesSetBackgroundPatternAction()
        {
            _vm.SetBackgroundPattern(false);

            Assert.Contains("app/setBackgroundPattern", _storeService.DispatchedActionTypes);
        }

        [Test]
        public void Logout_CallsAuthServiceLogout()
        {
            _vm.Logout();

            _mockAuthService.Verify(a => a.Logout(), Times.Once());
        }

        [Test]
        public void Logout_RaisesNavigationRequested()
        {
            bool navigationFired = false;
            string capturedAction = null;
            _vm.NavigationRequested += (action, args) =>
            {
                navigationFired = true;
                capturedAction = action;
            };

            _vm.Logout();

            Assert.IsTrue(navigationFired);
            Assert.AreEqual("go_to_auth", capturedAction);
        }

        [Test]
        public void MultipleSetCalls_DispatchesActionsInOrder()
        {
            _vm.SetTheme("dark");
            _vm.SetLanguage("en");
            _vm.SetScale("large");
            _vm.SetFont("open-dyslexic");

            var types = _storeService.DispatchedActionTypes;
            int count = types.Count;
            Assert.AreEqual("app/setTheme", types[count - 4]);
            Assert.AreEqual("app/setLanguage", types[count - 3]);
            Assert.AreEqual("app/setScale", types[count - 2]);
            Assert.AreEqual("app/setFont", types[count - 1]);
        }

        [Test]
        public void Dispose_DoesNotThrowOnDoubleDispose()
        {
            _vm.Dispose();

            Assert.DoesNotThrow(() => _vm.Dispose());
        }

        [Test]
        public async Task SetTheme_SyncSettingsCalledAfterDelay()
        {
            _mockAuthService
                .Setup(x => x.SyncSettingsAsync())
                .Returns(Task.CompletedTask);

            _vm.SetTheme("dark");

            await Task.Delay(700);

            _mockAuthService.Verify(x => x.SyncSettingsAsync(), Times.AtLeastOnce);
        }

        [Test]
        public void SetTheme_QuickConsecutive_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                _vm.SetTheme("dark");
                _vm.SetTheme("light");
                _vm.SetTheme("system");
            });
        }

        [Test]
        public void SyncAfterDelay_OperationCanceledException_IsCaught()
        {
            _mockAuthService
                .Setup(x => x.SyncSettingsAsync())
                .ThrowsAsync(new OperationCanceledException());

            Assert.DoesNotThrow(() =>
            {
                _vm.SetTheme("dark");
            });
        }

        [Test]
        public void Constructor_WithNullUserNameInStore_FallsBackToUser()
        {
            _storeService.Dispose();
            _storeService = new TestStoreService();
            var state = _storeService.GetAppState();
            state.userName = null;
            _storeService.SetAppState(state);

            _vm?.Dispose();
            _vm = new SettingsViewModel(_storeService, _mockAuthService.Object, _mockCatalogService.Object);

            Assert.AreEqual("User", _vm.UserName);
        }
    }
}
