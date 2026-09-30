using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using UnityEngine.Localization.Settings;
using eu.foodmission.platform;

namespace eu.foodmission.platform.Tests
{
    [TestFixture]
    public class FoodComparisonViewModelTests
    {
        private Mock<IStoreService> _mockStore;
        private Mock<IShoppingListService> _mockShoppingListService;
        private Mock<IFoodFootprintCalculator> _mockCalculator;
        private Mock<IChallengeService> _mockChallengeService;
        private FoodComparisonViewModel _vm;

        [SetUp]
        public void SetUp()
        {
            _mockStore = new Mock<IStoreService>();
            _mockShoppingListService = new Mock<IShoppingListService>();
            _mockCalculator = new Mock<IFoodFootprintCalculator>();
            _mockChallengeService = new Mock<IChallengeService>();

            _mockStore.Setup(s => s.GetAppState()).Returns(new AppState());

            _vm = new FoodComparisonViewModel(
                _mockStore.Object,
                _mockShoppingListService.Object,
                _mockCalculator.Object,
                _mockChallengeService.Object
            );
        }

        [Test]
        public async Task LoadComparisonDataAsync_WhenFewerThan2Proteins_SetsHasInsufficientProteinsTrue()
        {
            // Setup list with non-protein items
            var lists = new[] { new ShoppingList { id = "list-1", title = "Mi compra" } };
            _mockShoppingListService.Setup(s => s.GetListsAsync())
                .ReturnsAsync((lists, null));

            var items = new[]
            {
                new ShoppingListItem
                {
                    id = "item-1",
                    genericFood = new GenericFood { foodName = "Manzana", legume = false, meatOrFish = false }
                }
            };
            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<GenericFood>())).Returns(false);
            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1"))
                .ReturnsAsync((items, null));

