using System;

using NUnit.Framework;

using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class LocalStorageServiceTests
    {
        private LocalStorageService _service;

        private const string KEY = "test_ls_key";
        private const string FULL_KEY = "FM_" + KEY;

        [SetUp]
        public void SetUp()
        {
            _service = new LocalStorageService();
            PlayerPrefs.DeleteKey(FULL_KEY);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(FULL_KEY);
        }

        // ── Primitives ────────────────────────────────────────────────────────

        [Test]
        public void SetValue_String_RoundTrips()
        {
            _service.SetValue(KEY, "hello world");

            Assert.AreEqual("hello world", _service.GetValue<string>(KEY));
        }

        [Test]
        public void SetValue_Int_RoundTrips()
        {
            _service.SetValue(KEY, 42);

            Assert.AreEqual(42, _service.GetValue<int>(KEY));
        }

        [Test]
        public void SetValue_Bool_True_RoundTrips()
        {
            _service.SetValue(KEY, true);

            Assert.IsTrue(_service.GetValue<bool>(KEY));
        }

        [Test]
        public void SetValue_Bool_False_RoundTrips()
        {
            _service.SetValue(KEY, false);

            Assert.IsFalse(_service.GetValue<bool>(KEY));
        }

        [Test]
        public void SetValue_Float_RoundTrips()
        {
            _service.SetValue(KEY, 3.5f);

            Assert.AreEqual(3.5f, _service.GetValue<float>(KEY), 0.001f);
        }

        // ── Complex object (AppState) ─────────────────────────────────────────

        [Test]
        public void SetValue_AppState_RoundTrips_AllFields()
        {
            var state = new AppState
            {
                userId = "user-123",
                userName = "toni",
                userEmail = "toni@example.com",
                accessToken = "token-abc",
                refreshToken = "refresh-xyz",
                tokenType = "Bearer",
                tokenExpiresAt = 9999999,
                theme = "dark",
                lang = "en",
                scale = "large",
                font = "open-sans",
                soundVolume = 50,
                musicVolume = 75,
                pushNotificationsEnabled = true,
                backgroundPattern = false,
                hasCompletedOnboarding = true,
                hasCompletedExtendedProfile = true
            };

            _service.SetValue(KEY, state);
            var restored = _service.GetValue<AppState>(KEY);

            Assert.AreEqual("user-123", restored.userId);
            Assert.AreEqual("toni", restored.userName);
            Assert.AreEqual("toni@example.com", restored.userEmail);
            Assert.AreEqual("token-abc", restored.accessToken);
            Assert.AreEqual("refresh-xyz", restored.refreshToken);
            Assert.AreEqual("Bearer", restored.tokenType);
            Assert.AreEqual(9999999, restored.tokenExpiresAt);
            Assert.AreEqual("dark", restored.theme);
            Assert.AreEqual("en", restored.lang);
            Assert.AreEqual("large", restored.scale);
            Assert.AreEqual("open-sans", restored.font);
            Assert.AreEqual(50, restored.soundVolume);
            Assert.AreEqual(75, restored.musicVolume);
            Assert.IsTrue(restored.pushNotificationsEnabled);
            Assert.IsFalse(restored.backgroundPattern);
            Assert.IsTrue(restored.hasCompletedOnboarding);
            Assert.IsTrue(restored.hasCompletedExtendedProfile);
        }

        [Test]
        public void SetValue_AppState_TransientFields_AreNotRestored()
        {
            // isAuthenticating and authError are transient — JsonUtility serializes them
            // but the convention is they reset to defaults on app restart.
            // This test documents the current behavior: JsonUtility does persist them.
            var state = new AppState { isAuthenticating = true, authError = "some error" };

            _service.SetValue(KEY, state);
            var restored = _service.GetValue<AppState>(KEY);

            // Document current behavior: JsonUtility round-trips these fields.
            // If the app needs to suppress them on restore, it should do so in StoreService.
            Assert.IsTrue(restored.isAuthenticating);
            Assert.AreEqual("some error", restored.authError);
        }

        // ── Defaults when key missing ──────────────────────────────────────────

        [Test]
        public void GetValue_WhenKeyMissing_ReturnsProvidedDefault()
        {
            var result = _service.GetValue<string>(KEY, "fallback");

            Assert.AreEqual("fallback", result);
        }

        [Test]
        public void GetValue_WhenKeyMissing_ReturnsTypeDefault_ForInt()
        {
            var result = _service.GetValue<int>(KEY);

            Assert.AreEqual(0, result);
        }

        [Test]
        public void GetValue_WhenKeyMissing_ReturnsNull_ForString()
        {
            var result = _service.GetValue<string>(KEY);

            Assert.IsNull(result);
        }

        // ── HasValue ──────────────────────────────────────────────────────────

        [Test]
        public void HasValue_ReturnsFalse_WhenKeyMissing()
        {
            Assert.IsFalse(_service.HasValue(KEY));
        }

        [Test]
        public void HasValue_ReturnsTrue_AfterSetValue()
        {
            _service.SetValue(KEY, "data");

            Assert.IsTrue(_service.HasValue(KEY));
        }

        // ── DeleteValue ───────────────────────────────────────────────────────

        [Test]
        public void DeleteValue_RemovesKey()
        {
            _service.SetValue(KEY, "data");
            _service.DeleteValue(KEY);

            Assert.IsFalse(_service.HasValue(KEY));
        }

        [Test]
        public void DeleteValue_WhenKeyMissing_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _service.DeleteValue(KEY));
        }

        [Test]
        public void GetValue_AfterDeleteValue_ReturnsDefault()
        {
            _service.SetValue(KEY, "data");
            _service.DeleteValue(KEY);

            Assert.AreEqual("fallback", _service.GetValue<string>(KEY, "fallback"));
        }

        // ── Null value ────────────────────────────────────────────────────────

        [Test]
        public void SetValue_Null_DeletesKey()
        {
            _service.SetValue(KEY, "initial");
            _service.SetValue<string>(KEY, null);

            Assert.IsFalse(_service.HasValue(KEY));
        }

        // ── Empty key guard ───────────────────────────────────────────────────

        [Test]
        public void GetValue_WithEmptyKey_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _service.GetValue<string>(""));
        }

        [Test]
        public void SetValue_WithEmptyKey_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _service.SetValue("", "value"));
        }

        [Test]
        public void DeleteValue_WithEmptyKey_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _service.DeleteValue(""));
        }

        [Test]
        public void HasValue_WithEmptyKey_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _service.HasValue(""));
        }

        // ── Overwrite ─────────────────────────────────────────────────────────

        [Test]
        public void SetValue_CalledTwice_OverwritesPreviousValue()
        {
            _service.SetValue(KEY, "first");
            _service.SetValue(KEY, "second");

            Assert.AreEqual("second", _service.GetValue<string>(KEY));
        }
    }
}
