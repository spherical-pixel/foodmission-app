using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace eu.foodmission.platform
{
    public class OffNutrimentsRaw
    {
        [JsonProperty("energy-kcal_100g")] public float? energy_kcal_100g { get; set; }
        [JsonProperty("energy-kcal")] public float? energy_kcal { get; set; }
        [JsonProperty("energy-kj_100g")] public float? energy_kj_100g { get; set; }
        [JsonProperty("energy-kj")] public float? energy_kj { get; set; }
        
        [JsonProperty("fat_100g")] public float? fat_100g { get; set; }
        [JsonProperty("fat")] public float? fat { get; set; }
        
        [JsonProperty("saturated-fat_100g")] public float? saturated_fat_100g { get; set; }
        [JsonProperty("saturated-fat")] public float? saturated_fat { get; set; }
        
        [JsonProperty("trans-fat_100g")] public float? trans_fat_100g { get; set; }
        [JsonProperty("trans-fat")] public float? trans_fat { get; set; }
        
        [JsonProperty("cholesterol_100g")] public float? cholesterol_100g { get; set; }
        [JsonProperty("cholesterol")] public float? cholesterol { get; set; }
        
        [JsonProperty("carbohydrates_100g")] public float? carbohydrates_100g { get; set; }
        [JsonProperty("carbohydrates")] public float? carbohydrates { get; set; }
        
        [JsonProperty("sugars_100g")] public float? sugars_100g { get; set; }
        [JsonProperty("sugars")] public float? sugars { get; set; }
        
        [JsonProperty("fiber_100g")] public float? fiber_100g { get; set; }
        [JsonProperty("fiber")] public float? fiber { get; set; }
        
        [JsonProperty("proteins_100g")] public float? proteins_100g { get; set; }
        [JsonProperty("proteins")] public float? proteins { get; set; }
        
        [JsonProperty("salt_100g")] public float? salt_100g { get; set; }
        [JsonProperty("salt")] public float? salt { get; set; }
        
        [JsonProperty("sodium_100g")] public float? sodium_100g { get; set; }
        [JsonProperty("sodium")] public float? sodium { get; set; }
        
        [JsonProperty("vitamin-a_100g")] public float? vitamin_a_100g { get; set; }
        [JsonProperty("vitamin-a")] public float? vitamin_a { get; set; }
        
        [JsonProperty("vitamin-c_100g")] public float? vitamin_c_100g { get; set; }
        [JsonProperty("vitamin-c")] public float? vitamin_c { get; set; }
        
        [JsonProperty("calcium_100g")] public float? calcium_100g { get; set; }
        [JsonProperty("calcium")] public float? calcium { get; set; }
        
        [JsonProperty("iron_100g")] public float? iron_100g { get; set; }
        [JsonProperty("iron")] public float? iron { get; set; }

        [JsonProperty("carbon-footprint-from-known-ingredients_product")]
        public float? carbon_footprint_from_known_ingredients_product { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraNutriments { get; set; }
    }

    public class OffNutriscoreRaw
    {
        public int? score { get; set; }
    }

    public class OffProductRaw
    {
        public string _id { get; set; }
        public int? nutriscore_score { get; set; }
        public OffNutriscoreRaw nutriscore_data { get; set; }
        public string product_name { get; set; }
        public string generic_name { get; set; }
        public string brands { get; set; }
        public string[] categories_tags { get; set; }
        public string[] labels_tags { get; set; }
        public string quantity { get; set; }
        public string serving_size { get; set; }
        public string[] packaging_tags { get; set; }
        public string origins { get; set; }
        public string manufacturing_places { get; set; }
        public string ingredients_text { get; set; }
        public string[] allergens_tags { get; set; }
        public string[] traces_tags { get; set; }
        public string nutrition_grades { get; set; }
        public int? nova_group { get; set; }
        public string ecoscore_grade { get; set; }
        public string image_url { get; set; }
        public string image_front_url { get; set; }
        public string image_nutrition_url { get; set; }
        public string image_ingredients_url { get; set; }
        public string[] countries_tags { get; set; }
        public string[] stores_tags { get; set; }
        public float? completeness { get; set; }
        public long? created_t { get; set; }
        public long? last_modified_t { get; set; }
        public string nutrition_data_per { get; set; }
        public OffNutrimentsRaw nutriments { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraData { get; set; }
    }

    public class OffSearchResponseRaw
    {
        public List<OffProductRaw> products { get; set; }
        public int count { get; set; }
        public string page { get; set; }
        public int page_size { get; set; }
    }

    public static class OpenFoodFactsParser
    {
        public static OpenFoodFactsProduct ParseProduct(string json, string targetLang = "en")
        {
            if (string.IsNullOrEmpty(json)) return null;

            // Barcode lookup returns { code: "...", status: 1, product: { ... } }
            var wrapper = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (wrapper != null && wrapper.TryGetValue("product", out object productObj))
            {
                var rawProduct = JsonConvert.DeserializeObject<OffProductRaw>(JsonConvert.SerializeObject(productObj));
                return MapProduct(rawProduct, targetLang);
            }

            return null;
        }

        public static OpenFoodFactsSearchResponse ParseSearch(string json, string targetLang = "en")
        {
            if (string.IsNullOrEmpty(json)) return null;

            var raw = JsonConvert.DeserializeObject<OffSearchResponseRaw>(json);
            if (raw == null) return null;

            var response = new OpenFoodFactsSearchResponse
            {
                totalCount = raw.count,
                page = raw.page,
                pageSize = raw.page_size,
                products = raw.products != null 
                    ? Array.ConvertAll(raw.products.ToArray(), p => MapProduct(p, targetLang)) 
                    : Array.Empty<OpenFoodFactsProduct>(),
                totalPages = raw.page_size > 0 ? (int)Math.Ceiling((double)raw.count / raw.page_size) : 0
            };

            return response;
        }

        public static OpenFoodFactsProduct MapProduct(OffProductRaw raw)
        {
            return MapProduct(raw, "en");
        }

        public static OpenFoodFactsProduct MapProduct(OffProductRaw raw, string targetLang)
        {
            if (raw == null) return null;

            var mapped = new OpenFoodFactsProduct
            {
                id = raw._id,
                barcode = raw._id,
                name = GetLocalizedField(raw, "product_name", targetLang),
                genericName = raw.generic_name,
                brands = !string.IsNullOrEmpty(raw.brands)
                    ? Array.ConvertAll(raw.brands.Split(','), b => b.Trim())
                    : Array.Empty<string>(),
                quantity = raw.quantity,
                ingredients = GetLocalizedField(raw, "ingredients_text", targetLang),
                allergens = raw.allergens_tags ?? Array.Empty<string>(),
                traces = raw.traces_tags ?? Array.Empty<string>(),
                nutritionGrade = raw.nutrition_grades,
                ecoscoreGrade = raw.ecoscore_grade,
                imageFrontUrl = raw.image_front_url,
                
                // Extended properties
                imageUrl = raw.image_url,
                categories = raw.categories_tags ?? Array.Empty<string>(),
                labels = raw.labels_tags ?? Array.Empty<string>(),
                servingSize = raw.serving_size,
                origins = raw.origins,
                manufacturingPlaces = raw.manufacturing_places,
                novaGroup = raw.nova_group,
                nutriscoreScore = raw.nutriscore_score ?? raw.nutriscore_data?.score,
                nutritionDataPer = raw.nutrition_data_per,
                imageNutritionUrl = raw.image_nutrition_url,
                imageIngredientsUrl = raw.image_ingredients_url,
                countries = raw.countries_tags ?? Array.Empty<string>(),
                stores = raw.stores_tags ?? Array.Empty<string>(),
                completeness = raw.completeness,
                createdAt = raw.created_t.HasValue ? (DateTime?)DateTimeOffset.FromUnixTimeSeconds(raw.created_t.Value).UtcDateTime : null,
                lastModified = raw.last_modified_t.HasValue ? (DateTime?)DateTimeOffset.FromUnixTimeSeconds(raw.last_modified_t.Value).UtcDateTime : null,
            };

            if (raw.nutriments != null)
            {
                mapped.carbonFootprint = raw.nutriments.carbon_footprint_from_known_ingredients_product;
                mapped.rawNutriments = JsonConvert.SerializeObject(raw.nutriments);
                mapped.nutritionalInfo = new NutritionalInfo
                {
                    energyKcal = raw.nutriments.energy_kcal_100g ?? raw.nutriments.energy_kcal ?? 0f,
                    energyKj = raw.nutriments.energy_kj_100g ?? raw.nutriments.energy_kj ?? 0f,
                    fat = raw.nutriments.fat_100g ?? raw.nutriments.fat ?? 0f,
                    saturatedFat = raw.nutriments.saturated_fat_100g ?? raw.nutriments.saturated_fat ?? 0f,
                    carbohydrates = raw.nutriments.carbohydrates_100g ?? raw.nutriments.carbohydrates ?? 0f,
                    sugars = raw.nutriments.sugars_100g ?? raw.nutriments.sugars ?? 0f,
                    proteins = raw.nutriments.proteins_100g ?? raw.nutriments.proteins ?? 0f,
                    salt = raw.nutriments.salt_100g ?? raw.nutriments.salt ?? 0f,
                    sodium = raw.nutriments.sodium_100g ?? raw.nutriments.sodium ?? 0f,
                };
            }

            return mapped;
        }

        public static string GetLocalizedField(OffProductRaw raw, string fieldName, string targetLang = "en")
        {
            if (raw == null) return "";

            string lang = string.IsNullOrEmpty(targetLang) ? "en" : targetLang.ToLowerInvariant();
            if (lang.Contains("-")) lang = lang.Split('-')[0];

            // 1. Check ExtraData for fieldName_lang (e.g. product_name_es, product_name_fr, ingredients_text_ca)
            if (raw.ExtraData != null && raw.ExtraData.TryGetValue($"{fieldName}_{lang}", out JToken token))
            {
                string val = token?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(val)) return val;
            }

            // 2. Check main field (product_name, ingredients_text)
            if (fieldName == "product_name" && !string.IsNullOrEmpty(raw.product_name))
                return raw.product_name;
            if (fieldName == "ingredients_text" && !string.IsNullOrEmpty(raw.ingredients_text))
                return raw.ingredients_text;

            // 3. Check English fallback (fieldName_en)
            if (lang != "en" && raw.ExtraData != null && raw.ExtraData.TryGetValue($"{fieldName}_en", out JToken enToken))
            {
                string val = enToken?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(val)) return val;
            }

            // 4. Fallback to main field or generic name
            if (fieldName == "product_name")
                return !string.IsNullOrEmpty(raw.product_name) ? raw.product_name : (!string.IsNullOrEmpty(raw.generic_name) ? raw.generic_name : "Unknown Product");

            if (fieldName == "ingredients_text")
                return raw.ingredients_text ?? "";

            return "";
        }
    }
}
