using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace eu.foodmission.platform
{
    public class CreateFoodProductRequest
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("barcode", NullValueHandling = NullValueHandling.Ignore)]
        public string Barcode { get; set; }

        [JsonProperty("brands", NullValueHandling = NullValueHandling.Ignore)]
        public string Brands { get; set; }

        [JsonProperty("categories", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Categories { get; set; }

        [JsonProperty("labels", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Labels { get; set; }

        [JsonProperty("quantity", NullValueHandling = NullValueHandling.Ignore)]
        public string Quantity { get; set; }

        [JsonProperty("servingSize", NullValueHandling = NullValueHandling.Ignore)]
        public string ServingSize { get; set; }

        [JsonProperty("ingredientsText", NullValueHandling = NullValueHandling.Ignore)]
        public string IngredientsText { get; set; }

        [JsonProperty("allergens", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Allergens { get; set; }

        [JsonProperty("traces", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Traces { get; set; }

        [JsonProperty("countries", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Countries { get; set; }

        [JsonProperty("origins", NullValueHandling = NullValueHandling.Ignore)]
        public string Origins { get; set; }

        [JsonProperty("manufacturingPlaces", NullValueHandling = NullValueHandling.Ignore)]
        public string ManufacturingPlaces { get; set; }

        [JsonProperty("imageUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string ImageUrl { get; set; }

        [JsonProperty("imageFrontUrl", NullValueHandling = NullValueHandling.Ignore)]
        public string ImageFrontUrl { get; set; }

        [JsonProperty("isVegan", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsVegan { get; set; }

        [JsonProperty("isVegetarian", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsVegetarian { get; set; }

        [JsonProperty("isPalmOilFree", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsPalmOilFree { get; set; }

        [JsonProperty("nutrimentsRaw", NullValueHandling = NullValueHandling.Ignore)]
        public object NutrimentsRaw { get; set; }

        [JsonProperty("nutrientLevels", NullValueHandling = NullValueHandling.Ignore)]
        public object NutrientLevels { get; set; }

        public string ToJsonBody()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
