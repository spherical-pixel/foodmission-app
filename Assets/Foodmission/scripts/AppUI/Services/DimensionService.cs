using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.AppUI.Redux;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace eu.foodmission.platform
{
    public class DimensionService : IDimensionService
    {
        private readonly IStoreService _storeService;
        private readonly IDisposableSubscription _storeSubscription;

        private readonly Dictionary<string, Dimension[]> _cacheByLang = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<Dimension> _dimensions = new();
        private readonly List<Topic> _topics = new();
        private readonly Dictionary<string, Dimension> _dimensionByCodeOrId = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Topic> _topicByCodeOrId = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Dimension> _topicToDimension = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<Topic>> _topicsByDimension = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Sprite> _spriteCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AsyncOperationHandle<Sprite>> _handleCache = new(StringComparer.OrdinalIgnoreCase);

        public bool IsLoaded => _dimensions.Count > 0;
        public string LoadedLanguage { get; private set; }

        public DimensionService(IStoreService storeService)
        {
            _storeService = storeService ?? throw new ArgumentNullException(nameof(storeService));

            _storeSubscription = _storeService.store?.Subscribe(
                state => (state.lang, state.accessToken),
                OnStateChanged
            );
        }

        private string AuthHeader
        {
            get
            {
                AppState s = _storeService.GetAppState();
                return $"{s.tokenType} {s.accessToken}";
            }
        }

        private string ResolveLang(string lang)
        {
            if (!string.IsNullOrEmpty(lang))
                return lang;

            AppState s = _storeService.GetAppState();
            if (!string.IsNullOrEmpty(s?.lang) && s.lang != "none")
                return s.lang;

            return "en";
        }

        private void OnStateChanged((string lang, string accessToken) state)
        {
            string currentLang = ResolveLang(state.lang);
            if (!string.IsNullOrEmpty(LoadedLanguage) &&
                !string.Equals(LoadedLanguage, currentLang, StringComparison.OrdinalIgnoreCase))
            {
                InvalidateCache();

                if (!string.IsNullOrEmpty(state.accessToken))
                {
                    _ = PreloadAsync(currentLang, force: true);
                }
            }
        }

        public async Task<(Dimension[] Result, ApiErrorResponse Error)> PreloadAsync(string lang = null, bool force = false)
        {
            string effectiveLang = ResolveLang(lang);
            if (!force && IsLoaded && string.Equals(LoadedLanguage, effectiveLang, StringComparison.OrdinalIgnoreCase))
            {
                return (_dimensions.ToArray(), null);
            }

            var (result, error) = await GetDimensionsAsync(effectiveLang, forceRefresh: force);
            if (error == null && result != null)
            {
                UpdateMemoryIndex(result, effectiveLang);
            }
            return (result, error);
        }

        public void InvalidateCache()
        {
            _cacheByLang.Clear();
            _dimensions.Clear();
            _topics.Clear();
            _dimensionByCodeOrId.Clear();
            _topicByCodeOrId.Clear();
            _topicToDimension.Clear();
            _topicsByDimension.Clear();
            ClearSpriteCache();
            LoadedLanguage = null;
        }

        // ── Asynchronous API Queries ────────────────────────────────────────

        public async Task<(Dimension[] Result, ApiErrorResponse Error)> GetDimensionsAsync(
            string lang = null,
            bool forceRefresh = false)
        {
            string effectiveLang = ResolveLang(lang);

            if (!forceRefresh && _cacheByLang.TryGetValue(effectiveLang, out var cached) && cached != null)
            {
                if (!IsLoaded || !string.Equals(LoadedLanguage, effectiveLang, StringComparison.OrdinalIgnoreCase))
                {
                    UpdateMemoryIndex(cached, effectiveLang);
                }
                return (cached, null);
            }

            string url = $"{ApiConfig.BaseUrl}/api/v1/dimensions?lang={Uri.EscapeDataString(effectiveLang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetDimensionsAsync"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                Dimension[] dimensions = JsonConvert.DeserializeObject<Dimension[]>(raw) ?? Array.Empty<Dimension>();

                _cacheByLang[effectiveLang] = dimensions;
                UpdateMemoryIndex(dimensions, effectiveLang);

                return (dimensions, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize Dimension[]: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(Dimension Result, ApiErrorResponse Error)> GetDimensionAsync(
            string codeOrId,
            string lang = null)
        {
            if (string.IsNullOrEmpty(codeOrId))
            {
                return (null, null);
            }

            string effectiveLang = ResolveLang(lang);

            // If already loaded in memory for this language, return from memory
            if (IsLoaded && string.Equals(LoadedLanguage, effectiveLang, StringComparison.OrdinalIgnoreCase))
            {
                Dimension cachedDim = GetDimension(codeOrId);
                if (cachedDim != null)
                {
                    return (cachedDim, null);
                }
            }

            string url = $"{ApiConfig.BaseUrl}/api/v1/dimensions/{Uri.EscapeDataString(codeOrId)}?lang={Uri.EscapeDataString(effectiveLang)}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", AuthHeader);
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetDimensionAsync"));
            }

            try
            {
                string raw = request.downloadHandler.text;
                Dimension dimension = JsonConvert.DeserializeObject<Dimension>(raw);
                return (dimension, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] Failed to deserialize Dimension: {ex.Message}");
                return (null, new ApiErrorResponse { message = ex.Message });
            }
        }

        public async Task<(Topic[] Result, ApiErrorResponse Error)> GetTopicsAsync(
            string dimensionCodeOrId = null,
            string lang = null)
        {
            var (dimensions, error) = await GetDimensionsAsync(lang);
            if (error != null)
            {
                return (null, error);
            }

            if (dimensions == null || dimensions.Length == 0)
            {
                return (Array.Empty<Topic>(), null);
            }

            if (string.IsNullOrEmpty(dimensionCodeOrId))
            {
                var allTopics = dimensions
                    .Where(d => d.topics != null)
                    .SelectMany(d => d.topics)
                    .OrderBy(t => t.sortOrder)
                    .ToArray();
                return (allTopics, null);
            }

            var matchingDim = dimensions.FirstOrDefault(d =>
                string.Equals(d.code, dimensionCodeOrId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(d.id, dimensionCodeOrId, StringComparison.OrdinalIgnoreCase));

            if (matchingDim?.topics == null)
            {
                return (Array.Empty<Topic>(), null);
            }

            return (matchingDim.topics.OrderBy(t => t.sortOrder).ToArray(), null);
        }

        public async Task<(Topic Result, ApiErrorResponse Error)> GetTopicAsync(
            string topicCodeOrId,
            string lang = null)
        {
            if (string.IsNullOrEmpty(topicCodeOrId))
            {
                return (null, null);
            }

            string effectiveLang = ResolveLang(lang);
            if (IsLoaded && string.Equals(LoadedLanguage, effectiveLang, StringComparison.OrdinalIgnoreCase))
            {
                Topic cachedTopic = GetTopic(topicCodeOrId);
                if (cachedTopic != null)
                {
                    return (cachedTopic, null);
                }
            }

            var (topics, error) = await GetTopicsAsync(null, effectiveLang);
            if (error != null)
            {
                return (null, error);
            }

            var found = topics?.FirstOrDefault(t =>
                string.Equals(t.code, topicCodeOrId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(t.id, topicCodeOrId, StringComparison.OrdinalIgnoreCase));

            return (found, null);
        }

        // ── Synchronous In-Memory Lookups (Preloaded) ───────────────────────

        public IReadOnlyList<Dimension> GetAllDimensions()
        {
            return _dimensions.AsReadOnly();
        }

        public Dimension GetDimension(string codeOrId)
        {
            if (string.IsNullOrEmpty(codeOrId))
                return null;

            _dimensionByCodeOrId.TryGetValue(codeOrId, out var dimension);
            return dimension;
        }

        public IReadOnlyList<Topic> GetAllTopics()
        {
            return _topics.AsReadOnly();
        }

        public IReadOnlyList<Topic> GetTopicsForDimension(string dimensionCodeOrId)
        {
            if (string.IsNullOrEmpty(dimensionCodeOrId))
                return Array.Empty<Topic>();

            if (_topicsByDimension.TryGetValue(dimensionCodeOrId, out var list))
            {
                return list.AsReadOnly();
            }

            return Array.Empty<Topic>();
        }

        public Dimension GetDimensionForTopic(string topicCodeOrId)
        {
            if (string.IsNullOrEmpty(topicCodeOrId))
                return null;

            _topicToDimension.TryGetValue(topicCodeOrId, out var dimension);
            return dimension;
        }

        public Topic GetTopic(string topicCodeOrId)
        {
            if (string.IsNullOrEmpty(topicCodeOrId))
                return null;

            _topicByCodeOrId.TryGetValue(topicCodeOrId, out var topic);
            return topic;
        }

        // ── Visual Resources (Addressables Banners & Memory Management) ─────

        public string GetTopicSpriteAddress(string topicCodeOrId)
        {
            if (string.IsNullOrEmpty(topicCodeOrId))
                return GetDefaultSpriteAddress();

            Topic topic = GetTopic(topicCodeOrId);
            string topicCode = topic?.code ?? topicCodeOrId;

            if (string.IsNullOrEmpty(topicCode))
                return GetDefaultSpriteAddress();

            return $"topics/{topicCode.ToLowerInvariant()}";
        }

        public string GetDimensionSpriteAddress(string dimensionCodeOrId)
        {
            if (string.IsNullOrEmpty(dimensionCodeOrId))
                return GetDefaultSpriteAddress();

            Dimension dim = GetDimension(dimensionCodeOrId);
            string dimCode = dim?.code ?? dimensionCodeOrId;

            if (string.IsNullOrEmpty(dimCode))
                return GetDefaultSpriteAddress();

            return $"dimensions/{dimCode.ToLowerInvariant()}";
        }

        public string GetDefaultSpriteAddress() => "dimensions/default";

        public async Task<Sprite> GetTopicSpriteAsync(string topicCodeOrId)
        {
            string address = GetTopicSpriteAddress(topicCodeOrId);
            return await LoadSpriteFromAddressAsync(address);
        }

        public async Task<Sprite> GetDimensionSpriteAsync(string dimensionCodeOrId)
        {
            string address = GetDimensionSpriteAddress(dimensionCodeOrId);
            return await LoadSpriteFromAddressAsync(address);
        }

        public async Task<Sprite> GetDefaultSpriteAsync()
        {
            return await LoadSpriteFromAddressAsync(GetDefaultSpriteAddress());
        }

        public Sprite GetTopicSprite(string topicCodeOrId)
        {
            string address = GetTopicSpriteAddress(topicCodeOrId);
            if (_spriteCache.TryGetValue(address, out Sprite cached) && cached != null)
            {
                return cached;
            }
            return null;
        }

        public Sprite GetDimensionSprite(string dimensionCodeOrId)
        {
            string address = GetDimensionSpriteAddress(dimensionCodeOrId);
            if (_spriteCache.TryGetValue(address, out Sprite cached) && cached != null)
            {
                return cached;
            }
            return null;
        }

        public Sprite GetDefaultSprite()
        {
            string address = GetDefaultSpriteAddress();
            if (_spriteCache.TryGetValue(address, out Sprite cached) && cached != null)
            {
                return cached;
            }
            return null;
        }

        public void ClearSpriteCache()
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

        private async Task<Sprite> LoadSpriteFromAddressAsync(string address)
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
                    string defaultAddress = GetDefaultSpriteAddress();
                    if (address != defaultAddress)
                    {
                        return await LoadSpriteFromAddressAsync(defaultAddress);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] Failed to load Addressable sprite '{address}': {ex.Message}");
            }

            return null;
        }

        // ── Internal Helpers ────────────────────────────────────────────────

        private void UpdateMemoryIndex(Dimension[] dimensions, string lang)
        {
            _dimensions.Clear();
            _topics.Clear();
            _dimensionByCodeOrId.Clear();
            _topicByCodeOrId.Clear();
            _topicToDimension.Clear();
            _topicsByDimension.Clear();

            LoadedLanguage = lang;

            if (dimensions == null)
                return;

            foreach (var dim in dimensions)
            {
                if (dim == null)
                    continue;

                _dimensions.Add(dim);

                if (!string.IsNullOrEmpty(dim.id))
                    _dimensionByCodeOrId[dim.id] = dim;
                if (!string.IsNullOrEmpty(dim.code))
                    _dimensionByCodeOrId[dim.code] = dim;

                var dimTopics = new List<Topic>();

                if (dim.topics != null)
                {
                    foreach (var topic in dim.topics)
                    {
                        if (topic == null)
                            continue;

                        _topics.Add(topic);
                        dimTopics.Add(topic);

                        if (!string.IsNullOrEmpty(topic.id))
                        {
                            _topicByCodeOrId[topic.id] = topic;
                            _topicToDimension[topic.id] = dim;
                        }

                        if (!string.IsNullOrEmpty(topic.code))
                        {
                            _topicByCodeOrId[topic.code] = topic;
                            _topicToDimension[topic.code] = dim;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(dim.id))
                    _topicsByDimension[dim.id] = dimTopics;
                if (!string.IsNullOrEmpty(dim.code))
                    _topicsByDimension[dim.code] = dimTopics;
            }
        }

        public void Dispose()
        {
            _storeSubscription?.Dispose();
            ClearSpriteCache();
        }
    }
}
