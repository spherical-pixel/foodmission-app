using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class WhatsNewServiceTests
    {
        private TestLocalStorageService _localStorage;
        private IWhatsNewService _service;

        [SetUp]
        public void SetUp()
        {
            _localStorage = new TestLocalStorageService();
            _service = new WhatsNewService(_localStorage);
        }

        [TearDown]
        public void TearDown()
        {
            _localStorage.DeleteAll();
        }

        [Test]
        public async Task MarkAsSeenAsync_StoresCurrentVersion()
        {
            string expectedVersion = Application.version;

            await _service.MarkAsSeenAsync();

            string stored = _localStorage.GetValue<string>("whats_new_last_seen_version", "");
            Assert.AreEqual(expectedVersion, stored);
        }

        [Test]
        public async Task CheckShouldShowAsync_ReturnsTrueAndReleaseNotes_WhenVersionNotSeen()
        {
            _localStorage.DeleteValue("whats_new_last_seen_version");

            var (shouldShow, notes) = await _service.CheckShouldShowAsync();

            // Editor has network access: downloads real JSON, returns true with release notes.
            // On devices without network, returns (false, null) and retries next launch.
            Assert.IsTrue(shouldShow);
            Assert.IsNotNull(notes);
        }

        [Test]
        public async Task CheckShouldShowAsync_ReturnsFalse_WhenVersionAlreadySeen()
        {
            _localStorage.SetValue("whats_new_last_seen_version", Application.version);

            var (shouldShow, notes) = await _service.CheckShouldShowAsync();

            Assert.IsFalse(shouldShow);
            Assert.IsNull(notes);
        }
    }
}
