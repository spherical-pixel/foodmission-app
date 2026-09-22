using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class WhatsNewServiceTests
    {
        private const string NotesJson = "{\"releaseNotes\":\"English notes\",\"releaseNotes_es\":\"Spanish notes\"}";

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
        public async Task CheckShouldShowAsync_ReturnsTrueWithNotes_WhenVersionNotSeenAndNotesExist()
        {
            _localStorage.DeleteValue("whats_new_last_seen_version");
            _service = new WhatsNewService(_localStorage, _ => Task.FromResult(NotesJson));

            var (shouldShow, notes) = await _service.CheckShouldShowAsync();

            Assert.IsTrue(shouldShow);
            Assert.AreEqual("English notes", notes);
        }

        [Test]
        public async Task CheckShouldShowAsync_ReturnsFalse_WhenDownloadFails()
        {
            _localStorage.DeleteValue("whats_new_last_seen_version");
            _service = new WhatsNewService(_localStorage, _ => Task.FromResult<string>(null));

            var (shouldShow, notes) = await _service.CheckShouldShowAsync();

            Assert.IsFalse(shouldShow);
            Assert.IsNull(notes);
        }

        [Test]
        public async Task CheckShouldShowAsync_ReturnsFalse_WhenNotesAreEmpty()
        {
            _localStorage.DeleteValue("whats_new_last_seen_version");
            _service = new WhatsNewService(_localStorage, _ => Task.FromResult("{\"releaseNotes\":\"\"}"));

            var (shouldShow, notes) = await _service.CheckShouldShowAsync();

            Assert.IsFalse(shouldShow);
            Assert.IsNull(notes);
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