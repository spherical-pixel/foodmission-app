using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>One page of FMSearchOrCategoryField results: GenericFood and OpenFoodFactsProduct items.</summary>
    public class FoodSearchPage
    {
        public List<object> Items = new();
        /// <summary>Exact number of pages, or 0 when it isn't known (the backend OFF search doesn't count matches).</summary>
        public int TotalPages;
        public bool HasNextPage;
    }

    /// <summary>
    /// Text search shared by every screen that hosts FMSearchOrCategoryField.
    /// Each source keeps its own order (the backend ranks the results). When both are on, results are
    /// fetched in chunks of half a page: a page takes one chunk of each, interleaved starting with a
    /// generic food, and once a source runs out the other one fills the whole page.
    /// An error is only returned when there is nothing to show.
    /// </summary>
    public static class FoodSearchFlow
    {
        public const int PageSize = 10;
        public const int ChunkSize = PageSize / 2;

        // Stands for "not known yet"; small enough that 2 * page - Unknown can't overflow
        private const int Unknown = int.MaxValue / 4;
        private const int KnownCountsTtlMinutes = 10;

        // Chunk counts learned per query. A page only knows where a source ends once it has seen the end,
        // and later pages (or going back) need the same counts to place the chunks without gaps or repeats.
        private static readonly Dictionary<string, (int? Generic, int? Products, DateTime At)> s_knownCounts = new();
        private static readonly object s_lock = new();

        public static void ClearKnownCounts()
        {
            lock (s_lock)
            {
                s_knownCounts.Clear();
            }
        }

        public static async Task<(FoodSearchPage Result, ApiErrorResponse Error)> SearchAsync(
            IGenericFoodService genericFoodService,
            IFoodProductService foodService,
            IOpenFoodFactsClientService offService,
            string query,
            int page,
            bool includeGeneric,
            bool includeProducts)
        {
            var result = new FoodSearchPage();
            if (string.IsNullOrWhiteSpace(query) || (!includeGeneric && !includeProducts))
            {
                return (result, null);
            }

            string trimmed = query.Trim();
            int pageIndex = Math.Max(1, page) - 1;

            ApiErrorResponse error;
            if (includeGeneric && includeProducts)
            {
                error = await SearchBothAsync(genericFoodService, foodService, offService, trimmed, pageIndex, result);
            }
            else if (includeGeneric)
            {
                error = await SearchGenericOnlyAsync(genericFoodService, trimmed, pageIndex, result);
            }
            else
            {
                error = await SearchProductsOnlyAsync(foodService, offService, trimmed, pageIndex, result);
            }

            if (error != null && result.Items.Count > 0)
            {
                Debug.LogWarning($"[FoodSearchFlow] One source failed, showing the other: {error.message}");
                error = null;
            }
            return (result, error);
        }

        private static async Task<ApiErrorResponse> SearchGenericOnlyAsync(
            IGenericFoodService genericFoodService, string query, int pageIndex, FoodSearchPage result)
        {
            var (response, error) = await genericFoodService.SearchGenericFoodsAsync(query, page: pageIndex + 1, pageSize: PageSize);
            if (response?.items != null)
            {
                result.Items.AddRange(response.items);
                result.TotalPages = response.totalPages;
                result.HasNextPage = pageIndex + 1 < response.totalPages;
            }
            return error;
        }

        private static async Task<ApiErrorResponse> SearchProductsOnlyAsync(
            IFoodProductService foodService, IOpenFoodFactsClientService offService, string query, int pageIndex, FoodSearchPage result)
        {
            var (response, error) = await FoodProductFlow.SearchProductsAsync(foodService, offService, query, pageIndex + 1, PageSize);
            if (response?.products != null)
            {
                result.Items.AddRange(response.products);
                if (response.hasMore.HasValue)
                {
                    result.HasNextPage = response.hasMore.Value;
                    result.TotalPages = response.hasMore.Value ? 0 : pageIndex + 1;
                }
                else
                {
                    result.TotalPages = ExactProductPages(response, PageSize);
                    result.HasNextPage = pageIndex + 1 < result.TotalPages;
                }
            }
            return error;
        }

        private static async Task<ApiErrorResponse> SearchBothAsync(
            IGenericFoodService genericFoodService,
            IFoodProductService foodService,
            IOpenFoodFactsClientService offService,
            string query,
            int pageIndex,
            FoodSearchPage result)
        {
            string key = query.ToLowerInvariant();
            var generics = new ChunkSource<GenericFood>(async chunk =>
            {
                var (response, error) = await genericFoodService.SearchGenericFoodsAsync(query, page: chunk + 1, pageSize: ChunkSize);
                return response?.items == null
                    ? new Chunk<GenericFood>(null, null, error)
                    : new Chunk<GenericFood>(response.items, GenericChunkCount(response), error);
            });
            var products = new ChunkSource<OpenFoodFactsProduct>(async chunk =>
            {
                var (response, error) = await FoodProductFlow.SearchProductsAsync(foodService, offService, query, chunk + 1, ChunkSize);
                return response?.products == null
                    ? new Chunk<OpenFoodFactsProduct>(null, null, error)
                    : new Chunk<OpenFoodFactsProduct>(response.products, ProductChunkCount(response, chunk), error);
            });

            lock (s_lock)
            {
                if (s_knownCounts.TryGetValue(key, out var known) && (DateTime.UtcNow - known.At).TotalMinutes < KnownCountsTtlMinutes)
                {
                    generics.Count = known.Generic;
                    products.Count = known.Products;
                }
            }

            // Fetching can reveal where a source ends, which moves the other source's chunks: re-plan until stable
            ChunkPlan plan = Plan(pageIndex, generics.Count, products.Count);
            for (int round = 0; round < 3; round++)
            {
                await Task.WhenAll(generics.FetchAsync(plan.GenericFrom, plan.GenericTo),
                                   products.FetchAsync(plan.ProductsFrom, plan.ProductsTo));
                await products.FindEndAsync();

                ChunkPlan next = Plan(pageIndex, generics.Count, products.Count);
                if (next.Equals(plan))
                {
                    break;
                }
                plan = next;
            }

            lock (s_lock)
            {
                s_knownCounts[key] = (generics.Count, products.Count, DateTime.UtcNow);
            }

            result.Items = Interleave(generics.Items(plan.GenericFrom, plan.GenericTo), products.Items(plan.ProductsFrom, plan.ProductsTo));
            result.HasNextPage = !Plan(pageIndex + 1, generics.Count, products.Count).IsEmpty;
            result.TotalPages = generics.Count.HasValue && products.Count.HasValue
                ? (generics.Count.Value + products.Count.Value + 1) / 2
                : 0;
            return generics.Error ?? products.Error;
        }

        public static List<object> Interleave(IReadOnlyList<GenericFood> generics, IReadOnlyList<OpenFoodFactsProduct> products)
        {
            var items = new List<object>(generics.Count + products.Count);
            int count = Math.Max(generics.Count, products.Count);
            for (int i = 0; i < count; i++)
            {
                if (i < generics.Count)
                {
                    items.Add(generics[i]);
                }
                if (i < products.Count)
                {
                    items.Add(products[i]);
                }
            }
            return items;
        }

        /// <summary>
        /// Chunks [From, To) of each source shown on a page. Every page takes two chunks: one of each while
        /// both last, both from the other source once one has run out. Unknown counts are treated as endless.
        /// </summary>
        public static ChunkPlan Plan(int pageIndex, int? genericChunks, int? productChunks)
        {
            int g = genericChunks ?? Unknown;
            int p = productChunks ?? Unknown;
            return new ChunkPlan(
                UsedBefore(pageIndex, g, p), UsedBefore(pageIndex + 1, g, p),
                UsedBefore(pageIndex, p, g), UsedBefore(pageIndex + 1, p, g));
        }

        // Chunks of one source shown on the pages before pageIndex: its half of every page,
        // plus the other source's half on the pages after that one ran out.
        private static int UsedBefore(int pageIndex, int own, int other)
        {
            return Math.Min(own, Math.Max(pageIndex, 2 * pageIndex - other));
        }

        private static int GenericChunkCount(PaginatedGenericFoodResponse response)
        {
            return (response.total + ChunkSize - 1) / ChunkSize;
        }

        // Exact chunk count when the response reveals it, null while more products may follow
        private static int? ProductChunkCount(OpenFoodFactsSearchResponse response, int chunk)
        {
            if (!response.hasMore.HasValue)
            {
                return ExactProductPages(response, ChunkSize);
            }
            if (response.hasMore.Value)
            {
                return null;
            }
            if (response.products.Length > 0)
            {
                return chunk + 1;
            }
            // Past the end: only page 1 tells where it is
            return chunk == 0 ? 0 : null;
        }

        private static int ExactProductPages(OpenFoodFactsSearchResponse response, int pageSize)
        {
            if (response.totalPages > 0)
            {
                return response.totalPages;
            }

            int size = response.pageSize > 0 ? response.pageSize : pageSize;
            return (response.totalCount + size - 1) / size;
        }

        public readonly struct ChunkPlan : IEquatable<ChunkPlan>
        {
            public readonly int GenericFrom;
            public readonly int GenericTo;
            public readonly int ProductsFrom;
            public readonly int ProductsTo;

            public ChunkPlan(int genericFrom, int genericTo, int productsFrom, int productsTo)
            {
                GenericFrom = genericFrom;
                GenericTo = genericTo;
                ProductsFrom = productsFrom;
                ProductsTo = productsTo;
            }

            public bool IsEmpty => GenericTo <= GenericFrom && ProductsTo <= ProductsFrom;

            public bool Equals(ChunkPlan other)
            {
                return GenericFrom == other.GenericFrom && GenericTo == other.GenericTo
                    && ProductsFrom == other.ProductsFrom && ProductsTo == other.ProductsTo;
            }

            public override bool Equals(object obj) => obj is ChunkPlan other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(GenericFrom, GenericTo, ProductsFrom, ProductsTo);
        }

        private readonly struct Chunk<T>
        {
            public readonly T[] Items;
            public readonly int? Count;
            public readonly ApiErrorResponse Error;

            public Chunk(T[] items, int? count, ApiErrorResponse error)
            {
                Items = items;
                Count = count;
                Error = error;
            }
        }

        /// <summary>One source's chunks fetched during a page load, and its chunk count once known.</summary>
        private sealed class ChunkSource<T>
        {
            private readonly Func<int, Task<Chunk<T>>> _fetch;
            private readonly Dictionary<int, T[]> _chunks = new();
            private readonly HashSet<int> _failed = new();

            public int? Count;
            public ApiErrorResponse Error;

            public ChunkSource(Func<int, Task<Chunk<T>>> fetch)
            {
                _fetch = fetch;
            }

            public async Task FetchAsync(int from, int to)
            {
                var pending = new List<Task>();
                for (int chunk = from; chunk < to; chunk++)
                {
                    if (!_chunks.ContainsKey(chunk) && !_failed.Contains(chunk))
                    {
                        pending.Add(FetchOneAsync(chunk));
                    }
                }
                await Task.WhenAll(pending);
            }

            /// <summary>
            /// A chunk past the end doesn't say where the end is: walk back to the last chunk with items.
            /// Rarely needed, the page before usually saw the end already.
            /// </summary>
            public async Task FindEndAsync()
            {
                int empty = FirstEmptyChunk();
                if (Count.HasValue || empty < 0)
                {
                    return;
                }

                for (int chunk = empty - 1; chunk >= 0 && !Count.HasValue; chunk--)
                {
                    if (!_chunks.ContainsKey(chunk))
                    {
                        await FetchOneAsync(chunk);
                    }
                    if (!_chunks.TryGetValue(chunk, out T[] items))
                    {
                        return;
                    }
                    if (items.Length > 0)
                    {
                        Count = chunk + 1;
                    }
                }
                Count ??= 0;
            }

            public IReadOnlyList<T> Items(int from, int to)
            {
                var items = new List<T>();
                for (int chunk = from; chunk < to; chunk++)
                {
                    if (_chunks.TryGetValue(chunk, out T[] chunkItems))
                    {
                        items.AddRange(chunkItems);
                    }
                }
                return items;
            }

            private int FirstEmptyChunk()
            {
                int first = -1;
                foreach (var pair in _chunks)
                {
                    if (pair.Value.Length == 0 && (first < 0 || pair.Key < first))
                    {
                        first = pair.Key;
                    }
                }
                return first;
            }

            private async Task FetchOneAsync(int chunk)
            {
                Chunk<T> fetched = await _fetch(chunk);
                if (fetched.Items == null)
                {
                    lock (_chunks)
                    {
                        _failed.Add(chunk);
                        Error ??= fetched.Error;
                    }
                    return;
                }

                lock (_chunks)
                {
                    _chunks[chunk] = fetched.Items;
                    if (fetched.Count.HasValue)
                    {
                        Count = fetched.Count;
                    }
                }
            }
        }
    }
}
