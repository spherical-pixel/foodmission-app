using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class BannerServiceTests
    {
        private IBannerService _bannerService;

        [SetUp]
        public void SetUp()
        {
            _bannerService = new BannerService();
        }

        [TearDown]
        public void TearDown()
        {
            _bannerService?.Dispose();
        }

        [Test]
        public void GetDefaultBannerAddress_ReturnsDimensionsDefault()
        {
            Assert.AreEqual("dimensions/default", _bannerService.GetDefaultBannerAddress());
        }

        [Test]
        public void GetDimensionBannerAddress_WithValidCode_ReturnsFormattedAddress()
        {
            string address = _bannerService.GetDimensionBannerAddress("DIET_CHANGES");
            Assert.AreEqual("dimensions/diet_changes", address);
        }

        [Test]
        public void GetDimensionBannerAddress_WithEmptyOrNull_ReturnsDefaultAddress()
        {
            Assert.AreEqual("dimensions/default", _bannerService.GetDimensionBannerAddress(null));
            Assert.AreEqual("dimensions/default", _bannerService.GetDimensionBannerAddress(""));
        }

        [Test]
        public void GetTopicBannerAddress_WithValidCode_ReturnsFormattedAddress()
        {
            string address = _bannerService.GetTopicBannerAddress("REDUCING_MEAT_CONSUMPTION");
            Assert.AreEqual("topics/reducing_meat_consumption", address);
        }

        [Test]
        public void GetTopicBannerAddress_WithEmptyOrNull_ReturnsDefaultAddress()
        {
            Assert.AreEqual("dimensions/default", _bannerService.GetTopicBannerAddress(null));
            Assert.AreEqual("dimensions/default", _bannerService.GetTopicBannerAddress(""));
        }

        [Test]
        public void GetKnowledgeBannerAddress_WithValidId_ReturnsFormattedAddress()
        {
            Assert.AreEqual("knowledge/quiz", _bannerService.GetKnowledgeBannerAddress("quiz"));
            Assert.AreEqual("knowledge/foodfacts", _bannerService.GetKnowledgeBannerAddress("FOODFACTS"));
        }

        [Test]
        public void GetKnowledgeBannerAddress_WithEmptyOrNull_ReturnsDefaultAddress()
        {
            Assert.AreEqual("dimensions/default", _bannerService.GetKnowledgeBannerAddress(null));
            Assert.AreEqual("dimensions/default", _bannerService.GetKnowledgeBannerAddress(""));
        }

        [Test]
        public async Task BindBanner_WithNullTargetImage_ReturnsFalse()
        {
            bool result = await _bannerService.BindBanner(null, "dimensions/diet_changes");
            Assert.IsFalse(result);
        }

        [Test]
        public async Task BindBanner_WithNullOrEmptyAddress_HidesImageAndReturnsFalse()
        {
            var image = new Image();
            bool result = await _bannerService.BindBanner(image, null);
            Assert.IsFalse(result);
            Assert.IsNull(image.sprite);
            Assert.AreEqual(DisplayStyle.None, image.style.display.value);
        }

        [Test]
        public void IsBannerLoaded_WhenNotLoaded_ReturnsFalse()
        {
            Assert.IsFalse(_bannerService.IsBannerLoaded("dimensions/diet_changes"));
            Assert.IsNull(_bannerService.GetCachedSprite("dimensions/diet_changes"));
        }

        [Test]
        public void ClearCache_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _bannerService.ClearCache());
        }
    }
}
