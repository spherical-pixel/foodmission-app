using System.Collections.Generic;

using NUnit.Framework;

namespace eu.foodmission.platform.Tests
{
    /// <summary>The device's user caches are wiped whenever the signed-in user changes, however the session ended.</summary>
    [TestFixture]
    public class StoreServiceUserDataTests
    {
        private TestLocalStorageService _storage;
        private StoreService _storeService;

        [SetUp]
        public void SetUp()
        {
            _storage = new TestLocalStorageService();
        }

        [TearDown]
        public void TearDown()
        {
            _storeService?.Dispose();
        }

        private void StartWith(string userId, string lastUserOnDevice)
        {
            _storage.SetValue(StoreService.APP_STATE_KEY, new AppState { userId = userId });
            if (lastUserOnDevice != null)
            {
                _storage.SetValue(StoreService.LastUserKey, lastUserOnDevice);
            }
            _storage.SetValue("pantry_cache", new List<string> { "cached" });
            _storeService = new StoreService(_storage);
        }

        private static AppActions.LoginPayload Login(string userId) =>
            new AppActions.LoginPayload(userId, "name", "mail@test.com", "token", "Bearer", 0, "refresh");

        private bool CacheKept => _storage.GetValue<List<string>>("pantry_cache") != null;

        [Test]
        public void Logout_FromAnyPath_ClearsTheUserCaches()
        {
            // EditProfile (delete account) and the legal consent prompt dispatch logout directly
            StartWith("u1", "u1");

            _storeService.store.Dispatch(AppActions.logout.Invoke());

            Assert.IsFalse(CacheKept);
        }

        [Test]
        public void LoginOfAnotherUser_ClearsWhatTheLastUserLeft()
        {
            // The app was killed before the previous session's logout finished
            StartWith("", "u1");

            _storeService.store.Dispatch(AppActions.loginSuccess.Invoke(Login("u2")));

            Assert.IsFalse(CacheKept);
            Assert.AreEqual("u2", _storage.GetValue<string>(StoreService.LastUserKey));
        }

        [Test]
        public void SameUser_KeepsTheCaches()
        {
            StartWith("u1", "u1");

            _storeService.store.Dispatch(AppActions.loginSuccess.Invoke(Login("u1")));

            Assert.IsTrue(CacheKept);
        }
    }
}
