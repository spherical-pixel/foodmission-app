using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Default implementation of ISpriteService for managing 2D Sprites loaded from Addressables.
    /// Supports asynchronous caching, in-flight load deduplication, and UI Toolkit element binding.
    /// </summary>
    public class SpriteService : ISpriteService
    {
        private readonly Dictionary<string, Sprite> _spriteCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AsyncOperationHandle<Sprite>> _handleCache = new(StringComparer.OrdinalIgnoreCase);

        // ── Loading & Caching ───────────────────────────────────────────────

        public async Task<Sprite> LoadSpriteAsync(string address, string fallbackAddress = null)
        {
            if (string.IsNullOrEmpty(address))
            {
                if (!string.IsNullOrEmpty(fallbackAddress))
                {
                    return await LoadSpriteAsync(fallbackAddress);
                }
                return null;
            }

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
                    if (!string.IsNullOrEmpty(fallbackAddress) && !string.Equals(address, fallbackAddress, StringComparison.OrdinalIgnoreCase))
                    {
                        return await LoadSpriteAsync(fallbackAddress);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] Failed to load Addressable sprite '{address}': {ex.Message}");
                if (!string.IsNullOrEmpty(fallbackAddress) && !string.Equals(address, fallbackAddress, StringComparison.OrdinalIgnoreCase))
                {
                    return await LoadSpriteAsync(fallbackAddress);
                }
            }

            return null;
        }

        public Sprite GetCachedSprite(string address)
        {
            if (string.IsNullOrEmpty(address)) return null;
            _spriteCache.TryGetValue(address, out Sprite cached);
            return cached;
        }

        public bool IsSpriteLoaded(string address)
        {
            if (string.IsNullOrEmpty(address)) return false;
            return _spriteCache.ContainsKey(address) && _spriteCache[address] != null;
        }

        // ── UI Toolkit Binding Helpers ──────────────────────────────────────

        public async Task<bool> BindSprite(Image targetImage, string address, bool autoAspectRatio = true, Action<Sprite> onLoaded = null, string fallbackAddress = null)
        {
            if (targetImage == null) return false;

            Sprite sprite = await LoadSpriteAsync(address, fallbackAddress);
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

        public async Task<bool> BindBackgroundSprite(VisualElement targetElement, string address, Action<Sprite> onLoaded = null, string fallbackAddress = null)
        {
            if (targetElement == null) return false;

            Sprite sprite = await LoadSpriteAsync(address, fallbackAddress);
            if (sprite != null && targetElement != null)
            {
                targetElement.style.backgroundImage = new StyleBackground(sprite);
                targetElement.style.display = DisplayStyle.Flex;
                onLoaded?.Invoke(sprite);
                return true;
            }

            if (targetElement != null)
            {
                targetElement.style.backgroundImage = StyleKeyword.None;
            }
            return false;
        }

        // ── Memory Management ───────────────────────────────────────────────

        public void ReleaseSprite(string address)
        {
            if (string.IsNullOrEmpty(address)) return;

            if (_handleCache.TryGetValue(address, out var handle))
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                _handleCache.Remove(address);
            }
            _spriteCache.Remove(address);
        }

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
