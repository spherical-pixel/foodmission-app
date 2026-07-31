using NUnit.Framework;
using System.Threading.Tasks;
using UnityEngine;
using eu.foodmission.platform;
using eu.foodmission.platform.Tests;

namespace eu.foodmission.tests
{
    [TestFixture]
    public class AvatarFaceTextureTests
    {
        private TestStoreService _storeService;

        [SetUp]
        public void SetUp()
        {
            _storeService = new TestStoreService();
            PlayerPrefs.DeleteKey("AvatarConfig");
            PlayerPrefs.DeleteKey("HasAvatar");
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("AvatarConfig");
            PlayerPrefs.DeleteKey("HasAvatar");
        }

        [Test]
        public void GetFaceTexture_ReturnsNull_WhenHasAvatarIsFalse()
        {
            var avatarService = new AvatarService(_storeService, null);
            Assert.IsFalse(avatarService.HasAvatar);
            Assert.IsNull(avatarService.GetFaceTexture());
        }

        [Test]
        public void ClearFaceTexture_FiresOnFaceTextureChanged()
        {
            var avatarService = new AvatarService(_storeService, null);
            bool fired = false;
            avatarService.OnFaceTextureChanged += () => fired = true;

            avatarService.ClearFaceTexture();
            Assert.IsTrue(fired);
            Assert.IsNull(avatarService.GetFaceTexture());
        }

        [Test]
        public async Task SaveCurrentConfigAsync_WithHasAvatarFalse_ClearsFaceTexture()
        {
            var avatarService = new AvatarService(_storeService, null);
            bool eventFired = false;
            avatarService.OnFaceTextureChanged += () => eventFired = true;

            await avatarService.SetHasAvatarAsync(false);

            Assert.IsTrue(eventFired);
            Assert.IsFalse(avatarService.HasAvatar);
            Assert.IsNull(avatarService.GetFaceTexture());
        }
    }
}