            await _vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "proteins", source: "shopping_list");

            Assert.IsTrue(_vm.HasInsufficientProteins);
            Assert.AreEqual(ComparisonState.SelectionOrEmpty, _vm.CurrentState);
        }

        [Test]
        public async Task LoadComparisonDataAsync_When2OrMoreProteins_SelectsFirstTwoAndEntersDuel()
        {
            var lists = new[] { new ShoppingList { id = "list-1", title = "Mi compra" } };
            _mockShoppingListService.Setup(s => s.GetListsAsync())
                .ReturnsAsync((lists, null));

            var items = new[]
            {
                new ShoppingListItem
                {
                    id = "p-1",
                    genericFood = new GenericFood { foodName = "Ternera", meatOrFish = true }
                },
                new ShoppingListItem
                {
                    id = "p-2",
                    genericFood = new GenericFood { foodName = "Lentejas", legume = true }
                }
            };
            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<GenericFood>())).Returns(true);
            _mockCalculator.Setup(c => c.CalculateGenericFoodFootprint(It.Is<GenericFood>(g => g.foodName == "Ternera")))
                .Returns(new FootprintMetric { CarbonFootprintKg = 60.0f, CompositeImpactScore = 33.0f });
            _mockCalculator.Setup(c => c.CalculateGenericFoodFootprint(It.Is<GenericFood>(g => g.foodName == "Lentejas")))
                .Returns(new FootprintMetric { CarbonFootprintKg = 1.2f, CompositeImpactScore = 1.0f });

            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1"))
                .ReturnsAsync((items, null));

            await _vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "proteins", source: "shopping_list");

            Assert.IsFalse(_vm.HasInsufficientProteins);
            Assert.AreEqual(ComparisonState.NutriDuel, _vm.CurrentState);
            Assert.IsNotNull(_vm.ItemA);
            Assert.IsNotNull(_vm.ItemB);
        }

        [Test]
        public async Task SubmitGuessAsync_UpdatesChallengeProgressAndSetsReward()
        {
            _vm.ItemA = new FoodComparisonItem { Id = "1", Name = "Ternera", FootprintKg = 60.0f };
            _vm.ItemB = new FoodComparisonItem { Id = "2", Name = "Lentejas", FootprintKg = 1.2f };
            _vm.ChallengeCode = "CH.B1.1";

            var updatedProgress = new ChallengeProgress
            {
                challengeCode = "CH.B1.1",
                completed = true,
                progress = 100f,
                reward = new ContentReward { xp = 15, points = 20 }
            };
            _mockChallengeService.Setup(c => c.UpdateChallengeProgressAsync("CH.B1.1", true, 100f, null))
                .ReturnsAsync((updatedProgress, (ApiErrorResponse)null));

            // User bets on ItemB (Lentejas - lowest footprint, index 1)
            await _vm.SubmitGuessAsync(selectedItemIndex: 1);

            Assert.IsTrue(_vm.IsGuessRevealed);
            Assert.IsTrue(_vm.IsGuessCorrect);
            Assert.AreEqual(1, _vm.WinnerIndex);
            Assert.IsNotNull(_vm.EarnedReward);
            Assert.AreEqual(15, _vm.EarnedReward.xp);
            Assert.AreEqual(20, _vm.EarnedReward.points);

            _mockChallengeService.Verify(c => c.UpdateChallengeProgressAsync(
                "CH.B1.1", true, 100f, null
            ), Times.Once);
        }

        [Test]
        public async Task CompleteChallengeAsync_WhenApiFails_StaysIncompleteExposesErrorAndCanRetry()
        {
            _vm.ChallengeCode = "CH.B1.1";
            var apiError = new ApiErrorResponse { statusCode = 503, message = "Service unavailable" };
            var progress = new ChallengeProgress { challengeCode = "CH.B1.1", completed = true, progress = 100f };

            _mockChallengeService.SetupSequence(c => c.UpdateChallengeProgressAsync("CH.B1.1", true, 100f, null))
                .ReturnsAsync(((ChallengeProgress)null, apiError))
                .ReturnsAsync((progress, (ApiErrorResponse)null));

            await _vm.CompleteChallengeAsync();

            Assert.IsFalse(_vm.IsChallengeCompleted);
            Assert.AreSame(apiError, _vm.ErrorDetail);

            await _vm.CompleteChallengeAsync();

            Assert.IsTrue(_vm.IsChallengeCompleted);
            Assert.IsNull(_vm.ErrorDetail);
            _mockChallengeService.Verify(c => c.UpdateChallengeProgressAsync("CH.B1.1", true, 100f, null), Times.Exactly(2));
        }

        [Test]
        public async Task CompleteChallengeAsync_ConcurrentCalls_ShareOneRequest()
        {
            _vm.ChallengeCode = "CH.B1.1";
            var pending = new TaskCompletionSource<(ChallengeProgress, ApiErrorResponse)>();
            _mockChallengeService.Setup(c => c.UpdateChallengeProgressAsync("CH.B1.1", true, 100f, null))
                .Returns(pending.Task);

            // The guess starts the request and the Done button calls again before it finishes
            Task<bool> fromGuess = _vm.CompleteChallengeAsync();
            Task<bool> fromDone = _vm.CompleteChallengeAsync();
            pending.SetResult((new ChallengeProgress { challengeCode = "CH.B1.1", completed = true }, null));
            await Task.WhenAll(fromGuess, fromDone);

            Assert.IsTrue(_vm.IsChallengeCompleted);
            _mockChallengeService.Verify(c => c.UpdateChallengeProgressAsync("CH.B1.1", true, 100f, null), Times.Once);
        }

        [Test]
        public async Task LoadComparisonDataAsync_UsesUserLastShoppingListId_WhenSetInAppState()
        {
            var appState = new AppState { userLastShoppingListId = "list-fav" };
            _mockStore.Setup(s => s.GetAppState()).Returns(appState);

            var lists = new[]
            {
                new ShoppingList { id = "list-old", title = "Vieja" },
                new ShoppingList { id = "list-fav", title = "Mi Lista Habitual" }
            };
            _mockShoppingListService.Setup(s => s.GetListsAsync()).ReturnsAsync((lists, null));

            var itemsFav = new[]
            {
                new ShoppingListItem { id = "p-1", genericFood = new GenericFood { foodName = "Pollo", meatOrFish = true } },
                new ShoppingListItem { id = "p-2", genericFood = new GenericFood { foodName = "Huevos", foodGroupSlug = "eggs" } }
            };
            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-fav")).ReturnsAsync((itemsFav, null));
            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<GenericFood>())).Returns(true);
            _mockCalculator.Setup(c => c.CalculateGenericFoodFootprint(It.IsAny<GenericFood>()))
                .Returns(new FootprintMetric { CarbonFootprintKg = 5.0f, CompositeImpactScore = 10.0f });

            await _vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "proteins", source: "shopping_list");

            Assert.AreEqual("list-fav", _vm.DefaultShoppingListId);
            Assert.AreEqual("Mi Lista Habitual", _vm.DefaultShoppingListTitle);
            Assert.AreEqual(2, _vm.AvailableItems.Count);
        }

        [Test]
        public async Task LoadComparisonDataAsync_EnrichesMissingGenericFood_ViaGenericFoodService()
        {
            var mockGenericFoodService = new Mock<IGenericFoodService>();
            var vm = new FoodComparisonViewModel(
                _mockStore.Object,
                _mockShoppingListService.Object,
                _mockCalculator.Object,
                _mockChallengeService.Object,
                foodProductService: null,
                genericFoodService: mockGenericFoodService.Object
            );

            var lists = new[] { new ShoppingList { id = "list-1", title = "Mi compra" } };
            _mockShoppingListService.Setup(s => s.GetListsAsync()).ReturnsAsync((lists, null));

            // ShoppingListItem without inline genericFood but with genericFoodId
            var items = new[]
            {
                new ShoppingListItem { id = "item-raw-1", genericFoodId = "gf-100", genericFood = null },
                new ShoppingListItem { id = "item-raw-2", genericFoodId = "gf-200", genericFood = null }
            };
            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1")).ReturnsAsync((items, null));

            var enrichedGf1 = new GenericFood { id = "gf-100", foodName = "Atún", meatOrFish = true };
            var enrichedGf2 = new GenericFood { id = "gf-200", foodName = "Garbanzos", legume = true };

            mockGenericFoodService.Setup(s => s.GetGenericFoodByIdAsync("gf-100")).ReturnsAsync((enrichedGf1, null));
            mockGenericFoodService.Setup(s => s.GetGenericFoodByIdAsync("gf-200")).ReturnsAsync((enrichedGf2, null));

            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<GenericFood>())).Returns(true);
            _mockCalculator.Setup(c => c.CalculateGenericFoodFootprint(It.IsAny<GenericFood>()))
                .Returns(new FootprintMetric { CarbonFootprintKg = 3.0f, CompositeImpactScore = 5.0f });

            await vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "proteins", source: "shopping_list");

            Assert.IsFalse(vm.HasInsufficientProteins);
            Assert.AreEqual(2, vm.AvailableItems.Count);
            Assert.AreEqual("Atún", vm.AvailableItems[0].Name);
            Assert.AreEqual("Garbanzos", vm.AvailableItems[1].Name);
        }

        [Test]
        public async Task LoadComparisonDataAsync_WhenGenericMeatHasNoLangualCodes_EnrichesViaGenericFoodService()
        {
            var mockGenericFoodService = new Mock<IGenericFoodService>();
            var vm = new FoodComparisonViewModel(
                _mockStore.Object,
                _mockShoppingListService.Object,
                _mockCalculator.Object,
                _mockChallengeService.Object,
                foodProductService: null,
                genericFoodService: mockGenericFoodService.Object
            );

            var lists = new[] { new ShoppingList { id = "list-1", title = "Mi compra" } };
            _mockShoppingListService.Setup(s => s.GetListsAsync()).ReturnsAsync((lists, null));

            // Named and recognized as protein, but meat without species codes; eggs need no species codes
            var items = new[]
            {
                new ShoppingListItem { id = "item-1", genericFoodId = "gf-beef", genericFood = new GenericFood { id = "gf-beef", foodName = "Ternera", foodGroupSlug = "meat-and-poultry" } },
                new ShoppingListItem { id = "item-2", genericFoodId = "gf-eggs", genericFood = new GenericFood { id = "gf-eggs", foodName = "Huevos", foodGroupSlug = "eggs" } }
            };
            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1")).ReturnsAsync((items, null));

            var enrichedBeef = new GenericFood { id = "gf-beef", foodName = "Ternera", foodGroupSlug = "meat-and-poultry", langualCodes = new[] { "B1201" } };
            mockGenericFoodService.Setup(s => s.GetGenericFoodByIdAsync("gf-beef")).ReturnsAsync((enrichedBeef, null));

            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<GenericFood>())).Returns(true);
            _mockCalculator.Setup(c => c.CalculateGenericFoodFootprint(It.IsAny<GenericFood>()))
                .Returns(new FootprintMetric { CarbonFootprintKg = 3.0f, CompositeImpactScore = 5.0f });

            await vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "proteins", source: "shopping_list");

            mockGenericFoodService.Verify(s => s.GetGenericFoodByIdAsync("gf-beef"), Times.Once);
            mockGenericFoodService.Verify(s => s.GetGenericFoodByIdAsync("gf-eggs"), Times.Never);
            _mockCalculator.Verify(c => c.CalculateGenericFoodFootprint(It.Is<GenericFood>(g => g.langualCodes != null && g.langualCodes[0] == "B1201")), Times.Once);
        }

        [Test]
        public async Task LoadComparisonDataAsync_WhenGenericFoodLookupIsThrottled_RetriesAndEnriches()
        {
            var mockGenericFoodService = new Mock<IGenericFoodService>();
            var vm = new FoodComparisonViewModel(
                _mockStore.Object,
                _mockShoppingListService.Object,
                _mockCalculator.Object,
                _mockChallengeService.Object,
                foodProductService: null,
                genericFoodService: mockGenericFoodService.Object
            );

            var lists = new[] { new ShoppingList { id = "list-1", title = "Mi compra" } };
            _mockShoppingListService.Setup(s => s.GetListsAsync()).ReturnsAsync((lists, null));

            var items = new[] { new ShoppingListItem { id = "item-1", genericFoodId = "gf-throttle-1", genericFood = null } };
            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1")).ReturnsAsync((items, null));

            mockGenericFoodService.SetupSequence(s => s.GetGenericFoodByIdAsync("gf-throttle-1"))
                .ReturnsAsync(((GenericFood)null, new ApiErrorResponse { statusCode = 429, error = "ThrottlerException" }))
                .ReturnsAsync((new GenericFood { id = "gf-throttle-1", foodName = "Lentils", foodGroupSlug = "legumes" }, (ApiErrorResponse)null));

            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<GenericFood>())).Returns(false);

            await vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "proteins", source: "shopping_list");

            Assert.AreEqual("Lentils", items[0].genericFood?.foodName);
            mockGenericFoodService.Verify(s => s.GetGenericFoodByIdAsync("gf-throttle-1"), Times.Exactly(2));
        }

        [Test]
        public async Task LoadComparisonDataAsync_WhenGenericFoodHasEmptyFoodName_EnrichesViaGenericFoodService()
        {
            var mockGenericFoodService = new Mock<IGenericFoodService>();
            var vm = new FoodComparisonViewModel(
                _mockStore.Object,
                _mockShoppingListService.Object,
                _mockCalculator.Object,
                _mockChallengeService.Object,
                foodProductService: null,
                genericFoodService: mockGenericFoodService.Object
            );

            var lists = new[] { new ShoppingList { id = "list-1", title = "Mi compra" } };
            _mockShoppingListService.Setup(s => s.GetListsAsync()).ReturnsAsync((lists, null));

            // ShoppingListItem with non-null genericFood but empty foodName
            var items = new[]
            {
                new ShoppingListItem { id = "item-raw-1", genericFoodId = "gf-100", genericFood = new GenericFood { id = "gf-100", foodName = null } },
                new ShoppingListItem { id = "item-raw-2", genericFoodId = "gf-200", genericFood = new GenericFood { id = "gf-200", foodName = "" } }
            };
            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1")).ReturnsAsync((items, null));

            var enrichedGf1 = new GenericFood { id = "gf-100", foodName = "Salmón", meatOrFish = true };
            var enrichedGf2 = new GenericFood { id = "gf-200", foodName = "Lentejas", legume = true };

            mockGenericFoodService.Setup(s => s.GetGenericFoodByIdAsync("gf-100")).ReturnsAsync((enrichedGf1, null));
            mockGenericFoodService.Setup(s => s.GetGenericFoodByIdAsync("gf-200")).ReturnsAsync((enrichedGf2, null));

            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<GenericFood>())).Returns(true);
            _mockCalculator.Setup(c => c.CalculateGenericFoodFootprint(It.IsAny<GenericFood>()))
                .Returns(new FootprintMetric { CarbonFootprintKg = 2.0f, CompositeImpactScore = 3.0f });

            await vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "proteins", source: "shopping_list");

            Assert.IsFalse(vm.HasInsufficientProteins);
            Assert.AreEqual(2, vm.AvailableItems.Count);
            Assert.AreEqual("Salmón", vm.AvailableItems[0].Name);
            Assert.AreEqual("Lentejas", vm.AvailableItems[1].Name);
        }

        [Test]
        public void NavigateToShoppingList_WhenDefaultListKnown_NavigatesToShoppingListDetailWithArgs()
        {
            _vm.DefaultShoppingListId = "list-123";
            _vm.DefaultShoppingListTitle = "Mi Lista";

            string requestedAction = null;
            Unity.AppUI.Navigation.Argument[] requestedArgs = null;

            _vm.NavigationRequested += (action, args) =>
            {
                requestedAction = action;
                requestedArgs = args;
            };

            _vm.NavigateToShoppingList();

            Assert.AreEqual(Unity.AppUI.Navigation.Generated.Actions.shopping_list_to_detail, requestedAction);
            Assert.IsNotNull(requestedArgs);
            Assert.AreEqual(2, requestedArgs.Length);
            Assert.AreEqual("listId", requestedArgs[0].name);
            Assert.AreEqual("list-123", requestedArgs[0].value);
            Assert.AreEqual("listTitle", requestedArgs[1].name);
            Assert.AreEqual("Mi Lista", requestedArgs[1].value);
        }

        [Test]
        public async Task LoadComparisonDataAsync_WhenFoodProductHasImage_MapsImageUrlCorrectly()
        {
            var lists = new[] { new ShoppingList { id = "list-1", title = "Mi compra" } };
            _mockShoppingListService.Setup(s => s.GetListsAsync()).ReturnsAsync((lists, null));

            var items = new[]
            {
                new ShoppingListItem
                {
                    id = "p-1",
                    foodProduct = new FoodProduct
                    {
                        id = "prod-1",
                        name = "Tofu Bio",
                        imageUrl = "https://images.openfoodfacts.org/images/products/tofu.jpg"
                    }
                },
                new ShoppingListItem
                {
                    id = "p-2",
                    foodProduct = new FoodProduct
                    {
                        id = "prod-2",
                        name = "Pollo Asado",
                        imageFrontUrl = "https://images.openfoodfacts.org/images/products/pollo.jpg"
                    }
                }
            };

            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<FoodProduct>())).Returns(true);
            _mockCalculator.Setup(c => c.CalculateProductFootprint(It.IsAny<FoodProduct>()))
                .Returns(new FootprintMetric { CarbonFootprintKg = 4.0f, CompositeImpactScore = 6.0f });

            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1")).ReturnsAsync((items, null));

            await _vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "proteins", source: "shopping_list");

            Assert.IsFalse(_vm.HasInsufficientProteins);
            Assert.AreEqual("https://images.openfoodfacts.org/images/products/tofu.jpg", _vm.ItemA.ImageUrl);
            Assert.AreEqual("https://images.openfoodfacts.org/images/products/pollo.jpg", _vm.ItemB.ImageUrl);
        }

        [Test]
        public async Task LoadComparisonDataAsync_WhenProductImageUrlIsNull_EnrichesViaFoodProductDetail()
        {
            var mockFoodProductService = new Mock<IFoodProductService>();
            var vm = new FoodComparisonViewModel(
                _mockStore.Object,
                _mockShoppingListService.Object,
                _mockCalculator.Object,
                _mockChallengeService.Object,
                mockFoodProductService.Object
            );

            var lists = new[] { new ShoppingList { id = "list-1", title = "Mi compra" } };
            _mockShoppingListService.Setup(s => s.GetListsAsync()).ReturnsAsync((lists, null));

            var items = new[]
            {
                new ShoppingListItem
                {
                    id = "p-1",
                    foodProductId = "prod-100",
                    foodProduct = new FoodProduct
                    {
                        id = "prod-100",
                        name = "Tofu Bratfilets",
                        imageUrl = null // No direct imageUrl
                    }
                },
                new ShoppingListItem
                {
                    id = "p-2",
                    genericFood = new GenericFood { foodName = "Lentejas", legume = true }
                }
            };

            mockFoodProductService.Setup(s => s.GetFoodProductDetailAsync("prod-100"))
                .ReturnsAsync((new FoodProductDetail
                {
                    id = "prod-100",
                    imageUrl = "https://images.openfoodfacts.org/images/products/tofu-bratfilets.jpg"
                }, null));

            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<FoodProduct>())).Returns(true);
            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<GenericFood>())).Returns(true);
            _mockCalculator.Setup(c => c.CalculateProductFootprint(It.IsAny<FoodProduct>()))
                .Returns(new FootprintMetric { CarbonFootprintKg = 3.0f, CompositeImpactScore = 2.0f });
            _mockCalculator.Setup(c => c.CalculateGenericFoodFootprint(It.IsAny<GenericFood>()))
                .Returns(new FootprintMetric { CarbonFootprintKg = 1.2f, CompositeImpactScore = 1.0f });

            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1")).ReturnsAsync((items, null));

            await vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "proteins", source: "shopping_list");
            if (vm.ImageFetchTask != null) await vm.ImageFetchTask;

            Assert.IsFalse(vm.HasInsufficientProteins);
            Assert.AreEqual("https://images.openfoodfacts.org/images/products/tofu-bratfilets.jpg", vm.ItemA.ImageUrl);
            mockFoodProductService.Verify(s => s.GetFoodProductDetailAsync("prod-100"), Times.Once);
            mockFoodProductService.Verify(s => s.GetFoodProductDetailAsync("p-2"), Times.Never);
        }

        [Test]
        public async Task LoadComparisonDataAsync_WhenProductHasNoCategories_CopiesOpenFoodFactsCategoriesByBarcode()
        {
            var mockFoodProductService = new Mock<IFoodProductService>();
            var vm = new FoodComparisonViewModel(
                _mockStore.Object,
                _mockShoppingListService.Object,
                _mockCalculator.Object,
                _mockChallengeService.Object,
                mockFoodProductService.Object
            );

            var lists = new[] { new ShoppingList { id = "list-1", title = "Mi compra" } };
            _mockShoppingListService.Setup(s => s.GetListsAsync()).ReturnsAsync((lists, null));

            var chicken = new FoodProduct { id = "prod-local-1", name = "Pollo 1954", barcode = "8410843158622" };
            var tofu = new FoodProduct { id = "prod-local-2", name = "Tofu", barcode = "4012359144003", categories = new[] { "en:meat-alternatives" }, nutriscoreGrade = "b", novaGroup = 3 };
            var items = new[]
            {
                new ShoppingListItem { id = "p-1", foodProductId = "prod-local-1", foodProduct = chicken },
                new ShoppingListItem { id = "p-2", foodProductId = "prod-local-2", foodProduct = tofu }
            };
            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1")).ReturnsAsync((items, null));

            // Barcode lookup returns the OFF passthrough, whose id is the barcode
            mockFoodProductService.Setup(s => s.FindByBarcodeAsync("8410843158622", true))
                .ReturnsAsync((new FoodProduct { id = "8410843158622", categories = new[] { "en:meats", "en:poultries" } }, null));

            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<FoodProduct>())).Returns(true);
            _mockCalculator.Setup(c => c.CalculateProductFootprint(It.IsAny<FoodProduct>()))
                .Returns(new FootprintMetric { CarbonFootprintKg = 6.0f, CompositeImpactScore = 6.0f });

            await vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "proteins", source: "shopping_list");

            CollectionAssert.AreEqual(new[] { "en:meats", "en:poultries" }, chicken.categories);
            Assert.AreEqual("prod-local-1", chicken.id);
            mockFoodProductService.Verify(s => s.FindByBarcodeAsync("4012359144003", It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public async Task LoadComparisonDataAsync_ScoresComeOnlyFromBackendData()
        {
            var mockFoodProductService = new Mock<IFoodProductService>();
            var vm = new FoodComparisonViewModel(
                _mockStore.Object,
                _mockShoppingListService.Object,
                _mockCalculator.Object,
                _mockChallengeService.Object,
                mockFoodProductService.Object
            );

            var lists = new[] { new ShoppingList { id = "list-1", title = "Mi compra" } };
            _mockShoppingListService.Setup(s => s.GetListsAsync()).ReturnsAsync((lists, null));

            var tofu = new FoodProduct { id = "prod-scores-1", name = "Tofu", barcode = "scores-test-4012359144003", categories = new[] { "en:meat-alternatives" } };
            var items = new[]
            {
                new ShoppingListItem { id = "p-1", foodProductId = "prod-scores-1", foodProduct = tofu },
                new ShoppingListItem { id = "p-2", genericFood = new GenericFood { id = "gf-scores-1", foodName = "Lentils", foodGroupSlug = "legumes" } }
            };
            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1")).ReturnsAsync((items, null));

            mockFoodProductService.Setup(s => s.FindByBarcodeAsync("scores-test-4012359144003", true))
                .ReturnsAsync((new FoodProduct
                {
                    id = "scores-test-4012359144003",
                    openFoodFactsInfo = new OpenFoodFactsInfoDto { nutritionGrade = "b", novaGroup = 3 }
                }, (ApiErrorResponse)null));

            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<FoodProduct>())).Returns(true);
            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<GenericFood>())).Returns(true);
            _mockCalculator.Setup(c => c.CalculateProductFootprint(It.IsAny<FoodProduct>()))
                .Returns(new FootprintMetric { CarbonFootprintKg = 3.0f, CompositeImpactScore = 1.8f });
            _mockCalculator.Setup(c => c.CalculateGenericFoodFootprint(It.IsAny<GenericFood>()))
                .Returns(new FootprintMetric { CarbonFootprintKg = 1.2f, CompositeImpactScore = 1.0f });

            await vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "proteins", source: "shopping_list");

            var product = vm.AvailableItems.Find(i => i.Id == "prod-scores-1");
            Assert.IsNotNull(product);
            Assert.AreEqual("B", product.NutriScore);
            Assert.AreEqual(3, product.NovaGroup);
            Assert.IsNull(product.EcoScore);

            var generic = vm.AvailableItems.Find(i => i.Id == "gf-scores-1");
            Assert.IsNotNull(generic);
            Assert.IsNull(generic.NutriScore);
            Assert.IsNull(generic.EcoScore);
            Assert.IsNull(generic.NovaGroup);
        }

        [Test]
        public async Task LoadComparisonDataAsync_WhenBarcodeLookupIsThrottled_RetriesAndCopiesCategories()
        {
            var mockFoodProductService = new Mock<IFoodProductService>();
            var vm = new FoodComparisonViewModel(
                _mockStore.Object,
                _mockShoppingListService.Object,
                _mockCalculator.Object,
                _mockChallengeService.Object,
                mockFoodProductService.Object
            );

            var lists = new[] { new ShoppingList { id = "list-1", title = "Mi compra" } };
            _mockShoppingListService.Setup(s => s.GetListsAsync()).ReturnsAsync((lists, null));

            var fishSticks = new FoodProduct { id = "prod-local-3", name = "Bâtonnets Colin d'Alaska MSC", barcode = "throttle-test-3599741003311" };
            var items = new[] { new ShoppingListItem { id = "p-1", foodProductId = "prod-local-3", foodProduct = fishSticks } };
            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1")).ReturnsAsync((items, null));

            mockFoodProductService.SetupSequence(s => s.FindByBarcodeAsync("throttle-test-3599741003311", true))
                .ReturnsAsync(((FoodProduct)null, new ApiErrorResponse { statusCode = 429, error = "ThrottlerException" }))
                .ReturnsAsync((new FoodProduct { categories = new[] { "en:seafood", "en:fishes-and-their-products" } }, (ApiErrorResponse)null));

            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<FoodProduct>())).Returns(false);

            await vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "proteins", source: "shopping_list");

            CollectionAssert.AreEqual(new[] { "en:seafood", "en:fishes-and-their-products" }, fishSticks.categories);
            mockFoodProductService.Verify(s => s.FindByBarcodeAsync("throttle-test-3599741003311", true), Times.Exactly(2));
        }

        [Test]
        public async Task LoadComparisonDataAsync_WhenBothItemsAreGenericFoods_NeverCallsFoodProductDetail()
        {
            var mockFoodProductService = new Mock<IFoodProductService>();
            var vm = new FoodComparisonViewModel(
                _mockStore.Object,
                _mockShoppingListService.Object,
                _mockCalculator.Object,
                _mockChallengeService.Object,
                mockFoodProductService.Object
            );

            var lists = new[] { new ShoppingList { id = "list-1", title = "Mi compra" } };
            _mockShoppingListService.Setup(s => s.GetListsAsync()).ReturnsAsync((lists, null));

            var items = new[]
            {
                new ShoppingListItem
                {
                    id = "item-g1",
                    genericFood = new GenericFood { id = "g-1", foodName = "Ternera", meatOrFish = true }
                },
                new ShoppingListItem
                {
                    id = "item-g2",
                    genericFood = new GenericFood { id = "g-2", foodName = "Lentejas", legume = true }
                }
            };

            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<GenericFood>())).Returns(true);
            _mockCalculator.Setup(c => c.CalculateGenericFoodFootprint(It.IsAny<GenericFood>()))
                .Returns(new FootprintMetric { CarbonFootprintKg = 5.0f, CompositeImpactScore = 10.0f });

            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1")).ReturnsAsync((items, null));

            await vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "proteins", source: "shopping_list");
            if (vm.ImageFetchTask != null) await vm.ImageFetchTask;

            Assert.IsFalse(vm.HasInsufficientProteins);
            mockFoodProductService.Verify(s => s.GetFoodProductDetailAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task LoadComparisonDataAsync_WhenMoreThan2Proteins_SelectsTwoDistinctItems()
        {
            var lists = new[] { new ShoppingList { id = "list-1", title = "Mi compra" } };
            _mockShoppingListService.Setup(s => s.GetListsAsync()).ReturnsAsync((lists, null));

            var items = new[]
            {
                new ShoppingListItem { id = "p-1", genericFood = new GenericFood { id = "g-1", foodName = "Ternera", meatOrFish = true } },
                new ShoppingListItem { id = "p-2", genericFood = new GenericFood { id = "g-2", foodName = "Lentejas", legume = true } },
                new ShoppingListItem { id = "p-3", genericFood = new GenericFood { id = "g-3", foodName = "Pollo", meatOrFish = true } },
                new ShoppingListItem { id = "p-4", genericFood = new GenericFood { id = "g-4", foodName = "Tofu", foodGroupSlug = "meat-substitutes-and-dairy-substitutes" } }
            };

            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<GenericFood>())).Returns(true);
            _mockCalculator.Setup(c => c.CalculateGenericFoodFootprint(It.IsAny<GenericFood>()))
                .Returns(new FootprintMetric { CarbonFootprintKg = 5.0f, CompositeImpactScore = 10.0f });

            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1")).ReturnsAsync((items, null));

            await _vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "proteins", source: "shopping_list");

            Assert.IsFalse(_vm.HasInsufficientProteins);
            Assert.AreEqual(4, _vm.AvailableItems.Count);
            Assert.IsNotNull(_vm.ItemA);
            Assert.IsNotNull(_vm.ItemB);
            Assert.AreNotEqual(_vm.ItemA.Id, _vm.ItemB.Id);
            Assert.IsTrue(_vm.AvailableItems.Exists(x => x.Id == _vm.ItemA.Id));
            Assert.IsTrue(_vm.AvailableItems.Exists(x => x.Id == _vm.ItemB.Id));
            Assert.AreEqual(ComparisonState.NutriDuel, _vm.CurrentState);
        }

        [Test]
        public async Task ResetComparison_WhenMultipleProteinsAvailable_ReSelectsContestantsAndResetsDuel()
        {
            var lists = new[] { new ShoppingList { id = "list-1", title = "Mi compra" } };
            _mockShoppingListService.Setup(s => s.GetListsAsync()).ReturnsAsync((lists, null));

            var items = new[]
            {
                new ShoppingListItem { id = "p-1", genericFood = new GenericFood { id = "g-1", foodName = "Ternera", meatOrFish = true } },
                new ShoppingListItem { id = "p-2", genericFood = new GenericFood { id = "g-2", foodName = "Lentejas", legume = true } },
                new ShoppingListItem { id = "p-3", genericFood = new GenericFood { id = "g-3", foodName = "Huevos", foodGroupSlug = "eggs" } }
            };

            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<GenericFood>())).Returns(true);
            _mockCalculator.Setup(c => c.CalculateGenericFoodFootprint(It.IsAny<GenericFood>()))
                .Returns(new FootprintMetric { CarbonFootprintKg = 4.0f, CompositeImpactScore = 8.0f });

            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1")).ReturnsAsync((items, null));

            await _vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "proteins", source: "shopping_list");
            await _vm.SubmitGuessAsync(0);

            Assert.IsTrue(_vm.IsGuessRevealed);
            Assert.AreEqual(0, _vm.SelectedItemIndex);

            // User clicks "Comparar otros alimentos"
            _vm.ResetComparison();

            Assert.IsFalse(_vm.IsGuessRevealed);
            Assert.AreEqual(-1, _vm.SelectedItemIndex);
            Assert.IsNotNull(_vm.ItemA);
            Assert.IsNotNull(_vm.ItemB);
            Assert.AreNotEqual(_vm.ItemA.Id, _vm.ItemB.Id);
            Assert.AreEqual(ComparisonState.NutriDuel, _vm.CurrentState);
        }

        [Test]
        public void LoadSampleDuel_PopulatesBenchmarkProteins_AndEntersDuel()
        {
            _mockCalculator.Setup(c => c.CalculateGenericFoodFootprint(It.IsAny<GenericFood>()))
                .Returns((GenericFood g) => new FootprintMetric
                {
                    CarbonFootprintKg = g.meatOrFish ? 20.0f : 1.5f,
                    CompositeImpactScore = g.meatOrFish ? 30.0f : 2.0f
                });

            _vm.HasInsufficientProteins = true;
            _vm.CurrentState = ComparisonState.SelectionOrEmpty;
            // The duel is only entered from a challenge; without one the sample opens the full comparison.
            _vm.ChallengeCode = "CH.B1.1";

            _vm.LoadSampleDuel();

            Assert.IsFalse(_vm.HasInsufficientProteins);
            Assert.AreEqual(6, _vm.AvailableItems.Count);
            Assert.AreEqual(ComparisonState.NutriDuel, _vm.CurrentState);
            Assert.IsNotNull(_vm.ItemA);
            Assert.IsNotNull(_vm.ItemB);
            Assert.AreNotEqual(_vm.ItemA.Id, _vm.ItemB.Id);
        }

        [Test]
        public async Task SubmitGuessAsync_AfterReset_RevealsFeedbackOfTheNewPairOnly()
        {
            _vm.LoadSampleDuel();

            string feedbackAtReveal = null;
            _vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(FoodComparisonViewModel.IsGuessRevealed) && _vm.IsGuessRevealed)
                {
                    feedbackAtReveal = _vm.DidacticFeedback;
                }
            };

            await _vm.SubmitGuessAsync(0);
            string firstFeedback = _vm.DidacticFeedback;
            Assert.IsFalse(string.IsNullOrEmpty(firstFeedback));
            Assert.AreEqual(firstFeedback, feedbackAtReveal, "Feedback must be ready when the reveal is triggered");

            _vm.ResetComparison();
            Assert.IsFalse(_vm.IsGuessRevealed);
            Assert.IsTrue(string.IsNullOrEmpty(_vm.DidacticFeedback), "Reset must clear the previous duel's feedback");
            Assert.AreEqual(-1, _vm.SelectedItemIndex);
            Assert.AreEqual(_vm.ItemA.FootprintKg <= _vm.ItemB.FootprintKg ? 0 : 1, _vm.WinnerIndex);

            await _vm.SubmitGuessAsync(1);
            Assert.AreEqual(_vm.DidacticFeedback, feedbackAtReveal);
        }

        [Test]
        public void FoodComparisonItem_FootprintDisclaimer_WhenExactConfidence_ReturnsDirectOpenFoodFacts()
        {
            var item = new FoodComparisonItem
            {
                Confidence = FootprintConfidence.Exact,
                ScientificRef = "Direct product data / OpenFoodFacts"
            };

            Assert.AreEqual(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "FC_DISCLAIMER_DIRECT"), item.FootprintDisclaimer);
        }

        [Test]
        public void FoodComparisonItem_FootprintDisclaimer_WhenEstimatedConfidence_ReturnsScientificEstimation()
        {
            var item = new FoodComparisonItem
            {
                Confidence = FootprintConfidence.Estimated,
                ScientificRef = "Clark et al. (PNAS 2022) / Poore & Nemecek (Science 2018)"
            };

            Assert.AreEqual(LocalizationSettings.StringDatabase.GetLocalizedString("UI", "FC_DISCLAIMER_ESTIMATE"), item.FootprintDisclaimer);
        }

        [Test]
        public async Task LoadComparisonDataAsync_WhenModeIsSample_DirectlyLoadsSampleDuel()
        {
            _mockCalculator.Setup(c => c.CalculateGenericFoodFootprint(It.IsAny<GenericFood>()))
                .Returns(new FootprintMetric { CarbonFootprintKg = 2.0f, CompositeImpactScore = 1.0f });

            await _vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "sample", source: "sample");

            Assert.IsFalse(_vm.HasInsufficientProteins);
            Assert.AreEqual(6, _vm.AvailableItems.Count);
            Assert.AreEqual(ComparisonState.NutriDuel, _vm.CurrentState);
            Assert.IsNotNull(_vm.ItemA);
            Assert.IsNotNull(_vm.ItemB);
        }

        [Test]
        public async Task LoadComparisonDataAsync_WhenModeIsMatrix_DirectlyEntersFullComparison()
        {
            _mockCalculator.Setup(c => c.CalculateGenericFoodFootprint(It.IsAny<GenericFood>()))
                .Returns(new FootprintMetric { CarbonFootprintKg = 2.0f, CompositeImpactScore = 1.0f });

            await _vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "matrix", source: "matrix");

            Assert.IsFalse(_vm.HasInsufficientProteins);
            Assert.AreEqual(ComparisonState.FullComparison, _vm.CurrentState);
            Assert.IsTrue(_vm.IsGuessRevealed);
            Assert.IsNotNull(_vm.ItemA);
            Assert.IsNotNull(_vm.ItemB);
        }

        [Test]
        public async Task LoadComparisonDataAsync_WhenModeIsEmpty_DirectlyEntersSelectionOrEmpty()
        {
            await _vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "empty", source: "empty");

            Assert.IsTrue(_vm.HasInsufficientProteins);
            Assert.AreEqual(ComparisonState.SelectionOrEmpty, _vm.CurrentState);
        }

        [Test]
        public async Task LoadComparisonDataAsync_WhenItemIsFishOrCaviar_AssignsFishEmojiAndCategory()
        {
            var lists = new[] { new ShoppingList { id = "list-1", title = "Mi compra" } };
            _mockShoppingListService.Setup(s => s.GetListsAsync()).ReturnsAsync((lists, null));

            var items = new[]
            {
                new ShoppingListItem
                {
                    id = "item-1",
                    genericFood = new GenericFood { id = "gf-caviar", foodName = "Caviale", foodGroupSlug = "fish-crustacean-and-shellfish", foodGroup = "Pescado", meatOrFish = true }
                },
                new ShoppingListItem
                {
                    id = "item-2",
                    genericFood = new GenericFood { id = "gf-beef", foodName = "Carne di manzo", foodGroupSlug = "meat-and-poultry", foodGroup = "Carne", meatOrFish = true }
                }
            };

            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1")).ReturnsAsync((items, null));
            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<GenericFood>())).Returns(true);
            _mockCalculator.Setup(c => c.CalculateGenericFoodFootprint(It.IsAny<GenericFood>()))
                .Returns(new FootprintMetric { CarbonFootprintKg = 5.0f, CompositeImpactScore = 10.0f });

            await _vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "proteins", source: "shopping_list");

            Assert.IsFalse(_vm.HasInsufficientProteins);
            Assert.AreEqual(2, _vm.AvailableItems.Count);

            var caviarItem = _vm.AvailableItems.Find(i => i.Id == "gf-caviar");
            Assert.IsNotNull(caviarItem);
            Assert.AreEqual("🐟", caviarItem.Emoji);
            Assert.AreEqual("🐟 Pescado", caviarItem.Category);

            var beefItem = _vm.AvailableItems.Find(i => i.Id == "gf-beef");
            Assert.IsNotNull(beefItem);
            // "meat-and-poultry" maps to 🍗 in FMSearchOrCategoryField.GetCategoryEmoji.
            Assert.AreEqual("🍗", beefItem.Emoji);
            Assert.AreEqual("🍗 Carne", beefItem.Category);
        }

        [Test]
        public async Task LoadComparisonDataAsync_WhenFoodProductHasTaxonomyCategories_AssignsMatchingCategoryAndEmoji()
        {
            var lists = new[] { new ShoppingList { id = "list-1", title = "Mi compra" } };
            _mockShoppingListService.Setup(s => s.GetListsAsync()).ReturnsAsync((lists, null));

            var items = new[]
            {
                new ShoppingListItem
                {
                    id = "item-fp-1",
                    foodProduct = new FoodProduct
                    {
                        id = "prod-fish-1",
                        name = "Filetes de bacalao",
                        categories = new[] { "en:seafood", "en:fishes-and-their-products", "en:fishes", "en:cods" }
                    }
                },
                new ShoppingListItem
                {
                    id = "item-fp-2",
                    foodProduct = new FoodProduct
                    {
                        id = "prod-tofu-1",
                        name = "Tofu ahumado",
                        categories = new[] { "en:plant-based-foods", "en:meat-alternatives", "xx:tofu" }
                    }
                }
            };

            _mockShoppingListService.Setup(s => s.GetItemsAsync("list-1")).ReturnsAsync((items, null));
            _mockCalculator.Setup(c => c.IsProtein(It.IsAny<FoodProduct>())).Returns(true);
            _mockCalculator.Setup(c => c.CalculateProductFootprint(It.IsAny<FoodProduct>()))
                .Returns(new FootprintMetric { CarbonFootprintKg = 2.0f, CompositeImpactScore = 3.0f });

            await _vm.LoadComparisonDataAsync(challengeCode: "CH.B1.1", mode: "proteins", source: "shopping_list");

            Assert.IsFalse(_vm.HasInsufficientProteins);
            Assert.AreEqual(2, _vm.AvailableItems.Count);

            var fishItem = _vm.AvailableItems.Find(i => i.Id == "prod-fish-1");
            Assert.IsNotNull(fishItem);
            Assert.AreEqual("🐟", fishItem.Emoji);
            Assert.AreEqual($"🐟 {LocalizationSettings.StringDatabase.GetLocalizedString("UI", "FC_CAT_FISH")}", fishItem.Category);

            var tofuItem = _vm.AvailableItems.Find(i => i.Id == "prod-tofu-1");
            Assert.IsNotNull(tofuItem);
            Assert.AreEqual("🌱", tofuItem.Emoji);
            Assert.AreEqual($"🌱 {LocalizationSettings.StringDatabase.GetLocalizedString("UI", "FC_CAT_PLANT_ALT")}", tofuItem.Category);
        }
    }
}
