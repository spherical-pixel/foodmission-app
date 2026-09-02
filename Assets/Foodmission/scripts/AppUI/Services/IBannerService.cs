using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace eu.foodmission.platform
{
    public interface IBannerService : IDisposable
    {
        /// <summary>
        /// Resolves the Addressables address for a given dimension banner (e.g. "dimensions/diet_changes").
        /// </summary>
        string GetDimensionBannerAddress(string dimensionCodeOrId);

        /// <summary>
        /// Resolves the Addressables address for a given topic banner (e.g. "topics/reducing_meat_consumption").
        /// </summary>
        string GetTopicBannerAddress(string topicCodeOrId);

        /// <summary>
        /// Resolves the Addressables address for a given knowledge section (e.g. "knowledge/quiz").
        /// </summary>
        string GetKnowledgeBannerAddress(string sectionId);

        /// <summary>
        /// Returns the default fallback Addressables banner address ("dimensions/default").
        /// </summary>
        string GetDefaultBannerAddress();

        /// <summary>
        /// Asynchronously loads a banner Sprite from Addressables with caching.
        /// </summary>
        Task<Sprite> LoadBannerAsync(string address);

        /// <summary>
        /// Asynchronously loads a dimension banner Sprite with caching and fallback.
        /// </summary>
        Task<Sprite> LoadDimensionBannerAsync(string dimensionCodeOrId);

        /// <summary>
        /// Asynchronously loads a topic banner Sprite with caching and fallback.
        /// </summary>
        Task<Sprite> LoadTopicBannerAsync(string topicCodeOrId);

        /// <summary>
        /// Returns the cached Sprite if already loaded into memory, otherwise null.
        /// </summary>
        Sprite GetCachedSprite(string address);

        /// <summary>
        /// Checks if a banner is currently cached in memory.
        /// </summary>
        bool IsBannerLoaded(string address);

        /// <summary>
        /// Binds an Addressables banner Sprite to a UI Toolkit Image element.
        /// Automatically handles asynchronous loading, setting sprite, ScaleMode.ScaleToFit,
        /// dynamic aspect ratio calculation and display styling.
        /// </summary>
        Task<bool> BindBanner(Image targetImage, string address, bool autoAspectRatio = true, Action<Sprite> onLoaded = null);

        /// <summary>
        /// Binds a dimension banner Sprite to a UI Toolkit Image element.
        /// </summary>
        Task<bool> BindDimensionBanner(Image targetImage, string dimensionCodeOrId, bool autoAspectRatio = true, Action<Sprite> onLoaded = null);

        /// <summary>
        /// Binds a topic banner Sprite to a UI Toolkit Image element.
        /// </summary>
        Task<bool> BindTopicBanner(Image targetImage, string topicCodeOrId, bool autoAspectRatio = true, Action<Sprite> onLoaded = null);

        /// <summary>
        /// Releases all loaded Addressables sprite handles and clears the in-memory cache.
        /// </summary>
        void ClearCache();
    }
}
