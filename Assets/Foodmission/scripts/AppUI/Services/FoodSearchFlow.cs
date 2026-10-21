using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    /// <summary>One page of FMSearchOrCategoryField results: GenericFood and OpenFoodFactsProduct items.</summary>
    public class FoodSearchPage
    {
        public List<object> Items = new();
        public int TotalPages;
    }

    /// <summary>
    /// Text search shared by every screen that hosts FMSearchOrCategoryField.
    /// Each source keeps its own order (the backend ranks the results); when both are on,
    /// a page holds half a page of each, interleaved starting with a generic food.
    /// </summary>
    public static class FoodSearchFlow
    {
        public const int PageSize = 10;

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
            int perSource = includeGeneric && includeProducts ? PageSize / 2 : PageSize;

            Task<(PaginatedGenericFoodResponse Result, ApiErrorResponse Error)> genericTask = includeGeneric
                ? genericFoodService.SearchGenericFoodsAsync(trimmed, page: page, pageSize: perSource)
                : null;
            Task<(OpenFoodFactsSearchResponse Result, ApiErrorResponse Error)> productTask = includeProducts
                ? FoodProductFlow.SearchProductsAsync(foodService, offService, trimmed, page, perSource)
                : null;

            ApiErrorResponse error = null;
            IReadOnlyList<GenericFood> generics = Array.Empty<GenericFood>();
            IReadOnlyList<OpenFoodFactsProduct> products = Array.Empty<OpenFoodFactsProduct>();

            if (genericTask != null)
            {
                var (response, genericError) = await genericTask;
                error ??= genericError;
                if (response?.items != null)
                {
                    generics = response.items;
                    result.TotalPages = Math.Max(result.TotalPages, response.totalPages);
                }
            }

            if (productTask != null)
            {
                var (response, productError) = await productTask;
                error ??= productError;
                if (response?.products != null)
                {
                    products = response.products;
                    result.TotalPages = Math.Max(result.TotalPages, ProductTotalPages(response, perSource));
                }
            }

            result.Items = Interleave(generics, products);
            return (result, error);
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

        private static int ProductTotalPages(OpenFoodFactsSearchResponse response, int pageSize)
        {
            if (response.totalPages > 0)
            {
                return response.totalPages;
            }

            int size = response.pageSize > 0 ? response.pageSize : pageSize;
            return (response.totalCount + size - 1) / size;
        }
    }
}
