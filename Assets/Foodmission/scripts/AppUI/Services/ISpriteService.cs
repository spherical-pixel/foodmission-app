using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    /// <summary>
    /// Service contract for loading, caching, and binding 2D Sprites from Addressables
    /// to UI Toolkit elements (Image and VisualElement background).
    /// </summary>
    public interface ISpriteService : IDisposable
    {
        /// <summary>
        /// Asynchronously loads a Sprite from Addressables with in-memory caching and optional fallback address.
        /// </summary>
        Task<Sprite> LoadSpriteAsync(string address, string fallbackAddress = null);

        /// <summary>
        /// Returns the cached Sprite if already loaded into memory, otherwise null.
        /// </summary>
        Sprite GetCachedSprite(string address);

        /// <summary>
        /// Checks if a Sprite for the given address is currently cached in memory.
        /// </summary>
        bool IsSpriteLoaded(string address);

        /// <summary>
        /// Binds an Addressables Sprite to a UI Toolkit Image element.
        /// Handles async loading, setting sprite, ScaleMode.ScaleToFit, aspect ratio calculation, and display styling.
        /// </summary>
        Task<bool> BindSprite(Image targetImage, string address, bool autoAspectRatio = true, Action<Sprite> onLoaded = null, string fallbackAddress = null);

        /// <summary>
        /// Binds an Addressables Sprite to a UI Toolkit VisualElement's background (style.backgroundImage).
        /// Handles async loading, applying StyleBackground, display styling, and optional fallback address.
        /// </summary>
        Task<bool> BindBackgroundSprite(VisualElement targetElement, string address, Action<Sprite> onLoaded = null, string fallbackAddress = null);

        /// <summary>
        /// Releases the loaded Addressables handle for a specific address and removes it from the in-memory cache.
        /// </summary>
        void ReleaseSprite(string address);

        /// <summary>
        /// Releases all loaded Addressables sprite handles and clears the in-memory cache.
        /// </summary>
        void ClearCache();
    }
}
