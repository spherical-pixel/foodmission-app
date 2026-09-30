using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.AppUI.MVVM;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace eu.foodmission.platform
{
    public enum ComparisonState
    {
        SelectionOrEmpty,
        NutriDuel,
        FullComparison
    }

    [Serializable]
    public class FoodComparisonItem
    {
        public string Id { get; set; }
        public string FoodProductId { get; set; }
        public bool IsCommercialProduct => !string.IsNullOrEmpty(FoodProductId);
        public string Name { get; set; }
        public string Category { get; set; }
        public string ImageUrl { get; set; }
        public string Emoji { get; set; }
        public float FootprintKg { get; set; }
        public float CompositeScore { get; set; }
        public FootprintConfidence Confidence { get; set; }
        public string ScientificRef { get; set; }
        // Only backend data; null = not available (the matrix shows "?" or hides the row)
        public string NutriScore { get; set; }
        public string EcoScore { get; set; }
        public int? NovaGroup { get; set; }
        public float? ProteinsGrams { get; set; }
        public float? EnergyKcal { get; set; }
        public float? SaturatedFatGrams { get; set; }

        public string FootprintDisclaimer
        {
            get
            {
                return LocalizationSettings.StringDatabase.GetLocalizedString("UI",
                    Confidence == FootprintConfidence.Exact ? "FC_DISCLAIMER_DIRECT" : "FC_DISCLAIMER_ESTIMATE");
            }
        }
    }

    [ObservableObject]
    public partial class FoodComparisonViewModel : ViewModelBase
    {
        private readonly IShoppingListService _shoppingListService;
        private readonly IFoodFootprintCalculator _footprintCalculator;
        private readonly IChallengeService _challengeService;
        private readonly IAuthService _authService;
        private readonly IChallengeCompletionService _completion;
        private readonly IFoodProductService _foodProductService;
        private readonly IGenericFoodService _genericFoodService;

        // Backend throttler allows 5 requests/s and 100/min per client
        private const int MinRequestSpacingMs = 250;
        private const int ThrottleBackoffMs = 1000;
        private const int MaxThrottleRetries = 2;

        // Session caches shared across transient VMs:
        // barcode → OFF data returned by the backend (categories empty = OFF has none)
        private static readonly Dictionary<string, OffSnapshot> s_OffSnapshotsByBarcode = new();
        // generic food / product ids already fetched once; later calls are served by the service cache, so they skip pacing
        private static readonly HashSet<string> s_FetchedGenericFoodIds = new();
        private static readonly HashSet<string> s_FetchedFoodProductIds = new();
        private static DateTime s_LastRequestUtc = DateTime.MinValue;


        [ObservableProperty]
        private ApiErrorResponse _errorDetail;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _loadingText;

        [ObservableProperty]
        private ComparisonState _currentState = ComparisonState.SelectionOrEmpty;

        [ObservableProperty]
        private bool _hasInsufficientProteins;

        [ObservableProperty]
        private string _challengeCode = "";

        [ObservableProperty]
        private string _mode = "proteins";

        [ObservableProperty]
        private string _defaultShoppingListId = "";

        [ObservableProperty]
        private string _defaultShoppingListTitle = "";

        [ObservableProperty]
        private List<FoodComparisonItem> _availableItems = new();

        [ObservableProperty]
        private FoodComparisonItem _itemA;

        [ObservableProperty]
        private FoodComparisonItem _itemB;

        [ObservableProperty]
        private int _selectedItemIndex = -1; // 0 for ItemA, 1 for ItemB

        [ObservableProperty]
        private int _winnerIndex = -1;       // 0 for ItemA, 1 for ItemB

        [ObservableProperty]
        private bool _isGuessRevealed;

        [ObservableProperty]
        private bool _isGuessCorrect;

        [ObservableProperty]
        private string _didacticFeedback = "";

        [ObservableProperty]
        private ContentReward _earnedReward;

        private bool _isChallengeCompleted;

        public Task ImageFetchTask { get; private set; } = Task.CompletedTask;

        public FoodComparisonViewModel(
            IStoreService storeService,
            IShoppingListService shoppingListService,
            IFoodFootprintCalculator footprintCalculator,
            IChallengeService challengeService = null,
            IFoodProductService foodProductService = null,
            IGenericFoodService genericFoodService = null,
            IAuthService authService = null,
            IChallengeCompletionService completionService = null) : base(storeService)
        {
            _shoppingListService = shoppingListService;
            _footprintCalculator = footprintCalculator;
            _challengeService = challengeService ?? App.current?.services?.GetService<IChallengeService>();
            _foodProductService = foodProductService;
            _genericFoodService = genericFoodService;
            _authService = authService ?? App.current?.services?.GetService<IAuthService>();
            // An injected IChallengeService (tests) gets its own completion service; otherwise share the app singleton
            _completion = completionService
                ?? (challengeService == null ? App.current?.services?.GetService<IChallengeCompletionService>() : null)
                ?? new ChallengeCompletionService(_challengeService, _authService);
        }

        public async Task LoadComparisonDataAsync(string challengeCode, string mode, string source)
        {
            ChallengeCode = challengeCode ?? "";
            Mode = mode ?? "proteins";
            LoadingText = LocalizationSettings.StringDatabase.GetLocalizedString("UI", "FC_LOADING_DUEL");
            IsLoading = true;
            HasInsufficientProteins = false;
            IsGuessRevealed = false;
            WinnerIndex = -1;
            SelectedItemIndex = -1;
            EarnedReward = null;
            _isChallengeCompleted = false;

            if (mode == "sample" || source == "sample")
            {
                LoadSampleDuel();
                CurrentState = ComparisonState.NutriDuel;
                IsLoading = false;
                return;
            }

            if (mode == "matrix" || source == "matrix")
            {
                LoadSampleDuel();
                WinnerIndex = ItemA != null && ItemB != null && ItemA.FootprintKg <= ItemB.FootprintKg ? 0 : 1;
                IsGuessRevealed = true;
                CurrentState = ComparisonState.FullComparison;
                IsLoading = false;
                return;
            }

            if (mode == "empty" || source == "empty")
            {
                HasInsufficientProteins = true;
                CurrentState = ComparisonState.SelectionOrEmpty;
                IsLoading = false;
                return;
            }

            try
            {
                var (lists, _) = await _shoppingListService.GetListsAsync();
                var state = _storeService?.GetAppState();
                string lastListId = state?.userLastShoppingListId;

                ShoppingList targetList = null;
                if (!string.IsNullOrEmpty(lastListId) && lists != null)
                {
                    targetList = System.Array.Find(lists, l => l.id == lastListId);
                }
                if (targetList == null && lists != null && lists.Length > 0)
                {
                    targetList = lists[0];
                }

                if (targetList == null)
                {
                    HasInsufficientProteins = true;
                    CurrentState = ComparisonState.SelectionOrEmpty;
                    return;
                }

                DefaultShoppingListId = targetList.id;
                DefaultShoppingListTitle = string.IsNullOrEmpty(targetList.title)
                    ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "SHOPPING_LIST")
                    : targetList.title;

                var (items, _) = await _shoppingListService.GetItemsAsync(targetList.id);
                var proteinItems = new List<FoodComparisonItem>();

                if (items != null)
                {
                    await ProcessShoppingItemsAsync(items, proteinItems);
                }

                // If fewer than 2 proteins in the primary list, also check other lists as a fallback
                if (proteinItems.Count < 2 && lists != null && lists.Length > 1)
                {
                    foreach (var otherList in lists)
                    {
                        if (otherList.id == targetList.id) continue;
                        var (otherItems, _) = await _shoppingListService.GetItemsAsync(otherList.id);
                        if (otherItems != null)
                        {
                            await ProcessShoppingItemsAsync(otherItems, proteinItems);
                        }
                        if (proteinItems.Count >= 2) break;
                    }
                }

                AvailableItems = proteinItems;

                if (proteinItems.Count < 2)
                {
                    HasInsufficientProteins = true;
                    CurrentState = ComparisonState.SelectionOrEmpty;
                }
                else
                {
                    HasInsufficientProteins = false;
                    SelectRandomPair();

                    CurrentState = string.IsNullOrEmpty(ChallengeCode)
                        ? ComparisonState.FullComparison
                        : ComparisonState.NutriDuel;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{GetType().Name}] LoadComparisonDataAsync error: {ex.Message}");
                HasInsufficientProteins = true;
                CurrentState = ComparisonState.SelectionOrEmpty;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task SubmitGuessAsync(int selectedItemIndex)
        {
            if (IsGuessRevealed || ItemA == null || ItemB == null) return;

            // Everything the reveal shows must be set before IsGuessRevealed, which triggers the redraw
            SelectedItemIndex = selectedItemIndex;
            WinnerIndex = ItemA.FootprintKg <= ItemB.FootprintKg ? 0 : 1;
            IsGuessCorrect = (SelectedItemIndex == WinnerIndex);

            var winner = WinnerIndex == 0 ? ItemA : ItemB;
            var other = WinnerIndex == 0 ? ItemB : ItemA;
            float ratio = other.FootprintKg / Math.Max(0.1f, winner.FootprintKg);

            DidacticFeedback = ratio > 1.5f
                ? LocalizationSettings.StringDatabase.GetLocalizedString("UI", "FC_FEEDBACK_RATIO",
                    new object[] { winner.Name, ratio, other.Name, winner.FootprintKg, other.FootprintKg })
                : LocalizationSettings.StringDatabase.GetLocalizedString("UI", "FC_FEEDBACK_SLIGHT",
                    new object[] { winner.Name, other.Name, winner.FootprintKg, other.FootprintKg });

            IsGuessRevealed = true;

            if (!string.IsNullOrEmpty(ChallengeCode))
            {
                await CompleteChallengeAsync();
            }
        }

        public bool IsChallengeCompleted => _isChallengeCompleted;

        // Completes the challenge once through the shared service (single in-flight request, success-only).
        public async Task<bool> CompleteChallengeAsync()
        {
            if (string.IsNullOrEmpty(ChallengeCode) || _isChallengeCompleted || _completion == null)
            {
                return _isChallengeCompleted;
            }

            ErrorDetail = null;
            ChallengeCompletionResult result = await _completion.CompleteAsync(ChallengeCode);
            if (!result.Success)
            {
                ErrorDetail = result.Error ?? new ApiErrorResponse();
                return false;
            }

            _isChallengeCompleted = true;
            if (result.Reward != null)
            {
                EarnedReward = result.Reward;
            }
            return true;
        }

        private sealed class OffSnapshot
        {
            public string[] Categories;
            public string NutriScoreGrade;
            public int? NovaGroup;
        }

        // Returns the OFF data the backend has for a barcode, or null on a transient failure.
        // Results are cached for the session.
        private async Task<OffSnapshot> GetOpenFoodFactsSnapshotAsync(string barcode)
        {
            if (s_OffSnapshotsByBarcode.TryGetValue(barcode, out OffSnapshot cached))
            {
                return cached;
            }

            var (offProduct, error) = await CallPacedAsync(() => _foodProductService.FindByBarcodeAsync(barcode, includeOpenFoodFacts: true));
            if (error != null && error.statusCode != 404)
            {
                return null;
            }

            var info = offProduct?.openFoodFactsInfo;
            var snapshot = new OffSnapshot
            {
                Categories = offProduct?.categories != null && offProduct.categories.Length > 0
                    ? offProduct.categories
                    : info?.categories ?? Array.Empty<string>(),
                NutriScoreGrade = NormalizeGrade(offProduct?.nutriscoreGrade) ?? NormalizeGrade(info?.nutritionGrade),
                NovaGroup = NormalizeNova(offProduct?.novaGroup) ?? NormalizeNova(info?.novaGroup)
            };
            s_OffSnapshotsByBarcode[barcode] = snapshot;
            return snapshot;
        }

        // Official grades only (a-e); OFF also returns "unknown" / "not-applicable"
        private static string NormalizeGrade(string grade)
        {
            if (string.IsNullOrWhiteSpace(grade))
            {
                return null;
            }

            string g = grade.Trim().ToUpperInvariant();
            return g.Length == 1 && g[0] >= 'A' && g[0] <= 'E' ? g : null;
        }

        private static int? NormalizeNova(int? nova)
        {
            return nova.HasValue && nova.Value >= 1 && nova.Value <= 4 ? nova : null;
        }

        private async Task<GenericFood> FetchGenericFoodAsync(string id)
        {
            var (food, error) = s_FetchedGenericFoodIds.Contains(id)
                ? await _genericFoodService.GetGenericFoodByIdAsync(id)
                : await CallPacedAsync(() => _genericFoodService.GetGenericFoodByIdAsync(id));
            if (food != null && error == null)
            {
                s_FetchedGenericFoodIds.Add(id);
            }
            return food;
        }

        private async Task<FoodProduct> FetchFoodProductAsync(string id)
        {
            var (product, error) = s_FetchedFoodProductIds.Contains(id)
                ? await _foodProductService.GetFoodByIdAsync(id)
                : await CallPacedAsync(() => _foodProductService.GetFoodByIdAsync(id));
            if (product != null && error == null)
            {
                s_FetchedFoodProductIds.Add(id);
            }
            return product;
        }

        // Keeps at least MinRequestSpacingMs between network requests and retries throttled (429) responses with backoff.
        private static async Task<(T Result, ApiErrorResponse Error)> CallPacedAsync<T>(Func<Task<(T Result, ApiErrorResponse Error)>> request)
        {
            for (int attempt = 0; ; attempt++)
            {
                double waitMs = MinRequestSpacingMs - (DateTime.UtcNow - s_LastRequestUtc).TotalMilliseconds;
                if (waitMs > 0)
                {
                    await Task.Delay((int)Math.Ceiling(waitMs));
                }
                s_LastRequestUtc = DateTime.UtcNow;

                var response = await request();
                if (IsThrottled(response.Error) && attempt < MaxThrottleRetries)
                {
                    await Task.Delay(ThrottleBackoffMs * (attempt + 1));
                    continue;
                }
                return response;
            }
        }

        private static bool IsThrottled(ApiErrorResponse error)
        {
            return error != null
                && (error.statusCode == 429 || string.Equals(error.error, "ThrottlerException", StringComparison.OrdinalIgnoreCase));
        }

        private async Task ProcessShoppingItemsAsync(ShoppingListItem[] items, List<FoodComparisonItem> proteinItems)
        {
            foreach (var i in items)
            {
                // Enrich generic food if null, foodName is missing, not yet recognized as protein, or meat without
                // LanguaL species codes (needed for beef vs. poultry footprint), but an ID exists
                string gfId = !string.IsNullOrEmpty(i.genericFoodId) ? i.genericFoodId : i.genericFood?.id;
                if (!string.IsNullOrEmpty(gfId) && _genericFoodService != null)
                {
                    if (i.genericFood == null || string.IsNullOrEmpty(i.genericFood.foodName) || !_footprintCalculator.IsProtein(i.genericFood)
                        || (i.genericFood.langualCodes == null
                            && (i.genericFood.foodGroupSlug == FoodFootprintCalculator.SlugMeat || i.genericFood.foodGroupSlug == FoodFootprintCalculator.SlugColdMeatCuts)))
                    {
                        try
                        {
                            GenericFood gf = await FetchGenericFoodAsync(gfId);
                            if (gf != null)
                            {
                                i.genericFood = gf;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[{GetType().Name}] Failed to enrich generic food {gfId}: {ex.Message}");
                        }
                    }
                }

                // Enrich commercial food product if null or name is missing but an ID exists
                string fpId = !string.IsNullOrEmpty(i.foodProductId) ? i.foodProductId : i.foodProduct?.id;
                if ((i.foodProduct == null || string.IsNullOrEmpty(i.foodProduct.name)) && !string.IsNullOrEmpty(fpId) && _foodProductService != null)
                {
                    try
                    {
                        FoodProduct fp = await FetchFoodProductAsync(fpId);
                        if (fp != null)
                        {
                            i.foodProduct = fp;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[{GetType().Name}] Failed to enrich food product {fpId}: {ex.Message}");
                    }
                }

                // Imported products are stored without OFF categories, Nutri-Score or NOVA; fetch them by barcode
                // (includeOpenFoodFacts) and copy only those fields, since that response carries the barcode as id
                // instead of the local id. Scores are only needed for protein items.
                if (i.foodProduct != null && !string.IsNullOrEmpty(i.foodProduct.barcode) && _foodProductService != null)
                {
                    bool missingCategories = i.foodProduct.categories == null || i.foodProduct.categories.Length == 0;
                    bool missingScores = string.IsNullOrEmpty(i.foodProduct.nutriscoreGrade) || !i.foodProduct.novaGroup.HasValue;
                    if (missingCategories || (missingScores && _footprintCalculator.IsProtein(i.foodProduct)))
                    {
                        try
                        {
                            OffSnapshot off = await GetOpenFoodFactsSnapshotAsync(i.foodProduct.barcode);
                            if (off != null)
                            {
                                if (missingCategories && off.Categories.Length > 0)
                                {
                                    i.foodProduct.categories = off.Categories;
                                }
                                if (string.IsNullOrEmpty(i.foodProduct.nutriscoreGrade))
                                {
                                    i.foodProduct.nutriscoreGrade = off.NutriScoreGrade;
                                }
                                if (!i.foodProduct.novaGroup.HasValue)
                                {
                                    i.foodProduct.novaGroup = off.NovaGroup;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[{GetType().Name}] Failed to fetch OpenFoodFacts data for {i.foodProduct.barcode}: {ex.Message}");
                        }
                    }
                }

                bool isGenericProtein = i.genericFood != null && _footprintCalculator.IsProtein(i.genericFood);
                bool isProductProtein = i.foodProduct != null && _footprintCalculator.IsProtein(i.foodProduct);

                Debug.Log($"[{GetType().Name}] Item: generic='{i.genericFood?.foodName}', product='{i.foodProduct?.name}', isGenProt={isGenericProtein}, isProdProt={isProductProtein}");

                if (isGenericProtein)
                {
                    string id = i.genericFood.id ?? i.id;
                    if (proteinItems.Any(p => p.Id == id)) continue;

                    var metric = _footprintCalculator.CalculateGenericFoodFootprint(i.genericFood);

                    var (cat, emoji) = ResolveCategoryAndEmoji(i.genericFood);
                    proteinItems.Add(new FoodComparisonItem
                    {
                        Id = id,
                        Name = i.genericFood.foodName,
                        Category = cat,
                        FootprintKg = metric.CarbonFootprintKg,
                        CompositeScore = metric.CompositeImpactScore,
                        Confidence = metric.Confidence,
                        ScientificRef = metric.ScientificReference,
                        Emoji = emoji
                        // NEVO generic foods have no Eco-Score, Nutri-Score or NOVA
                    });
                }
                else if (i.foodProduct != null && _footprintCalculator.IsProtein(i.foodProduct))
                {
                    string id = i.foodProduct.id ?? i.id;
                    if (proteinItems.Any(p => p.Id == id)) continue;

                    var metric = _footprintCalculator.CalculateProductFootprint(i.foodProduct);

                    string prodId = !string.IsNullOrEmpty(i.foodProductId) ? i.foodProductId : i.foodProduct.id;
                    var (cat, emoji) = ResolveCategoryAndEmoji(i.foodProduct);
                    proteinItems.Add(new FoodComparisonItem
                    {
                        Id = id,
                        FoodProductId = prodId,
                        Name = i.foodProduct.name,
                        Category = cat,
                        ImageUrl = i.foodProduct.ResolvedImageUrl,
                        Emoji = emoji,
                        FootprintKg = metric.CarbonFootprintKg,
                        CompositeScore = metric.CompositeImpactScore,
                        Confidence = metric.Confidence,
                        ScientificRef = metric.ScientificReference,
                        NutriScore = NormalizeGrade(i.foodProduct.nutriscoreGrade) ?? NormalizeGrade(i.foodProduct.openFoodFactsInfo?.nutritionGrade),
                        EcoScore = NormalizeGrade(i.foodProduct.ecoscoreGrade),
                        NovaGroup = NormalizeNova(i.foodProduct.novaGroup) ?? NormalizeNova(i.foodProduct.openFoodFactsInfo?.novaGroup)
                    });
                }
            }
        }

        public void TransitionToMatrix()
        {
            CurrentState = ComparisonState.FullComparison;
        }

        public void SelectRandomPair()
        {
            if (AvailableItems == null || AvailableItems.Count < 2) return;

            if (AvailableItems.Count == 2)
            {
                SetPair(AvailableItems[0], AvailableItems[1]);
                return;
            }

            var rng = new System.Random();
            int idxA = 0;
            int idxB = 1;
            int attempts = 0;
            do
            {
                idxA = rng.Next(AvailableItems.Count);
                idxB = rng.Next(AvailableItems.Count - 1);
                if (idxB >= idxA) idxB++;
                attempts++;
            } while (attempts < 10 && ItemA != null && ItemB != null &&
                     ((AvailableItems[idxA].Id == ItemA.Id && AvailableItems[idxB].Id == ItemB.Id) ||
                      (AvailableItems[idxA].Id == ItemB.Id && AvailableItems[idxB].Id == ItemA.Id)));

            SetPair(AvailableItems[idxA], AvailableItems[idxB]);
        }

        // Clears the previous duel's result before assigning the items, since ItemA/ItemB changes trigger the redraw
        private void SetPair(FoodComparisonItem itemA, FoodComparisonItem itemB)
        {
            IsGuessRevealed = false;
            IsGuessCorrect = false;
            SelectedItemIndex = -1;
            DidacticFeedback = null;
            WinnerIndex = itemA.FootprintKg <= itemB.FootprintKg ? 0 : 1;

            ItemA = itemA;
            ItemB = itemB;

            ImageFetchTask = FetchMissingProductImagesAsync(ItemA, ItemB);
        }

        public void ResetComparison()
        {
            if (AvailableItems != null && AvailableItems.Count >= 2)
            {
                SelectRandomPair();
                CurrentState = string.IsNullOrEmpty(ChallengeCode)
                    ? ComparisonState.FullComparison
                    : ComparisonState.NutriDuel;
            }
            else
            {
                CurrentState = ComparisonState.SelectionOrEmpty;
                IsGuessRevealed = false;
                SelectedItemIndex = -1;
            }
        }

        public void LoadSampleDuel()
        {
            var samples = new List<FoodComparisonItem>
            {
                new FoodComparisonItem
                {
                    Id = "sample-beef",
                    Name = GetLocalizedCategory("FC_SAMPLE_BEEF_BURGER"),
                    Category = $"🥩 {GetLocalizedCategory("FC_CAT_RED_MEAT")}",
                    FootprintKg = FoodFootprintCalculator.BeefFootprintKg,
                    CompositeScore = FoodFootprintCalculator.BeefCompositeScore,
                    Confidence = FootprintConfidence.Estimated,
                    ScientificRef = "Poore & Nemecek (2018) / Clark et al. (2022)",
                    Emoji = "🥩"
                },
                new FoodComparisonItem
                {
                    Id = "sample-lentils",
                    Name = GetLocalizedCategory("FC_SAMPLE_LENTILS"),
                    Category = $"🫘 {GetLocalizedCategory("FC_CAT_LEGUMES")}",
                    FootprintKg = FoodFootprintCalculator.LegumesFootprintKg,
                    CompositeScore = FoodFootprintCalculator.LegumesCompositeScore,
                    Confidence = FootprintConfidence.Estimated,
                    ScientificRef = "Poore & Nemecek (2018) / Clark et al. (2022)",
                    Emoji = "🫘"
                },
                new FoodComparisonItem
                {
                    Id = "sample-chicken",
                    Name = GetLocalizedCategory("FC_SAMPLE_CHICKEN"),
                    Category = $"🍗 {GetLocalizedCategory("FC_CAT_WHITE_MEAT")}",
                    FootprintKg = FoodFootprintCalculator.PoultryFootprintKg,
                    CompositeScore = FoodFootprintCalculator.PoultryCompositeScore,
                    Confidence = FootprintConfidence.Estimated,
                    ScientificRef = "Poore & Nemecek (2018) / Clark et al. (2022)",
                    Emoji = "🍗"
                },
                new FoodComparisonItem
                {
                    Id = "sample-tofu",
                    Name = GetLocalizedCategory("FC_SAMPLE_TOFU"),
                    Category = $"🌱 {GetLocalizedCategory("FC_CAT_PLANT_ALT")}",
                    FootprintKg = FoodFootprintCalculator.TofuPlantAltFootprintKg,
                    CompositeScore = FoodFootprintCalculator.TofuCompositeScore,
                    Confidence = FootprintConfidence.Estimated,
                    ScientificRef = "Poore & Nemecek (2018) / Clark et al. (2022)",
                    Emoji = "🌱"
                },
                new FoodComparisonItem
                {
                    Id = "sample-salmon",
                    Name = GetLocalizedCategory("FC_SAMPLE_SALMON"),
                    Category = $"🐟 {GetLocalizedCategory("FC_CAT_FISH")}",
                    FootprintKg = FoodFootprintCalculator.FishFootprintKg,
                    CompositeScore = FoodFootprintCalculator.FishCompositeScore,
                    Confidence = FootprintConfidence.Estimated,
                    ScientificRef = "Gephart et al. (2021) / Poore & Nemecek (2018)",
                    Emoji = "🐟"
                },
                new FoodComparisonItem
                {
                    Id = "sample-eggs",
                    Name = GetLocalizedCategory("FC_SAMPLE_EGGS"),
                    Category = $"🥚 {GetLocalizedCategory("FC_CAT_EGGS")}",
                    FootprintKg = FoodFootprintCalculator.EggsFootprintKg,
                    CompositeScore = FoodFootprintCalculator.EggsCompositeScore,
                    Confidence = FootprintConfidence.Estimated,
                    ScientificRef = "Poore & Nemecek (2018) / Clark et al. (2022)",
                    Emoji = "🥚"
                }
            };

            AvailableItems = samples;
            HasInsufficientProteins = false;
            SelectRandomPair();
            CurrentState = string.IsNullOrEmpty(ChallengeCode)
                ? ComparisonState.FullComparison
                : ComparisonState.NutriDuel;
        }

        public void NavigateToShoppingList()
        {
            if (!string.IsNullOrEmpty(DefaultShoppingListId))
            {
                RaiseNavigationRequested(
                    Unity.AppUI.Navigation.Generated.Actions.shopping_list_to_detail,
                    new Unity.AppUI.Navigation.Argument("listId", DefaultShoppingListId),
                    new Unity.AppUI.Navigation.Argument("listTitle", DefaultShoppingListTitle));
            }
            else
            {
                RaiseNavigationRequested(Unity.AppUI.Navigation.Generated.Actions.go_to_shopping_list);
            }
        }

        private async Task FetchMissingProductImagesAsync(FoodComparisonItem itemA, FoodComparisonItem itemB)
        {
            if (_foodProductService == null) return;

            if (itemA != null && itemA.IsCommercialProduct && string.IsNullOrEmpty(itemA.ImageUrl) && !string.IsNullOrEmpty(itemA.FoodProductId))
            {
                try
                {
                    var (detail, _) = await _foodProductService.GetFoodProductDetailAsync(itemA.FoodProductId);
                    if (detail != null)
                    {
                        string img = detail.imageUrl ?? detail.imageFrontUrl ?? detail.openFoodFactsInfo?.imageUrl;
                        if (!string.IsNullOrEmpty(img))
                        {
                            itemA.ImageUrl = img;
                            OnPropertyChanged(nameof(ItemA));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{GetType().Name}] Could not resolve image for ItemA: {ex.Message}");
                }
            }

            if (itemB != null && itemB.IsCommercialProduct && string.IsNullOrEmpty(itemB.ImageUrl) && !string.IsNullOrEmpty(itemB.FoodProductId))
            {
                try
                {
                    var (detail, _) = await _foodProductService.GetFoodProductDetailAsync(itemB.FoodProductId);
                    if (detail != null)
                    {
                        string img = detail.imageUrl ?? detail.imageFrontUrl ?? detail.openFoodFactsInfo?.imageUrl;
                        if (!string.IsNullOrEmpty(img))
                        {
                            itemB.ImageUrl = img;
                            OnPropertyChanged(nameof(ItemB));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[{GetType().Name}] Could not resolve image for ItemB: {ex.Message}");
                }
            }
        }

        private static string GetLocalizedCategory(string locKey)
        {

            return LocalizationSettings.StringDatabase.GetLocalizedString("UI", locKey);
        }

        private static (string Category, string Emoji) ResolveCategoryAndEmoji(GenericFood food)
        {
            if (food == null) return ($"🥩 {GetLocalizedCategory("FC_CAT_PROTEIN")}", "🥩");

            // Derive emoji based on foodGroupSlug, foodGroup, or flags
            string emoji = Components.FMSearchOrCategoryField.GetCategoryEmoji(food.foodGroupSlug ?? food.foodGroup);
            if (emoji == "📦")
            {
                if (food.legume) emoji = "🫘";
                else if (food.meatOrFish) emoji = "🥩";
                else if (food.vegan) emoji = "🌱";
                else emoji = "🥩";
            }

            // Category text comes directly from food groups as defined in database
            string groupName = !string.IsNullOrWhiteSpace(food.foodGroup)
                ? food.foodGroup.Trim()
                : GetLocalizedCategory("FC_CAT_PROTEIN"); // the slug is an English id, not display text

            if (food.legume && string.IsNullOrWhiteSpace(food.foodGroup))
            {
                groupName = GetLocalizedCategory("FC_CAT_LEGUMES");
                emoji = "🫘";
            }

            string displayCategory = groupName.StartsWith(emoji) ? groupName : $"{emoji} {groupName}";
            return (displayCategory, emoji);
        }

        // Same OFF taxonomy classification as the footprint estimate (no name matching)
        private static (string Category, string Emoji) ResolveCategoryAndEmoji(FoodProduct product)
        {
            IReadOnlyList<ProteinClass> classes = FoodFootprintCalculator.ClassifyProduct(product);

            string key;
            string emoji;
            if (classes.Count == 0)
            {
                key = "FC_CAT_PROTEIN";
                emoji = "🍽️";
            }
            else if (classes.Contains(ProteinClass.Beef) || classes.Contains(ProteinClass.Lamb)
                || classes.Contains(ProteinClass.Pork) || classes.Contains(ProteinClass.UnspecifiedMeat))
            {
                key = "FC_CAT_RED_MEAT";
                emoji = "🥩";
            }
            else
            {
                switch (classes[0])
                {
                    case ProteinClass.Poultry:
                        key = "FC_CAT_WHITE_MEAT";
                        emoji = "🍗";
                        break;
                    case ProteinClass.Fish:
                        key = "FC_CAT_FISH";
                        emoji = "🐟";
                        break;
                    case ProteinClass.Cheese:
                        key = "FC_CAT_CHEESE";
                        emoji = "🧀";
                        break;
                    case ProteinClass.Eggs:
                        key = "FC_CAT_EGGS";
                        emoji = "🥚";
                        break;
                    case ProteinClass.PlantAlternative:
                        key = "FC_CAT_PLANT_ALT";
                        emoji = "🌱";
                        break;
                    case ProteinClass.Legumes:
                        key = "FC_CAT_LEGUMES";
                        emoji = "🫘";
                        break;
                    default:
                        key = "FC_CAT_PROTEIN";
                        emoji = "🍽️";
                        break;
                }
            }

            return ($"{emoji} {GetLocalizedCategory(key)}", emoji);
        }
    }
}
