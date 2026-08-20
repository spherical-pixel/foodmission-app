using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace eu.foodmission.platform
{
    public interface IDimensionService : IDisposable
    {
        /// <summary>
        /// True if dimensions and topics have been loaded into memory for the active language.
        /// </summary>
        bool IsLoaded { get; }

        /// <summary>
        /// Currently loaded language code.
        /// </summary>
        string LoadedLanguage { get; }

        /// <summary>
        /// Preloads or refreshes all dimensions and nested topics from the backend.
        /// </summary>
        Task<(Dimension[] Result, ApiErrorResponse Error)> PreloadAsync(string lang = null, bool force = false);

        /// <summary>
        /// Clears the in-memory cache and loaded taxonomy.
        /// </summary>
        void InvalidateCache();

        // ── Asynchronous API Queries ────────────────────────────────────────

        /// <summary>
        /// Fetches all dimensions (with nested topics) for the specified language.
        /// Uses cached data unless forceRefresh is true.
        /// </summary>
        Task<(Dimension[] Result, ApiErrorResponse Error)> GetDimensionsAsync(string lang = null, bool forceRefresh = false);

        /// <summary>
        /// Fetches a single dimension by code or UUID from backend or local cache.
        /// </summary>
        Task<(Dimension Result, ApiErrorResponse Error)> GetDimensionAsync(string codeOrId, string lang = null);

        /// <summary>
        /// Fetches all topics, optionally filtered by dimension code or UUID.
        /// </summary>
        Task<(Topic[] Result, ApiErrorResponse Error)> GetTopicsAsync(string dimensionCodeOrId = null, string lang = null);

        /// <summary>
        /// Fetches a single topic by code or UUID.
        /// </summary>
        Task<(Topic Result, ApiErrorResponse Error)> GetTopicAsync(string topicCodeOrId, string lang = null);

        // ── Synchronous In-Memory Lookups (Preloaded) ───────────────────────

        /// <summary>
        /// Returns all preloaded dimensions in memory.
        /// </summary>
        IReadOnlyList<Dimension> GetAllDimensions();

        /// <summary>
        /// Gets a preloaded dimension by its code (e.g. "DIET_CHANGES") or UUID.
        /// </summary>
        Dimension GetDimension(string codeOrId);

        /// <summary>
        /// Returns all preloaded topics across all dimensions.
        /// </summary>
        IReadOnlyList<Topic> GetAllTopics();

        /// <summary>
        /// Returns all preloaded topics belonging to the given dimension code or UUID.
        /// </summary>
        IReadOnlyList<Topic> GetTopicsForDimension(string dimensionCodeOrId);

        /// <summary>
        /// Returns the parent dimension for a given topic code or UUID.
        /// </summary>
        Dimension GetDimensionForTopic(string topicCodeOrId);

        /// <summary>
        /// Gets a preloaded topic by its code (e.g. "REDUCING_MEAT_CONSUMPTION") or UUID.
        /// </summary>
        Topic GetTopic(string topicCodeOrId);

        // ── Visual Resources (Sprites & Memory Management) ──────────────────

        /// <summary>
        /// Gets the Sprite for a given topic by its code or UUID.
        /// Hierarchy of resolution:
        /// 1. Resources/topics/{topic_code.ToLower()}
        /// 2. Resources/dimensions/{dimension_code.ToLower()} or Resources/topics/{dimension_code.ToLower()}
        /// 3. Resources/topics/default or Resources/dimensions/default
        /// </summary>
        Sprite GetTopicSprite(string topicCodeOrId);

        /// <summary>
        /// Gets the Sprite for a given dimension by its code or UUID.
        /// Hierarchy of resolution:
        /// 1. Resources/dimensions/{dimension_code.ToLower()} or Resources/topics/{dimension_code.ToLower()}
        /// 2. Resources/dimensions/default or Resources/topics/default
        /// </summary>
        Sprite GetDimensionSprite(string dimensionCodeOrId);

        /// <summary>
        /// Gets the default fallback topic/dimension sprite.
        /// </summary>
        Sprite GetDefaultSprite();

        /// <summary>
        /// Clears cached sprite references in memory to allow garbage collection.
        /// </summary>
        void ClearSpriteCache();
    }
}
