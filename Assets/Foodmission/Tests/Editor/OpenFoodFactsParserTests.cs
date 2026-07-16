using NUnit.Framework;
using System;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class OpenFoodFactsParserTests
    {
        [Test]
        public void ParseProduct_ValidJson_MapsCorrectly()
        {
            string json = @"{
                ""status"": 1,
                ""code"": ""3017620422003"",
                ""product"": {
                    ""_id"": ""3017620422003"",
                    ""product_name_en"": ""Nutella 400g"",
                    ""product_name"": ""Nutella"",
                    ""generic_name"": ""Hazelnut spread with cocoa"",
                    ""brands"": ""Ferrero, Nutella"",
                    ""quantity"": ""400g"",
                    ""ingredients_text_en"": ""Sugar, vegetable oil (palm), hazelnuts (13%), skimmed milk powder (8.7%), fat-reduced cocoa (7.4%), emulsifier: lecithins (soya), vanillin"",
                    ""allergens_tags"": [""en:milk"", ""en:soybeans"", ""en:nuts""],
                    ""traces_tags"": [""en:nuts""],
                    ""nutrition_grades"": ""e"",
                    ""ecoscore_grade"": ""d"",
                    ""image_front_url"": ""https://images.openfoodfacts.org/front.jpg"",
                    ""image_url"": ""https://images.openfoodfacts.org/full.jpg"",
                    ""categories_tags"": [""en:spreads"", ""en:sweet-spreads""],
                    ""labels_tags"": [""en:vegetarian"", ""en:no-gluten""],
                    ""serving_size"": ""15g"",
                    ""origins"": ""Italy"",
                    ""manufacturing_places"": ""Alba, Italy"",
                    ""nova_group"": 4,
                    ""completeness"": 0.95,
                    ""created_t"": 1451606400,
                    ""last_modified_t"": 1483228800,
                    ""nutriments"": {
                        ""energy-kcal_100g"": 539,
                        ""energy-kj_100g"": 2252,
                        ""fat_100g"": 30.9,
                        ""saturated-fat_100g"": 10.6,
                        ""carbohydrates_100g"": 57.5,
                        ""sugars_100g"": 56.3,
                        ""proteins_100g"": 6.3,
                        ""salt_100g"": 0.107,
                        ""sodium_100g"": 0.0428,
                        ""carbon-footprint-from-known-ingredients_product"": 150.5
                    }
                }
            }";

            OpenFoodFactsProduct product = OpenFoodFactsParser.ParseProduct(json);

            Assert.IsNotNull(product);
            Assert.AreEqual("3017620422003", product.id);
            Assert.AreEqual("3017620422003", product.barcode);
            Assert.AreEqual("Nutella 400g", product.name);
            Assert.AreEqual("Hazelnut spread with cocoa", product.genericName);
            Assert.AreEqual(2, product.brands.Length);
            Assert.AreEqual("Ferrero", product.brands[0]);
            Assert.AreEqual("Nutella", product.brands[1]);
            Assert.AreEqual("400g", product.quantity);
            Assert.AreEqual("Sugar, vegetable oil (palm), hazelnuts (13%), skimmed milk powder (8.7%), fat-reduced cocoa (7.4%), emulsifier: lecithins (soya), vanillin", product.ingredients);
            Assert.AreEqual(3, product.allergens.Length);
            Assert.AreEqual("en:milk", product.allergens[0]);
            Assert.AreEqual("e", product.nutritionGrade);
            Assert.AreEqual("d", product.ecoscoreGrade);
            Assert.AreEqual("https://images.openfoodfacts.org/front.jpg", product.imageFrontUrl);
            Assert.AreEqual("https://images.openfoodfacts.org/full.jpg", product.imageUrl);
            Assert.AreEqual("15g", product.servingSize);
            Assert.AreEqual("Italy", product.origins);
            Assert.AreEqual("Alba, Italy", product.manufacturingPlaces);
            Assert.AreEqual(4, product.novaGroup);
            Assert.AreEqual(0.95f, product.completeness);
            Assert.AreEqual(new DateTime(2016, 1, 1, 0, 0, 0, DateTimeKind.Utc), product.createdAt);
            Assert.AreEqual(new DateTime(2017, 1, 1, 0, 0, 0, DateTimeKind.Utc), product.lastModified);

            Assert.IsNotNull(product.nutritionalInfo);
            Assert.AreEqual(539f, product.nutritionalInfo.energyKcal);
            Assert.AreEqual(2252f, product.nutritionalInfo.energyKj);
            Assert.AreEqual(30.9f, product.nutritionalInfo.fat);
            Assert.AreEqual(10.6f, product.nutritionalInfo.saturatedFat);
            Assert.AreEqual(57.5f, product.nutritionalInfo.carbohydrates);
            Assert.AreEqual(56.3f, product.nutritionalInfo.sugars);
            Assert.AreEqual(6.3f, product.nutritionalInfo.proteins);
            Assert.AreEqual(0.107f, product.nutritionalInfo.salt);
            Assert.AreEqual(0.0428f, product.nutritionalInfo.sodium);
            Assert.AreEqual(150.5f, product.carbonFootprint);
            Assert.IsNotEmpty(product.rawNutriments);
        }

        [Test]
        public void ParseSearch_ValidJson_MapsCorrectly()
        {
            string json = @"{
                ""count"": 15,
                ""page"": ""1"",
                ""page_size"": 10,
                ""products"": [
                    {
                        ""_id"": ""11111"",
                        ""product_name"": ""Apple"",
                        ""brands"": ""Orchard""
                    },
                    {
                        ""_id"": ""22222"",
                        ""product_name_en"": ""Orange"",
                        ""brands"": ""Citrus""
                    }
                ]
            }";

            OpenFoodFactsSearchResponse response = OpenFoodFactsParser.ParseSearch(json);

            Assert.IsNotNull(response);
            Assert.AreEqual(15, response.totalCount);
            Assert.AreEqual("1", response.page);
            Assert.AreEqual(10, response.pageSize);
            Assert.AreEqual(2, response.totalPages);
            Assert.AreEqual(2, response.products.Length);

            Assert.AreEqual("11111", response.products[0].id);
            Assert.AreEqual("Apple", response.products[0].name);
            Assert.AreEqual("Orchard", response.products[0].brands[0]);

            Assert.AreEqual("22222", response.products[1].id);
            Assert.AreEqual("Orange", response.products[1].name);
            Assert.AreEqual("Citrus", response.products[1].brands[0]);
        }
    }
}
