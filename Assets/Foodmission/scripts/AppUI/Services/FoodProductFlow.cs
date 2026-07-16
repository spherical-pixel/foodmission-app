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
                // Execute old proxy chain
                var (existing, findError) = await foodService.FindByBarcodeAsync(barcode, includeOpenFoodFacts: false);
                if (findError == null && existing != null)
                {
                    return (existing, null);
                }

                var (imported, importError) = await foodService.ImportFromBarcodeAsync(barcode);
                if (importError != null)
                {
                    // Fallback to searching if already imported or conflict
                    var (existingFood, findErr2) = await foodService.FindByBarcodeAsync(barcode, includeOpenFoodFacts: true);
                    if (findErr2 == null && existingFood != null)
                    {
                        return (existingFood, null);
                    }
                    return (null, importError);
                }
                return (imported, null);
            }
        }
    }
}
