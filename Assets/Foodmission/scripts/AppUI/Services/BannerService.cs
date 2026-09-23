using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Domain-specific banner service delegating generic sprite loading and caching to ISpriteService.
    /// Manages taxonomy-to-address resolution for dimensions, topics, and knowledge hub sections.
    /// </summary>
    public class BannerService : IBannerService
    {
        private readonly ISpriteService _spriteService;
        private readonly IDimensionService _dimensionService;

        public BannerService(ISpriteService spriteService = null, IDimensionService dimensionService = null)
        {
            _spriteService = spriteService ?? new SpriteService();
            _dimensionService = dimensionService;
        }

        // ── Address Resolution ──────────────────────────────────────────────

        public string GetDimensionBannerAddress(string dimensionCodeOrId)
        {
            if (string.IsNullOrEmpty(dimensionCodeOrId))
                return GetDefaultBannerAddress();

            Dimension dim = _dimensionService?.GetDimension(dimensionCodeOrId);
            string dimCode = dim?.code ?? dimensionCodeOrId;

            if (string.IsNullOrEmpty(dimCode))
                return GetDefaultBannerAddress();

            return $"dimensions/{dimCode.ToLowerInvariant()}";
        }

        public string GetTopicBannerAddress(string topicCodeOrId)
        {
            if (string.IsNullOrEmpty(topicCodeOrId))
                return GetDefaultBannerAddress();

            Topic topic = _dimensionService?.GetTopic(topicCodeOrId);
            string topicCode = topic?.code ?? topicCodeOrId;

            if (string.IsNullOrEmpty(topicCode))
                return GetDefaultBannerAddress();

            return $"topics/{topicCode.ToLowerInvariant()}";
        }

        public string GetKnowledgeBannerAddress(string sectionId)
        {
            if (string.IsNullOrEmpty(sectionId))
                return GetDefaultBannerAddress();

            return $"knowledge/{sectionId.ToLowerInvariant()}";
        }

        public string GetDefaultBannerAddress() => "dimensions/default";

        // ── Banner Loading & Binding ────────────────────────────────────────

        public Task<Sprite> LoadBannerAsync(string address)
        {
            return _spriteService.LoadSpriteAsync(address, GetDefaultBannerAddress());
        }

        public Task<Sprite> LoadDimensionBannerAsync(string dimensionCodeOrId)
        {
            string address = GetDimensionBannerAddress(dimensionCodeOrId);
            return LoadBannerAsync(address);
        }

        public Task<Sprite> LoadTopicBannerAsync(string topicCodeOrId)
        {
            string address = GetTopicBannerAddress(topicCodeOrId);
            return LoadBannerAsync(address);
        }

        public bool IsBannerLoaded(string address)
        {
            return _spriteService.IsSpriteLoaded(address);
        }

        public Task<bool> BindBanner(Image targetImage, string address, bool autoAspectRatio = true, Action<Sprite> onLoaded = null)
        {
            return _spriteService.BindSprite(targetImage, address, autoAspectRatio, onLoaded, GetDefaultBannerAddress());
        }

        public Task<bool> BindDimensionBanner(Image targetImage, string dimensionCodeOrId, bool autoAspectRatio = true, Action<Sprite> onLoaded = null)
        {
            string address = GetDimensionBannerAddress(dimensionCodeOrId);
            return BindBanner(targetImage, address, autoAspectRatio, onLoaded);
        }

        public Task<bool> BindTopicBanner(Image targetImage, string topicCodeOrId, bool autoAspectRatio = true, Action<Sprite> onLoaded = null)
        {
            string address = GetTopicBannerAddress(topicCodeOrId);
            return BindBanner(targetImage, address, autoAspectRatio, onLoaded);
        }

        // ── ISpriteService Delegation ───────────────────────────────────────

        public Task<Sprite> LoadSpriteAsync(string address, string fallbackAddress = null)
        {
            return _spriteService.LoadSpriteAsync(address, fallbackAddress);
        }

        public Sprite GetCachedSprite(string address)
        {
            return _spriteService.GetCachedSprite(address);
        }

        public bool IsSpriteLoaded(string address)
        {
            return _spriteService.IsSpriteLoaded(address);
        }

        public Task<bool> BindSprite(Image targetImage, string address, bool autoAspectRatio = true, Action<Sprite> onLoaded = null, string fallbackAddress = null)
        {
            return _spriteService.BindSprite(targetImage, address, autoAspectRatio, onLoaded, fallbackAddress);
        }

        public Task<bool> BindBackgroundSprite(VisualElement targetElement, string address, Action<Sprite> onLoaded = null, string fallbackAddress = null)
        {
            return _spriteService.BindBackgroundSprite(targetElement, address, onLoaded, fallbackAddress);
        }

        public void ReleaseSprite(string address)
        {
            _spriteService.ReleaseSprite(address);
        }

        public void ClearCache()
        {
            _spriteService.ClearCache();
        }

        public void Dispose()
        {
            _spriteService.Dispose();
        }
    }
}
