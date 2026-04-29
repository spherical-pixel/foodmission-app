using NUnit.Framework;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    /// <summary>
    /// Integration tests for StoreService + LocalStorageService.
    /// Verifies that dispatched actions are auto-persisted to storage.
    /// Reads back via LocalStorageService to avoid creating multiple StoreService
    /// instances (which would trigger the UNITY_EDITOR DevTools enhancer).
    /// </summary>
    [TestFixture]
    public class StoreServiceIntegrationTests
    {
        private LocalStorageService _localStorage;
        private StoreService _storeService;

        private const string STATE_KEY = "app_state";
        private const string FULL_STATE_KEY = "FM_" + STATE_KEY;

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(FULL_STATE_KEY);
            _localStorage = new LocalStorageService();
            _storeService = new StoreService(_localStorage);
        }

        [TearDown]
        public void TearDown()
        {
            _storeService?.Dispose();
            PlayerPrefs.DeleteKey(FULL_STATE_KEY);
        }

        // ── Initial state ─────────────────────────────────────────────────────

        [Test]
        public void InitialState_HasExpectedDefaults()
        {
            var state = _storeService.GetAppState();

            Assert.AreEqual("system", state.theme);
            Assert.AreEqual("en", state.lang);
            Assert.AreEqual("medium", state.scale);
            Assert.AreEqual("roboto", state.font);
            Assert.IsEmpty(state.userId);
            Assert.IsFalse(state.isAuthenticating);
            Assert.IsFalse(state.hasCompletedOnboarding);
        }

        // ── Auto-persist on dispatch ──────────────────────────────────────────

        [Test]
        public void Dispatch_WritesToPlayerPrefs()
        {
            _storeService.store.Dispatch(AppActions.setTheme.Invoke("dark"));

            Assert.IsTrue(PlayerPrefs.HasKey(FULL_STATE_KEY));
        }

        [Test]
        public void Dispatch_PreferenceChanges_PersistedToStorage()
        {
            _storeService.store.Dispatch(AppActions.setTheme.Invoke("dark"));
            _storeService.store.Dispatch(AppActions.setLanguage.Invoke("en"));
            _storeService.store.Dispatch(AppActions.setScale.Invoke("large"));

            var persisted = _localStorage.GetValue<AppState>(STATE_KEY);
            Assert.AreEqual("dark", persisted.theme);
            Assert.AreEqual("en", persisted.lang);
            Assert.AreEqual("large", persisted.scale);
        }

        // ── Session persistence ───────────────────────────────────────────────

        [Test]
        public void Dispatch_LoginSuccess_SessionPersistedToStorage()
        {
            var payload = new AppActions.LoginPayload(
                userId: "user-123",
                userName: "testuser",
                email: "test@example.com",
                accessToken: "token-abc",
                tokenType: "Bearer",
                expiresAt: 9999999,
                refreshToken: "refresh-xyz"
            );
            _storeService.store.Dispatch(AppActions.loginSuccess.Invoke(payload));

            var persisted = _localStorage.GetValue<AppState>(STATE_KEY);
            Assert.AreEqual("user-123", persisted.userId);
            Assert.AreEqual("testuser", persisted.userName);
            Assert.AreEqual("token-abc", persisted.accessToken);
            Assert.AreEqual("Bearer", persisted.tokenType);
            Assert.AreEqual(9999999, persisted.tokenExpiresAt);
            Assert.AreEqual("refresh-xyz", persisted.refreshToken);
        }

        [Test]
        public void Dispatch_Logout_ClearsSessionInStorage()
        {
            var payload = new AppActions.LoginPayload(
                userId: "user-123",
                userName: "testuser",
                email: "test@example.com",
                accessToken: "token-abc",
                tokenType: "Bearer",
                expiresAt: 9999999,
                refreshToken: "refresh-xyz"
            );
            _storeService.store.Dispatch(AppActions.loginSuccess.Invoke(payload));
            _storeService.store.Dispatch(AppActions.logout.Invoke());

            var persisted = _localStorage.GetValue<AppState>(STATE_KEY);
            Assert.IsEmpty(persisted.userId);
            Assert.IsEmpty(persisted.accessToken);
            Assert.IsEmpty(persisted.refreshToken);
        }

        [Test]
        public void Dispatch_TokenRefreshed_NewTokenPersistedToStorage()
        {
            var loginPayload = new AppActions.LoginPayload(
                userId: "user-123", userName: "testuser", email: "test@example.com",
                accessToken: "old-token", tokenType: "Bearer", expiresAt: 1000,
                refreshToken: "old-refresh"
            );
            _storeService.store.Dispatch(AppActions.loginSuccess.Invoke(loginPayload));

            var refreshPayload = new AppActions.TokenRefreshPayload(
                accessToken: "new-token",
                tokenType: "Bearer",
                expiresAt: 9999,
                refreshToken: "new-refresh"
            );
            _storeService.store.Dispatch(AppActions.tokenRefreshed.Invoke(refreshPayload));

            var persisted = _localStorage.GetValue<AppState>(STATE_KEY);
            Assert.AreEqual("new-token", persisted.accessToken);
            Assert.AreEqual("new-refresh", persisted.refreshToken);
            Assert.AreEqual(9999, persisted.tokenExpiresAt);
            Assert.AreEqual("user-123", persisted.userId); // identity preserved
        }

        // ── Onboarding flags ──────────────────────────────────────────────────

        [Test]
        public void Dispatch_CompleteOnboarding_PersistedToStorage()
        {
            _storeService.store.Dispatch(AppActions.completeOnboarding.Invoke());

            var persisted = _localStorage.GetValue<AppState>(STATE_KEY);
            Assert.IsTrue(persisted.hasCompletedOnboarding);
        }

        [Test]
        public void Dispatch_SetExtendedProfile_PersistedToStorage()
        {
            _storeService.store.Dispatch(AppActions.setExtendedProfile.Invoke());

            var persisted = _localStorage.GetValue<AppState>(STATE_KEY);
            Assert.IsTrue(persisted.hasCompletedExtendedProfile);
        }

        // ── RestoreAppState ───────────────────────────────────────────────────

        [Test]
        public void RestoreAppState_ReloadsCurrentPersistedState()
        {
            _storeService.store.Dispatch(AppActions.setTheme.Invoke("dark"));

            _storeService.RestoreAppState();

            Assert.AreEqual("dark", _storeService.GetAppState().theme);
        }

        // ── SaveAppState ──────────────────────────────────────────────────────

        [Test]
        public void SaveAppState_WritesCurrentStateToStorage()
        {
            _storeService.store.Dispatch(AppActions.setTheme.Invoke("dark"));
            _storeService.SaveAppState();

            Assert.IsTrue(PlayerPrefs.HasKey(FULL_STATE_KEY));
        }

        // ── GetAppState reflects dispatched actions ────────────────────────────

        [Test]
        public void GetAppState_ReflectsDispatchedActions()
        {
            _storeService.store.Dispatch(AppActions.setTheme.Invoke("light"));
            _storeService.store.Dispatch(AppActions.setLanguage.Invoke("ca"));

            var state = _storeService.GetAppState();
            Assert.AreEqual("light", state.theme);
            Assert.AreEqual("ca", state.lang);
        }

        // ── Dispose ───────────────────────────────────────────────────────────

        [Test]
        public void Dispose_CanBeCalledMultipleTimes_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                _storeService.Dispose();
                _storeService.Dispose();
            });
        }
    }
}
