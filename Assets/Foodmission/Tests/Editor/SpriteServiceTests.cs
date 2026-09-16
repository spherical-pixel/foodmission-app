using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class SpriteServiceTests
    {
        private ISpriteService _spriteService;

        [SetUp]
        public void SetUp()
        {
            _spriteService = new SpriteService();
        }

        [TearDown]
        public void TearDown()
        {
            _spriteService?.Dispose();
        }

        [Test]
        public void GetCachedSprite_WithNullOrEmptyAddress_ReturnsNull()
        {
            Assert.IsNull(_spriteService.GetCachedSprite(null));
            Assert.IsNull(_spriteService.GetCachedSprite(""));
        }

        [Test]
        public void GetCachedSprite_WhenNotLoaded_ReturnsNull()
        {
            Assert.IsNull(_spriteService.GetCachedSprite("dimensions/diet_changes"));
            Assert.IsNull(_spriteService.GetCachedSprite("non_existent_sprite"));
        }

        [Test]
        public void IsSpriteLoaded_WithNullOrEmptyAddress_ReturnsFalse()
        {
            Assert.IsFalse(_spriteService.IsSpriteLoaded(null));
            Assert.IsFalse(_spriteService.IsSpriteLoaded(""));
        }

        [Test]
        public void IsSpriteLoaded_WhenNotLoaded_ReturnsFalse()
        {
            Assert.IsFalse(_spriteService.IsSpriteLoaded("dimensions/diet_changes"));
            Assert.IsFalse(_spriteService.IsSpriteLoaded("icons/xp"));
        }

        [Test]
        public async Task BindSprite_WithNullTargetImage_ReturnsFalse()
        {
            bool result = await _spriteService.BindSprite(null, "dimensions/diet_changes");
            Assert.IsFalse(result);
        }

        [Test]
        public async Task BindSprite_WithNullOrEmptyAddress_HidesImageAndReturnsFalse()
        {
            var image = new Image();
            bool result = await _spriteService.BindSprite(image, null);
            Assert.IsFalse(result);
            Assert.IsNull(image.sprite);
            Assert.AreEqual(DisplayStyle.None, image.style.display.value);
        }

        [Test]
        public async Task BindBackgroundSprite_WithNullTargetElement_ReturnsFalse()
        {
            bool result = await _spriteService.BindBackgroundSprite(null, "dimensions/diet_changes");
            Assert.IsFalse(result);
        }

        [Test]
        public async Task BindBackgroundSprite_WithNullOrEmptyAddress_ClearsBackgroundAndReturnsFalse()
        {
            var element = new VisualElement();
            bool result = await _spriteService.BindBackgroundSprite(element, null);
            Assert.IsFalse(result);
            Assert.AreEqual(StyleKeyword.None, element.style.backgroundImage.keyword);
        }

        [Test]
        public void ReleaseSprite_WithNullOrEmptyAddress_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _spriteService.ReleaseSprite(null));
            Assert.DoesNotThrow(() => _spriteService.ReleaseSprite(""));
        }

        [Test]
        public void ReleaseSprite_WithNotLoadedAddress_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _spriteService.ReleaseSprite("dimensions/diet_changes"));
        }

        [Test]
        public void ClearCache_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _spriteService.ClearCache());
        }

        [Test]
        public void Dispose_ClearsCacheWithoutThrowing()
        {
            Assert.DoesNotThrow(() => _spriteService.Dispose());
        }
    }
}
