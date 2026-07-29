using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace eu.foodmission.platform
{
    public static class FoodProductFlow
    {
        public static Func<bool> UseDirectClientOverride { get; set; } = () => ApiConfig.UseDirectOpenFoodFactsClient;

        public static async Task<(List<OpenFoodFactsProduct> Result, ApiErrorResponse Error)> SearchProductsAsync(
            IFoodProductService foodService,
            IOpenFoodFactsClientService offService,
            string query)
        {
            if (UseDirectClientOverride())
            {
                if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 3)
                {
                    return (new List<OpenFoodFactsProduct>(), null);
                }

                var (response, error) = await offService.SearchAsync(query, 1);
                if (error != null)
                {
                    return (null, error);
                }

                return (response?.products != null ? new List<OpenFoodFactsProduct>(response.products) : new List<OpenFoodFactsProduct>(), null);
            }
            else
            {
                var (response, error) = await foodService.SearchOpenFoodFactsAsync(query, 1, 20);
                if (error != null)
                {
                    return (null, error);
                }

                return (response?.products != null ? new List<OpenFoodFactsProduct>(response.products) : new List<OpenFoodFactsProduct>(), null);
            }
        }

        public static async Task<(FoodProduct Result, ApiErrorResponse Error)> ImportByBarcodeAsync(
            IFoodProductService foodService,
            IOpenFoodFactsClientService offService,
            string barcode)
        {
            if (UseDirectClientOverride())
            {
                // 1. Check local database first by barcode
                var (existing, findError) = await foodService.FindByBarcodeAsync(barcode, includeOpenFoodFacts: false);
                if (findError == null && existing != null)
                {
                    UnityEngine.Debug.Log($"[FoodProductFlow] Product {barcode} found in local database. Returning cached item.");
                    return (existing, null);
                }

                UnityEngine.Debug.Log($"[FoodProductFlow] DB Miss for barcode: {barcode}. Proceeding to query OpenFoodFacts directly...");

                // 2. Query OpenFoodFacts client directly on database miss
                var (offProduct, offError) = await offService.GetByBarcodeAsync(barcode);
                if (offError != null)
                {
                    UnityEngine.Debug.LogWarning($"[FoodProductFlow] OpenFoodFacts lookup failed for barcode {barcode}: {offError.message}");
                    return (null, offError);
                }

                UnityEngine.Debug.Log($"[FoodProductFlow] Product {barcode} ({offProduct.name}) found in OpenFoodFacts. Creating record in backend DB...");

                // 3. Create food product in database using POST /api/v1/food-products
                var createRequest = OpenFoodFactsProductMapper.ToCreateRequest(offProduct);
                var (createdFood, createError) = await foodService.CreateAsync(createRequest);
                if (createError != null)
                {
                    UnityEngine.Debug.LogError($"[FoodProductFlow] Backend database creation failed for barcode {barcode}: {createError.message}");
                    return (null, createError);
                }

                UnityEngine.Debug.Log($"[FoodProductFlow] Product {barcode} successfully created in backend DB.");
                return (createdFood, null);
            }
            else
            {
                // Proxy flow via backend food-products endpoints:
                // 1. Search backend SQL database first by barcode via GET /api/v1/food-products?barcode={barcode}
                var (dbRes, dbErr) = await foodService.SearchFoodsByBarcodeAsync(barcode);
                if (dbErr == null && dbRes?.data != null && dbRes.data.Length > 0)
                {
                    var match = Array.Find(dbRes.data, p => string.Equals(p.barcode, barcode, StringComparison.OrdinalIgnoreCase));
                    if (match != null && Guid.TryParse(match.id, out _))
                    {
                        return (match, null);
                    }
                }

                // 2. Not in backend SQL DB yet: import from OpenFoodFacts via backend POST /api/v1/food-products/import/openfoodfacts/{barcode}
                var (imported, importError) = await foodService.ImportFromBarcodeAsync(barcode);
                if (importError == null && imported != null && Guid.TryParse(imported.id, out _))
                {
                    return (imported, null);
                }

                // 3. Fallback: if import returned error (e.g. 400 because product exists), query DB again by barcode
                var (dbRes2, dbErr2) = await foodService.SearchFoodsByBarcodeAsync(barcode);
                if (dbErr2 == null && dbRes2?.data != null && dbRes2.data.Length > 0)
                {
                    var match2 = Array.Find(dbRes2.data, p => string.Equals(p.barcode, barcode, StringComparison.OrdinalIgnoreCase));
                    if (match2 != null && Guid.TryParse(match2.id, out _))
                    {
                        return (match2, null);
                    }
                }

                return (null, importError ?? dbErr ?? dbErr2);
            }
        }
    }
}
