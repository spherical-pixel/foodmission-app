using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Unity.AppUI.MVVM;

using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    [ObservableObject]
    public partial class FoodInfoViewModel : ViewModelBase
    {
        private readonly IGenericFoodService _genericFoodService;
        private readonly IFoodProductService _foodProductService;

        private string _foodId;
        private string _entryContext;
        private string _foodData;

        [ObservableProperty] private FoodInfoType m_FoodType;
        [ObservableProperty] private string m_FoodName = "";
        [ObservableProperty] private string m_FoodSubtitle = "";
        [ObservableProperty] private string m_ImageUrl = "";
        [ObservableProperty] private string m_Emoji = "";
        [ObservableProperty] private string m_NutritionGrade = "";
        [ObservableProperty] private int m_NovaGroup;
        [ObservableProperty] private string m_EcoScoreGrade = "";
        [ObservableProperty] private List<TrafficLight> m_TrafficLights;
        [ObservableProperty] private List<NutritionRow> m_MacroCards;
        [ObservableProperty] private List<NutritionGroup> m_NutritionDetail;
        [ObservableProperty] private string m_Ingredients = "";
        [ObservableProperty] private string m_Allergens = "";
        [ObservableProperty] private List<MetaRow> m_MetaRows;
        [ObservableProperty] private bool m_IsLoading;
        [ObservableProperty] private ApiErrorResponse m_ErrorDetail;
        [ObservableProperty] private string m_ActionButtonText = "";
        [ObservableProperty] private bool m_ShowActionButton;

        public FoodInfoViewModel(
            IStoreService storeService,
            IGenericFoodService genericFoodService,
            IFoodProductService foodProductService)
            : base(storeService)
        {
            _genericFoodService = genericFoodService;
            _foodProductService = foodProductService;
        }

        public async Task LoadAsync(FoodInfoType type, string foodId, string entryContext, string foodData = null)
        {
            _foodId = foodId;
            _entryContext = entryContext;
            _foodData = foodData;
            FoodType = type;
            IsLoading = true;
            ErrorDetail = null;

            SetActionButton(entryContext);

            try
            {
                if (!string.IsNullOrEmpty(foodData))
                {
                    if (type == FoodInfoType.Product)
                        LoadProductFromData(foodData);
                    else
                        LoadGenericFromData(foodData);
                }
                else if (!string.IsNullOrEmpty(foodId) && Guid.TryParse(foodId, out _))
                {
                    if (type == FoodInfoType.Product)
                        await LoadProductAsync(foodId);
                    else
                        await LoadGenericAsync(foodId);
                }
                else
                {
                    Debug.LogWarning($"[{GetType().Name}] LoadAsync — no foodData and foodId is not a valid UUID: {foodId}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadAsync failed: {ex.Message}");
            }

            IsLoading = false;
        }

        private async Task LoadProductAsync(string foodId)
        {
            var (detail, error) = await _foodProductService.GetFoodProductDetailAsync(foodId);

            if (error != null)
            {
                ErrorDetail = error;
                return;
            }

            if (detail == null)
                return;

            FoodName = detail.name ?? "";
            FoodSubtitle = detail.brands ?? "";
            ImageUrl = detail.imageFrontUrl ?? detail.imageUrl ?? "";
            NutritionGrade = detail.nutritionGrade ?? "";
            NovaGroup = detail.novaGroup ?? 0;
            EcoScoreGrade = detail.ecoscoreGrade ?? "";
            Ingredients = detail.ingredientsText ?? "";
            Allergens = detail.allergens != null ? string.Join(", ", detail.allergens) : "";

            TrafficLights = BuildTrafficLights(detail.nutrientLevels);
            MacroCards = BuildProductMacroCards(detail.nutrimentsRaw);
            NutritionDetail = BuildProductNutritionDetail(detail.nutrimentsRaw);
            MetaRows = BuildProductMetaRows(detail);
        }

        private async Task LoadGenericAsync(string foodId)
        {
            var (detail, error) = await _genericFoodService.GetGenericFoodDetailAsync(foodId);

            if (error != null)
            {
                ErrorDetail = error;
                return;
            }

            if (detail == null)
                return;

            FoodName = detail.foodName ?? "";
            FoodSubtitle = detail.foodGroup ?? "";
            Emoji = GetEmojiForFoodGroup(detail.foodGroup);
            NutritionGrade = "";
            NovaGroup = 0;
            EcoScoreGrade = "";
            Ingredients = "";
            Allergens = detail.containsTracesOf ?? "";

            MacroCards = BuildGenericMacroCards(detail);
            NutritionDetail = BuildGenericNutritionDetail(detail);
            MetaRows = BuildGenericMetaRows(detail);
        }

        private void LoadProductFromData(string foodData)
        {
            try
            {
                var product = JsonConvert.DeserializeObject<OpenFoodFactsProduct>(foodData);
                if (product == null)
                    return;

                FoodName = product.name ?? "";
                FoodSubtitle = product.brands != null && product.brands.Length > 0 ? string.Join(", ", product.brands) : "";
                ImageUrl = product.imageFrontUrl ?? "";
                NutritionGrade = product.nutritionGrade ?? "";
                NovaGroup = 0;
                EcoScoreGrade = product.ecoscoreGrade ?? "";
                Ingredients = product.ingredients ?? "";
                Allergens = product.allergens != null ? string.Join(", ", product.allergens) : "";

                MacroCards = BuildMacroCardsFromNutritionalInfo(product.nutritionalInfo);
                NutritionDetail = BuildNutritionDetailFromNutritionalInfo(product.nutritionalInfo);
                MetaRows = new List<MetaRow>();

                if (!string.IsNullOrEmpty(product.quantity))
                    MetaRows.Add(new MetaRow(GetLocString("META_QUANTITY"), product.quantity));
                if (!string.IsNullOrEmpty(product.barcode))
                    MetaRows.Add(new MetaRow(GetLocString("META_BARCODE"), product.barcode));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadProductFromData parse error: {ex.Message}");
            }
        }

        private void LoadGenericFromData(string foodData)
        {
            try
            {
                var genericFood = JsonConvert.DeserializeObject<GenericFood>(foodData);
                if (genericFood == null)
                    return;

                FoodName = genericFood.foodName ?? "";
                FoodSubtitle = genericFood.foodGroup ?? "";
                Emoji = GetEmojiForFoodGroup(genericFood.foodGroup);
                NutritionGrade = "";
                NovaGroup = 0;
                EcoScoreGrade = "";
                Ingredients = "";
                Allergens = "";

                MacroCards = new List<NutritionRow>
                {
                    new(GetLocString("NUTR_ENERGY_KCAL"), null, "kcal"),
                    new(GetLocString("NUTR_PROTEINS"), null, "g"),
                    new(GetLocString("NUTR_FAT"), null, "g"),
                    new(GetLocString("NUTR_CARBOHYDRATES"), null, "g")
                };
                NutritionDetail = new List<NutritionGroup>();
                MetaRows = new List<MetaRow>();

                if (!string.IsNullOrEmpty(genericFood.id) && Guid.TryParse(genericFood.id, out _))
                {
                    _ = LoadGenericAsync(genericFood.id);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadGenericFromData parse error: {ex.Message}");
            }
        }

        private List<NutritionRow> BuildMacroCardsFromNutritionalInfo(NutritionalInfo info)
        {
            if (info == null)
                return new List<NutritionRow>();

            return new List<NutritionRow>
            {
                new(GetLocString("NUTR_ENERGY_KCAL"), info.energyKcal > 0 ? info.energyKcal : (float?)null, "kcal"),
                new(GetLocString("NUTR_PROTEINS"), info.proteins > 0 ? info.proteins : (float?)null, "g"),
                new(GetLocString("NUTR_FAT"), info.fat > 0 ? info.fat : (float?)null, "g"),
                new(GetLocString("NUTR_CARBOHYDRATES"), info.carbohydrates > 0 ? info.carbohydrates : (float?)null, "g")
            };
        }

        private List<NutritionGroup> BuildNutritionDetailFromNutritionalInfo(NutritionalInfo info)
        {
            if (info == null)
                return new List<NutritionGroup>();

            var rows = new List<NutritionRow>
            {
                new(GetLocString("NUTR_ENERGY_KJ"), info.energyKj > 0 ? info.energyKj : (float?)null, "kJ"),
                new(GetLocString("NUTR_SATURATED_FAT"), info.saturatedFat > 0 ? info.saturatedFat : (float?)null, "g"),
                new(GetLocString("NUTR_SUGARS"), info.sugars > 0 ? info.sugars : (float?)null, "g"),
                new(GetLocString("NUTR_SALT"), info.salt > 0 ? info.salt : (float?)null, "g"),
                new(GetLocString("NUTR_SODIUM"), info.sodium > 0 ? info.sodium : (float?)null, "mg")
            };

            var filtered = rows.Where(r => r.Value.HasValue && r.Value.Value > 0).ToList();
            if (filtered.Count == 0)
                return new List<NutritionGroup>();

            return new List<NutritionGroup>
            {
                new(GetLocString("NUTR_GROUP_MACROS"), filtered)
            };
        }

        private void SetActionButton(string context)
        {
            switch (context)
            {
                case "pantry":
                    ActionButtonText = GetLocStringOrFallback("ADD_TO_PANTRY", "Add to pantry");
                    ShowActionButton = true;
                    break;
                case "shoppingList":
                    ActionButtonText = GetLocStringOrFallback("ADD_TO_SHOPPING_LIST", "Add to shopping list");
                    ShowActionButton = true;
                    break;
                case "mealLog":
                    ActionButtonText = "";
                    ShowActionButton = false;
                    break;
                default:
                    ActionButtonText = "";
                    ShowActionButton = false;
                    break;
            }
        }

        public void OnActionButtonClicked()
        {
            if (!ShowActionButton)
                return;

            _store.Dispatch(AppActions.foodInfoAddRequested.Invoke(new AddToContextRequestedAction
            {
                FoodType = FoodType,
                FoodId = _foodId,
                EntryContext = _entryContext,
                FoodData = _foodData
            }));
        }

        private List<TrafficLight> BuildTrafficLights(string nutrientLevelsJson)
        {
            if (string.IsNullOrEmpty(nutrientLevelsJson))
                return new List<TrafficLight>();

            try
            {
                JObject obj = JObject.Parse(nutrientLevelsJson);
                return new List<TrafficLight>
                {
                    new(GetLocString("TL_FAT"), obj["fat"]?.ToString()),
                    new(GetLocString("TL_SATURATED_FAT"), obj["saturated-fat"]?.ToString()),
                    new(GetLocString("TL_SUGARS"), obj["sugars"]?.ToString()),
                    new(GetLocString("TL_SALT"), obj["salt"]?.ToString())
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] BuildTrafficLights parse error: {ex.Message}");
                return new List<TrafficLight>();
            }
        }

        private List<NutritionRow> BuildProductMacroCards(string nutrimentsRawJson)
        {
            if (string.IsNullOrEmpty(nutrimentsRawJson))
                return new List<NutritionRow>();

            try
            {
                JObject n = JObject.Parse(nutrimentsRawJson);
                return new List<NutritionRow>
                {
                    new(GetLocString("NUTR_ENERGY_KCAL"), GetFloat(n, "energy-kcal_100g") ?? GetFloat(n, "energy-kcal"), "kcal"),
                    new(GetLocString("NUTR_PROTEINS"), GetFloat(n, "proteins_100g") ?? GetFloat(n, "proteins"), "g"),
                    new(GetLocString("NUTR_FAT"), GetFloat(n, "fat_100g") ?? GetFloat(n, "fat"), "g"),
                    new(GetLocString("NUTR_CARBOHYDRATES"), GetFloat(n, "carbohydrates_100g") ?? GetFloat(n, "carbohydrates"), "g")
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] BuildProductMacroCards parse error: {ex.Message}");
                return new List<NutritionRow>();
            }
        }

        private List<NutritionGroup> BuildProductNutritionDetail(string nutrimentsRawJson)
        {
            if (string.IsNullOrEmpty(nutrimentsRawJson))
                return new List<NutritionGroup>();

            try
            {
                JObject n = JObject.Parse(nutrimentsRawJson);
                var rows = new List<NutritionRow>
                {
                    new(GetLocString("NUTR_ENERGY_KJ"), GetFloat(n, "energy-kj_100g") ?? GetFloat(n, "energy-kj"), "kJ"),
                    new(GetLocString("NUTR_SATURATED_FAT"), GetFloat(n, "saturated-fat_100g") ?? GetFloat(n, "saturated-fat"), "g"),
                    new(GetLocString("NUTR_SUGARS"), GetFloat(n, "sugars_100g") ?? GetFloat(n, "sugars"), "g"),
                    new(GetLocString("NUTR_FIBER"), GetFloat(n, "fiber_100g") ?? GetFloat(n, "fiber"), "g"),
                    new(GetLocString("NUTR_SALT"), GetFloat(n, "salt_100g") ?? GetFloat(n, "salt"), "g"),
                    new(GetLocString("NUTR_SODIUM"), GetFloat(n, "sodium_100g") ?? GetFloat(n, "sodium"), "mg")
                };

                return new List<NutritionGroup>
                {
                    new(GetLocString("NUTR_GROUP_MACROS"), rows.Where(r => r.Value.HasValue && r.Value.Value > 0).ToList())
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] BuildProductNutritionDetail parse error: {ex.Message}");
                return new List<NutritionGroup>();
            }
        }

        private List<NutritionRow> BuildGenericMacroCards(GenericFoodDetail d)
        {
            return new List<NutritionRow>
            {
                new(GetLocString("NUTR_ENERGY_KCAL"), d.energyKcal, "kcal"),
                new(GetLocString("NUTR_PROTEINS"), d.proteins, "g"),
                new(GetLocString("NUTR_FAT"), d.fat, "g"),
                new(GetLocString("NUTR_CARBOHYDRATES"), d.carbohydrates, "g")
            };
        }

        private List<NutritionGroup> BuildGenericNutritionDetail(GenericFoodDetail d)
        {
            string L(string key) => GetLocString(key);

            var macros = new List<NutritionRow>
            {
                new(L("NUTR_ENERGY_KJ"), d.energyKj, "kJ"),
                new(L("NUTR_WATER"), d.water, "g"),
                new(L("NUTR_PROTEINS_PLANT"), d.proteinsPlant, "g"),
                new(L("NUTR_PROTEINS_ANIMAL"), d.proteinsAnimal, "g"),
                new(L("NUTR_SUGARS"), d.sugars, "g"),
                new(L("NUTR_ADDED_SUGARS"), d.addedSugars, "g"),
                new(L("NUTR_STARCH"), d.starch, "g"),
                new(L("NUTR_FIBER"), d.fiber, "g")
            };

            var fats = new List<NutritionRow>
            {
                new(L("NUTR_SATURATED_FAT"), d.saturatedFat, "g"),
                new(L("NUTR_MONO_UNSATURATED_FAT"), d.monoUnsaturatedFat, "g"),
                new(L("NUTR_POLY_UNSATURATED_FAT"), d.polyUnsaturatedFat, "g"),
                new(L("NUTR_OMEGA3_FAT"), d.omega3Fat, "g"),
                new(L("NUTR_OMEGA6_FAT"), d.omega6Fat, "g"),
                new(L("NUTR_TRANS_FAT"), d.transFat, "g")
            };

            var vitamins = new List<NutritionRow>
            {
                new(L("NUTR_VITAMIN_A"), d.vitaminARae, "\u00b5g"),
                new(L("NUTR_VITAMIN_D"), d.vitaminD, "\u00b5g"),
                new(L("NUTR_VITAMIN_E"), d.vitaminE, "mg"),
                new(L("NUTR_VITAMIN_K"), d.vitaminK, "\u00b5g"),
                new(L("NUTR_VITAMIN_C"), d.vitaminC, "mg"),
                new(L("NUTR_THIAMIN"), d.thiamin, "mg"),
                new(L("NUTR_RIBOFLAVIN"), d.riboflavin, "mg"),
                new(L("NUTR_VITAMIN_B6"), d.vitaminB6, "mg"),
                new(L("NUTR_VITAMIN_B12"), d.vitaminB12, "\u00b5g"),
                new(L("NUTR_FOLATE_TOTAL"), d.folateTotal, "\u00b5g")
            };

            var minerals = new List<NutritionRow>
            {
                new(L("NUTR_SODIUM"), d.sodium, "mg"),
                new(L("NUTR_POTASSIUM"), d.potassium, "mg"),
                new(L("NUTR_CALCIUM"), d.calcium, "mg"),
                new(L("NUTR_PHOSPHORUS"), d.phosphorus, "mg"),
                new(L("NUTR_MAGNESIUM"), d.magnesium, "mg"),
                new(L("NUTR_IRON"), d.iron, "mg"),
                new(L("NUTR_ZINC"), d.zinc, "mg")
            };

            string GT(string key) => GetLocString(key);

            return new List<NutritionGroup>
            {
                new(GT("NUTR_GROUP_MACROS"), macros.Where(r => r.Value.HasValue && r.Value.Value > 0).ToList()),
                new(GT("NUTR_GROUP_FATS"), fats.Where(r => r.Value.HasValue && r.Value.Value > 0).ToList()),
                new(GT("NUTR_GROUP_VITAMINS"), vitamins.Where(r => r.Value.HasValue && r.Value.Value > 0).ToList()),
                new(GT("NUTR_GROUP_MINERALS"), minerals.Where(r => r.Value.HasValue && r.Value.Value > 0).ToList())
            };
        }

        private List<MetaRow> BuildProductMetaRows(FoodProductDetail d)
        {
            var rows = new List<MetaRow>();

            if (!string.IsNullOrEmpty(d.quantity))
                rows.Add(new MetaRow(GetLocString("META_QUANTITY"), d.quantity));
            if (!string.IsNullOrEmpty(d.servingSize))
                rows.Add(new MetaRow(GetLocString("META_SERVING_SIZE"), d.servingSize));
            if (!string.IsNullOrEmpty(d.brands))
                rows.Add(new MetaRow(GetLocString("META_BRANDS"), d.brands));
            if (!string.IsNullOrEmpty(d.origins))
                rows.Add(new MetaRow(GetLocString("META_ORIGINS"), d.origins));
            if (!string.IsNullOrEmpty(d.manufacturingPlaces))
                rows.Add(new MetaRow(GetLocString("META_MANUFACTURING"), d.manufacturingPlaces));
            if (d.countries != null && d.countries.Length > 0)
                rows.Add(new MetaRow(GetLocString("META_COUNTRIES"), string.Join(", ", d.countries)));
            if (d.labels != null && d.labels.Length > 0)
                rows.Add(new MetaRow(GetLocString("META_LABELS"), string.Join(", ", d.labels)));
            if (!string.IsNullOrEmpty(d.barcode))
                rows.Add(new MetaRow(GetLocString("META_BARCODE"), d.barcode));
            if (d.carbonFootprint.HasValue && d.carbonFootprint.Value > 0)
                rows.Add(new MetaRow(GetLocString("META_CARBON_FOOTPRINT"), $"{d.carbonFootprint.Value:F1} kg CO\u2082e/kg"));

            return rows;
        }

        private List<MetaRow> BuildGenericMetaRows(GenericFoodDetail d)
        {
            var rows = new List<MetaRow>();

            if (!string.IsNullOrEmpty(d.quantity))
                rows.Add(new MetaRow(GetLocString("META_QUANTITY"), d.quantity));
            if (!string.IsNullOrEmpty(d.synonym))
                rows.Add(new MetaRow(GetLocString("NO_DATA_AVAILABLE"), d.synonym));

            return rows;
        }

        private static float? GetFloat(JObject obj, string key)
        {
            if (obj[key] != null)
            {
                if (obj[key].Type == JTokenType.Float)
                    return (float)obj[key];
                if (obj[key].Type == JTokenType.Integer)
                    return (float)(int)obj[key];
            }
            return null;
        }

        private static string GetEmojiForFoodGroup(string foodGroup)
        {
            if (string.IsNullOrEmpty(foodGroup))
                return "\ud83c\udf7d\ufe0f";

            var emojis = Components.FMSearchOrCategoryField.CategoryEmojisPublic;
            return emojis.TryGetValue(foodGroup, out string emoji) ? emoji : "\ud83c\udf7d\ufe0f";
        }

        private static string GetLocString(string key)
        {
            return LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
        }

        private static string GetLocStringOrFallback(string key, string fallback)
        {
            string result = LocalizationSettings.StringDatabase.GetLocalizedString("UI", key);
            return string.IsNullOrEmpty(result) || result == key ? fallback : result;
        }
    }
}
