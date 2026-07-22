using System;
using System.Collections.Generic;

namespace eu.foodmission.platform
{
    public static class OpenFoodFactsProductMapper
    {
        public static CreateFoodProductRequest ToCreateRequest(OpenFoodFactsProduct p)
        {
            if (p == null) return null;

            var req = new CreateFoodProductRequest
            {
                Name = p.name,
                Description = p.genericName,
                Barcode = p.barcode,
                Brands = p.brands != null ? string.Join(", ", p.brands) : null,
                Categories = p.categories != null ? new List<string>(p.categories) : new List<string>(),
                Labels = p.labels != null ? new List<string>(p.labels) : new List<string>(),
                Quantity = p.quantity,
                ServingSize = p.servingSize,
                IngredientsText = p.ingredients,
                Allergens = p.allergens != null ? new List<string>(p.allergens) : new List<string>(),
                Traces = p.traces != null ? new List<string>(p.traces) : new List<string>(),
                Countries = p.countries != null ? new List<string>(p.countries) : new List<string>(),
                Origins = p.origins,
                ManufacturingPlaces = p.manufacturingPlaces,
                ImageUrl = p.imageUrl,
                ImageFrontUrl = p.imageFrontUrl,
                //NutriscoreGrade = p.nutritionGrade,
                //NutriscoreScore = p.nutriscoreScore,
                //NovaGroup = p.novaGroup,
                //EcoscoreGrade = p.ecoscoreGrade,
            };

            // Detect vegan/vegetarian/palm-oil free flags from labels tags
            if (p.labels != null)
            {
                bool isVegan = false;
                bool isVegetarian = false;
                bool isPalmOilFree = true;

                foreach (var label in p.labels)
                {
                    string l = label.ToLowerInvariant();
                    if (l.Contains("vegan")) isVegan = true;
                    if (l.Contains("vegetarian")) isVegetarian = true;
                    if (l.Contains("palm-oil")) isPalmOilFree = false;
                }

                req.IsVegan = isVegan;
                req.IsVegetarian = isVegetarian;
                req.IsPalmOilFree = isPalmOilFree;
            }

            // Map nutritional JSON
            if (!string.IsNullOrEmpty(p.rawNutriments))
            {
                try
                {
                    req.NutrimentsRaw = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(p.rawNutriments);
                }
                catch (Exception)
                {
                    // Fallback
                }
            }

            return req;
        }
    }
}
