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

        [Test]
        public void AvatarConfig_CreateDefault_ReturnsValidDeterministicConfig()
        {
            var config = AvatarConfig.CreateDefault();
            Assert.IsNotNull(config);
            Assert.IsNotNull(config.hair);
            Assert.IsNotNull(config.eyebrows);
            Assert.IsNotNull(config.eyes);
            Assert.IsNotNull(config.nose);
            Assert.IsNotNull(config.mouth);
            Assert.IsNotNull(config.facialHair);
            Assert.IsNotNull(config.skin);
            Assert.IsNotNull(config.tshirt);
            Assert.IsNotNull(config.trousers);
            Assert.IsNotNull(config.shoes);

            Assert.AreEqual(5, config.hair.idPart);
            Assert.AreEqual(5, config.hair.idColor);
            Assert.AreEqual(1, config.skin.idPart);
            Assert.AreEqual(1, config.skin.idColor);
        }

        [Test]
        public void AvatarService_GetDefaultConfig_ReturnsValidDefaultConfig()
        {
            var avatarService = new AvatarService(_storeService, null);
            var defaultConfig = avatarService.GetDefaultConfig();
            Assert.IsNotNull(defaultConfig);
            Assert.AreEqual(5, defaultConfig.hair.idPart);
        }
    }
}
