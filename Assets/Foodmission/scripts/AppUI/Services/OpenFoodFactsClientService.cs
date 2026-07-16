using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace eu.foodmission.platform
{
    public class OpenFoodFactsClientService : IOpenFoodFactsClientService
    {
        private readonly Queue<DateTime> _requestTimestamps = new();
        private readonly Dictionary<string, (OpenFoodFactsSearchResponse data, DateTime cachedAt)> _searchCache = new();
        private readonly Dictionary<string, (OpenFoodFactsProduct data, DateTime cachedAt)> _barcodeCache = new();
        private readonly object _lockObj = new();

        public async Task<(OpenFoodFactsProduct Result, ApiErrorResponse Error)> GetByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrEmpty(barcode))
            {
                return (null, null);
            }

            // Check Cache
            lock (_lockObj)
            {
                if (_barcodeCache.TryGetValue(barcode, out var cached) && IsCacheFresh(cached.cachedAt))
                {
                    return (cached.data, null);
                }
            }

            // Enforce Rate Limit
            await EnforceRateLimitAsync();

            string url = $"{ApiConfig.OpenFoodFactsBaseUrl}/api/v0/product/{Uri.EscapeDataString(barcode)}.json";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("User-Agent", OpenFoodFactsUserAgent.Build());
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] GetByBarcode {barcode}"));
            }

            try
            {
                OpenFoodFactsProduct product = OpenFoodFactsParser.ParseProduct(request.downloadHandler.text);
                if (product == null)
                {
                    var notFoundErr = new ApiErrorResponse
                    {
                        statusCode = 404,
                        message = "Product not found in OpenFoodFacts"
                    };
                    return (null, notFoundErr);
                }

                lock (_lockObj)
                {
                    _barcodeCache[barcode] = (product, DateTime.UtcNow);
                }

                return (product, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] ParseProduct error: {ex.Message}");
                var parseErr = new ApiErrorResponse
                {
                    statusCode = 500,
                    message = $"Error parsing product details: {ex.Message}"
                };
                return (null, parseErr);
            }
        }

        public async Task<(OpenFoodFactsSearchResponse Result, ApiErrorResponse Error)> SearchAsync(string query, int page)
        {
            if (string.IsNullOrEmpty(query))
            {
                return (null, null);
            }

            string cacheKey = $"{query.Trim().ToLowerInvariant()}|{page}";

            // Check Cache
            lock (_lockObj)
            {
                if (_searchCache.TryGetValue(cacheKey, out var cached) && IsCacheFresh(cached.cachedAt))
                {
                    return (cached.data, null);
                }
            }

            // Enforce Rate Limit
            await EnforceRateLimitAsync();

            int pageSize = ApiConfig.OpenFoodFactsSearchPageSize;
            string url = $"{ApiConfig.OpenFoodFactsBaseUrl}/cgi/search.pl?search_terms={Uri.EscapeDataString(query)}&search_simple=1&action=process&json=1&page={page}&page_size={pageSize}";

            using UnityWebRequest request = UnityWebRequest.Get(url);
            request.SetRequestHeader("User-Agent", OpenFoodFactsUserAgent.Build());
            request.SetRequestHeader("Accept", "application/json");

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            while (!op.isDone)
            {
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                return (null, ApiErrorHelper.Parse(request, $"[{GetType().Name}] Search {query}"));
            }

            try
            {
                OpenFoodFactsSearchResponse searchResponse = OpenFoodFactsParser.ParseSearch(request.downloadHandler.text);
                if (searchResponse == null)
                {
                    var emptyResp = new OpenFoodFactsSearchResponse
                    {
                        products = Array.Empty<OpenFoodFactsProduct>(),
                        totalCount = 0,
                        page = page.ToString(),
                        pageSize = pageSize,
                        totalPages = 0
                    };
                    return (emptyResp, null);
                }

                lock (_lockObj)
                {
                    _searchCache[cacheKey] = (searchResponse, DateTime.UtcNow);
                }

                return (searchResponse, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] ParseSearch error: {ex.Message}");
                var parseErr = new ApiErrorResponse
                {
                    statusCode = 500,
                    message = $"Error parsing search results: {ex.Message}"
                };
                return (null, parseErr);
            }
        }

        private async Task EnforceRateLimitAsync()
        {
            int remainingWaitMs = 0;

            lock (_lockObj)
            {
                DateTime now = DateTime.UtcNow;
                
                // Prune timestamps older than 60 seconds
                while (_requestTimestamps.Count > 0 && (now - _requestTimestamps.Peek()).TotalSeconds >= 60)
                {
                    _requestTimestamps.Dequeue();
                }

                // If at limit (max 9 requests in 60s window), calculate wait time
                if (_requestTimestamps.Count >= 9)
                {
                    DateTime oldest = _requestTimestamps.Peek();
                    double elapsedSec = (now - oldest).TotalSeconds;
                    double waitSec = Math.Max(0, 60.0 - elapsedSec);
                    remainingWaitMs = (int)(waitSec * 1000) + 100; // safety margin of 100ms
                }
            }

            if (remainingWaitMs > 0)
            {
                Debug.LogWarning($"[{GetType().Name}] Approaching rate limit window. Pausing query execution for {remainingWaitMs} ms...");
                await Task.Delay(remainingWaitMs);
                
                // Re-evaluate limit recursively to ensure budget is free
                await EnforceRateLimitAsync();
                return;
            }

            lock (_lockObj)
            {
                _requestTimestamps.Enqueue(DateTime.UtcNow);
            }
        }

        private bool IsCacheFresh(DateTime cachedAt)
        {
            int ttlMinutes = ApiConfig.OpenFoodFactsSearchCacheTtlMinutes;
            return (DateTime.UtcNow - cachedAt).TotalMinutes < ttlMinutes;
        }

        // Test Helper to simulate requests without real clock delays
        public void ClearCache()
        {
            lock (_lockObj)
            {
                _searchCache.Clear();
                _barcodeCache.Clear();
            }
        }
    }
}
