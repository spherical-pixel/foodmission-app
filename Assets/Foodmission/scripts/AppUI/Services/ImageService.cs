using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class ImageService : IImageService
    {
        private readonly Dictionary<string, Texture2D> _cache = new();
        private readonly LinkedList<string> _lruOrder = new();
        private const int MAX_CACHE_SIZE = 50;

        public async Task<Texture2D> LoadImageAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            if (_cache.TryGetValue(url, out Texture2D cached))
            {
                _lruOrder.Remove(url);
                _lruOrder.AddFirst(url);
                return cached;
            }

            try
            {
                using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
                UnityWebRequestAsyncOperation op = request.SendWebRequest();
                while (!op.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[{GetType().Name}] Failed to load image from {url}: {request.error}");
                    return null;
                }

                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;

                if (texture != null)
                {
                    EvictIfNeeded();
                    _cache[url] = texture;
                    _lruOrder.AddFirst(url);
                }
                return texture;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadImageAsync exception: {ex.Message}");
                return null;
            }
        }

        public void ClearCache()
        {
            foreach (var kvp in _cache)
            {
                if (kvp.Value != null)
                    UnityEngine.Object.Destroy(kvp.Value);
            }
            _cache.Clear();
            _lruOrder.Clear();
        }

        private void EvictIfNeeded()
        {
            while (_cache.Count >= MAX_CACHE_SIZE && _lruOrder.Count > 0)
            {
                string oldest = _lruOrder.Last.Value;
                _lruOrder.RemoveLast();
                if (_cache.TryGetValue(oldest, out Texture2D evicted) && evicted != null)
                    UnityEngine.Object.Destroy(evicted);
                _cache.Remove(oldest);
            }
        }
    }
}
