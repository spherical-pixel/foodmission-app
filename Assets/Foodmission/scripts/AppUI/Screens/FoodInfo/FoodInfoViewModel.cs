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
        private readonly IOpenFoodFactsClientService _openFoodFactsClientService;

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
        [ObservableProperty] private string m_Description = "";
        [ObservableProperty] private string m_Traces = "";
        [ObservableProperty] private string m_Categories = "";
        [ObservableProperty] private string m_Stores = "";
        [ObservableProperty] private DietaryFlags m_DietaryFlags;

        public FoodInfoViewModel(
            IStoreService storeService,
            IGenericFoodService genericFoodService,
            IFoodProductService foodProductService,
            IOpenFoodFactsClientService openFoodFactsClientService = null)
            : base(storeService)
        {
            _genericFoodService = genericFoodService;
            _foodProductService = foodProductService;
            _openFoodFactsClientService = openFoodFactsClientService ?? App.current?.services?.GetService<IOpenFoodFactsClientService>();
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
                        await LoadGenericFromData(foodData);
                }
                else if (!string.IsNullOrEmpty(foodId))
                {
                    if (type == FoodInfoType.Product)
                    {
                        if (Guid.TryParse(foodId, out _))
                            await LoadProductAsync(foodId);
                        else
                            await TryEnrichFromOpenFoodFactsAsync(foodId);
                    }
                    else
                    {
                        if (Guid.TryParse(foodId, out _))
                            await LoadGenericAsync(foodId);
                    }
                }
                else
                {
                    Debug.LogWarning($"[{GetType().Name}] LoadAsync — no foodData and foodId is empty");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadAsync failed: {ex.Message}");
            }

            IsLoading = false;
        }

        private async Task TryEnrichFromOpenFoodFactsAsync(string barcode)
        {
            if (string.IsNullOrEmpty(barcode) || _openFoodFactsClientService == null)
                return;

            try
            {
                var (product, error) = await _openFoodFactsClientService.GetByBarcodeAsync(barcode);
                if (error != null || product == null)
                {
                    Debug.Log($"[{GetType().Name}] OFF lookup for barcode {barcode} returned no product");
                    return;
                }

                PopulateFromOpenFoodFactsProduct(product);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] TryEnrichFromOpenFoodFactsAsync failed for barcode {barcode}: {ex.Message}");
            }
        }

        private void PopulateFromOpenFoodFactsProduct(OpenFoodFactsProduct product)
        {
            if (product == null) return;

            if (!string.IsNullOrEmpty(product.name)) FoodName = product.name;
            if (product.brands != null && product.brands.Length > 0) FoodSubtitle = string.Join(", ", product.brands);
            string imgUrl = !string.IsNullOrEmpty(product.imageFrontUrl) ? product.imageFrontUrl : product.imageUrl;
            if (!string.IsNullOrEmpty(imgUrl)) ImageUrl = imgUrl;
            NutritionGrade = !string.IsNullOrEmpty(product.nutritionGrade) ? product.nutritionGrade : "unknown";
            NovaGroup = product.novaGroup.HasValue && product.novaGroup.Value >= 1 && product.novaGroup.Value <= 4 ? product.novaGroup.Value : 0;
            EcoScoreGrade = !string.IsNullOrEmpty(product.ecoscoreGrade) ? product.ecoscoreGrade : "unknown";
            if (!string.IsNullOrEmpty(product.ingredients)) Ingredients = product.ingredients;
            if (product.allergens != null && product.allergens.Length > 0) Allergens = FormatTagsList(product.allergens, "en");
            if (!string.IsNullOrEmpty(product.genericName)) Description = product.genericName;
            if (product.traces != null && product.traces.Length > 0) Traces = FormatTagsList(product.traces, "en");
            if (product.categories != null && product.categories.Length > 0) Categories = FormatTagsList(product.categories, "en");
            if (product.stores != null && product.stores.Length > 0) Stores = FormatTagsList(product.stores, "en");

            if (product.nutritionalInfo != null)
            {
                MacroCards = BuildMacroCardsFromNutritionalInfo(product.nutritionalInfo);
            }

            if (!string.IsNullOrEmpty(product.rawNutriments))
            {
                var deepNutrition = BuildProductNutritionDetailFromOFF(product.rawNutriments);
                if (deepNutrition != null && deepNutrition.Count > 0)
                    NutritionDetail = deepNutrition;
                else if (product.nutritionalInfo != null)
                    NutritionDetail = BuildNutritionDetailFromNutritionalInfo(product.nutritionalInfo);
            }
            else if (product.nutritionalInfo != null)
            {
                NutritionDetail = BuildNutritionDetailFromNutritionalInfo(product.nutritionalInfo);
            }
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
            NutritionGrade = !string.IsNullOrEmpty(detail.nutritionGrade) && !detail.nutritionGrade.Equals("unknown", StringComparison.OrdinalIgnoreCase) ? detail.nutritionGrade : "";
            NovaGroup = detail.novaGroup.HasValue && detail.novaGroup.Value >= 1 && detail.novaGroup.Value <= 4 ? detail.novaGroup.Value : 0;
            EcoScoreGrade = !string.IsNullOrEmpty(detail.ecoscoreGrade) && !detail.ecoscoreGrade.Equals("unknown", StringComparison.OrdinalIgnoreCase) ? detail.ecoscoreGrade : "";
            Ingredients = detail.ingredientsText ?? "";
            Allergens = detail.allergens != null ? FormatTagsList(detail.allergens, "en") : "";
            Description = detail.description ?? "";
            Traces = detail.traces != null ? FormatTagsList(detail.traces, "en") : "";
            Categories = detail.categories != null && detail.categories.Length > 0
                ? FormatTagsList(detail.categories, "en")
                : "";
            DietaryFlags = new DietaryFlags(detail.isVegan, detail.isVegetarian, detail.isPalmOilFree);

            TrafficLights = BuildTrafficLights(detail.nutrientLevels?.ToString());
            MacroCards = BuildProductMacroCards(detail.nutrimentsRaw?.ToString());
            NutritionDetail = BuildProductNutritionDetail(detail.nutrimentsRaw?.ToString());
            MetaRows = BuildProductMetaRows(detail);

            if (!string.IsNullOrEmpty(detail.barcode))
            {
                await TryEnrichFromOpenFoodFactsAsync(detail.barcode);
                MetaRows = BuildProductMetaRows(detail);
                if (!string.IsNullOrEmpty(Stores))
                {
                    var rows = new List<MetaRow>(MetaRows ?? new List<MetaRow>());
                    rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_STORES"), Stores));
                    MetaRows = rows;
                }
                if (!string.IsNullOrEmpty(Traces))
                {
                    var rows = MetaRows ?? new List<MetaRow>();
                    bool hasTracesRow = false;
                    for (int i = 0; i < rows.Count; i++)
                    {
                        if (rows[i].Label == LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_TRACES"))
                        {
                            hasTracesRow = true;
                            break;
                        }
                    }
                    if (!hasTracesRow)
                    {
                        var newRows = new List<MetaRow>(rows);
                        newRows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_TRACES"), Traces));
                        MetaRows = newRows;
                    }
                }
                if (!string.IsNullOrEmpty(Categories) && Categories != string.Join(", ", detail.categories ?? new string[0]))
                {
                    var rows = MetaRows ?? new List<MetaRow>();
                    var newRows = new List<MetaRow>();
                    foreach (var row in rows)
                    {
                        if (row.Label == LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_CATEGORIES"))
                            newRows.Add(new MetaRow(row.Label, Categories));
                        else
                            newRows.Add(row);
                    }
                    MetaRows = newRows;
                }
            }
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

            PopulateFromGenericFoodDetail(detail);
        }

        private void PopulateFromGenericFoodDetail(GenericFoodDetail detail)
        {
            if (detail == null) return;

            FoodName = detail.foodName ?? "";
            FoodSubtitle = detail.foodGroup ?? "";
            Emoji = GetEmojiForFoodGroup(detail.foodGroup);
            NutritionGrade = "unknown";
            NovaGroup = 0;
            EcoScoreGrade = "unknown";
            Ingredients = "";
            Allergens = detail.containsTracesOf ?? "";
            Description = "";
            Traces = "";
            Categories = "";
            Stores = "";
            DietaryFlags = new DietaryFlags(null, null, null);

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
                ImageUrl = !string.IsNullOrEmpty(product.imageFrontUrl) ? product.imageFrontUrl : (product.imageUrl ?? "");
                NutritionGrade = !string.IsNullOrEmpty(product.nutritionGrade) ? product.nutritionGrade : "unknown";
                NovaGroup = product.novaGroup ?? 0;
                EcoScoreGrade = !string.IsNullOrEmpty(product.ecoscoreGrade) ? product.ecoscoreGrade : "unknown";
                Ingredients = product.ingredients ?? "";
                Allergens = product.allergens != null ? FormatTagsList(product.allergens, "en") : "";
                Description = product.genericName ?? "";
                Traces = product.traces != null ? FormatTagsList(product.traces, "en") : "";
                Categories = product.categories != null && product.categories.Length > 0 ? FormatTagsList(product.categories, "en") : "";
                Stores = product.stores != null && product.stores.Length > 0 ? FormatTagsList(product.stores, "en") : "";
                DietaryFlags = new DietaryFlags(null, null, null);

                MacroCards = BuildMacroCardsFromNutritionalInfo(product.nutritionalInfo);
                NutritionDetail = BuildNutritionDetailFromNutritionalInfo(product.nutritionalInfo);
                if (!string.IsNullOrEmpty(product.rawNutriments))
                {
                    var deepNutrition = BuildProductNutritionDetailFromOFF(product.rawNutriments);
                    if (deepNutrition != null && deepNutrition.Count > 0)
                        NutritionDetail = deepNutrition;
                }
                MetaRows = new List<MetaRow>();

                if (!string.IsNullOrEmpty(product.quantity))
                    MetaRows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_QUANTITY"), product.quantity));
                if (!string.IsNullOrEmpty(product.servingSize))
                    MetaRows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_SERVING_SIZE"), product.servingSize));
                if (product.brands != null && product.brands.Length > 0)
                    MetaRows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_BRANDS"), string.Join(", ", product.brands)));
                if (!string.IsNullOrEmpty(product.origins))
                    MetaRows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_ORIGINS"), product.origins));
                if (!string.IsNullOrEmpty(product.manufacturingPlaces))
                    MetaRows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_MANUFACTURING"), product.manufacturingPlaces));
                if (!string.IsNullOrEmpty(Categories))
                    MetaRows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_CATEGORIES"), Categories));
                if (product.countries != null && product.countries.Length > 0)
                    MetaRows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_COUNTRIES"), FormatTagsList(product.countries, "en")));
                if (product.labels != null && product.labels.Length > 0)
                    MetaRows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_LABELS"), FormatTagsList(product.labels, "en")));
                if (!string.IsNullOrEmpty(Traces))
                    MetaRows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_TRACES"), Traces));
                if (!string.IsNullOrEmpty(product.barcode))
                {
                    MetaRows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_BARCODE"), product.barcode));
                    _ = TryEnrichFromOpenFoodFactsAsync(product.barcode);
                }
                if (product.carbonFootprint.HasValue && product.carbonFootprint.Value > 0)
                    MetaRows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_CARBON_FOOTPRINT"), $"{product.carbonFootprint.Value:F1} kg CO\u2082e/kg"));
                if (!string.IsNullOrEmpty(Stores))
                    MetaRows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_STORES"), Stores));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadProductFromData parse error: {ex.Message}");
            }
        }

        private async Task LoadGenericFromData(string foodData)
        {
            try
            {
                var genericDetail = JsonConvert.DeserializeObject<GenericFoodDetail>(foodData);
                if (genericDetail != null && (!string.IsNullOrEmpty(genericDetail.synonym) || genericDetail.energyKcal.HasValue || !string.IsNullOrEmpty(genericDetail.containsTracesOf) || !string.IsNullOrEmpty(genericDetail.isFortifiedWith)))
                {
                    PopulateFromGenericFoodDetail(genericDetail);
                    return;
                }

                var genericFood = JsonConvert.DeserializeObject<GenericFood>(foodData);
                if (genericFood == null)
                    return;

                FoodName = genericFood.foodName ?? "";
                FoodSubtitle = genericFood.foodGroup ?? "";
                Emoji = GetEmojiForFoodGroup(genericFood.foodGroup);
                NutritionGrade = "unknown";
                NovaGroup = 0;
                EcoScoreGrade = "unknown";
                Ingredients = "";
                Allergens = "";

                MacroCards = new List<NutritionRow>
                {
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_ENERGY_KCAL"), null, "kcal"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_PROTEINS"), null, "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_FAT"), null, "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_CARBOHYDRATES"), null, "g")
                };
                NutritionDetail = new List<NutritionGroup>();
                MetaRows = new List<MetaRow>();

                if (!string.IsNullOrEmpty(genericFood.id) && Guid.TryParse(genericFood.id, out _))
                {
                    await LoadGenericAsync(genericFood.id);
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
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_ENERGY_KCAL"), info.energyKcal > 0 ? info.energyKcal : (float?)null, "kcal"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_PROTEINS"), info.proteins > 0 ? info.proteins : (float?)null, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_FAT"), info.fat > 0 ? info.fat : (float?)null, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_CARBOHYDRATES"), info.carbohydrates > 0 ? info.carbohydrates : (float?)null, "g")
            };
        }

        private List<NutritionGroup> BuildNutritionDetailFromNutritionalInfo(NutritionalInfo info)
        {
            if (info == null)
                return new List<NutritionGroup>();

            var rows = new List<NutritionRow>
            {
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_ENERGY_KJ"), info.energyKj > 0 ? info.energyKj : (float?)null, "kJ"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_SATURATED_FAT"), info.saturatedFat > 0 ? info.saturatedFat : (float?)null, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_SUGARS"), info.sugars > 0 ? info.sugars : (float?)null, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_SALT"), info.salt > 0 ? info.salt : (float?)null, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_SODIUM"), info.sodium > 0 ? info.sodium : (float?)null, "mg")
            };

            var filtered = rows.Where(r => r.Value.HasValue && r.Value.Value > 0).ToList();
            if (filtered.Count == 0)
                return new List<NutritionGroup>();

            return new List<NutritionGroup>
            {
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_GROUP_MACROS"), filtered)
            };
        }

        private void SetActionButton(string context)
        {
            switch (context)
            {
                case "pantry":
                    ActionButtonText = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ADD_TO_PANTRY");
                    ShowActionButton = true;
                    break;
                case "shoppingList":
                    ActionButtonText = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ADD_TO_SHOPPING_LIST");
                    ShowActionButton = true;
                    break;
                case "mealLog":
                    ActionButtonText = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "ADD_TO_MEAL_LOG");
                    ShowActionButton = true;
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
            {
                return new List<TrafficLight>();
            }

            try
            {
                JObject obj = JObject.Parse(nutrientLevelsJson);
                return new List<TrafficLight>
                {
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","TL_FAT"), obj["fat"]?.ToString()),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","TL_SATURATED_FAT"), obj["saturated-fat"]?.ToString()),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","TL_SUGARS"), obj["sugars"]?.ToString()),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","TL_SALT"), obj["salt"]?.ToString())
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
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_ENERGY_KCAL"), GetFloat(n, "energy-kcal_100g") ?? GetFloat(n, "energy-kcal"), "kcal"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_PROTEINS"), GetFloat(n, "proteins_100g") ?? GetFloat(n, "proteins"), "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_FAT"), GetFloat(n, "fat_100g") ?? GetFloat(n, "fat"), "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_CARBOHYDRATES"), GetFloat(n, "carbohydrates_100g") ?? GetFloat(n, "carbohydrates"), "g")
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
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_ENERGY_KJ"), GetFloat(n, "energy-kj_100g") ?? GetFloat(n, "energy-kj"), "kJ"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_SATURATED_FAT"), GetFloat(n, "saturated-fat_100g") ?? GetFloat(n, "saturated-fat"), "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_SUGARS"), GetFloat(n, "sugars_100g") ?? GetFloat(n, "sugars"), "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_FIBER"), GetFloat(n, "fiber_100g") ?? GetFloat(n, "fiber"), "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_SALT"), GetFloat(n, "salt_100g") ?? GetFloat(n, "salt"), "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_SODIUM"), GetFloat(n, "sodium_100g") ?? GetFloat(n, "sodium"), "mg")
                };

                return new List<NutritionGroup>
            {
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_GROUP_MACROS"), rows.Where(r => r.Value.HasValue && r.Value.Value > 0).ToList())
            };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] BuildProductNutritionDetail parse error: {ex.Message}");
                return new List<NutritionGroup>();
            }
        }

        private List<NutritionGroup> BuildProductNutritionDetailFromOFF(string rawNutrimentsJson)
        {
            if (string.IsNullOrEmpty(rawNutrimentsJson))
                return new List<NutritionGroup>();

            try
            {
                JObject n = JObject.Parse(rawNutrimentsJson);

                var macros = new List<NutritionRow>
                {
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_ENERGY_KCAL"), GetFloat(n, "energy-kcal_100g") ?? GetFloat(n, "energy-kcal"), "kcal"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_ENERGY_KJ"), GetFloat(n, "energy-kj_100g") ?? GetFloat(n, "energy-kj"), "kJ"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_WATER"), GetFloat(n, "water_100g") ?? GetFloat(n, "water"), "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_PROTEINS"), GetFloat(n, "proteins_100g") ?? GetFloat(n, "proteins"), "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_CARBOHYDRATES"), GetFloat(n, "carbohydrates_100g") ?? GetFloat(n, "carbohydrates"), "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_SUGARS"), GetFloat(n, "sugars_100g") ?? GetFloat(n, "sugars"), "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_FIBER"), GetFloat(n, "fiber_100g") ?? GetFloat(n, "fiber"), "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_FAT"), GetFloat(n, "fat_100g") ?? GetFloat(n, "fat"), "g")
                };

                var fats = new List<NutritionRow>
                {
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_SATURATED_FAT"), GetFloat(n, "saturated-fat_100g") ?? GetFloat(n, "saturated-fat"), "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_MONO_UNSATURATED_FAT"), GetFloat(n, "monounsaturated-fat_100g") ?? GetFloat(n, "monounsaturated-fat"), "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_POLY_UNSATURATED_FAT"), GetFloat(n, "polyunsaturated-fat_100g") ?? GetFloat(n, "polyunsaturated-fat"), "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_OMEGA3_FAT"), GetFloat(n, "omega-3-fat_100g") ?? GetFloat(n, "omega-3-fat"), "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_OMEGA6_FAT"), GetFloat(n, "omega-6-fat_100g") ?? GetFloat(n, "omega-6-fat"), "g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_TRANS_FAT"), GetFloat(n, "trans-fat_100g") ?? GetFloat(n, "trans-fat"), "g")
                };

                var vitamins = new List<NutritionRow>
                {
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_VITAMIN_A"), GetFloat(n, "vitamin-a_100g") ?? GetFloat(n, "vitamin-a"), "\u00b5g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_VITAMIN_D"), GetFloat(n, "vitamin-d_100g") ?? GetFloat(n, "vitamin-d"), "\u00b5g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_VITAMIN_E"), GetFloat(n, "vitamin-e_100g") ?? GetFloat(n, "vitamin-e"), "mg"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_VITAMIN_K"), GetFloat(n, "vitamin-k_100g") ?? GetFloat(n, "vitamin-k"), "\u00b5g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_VITAMIN_C"), GetFloat(n, "vitamin-c_100g") ?? GetFloat(n, "vitamin-c"), "mg"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_THIAMIN"), GetFloat(n, "vitamin-b1_100g") ?? GetFloat(n, "vitamin-b1"), "mg"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_RIBOFLAVIN"), GetFloat(n, "vitamin-b2_100g") ?? GetFloat(n, "vitamin-b2"), "mg"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_VITAMIN_B6"), GetFloat(n, "vitamin-b6_100g") ?? GetFloat(n, "vitamin-b6"), "mg"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_VITAMIN_B12"), GetFloat(n, "vitamin-b12_100g") ?? GetFloat(n, "vitamin-b12"), "\u00b5g"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_FOLATE_TOTAL"), GetFloat(n, "vitamin-b9_100g") ?? GetFloat(n, "vitamin-b9"), "\u00b5g")
                };

                var minerals = new List<NutritionRow>
                {
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_SODIUM"), GetFloat(n, "sodium_100g") ?? GetFloat(n, "sodium"), "mg"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_POTASSIUM"), GetFloat(n, "potassium_100g") ?? GetFloat(n, "potassium"), "mg"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_CALCIUM"), GetFloat(n, "calcium_100g") ?? GetFloat(n, "calcium"), "mg"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_PHOSPHORUS"), GetFloat(n, "phosphorus_100g") ?? GetFloat(n, "phosphorus"), "mg"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_MAGNESIUM"), GetFloat(n, "magnesium_100g") ?? GetFloat(n, "magnesium"), "mg"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_IRON"), GetFloat(n, "iron_100g") ?? GetFloat(n, "iron"), "mg"),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_ZINC"), GetFloat(n, "zinc_100g") ?? GetFloat(n, "zinc"), "mg")
                };

                return new List<NutritionGroup>
                {
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_GROUP_MACROS"), macros.Where(r => r.Value.HasValue && r.Value.Value > 0).ToList()),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_GROUP_FATS"), fats.Where(r => r.Value.HasValue && r.Value.Value > 0).ToList()),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_GROUP_VITAMINS"), vitamins.Where(r => r.Value.HasValue && r.Value.Value > 0).ToList()),
                    new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_GROUP_MINERALS"), minerals.Where(r => r.Value.HasValue && r.Value.Value > 0).ToList())
                };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{GetType().Name}] BuildProductNutritionDetailFromOFF parse error: {ex.Message}");
                return new List<NutritionGroup>();
            }
        }

        private List<NutritionRow> BuildGenericMacroCards(GenericFoodDetail d)
        {
            return new List<NutritionRow>
            {
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_ENERGY_KCAL"), d.energyKcal, "kcal"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_PROTEINS"), d.proteins, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_FAT"), d.fat, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_CARBOHYDRATES"), d.carbohydrates, "g")
            };
        }

        private List<NutritionGroup> BuildGenericNutritionDetail(GenericFoodDetail d)
        {
            var macros = new List<NutritionRow>
            {
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_ENERGY_KCAL"), d.energyKcal, "kcal"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_ENERGY_KJ"), d.energyKj, "kJ"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_WATER"), d.water, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_PROTEINS"), d.proteins, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_PROTEINS_PLANT"), d.proteinsPlant, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_PROTEINS_ANIMAL"), d.proteinsAnimal, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_CARBOHYDRATES"), d.carbohydrates, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_SUGARS"), d.sugars, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_ADDED_SUGARS"), d.addedSugars, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_STARCH"), d.starch, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_FIBER"), d.fiber, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_FAT"), d.fat, "g")
            };

            var fats = new List<NutritionRow>
            {
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_SATURATED_FAT"), d.saturatedFat, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_MONO_UNSATURATED_FAT"), d.monoUnsaturatedFat, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_POLY_UNSATURATED_FAT"), d.polyUnsaturatedFat, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_OMEGA3_FAT"), d.omega3Fat, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_OMEGA6_FAT"), d.omega6Fat, "g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_TRANS_FAT"), d.transFat, "g")
            };

            var vitamins = new List<NutritionRow>
            {
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_VITAMIN_A"), d.vitaminARae, "\u00b5g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_VITAMIN_D"), d.vitaminD, "\u00b5g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_VITAMIN_E"), d.vitaminE, "mg"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_VITAMIN_K"), d.vitaminK, "\u00b5g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_VITAMIN_C"), d.vitaminC, "mg"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_THIAMIN"), d.thiamin, "mg"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_RIBOFLAVIN"), d.riboflavin, "mg"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_VITAMIN_B6"), d.vitaminB6, "mg"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_VITAMIN_B12"), d.vitaminB12, "\u00b5g"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_FOLATE_TOTAL"), d.folateTotal, "\u00b5g")
            };

            var minerals = new List<NutritionRow>
            {
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_SODIUM"), d.sodium, "mg"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_POTASSIUM"), d.potassium, "mg"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_CALCIUM"), d.calcium, "mg"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_PHOSPHORUS"), d.phosphorus, "mg"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_MAGNESIUM"), d.magnesium, "mg"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_IRON"), d.iron, "mg"),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_ZINC"), d.zinc, "mg")
            };


            return new List<NutritionGroup>
            {
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_GROUP_MACROS"), macros.Where(r => r.Value.HasValue && r.Value.Value > 0).ToList()),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_GROUP_FATS"), fats.Where(r => r.Value.HasValue && r.Value.Value > 0).ToList()),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_GROUP_VITAMINS"), vitamins.Where(r => r.Value.HasValue && r.Value.Value > 0).ToList()),
                new(LocalizationSettings.StringDatabase.GetLocalizedString("UI","NUTR_GROUP_MINERALS"), minerals.Where(r => r.Value.HasValue && r.Value.Value > 0).ToList())
            };
        }

        private List<MetaRow> BuildProductMetaRows(FoodProductDetail d)
        {
            var rows = new List<MetaRow>();

            if (!string.IsNullOrEmpty(d.quantity))
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_QUANTITY"), d.quantity));
            }

            if (!string.IsNullOrEmpty(d.servingSize))
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_SERVING_SIZE"), d.servingSize));
            }

            if (!string.IsNullOrEmpty(d.brands))
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_BRANDS"), d.brands));
            }

            if (!string.IsNullOrEmpty(d.origins))
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_ORIGINS"), d.origins));
            }

            if (!string.IsNullOrEmpty(d.manufacturingPlaces))
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_MANUFACTURING"), d.manufacturingPlaces));
            }

            if (d.categories != null && d.categories.Length > 0)
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_CATEGORIES"), FormatTagsList(d.categories, "en")));
            }

            if (d.countries != null && d.countries.Length > 0)
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_COUNTRIES"), FormatTagsList(d.countries, "en")));
            }

            if (d.labels != null && d.labels.Length > 0)
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_LABELS"), FormatTagsList(d.labels, "en")));
            }

            if (d.traces != null && d.traces.Length > 0)
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_TRACES"), FormatTagsList(d.traces, "en")));
            }

            if (!string.IsNullOrEmpty(d.barcode))
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_BARCODE"), d.barcode));
            }

            if (d.carbonFootprint.HasValue && d.carbonFootprint.Value > 0)
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_CARBON_FOOTPRINT"), $"{d.carbonFootprint.Value:F1} kg CO\u2082e/kg"));
            }

            if (d.isVegan.HasValue)
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_VEGAN"),
                    d.isVegan.Value ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TXT_YES") : LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TXT_NO")));
            }

            if (d.isVegetarian.HasValue)
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_VEGETARIAN"),
                    d.isVegetarian.Value ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TXT_YES") : LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TXT_NO")));
            }

            if (d.isPalmOilFree.HasValue)
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_PALM_OIL_FREE"),
                    d.isPalmOilFree.Value ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TXT_YES") : LocalizationSettings.StringDatabase.GetLocalizedString("UI", "TXT_NO")));
            }

            return rows;
        }

        private List<MetaRow> BuildGenericMetaRows(GenericFoodDetail d)
        {
            var rows = new List<MetaRow>();

            if (!string.IsNullOrEmpty(d.foodGroup))
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_FOOD_GROUP"), d.foodGroup));
            }

            if (!string.IsNullOrEmpty(d.quantity))
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_QUANTITY"), d.quantity));
            }

            if (!string.IsNullOrEmpty(d.synonym))
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_SYNONYMS"), d.synonym));
            }

            if (!string.IsNullOrEmpty(d.isFortifiedWith))
            {
                rows.Add(new MetaRow(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "META_FORTIFIED_WITH"), d.isFortifiedWith));
            }

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

        public static string FormatTagsList(IEnumerable<string> rawTags, string preferredLang = "en")
        {
            if (rawTags == null) return "";

            var tagList = rawTags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .SelectMany(t => t.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(t => t.Trim())
                .ToList();

            if (tagList.Count == 0) return "";

            string prefPrefix = preferredLang.ToLowerInvariant() + ":";
            var prefTags = tagList.Where(t => t.StartsWith(prefPrefix, StringComparison.OrdinalIgnoreCase)).ToList();

            List<string> selectedTags;
            if (prefTags.Count > 0)
            {
                selectedTags = prefTags;
            }
            else
            {
                var enTags = tagList.Where(t => t.StartsWith("en:", StringComparison.OrdinalIgnoreCase)).ToList();
                selectedTags = enTags.Count > 0 ? enTags : tagList;
            }

            var result = new List<string>();
            foreach (var tag in selectedTags)
            {
                string val = tag;
                int colonIdx = val.IndexOf(':');
                if (colonIdx >= 0 && colonIdx < val.Length - 1)
                {
                    val = val.Substring(colonIdx + 1).Trim();
                }
                if (!string.IsNullOrEmpty(val))
                {
                    string formatted = char.ToUpper(val[0]) + (val.Length > 1 ? val.Substring(1) : "");
                    if (!result.Contains(formatted, StringComparer.OrdinalIgnoreCase))
                    {
                        result.Add(formatted);
                    }
                }
            }

            return string.Join(", ", result);
        }
    }
}
