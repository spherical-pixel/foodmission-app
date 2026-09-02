using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    public class BannerService : IBannerService
    {
        private readonly IDimensionService _dimensionService;
        private readonly Dictionary<string, Sprite> _spriteCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AsyncOperationHandle<Sprite>> _handleCache = new(StringComparer.OrdinalIgnoreCase);

        public BannerService(IDimensionService dimensionService = null)
        {
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

        // ── Loading & Caching ───────────────────────────────────────────────

        public async Task<Sprite> LoadBannerAsync(string address)
        {
            if (string.IsNullOrEmpty(address)) return null;

            if (_spriteCache.TryGetValue(address, out Sprite cached) && cached != null)
            {
                return cached;
            }

            if (_handleCache.TryGetValue(address, out var existingHandle) && existingHandle.IsValid())
            {
                if (existingHandle.IsDone)
                {
                    return existingHandle.Result;
                }
                await existingHandle.Task;
                return existingHandle.Result;
            }

            try
            {
                AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(address);
                _handleCache[address] = handle;
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
                {
                    _spriteCache[address] = handle.Result;
                    return handle.Result;
                }
                else
                {
                    string defaultAddress = GetDefaultBannerAddress();
                    if (address != defaultAddress)
                    {
                        return await LoadBannerAsync(defaultAddress);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] Failed to load Addressable banner '{address}': {ex.Message}");
            }

            return null;
        }

        public async Task<Sprite> LoadDimensionBannerAsync(string dimensionCodeOrId)
        {
            string address = GetDimensionBannerAddress(dimensionCodeOrId);
            return await LoadBannerAsync(address);
        }

        public async Task<Sprite> LoadTopicBannerAsync(string topicCodeOrId)
        {
            string address = GetTopicBannerAddress(topicCodeOrId);
            return await LoadBannerAsync(address);
        }

        public Sprite GetCachedSprite(string address)
        {
            if (string.IsNullOrEmpty(address)) return null;
            _spriteCache.TryGetValue(address, out Sprite cached);
            return cached;
        }

        public bool IsBannerLoaded(string address)
        {
            if (string.IsNullOrEmpty(address)) return false;
            return _spriteCache.ContainsKey(address) && _spriteCache[address] != null;
        }

        // ── UI Toolkit Image Binding Helpers ────────────────────────────────

        public async Task<bool> BindBanner(Image targetImage, string address, bool autoAspectRatio = true, Action<Sprite> onLoaded = null)
        {
            if (targetImage == null) return false;

            Sprite sprite = await LoadBannerAsync(address);
            if (sprite != null && targetImage != null)
            {
                targetImage.sprite = sprite;
                targetImage.scaleMode = ScaleMode.ScaleToFit;
                if (autoAspectRatio && sprite.rect.height > 0)
                {
                    targetImage.style.aspectRatio = sprite.rect.width / sprite.rect.height;
                }
                targetImage.style.display = DisplayStyle.Flex;
                onLoaded?.Invoke(sprite);
                return true;
            }

            if (targetImage != null)
            {
                targetImage.sprite = null;
                targetImage.style.display = DisplayStyle.None;
            }
            return false;
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

        // ── Memory Management ───────────────────────────────────────────────

        public void ClearCache()
        {
            foreach (var kvp in _handleCache)
            {
                if (kvp.Value.IsValid())
                {
                    Addressables.Release(kvp.Value);
                }
            }
            _handleCache.Clear();
            _spriteCache.Clear();
        }

        public void Dispose()
        {
            ClearCache();
        }
    }
}
